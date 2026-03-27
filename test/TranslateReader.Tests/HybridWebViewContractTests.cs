using System.Text.Json;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class HybridWebViewContractTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string JsRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "TranslateReader", "Resources", "Raw", "wwwroot", "js"));

    private static string ReadJsFile(string fileName) =>
        File.ReadAllText(Path.Combine(JsRoot, fileName));

    [Fact]
    public void PageInfo_DeserializesFromCamelCaseJson()
    {
        var json = """{"current":2,"total":10}""";
        var info = JsonSerializer.Deserialize<PageInfo>(json, CamelCaseOptions);

        Assert.NotNull(info);
        Assert.Equal(2, info.Current);
        Assert.Equal(10, info.Total);
    }

    [Fact]
    public void PageInfo_SerializesToCamelCaseJson()
    {
        var info = new PageInfo(3, 15);
        var json = JsonSerializer.Serialize(info, CamelCaseOptions);

        Assert.Contains("\"current\":3", json);
        Assert.Contains("\"total\":15", json);
        Assert.DoesNotContain("Current", json);
        Assert.DoesNotContain("Total", json);
    }

    [Fact]
    public void ScrollInfo_DeserializesFromCamelCaseJson()
    {
        var json = """{"chapterHRef":"ch1.html","chapterIndex":2,"relativeScroll":0.75}""";
        var info = JsonSerializer.Deserialize<ScrollInfo>(json, CamelCaseOptions);

        Assert.NotNull(info);
        Assert.Equal("ch1.html", info.ChapterHRef);
        Assert.Equal(2, info.ChapterIndex);
        Assert.Equal(0.75, info.RelativeScroll);
    }

    [Fact]
    public void VisibleParagraph_DeserializesFromCamelCaseJson()
    {
        var json = """[{"index":0,"text":"Hello"},{"index":3,"text":"World"}]""";
        var paragraphs = JsonSerializer.Deserialize<List<VisibleParagraph>>(json, CamelCaseOptions);

        Assert.NotNull(paragraphs);
        Assert.Equal(2, paragraphs.Count);
        Assert.Equal(0, paragraphs[0].Index);
        Assert.Equal("Hello", paragraphs[0].Text);
        Assert.Equal(3, paragraphs[1].Index);
        Assert.Equal("World", paragraphs[1].Text);
    }

    [Fact]
    public void BridgeJs_DefinesHybridWebViewBridge()
    {
        var js = ReadJsFile("bridge.js");

        Assert.Contains("window.HybridWebView", js);
        Assert.Contains("SendRawMessage", js);
    }

    [Fact]
    public void BridgeJs_ContainsPlatformSpecificBridges()
    {
        var js = ReadJsFile("bridge.js");

        Assert.Contains("chrome.webview.postMessage", js);
        Assert.Contains("webkit.messageHandlers", js);
        Assert.Contains("hybridWebViewHost", js);
    }

    [Fact]
    public void BridgeJs_SendsReadyMessageOnDomContentLoaded()
    {
        var js = ReadJsFile("bridge.js");

        Assert.Contains("DOMContentLoaded", js);
        Assert.Contains("SendRawMessage", js);
        Assert.Contains("ready", js);
    }

    [Fact]
    public void BridgeJs_DefinesCoreInteropFunctions()
    {
        var js = ReadJsFile("bridge.js");

        Assert.Contains("window.setMode", js);
        Assert.Contains("window.applyCss", js);
        Assert.Contains("window.loadChapter", js);
        Assert.Contains("window.loadScrollContent", js);
    }

    [Fact]
    public void PaginatedJs_ReturnObjectsNotJsonStrings()
    {
        var js = ReadJsFile("paginated.js");

        Assert.Contains("return { current:", js);
        Assert.DoesNotContain("JSON.stringify", js);
    }

    [Fact]
    public void PaginatedJs_DefinesAllNavigationFunctions()
    {
        var js = ReadJsFile("paginated.js");

        Assert.Contains("window.initPagination", js);
        Assert.Contains("window.goToPage", js);
        Assert.Contains("window.nextPage", js);
        Assert.Contains("window.prevPage", js);
        Assert.Contains("window.getPageInfo", js);
        Assert.Contains("window.goToLastPage", js);
        Assert.Contains("window.getTotalPages", js);
    }

    [Fact]
    public void PaginatedJs_PageInfoReturnMatchesCSharpDto()
    {
        var js = ReadJsFile("paginated.js");

        Assert.Contains("current:", js);
        Assert.Contains("total:", js);
        Assert.DoesNotContain("Current:", js);
        Assert.DoesNotContain("Total:", js);
    }

    [Fact]
    public void ScrollJs_ReturnObjectNotJsonString()
    {
        var js = ReadJsFile("scroll.js");

        Assert.Contains("return {", js);
        Assert.DoesNotContain("JSON.stringify", js);
    }

    [Fact]
    public void ScrollJs_ScrollInfoReturnMatchesCSharpDto()
    {
        var js = ReadJsFile("scroll.js");

        Assert.Contains("chapterHRef:", js);
        Assert.Contains("chapterIndex:", js);
        Assert.Contains("relativeScroll:", js);
    }

    [Fact]
    public void ScrollJs_DefinesAllFunctions()
    {
        var js = ReadJsFile("scroll.js");

        Assert.Contains("window.getScrollInfo", js);
        Assert.Contains("window.scrollToChapter", js);
    }

    [Fact]
    public void TranslationJs_DefinesAllFunctions()
    {
        var js = ReadJsFile("translation.js");

        Assert.Contains("window.getVisibleParagraphs", js);
        Assert.Contains("window.applyTranslations", js);
        Assert.Contains("window.clearTranslations", js);
    }

    [Fact]
    public void TranslationJs_VisibleParagraphReturnMatchesCSharpDto()
    {
        var js = ReadJsFile("translation.js");

        Assert.Contains("index:", js);
        Assert.Contains("text:", js);
    }

    [Fact]
    public void TranslationJs_ApplyTranslationsExpectsArrayNotString()
    {
        var js = ReadJsFile("translation.js");

        Assert.DoesNotContain("JSON.parse", js);
        Assert.Contains("items[i].index", js);
        Assert.Contains("items[i].translated", js);
    }

    [Fact]
    public void TranslationJs_ApplyTranslationsForcesRepaint()
    {
        var js = ReadJsFile("translation.js");

        Assert.Contains("goToPage", js);
    }

    [Fact]
    public void IndexHtml_LoadsAllScriptsInCorrectOrder()
    {
        var htmlPath = Path.Combine(JsRoot, "..", "index.html");
        var html = File.ReadAllText(htmlPath);

        var bridgePos = html.IndexOf("js/bridge.js", StringComparison.Ordinal);
        var paginatedPos = html.IndexOf("js/paginated.js", StringComparison.Ordinal);
        var scrollPos = html.IndexOf("js/scroll.js", StringComparison.Ordinal);
        var translationPos = html.IndexOf("js/translation.js", StringComparison.Ordinal);

        Assert.True(bridgePos > 0, "bridge.js not found in index.html");
        Assert.True(paginatedPos > 0, "paginated.js not found in index.html");
        Assert.True(scrollPos > 0, "scroll.js not found in index.html");
        Assert.True(translationPos > 0, "translation.js not found in index.html");
        Assert.True(bridgePos < paginatedPos, "bridge.js must load before paginated.js");
        Assert.True(paginatedPos < translationPos, "paginated.js must load before translation.js");
    }

    [Fact]
    public void IndexHtml_ContainsReaderThemeStyleElement()
    {
        var htmlPath = Path.Combine(JsRoot, "..", "index.html");
        var html = File.ReadAllText(htmlPath);

        Assert.Contains("id=\"reader-theme\"", html);
    }
}
