using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using TranslateReader.Access;
using TranslateReader.Business.Engines;
using TranslateReader.Business.Managers;
using TranslateReader.Contracts.Access;
using TranslateReader.Contracts.Engines;
using TranslateReader.Contracts.Managers;
using TranslateReader.Contracts.Utilities;
using TranslateReader.Models;
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
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Phosphor.ttf", "Phosphor");
                fonts.AddFont("Phosphor-Fill.ttf", "PhosphorFill");
            });

        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif


#if WINDOWS
        var imagesRoot = Path.Combine(FileSystem.AppDataDirectory, "books", "images");
        Microsoft.Maui.Handlers.HybridWebViewHandler.Mapper.AppendToMapping("EpubImages", (handler, view) =>
        {
            handler.PlatformView.CoreWebView2Initialized += (s, e) =>
            {
                handler.PlatformView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "epub-images", imagesRoot,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
#if DEBUG
                handler.PlatformView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                handler.PlatformView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
#endif
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
        services.AddSingleton<ISnippetTranslationAccess>(_ => new SnippetTranslationAccess(connectionString, initializeOnStartup: true));
        services.AddSingleton<IBookTranslationJobAccess>(_ => new BookTranslationJobAccess(connectionString, initializeOnStartup: true));
        services.AddSingleton<IModelAccess>(_ => new ModelAccess(
            new HttpClient { Timeout = Timeout.InfiniteTimeSpan }, modelsDirectory));

        // The managed, LLamaSharp-backed engine is the only ITranslationEngine that can ever touch
        // LLamaSharp's native loader, which throws PlatformNotSupportedException from its own
        // static constructor on iOS/MacCatalyst (D-2026-08-16-llm-mobile-5). Registering the
        // null-object engine there instead means that type is never touched on those TFMs, so the
        // crash cannot happen -- this is the only #if of the phase, and ITranslationEngine stays
        // the single point of variation by platform (Managers/PageModels never branch on platform).
#if IOS || MACCATALYST
        services.AddSingleton<ITranslationEngine, UnavailableTranslationEngine>();
#else
        services.AddSingleton<ITranslationEngine, TranslationEngine>();
#endif
        services.AddSingleton<IFileUtility, FileUtility>();
        services.AddSingleton<IDeviceMemoryUtility, DeviceMemoryUtility>();

        services.AddTransient<IParsingEngine, ParsingEngine>();
        services.AddTransient<IThemeEngine, ThemeEngine>();
        services.AddTransient<IPromptUtility, PromptUtility>();
        services.AddTransient<ILibraryManager>(sp => new LibraryManager(
            sp.GetRequiredService<IBooksAccess>(),
            sp.GetRequiredService<IReadingStateAccess>(),
            sp.GetRequiredService<ITranslationCacheAccess>(),
            sp.GetRequiredService<ISnippetTranslationAccess>(),
            sp.GetRequiredService<IParsingEngine>(),
            sp.GetRequiredService<IFileUtility>(),
            booksDirectory));
        services.AddTransient<IReadingManager>(sp => new ReadingManager(
            sp.GetRequiredService<IBooksAccess>(),
            sp.GetRequiredService<IReadingStateAccess>(),
            sp.GetRequiredService<IParsingEngine>(),
            sp.GetRequiredService<IFileUtility>(),
            booksDirectory));

        // The Manager must not name NativeBackendPlan itself (it is business data, not business
        // logic): the platform is detected once, here at the composition root, and only the
        // resulting bool crosses into TranslationManager's constructor.
        var isTranslationBackendSupported = NativeBackendPlan.For(DetectCurrentPlatform()).IsManagedBackendSupported;
        TranslationManager CreateTranslationManager(IServiceProvider sp) => new(
            sp.GetRequiredService<ITranslationEngine>(),
            sp.GetRequiredService<IModelAccess>(),
            sp.GetRequiredService<ITranslationCacheAccess>(),
            sp.GetRequiredService<IBookTranslationJobAccess>(),
            sp.GetRequiredService<IPromptUtility>(),
            sp.GetRequiredService<IBooksAccess>(),
            sp.GetRequiredService<IParsingEngine>(),
            sp.GetRequiredService<ISettingsAccess>(),
            sp.GetRequiredService<ISnippetTranslationAccess>(),
            sp.GetRequiredService<IDeviceMemoryUtility>(),
            isTranslationBackendSupported);
        services.AddTransient<ITranslationManager>(CreateTranslationManager);
        services.AddTransient<ISnippetTranslationManager>(CreateTranslationManager);
        services.AddTransient<ISettingsManager, SettingsManager>();

        services.AddTransient<LibraryPageModel>();
        services.AddTransient<ReaderPageModel>();
        services.AddTransient<LibraryPage>();
        services.AddTransient<ReaderPage>();
        services.AddTransient<SettingsOverlay>();
    }

    private static TranslationPlatform DetectCurrentPlatform()
    {
        if (OperatingSystem.IsWindows()) return TranslationPlatform.Windows;
        if (OperatingSystem.IsAndroid()) return TranslationPlatform.Android;
        if (OperatingSystem.IsIOS()) return TranslationPlatform.IOS;
        if (OperatingSystem.IsMacCatalyst()) return TranslationPlatform.MacCatalyst;
        return TranslationPlatform.Other;
    }
}
