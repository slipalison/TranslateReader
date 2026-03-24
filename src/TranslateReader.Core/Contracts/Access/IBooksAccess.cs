using TranslateReader.Models;

namespace TranslateReader.Contracts.Access;

public interface IBooksAccess
{
    Task<IReadOnlyList<Book>> FetchAllBooksAsync();
    Task<Book> FetchBookAsync(int bookId);
    Task<int> SaveBookAsync(Book book);
    Task SaveChaptersAsync(IEnumerable<Chapter> chapters);
    Task RemoveBookAsync(int bookId);
}
