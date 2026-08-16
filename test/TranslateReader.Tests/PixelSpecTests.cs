namespace TranslateReader.Tests;

/// <summary>
/// Structural tests for the pixel-perfect phase. Same disk-read pattern as
/// <see cref="DesignSystemTests"/> - the test project targets plain net10.0 and cannot
/// reference the MAUI client project, so these tests read the XAML/C# source files from disk
/// instead of instantiating the pages.
/// </summary>
public class PixelSpecTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string AppRoot = Path.Combine(RepoRoot, "src", "TranslateReader");
    private static readonly string PagesRoot = Path.Combine(AppRoot, "Pages");
    private static readonly string ControlsRoot = Path.Combine(PagesRoot, "Controls");
    private static readonly string PageModelsRoot = Path.Combine(AppRoot, "PageModels");
    private static readonly string FontsRoot = Path.Combine(AppRoot, "Resources", "Fonts");

    private static readonly string DesignTokensPath = Path.Combine(AppRoot, "Resources", "Styles", "DesignTokens.xaml");
    private static readonly string MauiProgramPath = Path.Combine(AppRoot, "MauiProgram.cs");
    private static readonly string LibraryPageXamlPath = Path.Combine(PagesRoot, "LibraryPage.xaml");
    private static readonly string LibraryPageCodeBehindPath = Path.Combine(PagesRoot, "LibraryPage.xaml.cs");
    private static readonly string ReaderPageXamlPath = Path.Combine(PagesRoot, "ReaderPage.xaml");
    private static readonly string SettingsOverlayXamlPath = Path.Combine(ControlsRoot, "SettingsOverlay.xaml");
    private static readonly string TranslateBookPopupXamlPath = Path.Combine(ControlsRoot, "TranslateBookPopup.xaml");
    private static readonly string LibraryPageModelPath = Path.Combine(PageModelsRoot, "LibraryPageModel.cs");
    private static readonly string ReaderPageModelPath = Path.Combine(PageModelsRoot, "ReaderPageModel.cs");

    private static readonly string[] PixelSpecTokenKeys =
    [
        "ColorDanger", "AccentTint10", "AccentTint08", "OverlayScrim", "CoverScrim",
        "TextMuted70", "TextMuted55", "TextMuted40", "ProgressTrackOnCover"
    ];

    private static readonly string[] FontAliases = ["InterRegular", "InterMedium", "Phosphor", "PhosphorFill"];
    private static readonly string[] FontFiles = ["Inter-Regular.ttf", "Inter-Medium.ttf", "Phosphor.ttf", "Phosphor-Fill.ttf"];

    private static readonly string[] AsciiArtGlyphs = ["☀", "☾", "☕", "✕"];

    private static readonly string[] LibraryPagePixelSpecNames =
        ["BooksListCollection", "GridToggleButton", "ListToggleButton"];

    private static readonly string[] ModelRowNames =
        ["GemmaModelButton", "QwenModelButton", "PhiModelButton", "HyMtModelButton", "HyMt2ModelButton"];

    [Fact]
    public void DesignTokens_ExposeThePixelSpecExtensions()
    {
        var tokens = File.ReadAllText(DesignTokensPath);
        Assert.True(tokens.Length > 0, "DesignTokens.xaml is empty - cannot verify pixel-spec tokens.");

        foreach (var key in PixelSpecTokenKeys)
            Assert.Contains($"x:Key=\"{key}\"", tokens, StringComparison.Ordinal);

        Assert.Contains("#E08A8A", tokens, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fonts_InterAndPhosphorAreRegistered()
    {
        foreach (var fontFile in FontFiles)
        {
            var path = Path.Combine(FontsRoot, fontFile);
            Assert.True(File.Exists(path), $"Font file '{fontFile}' does not exist at {path}.");
            Assert.True(new FileInfo(path).Length > 50_000, $"Font file '{fontFile}' is smaller than 50KB - looks like a placeholder.");
        }

        var mauiProgram = File.ReadAllText(MauiProgramPath);
        foreach (var alias in FontAliases)
            Assert.Contains($"\"{alias}\"", mauiProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Chrome_UsesNoLegacyDangerRed()
    {
        var searchRoots = new[] { PagesRoot, PageModelsRoot };
        foreach (var root in searchRoots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories)
                         .Concat(Directory.EnumerateFiles(root, "*.xaml.cs", SearchOption.AllDirectories))
                         .Concat(Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)))
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("#E53E3E", content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void LibraryPage_HasTheListViewAndToggle()
    {
        var xaml = File.ReadAllText(LibraryPageXamlPath);
        foreach (var name in LibraryPagePixelSpecNames)
            Assert.Contains($"x:Name=\"{name}\"", xaml, StringComparison.Ordinal);

        var pageModel = File.ReadAllText(LibraryPageModelPath);
        Assert.Contains("IsListView", pageModel, StringComparison.Ordinal);
        Assert.Contains("ShowGridView", pageModel, StringComparison.Ordinal);
        Assert.Contains("ShowListView", pageModel, StringComparison.Ordinal);
    }

    [Fact]
    public void LibraryPage_ImportButtonLivesInTheTopBar()
    {
        var xaml = File.ReadAllText(LibraryPageXamlPath);
        Assert.Contains("x:Name=\"ImportButton\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ToolbarItem", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void LibraryPage_GridSpanIsAdaptive()
    {
        var codeBehind = File.ReadAllText(LibraryPageCodeBehindPath);
        Assert.Contains("SizeChanged", codeBehind, StringComparison.Ordinal);
        Assert.Contains("187", codeBehind, StringComparison.Ordinal);
        Assert.Contains("Span", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void ReaderPage_HasChapterSubtitleAndStyledFooter()
    {
        var xaml = File.ReadAllText(ReaderPageXamlPath);
        Assert.Contains("x:Name=\"ChapterSubtitleLabel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ReaderFooter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PageProgressBar\"", xaml, StringComparison.Ordinal);

        var pageModel = File.ReadAllText(ReaderPageModelPath);
        Assert.Contains("ChapterSubtitle", pageModel, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsOverlay_ModelsAreAVerticalRadioList()
    {
        var xaml = File.ReadAllText(SettingsOverlayXamlPath);
        Assert.Contains("x:Name=\"ModelsList\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Orientation=\"Horizontal\"", xaml, StringComparison.Ordinal);

        foreach (var name in ModelRowNames)
            Assert.Contains($"x:Name=\"{name}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsOverlay_UsesPhosphorGlyphsNotAsciiArt()
    {
        var xaml = File.ReadAllText(SettingsOverlayXamlPath);
        Assert.Contains("FontFamily=\"Phosphor", xaml, StringComparison.Ordinal);

        foreach (var glyph in AsciiArtGlyphs)
            Assert.DoesNotContain(glyph, xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TranslateBookPopup_BannerFollowsThePickers()
    {
        var lines = File.ReadAllLines(TranslateBookPopupXamlPath);
        var bannerLine = Array.FindIndex(lines, l => l.Contains("x:Name=\"OfflineBanner\"", StringComparison.Ordinal));
        var sourcePickerLine = Array.FindIndex(lines, l => l.Contains("x:Name=\"SourcePicker\"", StringComparison.Ordinal));

        Assert.True(bannerLine >= 0, "OfflineBanner not found in TranslateBookPopup.xaml.");
        Assert.True(sourcePickerLine >= 0, "SourcePicker not found in TranslateBookPopup.xaml.");
        Assert.True(bannerLine > sourcePickerLine, "OfflineBanner must appear after SourcePicker in the XAML (D-...-8 banner order).");
    }
}
