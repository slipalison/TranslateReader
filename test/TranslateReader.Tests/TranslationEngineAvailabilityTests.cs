using NSubstitute;
using TranslateReader.Business.Engines;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;
using TranslateReader.Utilities;

namespace TranslateReader.Tests;

/// <summary>
/// Covers the platform/memory gate in <see cref="TranslationManager.InitializeEngineIfNeededAsync"/>
/// (D-2026-08-16-llm-mobile-8): a platform with no native backend, or a device without enough
/// available memory for the selected model, must refuse gracefully with
/// <see cref="TranslationUnavailableException"/> -- never crash, never leak the failure past the
/// PageModel boundary unhandled -- while a device that has both must keep initializing exactly as
/// it does today.
/// </summary>
public class TranslationEngineAvailabilityTests
{
    private readonly ITranslationEngine _translationEngine = Substitute.For<ITranslationEngine>();
    private readonly IModelAccess _modelAccess = Substitute.For<IModelAccess>();
    private readonly ITranslationCacheAccess _cacheAccess = Substitute.For<ITranslationCacheAccess>();
    private readonly IBookTranslationJobAccess _jobAccess = Substitute.For<IBookTranslationJobAccess>();
    private readonly IPromptUtility _promptUtility = Substitute.For<IPromptUtility>();
    private readonly IBooksAccess _booksAccess = Substitute.For<IBooksAccess>();
    private readonly IParsingEngine _parsingEngine = Substitute.For<IParsingEngine>();
    private readonly ISettingsAccess _settingsAccess = Substitute.For<ISettingsAccess>();
    private readonly ISnippetTranslationAccess _snippetTranslationAccess = Substitute.For<ISnippetTranslationAccess>();
    private readonly IDeviceMemoryUtility _deviceMemoryUtility = Substitute.For<IDeviceMemoryUtility>();

    public TranslationEngineAvailabilityTests()
    {
        _translationEngine.IsReady.Returns(false);
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "gemma-2-2b" });
        _modelAccess.GetModelPath(Arg.Any<string>()).Returns("/models/model.gguf");
    }

    private TranslationManager CreateSut(bool isTranslationBackendSupported) => new(
        _translationEngine,
        _modelAccess,
        _cacheAccess,
        _jobAccess,
        _promptUtility,
        _booksAccess,
        _parsingEngine,
        _settingsAccess,
        _snippetTranslationAccess,
        _deviceMemoryUtility,
        isTranslationBackendSupported);

    [Fact]
    public async Task InitializeEngineIfNeededAsync_WhenTheBackendIsUnsupportedOnThisPlatform_ThrowsTranslationUnavailable()
    {
        var sut = CreateSut(isTranslationBackendSupported: false);

        await Assert.ThrowsAsync<TranslationUnavailableException>(
            () => sut.InitializeEngineIfNeededAsync(CancellationToken.None));

        await _translationEngine.DidNotReceive().InitializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeEngineIfNeededAsync_WhenDeviceMemoryIsBelowTheModelRequirement_ThrowsTranslationUnavailable()
    {
        // gemma-2-2b is 1_629_413_888 bytes -> RequiredMemoryBytes (1.5x) = 2_444_120_832.
        // One byte short of that must refuse, never silently proceed into a likely OOM.
        _deviceMemoryUtility.GetAvailableMemoryBytes().Returns(2_444_120_831L);
        var sut = CreateSut(isTranslationBackendSupported: true);

        await Assert.ThrowsAsync<TranslationUnavailableException>(
            () => sut.InitializeEngineIfNeededAsync(CancellationToken.None));

        await _translationEngine.DidNotReceive().InitializeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeEngineIfNeededAsync_WhenDeviceMemoryIsSufficient_InitializesTheEngine()
    {
        _deviceMemoryUtility.GetAvailableMemoryBytes().Returns(long.MaxValue);
        var sut = CreateSut(isTranslationBackendSupported: true);

        await sut.InitializeEngineIfNeededAsync(CancellationToken.None);

        await _translationEngine.Received(1).InitializeAsync("/models/model.gguf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TranslationUnavailableException_DefaultConstructor_HasAGenericMessage()
    {
        var exception = new TranslationUnavailableException();

        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }

    [Fact]
    public void TranslationUnavailableException_WithInnerException_PreservesBoth()
    {
        var inner = new InvalidOperationException("native load failed");

        var exception = new TranslationUnavailableException("translation unavailable", inner);

        Assert.Equal("translation unavailable", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    // The null-object registered for platforms with no native backend (iOS/MacCatalyst,
    // D-2026-08-16-llm-mobile-5). Its whole contract is "never touch LLamaSharp, always refuse" --
    // tested directly here since MauiProgram's #if-gated DI wiring never runs on this Windows suite.
    [Fact]
    public void UnavailableTranslationEngine_IsNeverReady()
    {
        using var engine = new UnavailableTranslationEngine();

        Assert.False(engine.IsReady);
    }

    [Fact]
    public async Task UnavailableTranslationEngine_InitializeAsync_ThrowsTranslationUnavailable()
    {
        using var engine = new UnavailableTranslationEngine();

        await Assert.ThrowsAsync<TranslationUnavailableException>(
            () => engine.InitializeAsync("/models/model.gguf", CancellationToken.None));
    }

    [Fact]
    public async Task UnavailableTranslationEngine_GenerateAsync_ThrowsTranslationUnavailable()
    {
        using var engine = new UnavailableTranslationEngine();

        await Assert.ThrowsAsync<TranslationUnavailableException>(
            () => engine.GenerateAsync("system", "user", 0.1f, 100, CancellationToken.None));
    }

    [Fact]
    public void UnavailableTranslationEngine_GenerateStreamingAsync_ThrowsTranslationUnavailable()
    {
        using var engine = new UnavailableTranslationEngine();

        Assert.Throws<TranslationUnavailableException>(
            () => engine.GenerateStreamingAsync("system", "user", 0.1f, 100, CancellationToken.None));
    }

    [Fact]
    public void UnavailableTranslationEngine_Dispose_DoesNotThrow()
    {
        var engine = new UnavailableTranslationEngine();

        var exception = Record.Exception(engine.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public void DeviceMemoryUtility_GetAvailableMemoryBytes_ReturnsAPositiveNumber()
    {
        var utility = new DeviceMemoryUtility();

        var availableBytes = utility.GetAvailableMemoryBytes();

        Assert.True(availableBytes > 0);
    }
}
