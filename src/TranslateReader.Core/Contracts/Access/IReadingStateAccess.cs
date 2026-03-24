using TranslateReader.Models;

namespace TranslateReader.Contracts.Access;

public interface IReadingStateAccess
{
    Task<ReadingProgress?> FetchProgressAsync(int bookId);
    Task SaveProgressAsync(ReadingProgress progress);
    Task<IReadOnlyList<Bookmark>> FetchBookmarksAsync(int bookId);
    Task SaveBookmarkAsync(Bookmark bookmark);
    Task RemoveBookmarkAsync(int bookmarkId);
    Task RemoveStateForBookAsync(int bookId);
}
