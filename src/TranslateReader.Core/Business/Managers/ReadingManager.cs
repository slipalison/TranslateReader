using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;

namespace TranslateReader.Business.Managers;

public class ReadingManager(
    IBooksAccess booksAccess,
    IReadingStateAccess readingStateAccess,
    IParsingEngine parsingEngine,
    IFileUtility fileUtility,
    string booksDirectory) : IReadingManager
{
    public Task<Book> OpenBookAsync(int bookId) =>
        booksAccess.FetchBookAsync(bookId);

    public async Task<IReadOnlyList<Chapter>> LoadChaptersAsync(int bookId)
    {
        var book = await booksAccess.FetchBookAsync(bookId);
        return await parsingEngine.ExtractChaptersAsync(book.FilePath);
    }

    public async Task<ChapterHtmlResult> LoadChapterContentAsync(int bookId, string chapterHRef)
    {
        var book = await booksAccess.FetchBookAsync(bookId);
        var imagesDir = Path.Combine(booksDirectory, "images", bookId.ToString());
        await ExtractImagesIfNeededAsync(book.FilePath, imagesDir);
        var html = await parsingEngine.ExtractChapterContentAsync(
            book.FilePath, chapterHRef, imagesDir, ChapterContentPurpose.Display);
        return new ChapterHtmlResult(html, imagesDir);
    }

    public Task SaveProgressAsync(int bookId, string chapterHRef, double scrollPosition, double progressPercentage)
    {
        var progress = new ReadingProgress
        {
            BookId = bookId,
            ChapterHRef = chapterHRef,
            ScrollPosition = scrollPosition,
            ProgressPercentage = progressPercentage,
            UpdatedAt = DateTime.UtcNow
        };
        return readingStateAccess.SaveProgressAsync(progress);
    }

    public Task<ReadingProgress?> LoadProgressAsync(int bookId) =>
        readingStateAccess.FetchProgressAsync(bookId);

    private async Task ExtractImagesIfNeededAsync(string epubPath, string imagesDir)
    {
        // A non-empty images directory means this book was already extracted.
        if (fileUtility.DirectoryHasContent(imagesDir))
            return;

        await foreach (var (relativePath, content) in parsingEngine.ExtractAllImagesAsync(epubPath))
        {
            var outputPath = Path.Combine(imagesDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            await fileUtility.WriteFileAsync(outputPath, content);
        }
    }
}
