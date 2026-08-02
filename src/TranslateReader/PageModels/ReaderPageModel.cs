using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateReader.Contracts.Managers;
using TranslateReader.Models;
using TranslateReader.Utilities;

namespace TranslateReader.PageModels;

[QueryProperty(nameof(BookId), "bookId")]
public partial class ReaderPageModel(
    IReadingManager readingManager,
    ISettingsManager settingsManager,
    ITranslationManager translationManager) : ObservableObject
{
    [ObservableProperty]
    public partial int BookId { get; set; }

    [ObservableProperty]
    public partial Book? Book { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Chapter> Chapters { get; set; } = [];

    [ObservableProperty]
    public partial int CurrentChapterIndex { get; set; }

    [ObservableProperty]
    public partial string ChapterContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentCss { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ChapterSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasPreviousChapter { get; set; }

    [ObservableProperty]
    public partial bool HasNextChapter { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsTocVisible { get; set; }

    [ObservableProperty]
    public partial bool IsTranslating { get; set; }

    [ObservableProperty]
    public partial double TranslationProgress { get; set; }

    [ObservableProperty]
    public partial bool IsTranslationModeActive { get; set; }

    [ObservableProperty]
    public partial bool IsModelDownloading { get; set; }

    [ObservableProperty]
    public partial double ModelDownloadProgress { get; set; }

    [ObservableProperty]
    public partial bool IsModelLoading { get; set; }

    private CancellationTokenSource? _translationCts;

    public ReadingSettings CurrentSettings { get; private set; } = new();

    public bool IsModelAvailable { get; private set; }

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
            CurrentCss = settingsManager.GenerateReaderCss(CurrentSettings);
            var bodyHtml = HtmlUtility.ExtractBodyContent(result.Html);
            ChapterContent = string.Empty;
            ChapterContent = bodyHtml;
            HasPreviousChapter = CurrentChapterIndex > 0;
            HasNextChapter = CurrentChapterIndex < Chapters.Count - 1;
            UpdateChapterSubtitle();
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
        CurrentCss = settingsManager.GenerateReaderCss(CurrentSettings);
        var chapterContents = new List<(string href, string bodyContent)>();
        foreach (var chapter in Chapters)
        {
            var result = await readingManager.LoadChapterContentAsync(BookId, chapter.HRef);
            var body = HtmlUtility.ExtractBodyContent(result.Html);
            chapterContents.Add((chapter.HRef, body));
        }
        ChapterContent = string.Empty;
        ChapterContent = HtmlUtility.BuildContinuousScrollHtml(chapterContents);
        HasPreviousChapter = false;
        HasNextChapter = false;
        UpdateChapterSubtitle();
    }

    // Desktop shows the author, mobile drops it to fit the compact header
    // (PIXEL-SPEC "Reader - top bar" / "Reader mobile").
    private void UpdateChapterSubtitle()
    {
        var chapterNumber = CurrentChapterIndex + 1;
        var totalChapters = Chapters.Count;
        ChapterSubtitle = DeviceInfo.Current.Idiom == DeviceIdiom.Phone
            ? $"Cap. {chapterNumber} de {totalChapters}"
            : $"Capítulo {chapterNumber} de {totalChapters} — {Book?.Author}";
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
        CurrentCss = settingsManager.GenerateReaderCss(settings);
        var bodyHtml = HtmlUtility.ExtractBodyContent(result.Html);
        ChapterContent = string.Empty;
        ChapterContent = bodyHtml;
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

    public async Task GoToChapterAsync(int index)
    {
        if (index < 0 || index >= Chapters.Count) return;

        if (index != CurrentChapterIndex)
        {
            CurrentChapterIndex = index;
            await LoadCurrentChapterAsync();
        }

        IsTocVisible = false;
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

    [RelayCommand]
    private async Task TranslateAsync()
    {
        if (Chapters.Count == 0 || CurrentSettings.ReadingMode == ReadingMode.Scroll) return;
        if (IsTranslating || IsModelDownloading || IsModelLoading) return;

        if (IsTranslationModeActive)
        {
            DeactivateTranslationMode();
            return;
        }

        _translationCts?.Cancel();
        _translationCts?.Dispose();
        _translationCts = new CancellationTokenSource();
        var ct = _translationCts.Token;

        try
        {
            await EnsureModelDownloadedAsync(ct);
            IsTranslationModeActive = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error preparing translation: {ex}");
            await Shell.Current.DisplayAlert("Erro", "Não foi possível preparar a tradução.", "OK");
        }
    }

    public async Task<IReadOnlyList<TranslatedParagraph>> TranslateVisibleParagraphsAsync(
        IReadOnlyList<VisibleParagraph> paragraphs, CancellationToken ct)
    {
        if (!IsTranslationModeActive || paragraphs.Count == 0)
            return [];

        var results = new List<TranslatedParagraph>();
        var chapter = Chapters[CurrentChapterIndex];

        await Task.Run(async () =>
        {
            await foreach (var p in translationManager.TranslateParagraphsAsync(BookId, chapter.HRef, CurrentSettings.SourceLanguage, CurrentSettings.TargetLanguage, paragraphs, ct))
            {
                results.Add(p);
                MainThread.BeginInvokeOnMainThread(() => TranslationProgress = p.Progress);
            }
        }, ct);

        return results;
    }

    private async Task EnsureModelDownloadedAsync(CancellationToken ct)
    {
        try
        {
            IsModelDownloading = true;
            ModelDownloadProgress = 0;
            var progress = new Progress<double>(p => ModelDownloadProgress = p);
            await Task.Run(() => translationManager.DownloadModelIfNeededAsync(progress, ct), ct);
        }
        finally
        {
            IsModelDownloading = false;
        }

        try
        {
            IsModelLoading = true;
            await Task.Run(() => translationManager.InitializeEngineIfNeededAsync(ct), ct);
        }
        finally
        {
            IsModelLoading = false;
        }

        IsModelAvailable = true;
    }

    [RelayCommand]
    private void CancelTranslation()
    {
        _translationCts?.Cancel();
        _translationCts?.Dispose();
        _translationCts = null;
        DeactivateTranslationMode();
        IsModelDownloading = false;
        IsModelLoading = false;
        ModelDownloadProgress = 0;
    }

    public void DeactivateTranslationMode()
    {
        IsTranslationModeActive = false;
        IsTranslating = false;
        TranslationProgress = 0;
    }

    public async Task DeleteModelAsync()
    {
        await translationManager.DeleteModelAsync();
        IsModelAvailable = false;
        DeactivateTranslationMode();
    }
}
