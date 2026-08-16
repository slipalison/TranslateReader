using Microsoft.Data.Sqlite;
using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class SettingsAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private SettingsAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FetchSettingsAsync_ReturnsDefaults_WhenNothingSaved()
    {
        var settings = await CreateSut().FetchSettingsAsync();

        Assert.Equal(ThemeType.Light, settings.Theme);
        Assert.Equal("Georgia", settings.FontFamily);
        Assert.Equal(18, settings.FontSize);
        Assert.Equal(1.6, settings.LineSpacing);
        Assert.Equal(0, settings.LetterSpacing);
        Assert.Equal(0, settings.WordSpacing);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenFetch_ReturnsTheme()
    {
        var sut = CreateSut();
        var saved = new ReadingSettings { Theme = ThemeType.Dark };

        await sut.SaveSettingsAsync(saved);
        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal(ThemeType.Dark, fetched.Theme);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenFetch_ReturnsFontFamily()
    {
        var sut = CreateSut();
        var saved = new ReadingSettings { FontFamily = "monospace" };

        await sut.SaveSettingsAsync(saved);
        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal("monospace", fetched.FontFamily);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenFetch_ReturnsFontSize()
    {
        var sut = CreateSut();
        var saved = new ReadingSettings { FontSize = 24 };

        await sut.SaveSettingsAsync(saved);
        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal(24, fetched.FontSize);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenFetch_ReturnsAllSpacings()
    {
        var sut = CreateSut();
        var saved = new ReadingSettings
        {
            Theme = ThemeType.Sepia,
            FontFamily = "serif",
            FontSize = 16,
            LineSpacing = 2.0,
            LetterSpacing = 1.5,
            WordSpacing = 3.0
        };

        await sut.SaveSettingsAsync(saved);
        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal(ThemeType.Sepia, fetched.Theme);
        Assert.Equal("serif", fetched.FontFamily);
        Assert.Equal(16, fetched.FontSize);
        Assert.Equal(2.0, fetched.LineSpacing);
        Assert.Equal(1.5, fetched.LetterSpacing);
        Assert.Equal(3.0, fetched.WordSpacing);
    }

    [Fact]
    public async Task SaveSettingsAsync_Twice_OverwritesPrevious()
    {
        var sut = CreateSut();
        await sut.SaveSettingsAsync(new ReadingSettings { Theme = ThemeType.Dark });
        await sut.SaveSettingsAsync(new ReadingSettings { Theme = ThemeType.Sepia });

        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal(ThemeType.Sepia, fetched.Theme);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenFetch_ReturnsReadingMode()
    {
        var sut = CreateSut();
        var saved = new ReadingSettings { ReadingMode = ReadingMode.Paginated };

        await sut.SaveSettingsAsync(saved);
        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal(ReadingMode.Paginated, fetched.ReadingMode);
    }

    [Fact]
    public async Task FetchSettingsAsync_ReturnsDefaultLanguages_WhenNothingSaved()
    {
        var settings = await CreateSut().FetchSettingsAsync();

        Assert.Equal("English", settings.SourceLanguage);
        Assert.Equal("Brazilian Portuguese (PT-BR)", settings.TargetLanguage);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenFetch_ReturnsLanguageSettings()
    {
        var sut = CreateSut();
        var saved = new ReadingSettings
        {
            SourceLanguage = "Spanish",
            TargetLanguage = "French"
        };

        await sut.SaveSettingsAsync(saved);
        var fetched = await sut.FetchSettingsAsync();

        Assert.Equal("Spanish", fetched.SourceLanguage);
        Assert.Equal("French", fetched.TargetLanguage);
    }

    [Fact]
    public async Task FetchSettingsAsync_WithOnlyAnUnknownKeyStored_FallsBackToEveryDefault()
    {
        var sut = CreateSut();
        await InsertRawAsync("LegacyKeyNobodyReadsAnymore", "whatever");

        var settings = await sut.FetchSettingsAsync();

        Assert.Equal(ThemeType.Light, settings.Theme);
        Assert.Equal("Georgia", settings.FontFamily);
        Assert.Equal(18, settings.FontSize);
        Assert.Equal(1.6, settings.LineSpacing);
        Assert.Equal(0, settings.LetterSpacing);
        Assert.Equal(0, settings.WordSpacing);
        Assert.Equal(ReadingMode.Scroll, settings.ReadingMode);
        Assert.Equal("hy-mt2-1.8b", settings.TranslationModelName);
        Assert.Equal(0.1, settings.TranslationTemperature);
        Assert.Equal("English", settings.SourceLanguage);
        Assert.Equal("Brazilian Portuguese (PT-BR)", settings.TargetLanguage);
    }

    [Fact]
    public async Task FetchSettingsAsync_WithUnparsableStoredValues_FallsBackToTheTypedDefaults()
    {
        var sut = CreateSut();
        await InsertRawAsync("Theme", "Neon");
        await InsertRawAsync("ReadingMode", "Diagonal");
        await InsertRawAsync("FontSize", "grande");
        await InsertRawAsync("LineSpacing", "");
        await InsertRawAsync("LetterSpacing", "n/a");
        await InsertRawAsync("WordSpacing", "n/a");
        await InsertRawAsync("TranslationTemperature", "quente");

        var settings = await sut.FetchSettingsAsync();

        Assert.Equal(ThemeType.Light, settings.Theme);
        Assert.Equal(ReadingMode.Scroll, settings.ReadingMode);
        Assert.Equal(18, settings.FontSize);
        Assert.Equal(1.6, settings.LineSpacing);
        Assert.Equal(0, settings.LetterSpacing);
        Assert.Equal(0, settings.WordSpacing);
        Assert.Equal(0.1, settings.TranslationTemperature);
    }

    [Fact]
    public async Task Constructor_WithoutStartupInitialization_LeavesTheSchemaUncreated()
    {
        var sut = new SettingsAccess(_db.ConnectionString, initializeOnStartup: false);

        await Assert.ThrowsAsync<SqliteException>(sut.FetchSettingsAsync);
    }

    private async Task InsertRawAsync(string key, string value)
    {
        using var connection = new SqliteConnection(_db.ConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ($key, $value)";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }
}
