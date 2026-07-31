using System.Globalization;
using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

/// <summary>
/// Datas sao gravadas como string ISO-8601 e relidas com <c>DateTime.Parse</c>. Estes casos
/// prendem esse round-trip sob culturas de calendario nao-gregoriano, onde uma leitura ou uma
/// escrita sensivel a cultura desloca a data em centenas de anos.
/// </summary>
public sealed class CultureRoundTripTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        public CultureScope(string name) => CultureInfo.CurrentCulture = new CultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    private static readonly DateTime Added = new(2026, 7, 30, 15, 42, 7, DateTimeKind.Utc);
    private static readonly DateTime Opened = new(2026, 7, 31, 8, 5, 1, DateTimeKind.Utc);

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public async Task BooksAccess_RoundTripsBookDates_UnderANonGregorianCulture(string cultureName)
    {
        using var culture = new CultureScope(cultureName);
        var sut = new BooksAccess(_db.ConnectionString, initializeOnStartup: true);
        var id = await sut.SaveBookAsync(new Book
        {
            Title = "Livro",
            FilePath = "/tmp/livro.epub",
            TotalChapters = 3,
            DateAdded = Added,
            LastOpenedAt = Opened
        });

        var stored = await sut.FetchBookAsync(id);

        Assert.Equal(Added, stored.DateAdded.ToUniversalTime());
        Assert.Equal(Opened, stored.LastOpenedAt!.Value.ToUniversalTime());
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public async Task BooksAccess_RoundTripsANullLastOpenedAt_UnderANonGregorianCulture(string cultureName)
    {
        using var culture = new CultureScope(cultureName);
        var sut = new BooksAccess(_db.ConnectionString, initializeOnStartup: true);
        var id = await sut.SaveBookAsync(new Book { Title = "Sem leitura", DateAdded = Added, LastOpenedAt = null });

        var stored = await sut.FetchBookAsync(id);

        Assert.Null(stored.LastOpenedAt);
        Assert.Equal(Added, stored.DateAdded.ToUniversalTime());
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public async Task ReadingStateAccess_RoundTripsProgressAndBookmarkDates_UnderANonGregorianCulture(string cultureName)
    {
        using var culture = new CultureScope(cultureName);
        var sut = new ReadingStateAccess(_db.ConnectionString, initializeOnStartup: true);
        await sut.SaveProgressAsync(new ReadingProgress
        {
            BookId = 7,
            ChapterHRef = "cap1.html",
            ScrollPosition = 0.5,
            ProgressPercentage = 12.5,
            UpdatedAt = Added
        });
        await sut.SaveBookmarkAsync(new Bookmark
        {
            BookId = 7,
            ChapterHRef = "cap1.html",
            Position = 0.25,
            Label = "marca",
            CreatedAt = Opened
        });

        var progress = await sut.FetchProgressAsync(7);
        var bookmarks = await sut.FetchBookmarksAsync(7);

        Assert.NotNull(progress);
        Assert.Equal(Added, progress.UpdatedAt.ToUniversalTime());
        Assert.Equal(Opened, Assert.Single(bookmarks).CreatedAt.ToUniversalTime());
    }

    [Theory]
    [InlineData("ar-SA")]
    [InlineData("th-TH")]
    [InlineData("fa-IR")]
    public async Task BookTranslationJobAccess_RoundTripsJobDates_UnderANonGregorianCulture(string cultureName)
    {
        using var culture = new CultureScope(cultureName);
        var sut = new BookTranslationJobAccess(_db.ConnectionString, initializeOnStartup: true);
        var before = DateTime.UtcNow.AddMinutes(-1);
        await sut.SaveJobAsync(new BookTranslationJob
        {
            BookId = 9,
            SourceLanguage = "English",
            TargetLanguage = "Brazilian Portuguese (PT-BR)",
            Status = "InProgress"
        });

        var job = await sut.FetchActiveJobAsync(9);

        Assert.NotNull(job);
        var after = DateTime.UtcNow.AddMinutes(1);
        Assert.InRange(job.CreatedAt.ToUniversalTime(), before, after);
        Assert.InRange(job.UpdatedAt.ToUniversalTime(), before, after);
    }
}
