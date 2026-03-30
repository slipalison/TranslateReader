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

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
