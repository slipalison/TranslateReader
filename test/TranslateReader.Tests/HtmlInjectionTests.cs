using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public class HtmlInjectionTests
{
    [Fact]
    public void EmptyHtml_ShouldHaveBody()
    {
        var html = "";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.Contains("<body", result);
        Assert.Contains("</body>", result);
    }

    [Fact]
    public void FullHtmlWithAttributes_ShouldNotHaveDoubleHeads()
    {
        var html = "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\"><head><title>Test</title></head><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        var headCount = System.Text.RegularExpressions.Regex.Matches(result, "<head", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        Assert.Equal(1, headCount);
        
        Assert.Contains("Content", result);
        Assert.Contains("body", result);
        Assert.Contains("CSS", result);
        Assert.Contains("base", result);
    }

    [Fact]
    public void XmlDeclarationWithHtml_ShouldWorkCorrectly()
    {
        var html = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>Test</title></head><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.Contains("Content", result);
        Assert.Contains("<body", result);
        Assert.Contains("</body>", result);
        Assert.Contains("CSS", result);
        
        Assert.StartsWith("<?xml", result);
    }

    [Fact]
    public void Fragment_ShouldBeWrapped()
    {
        var html = "<p>Some text</p>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.Contains("<html>", result);
        Assert.Contains("<head>", result);
        Assert.Contains("<body>", result);
        Assert.Contains("<p>Some text</p>", result);
    }

    [Fact]
    public void FragmentWithBody_ShouldNotHaveNestedBodies()
    {
        var html = "<body><p>Some text</p></body>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        var bodyCount = System.Text.RegularExpressions.Regex.Matches(result, "<body", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        Assert.Equal(1, bodyCount);
        Assert.Contains("<html>", result);
        Assert.Contains("<head>", result);
    }

    [Fact]
    public void WhitespaceHtml_ShouldHaveBody()
    {
        var html = "   \n   ";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.Contains("<body", result);
        Assert.Contains("</body>", result);
    }

    [Fact]
    public void ExtractBodyContent_ReturnsContentBetweenBodyTags()
    {
        var html = "<html><head><title>T</title></head><body><p>Hello</p></body></html>";
        var result = HtmlUtility.ExtractBodyContent(html);
        Assert.Equal("<p>Hello</p>", result);
    }

    [Fact]
    public void ExtractBodyContent_ReturnsHtmlAsIs_WhenNoBodyTag()
    {
        var html = "<p>Just a paragraph</p>";
        var result = HtmlUtility.ExtractBodyContent(html);
        Assert.Equal("<p>Just a paragraph</p>", result);
    }

    [Fact]
    public void ExtractBodyContent_HandlesBodyWithAttributes()
    {
        var html = "<html><body class=\"main\" id=\"b\"><div>Content</div></body></html>";
        var result = HtmlUtility.ExtractBodyContent(html);
        Assert.Equal("<div>Content</div>", result);
    }

    [Fact]
    public void BuildContinuousScrollHtml_WrapsChaptersInDivs()
    {
        var chapters = new List<(string href, string bodyContent)>
        {
            ("ch1.html", "<p>Chapter 1</p>"),
            ("ch2.html", "<p>Chapter 2</p>")
        };
        var result = HtmlUtility.BuildContinuousScrollHtml(chapters, "<style>body{}</style>");

        Assert.Contains("data-chapter-href=\"ch1.html\"", result);
        Assert.Contains("data-chapter-href=\"ch2.html\"", result);
        Assert.Contains("data-chapter-index=\"0\"", result);
        Assert.Contains("data-chapter-index=\"1\"", result);
        Assert.Contains("<p>Chapter 1</p>", result);
        Assert.Contains("<p>Chapter 2</p>", result);
        Assert.Contains("chapter-separator", result);
        Assert.Contains("<style>body{}</style>", result);
    }

    [Fact]
    public void BuildContinuousScrollHtml_SingleChapter_NoSeparator()
    {
        var chapters = new List<(string href, string bodyContent)>
        {
            ("ch1.html", "<p>Only one</p>")
        };
        var result = HtmlUtility.BuildContinuousScrollHtml(chapters, "");

        Assert.Contains("<p>Only one</p>", result);
        Assert.DoesNotContain("chapter-separator", result);
    }
}
