using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ITranslationManager
{
    Task DownloadModelIfNeededAsync(IProgress<double>? progress, CancellationToken ct);
    Task InitializeEngineIfNeededAsync(CancellationToken ct);
    IAsyncEnumerable<TranslatedParagraph> TranslateChapterAsync(int bookId, string chapterHRef, string sourceLanguage, string targetLanguage, CancellationToken ct);
    IAsyncEnumerable<TranslatedParagraph> TranslateParagraphsAsync(int bookId, string chapterHRef, string sourceLanguage, string targetLanguage, IReadOnlyList<VisibleParagraph> paragraphs, CancellationToken ct);
    Task DeleteModelAsync();
}
