using NSubstitute;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class LibraryManagerTests
{
    private readonly IBooksAccess _booksAccess = Substitute.For<IBooksAccess>();
    private readonly IReadingStateAccess _readingStateAccess = Substitute.For<IReadingStateAccess>();
    private readonly ITranslationCacheAccess _translationCacheAccess = Substitute.For<ITranslationCacheAccess>();
    private readonly ISnippetTranslationAccess _snippetTranslationAccess = Substitute.For<ISnippetTranslationAccess>();
    private readonly IParsingEngine _parsingEngine = Substitute.For<IParsingEngine>();
    private readonly IFileUtility _fileUtility = Substitute.For<IFileUtility>();
    private readonly LibraryManager _sut;

    public LibraryManagerTests()
    {
        _sut = new LibraryManager(
            _booksAccess, _readingStateAccess, _translationCacheAccess, _snippetTranslationAccess,
            _parsingEngine, _fileUtility, "/tmp/books");
    }

    [Fact]
    public async Task ImportBookAsync_OrchestatesCopyExtractAndSave()
    {
        const string sourcePath = "/origem/livro.epub";
        const string localPath = "/tmp/books/livro.epub";
        var book = new Book { Title = "Livro", Author = "Autor", FilePath = localPath };
        var chapters = new List<Chapter> { new() { Title = "Cap 1", HRef = "cap1.html", OrderIndex = 0 } };

        _fileUtility.CopyFileAsync(sourcePath, "/tmp/books").Returns(localPath);
        _parsingEngine.ExtractMetadataAsync(localPath).Returns(book);
        _parsingEngine.ExtractCoverImageAsync(localPath).Returns((byte[]?)null);
        _booksAccess.SaveBookAsync(book).Returns(42);
        _parsingEngine.ExtractChaptersAsync(localPath).Returns(chapters);

        var result = await _sut.ImportBookAsync(sourcePath);

        Assert.Equal(42, result.Id);
        await _fileUtility.Received(1).CopyFileAsync(sourcePath, "/tmp/books");
        await _parsingEngine.Received(1).ExtractMetadataAsync(localPath);
        await _booksAccess.Received(1).SaveBookAsync(book);
        await _parsingEngine.Received(1).ExtractChaptersAsync(localPath);
        await _booksAccess.Received(1).SaveChaptersAsync(Arg.Any<IEnumerable<Chapter>>());
    }

    [Fact]
    public async Task ImportBookAsync_SavesCoverImageWhenPresent()
    {
        const string sourcePath = "/origem/livro.epub";
        const string localPath = "/tmp/books/livro.epub";
        var coverBytes = new byte[] { 1, 2, 3 };
        var book = new Book { Title = "Livro", Author = "Autor", FilePath = localPath };
        var chapters = new List<Chapter>();

        _fileUtility.CopyFileAsync(sourcePath, "/tmp/books").Returns(localPath);
        _parsingEngine.ExtractMetadataAsync(localPath).Returns(book);
        _parsingEngine.ExtractCoverImageAsync(localPath).Returns(coverBytes);
        _booksAccess.SaveBookAsync(book).Returns(1);
        _parsingEngine.ExtractChaptersAsync(localPath).Returns(chapters);

        await _sut.ImportBookAsync(sourcePath);

        await _fileUtility.Received(1).WriteFileAsync(
            Arg.Is<string>(p => p.Contains("covers")),
            coverBytes);
    }

    [Fact]
    public async Task ImportBookAsync_SkipsCoverSaveWhenNoCoverPresent()
    {
        const string sourcePath = "/origem/livro.epub";
        const string localPath = "/tmp/books/livro.epub";
        var book = new Book { Title = "Livro", Author = "Autor", FilePath = localPath };
        var chapters = new List<Chapter>();

        _fileUtility.CopyFileAsync(sourcePath, "/tmp/books").Returns(localPath);
        _parsingEngine.ExtractMetadataAsync(localPath).Returns(book);
        _parsingEngine.ExtractCoverImageAsync(localPath).Returns((byte[]?)null);
        _booksAccess.SaveBookAsync(book).Returns(1);
        _parsingEngine.ExtractChaptersAsync(localPath).Returns(chapters);

        var result = await _sut.ImportBookAsync(sourcePath);

        Assert.Equal(string.Empty, result.CoverImagePath);
        await _fileUtility.DidNotReceive().WriteFileAsync(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task DeleteBookAsync_FetchesBookThenRemovesAndDeletesFile()
    {
        var book = new Book { Id = 1, FilePath = "/tmp/books/livro.epub", CoverImagePath = string.Empty };
        _booksAccess.FetchBookAsync(1).Returns(book);

        await _sut.DeleteBookAsync(1);

        await _booksAccess.Received(1).RemoveBookAsync(1);
        await _fileUtility.Received(1).DeleteFileAsync("/tmp/books/livro.epub");
    }

    [Fact]
    public async Task DeleteBookAsync_DeletesCoverFileWhenPresent()
    {
        var book = new Book { Id = 1, FilePath = "/tmp/books/livro.epub", CoverImagePath = "/tmp/books/covers/livro_cover.jpg" };
        _booksAccess.FetchBookAsync(1).Returns(book);

        await _sut.DeleteBookAsync(1);

        await _fileUtility.Received(1).DeleteFileAsync("/tmp/books/covers/livro_cover.jpg");
    }

    [Fact]
    public async Task ListBooksAsync_DelegatesToBooksAccess()
    {
        var books = new List<Book> { new() { Title = "A" }, new() { Title = "B" } };
        _booksAccess.FetchAllBooksAsync().Returns(books);

        var result = await _sut.ListBooksAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ListBookSummariesAsync_ReturnsSummaryWithProgress()
    {
        var books = new List<Book>
        {
            new() { Id = 1, Title = "Dom Casmurro", Author = "Machado", CoverImagePath = "/covers/dom.jpg" },
            new() { Id = 2, Title = "Iracema", Author = "Alencar", CoverImagePath = string.Empty }
        };
        var progress1 = new ReadingProgress { BookId = 1, ProgressPercentage = 45.0 };

        _booksAccess.FetchAllBooksAsync().Returns(books);
        _readingStateAccess.FetchProgressAsync(1).Returns(progress1);
        _readingStateAccess.FetchProgressAsync(2).Returns((ReadingProgress?)null);

        var result = await _sut.ListBookSummariesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Dom Casmurro", result[0].Title);
        Assert.Equal(45.0, result[0].ProgressPercentage);
        Assert.Equal("/covers/dom.jpg", result[0].CoverImagePath);
        Assert.Equal("Iracema", result[1].Title);
        Assert.Equal(0, result[1].ProgressPercentage);
        Assert.Equal(string.Empty, result[1].CoverImagePath);
    }

    [Fact]
    public async Task SearchBooksAsync_FiltersCorrectly()
    {
        var books = new List<Book>
        {
            new() { Title = "Dom Casmurro", Author = "Machado" },
            new() { Title = "Iracema", Author = "Alencar" }
        };
        _booksAccess.FetchAllBooksAsync().Returns(books);

        var result = await _sut.SearchBooksAsync("dom");

        Assert.Single(result);
        Assert.Equal("Dom Casmurro", result[0].Title);
    }

    [Fact]
    public async Task DeleteBookAsync_RemovesReadingStateAndImagesDirectory()
    {
        var book = new Book { Id = 5, FilePath = "/tmp/books/livro.epub", CoverImagePath = string.Empty };
        _booksAccess.FetchBookAsync(5).Returns(book);

        await _sut.DeleteBookAsync(5);

        await _translationCacheAccess.Received(1).RemoveTranslationsForBookAsync(5);
        await _readingStateAccess.Received(1).RemoveStateForBookAsync(5);
        await _fileUtility.Received(1).DeleteDirectoryAsync(Arg.Is<string>(p => p.Contains("images") && p.Contains('5')));
    }

    [Fact]
    public async Task DeleteBookAsync_RemovesSnippetTranslationsForTheBook()
    {
        var book = new Book { Id = 7, FilePath = "/tmp/books/livro.epub", CoverImagePath = string.Empty };
        _booksAccess.FetchBookAsync(7).Returns(book);

        await _sut.DeleteBookAsync(7);

        await _snippetTranslationAccess.Received(1).RemoveSnippetsForBookAsync(7);
    }

    [Fact]
    public async Task ListBookSummariesAsync_WithoutQuery_ReturnsEveryBook()
    {
        var books = new List<Book>
        {
            new() { Id = 1, Title = "Dom Casmurro", Author = "Machado" },
            new() { Id = 2, Title = "Iracema", Author = "Alencar" }
        };
        _booksAccess.FetchAllBooksAsync().Returns(books);
        _readingStateAccess.FetchProgressAsync(Arg.Any<int>()).Returns((ReadingProgress?)null);

        var result = await _sut.ListBookSummariesAsync();

        Assert.Equal(2, result.Count);
        await _booksAccess.Received(1).FetchAllBooksAsync();
    }

    [Fact]
    public async Task ListBookSummariesAsync_WithQuery_FiltersByTitleOrAuthorIgnoringCase()
    {
        var books = new List<Book>
        {
            new() { Id = 1, Title = "Dom Casmurro", Author = "Machado" },
            new() { Id = 2, Title = "Iracema", Author = "Alencar" },
            new() { Id = 3, Title = "O Cortico", Author = "Azevedo" }
        };
        _booksAccess.FetchAllBooksAsync().Returns(books);
        _readingStateAccess.FetchProgressAsync(Arg.Any<int>()).Returns((ReadingProgress?)null);

        var result = await _sut.ListBookSummariesAsync("ALENCAR");

        Assert.Single(result);
        Assert.Equal("Iracema", result[0].Title);
    }

    [Fact]
    public async Task ListBookSummariesAsync_ProjectsLastReadAtAndTotalChapters()
    {
        var readAt = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        var books = new List<Book>
        {
            new() { Id = 1, Title = "Dom Casmurro", Author = "Machado", TotalChapters = 12 },
            new() { Id = 2, Title = "Iracema", Author = "Alencar", TotalChapters = 8 }
        };
        var progress1 = new ReadingProgress { BookId = 1, UpdatedAt = readAt };
        _booksAccess.FetchAllBooksAsync().Returns(books);
        _readingStateAccess.FetchProgressAsync(1).Returns(progress1);
        _readingStateAccess.FetchProgressAsync(2).Returns((ReadingProgress?)null);

        var result = await _sut.ListBookSummariesAsync();

        Assert.Equal(readAt, result[0].LastReadAt);
        Assert.Equal(12, result[0].TotalChapters);
        Assert.Null(result[1].LastReadAt);
        Assert.Equal(8, result[1].TotalChapters);
    }

    [Fact]
    public async Task ListRecentBookSummariesAsync_OrdersByLastReadDescending()
    {
        var older = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var books = new List<Book>
        {
            new() { Id = 1, Title = "Dom Casmurro" },
            new() { Id = 2, Title = "Iracema" }
        };
        _booksAccess.FetchAllBooksAsync().Returns(books);
        _readingStateAccess.FetchProgressAsync(1).Returns(new ReadingProgress { BookId = 1, UpdatedAt = older });
        _readingStateAccess.FetchProgressAsync(2).Returns(new ReadingProgress { BookId = 2, UpdatedAt = newer });

        var result = await _sut.ListRecentBookSummariesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Iracema", result[0].Title);
        Assert.Equal("Dom Casmurro", result[1].Title);
    }

    [Fact]
    public async Task ListRecentBookSummariesAsync_ExcludesBooksWithoutReadingProgress()
    {
        var readAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var books = new List<Book>
        {
            new() { Id = 1, Title = "Dom Casmurro" },
            new() { Id = 2, Title = "Iracema" }
        };
        _booksAccess.FetchAllBooksAsync().Returns(books);
        _readingStateAccess.FetchProgressAsync(1).Returns(new ReadingProgress { BookId = 1, UpdatedAt = readAt });
        _readingStateAccess.FetchProgressAsync(2).Returns((ReadingProgress?)null);

        var result = await _sut.ListRecentBookSummariesAsync();

        Assert.Single(result);
        Assert.Equal("Dom Casmurro", result[0].Title);
    }
}
