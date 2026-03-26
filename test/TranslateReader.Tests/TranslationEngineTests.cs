using TranslateReader.Business.Engines;

namespace TranslateReader.Tests;

public class TranslationEngineTests
{
    [Fact]
    public void IsReady_ReturnsFalse_BeforeInitialization()
    {
        using var sut = new TranslationEngine();

        Assert.False(sut.IsReady);
    }

    [Fact]
    public void Dispose_SetsIsReadyToFalse()
    {
        var sut = new TranslationEngine();

        sut.Dispose();

        Assert.False(sut.IsReady);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var sut = new TranslationEngine();

        sut.Dispose();
        sut.Dispose();

        Assert.False(sut.IsReady);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsObjectDisposedException_AfterDispose()
    {
        var sut = new TranslationEngine();
        sut.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            sut.InitializeAsync("fake.gguf", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsInvalidOperation_BeforeInitialization()
    {
        using var sut = new TranslationEngine();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateAsync("system", "test", 0.1f, 50, CancellationToken.None));
    }

    [Trait("Category", "Integration")]
    [Fact(Skip = "Requires GGUF model file for local development")]
    public async Task InitializeAsync_LoadsModel_WithValidPath()
    {
        using var sut = new TranslationEngine();
        var modelPath = Environment.GetEnvironmentVariable("LLAMASHARP_TEST_MODEL")
            ?? "path/to/test-model.gguf";

        await sut.InitializeAsync(modelPath, CancellationToken.None);

        Assert.True(sut.IsReady);
    }

    [Trait("Category", "Integration")]
    [Fact(Skip = "Requires GGUF model file for local development")]
    public async Task GenerateAsync_ProducesOutput_WithValidModel()
    {
        using var sut = new TranslationEngine();
        var modelPath = Environment.GetEnvironmentVariable("LLAMASHARP_TEST_MODEL")
            ?? "path/to/test-model.gguf";

        await sut.InitializeAsync(modelPath, CancellationToken.None);
        var result = await sut.GenerateAsync("Translate to Portuguese", "Hello", 0.1f, 50, CancellationToken.None);

        Assert.NotEmpty(result);
    }
}
