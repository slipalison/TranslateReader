using NSubstitute;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class ReadingManagerTests
{
    private readonly IBooksAccess _booksAccess = Substitute.For<IBooksAccess>();
    private readonly IReadingStateAccess _readingStateAccess = Substitute.For<IReadingStateAccess>();
    private readonly IParsingEngine _parsingEngine = Substitute.For<IParsingEngine>();
    private readonly IFileUtility _fileUtility = Substitute.For<IFileUtility>();
    private readonly ReadingManager _sut;

    public ReadingManagerTests()
    {
        _sut = new ReadingManager(_booksAccess, _readingStateAccess, _parsingEngine, _fileUtility, "/tmp/books");
    }

    [Fact]
    public async Task OpenBookAsync_DelegatesToBooksAccess()
    {
        var book = new Book { Id = 1, Title = "Teste" };
        _booksAccess.FetchBookAsync(1).Returns(book);

        var result = await _sut.OpenBookAsync(1);

        Assert.Equal("Teste", result.Title);
    }

    [Fact]
    public async Task LoadChaptersAsync_FetchesBookThenParses()
    {
        var book = new Book { Id = 1, FilePath = "/tmp/livro.epub" };
        var chapters = new List<Chapter> { new() { HRef = "cap1.html" } };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _parsingEngine.ExtractChaptersAsync("/tmp/livro.epub").Returns(chapters);

        var result = await _sut.LoadChaptersAsync(1);

        Assert.Single(result);
        await _parsingEngine.Received(1).ExtractChaptersAsync("/tmp/livro.epub");
    }

    [Fact]
    public async Task LoadChapterContentAsync_ExtractsImagesThenParsesContent()
    {
        var book = new Book { Id = 1, FilePath = "/tmp/livro.epub" };
        _booksAccess.FetchBookAsync(1).Returns(book);
        _fileUtility.DirectoryHasContent(Arg.Any<string>()).Returns(false);
        _parsingEngine.ExtractAllImagesAsync("/tmp/livro.epub").Returns(AsAsync());
        _parsingEngine.ExtractChapterContentAsync("/tmp/livro.epub", "cap1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<p>Texto</p>");

        var result = await _sut.LoadChapterContentAsync(1, "cap1.html");

        Assert.Equal("<p>Texto</p>", result.Html);
        Assert.Contains("images", result.BaseDirectory);
        await _parsingEngine.Received(1).ExtractChapterContentAsync(
            "/tmp/livro.epub", "cap1.html", Arg.Is<string>(s => s.Contains("images")), Arg.Any<ChapterContentPurpose>());
    }

    [Fact]
    public async Task SaveProgressAsync_DelegatesToReadingStateAccess()
    {
        await _sut.SaveProgressAsync(bookId: 1, chapterHRef: "cap1.html", scrollPosition: 0.5, progressPercentage: 25);

        await _readingStateAccess.Received(1).SaveProgressAsync(Arg.Is<ReadingProgress>(p =>
            p.BookId == 1 &&
            p.ChapterHRef == "cap1.html" &&
            p.ScrollPosition == 0.5 &&
            p.ProgressPercentage == 25));
    }

    [Fact]
    public async Task LoadProgressAsync_ReturnsNullWhenNoneExists()
    {
        _readingStateAccess.FetchProgressAsync(1).Returns((ReadingProgress?)null);

        var result = await _sut.LoadProgressAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadProgressAsync_ReturnsStoredProgressForRequestedBook()
    {
        _readingStateAccess.FetchProgressAsync(1).Returns(new ReadingProgress
        {
            BookId = 1,
            ChapterHRef = "cap2.html",
            ScrollPosition = 0.42,
            ProgressPercentage = 37.5,
            UpdatedAt = DateTime.UtcNow
        });

        var result = await _sut.LoadProgressAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.BookId);
        Assert.Equal("cap2.html", result.ChapterHRef);
        Assert.Equal(0.42, result.ScrollPosition);
        Assert.Equal(37.5, result.ProgressPercentage);
    }

    [Fact]
    public async Task LoadChapterContentAsync_WritesEveryExtractedImageBelowTheBookImagesDirectory()
    {
        var booksDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var sut = new ReadingManager(_booksAccess, _readingStateAccess, _parsingEngine, _fileUtility, booksDirectory);
        var cover = new byte[] { 1, 2, 3 };
        var illustration = new byte[] { 4, 5 };
        var book = new Book { Id = 7, FilePath = "/tmp/livro.epub" };
        _booksAccess.FetchBookAsync(7).Returns(book);
        _fileUtility.DirectoryHasContent(Arg.Any<string>()).Returns(false);
        _parsingEngine.ExtractAllImagesAsync("/tmp/livro.epub").Returns(AsAsync(
            new ExtractedImage("cover.jpg", cover),
            new ExtractedImage("images/fig1.png", illustration)));
        _parsingEngine.ExtractChapterContentAsync("/tmp/livro.epub", "cap1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<p>Texto</p>");

        await sut.LoadChapterContentAsync(7, "cap1.html");

        var imagesDir = Path.Combine(booksDirectory, "images", "7");
        await _fileUtility.Received(2).WriteFileAsync(Arg.Any<string>(), Arg.Any<byte[]>());
        await _fileUtility.Received(1).WriteFileAsync(Path.Combine(imagesDir, "cover.jpg"), cover);
        await _fileUtility.Received(1).WriteFileAsync(
            Path.Combine(imagesDir, "images" + Path.DirectorySeparatorChar + "fig1.png"), illustration);
    }

    [Fact]
    public async Task LoadChapterContentAsync_WhenImagesDirectoryAlreadyHasContent_SkipsExtractionEntirely()
    {
        var book = new Book { Id = 7, FilePath = "/tmp/livro.epub" };
        _booksAccess.FetchBookAsync(7).Returns(book);
        _fileUtility.DirectoryHasContent(Path.Combine("/tmp/books", "images", "7")).Returns(true);
        _parsingEngine.ExtractChapterContentAsync("/tmp/livro.epub", "cap1.html", Arg.Any<string>(), Arg.Any<ChapterContentPurpose>())
            .Returns("<p>Texto</p>");

        var result = await _sut.LoadChapterContentAsync(7, "cap1.html");

        Assert.Equal("<p>Texto</p>", result.Html);
        _parsingEngine.DidNotReceive().ExtractAllImagesAsync(Arg.Any<string>());
        await _fileUtility.DidNotReceive().WriteFileAsync(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    // IAsyncEnumerable stubs cannot come from Task-returning helpers; an async iterator is the
    // only shape NSubstitute can hand back. Task.Yield keeps it a real iterator (no CS1998).
    private static async IAsyncEnumerable<ExtractedImage> AsAsync(params ExtractedImage[] items)
    {
        await Task.Yield();
        foreach (var item in items)
            yield return item;
    }
}
