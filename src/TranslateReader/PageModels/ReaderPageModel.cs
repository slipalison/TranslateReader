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

    public async Task ApplySettingsAsync(ReadingSettings settings)
    {
        CurrentSettings = settings;
        if (Chapters.Count == 0) return;
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

    public async Task SaveProgressAsync(double scrollPosition)
    {
        if (Chapters.Count == 0) return;
        var chapter = Chapters[CurrentChapterIndex];
        var progressPercentage = Chapters.Count > 0
            ? (double)(CurrentChapterIndex + 1) / Chapters.Count * 100
            : 0;
        await readingManager.SaveProgressAsync(BookId, chapter.HRef, scrollPosition, progressPercentage);
    }
}
