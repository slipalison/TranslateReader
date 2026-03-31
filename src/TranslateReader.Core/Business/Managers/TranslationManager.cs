using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;
using TranslateReader.Utilities;

namespace TranslateReader.Business.Managers;

public partial class TranslationManager(
    ITranslationEngine translationEngine,
    IModelAccess modelAccess,
    ITranslationCacheAccess translationCacheAccess,
    IBookTranslationJobAccess bookTranslationJobAccess,
    IPromptUtility promptUtility,
    IBooksAccess booksAccess,
    IParsingEngine parsingEngine) : ITranslationManager
{
    private static readonly ModelInfo DefaultModel = new(
        Name: "gemma-2-2b",
        FileName: "gemma-2-2b-it-Q4_K_M.gguf",
        DownloadUrl: "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
        SizeBytes: 1_629_413_888);

    private const float TranslationTemperature = 0.1f;
    private const int MaxTokenMultiplier = 3;

    public async Task DownloadModelIfNeededAsync(IProgress<double>? progress, CancellationToken ct)
    {
        if (!modelAccess.IsModelAvailable())
            await modelAccess.DownloadModelAsync(DefaultModel.DownloadUrl, progress, ct);
    }

    public async Task InitializeEngineIfNeededAsync(CancellationToken ct)
    {
        if (!translationEngine.IsReady)
            await translationEngine.InitializeAsync(modelAccess.GetModelPath(), ct);
    }

    public async Task<string> TranslateBookAsync(
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

        try
        {
            await TranslateChaptersWithCacheAsync(
                book, chapters, job, startChapterIndex, sourceLanguage, targetLanguage, progress, ct);
        }
        catch (OperationCanceledException)
        {
            await bookTranslationJobAccess.UpdateJobProgressAsync(job.Id, job.LastCompletedChapterIndex, "Paused");
            throw;
        }

        var translatedChapters = await RebuildAllTranslatedChaptersAsync(
            book, chapters, sourceLanguage, targetLanguage);

        await bookTranslationJobAccess.DeleteJobAsync(job.Id);

        var translatedTitle = $"{book.Title} [{sourceLanguage} \u2192 {targetLanguage}]";
        return await parsingEngine.CreateTranslatedEpubAsync(
            book.FilePath, translatedTitle, translatedChapters, destinationDirectory);
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

    private async Task TranslateChaptersWithCacheAsync(
        Book book,
        IReadOnlyList<Chapter> chapters,
        BookTranslationJob job,
        int startChapterIndex,
        string sourceLanguage,
        string targetLanguage,
        IProgress<BookTranslationProgress>? progress,
        CancellationToken ct)
    {
        for (var chapterIdx = startChapterIndex; chapterIdx < chapters.Count; chapterIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var chapter = chapters[chapterIdx];
            await TranslateSingleChapterAsync(
                book, chapter, chapterIdx, chapters.Count,
                sourceLanguage, targetLanguage, progress, ct);

            job.LastCompletedChapterIndex = chapterIdx;
            await bookTranslationJobAccess.UpdateJobProgressAsync(job.Id, chapterIdx, "InProgress");
        }
    }

    private async Task TranslateSingleChapterAsync(
        Book book,
        Chapter chapter,
        int chapterIdx,
        int totalChapters,
        string sourceLanguage,
        string targetLanguage,
        IProgress<BookTranslationProgress>? progress,
        CancellationToken ct)
    {
        var html = await parsingEngine.ExtractChapterContentAsync(book.FilePath, chapter.HRef, string.Empty);
        var textBlocks = ExtractTextBlocks(HtmlUtility.ExtractBodyContent(html));

        for (var paraIdx = 0; paraIdx < textBlocks.Count; paraIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var original = textBlocks[paraIdx];
            var hash = ComputeHash(original, sourceLanguage, targetLanguage);

            var cached = await translationCacheAccess.FetchTranslationAsync(book.Id, chapter.HRef, hash);
            if (cached is null)
            {
                var (systemMessage, userMessage) = promptUtility.BuildTranslationMessages(
                    original, sourceLanguage, targetLanguage, book.Title, chapter.Title, null);
                var maxTokens = original.Length * MaxTokenMultiplier;
                var translated = await translationEngine.GenerateAsync(
                    systemMessage, userMessage, TranslationTemperature, maxTokens, ct);
                await translationCacheAccess.SaveTranslationAsync(
                    book.Id, chapter.HRef, hash, CleanTranslationOutput(translated));
            }

            ReportChapterProgress(progress, chapterIdx, totalChapters, paraIdx + 1, textBlocks.Count);
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

    private async Task<Dictionary<string, string>> RebuildAllTranslatedChaptersAsync(
        Book book,
        IReadOnlyList<Chapter> chapters,
        string sourceLanguage,
        string targetLanguage)
    {
        var translatedChapters = new Dictionary<string, string>();
        foreach (var chapter in chapters)
        {
            var html = await parsingEngine.ExtractChapterContentAsync(book.FilePath, chapter.HRef, string.Empty);
            var textBlocks = ExtractTextBlocks(HtmlUtility.ExtractBodyContent(html));
            var translations = await FetchTranslationsFromCacheAsync(
                book.Id, chapter.HRef, textBlocks, sourceLanguage, targetLanguage);
            translatedChapters[chapter.HRef] = ReplaceTextBlocksInHtml(html, translations);
        }
        return translatedChapters;
    }

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
        var html = await parsingEngine.ExtractChapterContentAsync(book.FilePath, chapterHRef, string.Empty);
        var bodyContent = HtmlUtility.ExtractBodyContent(html);
        var paragraphs = ExtractParagraphs(bodyContent);

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

    private static List<string> ExtractParagraphs(string bodyContent)
    {
        var matches = ParagraphRegex().Matches(bodyContent);
        return matches
            .Select(m => StripHtmlTags(m.Groups[1].Value).Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static List<string> ExtractTextBlocks(string bodyContent)
    {
        var matches = TextBlockRegex().Matches(bodyContent);
        return matches
            .Select(m => StripHtmlTags(m.Groups[2].Value).Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    private static string ReplaceTextBlocksInHtml(string html, IReadOnlyList<string> translations)
    {
        var index = 0;
        return TextBlockRegex().Replace(html, match =>
        {
            var innerHtml = match.Groups[2].Value;
            var text = StripHtmlTags(innerHtml).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return match.Value;
            if (index >= translations.Count)
                return match.Value;
            var translated = System.Net.WebUtility.HtmlEncode(translations[index++]);
            var tag = match.Groups[1].Value;
            var openTag = match.Value[..(match.Value.IndexOf('>') + 1)];
            return $"{openTag}{translated}</{tag}>";
        });
    }

    private static string StripHtmlTags(string html) =>
        HtmlTagRegex().Replace(html, string.Empty);

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

    [GeneratedRegex(@"<p\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex(@"<(p|h[1-6]|li)\b[^>]*>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TextBlockRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
