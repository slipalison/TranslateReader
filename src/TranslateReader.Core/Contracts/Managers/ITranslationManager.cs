using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ITranslationManager
{
    Task DownloadModelIfNeededAsync(IProgress<double>? progress, CancellationToken ct);
    Task InitializeEngineIfNeededAsync(CancellationToken ct);
    IAsyncEnumerable<TranslatedParagraph> TranslateChapterAsync(int bookId, string chapterHRef, string sourceLanguage, string targetLanguage, CancellationToken ct);
    IAsyncEnumerable<TranslatedParagraph> TranslateParagraphsAsync(int bookId, string chapterHRef, string sourceLanguage, string targetLanguage, IReadOnlyList<VisibleParagraph> paragraphs, CancellationToken ct);
    Task DeleteModelAsync();
    /// <summary>
    /// Translates every chapter of the book and writes a new EPUB into
    /// <paramref name="destinationDirectory"/>. The result also carries the share of the source
    /// text the block selection reached, so a book whose markup hides text from the extractor is
    /// never reported as a silent success. A low ratio is not an error and never throws.
    /// </summary>
    Task<BookTranslationResult> TranslateBookAsync(
        int bookId,
        string sourceLanguage,
        string targetLanguage,
        string destinationDirectory,
        IProgress<BookTranslationProgress>? progress,
        CancellationToken ct);
    Task<BookTranslationJob?> GetActiveTranslationJobAsync(int bookId);
    Task PauseTranslationAsync(int bookId);
}
