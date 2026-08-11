using System.Security.Cryptography;
using System.Text;
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

    // Context-pollution fix: the snippet path salts its TranslationCache lookup key away from the
    // paragraph path's, so hardening the prompt invalidates every previously-cached (possibly
    // polluted) snippet translation without touching per-paragraph cache entries.
    [Fact]
    public async Task TranslateSnippetAsync_CacheKeyIsSaltedAwayFromTheLegacyParagraphHash()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("She said yes.");

        await _sut.TranslateSnippetAsync(
            1, MakeRequest(text: "Ela disse que sim."), "English", "Portuguese", CancellationToken.None);

        var legacyHash = LegacyParagraphHash("Ela disse que sim.", "English", "Portuguese");
        await _cacheAccess.Received(1).FetchTranslationAsync(1, "ch1.html", Arg.Is<string>(h => h != legacyHash));
        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html", Arg.Is<string>(h => h != legacyHash), "She said yes.");
    }

    // Reproduces TranslationManager's own (unsalted) paragraph-path ComputeHash independently, so the
    // test above proves the snippet path really diverged instead of assuming it.
    private static string LegacyParagraphHash(string text, string sourceLanguage, string targetLanguage)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{sourceLanguage}|{targetLanguage}|{text}"));
        return Convert.ToHexString(bytes)[..16];
    }

    // Guard (D-2026-08-09-snippet-translation): a stale/poisoned cache entry (or a pre-hardening
    // row from before this guard existed) must never be trusted just because it is present.
    [Fact]
    public async Task TranslateSnippetAsync_WhenCachedTranslationIsImplausiblyLong_TreatsItAsAMissAndOverwritesTheCache()
    {
        var poisoned = new string('x', 500);
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(poisoned);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("She said yes.");

        var result = await _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None);

        Assert.Equal("She said yes.", result.TranslatedText);
        await _translationEngine.Received(1).GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html", Arg.Any<string>(), "She said yes.");
    }

    [Fact]
    public async Task TranslateSnippetAsync_WhenFirstAttemptIsTooLong_RetriesWithoutParagraphContext()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system-with-context", "user"));
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system-without-context", "user"));
        var tooLong = new string('x', 500);
        _translationEngine.GenerateAsync("system-with-context", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(tooLong);
        _translationEngine.GenerateAsync("system-without-context", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("She said yes.");

        var result = await _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None);

        Assert.Equal("She said yes.", result.TranslatedText);
        _promptUtility.Received(1).BuildSnippetTranslationMessages(
            "Ela disse que sim.", "Ela disse que sim. Ele concordou.", "English", "Portuguese",
            "Test Book", Arg.Any<string?>());
        _promptUtility.Received(1).BuildSnippetTranslationMessages(
            "Ela disse que sim.", "English", "Portuguese", "Test Book", Arg.Any<string?>());
        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html", Arg.Any<string>(), "She said yes.");
    }

    [Fact]
    public async Task TranslateSnippetAsync_WhenBothAttemptsAreTooLong_ThrowsAndPersistsNothing()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system-with-context", "user"));
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system-without-context", "user"));
        var tooLong = new string('x', 500);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(tooLong);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.TranslateSnippetAsync(1, MakeRequest(), "English", "Portuguese", CancellationToken.None));

        await _cacheAccess.DidNotReceive().SaveTranslationAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _snippetTranslationAccess.DidNotReceive().SaveSnippetAsync(Arg.Any<SnippetTranslation>());
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
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings());

        var result = await _sut.FetchSnippetsAsync(1, "ch1.html");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FetchSnippetsAsync_WhenEmpty_DoesNotCallSettingsOrRemove()
    {
        _snippetTranslationAccess.FetchSnippetsAsync(1, "ch1.html")
            .Returns(new List<SnippetTranslation>());

        var result = await _sut.FetchSnippetsAsync(1, "ch1.html");

        Assert.Empty(result);
        await _settingsAccess.DidNotReceive().FetchSettingsAsync();
    }

    // Purge on load (iter 10, D-A): a row a small model poisoned with a refusal before this guard
    // existed must not resurface forever just because it made it into the database once.
    [Fact]
    public async Task FetchSnippetsAsync_PurgesARowThatFailsPlausibility_AndDoesNotReturnIt()
    {
        var poisoned = new SnippetTranslation(
            1, 1, "ch1.html", 0, 0, 0, "hash", "I cannot provide a translation of this text.",
            false, DateTime.UtcNow);
        var legit = new SnippetTranslation(
            2, 1, "ch1.html", 1, 0, 0, "hash2", "text", false, DateTime.UtcNow);
        _snippetTranslationAccess.FetchSnippetsAsync(1, "ch1.html")
            .Returns(new List<SnippetTranslation> { poisoned, legit });
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings());

        var result = await _sut.FetchSnippetsAsync(1, "ch1.html");

        Assert.Single(result);
        Assert.Same(legit, result[0]);
        await _snippetTranslationAccess.Received(1).RemoveSnippetAsync(1, "ch1.html", 0, 0, 0);
        await _snippetTranslationAccess.DidNotReceive().RemoveSnippetAsync(1, "ch1.html", 1, 0, 0);
    }

    // Cache-hit path (iter 10, D-A): a cached row that fails the unified plausibility predicate is
    // treated exactly like a miss - the exact refusal text observed in production, verbatim from the
    // screenshot (this is model output, never real user data).
    [Fact]
    public async Task TranslateSnippetAsync_WhenCachedTranslationIsARefusal_TreatsItAsAMissAndOverwritesTheCache()
    {
        const string refusal = "No, I cannot provide a translation of this text. It contains " +
            "explicit sexual content, which violates my safety guidelines.";
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(refusal);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ela concordou rapidamente com a proposta apresentada durante a reunião.");

        var result = await _sut.TranslateSnippetAsync(
            1, MakeRequest(), "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None);

        Assert.NotEqual(refusal, result.TranslatedText);
        await _translationEngine.Received(1).GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html", Arg.Any<string>(), Arg.Any<string>());
    }

    // Inference path (iter 10, D-A): a fresh response that reads as the wrong language retries
    // without paragraph context exactly like the too-long guard already did.
    [Fact]
    public async Task TranslateSnippetAsync_WhenInferenceReturnsTheWrongLanguage_RetriesWithoutParagraphContext()
    {
        _cacheAccess.FetchTranslationAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system-with-context", "user"));
        _promptUtility.BuildSnippetTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system-without-context", "user"));
        const string wrongLanguage =
            "The committee reviewed the proposal and decided to postpone the final vote until next week.";
        _translationEngine.GenerateAsync("system-with-context", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(wrongLanguage);
        _translationEngine.GenerateAsync("system-without-context", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ela concordou rapidamente com a proposta apresentada durante a reunião.");

        var result = await _sut.TranslateSnippetAsync(
            1, MakeRequest(), "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None);

        Assert.Equal("Ela concordou rapidamente com a proposta apresentada durante a reunião.", result.TranslatedText);
        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html", Arg.Any<string>(), result.TranslatedText);
    }
}
