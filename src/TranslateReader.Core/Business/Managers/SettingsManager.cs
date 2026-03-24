using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Models;

namespace TranslateReader.Business.Managers;

public class SettingsManager(
    ISettingsAccess settingsAccess,
    IThemeEngine themeEngine) : ISettingsManager
{
    public Task<ReadingSettings> LoadSettingsAsync() =>
        settingsAccess.FetchSettingsAsync();

    public Task SaveSettingsAsync(ReadingSettings settings) =>
        settingsAccess.SaveSettingsAsync(settings);

    public string GenerateReaderCss(ReadingSettings settings) =>
        themeEngine.GenerateReaderCss(settings);
}
