using NSubstitute;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class SnippetTranslationManagerTests
{
    private const string SnipHashGolden = "9d2a73a5";

    private readonly ITranslationEngine _translationEngine = Substitute.For<ITranslationEngine>();
    private readonly IModelAccess _modelAccess = Substitute.For<IModelAccess>();
    private readonly ITranslationCacheAccess _cacheAccess = Substitute.For<ITranslationCacheAccess>();
    private readonly IBookTranslationJobAccess _jobAccess = Substitute.For<IBookTranslationJobAccess>();
    private readonly IPromptUtility _promptUtility = Substitute.For<IPromptUtility>();
    private readonly IBooksAccess _booksAccess = Substitute.For<IBooksAccess>();
    private readonly IParsingEngine _parsingEngine = Substitute.For<IParsingEngine>();
    private readonly ISettingsAccess _settingsAccess = Substitute.For<ISettingsAccess>();
    private readonly ISnippetTranslationAccess _snippetTranslationAccess = Substitute.For<ISnippetTranslationAccess>();
    private readonly ISnippetTranslationManager _sut;

    public SnippetTranslationManagerTests()
    {
        _booksAccess.FetchBookAsync(1).Returns(new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" });
        _sut = new TranslationManager(
            _translationEngine,
            _modelAccess,
            _cacheAccess,
            _jobAccess,
            _promptUtility,
            _booksAccess,
            _parsingEngine,
            _settingsAccess,
            _snippetTranslationAccess);
    }

    private static SnippetRequest MakeRequest(
        string chapterHRef = "ch1.html", int paragraphIndex = 0, int sentenceStart = 0, int sentenceEnd = 0,
        string text = "Ela disse que sim.", string paragraph = "Ela disse que sim. Ele concordou.") =>
        new(chapterHRef, paragraphIndex, sentenceStart, sentenceEnd, text, paragraph);

    [Fact]
    public async Task TranslateSnippetAsync_OnCacheHit_DoesNotCallTheEngine()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("She said yes.");

        await _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None);

        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateSnippetAsync_OnCacheMiss_CallsTheEngineAndSavesToCache()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync("system", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("She said yes.");

        var result = await _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None);

        Assert.Equal("She said yes.", result.TranslatedText);
        await _cacheAccess.Received(1).SaveTranslationAsync(
            1, "ch1.html", Arg.Any<string>(), "She said yes.");
    }

    [Fact]
    public async Task TranslateSnippetAsync_UsesTheSnippetPromptNotTheParagraphPrompt()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("She said yes.");

        await _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None);

        _promptUtility.Received(1).BuildSnippetTranslationMessages(
            "Ela disse que sim.", "Ela disse que sim. Ele concordou.", "English", "Portuguese",
            "Test Book", Arg.Any<string?>());
        _promptUtility.DidNotReceive().BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task TranslateSnippetAsync_SavesWithShowingOriginalFalse()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("She said yes.");

        var result = await _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None);

        Assert.False(result.ShowingOriginal);
        await _snippetTranslationAccess.Received(1).SaveSnippetAsync(
            Arg.Is<SnippetTranslation>(s => !s.ShowingOriginal));
    }

    [Fact]
    public async Task TranslateSnippetAsync_OriginalHashMatchesTheJsGoldenVector()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("She said yes.");

        var result = await _sut.TranslateSnippetAsync(
            1, MakeRequest(text: "Ela disse que sim."), "English", "Portuguese", CancellationToken.None);

        Assert.Equal(SnipHashGolden, result.OriginalHash);
    }

    [Fact]
    public async Task TranslateSnippetAsync_WhenCancelled_PropagatesOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", cts.Token));
    }

    [Fact]
    public async Task SetShowingOriginalAsync_DelegatesWithTheRightArguments()
    {
        var request = new SnippetToggleRequest("ch1.html", 2, 0, 1, ShowingOriginal: true);

        await _sut.SetShowingOriginalAsync(1, request);

        await _snippetTranslationAccess.Received(1).SetShowingOriginalAsync(1, "ch1.html", 2, 0, 1, true);
    }

    [Fact]
    public async Task RemoveSnippetAsync_DelegatesWithTheRightArguments()
    {
        var request = new SnippetRemoveRequest("ch1.html", 3, 1, 2);

        await _sut.RemoveSnippetAsync(1, request);

        await _snippetTranslationAccess.Received(1).RemoveSnippetAsync(1, "ch1.html", 3, 1, 2);
    }

    [Fact]
    public async Task FetchSnippetsAsync_DelegatesToAccess()
    {
        var expected = new List<SnippetTranslation>
        {
            new(1, 1, "ch1.html", 0, 0, 0, "hash", "text", false, DateTime.UtcNow),
        };
        _snippetTranslationAccess.FetchSnippetsAsync(1, "ch1.html").Returns(expected);

        var result = await _sut.FetchSnippetsAsync(1, "ch1.html");

        Assert.Same(expected, result);
    }
}
