using System.Collections.Frozen;
using System.IO.Compression;
using System.Xml;
using TranslateReader.Business.Engines;

namespace TranslateReader.Tests;

// End-to-end validation of the conversion pipeline over a SHORT book (Practice, 1.7 MB / 12 images)
// and a LARGE one (Wardley, 32 MB / 256 images). Every count is checked against an INDEPENDENT
// oracle - the raw zip entries and the spine of the .opf - so the engine never grades its own
// homework. The .opf is parsed with DtdProcessing.Prohibit and no resolver
// (.claude/rules/csharp.md section 4, XXE): an EPUB is untrusted input even under TestData.
// Reading real files from disk is the named exception to section 6 already used by
// ParsingEngineTests.
public class ParsingEngineFixtureValidationTests
{
    private static readonly FrozenSet<string> ImageExtensions =
        new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg", ".webp", ".bmp" }
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static string FindEpub(string pattern)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "TestData");
        return Directory.GetFiles(dir, pattern).Single();
    }

    private static readonly string PracticeEpub = FindEpub("Practice Makes Perfect*.epub");
    private static readonly string WardleyEpub = FindEpub("Wardley Maps*.epub");

    private static string ImagesDir => Path.Combine(Path.GetTempPath(), "translatereader_fixture_images");

    private readonly ParsingEngine _sut = new();

    // ── Practice Makes Perfect (short) ──────────────────────────────────────

    [Fact]
    public async Task Practice_ExtractMetadataAsync_ReturnsTheBooksIdentity()
    {
        var book = await _sut.ExtractMetadataAsync(PracticeEpub);

        Assert.False(string.IsNullOrWhiteSpace(book.Title));
        Assert.False(string.IsNullOrWhiteSpace(book.Author));
        Assert.False(string.IsNullOrWhiteSpace(book.Language));
        Assert.True(book.TotalChapters > 0);
    }

    [Fact]
    public async Task Practice_ExtractChaptersAsync_MatchesTheSpineDeclaredInTheOpf()
    {
        var chapters = await _sut.ExtractChaptersAsync(PracticeEpub);

        Assert.Equal(SpineItemCount(PracticeEpub), chapters.Count);
        Assert.All(chapters, chapter => Assert.False(string.IsNullOrWhiteSpace(chapter.HRef)));
        Assert.Equal(chapters.Count, chapters.Select(chapter => chapter.OrderIndex).Distinct().Count());
    }

    [Fact]
    public async Task Practice_ExtractChapterContentAsync_ReturnsRenderableHtmlForEveryChapter()
    {
        var chapters = await _sut.ExtractChaptersAsync(PracticeEpub);

        foreach (var chapter in chapters)
        {
            var html = await _sut.ExtractChapterContentAsync(PracticeEpub, chapter.HRef, ImagesDir);

            Assert.False(string.IsNullOrWhiteSpace(html), $"Capitulo vazio: {chapter.HRef}");
            Assert.DoesNotContain("src=\"../", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Practice_ExtractAllImagesAsync_StreamsExactlyTheImagesInsideTheArchive()
    {
        var expected = ImageEntryCount(PracticeEpub);

        var count = 0;
        await foreach (var _ in _sut.ExtractAllImagesAsync(PracticeEpub))
            count++;

        Assert.True(expected > 0, "O oraculo do zip nao achou imagem alguma no fixture curto.");
        Assert.Equal(expected, count);
    }

    [Fact]
    public async Task Practice_ExtractAllImagesAsync_YieldsNonEmptyBytesUnderUniquePaths()
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var image in _sut.ExtractAllImagesAsync(PracticeEpub))
        {
            Assert.True(image.Content.Length > 0, $"Imagem vazia: {image.RelativePath}");
            Assert.True(paths.Add(image.RelativePath), $"Caminho repetido: {image.RelativePath}");
        }

        Assert.NotEmpty(paths);
    }

    // ── Wardley Maps (large) ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Slow")]
    public async Task Wardley_ExtractMetadataAsync_ReturnsTheBooksIdentity()
    {
        var book = await _sut.ExtractMetadataAsync(WardleyEpub);

        Assert.False(string.IsNullOrWhiteSpace(book.Title));
        Assert.False(string.IsNullOrWhiteSpace(book.Author));
        Assert.False(string.IsNullOrWhiteSpace(book.Language));
        Assert.True(book.TotalChapters > 0);
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task Wardley_ExtractChaptersAsync_MatchesTheSpineDeclaredInTheOpf()
    {
        var chapters = await _sut.ExtractChaptersAsync(WardleyEpub);

        Assert.Equal(SpineItemCount(WardleyEpub), chapters.Count);
        Assert.All(chapters, chapter => Assert.False(string.IsNullOrWhiteSpace(chapter.HRef)));
        Assert.Equal(chapters.Count, chapters.Select(chapter => chapter.OrderIndex).Distinct().Count());
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task Wardley_ExtractChapterContentAsync_ReturnsRenderableHtmlForEveryChapter()
    {
        var chapters = await _sut.ExtractChaptersAsync(WardleyEpub);

        foreach (var chapter in chapters)
        {
            var html = await _sut.ExtractChapterContentAsync(WardleyEpub, chapter.HRef, ImagesDir);

            Assert.False(string.IsNullOrWhiteSpace(html), $"Capitulo vazio: {chapter.HRef}");
            Assert.DoesNotContain("src=\"../", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task Wardley_ExtractAllImagesAsync_StreamsExactlyTheImagesInsideTheArchive()
    {
        var expected = ImageEntryCount(WardleyEpub);

        var count = 0;
        await foreach (var _ in _sut.ExtractAllImagesAsync(WardleyEpub))
            count++;

        Assert.True(expected >= 200, $"Oraculo do zip esperava >= 200 imagens, achou {expected}.");
        Assert.Equal(expected, count);
    }

    [Fact]
    [Trait("Category", "Slow")]
    public async Task Wardley_ExtractAllImagesAsync_YieldsNonEmptyBytesUnderUniquePaths()
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var image in _sut.ExtractAllImagesAsync(WardleyEpub))
        {
            Assert.True(image.Content.Length > 0, $"Imagem vazia: {image.RelativePath}");
            Assert.True(paths.Add(image.RelativePath), $"Caminho repetido: {image.RelativePath}");
        }

        Assert.NotEmpty(paths);
    }

    // ── Independent oracle: the raw archive, never the engine ───────────────

    private static int ImageEntryCount(string epubPath)
    {
        using var archive = ZipFile.OpenRead(epubPath);
        return archive.Entries.Count(entry => ImageExtensions.Contains(Path.GetExtension(entry.FullName)));
    }

    private static int SpineItemCount(string epubPath)
    {
        using var archive = ZipFile.OpenRead(epubPath);
        var opf = archive.Entries.First(entry =>
            entry.FullName.EndsWith(".opf", StringComparison.OrdinalIgnoreCase));

        using var stream = opf.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });

        var count = 0;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element
                && string.Equals(reader.LocalName, "itemref", StringComparison.Ordinal))
                count++;
        }

        return count;
    }
}
