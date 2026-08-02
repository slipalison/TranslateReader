using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ILibraryManager
{
    Task<IReadOnlyList<Book>> ListBooksAsync();
    Task<IReadOnlyList<BookSummary>> ListBookSummariesAsync(string query = "");
    Task<IReadOnlyList<BookSummary>> ListRecentBookSummariesAsync();
    Task<Book> ImportBookAsync(string filePath);
    Task DeleteBookAsync(int bookId);
    Task<IReadOnlyList<Book>> SearchBooksAsync(string query);
}
