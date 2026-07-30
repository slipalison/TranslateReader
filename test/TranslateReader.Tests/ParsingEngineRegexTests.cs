using System.Reflection;
using System.Text.RegularExpressions;
using TranslateReader.Business.Engines;

namespace TranslateReader.Tests;

// Behavioural lock for the 7 compile-time patterns of ParsingEngine: a corrupted pattern or a
// dropped RegexOptions must fail here. Reached by reflection because the patterns are private
// detail of the Engine and every public entry point takes a file path (disk I/O is forbidden in
// new unit tests by .claude/rules/csharp.md section 6).
public class ParsingEngineRegexTests
{
    private static Regex Pattern(string name)
    {
        var factory = typeof(ParsingEngine).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"ParsingEngine.{name}() does not exist.");

        return (Regex)factory.Invoke(null, null)!;
    }

    // ── OpfTitleRegex ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(@"<dc:title id=""t1"">Old</dc:title>")]
    [InlineData(@"<DC:TITLE ID=""t1"">Old</DC:TITLE>")]
    public void OpfTitleRegex_CapturesTheTitleTextInAnyCase(string xml)
    {
        var match = Pattern("OpfTitleRegex").Match(xml);

        Assert.True(match.Success);
        Assert.Equal("Old", match.Groups[2].Value);
    }

    [Fact]
    public void OpfTitleRegex_ReplacesTheTitleTextKeepingBothTags()
    {
        var result = Pattern("OpfTitleRegex").Replace(@"<dc:title id=""t1"">Old</dc:title>", "$1New$3");

        Assert.Equal(@"<dc:title id=""t1"">New</dc:title>", result);
    }

    [Fact]
    public void OpfTitleRegex_MatchesATitleSpanningLines()
    {
        var match = Pattern("OpfTitleRegex").Match("<dc:title>First\nSecond</dc:title>");

        Assert.True(match.Success);
        Assert.Equal("First\nSecond", match.Groups[2].Value);
    }

    [Fact]
    public void OpfTitleRegex_ReplacesEachTitleElementSeparately()
    {
        var result = Pattern("OpfTitleRegex")
            .Replace("<dc:title>A</dc:title><dc:creator>X</dc:creator><dc:title>B</dc:title>", "$1N$3");

        Assert.Equal("<dc:title>N</dc:title><dc:creator>X</dc:creator><dc:title>N</dc:title>", result);
    }

    // ── LinkTagRegex ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(@"<link rel=""stylesheet"" href=""a.css""/>", @" rel=""stylesheet"" href=""a.css""")]
    [InlineData(@"<LINK REL=""stylesheet"" HREF=""a.css"">", @" REL=""stylesheet"" HREF=""a.css""")]
    public void LinkTagRegex_CapturesTheAttributesInAnyCase(string html, string expectedAttributes)
    {
        var match = Pattern("LinkTagRegex").Match(html);

        Assert.True(match.Success);
        Assert.Equal(expectedAttributes, match.Groups[1].Value);
    }

    [Fact]
    public void LinkTagRegex_DoesNotMatchATagMerelyStartingWithLink()
    {
        Assert.DoesNotMatch(Pattern("LinkTagRegex"), @"<linkage rel=""stylesheet"">");
    }

    // ── StylesheetRelRegex ──────────────────────────────────────────────────

    [Theory]
    [InlineData(@" rel=""stylesheet"" href=""a.css""")]
    [InlineData(@" REL = ""STYLESHEET"" href=""a.css""")]
    public void StylesheetRelRegex_MatchesTheStylesheetRelationInAnyCase(string attributes)
    {
        Assert.Matches(Pattern("StylesheetRelRegex"), attributes);
    }

    [Theory]
    [InlineData(@" rel=""stylesheets""")]
    [InlineData(@" xrel=""stylesheet""")]
    [InlineData(@" rel=""alternate stylesheet""")]
    public void StylesheetRelRegex_DoesNotMatchANonStylesheetRelation(string attributes)
    {
        Assert.DoesNotMatch(Pattern("StylesheetRelRegex"), attributes);
    }

    // ── StylesheetHrefRegex ─────────────────────────────────────────────────

    [Theory]
    [InlineData(@" rel=""stylesheet"" href=""css/main.css""")]
    [InlineData(@" REL=""stylesheet"" HREF = ""css/main.css""")]
    public void StylesheetHrefRegex_CapturesTheHrefValueInAnyCase(string attributes)
    {
        var match = Pattern("StylesheetHrefRegex").Match(attributes);

        Assert.True(match.Success);
        Assert.Equal("css/main.css", match.Groups[1].Value);
    }

    [Fact]
    public void StylesheetHrefRegex_DoesNotMatchAnEmptyHref()
    {
        Assert.DoesNotMatch(Pattern("StylesheetHrefRegex"), @" href=""""");
    }

    // ── ImgSrcRegex ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(@"<img class=""c"" src=""images/a.png""/>")]
    [InlineData(@"<IMG CLASS=""c"" SRC=""images/a.png""/>")]
    public void ImgSrcRegex_CapturesTheSourceInAnyCase(string html)
    {
        var match = Pattern("ImgSrcRegex").Match(html);

        Assert.True(match.Success);
        Assert.Equal("images/a.png", match.Groups[2].Value);
    }

    [Fact]
    public void ImgSrcRegex_RewritesOnlyTheSourceValue()
    {
        var result = Pattern("ImgSrcRegex")
            .Replace(@"<img src=""a.png""/>", "$1https://epub-images/book/a.png$3");

        Assert.Equal(@"<img src=""https://epub-images/book/a.png""/>", result);
    }

    [Fact]
    public void ImgSrcRegex_DoesNotMatchAnSvgImageElement()
    {
        Assert.DoesNotMatch(Pattern("ImgSrcRegex"), @"<image src=""a.png""/>");
    }

    // ── SvgImageXlinkHrefRegex ──────────────────────────────────────────────

    [Theory]
    [InlineData(@"<image width=""10"" xlink:href=""img/a.png""/>")]
    [InlineData(@"<IMAGE WIDTH=""10"" XLINK:HREF=""img/a.png""/>")]
    public void SvgImageXlinkHrefRegex_CapturesTheXlinkTargetInAnyCase(string html)
    {
        var match = Pattern("SvgImageXlinkHrefRegex").Match(html);

        Assert.True(match.Success);
        Assert.Equal("img/a.png", match.Groups[2].Value);
    }

    [Fact]
    public void SvgImageXlinkHrefRegex_DoesNotMatchAPlainHrefImage()
    {
        Assert.DoesNotMatch(Pattern("SvgImageXlinkHrefRegex"), @"<image href=""a.png""/>");
    }

    // ── SvgImageHrefRegex ───────────────────────────────────────────────────

    [Theory]
    [InlineData(@"<image height=""5"" href=""img/a.png""/>")]
    [InlineData(@"<IMAGE HEIGHT=""5"" HREF=""img/a.png""/>")]
    public void SvgImageHrefRegex_CapturesTheHrefTargetInAnyCase(string html)
    {
        var match = Pattern("SvgImageHrefRegex").Match(html);

        Assert.True(match.Success);
        Assert.Equal("img/a.png", match.Groups[2].Value);
    }

    [Fact]
    public void SvgImageHrefRegex_DoesNotMatchAnHtmlImgElement()
    {
        Assert.DoesNotMatch(Pattern("SvgImageHrefRegex"), @"<img href=""a.png""/>");
    }
}
