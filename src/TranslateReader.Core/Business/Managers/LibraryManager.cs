using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;

namespace TranslateReader.Business.Managers;

public class LibraryManager(
    IBooksAccess booksAccess,
    IReadingStateAccess readingStateAccess,
    IParsingEngine parsingEngine,
    IFileUtility fileUtility,
    string booksDirectory) : ILibraryManager
{

    public Task<IReadOnlyList<Book>> ListBooksAsync() =>
        booksAccess.FetchAllBooksAsync();

    public async Task<IReadOnlyList<BookSummary>> ListBookSummariesAsync()
    {
        var books = await booksAccess.FetchAllBooksAsync();
        var summaries = new List<BookSummary>(books.Count);
        foreach (var book in books)
        {
            var progress = await readingStateAccess.FetchProgressAsync(book.Id);
            summaries.Add(new BookSummary
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                CoverImagePath = book.CoverImagePath,
                ProgressPercentage = progress?.ProgressPercentage ?? 0
            });
        }
        return summaries;
    }

    public async Task<Book> ImportBookAsync(string filePath)
    {
        var localPath = await fileUtility.CopyFileAsync(filePath, booksDirectory);
        var book = await parsingEngine.ExtractMetadataAsync(localPath);
        book.FilePath = localPath;
        book.CoverImagePath = await SaveCoverImageAsync(localPath);
        var bookId = await booksAccess.SaveBookAsync(book);
        book.Id = bookId;
        var chapters = await parsingEngine.ExtractChaptersAsync(localPath);
        var chaptersWithBookId = chapters.Select(c => new Chapter
        {
            Title = c.Title,
            OrderIndex = c.OrderIndex,
            HRef = c.HRef,
            BookId = bookId
        }).ToList();
        await booksAccess.SaveChaptersAsync(chaptersWithBookId);
        return book;
    }

    public async Task DeleteBookAsync(int bookId)
    {
        var book = await booksAccess.FetchBookAsync(bookId);
        await readingStateAccess.RemoveStateForBookAsync(bookId);
        await booksAccess.RemoveBookAsync(bookId);
        await fileUtility.DeleteFileAsync(book.FilePath);
        if (!string.IsNullOrEmpty(book.CoverImagePath))
            await fileUtility.DeleteFileAsync(book.CoverImagePath);
        await fileUtility.DeleteDirectoryAsync(Path.Combine(booksDirectory, "images", bookId.ToString()));
    }

    public async Task<IReadOnlyList<Book>> SearchBooksAsync(string query)
    {
        var all = await booksAccess.FetchAllBooksAsync();
        return all
            .Where(b => b.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || b.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<string> SaveCoverImageAsync(string epubPath)
    {
        var coverBytes = await parsingEngine.ExtractCoverImageAsync(epubPath);
        if (coverBytes is null)
            return string.Empty;
        var coverFileName = Path.GetFileNameWithoutExtension(epubPath) + "_cover.jpg";
        var coverPath = Path.Combine(booksDirectory, "covers", coverFileName);
        await fileUtility.WriteFileAsync(coverPath, coverBytes);
        return coverPath;
    }
}
