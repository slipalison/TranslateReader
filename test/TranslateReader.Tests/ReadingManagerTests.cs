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
        _parsingEngine.ExtractAllImagesAsync("/tmp/livro.epub")
            .Returns(new Dictionary<string, byte[]>());
        _parsingEngine.ExtractChapterContentAsync("/tmp/livro.epub", "cap1.html", Arg.Any<string>())
            .Returns("<p>Texto</p>");

        var result = await _sut.LoadChapterContentAsync(1, "cap1.html");

        Assert.Equal("<p>Texto</p>", result.Html);
        Assert.Contains("images", result.BaseDirectory);
        await _parsingEngine.Received(1).ExtractChapterContentAsync(
            "/tmp/livro.epub", "cap1.html", Arg.Is<string>(s => s.Contains("images")));
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
}
