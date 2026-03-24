using TranslateReader.Models;

namespace TranslateReader.Contracts.Engines;

public interface IParsingEngine
{
    Task<Book> ExtractMetadataAsync(string filePath);
    Task<IReadOnlyList<Chapter>> ExtractChaptersAsync(string filePath);
    Task<string> ExtractChapterContentAsync(string filePath, string chapterHRef, string imagesDirectory);
    Task<IReadOnlyDictionary<string, byte[]>> ExtractAllImagesAsync(string filePath);
    Task<byte[]?> ExtractCoverImageAsync(string filePath);
}
