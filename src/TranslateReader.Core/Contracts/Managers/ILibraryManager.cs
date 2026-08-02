using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ILibraryManager
{
    Task<IReadOnlyList<Book>> ListBooksAsync();
    /// <summary>
    /// Projects every book (or, when <paramref name="query"/> is non-empty, only the ones whose
    /// title or author contain it, case-insensitive) into a <see cref="BookSummary"/> carrying
    /// reading progress and chapter count, so callers never join Book/ReadingProgress themselves.
    /// </summary>
    Task<IReadOnlyList<BookSummary>> ListBookSummariesAsync(string query = "");
    /// <summary>
    /// Returns the books with reading progress, most recently read first. A book with no
    /// <c>ReadingProgress</c> row was never opened and is excluded, so the list never claims a
    /// book was "recently read" when it was only imported.
    /// </summary>
    Task<IReadOnlyList<BookSummary>> ListRecentBookSummariesAsync();
    Task<Book> ImportBookAsync(string filePath);
    Task DeleteBookAsync(int bookId);
    Task<IReadOnlyList<Book>> SearchBooksAsync(string query);
}
