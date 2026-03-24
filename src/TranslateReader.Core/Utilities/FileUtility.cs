using TranslateReader.Contracts.Utilities;

namespace TranslateReader.Utilities;

public class FileUtility : IFileUtility
{
    public async Task<string> CopyFileAsync(string sourcePath, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        var fileName = Path.GetFileName(sourcePath);
        var destinationPath = Path.Combine(destinationDirectory, fileName);
        using var source = File.OpenRead(sourcePath);
        using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);
        return destinationPath;
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public bool FileExists(string filePath) => File.Exists(filePath);

    public string GetFileExtension(string filePath) => Path.GetExtension(filePath).ToLowerInvariant();

    public async Task WriteFileAsync(string filePath, byte[] content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, content);
    }

    public Task DeleteDirectoryAsync(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
            Directory.Delete(directoryPath, recursive: true);
        return Task.CompletedTask;
    }
}
