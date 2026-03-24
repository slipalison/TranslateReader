using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public class FileUtilityTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly FileUtility _sut = new();

    public FileUtilityTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task CopyFileAsync_CopiesFileToDestination()
    {
        var source = Path.Combine(_tempDir, "source.epub");
        await File.WriteAllTextAsync(source, "conteudo");
        var destDir = Path.Combine(_tempDir, "dest");

        var result = await _sut.CopyFileAsync(source, destDir);

        Assert.True(File.Exists(result));
        Assert.Equal("conteudo", await File.ReadAllTextAsync(result));
    }

    [Fact]
    public async Task CopyFileAsync_CreatesDestinationDirectoryIfMissing()
    {
        var source = Path.Combine(_tempDir, "source.epub");
        await File.WriteAllTextAsync(source, "x");
        var destDir = Path.Combine(_tempDir, "novo_dir");

        await _sut.CopyFileAsync(source, destDir);

        Assert.True(Directory.Exists(destDir));
    }

    [Fact]
    public async Task DeleteFileAsync_RemovesExistingFile()
    {
        var file = Path.Combine(_tempDir, "para_deletar.txt");
        await File.WriteAllTextAsync(file, "x");

        await _sut.DeleteFileAsync(file);

        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task DeleteFileAsync_DoesNotThrowForNonExistentFile()
    {
        var exception = await Record.ExceptionAsync(() =>
            _sut.DeleteFileAsync(Path.Combine(_tempDir, "nao_existe.txt")));

        Assert.Null(exception);
    }

    [Fact]
    public void FileExists_ReturnsTrueForExistingFile()
    {
        var file = Path.Combine(_tempDir, "existe.txt");
        File.WriteAllText(file, "x");

        Assert.True(_sut.FileExists(file));
    }

    [Fact]
    public void FileExists_ReturnsFalseForMissingFile()
    {
        Assert.False(_sut.FileExists(Path.Combine(_tempDir, "nao_existe.txt")));
    }

    [Fact]
    public void GetFileExtension_ReturnsLowercaseExtension()
    {
        Assert.Equal(".epub", _sut.GetFileExtension("book.EPUB"));
        Assert.Equal(".pdf", _sut.GetFileExtension("doc.PDF"));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_RemovesExistingDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "test.txt"), "content");

        var sut = new FileUtility();
        await sut.DeleteDirectoryAsync(dir);

        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_DoesNotThrowForNonExistentDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var sut = new FileUtility();
        await sut.DeleteDirectoryAsync(dir);
    }
}
