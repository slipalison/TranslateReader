using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ITranslationManager
{
    Task DownloadModelIfNeededAsync(IProgress<double>? progress, CancellationToken ct);
    Task InitializeEngineIfNeededAsync(CancellationToken ct);
    IAsyncEnumerable<TranslatedParagraph> TranslateChapterAsync(int bookId, string chapterHRef, CancellationToken ct);
    IAsyncEnumerable<TranslatedParagraph> TranslateParagraphsAsync(int bookId, string chapterHRef, IReadOnlyList<VisibleParagraph> paragraphs, CancellationToken ct);
    Task DeleteModelAsync();
}
