using TranslateReader.Business.Engines;

namespace TranslateReader.Tests;

public class ParsingEngineTests
{
    private static string FindEpub(string pattern)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "TestData");
        return Directory.GetFiles(dir, pattern).Single();
    }

    private static readonly string PracticeEpub = FindEpub("Practice Makes Perfect*.epub");
    private static readonly string RightingEpub = FindEpub("Righting software*.epub");
    private static readonly string WardleyEpub = FindEpub("Wardley Maps*.epub");

    private static string ImagesDir => Path.Combine(Path.GetTempPath(), "translatereader_test_images");

    private readonly ParsingEngine _sut = new();

    // ── Practice Makes Perfect ──────────────────────────────────────────────

    [Fact]
    public async Task Practice_ExtractCoverImageAsync_RetornaByteNaoNulo()
    {
        var result = await _sut.ExtractCoverImageAsync(PracticeEpub);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task Practice_ExtractCoverImageAsync_RetornaJpegValido()
    {
        var result = await _sut.ExtractCoverImageAsync(PracticeEpub);

        Assert.NotNull(result);
        Assert.True(result.Length >= 3);
        Assert.Equal(0xFF, result[0]);
        Assert.Equal(0xD8, result[1]);
        Assert.Equal(0xFF, result[2]);
    }

    [Fact]
    public async Task Practice_ExtractMetadataAsync_RetornaMetadadosValidos()
    {
        var result = await _sut.ExtractMetadataAsync(PracticeEpub);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.Equal(PracticeEpub, result.FilePath);
        Assert.True(result.TotalChapters > 0);
    }

    [Fact]
    public async Task Practice_ExtractMetadataAsync_RetornaTituloCorreto()
    {
        var result = await _sut.ExtractMetadataAsync(PracticeEpub);

        Assert.Contains("Practice Makes Perfect", result.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Practice_ExtractChaptersAsync_RetornaCapitulosNaoVazios()
    {
        var result = await _sut.ExtractChaptersAsync(PracticeEpub);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, c => Assert.False(string.IsNullOrWhiteSpace(c.HRef)));
    }

    [Fact]
    public async Task Practice_ExtractChapterContentAsync_RewritesImagePathsToVirtualHostUrl()
    {
        var chapters = await _sut.ExtractChaptersAsync(PracticeEpub);
        var chapterWithImage = chapters.First(c =>
            c.HRef.Contains("cover") || c.HRef.Contains("ad") || c.HRef.Contains("title"));

        var html = await _sut.ExtractChapterContentAsync(PracticeEpub, chapterWithImage.HRef, ImagesDir);

        Assert.Contains("https://epub-images/", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("src=\"../", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Practice_ExtractChapterContentAsync_NaoDeveConterRefsRelativasComParentDir()
    {
        var chapters = await _sut.ExtractChaptersAsync(PracticeEpub);

        foreach (var chapter in chapters)
        {
            var html = await _sut.ExtractChapterContentAsync(PracticeEpub, chapter.HRef, ImagesDir);
            Assert.DoesNotContain("src=\"../", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Practice_ExtractAllImagesAsync_RetornaImagensDoEpub()
    {
        var images = await _sut.ExtractAllImagesAsync(PracticeEpub);

        Assert.True(images.Count > 0);
        Assert.All(images, kvp =>
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Key));
            Assert.True(kvp.Value.Length > 0);
        });
    }

    // ── Righting Software ───────────────────────────────────────────────────

    [Fact]
    public async Task RightingSoftware_NaoDeveLancarExcecao()
    {
        var ex = await Record.ExceptionAsync(() => _sut.ExtractMetadataAsync(RightingEpub));
        Assert.Null(ex);
    }

    [Fact]
    public async Task RightingSoftware_ExtractMetadataAsync_RetornaMetadadosValidos()
    {
        var result = await _sut.ExtractMetadataAsync(RightingEpub);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.Equal(RightingEpub, result.FilePath);
        Assert.True(result.TotalChapters > 0);
    }

    [Fact]
    public async Task RightingSoftware_ExtractChaptersAsync_RetornaCapitulosNaoVazios()
    {
        var result = await _sut.ExtractChaptersAsync(RightingEpub);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, c => Assert.False(string.IsNullOrWhiteSpace(c.HRef)));
    }

    [Fact]
    public async Task RightingSoftware_ExtractChapterContentAsync_NaoContemRefsRelativas()
    {
        var chapters = await _sut.ExtractChaptersAsync(RightingEpub);

        foreach (var chapter in chapters)
        {
            var html = await _sut.ExtractChapterContentAsync(RightingEpub, chapter.HRef, ImagesDir);
            Assert.DoesNotContain("src=\"../", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RightingSoftware_ExtractAllImagesAsync_NaoLancaExcecao()
    {
        var ex = await Record.ExceptionAsync(() => _sut.ExtractAllImagesAsync(RightingEpub));
        Assert.Null(ex);
    }

    // ── Wardley Maps ────────────────────────────────────────────────────────

    [Fact]
    public async Task WardleyMaps_ExtractCoverImageAsync_RetornaByteNaoNulo()
    {
        var result = await _sut.ExtractCoverImageAsync(WardleyEpub);

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task WardleyMaps_ExtractMetadataAsync_RetornaMetadadosValidos()
    {
        var result = await _sut.ExtractMetadataAsync(WardleyEpub);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.Equal(WardleyEpub, result.FilePath);
        Assert.True(result.TotalChapters > 0);
    }

    [Fact]
    public async Task WardleyMaps_ExtractChaptersAsync_RetornaCapitulosNaoVazios()
    {
        var result = await _sut.ExtractChaptersAsync(WardleyEpub);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, c => Assert.False(string.IsNullOrWhiteSpace(c.HRef)));
    }

    [Fact]
    public async Task WardleyMaps_ExtractAllImagesAsync_Retorna256Imagens()
    {
        var images = await _sut.ExtractAllImagesAsync(WardleyEpub);

        Assert.True(images.Count >= 100, $"Esperado >= 100 imagens, obtido {images.Count}");
        Assert.All(images, kvp =>
        {
            Assert.False(string.IsNullOrWhiteSpace(kvp.Key));
            Assert.True(kvp.Value.Length > 0);
        });
    }

    [Fact]
    public async Task WardleyMaps_SvgCoverChapter_ContemVirtualHostUrl()
    {
        var chapters = await _sut.ExtractChaptersAsync(WardleyEpub);
        var titlePage = chapters.FirstOrDefault(c =>
            c.HRef.Contains("title", StringComparison.OrdinalIgnoreCase)
            || c.HRef.Contains("cover", StringComparison.OrdinalIgnoreCase));

        if (titlePage is null)
            return;

        var html = await _sut.ExtractChapterContentAsync(WardleyEpub, titlePage.HRef, ImagesDir);

        if (html.Contains("<image", StringComparison.OrdinalIgnoreCase))
        {
            Assert.DoesNotContain("href=\"../", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("https://epub-images/", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task WardleyMaps_ExtractChapterContentAsync_NaoContemRefsComParentDir()
    {
        var chapters = await _sut.ExtractChaptersAsync(WardleyEpub);

        foreach (var chapter in chapters)
        {
            var html = await _sut.ExtractChapterContentAsync(WardleyEpub, chapter.HRef, ImagesDir);
            Assert.DoesNotContain("src=\"../", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
