using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public class HtmlUtilityTests
{
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
