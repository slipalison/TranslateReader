using TranslateReader.Access;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class SettingsAccessTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private SettingsAccess CreateSut() => new(_db.ConnectionString, initializeOnStartup: true);

    public void Dispose() => _db.Dispose();

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
}
