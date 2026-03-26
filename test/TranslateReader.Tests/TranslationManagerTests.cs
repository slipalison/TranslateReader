using NSubstitute;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class TranslationManagerTests
{
    private readonly ITranslationEngine _translationEngine = Substitute.For<ITranslationEngine>();
    private readonly IModelAccess _modelAccess = Substitute.For<IModelAccess>();
    private readonly ITranslationCacheAccess _cacheAccess = Substitute.For<ITranslationCacheAccess>();
    private readonly IPromptUtility _promptUtility = Substitute.For<IPromptUtility>();
    private readonly IBooksAccess _booksAccess = Substitute.For<IBooksAccess>();
    private readonly IParsingEngine _parsingEngine = Substitute.For<IParsingEngine>();
    private readonly TranslationManager _sut;

    public TranslationManagerTests()
    {
        _sut = new TranslationManager(
            _translationEngine,
            _modelAccess,
            _cacheAccess,
            _promptUtility,
            _booksAccess,
            _parsingEngine);
    }

    [Fact]
    public async Task DownloadModelIfNeededAsync_WhenModelExists_DoesNotDownload()
    {
        _modelAccess.IsModelAvailable().Returns(true);

        await _sut.DownloadModelIfNeededAsync(null, CancellationToken.None);

        await _modelAccess.DidNotReceive().DownloadModelAsync(
            Arg.Any<string>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadModelIfNeededAsync_WhenModelMissing_Downloads()
    {
        _modelAccess.IsModelAvailable().Returns(false);

        await _sut.DownloadModelIfNeededAsync(null, CancellationToken.None);

        await _modelAccess.Received(1).DownloadModelAsync(
            Arg.Any<string>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeEngineIfNeededAsync_InitializesEngine()
    {
        _translationEngine.IsReady.Returns(false);
        _modelAccess.GetModelPath().Returns("/models/model.gguf");

        await _sut.InitializeEngineIfNeededAsync(CancellationToken.None);

        await _translationEngine.Received(1).InitializeAsync("/models/model.gguf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeEngineIfNeededAsync_WhenEngineAlreadyReady_SkipsInitialization()
    {
        _translationEngine.IsReady.Returns(true);

        await _sut.InitializeEngineIfNeededAsync(CancellationToken.None);

        await _translationEngine.DidNotReceive().InitializeAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateChapterAsync_WithCacheHit_DoesNotCallEngine()
    {
        SetupBookAndChapter("<html><body><p>Hello world</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns("Ola mundo");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", CancellationToken.None))
            results.Add(p);

        Assert.Single(results);
        Assert.Equal("Ola mundo", results[0].Translated);
        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateChapterAsync_WithCacheMiss_CallsEngineAndSaves()
    {
        SetupBookAndChapter("<html><body><p>Hello world</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync("system", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola mundo");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", CancellationToken.None))
            results.Add(p);

        Assert.Single(results);
        Assert.Equal("Ola mundo", results[0].Translated);
        await _cacheAccess.Received(1).SaveTranslationAsync(
            1, "ch1.html", Arg.Any<string>(), "Ola mundo");
    }

    [Fact]
    public async Task TranslateChapterAsync_PassesPreviousParagraphAsContext()
    {
        SetupBookAndChapter("<html><body><p>First paragraph</p><p>Second paragraph</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Primeiro paragrafo", "Segundo paragrafo");
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", CancellationToken.None))
            results.Add(p);

        Assert.Equal(2, results.Count);
        _promptUtility.Received(1).BuildTranslationMessages(
            "Second paragraph", Arg.Any<string?>(),
            Arg.Any<string?>(), "Primeiro paragrafo");
    }

    [Fact]
    public async Task TranslateChapterAsync_ReportsProgressCorrectly()
    {
        SetupBookAndChapter("<html><body><p>One</p><p>Two</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Um", "Dois");
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", CancellationToken.None))
            results.Add(p);

        Assert.Equal(0.5, results[0].Progress);
        Assert.Equal(1.0, results[1].Progress);
        Assert.Equal(2, results[0].TotalParagraphs);
    }

    [Fact]
    public async Task DeleteModelAsync_DelegatesToModelAccess()
    {
        await _sut.DeleteModelAsync();

        await _modelAccess.Received(1).DeleteModelAsync();
    }

    [Fact]
    public async Task TranslateChapterAsync_SkipsEmptyParagraphs()
    {
        SetupBookAndChapter("<html><body><p>Hello</p><p>   </p><p>World</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola", "Mundo");
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", CancellationToken.None))
            results.Add(p);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task TranslateParagraphsAsync_WithCacheHit_DoesNotCallEngine()
    {
        SetupBook();
        var paragraphs = new List<VisibleParagraph> { new(0, "Hello world") };
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns("Ola mundo");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", paragraphs, CancellationToken.None))
            results.Add(p);

        Assert.Single(results);
        Assert.Equal("Ola mundo", results[0].Translated);
        Assert.Equal(0, results[0].Index);
        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateParagraphsAsync_WithCacheMiss_CallsEngineAndSaves()
    {
        SetupBook();
        var paragraphs = new List<VisibleParagraph> { new(3, "Hello world") };
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync("system", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola mundo");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", paragraphs, CancellationToken.None))
            results.Add(p);

        Assert.Single(results);
        Assert.Equal("Ola mundo", results[0].Translated);
        Assert.Equal(3, results[0].Index);
        await _cacheAccess.Received(1).SaveTranslationAsync(
            1, "ch1.html", Arg.Any<string>(), "Ola mundo");
    }

    [Fact]
    public async Task TranslateParagraphsAsync_PreservesDomIndex()
    {
        SetupBook();
        var paragraphs = new List<VisibleParagraph> { new(5, "First"), new(10, "Second") };
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Primeiro", "Segundo");
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", paragraphs, CancellationToken.None))
            results.Add(p);

        Assert.Equal(2, results.Count);
        Assert.Equal(5, results[0].Index);
        Assert.Equal(10, results[1].Index);
    }

    [Fact]
    public async Task TranslateParagraphsAsync_ReportsProgressCorrectly()
    {
        SetupBook();
        var paragraphs = new List<VisibleParagraph> { new(0, "One"), new(1, "Two") };
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Um", "Dois");
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", paragraphs, CancellationToken.None))
            results.Add(p);

        Assert.Equal(0.5, results[0].Progress);
        Assert.Equal(1.0, results[1].Progress);
    }

    [Fact]
    public async Task TranslateParagraphsAsync_PassesPreviousTranslationAsContext()
    {
        SetupBook();
        var paragraphs = new List<VisibleParagraph> { new(0, "First paragraph"), new(1, "Second paragraph") };
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Primeiro paragrafo", "Segundo paragrafo");
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", paragraphs, CancellationToken.None))
            results.Add(p);

        _promptUtility.Received(1).BuildTranslationMessages(
            "Second paragraph", Arg.Any<string?>(),
            Arg.Any<string?>(), "Primeiro paragrafo");
    }

    private void SetupBook()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter> { new() { HRef = "ch1.html", Title = "Chapter 1" } };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
    }

    private void SetupBookAndChapter(string html)
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter> { new() { HRef = "ch1.html", Title = "Chapter 1" } };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns(html);
    }
}
