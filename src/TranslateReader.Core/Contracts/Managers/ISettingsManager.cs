using TranslateReader.Models;

namespace TranslateReader.Contracts.Managers;

public interface ISettingsManager
{
    Task<ReadingSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(ReadingSettings settings);
    string GenerateReaderCss(ReadingSettings settings);
}
