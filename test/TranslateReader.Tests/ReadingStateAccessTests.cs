using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class ReadingStateAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private ReadingStateAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task FetchProgressAsync_ReturnsNullWhenNoneExists()
    {
        var result = await CreateSut().FetchProgressAsync(bookId: 42);
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveProgressAsync_PersistsAndFetches()
    {
        var sut = CreateSut();
        var progress = new ReadingProgress
        {
            BookId = 1,
            ChapterHRef = "cap1.html",
            ScrollPosition = 0.5,
            ProgressPercentage = 33.3,
            UpdatedAt = DateTime.UtcNow
        };

        await sut.SaveProgressAsync(progress);
        var result = await sut.FetchProgressAsync(bookId: 1);

        Assert.NotNull(result);
        Assert.Equal("cap1.html", result.ChapterHRef);
        Assert.Equal(0.5, result.ScrollPosition);
    }

    [Fact]
    public async Task SaveProgressAsync_UpsertOverwritesPrevious()
    {
        var sut = CreateSut();
        var progress = new ReadingProgress { BookId = 1, ChapterHRef = "cap1.html", ScrollPosition = 0.1, ProgressPercentage = 10, UpdatedAt = DateTime.UtcNow };
        await sut.SaveProgressAsync(progress);

        progress.ChapterHRef = "cap5.html";
        progress.ScrollPosition = 0.9;
        await sut.SaveProgressAsync(progress);

        var result = await sut.FetchProgressAsync(bookId: 1);
        Assert.Equal("cap5.html", result!.ChapterHRef);
        Assert.Equal(0.9, result.ScrollPosition);
    }

    [Fact]
    public async Task SaveBookmarkAsync_AndFetchBookmarksAsync_Work()
    {
        var sut = CreateSut();
        var bookmark = new Bookmark
        {
            BookId = 1,
            ChapterHRef = "cap2.html",
            Position = 0.75,
            Label = "Cena importante",
            CreatedAt = DateTime.UtcNow
        };

        await sut.SaveBookmarkAsync(bookmark);
        var bookmarks = await sut.FetchBookmarksAsync(bookId: 1);

        Assert.Single(bookmarks);
        Assert.Equal("Cena importante", bookmarks[0].Label);
    }

    [Fact]
    public async Task RemoveBookmarkAsync_DeletesBookmark()
    {
        var sut = CreateSut();
        var bookmark = new Bookmark { BookId = 1, ChapterHRef = "cap1.html", Position = 0, Label = "X", CreatedAt = DateTime.UtcNow };
        await sut.SaveBookmarkAsync(bookmark);
        var saved = (await sut.FetchBookmarksAsync(1))[0];

        await sut.RemoveBookmarkAsync(saved.Id);

        var remaining = await sut.FetchBookmarksAsync(1);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task RemoveStateForBookAsync_RemovesProgressAndBookmarks()
    {
        var sut = CreateSut();
        var progress = new ReadingProgress { BookId = 1, ChapterHRef = "cap1.html", ScrollPosition = 0.5, ProgressPercentage = 33.3, UpdatedAt = DateTime.UtcNow };
        var bookmark = new Bookmark { BookId = 1, ChapterHRef = "cap2.html", Position = 0.75, Label = "Marcação", CreatedAt = DateTime.UtcNow };
        await sut.SaveProgressAsync(progress);
        await sut.SaveBookmarkAsync(bookmark);

        await sut.RemoveStateForBookAsync(1);

        Assert.Null(await sut.FetchProgressAsync(1));
        Assert.Empty(await sut.FetchBookmarksAsync(1));
    }
}
