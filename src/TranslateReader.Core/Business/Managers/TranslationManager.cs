using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;
using TranslateReader.Utilities;

namespace TranslateReader.Business.Managers;

public class TranslationManager(
    ITranslationEngine translationEngine,
    IModelAccess modelAccess,
    ITranslationCacheAccess translationCacheAccess,
    IBookTranslationJobAccess bookTranslationJobAccess,
    IPromptUtility promptUtility,
    IBooksAccess booksAccess,
    IParsingEngine parsingEngine,
    ISettingsAccess settingsAccess) : ITranslationManager
{
    private static readonly ModelInfo GemmaModel = new(
        Name: "gemma-2-2b",
        FileName: "gemma-2-2b-it-Q4_K_M.gguf",
        DownloadUrl: "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
        SizeBytes: 1_629_413_888);

    private static readonly ModelInfo HyMtModel = new(
        Name: "hy-mt1.5-1.8b",
        FileName: "HY-MT1.5-1.8B-Q4_K_M.gguf",
        DownloadUrl: "https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/resolve/main/HY-MT1.5-1.8B-Q4_K_M.gguf",
        SizeBytes: 1_133_080_512);

    private static readonly IReadOnlyDictionary<string, ModelInfo> ModelRegistry =
        new Dictionary<string, ModelInfo>(StringComparer.Ordinal)
        {
            [GemmaModel.Name] = GemmaModel,
            [HyMtModel.Name] = HyMtModel,
        };

    private const float TranslationTemperature = 0.1f;
    private const int MaxTokenMultiplier = 3;

    // Qwen/Phi are offered in the UI but have no real download URL yet (D-...-4); an unknown or
    // legacy settings value must resolve to a model that is actually downloadable, not throw.
    private static ModelInfo ResolveModel(string modelName) =>
        ModelRegistry.TryGetValue(modelName, out var model) ? model : GemmaModel;

    public async Task DownloadModelIfNeededAsync(IProgress<double>? progress, CancellationToken ct)
    {
        var settings = await settingsAccess.FetchSettingsAsync();
        var model = ResolveModel(settings.TranslationModelName);

        if (!modelAccess.IsModelAvailable(model.FileName))
            await modelAccess.DownloadModelAsync(model.DownloadUrl, progress, ct);
    }

    public async Task InitializeEngineIfNeededAsync(CancellationToken ct)
    {
        if (translationEngine.IsReady)
            return;

        var settings = await settingsAccess.FetchSettingsAsync();
        var model = ResolveModel(settings.TranslationModelName);
        await translationEngine.InitializeAsync(modelAccess.GetModelPath(model.FileName), ct);
    }

    public async Task<BookTranslationResult> TranslateBookAsync(
        int bookId,
        string sourceLanguage,
        string targetLanguage,
        string destinationDirectory,
        IProgress<BookTranslationProgress>? progress,
        CancellationToken ct)
    {
        var book = await booksAccess.FetchBookAsync(bookId);
        var chapters = await parsingEngine.ExtractChaptersAsync(book.FilePath);
        var job = await GetOrCreateJobAsync(bookId, sourceLanguage, targetLanguage);
        var startChapterIndex = job.LastCompletedChapterIndex + 1;
        var run = new TranslationRun(book, sourceLanguage, targetLanguage, progress);

        try
        {
            await TranslateChaptersWithCacheAsync(run, chapters, job, startChapterIndex, ct);
        }
        catch (OperationCanceledException)
        {
            await bookTranslationJobAccess.UpdateJobProgressAsync(job.Id, job.LastCompletedChapterIndex, "Paused");
            throw;
        }

        var rebuilt = await RebuildAllTranslatedChaptersAsync(
            book, chapters, sourceLanguage, targetLanguage);

        await bookTranslationJobAccess.DeleteJobAsync(job.Id);

        var translatedTitle = $"{book.Title} [{sourceLanguage} \u2192 {targetLanguage}]";
        var epubPath = await parsingEngine.CreateTranslatedEpubAsync(
            book.FilePath, translatedTitle, rebuilt.Chapters, destinationDirectory);
        return new BookTranslationResult(epubPath, rebuilt.CoveredTextRatio);
    }

    public async Task<BookTranslationJob?> GetActiveTranslationJobAsync(int bookId) =>
        await bookTranslationJobAccess.FetchActiveJobAsync(bookId);

    public async Task PauseTranslationAsync(int bookId)
    {
        var job = await bookTranslationJobAccess.FetchActiveJobAsync(bookId);
        if (job is not null)
            await bookTranslationJobAccess.UpdateJobProgressAsync(job.Id, job.LastCompletedChapterIndex, "Paused");
    }

    private async Task<BookTranslationJob> GetOrCreateJobAsync(
        int bookId, string sourceLanguage, string targetLanguage)
    {
        var existing = await bookTranslationJobAccess.FetchActiveJobAsync(bookId);
        if (existing is not null)
            return existing;

        var job = new BookTranslationJob
        {
            BookId = bookId,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Status = "InProgress",
            LastCompletedChapterIndex = -1
        };
        await bookTranslationJobAccess.SaveJobAsync(job);
        return job;
    }

    /// Groups the values that stay constant for a whole book translation, so the per-chapter
    /// helpers below stay inside the 7-parameter budget (S107).
    private sealed record TranslationRun(
        Book Book,
        string SourceLanguage,
        string TargetLanguage,
        IProgress<BookTranslationProgress>? Progress);

    private async Task TranslateSingleChapterAsync(
        TranslationRun run,
        Chapter chapter,
        int chapterIdx,
        int totalChapters,
        CancellationToken ct)
    {
        var book = run.Book;
        var html = await parsingEngine.ExtractChapterContentAsync(
            book.FilePath, chapter.HRef, string.Empty, ChapterContentPurpose.Export);
        var textBlocks = HtmlUtility.ExtractTextBlocks(HtmlUtility.ExtractBodyContent(html));

        for (var paraIdx = 0; paraIdx < textBlocks.Count; paraIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var original = textBlocks[paraIdx];
            var hash = ComputeHash(original, run.SourceLanguage, run.TargetLanguage);

            var cached = await translationCacheAccess.FetchTranslationAsync(book.Id, chapter.HRef, hash);
            if (cached is null)
            {
                var (systemMessage, userMessage) = promptUtility.BuildTranslationMessages(
                    original, run.SourceLanguage, run.TargetLanguage, book.Title, chapter.Title, null);
                var maxTokens = original.Length * MaxTokenMultiplier;
                var translated = await translationEngine.GenerateAsync(
                    systemMessage, userMessage, TranslationTemperature, maxTokens, ct);
                await translationCacheAccess.SaveTranslationAsync(
                    book.Id, chapter.HRef, hash, CleanTranslationOutput(translated));
            }

            ReportChapterProgress(run.Progress, chapterIdx, totalChapters, paraIdx + 1, textBlocks.Count);
        }
    }

    private async Task TranslateChaptersWithCacheAsync(
        TranslationRun run,
        IReadOnlyList<Chapter> chapters,
        BookTranslationJob job,
        int startChapterIndex,
        CancellationToken ct)
    {
        for (var chapterIdx = startChapterIndex; chapterIdx < chapters.Count; chapterIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var chapter = chapters[chapterIdx];
            await TranslateSingleChapterAsync(run, chapter, chapterIdx, chapters.Count, ct);

            job.LastCompletedChapterIndex = chapterIdx;
            await bookTranslationJobAccess.UpdateJobProgressAsync(job.Id, chapterIdx, "InProgress");
        }
    }

    private static void ReportChapterProgress(
        IProgress<BookTranslationProgress>? progress,
        int chapterIdx, int totalChapters,
        int currentParagraph, int totalParagraphs)
    {
        var overallProgress = ((double)chapterIdx / totalChapters) +
            ((double)currentParagraph / totalParagraphs / totalChapters);
        progress?.Report(new BookTranslationProgress(
            chapterIdx + 1, totalChapters, currentParagraph, totalParagraphs, overallProgress));
    }

    /// Chapters keyed by href plus the share of the body text that ended up inside a block. The
    /// rebuild pass already walks every chapter, so the coverage signal costs no extra I/O.
    private sealed record RebuiltBook(Dictionary<string, string> Chapters, double CoveredTextRatio);

    private async Task<RebuiltBook> RebuildAllTranslatedChaptersAsync(
        Book book,
        IReadOnlyList<Chapter> chapters,
        string sourceLanguage,
        string targetLanguage)
    {
        var translatedChapters = new Dictionary<string, string>(chapters.Count);
        var coveredChars = 0L;
        var totalChars = 0L;

        foreach (var href in chapters.Select(chapter => chapter.HRef))
        {
            var html = await parsingEngine.ExtractChapterContentAsync(
                book.FilePath, href, string.Empty, ChapterContentPurpose.Export);
            var bodyContent = HtmlUtility.ExtractBodyContent(html);
            var textBlocks = HtmlUtility.ExtractTextBlocks(bodyContent);
            var translations = await FetchTranslationsFromCacheAsync(
                book.Id, href, textBlocks, sourceLanguage, targetLanguage);

            coveredChars += CountBlockChars(textBlocks);
            totalChars += HtmlUtility.CountTextChars(bodyContent);
            translatedChapters[href] = HtmlUtility.ReplaceTextBlocksInHtml(html, translations);
        }

        return new RebuiltBook(translatedChapters, CoveredRatio(coveredChars, totalChars));
    }

    private static long CountBlockChars(List<string> textBlocks)
    {
        var covered = 0L;
        foreach (var block in textBlocks)
            covered += HtmlUtility.CountTextChars(block);
        return covered;
    }

    // Clamped because malformed chapter HTML (a raw '<' inside a block) can strip down to more
    // characters than the whole body does, and a coverage signal above 100% would just be a lie.
    private static double CoveredRatio(long coveredChars, long totalChars) =>
        totalChars == 0 ? 1.0 : Math.Min(1.0, (double)coveredChars / totalChars);

    private async Task<List<string>> FetchTranslationsFromCacheAsync(
        int bookId, string chapterHRef, List<string> textBlocks,
        string sourceLanguage, string targetLanguage)
    {
        var translations = new List<string>(textBlocks.Count);
        foreach (var original in textBlocks)
        {
            var hash = ComputeHash(original, sourceLanguage, targetLanguage);
            var cached = await translationCacheAccess.FetchTranslationAsync(bookId, chapterHRef, hash);
            translations.Add(cached ?? original);
        }
        return translations;
    }

    public async IAsyncEnumerable<TranslatedParagraph> TranslateChapterAsync(
        int bookId,
        string chapterHRef,
        string sourceLanguage,
        string targetLanguage,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var book = await booksAccess.FetchBookAsync(bookId);
        var html = await parsingEngine.ExtractChapterContentAsync(
            book.FilePath, chapterHRef, string.Empty, ChapterContentPurpose.Export);
        var bodyContent = HtmlUtility.ExtractBodyContent(html);
        var paragraphs = HtmlUtility.ExtractTextBlocks(bodyContent);

        string? previousParagraph = null;
        var chapter = (await parsingEngine.ExtractChaptersAsync(book.FilePath))
            .FirstOrDefault(c => c.HRef == chapterHRef);

        for (var i = 0; i < paragraphs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var original = paragraphs[i];
            var hash = ComputeHash(original, sourceLanguage, targetLanguage);

            var cached = await translationCacheAccess.FetchTranslationAsync(bookId, chapterHRef, hash);
            if (cached is not null)
            {
                yield return new TranslatedParagraph(original, cached, i, paragraphs.Count, (double)(i + 1) / paragraphs.Count);
                previousParagraph = cached;
                continue;
            }

            var (systemMessage, userMessage) = promptUtility.BuildTranslationMessages(
                original,
                sourceLanguage,
                targetLanguage,
                book.Title,
                chapter?.Title,
                previousParagraph);

            var maxTokens = original.Length * MaxTokenMultiplier;
            var translated = await translationEngine.GenerateAsync(systemMessage, userMessage, TranslationTemperature, maxTokens, ct);
            translated = CleanTranslationOutput(translated);

            await translationCacheAccess.SaveTranslationAsync(bookId, chapterHRef, hash, translated);

            yield return new TranslatedParagraph(original, translated, i, paragraphs.Count, (double)(i + 1) / paragraphs.Count);
            previousParagraph = translated;
        }
    }

    public async IAsyncEnumerable<TranslatedParagraph> TranslateParagraphsAsync(
        int bookId,
        string chapterHRef,
        string sourceLanguage,
        string targetLanguage,
        IReadOnlyList<VisibleParagraph> paragraphs,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var book = await booksAccess.FetchBookAsync(bookId);
        var chapter = (await parsingEngine.ExtractChaptersAsync(book.FilePath))
            .FirstOrDefault(c => c.HRef == chapterHRef);

        string? previousTranslation = null;

        for (var i = 0; i < paragraphs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var original = paragraphs[i].Text;
            var hash = ComputeHash(original, sourceLanguage, targetLanguage);

            var cached = await translationCacheAccess.FetchTranslationAsync(bookId, chapterHRef, hash);
            if (cached is not null)
            {
                yield return new TranslatedParagraph(original, cached, paragraphs[i].Index, paragraphs.Count, (double)(i + 1) / paragraphs.Count);
                previousTranslation = cached;
                continue;
            }

            var (systemMessage, userMessage) = promptUtility.BuildTranslationMessages(
                original, sourceLanguage, targetLanguage, book.Title, chapter?.Title, previousTranslation);

            var maxTokens = original.Length * MaxTokenMultiplier;
            var translated = await translationEngine.GenerateAsync(systemMessage, userMessage, TranslationTemperature, maxTokens, ct);
            translated = CleanTranslationOutput(translated);

            await translationCacheAccess.SaveTranslationAsync(bookId, chapterHRef, hash, translated);

            yield return new TranslatedParagraph(original, translated, paragraphs[i].Index, paragraphs.Count, (double)(i + 1) / paragraphs.Count);
            previousTranslation = translated;
        }
    }

    public Task DeleteModelAsync() =>
        modelAccess.DeleteModelAsync();

    private static string ComputeHash(string text, string sourceLanguage, string targetLanguage)
    {
        var input = $"{sourceLanguage}|{targetLanguage}|{text}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16];
    }

    private static string CleanTranslationOutput(string output)
    {
        var cleaned = output.Trim();
        cleaned = cleaned.TrimStart('"', '\'', '\u201C').TrimEnd('"', '\'', '\u201D');
        return cleaned.Trim();
    }
}
