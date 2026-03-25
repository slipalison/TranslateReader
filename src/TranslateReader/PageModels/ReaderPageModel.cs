using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateReader.Contracts.Managers;
using TranslateReader.Models;

using TranslateReader.Utilities;

namespace TranslateReader.PageModels;

[QueryProperty(nameof(BookId), "bookId")]
public partial class ReaderPageModel(
    IReadingManager readingManager,
    ISettingsManager settingsManager) : ObservableObject
{
    [ObservableProperty]
    private int _bookId;

    [ObservableProperty]
    private Book? _book;

    [ObservableProperty]
    private IReadOnlyList<Chapter> _chapters = [];

    [ObservableProperty]
    private int _currentChapterIndex;

    [ObservableProperty]
    private string _chapterContent = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasPreviousChapter;

    [ObservableProperty]
    private bool _hasNextChapter;

    [ObservableProperty]
    private bool _isSettingsVisible;

    public ReadingSettings CurrentSettings { get; private set; } = new();

    public double SavedScrollPosition { get; set; }

    partial void OnBookIdChanged(int value) => _ = InitializeAsync(value);

    private async Task InitializeAsync(int bookId)
    {
        IsBusy = true;
        try
        {
            Book = await readingManager.OpenBookAsync(bookId);
            Chapters = await readingManager.LoadChaptersAsync(bookId);
            CurrentSettings = await settingsManager.LoadSettingsAsync();
            var progress = await readingManager.LoadProgressAsync(bookId);
            CurrentChapterIndex = progress is not null
                ? Chapters.ToList().FindIndex(c => c.HRef == progress.ChapterHRef)
                : 0;
            if (CurrentChapterIndex < 0) CurrentChapterIndex = 0;
            SavedScrollPosition = progress?.ScrollPosition ?? 0;
            await LoadCurrentChapterAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error initializing reader: {ex}");
            await Shell.Current.DisplayAlert("Erro", "Não foi possível abrir o livro.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCurrentChapterAsync()
    {
        if (Chapters.Count == 0) return;
        try
        {
            if (CurrentSettings.ReadingMode == ReadingMode.Scroll)
            {
                await LoadScrollContentAsync();
                return;
            }

            var chapter = Chapters[CurrentChapterIndex];
            var result = await readingManager.LoadChapterContentAsync(BookId, chapter.HRef);
            var css = settingsManager.GenerateReaderCss(CurrentSettings);
            var fullHtml = HtmlUtility.InjectTags(result.Html, null, css);
            ChapterContent = string.Empty;
            ChapterContent = WriteChapterHtmlFile(fullHtml, result.BaseDirectory);
            HasPreviousChapter = CurrentChapterIndex > 0;
            HasNextChapter = CurrentChapterIndex < Chapters.Count - 1;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error loading chapter: {ex}");
            await Shell.Current.DisplayAlert("Erro", "Não foi possível carregar o conteúdo do capítulo.", "OK");
        }
    }

    private async Task LoadScrollContentAsync()
    {
        if (Chapters.Count == 0) return;
        var css = settingsManager.GenerateReaderCss(CurrentSettings);
        var chapterContents = new List<(string href, string bodyContent)>();
        string baseDirectory = string.Empty;
        foreach (var chapter in Chapters)
        {
            var result = await readingManager.LoadChapterContentAsync(BookId, chapter.HRef);
            baseDirectory = result.BaseDirectory;
            var body = HtmlUtility.ExtractBodyContent(result.Html);
            chapterContents.Add((chapter.HRef, body));
        }
        var fullHtml = HtmlUtility.BuildContinuousScrollHtml(chapterContents, css);
        ChapterContent = string.Empty;
        ChapterContent = WriteChapterHtmlFile(fullHtml, baseDirectory);
        HasPreviousChapter = false;
        HasNextChapter = false;
    }

    public async Task ApplySettingsAsync(ReadingSettings settings)
    {
        CurrentSettings = settings;
        if (Chapters.Count == 0) return;
        if (settings.ReadingMode == ReadingMode.Scroll)
        {
            await LoadScrollContentAsync();
            return;
        }
        var chapter = Chapters[CurrentChapterIndex];
        var result = await readingManager.LoadChapterContentAsync(BookId, chapter.HRef);
        var css = settingsManager.GenerateReaderCss(settings);
        var fullHtml = HtmlUtility.InjectTags(result.Html, null, css);
        ChapterContent = string.Empty;
        ChapterContent = WriteChapterHtmlFile(fullHtml, result.BaseDirectory);
    }

    private static string WriteChapterHtmlFile(string fullHtml, string baseDirectory)
    {
        Directory.CreateDirectory(baseDirectory);
        var filePath = Path.Combine(baseDirectory, "_chapter.html");
        File.WriteAllText(filePath, fullHtml);
        return new Uri(filePath).AbsoluteUri;
    }

    public Task SaveCurrentSettingsAsync() =>
        settingsManager.SaveSettingsAsync(CurrentSettings);

    [RelayCommand]
    private async Task NavigatePreviousAsync()
    {
        if (!HasPreviousChapter) return;
        CurrentChapterIndex--;
        await LoadCurrentChapterAsync();
    }

    [RelayCommand]
    private async Task NavigateNextAsync()
    {
        if (!HasNextChapter) return;
        CurrentChapterIndex++;
        await LoadCurrentChapterAsync();
    }

    public async Task SaveScrollProgressAsync(string chapterHRef, double relativeScroll)
    {
        if (Chapters.Count == 0) return;
        var chapterIndex = Chapters.ToList().FindIndex(c => c.HRef == chapterHRef);
        if (chapterIndex < 0) chapterIndex = 0;
        var progressPercentage = (chapterIndex + Math.Clamp(relativeScroll, 0, 1)) / Chapters.Count * 100;
        await readingManager.SaveProgressAsync(BookId, chapterHRef, relativeScroll, progressPercentage);
    }

    public async Task SaveProgressAsync(double scrollPosition, int currentPage = 0, int totalPages = 0)
    {
        if (Chapters.Count == 0) return;
        var chapter = Chapters[CurrentChapterIndex];
        var chapterFraction = totalPages > 0
            ? (double)(currentPage + 1) / totalPages
            : 1.0;
        var progressPercentage = (CurrentChapterIndex + chapterFraction) / Chapters.Count * 100;
        await readingManager.SaveProgressAsync(BookId, chapter.HRef, scrollPosition, progressPercentage);
    }
}
