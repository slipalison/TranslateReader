using System.Text.RegularExpressions;
using TranslateReader.Models;

namespace TranslateReader.Tests;

/// <summary>
/// Structural tests for the app-redesign phase. The test project targets plain net10.0 and only
/// references TranslateReader.Core, so it cannot reference the MAUI client project (Pages,
/// PageModels, DesignTokens.xaml). These tests read the XAML/C# source files from disk instead —
/// same pattern as <see cref="HybridWebViewContractTests"/> — to prove the app wiring (tokens,
/// event handlers, bindings, structure) is intact without a UI test harness.
/// </summary>
public class DesignSystemTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static readonly string AppRoot = Path.Combine(RepoRoot, "src", "TranslateReader");
    private static readonly string PagesRoot = Path.Combine(AppRoot, "Pages");
    private static readonly string ControlsRoot = Path.Combine(PagesRoot, "Controls");
    private static readonly string PageModelsRoot = Path.Combine(AppRoot, "PageModels");

    private static readonly string DesignTokensPath = Path.Combine(AppRoot, "Resources", "Styles", "DesignTokens.xaml");
    private static readonly string LibraryPageXamlPath = Path.Combine(PagesRoot, "LibraryPage.xaml");
    private static readonly string LibraryPageCodeBehindPath = Path.Combine(PagesRoot, "LibraryPage.xaml.cs");
    private static readonly string ReaderPageXamlPath = Path.Combine(PagesRoot, "ReaderPage.xaml");
    private static readonly string ReaderPageCodeBehindPath = Path.Combine(PagesRoot, "ReaderPage.xaml.cs");
    private static readonly string SettingsOverlayXamlPath = Path.Combine(ControlsRoot, "SettingsOverlay.xaml");
    private static readonly string SettingsOverlayCodeBehindPath = Path.Combine(ControlsRoot, "SettingsOverlay.xaml.cs");
    private static readonly string TranslateBookPopupXamlPath = Path.Combine(ControlsRoot, "TranslateBookPopup.xaml");
    private static readonly string TranslateBookPopupCodeBehindPath = Path.Combine(ControlsRoot, "TranslateBookPopup.xaml.cs");
    private static readonly string AppShellXamlPath = Path.Combine(AppRoot, "AppShell.xaml");
    private static readonly string AppShellCodeBehindPath = Path.Combine(AppRoot, "AppShell.xaml.cs");
    private static readonly string LibraryPageModelPath = Path.Combine(PageModelsRoot, "LibraryPageModel.cs");
    private static readonly string ReaderPageModelPath = Path.Combine(PageModelsRoot, "ReaderPageModel.cs");

    private static readonly string[] RedesignedXamlPaths =
    [
        LibraryPageXamlPath,
        ReaderPageXamlPath,
        SettingsOverlayXamlPath,
        TranslateBookPopupXamlPath,
        AppShellXamlPath
    ];

    // Old chrome palette, pre-redesign (D-...-3). Must not survive anywhere in the restyled pages.
    // #1A1A1A is intentionally excluded from this denylist: SettingsOverlay.xaml's
    // LightThemeButton pairs BackgroundColor="#FFFFFF" with TextColor="#1A1A1A" as a preview
    // swatch of the Light reading theme - the same content pattern already carved out for the
    // Sepia swatch (#F4ECD8/#5B4636), not a leftover chrome color.
    private static readonly string[] LegacyChromeHexes =
    [
        "#2563EB", "#60A5FA", "#8B6914", "#1A1A2E", "#2A2A3E",
        "#E4E4E7", "#F0F0F0", "#E0E0E0", "#333333", "#666666", "#999999"
    ];

    // The 9 mockup hexes DesignTokens.xaml must expose (D-...-3 / CONTEXT DoD 1).
    private static readonly string[] MockupPaletteHexes =
    [
        "#161826", "#232532", "#E9E9ED", "#9184D9", "#A7A1DB",
        "#F3F5FE", "#292B31", "#F5F4FF", "#2B2741"
    ];

    private static readonly string[] LibraryPageRequiredNames =
    [
        "SidebarPanel", "BookCountLabel", "SearchEntry", "ContinueReadingHero",
        "RecentFilterButton", "TargetLanguageChip", "ModelStatusCard"
    ];

    private static readonly string[] ReaderPageRequiredNames =
        ["ChaptersPanel", "ChaptersCollection", "TocButton"];

    // Shorthand form: {Binding Root.Nested, ...} - captures the raw path up to the first
    // delimiter that cannot be part of a member-access chain. Excludes property-value pairs
    // like "Source=" / "Converter=" because those are followed by '=', not by [,}\s].
    private static readonly Regex BindingShorthandRegex = new(
        @"\{Binding\s+([A-Za-z_][A-Za-z0-9_.]*)(?=[,}\s])",
        RegexOptions.Compiled);

    // Explicit form: {Binding Source=..., Path=Root.Nested}
    private static readonly Regex BindingPathRegex = new(
        @"Path=([A-Za-z_][A-Za-z0-9_.]*)",
        RegexOptions.Compiled);

    // Every event-handler attribute name actually used across the redesigned XAML surfaces.
    private static readonly Regex EventHandlerAttributeRegex = new(
        @"(?:Clicked|Tapped|SelectionChanged|SelectedIndexChanged|ValueChanged|RawMessageReceived|Loaded|Unloaded|TextChanged|Completed|Focused|Unfocused|CheckedChanged)=""([A-Za-z_][A-Za-z0-9_]*)""",
        RegexOptions.Compiled);

    // Public property/method declarations in a PageModel source file. The negative lookahead
    // skips the class declaration itself (its primary-constructor "(" would otherwise be
    // mistaken for a member delimiter).
    private static readonly Regex PublicMemberRegex = new(
        @"^\s*public\s+(?!(?:(?:async|static|partial|override)\s+)*class\b)(?:(?:async|static|partial|override)\s+)*.+?\s(\w+)\s*(\{|=>|\()",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // [RelayCommand] private Task/void FooAsync(...) -> CommunityToolkit.Mvvm generates FooCommand.
    private static readonly Regex RelayCommandMethodRegex = new(
        @"\[RelayCommand\]\s*(?:private|public|internal|protected)\s+(?:async\s+)?(?:Task(?:<[^>]*>)?|void)\s+(\w+)\s*\(",
        RegexOptions.Compiled);

    // [ObservableProperty] private Foo _bar; -> CommunityToolkit.Mvvm generates public Bar.
    // Not used by the current partial-property style in this codebase, kept for the field-based
    // convention too so the parser does not silently miss a future/legacy declaration style.
    private static readonly Regex ObservableFieldRegex = new(
        @"\[ObservableProperty\]\s*(?:\[[^\]]*\]\s*)*private\s+[^;]*?\s_(\w+)\s*;",
        RegexOptions.Compiled);

    [Fact]
    public void DesignTokens_ExposeTheMockupPalette()
    {
        var tokens = File.ReadAllText(DesignTokensPath);
        Assert.True(tokens.Length > 0, "DesignTokens.xaml is empty - cannot verify the palette.");

        foreach (var hex in MockupPaletteHexes)
            Assert.Contains(hex, tokens, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RedesignedXaml_HasNoLegacyChromeHex()
    {
        foreach (var path in RedesignedXamlPaths)
        {
            var xaml = File.ReadAllText(path);
            Assert.True(xaml.Length > 0, $"{Path.GetFileName(path)} is empty - cannot verify legacy hex absence.");

            foreach (var legacyHex in LegacyChromeHexes)
                Assert.DoesNotContain(legacyHex, xaml, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EveryXamlEventHandler_ExistsInTheCodeBehind()
    {
        var pairs = new (string Xaml, string CodeBehind)[]
        {
            (LibraryPageXamlPath, LibraryPageCodeBehindPath),
            (ReaderPageXamlPath, ReaderPageCodeBehindPath),
            (SettingsOverlayXamlPath, SettingsOverlayCodeBehindPath),
            (TranslateBookPopupXamlPath, TranslateBookPopupCodeBehindPath),
            (AppShellXamlPath, AppShellCodeBehindPath)
        };

        var totalHandlers = 0;
        foreach (var (xamlPath, codeBehindPath) in pairs)
        {
            var xaml = File.ReadAllText(xamlPath);
            var codeBehind = File.ReadAllText(codeBehindPath);

            foreach (Match match in EventHandlerAttributeRegex.Matches(xaml))
            {
                totalHandlers++;
                var handlerName = match.Groups[1].Value;
                var methodRegex = new Regex(
                    $@"(private|public|protected|internal)\s+(async\s+)?(void|Task)\s+{Regex.Escape(handlerName)}\s*\(");
                Assert.True(methodRegex.IsMatch(codeBehind),
                    $"Handler '{handlerName}' referenced in {Path.GetFileName(xamlPath)} has no matching method in {Path.GetFileName(codeBehindPath)}.");
            }
        }

        Assert.True(totalHandlers > 0, "No XAML event handlers were found - the handler-attribute regex is likely broken.");
    }

    [Fact]
    public void EveryPageBinding_ResolvesToAKnownMember()
    {
        var knownMembers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in ExtractCSharpMemberNames(File.ReadAllText(LibraryPageModelPath)))
            knownMembers.Add(name);
        foreach (var name in ExtractCSharpMemberNames(File.ReadAllText(ReaderPageModelPath)))
            knownMembers.Add(name);
        foreach (var property in typeof(BookSummary).GetProperties())
            knownMembers.Add(property.Name);
        foreach (var property in typeof(Chapter).GetProperties())
            knownMembers.Add(property.Name);

        var unresolved = new List<string>();
        var totalBindings = 0;

        foreach (var path in RedesignedXamlPaths)
        {
            var xaml = File.ReadAllText(path);
            foreach (var root in ExtractBindingRoots(xaml))
            {
                totalBindings++;
                if (!knownMembers.Contains(root))
                    unresolved.Add($"{Path.GetFileName(path)}:{root}");
            }
        }

        Assert.True(totalBindings > 0, "No bindings were found - the binding-root parser is likely broken.");
        Assert.True(unresolved.Count == 0, "Unresolved binding roots: " + string.Join(", ", unresolved));
    }

    [Fact]
    public void LibraryPage_HasTheMockupStructure()
    {
        var xaml = File.ReadAllText(LibraryPageXamlPath);
        foreach (var name in LibraryPageRequiredNames)
            Assert.Contains($"x:Name=\"{name}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ReaderPage_HasTheChapterNavigationPanel()
    {
        var xaml = File.ReadAllText(ReaderPageXamlPath);
        foreach (var name in ReaderPageRequiredNames)
            Assert.Contains($"x:Name=\"{name}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsOverlay_BranchesLayoutByIdiomWithoutDuplicatingTheControl()
    {
        var xaml = File.ReadAllText(SettingsOverlayXamlPath);
        Assert.Contains("OnIdiom", xaml, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(ControlsRoot, "SettingsPanel.xaml")),
            "SettingsPanel.xaml must not exist - the idiom branch belongs inside SettingsOverlay.xaml.");
        Assert.False(File.Exists(Path.Combine(ControlsRoot, "SettingsSheet.xaml")),
            "SettingsSheet.xaml must not exist - the idiom branch belongs inside SettingsOverlay.xaml.");
    }

    [Fact]
    public void Overlays_UseAnimatedTransitions()
    {
        var readerCodeBehind = File.ReadAllText(ReaderPageCodeBehindPath);
        var settingsCodeBehind = File.ReadAllText(SettingsOverlayCodeBehindPath);

        Assert.True(UsesAnimationApi(readerCodeBehind),
            "ReaderPage.xaml.cs does not animate with TranslateTo/FadeTo (or the *Async variants).");
        Assert.True(UsesAnimationApi(settingsCodeBehind),
            "SettingsOverlay.xaml.cs does not animate with TranslateTo/FadeTo (or the *Async variants).");
    }

    private static bool UsesAnimationApi(string source) =>
        source.Contains("TranslateTo", StringComparison.Ordinal) || source.Contains("FadeTo", StringComparison.Ordinal);

    private static IEnumerable<string> ExtractBindingRoots(string xaml)
    {
        foreach (Match match in BindingShorthandRegex.Matches(xaml))
            yield return RootOf(match.Groups[1].Value);
        foreach (Match match in BindingPathRegex.Matches(xaml))
            yield return RootOf(match.Groups[1].Value);
    }

    private static string RootOf(string path)
    {
        var dotIndex = path.IndexOf('.');
        return dotIndex < 0 ? path : path[..dotIndex];
    }

    private static IEnumerable<string> ExtractCSharpMemberNames(string source)
    {
        foreach (Match match in PublicMemberRegex.Matches(source))
            yield return match.Groups[1].Value;

        foreach (Match match in RelayCommandMethodRegex.Matches(source))
        {
            var method = match.Groups[1].Value;
            var baseName = method.EndsWith("Async", StringComparison.Ordinal)
                ? method[..^"Async".Length]
                : method;
            yield return $"{baseName}Command";
        }

        foreach (Match match in ObservableFieldRegex.Matches(source))
        {
            var field = match.Groups[1].Value;
            yield return char.ToUpperInvariant(field[0]) + field[1..];
        }
    }
}
