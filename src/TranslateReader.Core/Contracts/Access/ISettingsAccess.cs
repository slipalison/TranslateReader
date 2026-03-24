using TranslateReader.Models;

namespace TranslateReader.Contracts.Access;

public interface ISettingsAccess
{
    Task<ReadingSettings> FetchSettingsAsync();
    Task SaveSettingsAsync(ReadingSettings settings);
}
