using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class BooksAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private BooksAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose() => _db.Dispose();

    private static Book MakeBook(
        string title = "Livro Teste",
        DateTime? lastOpenedAt = null,
        DateTime? dateAdded = null) => new()
        {
            Title = title,
            Author = "Autor",
            Publisher = "Editora",
            Language = "pt",
            CoverImagePath = "",
            FilePath = "/tmp/livro.epub",
            TotalChapters = 3,
            DateAdded = dateAdded ?? DateTime.UtcNow,
            LastOpenedAt = lastOpenedAt
        };

    [Fact]
    public async Task SaveBookAsync_ReturnsPositiveId()
    {
        var id = await CreateSut().SaveBookAsync(MakeBook());
        Assert.True(id > 0);
    }

    [Fact]
    public async Task FetchBookAsync_ReturnsBookById()
    {
        var sut = CreateSut();
        var id = await sut.SaveBookAsync(MakeBook("Dom Casmurro"));

        var book = await sut.FetchBookAsync(id);

        Assert.Equal("Dom Casmurro", book.Title);
        Assert.Equal(id, book.Id);
    }

    [Fact]
    public async Task FetchBookAsync_ThrowsForUnknownId()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().FetchBookAsync(9999));
    }

    [Fact]
    public async Task FetchAllBooksAsync_ReturnsAllSaved()
    {
        var sut = CreateSut();
        await sut.SaveBookAsync(MakeBook("Livro A"));
        await sut.SaveBookAsync(MakeBook("Livro B"));

        var books = await sut.FetchAllBooksAsync();

        Assert.Equal(2, books.Count);
    }

    [Fact]
    public async Task RemoveBookAsync_DeletesBook()
    {
        var sut = CreateSut();
        var id = await sut.SaveBookAsync(MakeBook());

        await sut.RemoveBookAsync(id);

        var books = await sut.FetchAllBooksAsync();
        Assert.Empty(books);
    }

    [Fact]
    public async Task SaveChaptersAsync_PersistsChapters()
    {
        var sut = CreateSut();
        var bookId = await sut.SaveBookAsync(MakeBook());
        var chapters = new List<Chapter>
        {
            new() { BookId = bookId, Title = "Cap 1", OrderIndex = 0, HRef = "cap1.html" },
            new() { BookId = bookId, Title = "Cap 2", OrderIndex = 1, HRef = "cap2.html" }
        };

        var exception = await Record.ExceptionAsync(() => sut.SaveChaptersAsync(chapters));

        Assert.Null(exception);
    }

    [Fact]
    public async Task FetchAllBooksAsync_OrdersByLastOpenedAtDescending_WithNeverOpenedLast()
    {
        var sut = CreateSut();
        var now = DateTime.UtcNow;
        await sut.SaveBookAsync(MakeBook("Lido ontem", lastOpenedAt: now.AddDays(-1), dateAdded: now.AddDays(-3)));
        await sut.SaveBookAsync(MakeBook("Lido agora", lastOpenedAt: now.AddHours(-1), dateAdded: now.AddDays(-2)));
        await sut.SaveBookAsync(MakeBook("Nunca aberto", lastOpenedAt: null, dateAdded: now.AddDays(-1)));

        var titles = (await sut.FetchAllBooksAsync()).Select(b => b.Title).ToList();

        Assert.Equal(["Lido agora", "Lido ontem", "Nunca aberto"], titles);
    }

    [Fact]
    public async Task FetchAllBooksAsync_OrdersNeverOpenedBooksByDateAddedDescending()
    {
        var sut = CreateSut();
        var now = DateTime.UtcNow;
        await sut.SaveBookAsync(MakeBook("Importado antes", dateAdded: now.AddDays(-2)));
        await sut.SaveBookAsync(MakeBook("Importado depois", dateAdded: now));

        var titles = (await sut.FetchAllBooksAsync()).Select(b => b.Title).ToList();

        Assert.Equal(["Importado depois", "Importado antes"], titles);
    }
}
