using System.Reflection;
using System.Text.RegularExpressions;
using TranslateReader.Utilities;

namespace TranslateReader.Tests;

public partial class HtmlInjectionTests
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

        var headCount = HeadTagRegex().Count(result);
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

        var bodyCount = BodyTagRegex().Count(result);
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
        var result = HtmlUtility.BuildContinuousScrollHtml(chapters);

        Assert.Contains("data-chapter-href=\"ch1.html\"", result);
        Assert.Contains("data-chapter-href=\"ch2.html\"", result);
        Assert.Contains("data-chapter-index=\"0\"", result);
        Assert.Contains("data-chapter-index=\"1\"", result);
        Assert.Contains("<p>Chapter 1</p>", result);
        Assert.Contains("<p>Chapter 2</p>", result);
        Assert.Contains("chapter-separator", result);
        Assert.DoesNotContain("<html>", result);
    }

    [Fact]
    public void BuildContinuousScrollHtml_SingleChapter_NoSeparator()
    {
        var chapters = new List<(string href, string bodyContent)>
        {
            ("ch1.html", "<p>Only one</p>")
        };
        var result = HtmlUtility.BuildContinuousScrollHtml(chapters);

        Assert.Contains("<p>Only one</p>", result);
        Assert.DoesNotContain("chapter-separator", result);
    }

    [Fact]
    public void InjectTags_WhenHtmlAlreadyHasBaseTag_KeepsExactlyOneBase()
    {
        var html = "<html><head><base href=\"OEBPS/\" /><title>Test</title></head><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        var baseCount = BaseTagRegex().Count(result);
        Assert.Equal(1, baseCount);
        Assert.Contains("OEBPS/", result);
        Assert.Contains("CSS", result);
    }

    [Fact]
    public void InjectTags_WithNothingToInject_ReturnsHtmlUntouched()
    {
        var html = "<html><head><title>Test</title></head><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, null, null);

        Assert.Equal(html, result);
    }

    [Fact]
    public void ExtractBodyContent_WithUnclosedBody_ReturnsEverythingAfterTheOpenTag()
    {
        var html = "<html><head><title>T</title></head><body class=\"main\"><p>Hello</p>";
        var result = HtmlUtility.ExtractBodyContent(html);
        Assert.Equal("<p>Hello</p>", result);
    }

    [Fact]
    public void BuildContinuousScrollHtml_WithNoChapters_ReturnsEmptyString()
    {
        var result = HtmlUtility.BuildContinuousScrollHtml([]);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void InjectTags_HtmlTagWithoutHead_InsertsHeadRightAfterTheHtmlOpenTag()
    {
        var html = "<HTML lang=\"en\"><p>Text</p></HTML>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<HTML lang=\"en\">\n<head>", result);
        Assert.Equal(1, HeadTagRegex().Count(result));
        Assert.Contains("CSS", result);
        Assert.Contains("base", result);
        Assert.Contains("<p>Text</p>", result);
    }

    [Fact]
    public void InjectTags_XmlDeclarationWithoutHtmlOrBody_WrapsInFullDocument()
    {
        var html = "<?xml version=\"1.0\" encoding=\"utf-8\"?><p>Text</p>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<html>", result);
        Assert.EndsWith("\n</body>\n</html>", result);
        Assert.Equal(1, HeadTagRegex().Count(result));
        Assert.Equal(1, BodyTagRegex().Count(result));
        Assert.Contains("CSS", result);
        Assert.Contains("<p>Text</p>", result);
    }

    [Fact]
    public void InjectTags_UppercaseXmlDeclaration_IsStillRecognizedAsADeclaration()
    {
        var html = "<?XML version=\"1.0\"?><p>Text</p>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<?XML version=\"1.0\"?>\n<html>", result);
        Assert.EndsWith("\n</body>\n</html>", result);
    }

    [Fact]
    public void InjectTags_UppercaseHeadTag_InjectsInPlaceInsteadOfFallingBack()
    {
        var html = "<html><HEAD><title>T</title></HEAD><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<html><HEAD>\n<base href=\"./\" />", result);
        Assert.Equal(1, HeadTagRegex().Count(result));
        Assert.Contains("CSS</style>\n</HEAD>", result);
    }

    [Fact]
    public void InjectTags_UppercaseBodyFragment_IsWrappedWithoutNestingBodies()
    {
        var html = "<BODY><p>Text</p></BODY>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<html>\n<head>", result);
        Assert.EndsWith("</html>", result);
        Assert.Equal(1, BodyTagRegex().Count(result));
        Assert.Contains("<p>Text</p>", result);
    }

    [Fact]
    public void InjectTags_WhitespaceHtml_DropsTheWhitespaceInsteadOfWrappingIt()
    {
        var result = HtmlUtility.InjectTags("   \n   ", "<base href=\"./\" />", "<style>CSS</style>");

        Assert.Equal(
            "<html><head><base href=\"./\" /><style>CSS</style></head><body></body></html>",
            result);
    }

    [Fact]
    public void InjectTags_FragmentWithNothingToInject_IsNotWrapped()
    {
        var html = "<p>Text</p>";
        var result = HtmlUtility.InjectTags(html, null, null);

        Assert.Equal(html, result);
    }

    [Fact]
    public void InjectTags_OpenHeadWithoutClosingTag_AnchorsCssRightAfterTheOpenTag()
    {
        var html = "<html><head><title>T</title><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<html><head>\n<style>CSS</style>\n<base href=\"./\" />", result);
        Assert.Contains("<title>T</title>", result);
    }

    [Fact]
    public void InjectTags_ClosingHeadWithoutOpenTag_FallsBackWhenABaseTagIsPending()
    {
        var html = "<html></head><body>Content</body></html>";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("<html>\n<head><base href=\"./\" /><style>CSS</style>\n</head>", result);
        Assert.Contains("Content", result);
    }

    [Fact]
    public void InjectTags_BodyFragmentWithTruncatedHtmlTag_IsNotWrappedAgain()
    {
        // `<HTML` never closes, so the full-open-tag regex misses it while the
        // presence regex still sees it: the wrapper must not be added twice.
        var html = "<body><p>Text</p></body>\n<HTML";
        var result = HtmlUtility.InjectTags(html, "<base href=\"./\" />", "<style>CSS</style>");

        Assert.StartsWith("\n<head>", result);
        Assert.DoesNotContain("<html>", result);
        Assert.DoesNotContain("</html>", result);
    }

    [Fact]
    public void ExtractBodyContent_UppercaseBodyTag_ReturnsInnerContent()
    {
        var html = "<html><HEAD><title>T</title></HEAD><BODY class=\"m\"><p>Hello</p></BODY></html>";
        var result = HtmlUtility.ExtractBodyContent(html);
        Assert.Equal("<p>Hello</p>", result);
    }

    [Fact]
    public void EveryHtmlUtilityRegex_IsBoundedByAMatchTimeout()
    {
        var factories = typeof(HtmlUtility)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(Regex) && m.GetParameters().Length == 0)
            .ToList();

        Assert.Equal(7, factories.Count);
        foreach (var factory in factories)
        {
            var regex = factory.Invoke(null, null) as Regex;
            Assert.NotNull(regex);
            Assert.NotEqual(Regex.InfiniteMatchTimeout, regex.MatchTimeout);
        }
    }

    [GeneratedRegex("<head", RegexOptions.IgnoreCase)]
    private static partial Regex HeadTagRegex();

    [GeneratedRegex("<body", RegexOptions.IgnoreCase)]
    private static partial Regex BodyTagRegex();

    [GeneratedRegex("<base", RegexOptions.IgnoreCase)]
    private static partial Regex BaseTagRegex();
}
