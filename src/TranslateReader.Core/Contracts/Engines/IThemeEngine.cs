using TranslateReader.Models;

namespace TranslateReader.Contracts.Engines;

public interface IThemeEngine
{
    string GenerateReaderCss(ReadingSettings settings);
    ThemeColors ResolveThemeColors(ThemeType theme);
}
