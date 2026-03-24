using System.Globalization;
using TranslateReader.Contracts.Engines;
using TranslateReader.Models;

namespace TranslateReader.Business.Engines;

public class ThemeEngine : IThemeEngine
{
    public ThemeColors ResolveThemeColors(ThemeType theme) => theme switch
    {
        ThemeType.Light => new ThemeColors("#FFFFFF", "#1A1A1A", "#2563EB"),
        ThemeType.Dark  => new ThemeColors("#1A1A2E", "#E4E4E7", "#60A5FA"),
        ThemeType.Sepia => new ThemeColors("#F4ECD8", "#5B4636", "#8B6914"),
        _               => new ThemeColors("#FFFFFF", "#1A1A1A", "#2563EB")
    };

    public string GenerateReaderCss(ReadingSettings settings)
    {
        var c = ResolveThemeColors(settings.Theme);
        var inv = CultureInfo.InvariantCulture;
        return "<style>" +
               "body {" +
               $"background-color:{c.Background} !important;" +
               $"color:{c.Text} !important;" +
               $"font-family:{settings.FontFamily},serif !important;" +
               $"font-size:{settings.FontSize.ToString(inv)}px !important;" +
               $"line-height:{settings.LineSpacing.ToString(inv)} !important;" +
               $"letter-spacing:{settings.LetterSpacing.ToString(inv)}px;" +
               $"word-spacing:{settings.WordSpacing.ToString(inv)}px;" +
               "padding:16px 24px;" +
               "margin:0 auto;" +
               "max-width:720px;" +
               "}" +
               "img { max-width: 100%; height: auto; display: block; margin: 1em auto; }" +
               "svg { max-width: 100%; height: auto; }" +
               $"a{{color:{c.Accent} !important;}}" +
               "</style>";
    }
}
