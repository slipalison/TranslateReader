using System.Security.Cryptography;
using System.Text;
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
    private readonly IBookTranslationJobAccess _jobAccess = Substitute.For<IBookTranslationJobAccess>();
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
            _jobAccess,
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
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync("system", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola mundo");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None))
            results.Add(p);

        Assert.Equal(2, results.Count);
        _promptUtility.Received(1).BuildTranslationMessages(
            "Second paragraph", Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), "Primeiro paragrafo");
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None))
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
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", paragraphs, CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync("system", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola mundo");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", paragraphs, CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", paragraphs, CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", paragraphs, CancellationToken.None))
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateParagraphsAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", paragraphs, CancellationToken.None))
            results.Add(p);

        _promptUtility.Received(1).BuildTranslationMessages(
            "Second paragraph", Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), "Primeiro paragrafo");
    }

    [Fact]
    public async Task TranslateBookAsync_TranslatesAllChaptersAndCreatesEpub()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter>
        {
            new() { HRef = "ch1.html", Title = "Chapter 1" },
            new() { HRef = "ch2.html", Title = "Chapter 2" }
        };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch2.html", Arg.Any<string>())
            .Returns("<html><body><p>World</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync("system", "user", Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola", "Mundo");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        SetupCacheForRebuild(1, "ch2.html", "World", "Mundo");
        _parsingEngine.CreateTranslatedEpubAsync(
            "/tmp/test.epub", Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), "/dest")
            .Returns("/dest/test_translated.epub");

        var result = await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.Equal("/dest/test_translated.epub", result);
        await _parsingEngine.Received(1).CreateTranslatedEpubAsync(
            "/tmp/test.epub",
            "Test Book [English \u2192 Portuguese]",
            Arg.Is<IReadOnlyDictionary<string, string>>(d => d.Count == 2),
            "/dest");
    }

    [Fact]
    public async Task TranslateBookAsync_UsesFreshContextForEachParagraph()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter> { new() { HRef = "ch1.html", Title = "Chapter 1" } };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns("<html><body><p>First</p><p>Second</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Primeiro", "Segundo");
        SetupCacheForRebuild(1, "ch1.html", "First", "Primeiro");
        SetupCacheForRebuild(1, "ch1.html", "Second", "Segundo");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        _promptUtility.DidNotReceive().BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Is<string?>(x => x != null));
    }

    [Fact]
    public async Task TranslateBookAsync_ReportsProgress()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter> { new() { HRef = "ch1.html", Title = "Chapter 1" } };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns("<html><body><p>Hello</p><p>World</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola", "Mundo");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        SetupCacheForRebuild(1, "ch1.html", "World", "Mundo");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        var progressReports = new List<BookTranslationProgress>();
        var progress = new SynchronousProgress<BookTranslationProgress>(p => progressReports.Add(p));

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", progress, CancellationToken.None);

        Assert.True(progressReports.Count >= 2);
        Assert.Equal(1, progressReports[^1].TotalChapters);
        Assert.Equal(2, progressReports[^1].TotalParagraphs);
    }

    [Fact]
    public async Task TranslateBookAsync_TranslatesHeadingsAndListItems()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter> { new() { HRef = "ch1.html", Title = "Chapter 1" } };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns("<html><body><h1>Title</h1><p>Hello</p><li>Item</li></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Titulo", "Ola", "Item traduzido");
        SetupCacheForRebuild(1, "ch1.html", "Title", "Titulo");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        SetupCacheForRebuild(1, "ch1.html", "Item", "Item traduzido");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _parsingEngine.Received(1).CreateTranslatedEpubAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["ch1.html"].Contains("Titulo") &&
                d["ch1.html"].Contains("Ola") &&
                d["ch1.html"].Contains("Item traduzido")),
            Arg.Any<string>());
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

    [Fact]
    public async Task TranslateBookAsync_SavesParagraphsToCache()
    {
        SetupBookForTranslation(out var book, out var chapters,
            "<html><body><p>Hello</p><p>World</p></body></html>");
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola", "Mundo");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        SetupCacheForRebuild(1, "ch1.html", "World", "Mundo");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html",
            ComputeHash("Hello", "English", "Portuguese"), "Ola");
        await _cacheAccess.Received(1).SaveTranslationAsync(1, "ch1.html",
            ComputeHash("World", "English", "Portuguese"), "Mundo");
    }

    [Fact]
    public async Task TranslateBookAsync_UsesCachedTranslations()
    {
        SetupBookForTranslation(out var book, out var chapters,
            "<html><body><p>Hello</p></body></html>");
        var hash = ComputeHash("Hello", "English", "Portuguese");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", hash).Returns("Ola");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateBookAsync_CreatesNewJobWhenNoneExists()
    {
        SetupBookForTranslation(out _, out _, "<html><body><p>Hello</p></body></html>");
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _jobAccess.Received(1).SaveJobAsync(Arg.Is<BookTranslationJob>(j =>
            j.BookId == 1 && j.SourceLanguage == "English" && j.TargetLanguage == "Portuguese" && j.Status == "InProgress"));
    }

    [Fact]
    public async Task TranslateBookAsync_ResumesFromLastCompletedChapter()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter>
        {
            new() { HRef = "ch1.html", Title = "Chapter 1" },
            new() { HRef = "ch2.html", Title = "Chapter 2" }
        };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch2.html", Arg.Any<string>())
            .Returns("<html><body><p>World</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns(new BookTranslationJob
        {
            Id = 10, BookId = 1, SourceLanguage = "English", TargetLanguage = "Portuguese",
            Status = "Paused", LastCompletedChapterIndex = 0
        });
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Mundo");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        SetupCacheForRebuild(1, "ch2.html", "World", "Mundo");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _translationEngine.Received(1).GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateBookAsync_UpdatesJobProgressAfterEachChapter()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter>
        {
            new() { HRef = "ch1.html", Title = "Chapter 1" },
            new() { HRef = "ch2.html", Title = "Chapter 2" }
        };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch2.html", Arg.Any<string>())
            .Returns("<html><body><p>World</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola", "Mundo");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        SetupCacheForRebuild(1, "ch2.html", "World", "Mundo");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _jobAccess.Received(1).UpdateJobProgressAsync(Arg.Any<int>(), Arg.Is(0), Arg.Is("InProgress"));
        await _jobAccess.Received(1).UpdateJobProgressAsync(Arg.Any<int>(), Arg.Is(1), Arg.Is("InProgress"));
    }

    [Fact]
    public async Task TranslateBookAsync_DeletesJobOnCompletion()
    {
        SetupBookForTranslation(out _, out _, "<html><body><p>Hello</p></body></html>");
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _jobAccess.Received(1).DeleteJobAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetActiveTranslationJobAsync_DelegatesCorrectly()
    {
        var expectedJob = new BookTranslationJob { Id = 5, BookId = 1, Status = "Paused" };
        _jobAccess.FetchActiveJobAsync(1).Returns(expectedJob);

        var result = await _sut.GetActiveTranslationJobAsync(1);

        Assert.Same(expectedJob, result);
        await _jobAccess.Received(1).FetchActiveJobAsync(1);
    }

    [Fact]
    public async Task PauseTranslationAsync_UpdatesJobStatus()
    {
        var job = new BookTranslationJob { Id = 7, BookId = 1, Status = "InProgress", LastCompletedChapterIndex = 2 };
        _jobAccess.FetchActiveJobAsync(1).Returns(job);

        await _sut.PauseTranslationAsync(1);

        await _jobAccess.Received(1).UpdateJobProgressAsync(7, 2, "Paused");
    }

    private void SetupBookForTranslation(out Book book, out List<Chapter> chapters, string html)
    {
        book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        chapters = [new() { HRef = "ch1.html", Title = "Chapter 1" }];
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>())
            .Returns(html);
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
    }

    private void SetupCacheForRebuild(int bookId, string chapterHRef, string originalText, string translatedText)
    {
        var hash = ComputeHash(originalText, "English", "Portuguese");
        _cacheAccess.FetchTranslationAsync(bookId, chapterHRef, hash)
            .Returns(null as string, translatedText);
    }

    private static string ComputeHash(string text, string sourceLanguage, string targetLanguage)
    {
        var input = $"{sourceLanguage}|{targetLanguage}|{text}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16];
    }

    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
