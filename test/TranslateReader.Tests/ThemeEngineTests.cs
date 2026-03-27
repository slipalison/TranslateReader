using TranslateReader.Business.Engines;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class ThemeEngineTests
{
    private readonly ThemeEngine _sut = new();

    [Theory]
    [InlineData(ThemeType.Light, "#FFFFFF", "#1A1A1A", "#2563EB")]
    [InlineData(ThemeType.Dark,  "#1A1A2E", "#E4E4E7", "#60A5FA")]
    [InlineData(ThemeType.Sepia, "#F4ECD8", "#5B4636", "#8B6914")]
    public void ResolveThemeColors_ReturnsCorrectPalette(ThemeType theme, string bg, string text, string accent)
    {
        var colors = _sut.ResolveThemeColors(theme);

        Assert.Equal(bg, colors.Background);
        Assert.Equal(text, colors.Text);
        Assert.Equal(accent, colors.Accent);
    }

    [Fact]
    public void GenerateReaderCss_ReturnsRawCssWithoutTags()
    {
        var settings = new ReadingSettings { ReadingMode = ReadingMode.Paginated };
        var css = _sut.GenerateReaderCss(settings);

        Assert.DoesNotContain("<style>", css);
        Assert.DoesNotContain("<script>", css);
    }

    [Fact]
    public void GenerateReaderCss_ContainsLightThemeColors()
    {
        var settings = new ReadingSettings { Theme = ThemeType.Light };

        var css = _sut.GenerateReaderCss(settings);

        Assert.Contains("#FFFFFF", css);
        Assert.Contains("#1A1A1A", css);
    }

    [Fact]
    public void GenerateReaderCss_ContainsDarkThemeColors()
    {
        var settings = new ReadingSettings { Theme = ThemeType.Dark };

        var css = _sut.GenerateReaderCss(settings);

        Assert.Contains("#1A1A2E", css);
        Assert.Contains("#E4E4E7", css);
    }

    [Fact]
    public void GenerateReaderCss_ContainsSepiaThemeColors()
    {
        var settings = new ReadingSettings { Theme = ThemeType.Sepia };

        var css = _sut.GenerateReaderCss(settings);

        Assert.Contains("#F4ECD8", css);
        Assert.Contains("#5B4636", css);
    }

    [Fact]
    public void GenerateReaderCss_ContainsFontSettings()
    {
        var settings = new ReadingSettings { FontFamily = "Georgia", FontSize = 20 };

        var css = _sut.GenerateReaderCss(settings);

        Assert.Contains("Georgia", css);
        Assert.Contains("20px", css);
    }

    [Fact]
    public void GenerateReaderCss_ContainsLineSpacing()
    {
        var settings = new ReadingSettings { LineSpacing = 2.0 };

        var css = _sut.GenerateReaderCss(settings);

        Assert.Contains("2", css);
    }

    [Fact]
    public void GenerateReaderCss_ScrollMode_DoesNotContainColumnWidth()
    {
        var settings = new ReadingSettings { ReadingMode = ReadingMode.Scroll };
        var css = _sut.GenerateReaderCss(settings);
        Assert.DoesNotContain("column-width", css);
    }

    [Fact]
    public void GenerateReaderCss_PaginatedMode_ContainsOverflowHidden()
    {
        var settings = new ReadingSettings { ReadingMode = ReadingMode.Paginated };
        var css = _sut.GenerateReaderCss(settings);
        Assert.Contains("overflow:hidden", css);
        Assert.Contains("100vh", css);
    }

    [Fact]
    public void GenerateReaderCss_ScrollMode_ContainsChapterSeparatorCss()
    {
        var settings = new ReadingSettings { ReadingMode = ReadingMode.Scroll };
        var css = _sut.GenerateReaderCss(settings);
        Assert.Contains("chapter-separator", css);
        Assert.Contains("chapter-content", css);
    }

    [Fact]
    public void GenerateReaderCss_PaginatedMode_NoScriptTags()
    {
        var settings = new ReadingSettings { ReadingMode = ReadingMode.Paginated };
        var css = _sut.GenerateReaderCss(settings);
        Assert.DoesNotContain("<script>", css);
    }

    [Fact]
    public void GenerateReaderCss_PaginatedMode_ContainsBreakInsideAvoid()
    {
        var settings = new ReadingSettings { ReadingMode = ReadingMode.Paginated };
        var css = _sut.GenerateReaderCss(settings);
        Assert.Contains("break-inside:avoid", css);
    }
}
