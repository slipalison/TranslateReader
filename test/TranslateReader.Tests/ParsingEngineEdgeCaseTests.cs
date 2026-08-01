using System.IO.Compression;
using System.Reflection;
using System.Text;
using TranslateReader.Business.Engines;
using TranslateReader.Models;

namespace TranslateReader.Tests;

// Edge cases of ParsingEngine that the three real fixtures never reach: malformed packages,
// missing metadata, link/img references that must be left alone, and the manifest cover fallback.
// Pure path helpers are reached by reflection (zero I/O, same pattern as ParsingEngineRegexTests).
// The EPUBs are synthesised into a temp directory because every public entry point takes a file
// path — named exception to .claude/rules/csharp.md section 6, per D-2026-07-31-coverage-90-3 and
// the precedent already set by ParsingEngineTests. No binary fixture is committed.
public class ParsingEngineEdgeCaseTests : IDisposable
{
    private const string ContainerXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
          <rootfiles>
            <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
          </rootfiles>
        </container>
        """;

    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "translatereader_parsing_" + Guid.NewGuid().ToString("N"));

    private readonly ParsingEngine _sut = new();

    public ParsingEngineEdgeCaseTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        DeleteFixtures();
        GC.SuppressFinalize(this);
    }

    private void DeleteFixtures()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // VersOne.Epub keeps the archive handle open when the strict read throws
            // EpubPackageException, so the fallback fixture can still be locked here.
            // A leftover under %TEMP% must not fail the suite.
        }
    }

    // ── ReadEpubSafeAsync fallback ──────────────────────────────────────────

    [Fact]
    public async Task ExtractMetadataAsync_WithAPackageMissingItsMetadataNode_UsesTheLenientFallback()
    {
        var path = CreateEpub("no-metadata.epub", """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="bookid">
              <manifest>
                <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
              </manifest>
              <spine>
                <itemref idref="ch1"/>
              </spine>
            </package>
            """, archive => AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>"));

        var book = await _sut.ExtractMetadataAsync(path);

        Assert.Equal(path, book.FilePath);
        Assert.Equal(1, book.TotalChapters);
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithoutPublisherOrLanguage_FallsBackToEmptyStrings()
    {
        var path = CreateEpub("bare-metadata.epub", Package("""
              <dc:identifier id="bookid">urn:uuid:bare</dc:identifier>
              <dc:title>So um titulo</dc:title>
            """, """
              <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
            """, """
              <itemref idref="ch1"/>
            """), archive => AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>"));

        var book = await _sut.ExtractMetadataAsync(path);

        Assert.Equal("So um titulo", book.Title);
        Assert.Equal(string.Empty, book.Author);
        Assert.Equal(string.Empty, book.Publisher);
        Assert.Equal(string.Empty, book.Language);
    }

    // ── ExtractChaptersAsync / ExtractChapterContentAsync ───────────────────

    [Fact]
    public async Task ExtractChaptersAsync_WithoutNavigation_NamesChaptersAfterTheirFile()
    {
        var path = CreateRichEpub();

        var chapters = await _sut.ExtractChaptersAsync(path);

        Assert.Equal("chapter1", chapters[0].Title);
        Assert.Equal(0, chapters[0].OrderIndex);
    }

    [Fact]
    public async Task ExtractChapterContentAsync_WithAnUnknownHref_Throws()
    {
        var path = CreateRichEpub();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExtractChapterContentAsync(path, "text/nope.xhtml", _tempDir, ChapterContentPurpose.Display));
    }

    [Fact]
    public async Task ExtractChapterContentAsync_WithAnEmptyChapterFile_Throws()
    {
        var path = CreateRichEpub();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExtractChapterContentAsync(path, "OEBPS/text/empty.xhtml", _tempDir, ChapterContentPurpose.Display));

        Assert.Contains("no content", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractChapterContentAsync_InlinesOnlyTheStylesheetLinkItCanResolve()
    {
        var path = CreateRichEpub();

        var html = await _sut.ExtractChapterContentAsync(path, "OEBPS/text/chapter1.xhtml", _tempDir, ChapterContentPurpose.Display);

        Assert.Contains("<style type=\"text/css\">body { color: red; }</style>", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"icon\"", html, StringComparison.Ordinal);
        Assert.Contains("../styles/missing.css", html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"stylesheet\"/>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractChapterContentAsync_RewritesOnlyTheImagesItCanResolve()
    {
        var path = CreateRichEpub();

        var html = await _sut.ExtractChapterContentAsync(path, "OEBPS/text/chapter1.xhtml", _tempDir, ChapterContentPurpose.Display);

        var bookDir = Path.GetFileName(_tempDir);
        Assert.Contains($"https://epub-images/{bookDir}/OEBPS/images/pic.png", html, StringComparison.Ordinal);
        Assert.Contains("src=\"../images/missing.png\"", html, StringComparison.Ordinal);
        Assert.Contains("src=\"data:image/png;base64,iVBORw0KGgo=\"", html, StringComparison.Ordinal);
        Assert.Contains("src=\"http://example.invalid/remote.png\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractChapterContentAsync_RewritesSvgImageReferences()
    {
        var path = CreateRichEpub();

        var html = await _sut.ExtractChapterContentAsync(path, "OEBPS/text/svg.xhtml", _tempDir, ChapterContentPurpose.Display);

        var bookDir = Path.GetFileName(_tempDir);
        Assert.Equal(2, CountOccurrences(html, $"https://epub-images/{bookDir}/OEBPS/images/pic.png"));
    }

    // ── ExtractAllImagesAsync (lazy stream over EpubBookRef) ────────────────
    // Same named exception to .claude/rules/csharp.md section 6 declared at the top of this file
    // (D-2026-07-31-coverage-90-3 + the ParsingEngineTests precedent): every public entry point
    // takes a file path, so a synthesised EPUB under %TEMP% is the only reachable seam. The three
    // real fixtures never enter the lenient fallback, so only a synthetic package can cover it.

    [Fact]
    public async Task ExtractAllImagesAsync_StreamsEachLocalImageWithItsRelativePath()
    {
        var path = CreateRichEpub();

        var images = new List<ExtractedImage>();
        await foreach (var image in _sut.ExtractAllImagesAsync(path))
            images.Add(image);

        var only = Assert.Single(images);
        Assert.Equal("OEBPS/images/pic.png", only.RelativePath);
        Assert.Equal(PngBytes, only.Content);
    }

    [Fact]
    public async Task ExtractAllImagesAsync_WithAPackageMissingItsMetadataNode_UsesTheLenientFallback()
    {
        var path = CreateEpub("no-metadata-images.epub", """
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="bookid">
              <manifest>
                <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
                <item id="pic" href="images/pic.png" media-type="image/png"/>
              </manifest>
              <spine>
                <itemref idref="ch1"/>
              </spine>
            </package>
            """, archive =>
        {
            AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>");
            AddBytes(archive, "OEBPS/images/pic.png", PngBytes);
        });

        var images = new List<ExtractedImage>();
        await foreach (var image in _sut.ExtractAllImagesAsync(path))
            images.Add(image);

        var only = Assert.Single(images);
        Assert.Equal("OEBPS/images/pic.png", only.RelativePath);
        Assert.Equal(PngBytes, only.Content);
    }

    [Fact]
    public async Task ExtractAllImagesAsync_WithoutAnyImage_YieldsNothing()
    {
        var path = CreateEpub("text-only.epub", Package("""
              <dc:identifier id="bookid">urn:uuid:text-only</dc:identifier>
              <dc:title>So texto</dc:title>
            """, """
              <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
            """, """
              <itemref idref="ch1"/>
            """), archive => AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>"));

        var count = 0;
        await foreach (var _ in _sut.ExtractAllImagesAsync(path))
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ExtractAllImagesAsync_WhenTheConsumerBreaksEarly_ReleasesTheArchiveHandle()
    {
        var path = CreateTwoImageEpub();

        var seen = 0;
        await foreach (var image in _sut.ExtractAllImagesAsync(path))
        {
            seen++;
            Assert.NotEmpty(image.Content);
            break;
        }

        Assert.Equal(1, seen);

        // Windows refuses to delete a file the zip handle still holds, so this only passes when
        // the iterator's using disposed the EpubBookRef on the consumer's early break.
        File.Delete(path);
        Assert.False(File.Exists(path));
    }

    // ── ExtractCoverImageAsync fallbacks ────────────────────────────────────

    [Fact]
    public async Task ExtractCoverImageAsync_WithAnEmptyDeclaredCover_ReturnsNullInsteadOfEmptyBytes()
    {
        var path = CreateCoverEpub(
            "empty-cover.epub",
            metadataExtra: "<meta name=\"cover\" content=\"blank\"/>",
            coverItem: "<item id=\"blank\" href=\"images/blank.png\" media-type=\"image/png\"/>",
            imageHref: "OEBPS/images/blank.png",
            imageBytes: []);

        Assert.Null(await _sut.ExtractCoverImageAsync(path));
    }

    [Fact]
    public async Task ExtractCoverImageAsync_WithACoverImagePropertyPointingAtAMissingFile_ReturnsNoBytes()
    {
        var cover = await _sut.ExtractCoverImageAsync(CreateOrphanCoverEpub());
        Assert.Null(cover);
    }

    [Fact]
    public async Task ExtractCoverImageAsync_FallsBackToTheManifestItemIdentifiedAsCover()
    {
        var path = CreateCoverEpub(
            "id-cover.epub",
            metadataExtra: string.Empty,
            coverItem: "<item id=\"cover\" href=\"images/pic.png\" media-type=\"image/png\"/>",
            imageHref: "OEBPS/images/pic.png",
            imageBytes: PngBytes);

        Assert.Equal(PngBytes, await _sut.ExtractCoverImageAsync(path));
    }

    [Fact]
    public async Task ExtractCoverImageAsync_FallsBackToAManifestImageNamedLikeACover()
    {
        var path = CreateCoverEpub(
            "href-cover.epub",
            metadataExtra: string.Empty,
            coverItem: "<item id=\"img1\" href=\"images/the-cover.png\" media-type=\"image/png\"/>",
            imageHref: "OEBPS/images/the-cover.png",
            imageBytes: PngBytes);

        Assert.Equal(PngBytes, await _sut.ExtractCoverImageAsync(path));
    }

    [Fact]
    public async Task ExtractCoverImageAsync_IgnoresNonImageManifestItemsNamedLikeACover()
    {
        var path = CreateCoverEpub(
            "text-cover.epub",
            metadataExtra: string.Empty,
            coverItem: "<item id=\"cover\" href=\"text/cover-page.xhtml\" media-type=\"application/xhtml+xml\"/>",
            imageHref: "OEBPS/text/cover-page.xhtml",
            imageBytes: PngBytes);

        Assert.Null(await _sut.ExtractCoverImageAsync(path));
    }

    [Fact]
    public async Task ExtractCoverImageAsync_WhenTheCoverPropertyIsNotOnAnImage_ReturnsNull()
    {
        var path = CreateEpub("prop-on-text.epub", Package("""
              <dc:identifier id="bookid">urn:uuid:prop</dc:identifier>
              <dc:title>Capa</dc:title>
            """, """
              <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
              <item id="c" href="text/cover-page.xhtml" media-type="application/xhtml+xml" properties="cover-image"/>
              <item id="pic" href="images/pic.png" media-type="image/png"/>
            """, """
              <itemref idref="ch1"/>
            """), archive =>
        {
            AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>");
            AddText(archive, "OEBPS/text/cover-page.xhtml", "<html><body><p>capa</p></body></html>");
            AddBytes(archive, "OEBPS/images/pic.png", PngBytes);
        });

        Assert.Null(await _sut.ExtractCoverImageAsync(path));
    }

    [Fact]
    public async Task ExtractCoverImageAsync_WithNoCoverCandidateAtAll_ReturnsNull()
    {
        var path = CreateCoverEpub(
            "no-cover.epub",
            metadataExtra: string.Empty,
            coverItem: "<item id=\"img1\" href=\"images/pic.png\" media-type=\"image/png\"/>",
            imageHref: "OEBPS/images/pic.png",
            imageBytes: PngBytes);

        Assert.Null(await _sut.ExtractCoverImageAsync(path));
    }

    // ── CreateTranslatedEpubAsync ───────────────────────────────────────────

    [Fact]
    public async Task CreateTranslatedEpubAsync_WithAnArchiveWithoutAnOpf_StillProducesTheCopy()
    {
        var source = Path.Combine(_tempDir, "no-opf.epub");
        using (var stream = new FileStream(source, FileMode.Create))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            AddText(archive, "text/chapter1.xhtml", "<html><body><p>original</p></body></html>");
        }

        var destination = Path.Combine(_tempDir, "out");
        var result = await _sut.CreateTranslatedEpubAsync(
            source, "Titulo", new Dictionary<string, string> { ["text/chapter1.xhtml"] = "<p>novo</p>" }, destination);

        Assert.True(File.Exists(result));
        using var written = ZipFile.OpenRead(result);
        Assert.DoesNotContain(written.Entries, e => e.FullName.EndsWith(".opf", StringComparison.OrdinalIgnoreCase));
    }

    // ── Private path helpers ────────────────────────────────────────────────

    [Theory]
    [InlineData("a/b/../c", "a/c")]
    [InlineData("../a", "a")]
    [InlineData("../../a/b", "a/b")]
    [InlineData("a/./b", "a/b")]
    [InlineData("a//b", "a/b")]
    [InlineData("a\\b\\c", "a/b/c")]
    [InlineData("", "")]
    public void NormalizePath_CollapsesRelativeSegments(string input, string expected)
    {
        Assert.Equal(expected, Invoke<string>("NormalizePath", input));
    }

    [Theory]
    [InlineData("OEBPS/text/ch.xhtml", "OEBPS/text")]
    [InlineData("ch.xhtml", "")]
    [InlineData("OEBPS\\text\\ch.xhtml", "OEBPS/text")]
    public void GetDirectoryPath_ReturnsEverythingBeforeTheLastSeparator(string input, string expected)
    {
        Assert.Equal(expected, Invoke<string>("GetDirectoryPath", input));
    }

    [Theory]
    [InlineData("OEBPS/text", "../images/a.png", "OEBPS/images/a.png")]
    [InlineData("", "images/a.png", "images/a.png")]
    public void ResolvePath_CombinesTheChapterDirectoryWithTheReference(
        string chapterDir, string source, string expected)
    {
        Assert.Equal(expected, Invoke<string>("ResolvePath", chapterDir, source));
    }

    // ── fixtures ────────────────────────────────────────────────────────────

    private static T Invoke<T>(string name, params object[] arguments)
    {
        var method = typeof(ParsingEngine).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ParsingEngine.{name}() does not exist.");

        return (T)method.Invoke(null, arguments)!;
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = value.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = value.IndexOf(token, index + token.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string Package(string metadata, string manifest, string spine) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="bookid">
          <metadata xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:opf="http://www.idpf.org/2007/opf">
        {metadata}
          </metadata>
          <manifest>
        {manifest}
          </manifest>
          <spine>
        {spine}
          </spine>
        </package>
        """;

    private string CreateEpub(string name, string packageXml, Action<ZipArchive> addContent)
    {
        var path = Path.Combine(_tempDir, name);
        using var stream = new FileStream(path, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        AddText(archive, "mimetype", "application/epub+zip");
        AddText(archive, "META-INF/container.xml", ContainerXml);
        AddText(archive, "OEBPS/content.opf", packageXml);
        addContent(archive);
        return path;
    }

    private string CreateRichEpub() => CreateEpub("rich.epub", Package("""
          <dc:identifier id="bookid">urn:uuid:rich</dc:identifier>
          <dc:title>Rico</dc:title>
          <dc:creator>Autora</dc:creator>
          <dc:publisher>Editora</dc:publisher>
          <dc:language>pt-BR</dc:language>
        """, """
          <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
          <item id="svg" href="text/svg.xhtml" media-type="application/xhtml+xml"/>
          <item id="empty" href="text/empty.xhtml" media-type="application/xhtml+xml"/>
          <item id="css" href="styles/main.css" media-type="text/css"/>
          <item id="pic" href="images/pic.png" media-type="image/png"/>
        """, """
          <itemref idref="ch1"/>
          <itemref idref="svg"/>
          <itemref idref="empty"/>
        """), archive =>
    {
        AddText(archive, "OEBPS/styles/main.css", "body { color: red; }");
        AddBytes(archive, "OEBPS/images/pic.png", PngBytes);
        AddText(archive, "OEBPS/text/chapter1.xhtml", """
            <html xmlns="http://www.w3.org/1999/xhtml">
            <head>
            <link rel="stylesheet" href="../styles/main.css"/>
            <link rel="icon" href="../favicon.ico"/>
            <link rel="stylesheet"/>
            <link rel="stylesheet" href="../styles/missing.css"/>
            </head>
            <body>
            <img src="../images/pic.png"/>
            <img src="../images/missing.png"/>
            <img src="data:image/png;base64,iVBORw0KGgo="/>
            <img src="http://example.invalid/remote.png"/>
            </body>
            </html>
            """);
        AddText(archive, "OEBPS/text/svg.xhtml", """
            <html xmlns="http://www.w3.org/1999/xhtml"><body>
            <svg><image xlink:href="../images/pic.png"/></svg>
            <svg><image href="../images/pic.png"/></svg>
            </body></html>
            """);
        AddBytes(archive, "OEBPS/text/empty.xhtml", []);
    });

    // A cover-image property whose target is absent from the archive: the manifest fallback used to
    // hand back an empty byte[] instead of null.
    private string CreateOrphanCoverEpub() => CreateCoverEpub(
        "orphan-cover.epub",
        metadataExtra: string.Empty,
        coverItem: "<item id=\"c\" href=\"images/gone.png\" media-type=\"image/png\" properties=\"cover-image\"/>",
        imageHref: null,
        imageBytes: null);

    private string CreateTwoImageEpub() => CreateEpub("two-images.epub", Package("""
          <dc:identifier id="bookid">urn:uuid:two-images</dc:identifier>
          <dc:title>Duas imagens</dc:title>
        """, """
          <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
          <item id="pic1" href="images/pic1.png" media-type="image/png"/>
          <item id="pic2" href="images/pic2.png" media-type="image/png"/>
        """, """
          <itemref idref="ch1"/>
        """), archive =>
    {
        AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>");
        AddBytes(archive, "OEBPS/images/pic1.png", PngBytes);
        AddBytes(archive, "OEBPS/images/pic2.png", PngBytes);
    });

    private string CreateCoverEpub(
        string name, string metadataExtra, string coverItem, string? imageHref, byte[]? imageBytes) =>
        CreateEpub(name, Package($"""
              <dc:identifier id="bookid">urn:uuid:{name}</dc:identifier>
              <dc:title>Capa</dc:title>
              {metadataExtra}
            """, $"""
              <item id="ch1" href="text/chapter1.xhtml" media-type="application/xhtml+xml"/>
              {coverItem}
            """, """
              <itemref idref="ch1"/>
            """), archive =>
        {
            AddText(archive, "OEBPS/text/chapter1.xhtml", "<html><body><p>a</p></body></html>");
            if (imageHref is not null)
                AddBytes(archive, imageHref, imageBytes!);
        });

    private static void AddText(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static void AddBytes(ZipArchive archive, string name, byte[] content)
    {
        using var stream = archive.CreateEntry(name).Open();
        stream.Write(content);
    }
}
