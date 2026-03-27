using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TranslateReader.Access;
using TranslateReader.Business.Engines;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.PageModels;
using TranslateReader.Pages;
using TranslateReader.Pages.Controls;
using TranslateReader.Utilities;

#if WINDOWS
using Microsoft.Maui.Platform;
#endif

namespace TranslateReader;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif


#if WINDOWS && DEBUG
        Microsoft.Maui.Handlers.HybridWebViewHandler.Mapper.AppendToMapping("DevTools", (handler, view) =>
        {
            handler.PlatformView.CoreWebView2Initialized += (s, e) =>
            {
                handler.PlatformView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                handler.PlatformView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            };
        });
#endif
        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "translatereader.db");
        var connectionString = $"Data Source={dbPath}";
        var booksDirectory = Path.Combine(FileSystem.AppDataDirectory, "books");

        var modelsDirectory = Path.Combine(FileSystem.AppDataDirectory, "models");

        services.AddSingleton<IBooksAccess>(_ => new BooksAccess(connectionString, initializeOnStartup: true));
        services.AddSingleton<IReadingStateAccess>(_ => new ReadingStateAccess(connectionString, initializeOnStartup: true));
        services.AddSingleton<ISettingsAccess>(_ => new SettingsAccess(connectionString, initializeOnStartup: true));
        services.AddSingleton<ITranslationCacheAccess>(_ => new TranslationCacheAccess(connectionString, initializeOnStartup: true));
        services.AddSingleton<IModelAccess>(_ => new ModelAccess(
            new HttpClient { Timeout = Timeout.InfiniteTimeSpan }, modelsDirectory));
        services.AddSingleton<ITranslationEngine, TranslationEngine>();
        services.AddSingleton<IFileUtility, FileUtility>();

        services.AddTransient<IParsingEngine, ParsingEngine>();
        services.AddTransient<IThemeEngine, ThemeEngine>();
        services.AddTransient<IPromptUtility, PromptUtility>();
        services.AddTransient<ILibraryManager>(sp => new LibraryManager(
            sp.GetRequiredService<IBooksAccess>(),
            sp.GetRequiredService<IReadingStateAccess>(),
            sp.GetRequiredService<ITranslationCacheAccess>(),
            sp.GetRequiredService<IParsingEngine>(),
            sp.GetRequiredService<IFileUtility>(),
            booksDirectory));
        services.AddTransient<IReadingManager>(sp => new ReadingManager(
            sp.GetRequiredService<IBooksAccess>(),
            sp.GetRequiredService<IReadingStateAccess>(),
            sp.GetRequiredService<IParsingEngine>(),
            sp.GetRequiredService<IFileUtility>(),
            booksDirectory));
        services.AddTransient<ITranslationManager, TranslationManager>();
        services.AddTransient<ISettingsManager, SettingsManager>();

        services.AddTransient<LibraryPageModel>();
        services.AddTransient<ReaderPageModel>();
        services.AddTransient<LibraryPage>();
        services.AddTransient<ReaderPage>();
        services.AddTransient<SettingsOverlay>();
    }
}
