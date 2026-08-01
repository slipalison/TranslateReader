using TranslateReader.Models;

namespace TranslateReader.Contracts.Engines;

public interface IParsingEngine
{
    Task<Book> ExtractMetadataAsync(string filePath);
    Task<IReadOnlyList<Chapter>> ExtractChaptersAsync(string filePath);
    Task<string> ExtractChapterContentAsync(string filePath, string chapterHRef, string imagesDirectory);
    IAsyncEnumerable<ExtractedImage> ExtractAllImagesAsync(string filePath);
    Task<byte[]?> ExtractCoverImageAsync(string filePath);
    Task<string> CreateTranslatedEpubAsync(
        string originalFilePath,
        string translatedTitle,
        IReadOnlyDictionary<string, string> translatedChapterHtml,
        string destinationDirectory);
}
