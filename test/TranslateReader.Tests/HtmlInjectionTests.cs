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
}
