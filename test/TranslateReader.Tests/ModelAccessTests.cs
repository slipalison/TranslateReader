using TranslateReader.Access;

namespace TranslateReader.Tests;

public class ModelAccessTests : IDisposable
{
    private readonly string _modelsDir;
    private readonly HttpClient _httpClient = new();

    public ModelAccessTests()
    {
        _modelsDir = Path.Combine(Path.GetTempPath(), "TranslateReaderTests_" + Guid.NewGuid().ToString("N"));
    }

    private ModelAccess CreateSut() => new(_httpClient, _modelsDir);

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(_modelsDir))
            Directory.Delete(_modelsDir, recursive: true);
    }

    [Fact]
    public void IsModelAvailable_ReturnsFalseWhenDirectoryDoesNotExist()
    {
        Assert.False(CreateSut().IsModelAvailable());
    }

    [Fact]
    public void IsModelAvailable_ReturnsFalseWhenNoGgufFiles()
    {
        Directory.CreateDirectory(_modelsDir);
        File.WriteAllText(Path.Combine(_modelsDir, "readme.txt"), "not a model");

        Assert.False(CreateSut().IsModelAvailable());
    }

    [Fact]
    public void IsModelAvailable_ReturnsTrueWhenGgufExists()
    {
        Directory.CreateDirectory(_modelsDir);
        File.WriteAllText(Path.Combine(_modelsDir, "model.gguf"), "fake model data");

        Assert.True(CreateSut().IsModelAvailable());
    }

    [Fact]
    public void GetModelPath_ThrowsWhenDirectoryDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => CreateSut().GetModelPath());
    }

    [Fact]
    public void GetModelPath_ThrowsWhenNoGgufFiles()
    {
        Directory.CreateDirectory(_modelsDir);
        Assert.Throws<FileNotFoundException>(() => CreateSut().GetModelPath());
    }

    [Fact]
    public void GetModelPath_ReturnsPathToGgufFile()
    {
        Directory.CreateDirectory(_modelsDir);
        var expectedPath = Path.Combine(_modelsDir, "test-model.gguf");
        File.WriteAllText(expectedPath, "fake model data");

        var result = CreateSut().GetModelPath();

        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public async Task DeleteModelAsync_RemovesAllFiles()
    {
        Directory.CreateDirectory(_modelsDir);
        File.WriteAllText(Path.Combine(_modelsDir, "model.gguf"), "data");
        File.WriteAllText(Path.Combine(_modelsDir, "model.gguf.tmp"), "partial");

        await CreateSut().DeleteModelAsync();

        Assert.Empty(Directory.EnumerateFiles(_modelsDir));
    }

    [Fact]
    public async Task DeleteModelAsync_DoesNothingWhenDirectoryMissing()
    {
        var exception = await Record.ExceptionAsync(() => CreateSut().DeleteModelAsync());
        Assert.Null(exception);
    }
}
