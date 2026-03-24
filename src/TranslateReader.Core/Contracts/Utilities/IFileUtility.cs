namespace TranslateReader.Contracts.Utilities;

public interface IFileUtility
{
    Task<string> CopyFileAsync(string sourcePath, string destinationDirectory);
    Task DeleteFileAsync(string filePath);
    bool FileExists(string filePath);
    string GetFileExtension(string filePath);
    Task WriteFileAsync(string filePath, byte[] content);
    Task DeleteDirectoryAsync(string directoryPath);
}
