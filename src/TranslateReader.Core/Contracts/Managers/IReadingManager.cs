using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface IReadingManager
{
    Task<Book> OpenBookAsync(int bookId);
    Task<IReadOnlyList<Chapter>> LoadChaptersAsync(int bookId);
    Task<ChapterHtmlResult> LoadChapterContentAsync(int bookId, string chapterHRef);
    Task SaveProgressAsync(int bookId, string chapterHRef, double scrollPosition, double progressPercentage);
    Task<ReadingProgress?> LoadProgressAsync(int bookId);
}
