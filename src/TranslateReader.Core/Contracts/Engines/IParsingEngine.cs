using TranslateReader.Models;

namespace TranslateReader.Contracts.Engines;

public interface IParsingEngine
{
    Task<Book> ExtractMetadataAsync(string filePath);
    Task<IReadOnlyList<Chapter>> ExtractChaptersAsync(string filePath);
    Task<string> ExtractChapterContentAsync(string filePath, string chapterHRef, string imagesDirectory);
    /// <summary>
    /// Streams every local image of the book one at a time, without materializing the whole book
    /// in memory. The underlying archive handle is released when the enumeration ends or when the
    /// consumer stops early.
    /// </summary>
    IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync(string filePath);
    Task<byte[]?> ExtractCoverImageAsync(string filePath);
    Task<string> CreateTranslatedEpubAsync(
        string originalFilePath,
        string translatedTitle,
        IReadOnlyDictionary<string, string> translatedChapterHtml,
        string destinationDirectory);
}
