using TranslateReader.Access;

namespace TranslateReader.Tests;

public class TranslationCacheAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private TranslationCacheAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task FetchTranslationAsync_ReturnsNullWhenNotCached()
    {
        var result = await CreateSut().FetchTranslationAsync(1, "cap1.html", "abc123");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndFetch_RoundTrip()
    {
        var sut = CreateSut();

        await sut.SaveTranslationAsync(1, "cap1.html", "hash1", "Texto traduzido");
        var result = await sut.FetchTranslationAsync(1, "cap1.html", "hash1");

        Assert.Equal("Texto traduzido", result);
    }

    [Fact]
    public async Task SaveTranslationAsync_UpsertsOnDuplicate()
    {
        var sut = CreateSut();

        await sut.SaveTranslationAsync(1, "cap1.html", "hash1", "Primeira versao");
        await sut.SaveTranslationAsync(1, "cap1.html", "hash1", "Segunda versao");

        var result = await sut.FetchTranslationAsync(1, "cap1.html", "hash1");
        Assert.Equal("Segunda versao", result);
    }

    [Fact]
    public async Task FetchTranslationAsync_DistinguishesByHash()
    {
        var sut = CreateSut();

        await sut.SaveTranslationAsync(1, "cap1.html", "hashA", "Traducao A");
        await sut.SaveTranslationAsync(1, "cap1.html", "hashB", "Traducao B");

        Assert.Equal("Traducao A", await sut.FetchTranslationAsync(1, "cap1.html", "hashA"));
        Assert.Equal("Traducao B", await sut.FetchTranslationAsync(1, "cap1.html", "hashB"));
    }

    [Fact]
    public async Task RemoveTranslationsForBookAsync_RemovesOnlyTargetBook()
    {
        var sut = CreateSut();

        await sut.SaveTranslationAsync(1, "cap1.html", "h1", "Livro 1 traducao");
        await sut.SaveTranslationAsync(2, "cap1.html", "h2", "Livro 2 traducao");

        await sut.RemoveTranslationsForBookAsync(1);

        Assert.Null(await sut.FetchTranslationAsync(1, "cap1.html", "h1"));
        Assert.Equal("Livro 2 traducao", await sut.FetchTranslationAsync(2, "cap1.html", "h2"));
    }

    [Fact]
    public async Task RemoveTranslationsForBookAsync_RemovesAllChapters()
    {
        var sut = CreateSut();

        await sut.SaveTranslationAsync(1, "cap1.html", "h1", "Cap 1");
        await sut.SaveTranslationAsync(1, "cap2.html", "h2", "Cap 2");
        await sut.SaveTranslationAsync(1, "cap3.html", "h3", "Cap 3");

        await sut.RemoveTranslationsForBookAsync(1);

        Assert.Null(await sut.FetchTranslationAsync(1, "cap1.html", "h1"));
        Assert.Null(await sut.FetchTranslationAsync(1, "cap2.html", "h2"));
        Assert.Null(await sut.FetchTranslationAsync(1, "cap3.html", "h3"));
    }
}
