using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ISettingsManager
{
    Task<ReadingSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(ReadingSettings settings);
    string GenerateReaderCss(ReadingSettings settings);
    /// <summary>Resolves the bg/text/accent colors of the settings' active reading theme.</summary>
    ThemeColors ResolveThemeColors(ReadingSettings settings);
}
