using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ILibraryManager
{
    Task<IReadOnlyList<Book>> ListBooksAsync();
    Task<IReadOnlyList<BookSummary>> ListBookSummariesAsync();
    Task<Book> ImportBookAsync(string filePath);
    Task DeleteBookAsync(int bookId);
    Task<IReadOnlyList<Book>> SearchBooksAsync(string query);
}
