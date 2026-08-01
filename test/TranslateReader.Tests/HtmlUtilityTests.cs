using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public class HtmlUtilityTests
{
    // Fixture A of the phase CONTEXT: calibre exports wrap every paragraph in a <div class="calibreN">
    // and never emit a single <p>. The container div, the image-only div and the bullet-only div must
    // stay out of the block list.
    private const string CalibreBody = """
        <div class="calibre1">
        <div class="calibre2">First calibre paragraph with real text.</div>
        <div class="calibre2">Second calibre paragraph with more text.</div>
        <div class="calibre3"><img src="fig1.png"/></div>
        <div class="calibre2">&#8226;</div>
        <div class="calibre2">Third paragraph, letters only matter here.</div>
        </div>
        """;

    // Fixture B of the phase CONTEXT: every non-space character lives inside the single leaf div.
    private const string FullyCoveredCalibreBody =
        "<div class=\"calibre2\">Only paragraph, fully covered by the leaf div.</div>";

    [Fact]
    public void ExtractTextBlocks_ForCalibreStyleBody_ExtractsLeafDivsWithLetters()
    {
        string[] expected =
        [
            "First calibre paragraph with real text.",
            "Second calibre paragraph with more text.",
            "Third paragraph, letters only matter here."
        ];

        var blocks = HtmlUtility.ExtractTextBlocks(CalibreBody);

        Assert.Equal(expected, blocks);
    }

    [Fact]
    public void ExtractTextBlocks_ForFullyCoveredCalibreBody_ExtractsTheSingleLeafDiv()
    {
        var blocks = HtmlUtility.ExtractTextBlocks(FullyCoveredCalibreBody);

        Assert.Equal(new[] { "Only paragraph, fully covered by the leaf div." }, blocks);
    }

    [Fact]
    public void ExtractTextBlocks_WhenParagraphTagsPresent_IgnoresLeafDivs()
    {
        const string body = "<div class=\"s\"><p>Alpha paragraph</p><p>Beta paragraph</p></div>";

        var blocks = HtmlUtility.ExtractTextBlocks(body);

        Assert.Equal(new[] { "Alpha paragraph", "Beta paragraph" }, blocks);
        Assert.Equal(1, blocks.Count(b => b.Contains("Alpha paragraph", StringComparison.Ordinal)));
        Assert.Equal(1, blocks.Count(b => b.Contains("Beta paragraph", StringComparison.Ordinal)));
    }

    [Fact]
    public void ExtractTextBlocks_ForLeafDivWithoutAnyLetter_SkipsTheBlock()
    {
        const string body =
            "<div class=\"c\">1234</div><div class=\"c\">&#8226;</div><div class=\"c\">Has letters</div>";

        var blocks = HtmlUtility.ExtractTextBlocks(body);

        Assert.Equal(new[] { "Has letters" }, blocks);
    }

    [Fact]
    public void ExtractTextBlocks_ForSelfClosingDiv_DoesNotSwallowTheFollowingDivs()
    {
        const string body = "<div/><br/><div class=\"c\">Alpha text</div><div class=\"c\">Beta text</div>";

        var blocks = HtmlUtility.ExtractTextBlocks(body);

        Assert.Equal(new[] { "Alpha text", "Beta text" }, blocks);
    }

    [Fact]
    public void ExtractTextBlocks_ForLeafDivWithLineBreaks_KeepsItAsOneBlock()
    {
        const string body = "<div class=\"c\">Line one<br/>Line two</div>";

        var blocks = HtmlUtility.ExtractTextBlocks(body);

        Assert.Equal(new[] { "Line oneLine two" }, blocks);
    }

    [Fact]
    public void ReplaceTextBlocksInHtml_ForCalibreStyleBody_WritesEachTranslationIntoItsOwnDiv()
    {
        var result = HtmlUtility.ReplaceTextBlocksInHtml(CalibreBody, ["T1", "T2", "T3"]);

        Assert.Contains("<div class=\"calibre2\">T1</div>", result, StringComparison.Ordinal);
        Assert.Contains("<div class=\"calibre2\">T2</div>", result, StringComparison.Ordinal);
        Assert.Contains("<div class=\"calibre2\">T3</div>", result, StringComparison.Ordinal);
        Assert.True(
            result.IndexOf("T1", StringComparison.Ordinal) < result.IndexOf("T2", StringComparison.Ordinal) &&
            result.IndexOf("T2", StringComparison.Ordinal) < result.IndexOf("T3", StringComparison.Ordinal),
            "translations must land in document order");
        Assert.DoesNotContain("First calibre paragraph", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Third paragraph, letters", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ReplaceTextBlocksInHtml_ForCalibreStyleBody_LeavesLetterlessDivsByteIdentical()
    {
        var result = HtmlUtility.ReplaceTextBlocksInHtml(CalibreBody, ["T1", "T2", "T3"]);

        Assert.Contains("<div class=\"calibre3\"><img src=\"fig1.png\"/></div>", result, StringComparison.Ordinal);
        Assert.Contains("<div class=\"calibre2\">&#8226;</div>", result, StringComparison.Ordinal);
        Assert.Contains("<div class=\"calibre1\">", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractTextBlocks_ForLargeCalibreBody_ExtractsEveryBlockUnderOneSecond()
    {
        const int divCount = 5_000;
        var body = BuildLargeCalibreBody(divCount);

        var stopwatch = Stopwatch.StartNew();
        var blocks = HtmlUtility.ExtractTextBlocks(body);
        stopwatch.Stop();

        Assert.Equal(divCount, blocks.Count);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"extraction of {body.Length} chars took {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void ExtractTextBlocks_ForDegenerateUnclosedDivs_ReturnsOrTimesOutWithoutHanging()
    {
        var body = string.Concat(Enumerable.Repeat("<div class=\"c\" ", 20_000));

        var stopwatch = Stopwatch.StartNew();
        var exception = Record.Exception(() => HtmlUtility.ExtractTextBlocks(body));
        stopwatch.Stop();

        Assert.True(
            exception is null or RegexMatchTimeoutException,
            $"unexpected exception: {exception}");
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"degenerate body pinned the thread for {stopwatch.ElapsedMilliseconds} ms");
    }

    private static string BuildLargeCalibreBody(int divCount)
    {
        var builder = new StringBuilder(divCount * 56);
        for (var i = 0; i < divCount; i++)
            builder.Append("<div class=\"calibre2\">Paragraph number ").Append(i).Append(" with real words.</div>\n");
        return builder.ToString();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n\t ")]
    public void ExtractBodyContent_WithBlankHtml_ReturnsEmptyString(string html)
    {
        Assert.Equal(string.Empty, HtmlUtility.ExtractBodyContent(html));
    }

    [Fact]
    public void ReplaceTextBlocksInHtml_LeavesBlocksWithoutTextUntouched()
    {
        const string html = "<p><img src=\"a.png\" /></p><p>real</p>";

        var result = HtmlUtility.ReplaceTextBlocksInHtml(html, ["traduzido"]);

        Assert.Contains("<p><img src=\"a.png\" /></p>", result, StringComparison.Ordinal);
        Assert.Contains("<p>traduzido</p>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ReplaceTextBlocksInHtml_LeavesBlocksBeyondTheTranslationListUntouched()
    {
        const string html = "<p>um</p><p>dois</p>";

        var result = HtmlUtility.ReplaceTextBlocksInHtml(html, ["traduzido"]);

        Assert.Equal("<p>traduzido</p><p>dois</p>", result);
    }

    [Fact]
    public void ReplaceTextBlocksInHtml_WithNoTranslations_ReturnsHtmlUntouched()
    {
        const string html = "<h2>titulo</h2><li>item</li>";

        Assert.Equal(html, HtmlUtility.ReplaceTextBlocksInHtml(html, []));
    }

    [Fact]
    public void InjectTags_WithBaseTagAndNoCss_InjectsOnlyTheBaseTag()
    {
        const string html = "<html><head><title>t</title></head><body>x</body></html>";

        var result = HtmlUtility.InjectTags(html, "<base href=\"app://local/\" />", css: null);

        Assert.Contains("<base href=\"app://local/\" />", result, StringComparison.Ordinal);
        Assert.DoesNotContain("<style", result, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountOccurrences(result, "<head>"));
    }

    [Fact]
    public void InjectTags_ClosingHeadWithoutOpenTagAndCssOnly_InjectsBeforeTheClosingHead()
    {
        const string html = "<html></head><body>x</body></html>";

        var result = HtmlUtility.InjectTags(html, baseTag: null, css: "<style>p{}</style>");

        Assert.Contains("<style>p{}</style>\n</head>", result, StringComparison.Ordinal);
        Assert.DoesNotContain("<head>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void InjectTags_WithoutAnyHeadTagAndCssOnly_FallsBackToBuildingAHead()
    {
        const string html = "<html><body>x</body></html>";

        var result = HtmlUtility.InjectTags(html, baseTag: null, css: "<style>p{}</style>");

        Assert.Contains("<head><style>p{}</style>\n</head>", result, StringComparison.Ordinal);
        Assert.Contains("<body>x</body>", result, StringComparison.Ordinal);
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
}
