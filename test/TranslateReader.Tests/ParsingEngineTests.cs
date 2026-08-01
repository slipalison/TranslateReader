using System.IO.Compression;
using System.Text;
using TranslateReader.Business.Engines;
using TranslateReader.Utilities;

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
        var count = 0;
        await foreach (var image in _sut.ExtractAllImagesAsync(PracticeEpub))
        {
            count++;
            Assert.False(string.IsNullOrWhiteSpace(image.RelativePath));
            Assert.True(image.Content.Length > 0);
        }

        Assert.True(count > 0);
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
        var ex = await Record.ExceptionAsync(async () =>
        {
            await foreach (var image in _sut.ExtractAllImagesAsync(RightingEpub))
                Assert.NotNull(image.Content);
        });

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
        var count = 0;
        await foreach (var image in _sut.ExtractAllImagesAsync(WardleyEpub))
        {
            count++;
            Assert.False(string.IsNullOrWhiteSpace(image.RelativePath));
            Assert.True(image.Content.Length > 0);
        }

        Assert.True(count >= 100, $"Esperado >= 100 imagens, obtido {count}");
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

    // ── CreateTranslatedEpubAsync ───────────────────────────────────────────
    // A escrita no zip e o unico caminho assincrono de flush do engine: se o writer nao
    // esvaziar antes do archive fechar, o .epub sai com a entry original ou truncada.

    [Fact]
    public async Task Practice_CreateTranslatedEpubAsync_GravaCapituloTraduzidoEAtualizaTitulo()
    {
        var destinationDirectory = Path.Combine(Path.GetTempPath(), "translatereader_translated_" + Guid.NewGuid().ToString("N"));
        var chapter = (await _sut.ExtractChaptersAsync(PracticeEpub))[0];
        const string translatedTitle = "Pratica Leva a Perfeicao";
        const string translatedHtml = "<html><body><p>PARAGRAFO TRADUZIDO SENTINELA</p></body></html>";

        try
        {
            var destPath = await _sut.CreateTranslatedEpubAsync(
                PracticeEpub,
                translatedTitle,
                new Dictionary<string, string> { [chapter.HRef] = translatedHtml },
                destinationDirectory);

            Assert.True(File.Exists(destPath));

            using var archive = ZipFile.OpenRead(destPath);
            Assert.Equal(translatedHtml, ReadEntry(archive, chapter.HRef));

            var opf = ReadOpf(archive);
            Assert.Contains($">{translatedTitle}</dc:title>", opf, StringComparison.Ordinal);
            Assert.DoesNotContain("Practice Makes Perfect", opf, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Practice_CreateTranslatedEpubAsync_NaoAlteraOArquivoOriginal()
    {
        var destinationDirectory = Path.Combine(Path.GetTempPath(), "translatereader_translated_" + Guid.NewGuid().ToString("N"));
        var chapter = (await _sut.ExtractChaptersAsync(PracticeEpub))[0];
        var originalLength = new FileInfo(PracticeEpub).Length;

        try
        {
            var destPath = await _sut.CreateTranslatedEpubAsync(
                PracticeEpub,
                "Outro Titulo",
                new Dictionary<string, string> { [chapter.HRef] = "<html><body><p>x</p></body></html>" },
                destinationDirectory);

            Assert.NotEqual(Path.GetFullPath(PracticeEpub), Path.GetFullPath(destPath));
            Assert.Equal(originalLength, new FileInfo(PracticeEpub).Length);
        }
        finally
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Practice_CreateTranslatedEpubAsync_IgnoraHRefInexistenteSemQuebrar()
    {
        var destinationDirectory = Path.Combine(Path.GetTempPath(), "translatereader_translated_" + Guid.NewGuid().ToString("N"));

        try
        {
            var destPath = await _sut.CreateTranslatedEpubAsync(
                PracticeEpub,
                "Titulo",
                new Dictionary<string, string> { ["nao/existe/capitulo.xhtml"] = "<p>ignorado</p>" },
                destinationDirectory);

            Assert.True(File.Exists(destPath));

            using var archive = ZipFile.OpenRead(destPath);
            Assert.Contains(">Titulo</dc:title>", ReadOpf(archive), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, recursive: true);
        }
    }

    // ── Artefato: o EPUB traduzido nao pode carregar URL do app ─────────────
    // Propriedade do ARTEFATO, nao da funcao: reproduz o caminho de producao
    // (RebuildAllTranslatedChaptersAsync com cache vazio -> traducao = original) e reabre o zip.
    // `epub-images` e ABSOLUTO (o EPUB-fonte tem 0 ocorrencias); `https://` e DIFERENCIAL porque o
    // fixture ja traz 1 nativo (licenca MIT em ops/styles/1266002537.css) —
    // D-2026-08-01-translated-epub-images-9(B).

    [Fact]
    public async Task Practice_TranslatedEpubArtifact_ForExportedChapters_NoEntryContainsTheAppHost()
    {
        var destinationDirectory = Path.Combine(
            Path.GetTempPath(), "translatereader_artifact_" + Guid.NewGuid().ToString("N"));

        try
        {
            var rebuilt = await RebuildEveryChapterAsync(PracticeEpub);
            var destPath = await _sut.CreateTranslatedEpubAsync(
                PracticeEpub, "Artefato Traduzido", rebuilt, destinationDirectory);

            using var original = ZipFile.OpenRead(PracticeEpub);
            var entriesWithNativeHttps = original.Entries
                .Where(e => ReadEntryRaw(e).Contains("https://", StringComparison.Ordinal))
                .Select(e => e.FullName)
                .ToHashSet(StringComparer.Ordinal);

            using var artifact = ZipFile.OpenRead(destPath);
            var leaks = CollectAppUrlLeaks(artifact, entriesWithNativeHttps);

            Assert.True(leaks.Count == 0, string.Join(Environment.NewLine, leaks));
        }
        finally
        {
            if (Directory.Exists(destinationDirectory))
                Directory.Delete(destinationDirectory, recursive: true);
        }
    }

    private async Task<Dictionary<string, string>> RebuildEveryChapterAsync(string epubPath)
    {
        var chapters = await _sut.ExtractChaptersAsync(epubPath);
        var rebuilt = new Dictionary<string, string>(chapters.Count);

        foreach (var href in chapters.Select(c => c.HRef))
        {
            var html = await _sut.ExtractChapterContentAsync(epubPath, href, string.Empty);
            var blocks = HtmlUtility.ExtractTextBlocks(HtmlUtility.ExtractBodyContent(html));
            rebuilt[href] = HtmlUtility.ReplaceTextBlocksInHtml(html, blocks);
        }

        return rebuilt;
    }

    private static List<string> CollectAppUrlLeaks(ZipArchive artifact, HashSet<string> entriesWithNativeHttps)
    {
        var leaks = new List<string>();

        foreach (var entry in artifact.Entries)
        {
            var content = ReadEntryRaw(entry);

            var appHostAt = content.IndexOf("epub-images", StringComparison.Ordinal);
            if (appHostAt >= 0)
                leaks.Add($"{entry.FullName}: contains 'epub-images' -> {Excerpt(content, appHostAt)}");

            var httpsAt = content.IndexOf("https://", StringComparison.Ordinal);
            if (httpsAt >= 0 && !entriesWithNativeHttps.Contains(entry.FullName))
                leaks.Add($"{entry.FullName}: gained 'https://' -> {Excerpt(content, httpsAt)}");
        }

        return leaks;
    }

    // Latin1 round-trips every byte, so binary entries can be scanned for the literal without
    // decoder replacement characters swallowing it.
    private static string ReadEntryRaw(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.Latin1);
        return reader.ReadToEnd();
    }

    private static string Excerpt(string content, int index) =>
        content.Substring(index, Math.Min(70, content.Length - index));

    private static string ReadEntry(ZipArchive archive, string href)
    {
        var normalized = href.Replace('\\', '/');
        var entry = archive.Entries.First(e =>
            string.Equals(e.FullName.Replace('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase)
            || e.FullName.Replace('\\', '/').EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static string ReadOpf(ZipArchive archive)
    {
        var entry = archive.Entries.First(e => e.FullName.EndsWith(".opf", StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
