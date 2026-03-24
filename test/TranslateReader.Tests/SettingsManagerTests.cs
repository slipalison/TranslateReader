using NSubstitute;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Models;

namespace TranslateReader.Tests;

public class SettingsManagerTests
{
    private readonly ISettingsAccess _settingsAccess = Substitute.For<ISettingsAccess>();
    private readonly IThemeEngine _themeEngine = Substitute.For<IThemeEngine>();
    private SettingsManager CreateSut() => new(_settingsAccess, _themeEngine);

    [Fact]
    public async Task LoadSettingsAsync_DelegatesToAccess()
    {
        var expected = new ReadingSettings { Theme = ThemeType.Dark };
        _settingsAccess.FetchSettingsAsync().Returns(expected);

        var result = await CreateSut().LoadSettingsAsync();

        Assert.Equal(ThemeType.Dark, result.Theme);
        await _settingsAccess.Received(1).FetchSettingsAsync();
    }

    [Fact]
    public async Task SaveSettingsAsync_DelegatesToAccess()
    {
        var settings = new ReadingSettings { Theme = ThemeType.Sepia };

        await CreateSut().SaveSettingsAsync(settings);

        await _settingsAccess.Received(1).SaveSettingsAsync(settings);
    }

    [Fact]
    public void GenerateReaderCss_DelegatesToEngine()
    {
        var settings = new ReadingSettings();
        _themeEngine.GenerateReaderCss(settings).Returns("<style>body{}</style>");

        var result = CreateSut().GenerateReaderCss(settings);

        Assert.Equal("<style>body{}</style>", result);
        _themeEngine.Received(1).GenerateReaderCss(settings);
    }
}
