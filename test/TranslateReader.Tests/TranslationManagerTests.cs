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
    private readonly ISettingsAccess _settingsAccess = Substitute.For<ISettingsAccess>();
    private readonly TranslationManager _sut;

    public TranslationManagerTests()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "gemma-2-2b" });
        _sut = new TranslationManager(
            _translationEngine,
            _modelAccess,
            _cacheAccess,
            _jobAccess,
            _promptUtility,
            _booksAccess,
            _parsingEngine,
            _settingsAccess);
    }

    [Fact]
    public async Task DownloadModelIfNeededAsync_WhenModelExists_DoesNotDownload()
    {
        _modelAccess.IsModelAvailable(Arg.Any<string>()).Returns(true);

        await _sut.DownloadModelIfNeededAsync(null, CancellationToken.None);

        await _modelAccess.DidNotReceive().DownloadModelAsync(
            Arg.Any<string>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadModelIfNeededAsync_WhenModelMissing_Downloads()
    {
        _modelAccess.IsModelAvailable(Arg.Any<string>()).Returns(false);

        await _sut.DownloadModelIfNeededAsync(null, CancellationToken.None);

        await _modelAccess.Received(1).DownloadModelAsync(
            Arg.Any<string>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeEngineIfNeededAsync_InitializesEngine()
    {
        _translationEngine.IsReady.Returns(false);
        _modelAccess.GetModelPath(Arg.Any<string>()).Returns("/models/model.gguf");

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
    public async Task DownloadModelIfNeededAsync_WhenSettingsSelectHyMt_DownloadsTheHyMtUrl()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "hy-mt1.5-1.8b" });
        _modelAccess.IsModelAvailable(Arg.Any<string>()).Returns(false);

        await _sut.DownloadModelIfNeededAsync(null, CancellationToken.None);

        await _settingsAccess.Received(1).FetchSettingsAsync();
        await _modelAccess.Received(1).DownloadModelAsync(
            "https://huggingface.co/tencent/HY-MT1.5-1.8B-GGUF/resolve/main/HY-MT1.5-1.8B-Q4_K_M.gguf",
            Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
        await _modelAccess.DidNotReceive().DownloadModelAsync(
            "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
            Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadModelIfNeededAsync_WhenSettingsSelectAnUnregisteredModel_FallsBackToGemma()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "qwen-2.5-3b" });
        _modelAccess.IsModelAvailable(Arg.Any<string>()).Returns(false);

        await _sut.DownloadModelIfNeededAsync(null, CancellationToken.None);

        await _settingsAccess.Received(1).FetchSettingsAsync();
        await _modelAccess.Received(1).DownloadModelAsync(
            "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
            Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitializeEngineIfNeededAsync_WhenSettingsSelectHyMt_UsesTheHyMtFileName()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "hy-mt1.5-1.8b" });
        _translationEngine.IsReady.Returns(false);
        _modelAccess.GetModelPath("HY-MT1.5-1.8B-Q4_K_M.gguf").Returns("/models/HY-MT1.5-1.8B-Q4_K_M.gguf");

        await _sut.InitializeEngineIfNeededAsync(CancellationToken.None);

        await _settingsAccess.Received(1).FetchSettingsAsync();
        await _translationEngine.Received(1).InitializeAsync(
            "/models/HY-MT1.5-1.8B-Q4_K_M.gguf", Arg.Any<CancellationToken>());
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
    public async Task GetSelectedModelStatusAsync_ReturnsTheModelSelectedInSettings()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "hy-mt1.5-1.8b" });
        _modelAccess.IsModelAvailable(Arg.Any<string>()).Returns(false);

        var status = await _sut.GetSelectedModelStatusAsync();

        Assert.Equal("hy-mt1.5-1.8b", status.Name);
        Assert.Equal("HY-MT1.5-1.8B-Q4_K_M.gguf", status.FileName);
        Assert.Equal(1_133_080_512, status.SizeBytes);
    }

    [Fact]
    public async Task GetSelectedModelStatusAsync_ReportsDownloadedWhenTheFileExists()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "gemma-2-2b" });
        _modelAccess.IsModelAvailable("gemma-2-2b-it-Q4_K_M.gguf").Returns(true);

        var status = await _sut.GetSelectedModelStatusAsync();

        Assert.True(status.IsDownloaded);
    }

    [Fact]
    public async Task GetSelectedModelStatusAsync_FallsBackToGemmaForAnUnregisteredName()
    {
        _settingsAccess.FetchSettingsAsync().Returns(new ReadingSettings { TranslationModelName = "qwen-2.5-3b" });
        _modelAccess.IsModelAvailable(Arg.Any<string>()).Returns(false);

        var status = await _sut.GetSelectedModelStatusAsync();

        Assert.Equal("gemma-2-2b", status.Name);
        Assert.Equal("gemma-2-2b-it-Q4_K_M.gguf", status.FileName);
        Assert.False(status.IsDownloaded);
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

    // Calibre exports hold every paragraph in a leaf `div class="calibreN"` and never emit a `p`,
    // so a chapter body like this one is exactly what the reader hands to the manager for a book
    // that reproduces the reported defect.
    [Fact]
    public async Task TranslateChapterAsync_ForCalibreStyleBody_TranslatesLeafDivParagraphs()
    {
        SetupBookAndChapter($"<html><body>{CalibreFixtures.PartiallyCoveredBody}</body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Primeiro", "Segundo", "Terceiro");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", CancellationToken.None))
            results.Add(p);

        Assert.Equal(3, results.Count);
        Assert.Equal(
            [
                "First calibre paragraph with real text.",
                "Second calibre paragraph with more text.",
                "Third paragraph, letters only matter here.",
            ],
            results.Select(r => r.Original));
        Assert.Equal(["Primeiro", "Segundo", "Terceiro"], results.Select(r => r.Translated));
        Assert.Equal([0, 1, 2], results.Select(r => r.Index));
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch2.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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

        Assert.Equal("/dest/test_translated.epub", result.EpubPath);
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch2.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<html><body><p>World</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns(new BookTranslationJob
        {
            Id = 10,
            BookId = 1,
            SourceLanguage = "English",
            TargetLanguage = "Portuguese",
            Status = "Paused",
            LastCompletedChapterIndex = 0
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
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch2.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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

    [Fact]
    public async Task PauseTranslationAsync_WithoutActiveJob_DoesNotUpdateAnyJob()
    {
        _jobAccess.FetchActiveJobAsync(1).Returns((BookTranslationJob?)null);

        await _sut.PauseTranslationAsync(1);

        await _jobAccess.DidNotReceive().UpdateJobProgressAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TranslateBookAsync_WhenCancelledMidLoop_PausesJobAndSkipsEpubCreation()
    {
        var book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        var chapters = new List<Chapter>
        {
            new() { HRef = "ch1.html", Title = "Chapter 1" },
            new() { HRef = "ch2.html", Title = "Chapter 2" }
        };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<html><body><p>Hello</p></body></html>");
        _jobAccess.FetchActiveJobAsync(1).Returns(new BookTranslationJob
        {
            Id = 42,
            BookId = 1,
            SourceLanguage = "English",
            TargetLanguage = "Portuguese",
            Status = "Paused",
            LastCompletedChapterIndex = -1
        });
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        _promptUtility.BuildTranslationMessages(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(("system", "user"));
        using var cts = new CancellationTokenSource();
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return "Ola";
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, cts.Token));

        await _jobAccess.Received(1).UpdateJobProgressAsync(42, 0, "Paused");
        await _jobAccess.DidNotReceive().DeleteJobAsync(Arg.Any<int>());
        await _parsingEngine.DidNotReceive().CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TranslateChapterAsync_WithCancelledToken_ThrowsWhileIterating()
    {
        SetupBookAndChapter("<html><body><p>Hello world</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _sut.TranslateChapterAsync(
                1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", cts.Token))
            {
            }
        });

        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateParagraphsAsync_WithCancelledToken_ThrowsWhileIterating()
    {
        SetupBook();
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>())
            .Returns((string?)null);
        var paragraphs = new List<VisibleParagraph> { new(0, "Hello world") };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _sut.TranslateParagraphsAsync(
                1, "ch1.html", "English", "Brazilian Portuguese (PT-BR)", paragraphs, cts.Token))
            {
            }
        });

        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateBookAsync_HtmlEncodesTranslatedTextBeforeBuildingTheEpub()
    {
        const string hostileTranslation = "<script>alert(1)</script> Tom & Jerry";
        SetupBookForTranslation(out _, out _, "<html><body><p>Hello</p></body></html>");
        _translationEngine.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(hostileTranslation);
        SetupCacheForRebuild(1, "ch1.html", "Hello", hostileTranslation);
        IReadOnlyDictionary<string, string>? written = null;
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns(call =>
            {
                written = call.Arg<IReadOnlyDictionary<string, string>>();
                return "/dest/out.epub";
            });

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.NotNull(written);
        var chapterHtml = written["ch1.html"];
        Assert.Contains("&lt;script&gt;", chapterHtml);
        Assert.Contains("&amp;", chapterHtml);
        Assert.DoesNotContain("<script>", chapterHtml);
    }

    // Fixture A of the phase CONTEXT as a chapter document: three leaf divs carry prose, the image
    // div and the bullet div do not. The bullet is 7 non-space characters that no block covers, so
    // the ratio must drop. The markup is the copy shared with HtmlUtilityTests.
    private const string CalibreChapterHtml =
        "<html><body>" + CalibreFixtures.PartiallyCoveredBody + "</body></html>";

    // Fixture B of the phase CONTEXT: every non-space character sits inside the single leaf div.
    private const string FullyCoveredChapterHtml =
        "<html><body>" + CalibreFixtures.FullyCoveredBody + "</body></html>";

    [Fact]
    public async Task TranslateBookAsync_CoveredTextRatio_IsBelowOneWhenTextEscapesEveryBlock()
    {
        SetupBookForTranslation(out _, out _, CalibreChapterHtml);
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        var result = await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.True(result.CoveredTextRatio < 1.0, $"ratio was {result.CoveredTextRatio}");
        Assert.Equal(106d / 113d, result.CoveredTextRatio, 10);
    }

    [Fact]
    public async Task TranslateBookAsync_CoveredTextRatio_IsOneWhenEveryCharacterIsCovered()
    {
        SetupBookForTranslation(out _, out _, FullyCoveredChapterHtml);
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        var result = await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.Equal(1.0, result.CoveredTextRatio);
        Assert.Equal("/dest/out.epub", result.EpubPath);
    }

    [Fact]
    public async Task TranslateBookAsync_WithZeroCoverageChapter_CompletesWithoutThrowing()
    {
        const string html = "<html><body><img src=\"fig.png\"/>Loose caption outside any block.</body></html>";
        SetupBookForTranslation(out _, out _, html);
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        var result = await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.Equal(0.0, result.CoveredTextRatio);
        Assert.Equal("/dest/out.epub", result.EpubPath);
        await _jobAccess.Received(1).DeleteJobAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task TranslateBookAsync_CoveredTextRatio_IsOneWhenTheBodyHasNoTextAtAll()
    {
        const string html = "<html><body><img src=\"fig.png\"/></body></html>";
        SetupBookForTranslation(out _, out _, html);
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        var result = await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.Equal(1.0, result.CoveredTextRatio);
        await _translationEngine.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // EPUB HTML is untrusted input: a raw '<' inside a leaf div is invalid XHTML, yet it makes the
    // stripped block longer than the stripped body (the '<' pairs with the '>' of the closing tag),
    // so the ratio has to be clamped or the coverage signal reports more than 100%.
    [Fact]
    public async Task TranslateBookAsync_CoveredTextRatio_IsNeverAboveOneOnMalformedHtml()
    {
        const string html = "<html><body><div class=\"c\">a < b</div></body></html>";
        SetupBookForTranslation(out _, out _, html);
        _cacheAccess.FetchTranslationAsync(1, Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        var result = await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        Assert.Equal(1.0, result.CoveredTextRatio);
    }

    // Purpose is what keeps the app-only rewrites out of the exported .epub, so it is pinned per
    // call site instead of being left to Arg.Any (D-2026-08-01-translated-epub-images-4).

    [Fact]
    public async Task TranslateBookAsync_UsesExportPurposeForCacheExtractionAndRebuild()
    {
        SetupBookForTranslation(out _, out _, "<html><body><p>Hello</p></body></html>");
        _translationEngine.GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<float>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("Ola");
        SetupCacheForRebuild(1, "ch1.html", "Hello", "Ola");
        _parsingEngine.CreateTranslatedEpubAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<string>())
            .Returns("/dest/out.epub");

        await _sut.TranslateBookAsync(1, "English", "Portuguese", "/dest", null, CancellationToken.None);

        await _parsingEngine.Received(2).ExtractChapterContentAsync(
            "/tmp/test.epub", "ch1.html", Arg.Any<string>(), ChapterContentPurpose.Export);
        await _parsingEngine.DidNotReceive().ExtractChapterContentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), ChapterContentPurpose.Display);
    }

    [Fact]
    public async Task TranslateChapterAsync_UsesExportPurposeToReadChapterHtml()
    {
        SetupBookForTranslation(out _, out _, "<html><body><p>Hello</p></body></html>");
        _cacheAccess.FetchTranslationAsync(1, "ch1.html", Arg.Any<string>()).Returns("Ola");

        var results = new List<TranslatedParagraph>();
        await foreach (var p in _sut.TranslateChapterAsync(1, "ch1.html", "English", "Portuguese", CancellationToken.None))
            results.Add(p);

        Assert.Single(results);
        await _parsingEngine.Received(1).ExtractChapterContentAsync(
            "/tmp/test.epub", "ch1.html", Arg.Any<string>(), ChapterContentPurpose.Export);
        await _parsingEngine.DidNotReceive().ExtractChapterContentAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), ChapterContentPurpose.Display);
    }

    private void SetupBookForTranslation(out Book book, out List<Chapter> chapters, string html)
    {
        book = new Book { Id = 1, Title = "Test Book", FilePath = "/tmp/test.epub" };
        chapters = [new() { HRef = "ch1.html", Title = "Chapter 1" }];
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/test.epub").Returns(chapters);
        _parsingEngine.ExtractChapterContentAsync("/tmp/test.epub", "ch1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
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
