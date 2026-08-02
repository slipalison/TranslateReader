using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateReader.Contracts.Managers;
using TranslateReader.Models;

namespace TranslateReader.PageModels;

public partial class LibraryPageModel(
    ILibraryManager libraryManager,
    ITranslationManager translationManager,
    ISettingsManager settingsManager) : ObservableObject
{
    [ObservableProperty]
    public partial IReadOnlyList<BookSummary> Books { get; set; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRecentFilterActive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasContinueReadingBook))]
    public partial BookSummary? ContinueReadingBook { get; set; }

    [ObservableProperty]
    public partial string TargetLanguage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModelSizeDisplay))]
    [NotifyPropertyChangedFor(nameof(ModelStatusText))]
    public partial TranslationModelStatus? ModelStatus { get; set; }

    [ObservableProperty]
    public partial bool IsTranslatingBook { get; set; }

    [ObservableProperty]
    public partial double BookTranslationProgress { get; set; }

    [ObservableProperty]
    public partial string TranslatingBookTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsModelDownloading { get; set; }

    [ObservableProperty]
    public partial double ModelDownloadProgress { get; set; }

    [ObservableProperty]
    public partial bool IsModelLoading { get; set; }

    private CancellationTokenSource? _translationCts;
    private int? _translatingBookId;
    private int _loadBooksGeneration;

    public bool HasContinueReadingBook => ContinueReadingBook is not null;

    public string ModelSizeDisplay => ModelStatus is null
        ? string.Empty
        : $"{ModelStatus.SizeBytes / 1024d / 1024d / 1024d:0.0} GB";

    public string ModelStatusText => ModelStatus is null
        ? string.Empty
        : ModelStatus.IsDownloaded ? "Modelo baixado" : "Modelo não baixado";

    partial void OnSearchQueryChanged(string value) => LoadBooksCommand.Execute(null);

    [RelayCommand]
    private async Task LoadBooksAsync()
    {
        var generation = Interlocked.Increment(ref _loadBooksGeneration);
        IsBusy = true;
        try
        {
            var recentBooks = await libraryManager.ListRecentBookSummariesAsync();
            var books = IsRecentFilterActive
                ? recentBooks
                : await libraryManager.ListBookSummariesAsync(SearchQuery);

            // A newer call (from a later keystroke) may have started and finished while this
            // one was awaiting - discard this stale result instead of overwriting Books/
            // ContinueReadingBook with out-of-order data (no debounce per D-...-7).
            if (generation != Volatile.Read(ref _loadBooksGeneration))
                return;

            ContinueReadingBook = recentBooks.Count > 0 ? recentBooks[0] : null;
            Books = books;
        }
        finally
        {
            // Same generation guard as the Books/ContinueReadingBook write above: a stale call
            // finishing after a newer one has already started must not hide the busy indicator
            // while the newer call is still in flight.
            if (generation == Volatile.Read(ref _loadBooksGeneration))
                IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ShowLibraryBooksAsync()
    {
        IsRecentFilterActive = false;
        return LoadBooksAsync();
    }

    [RelayCommand]
    private Task ShowRecentBooksAsync()
    {
        IsRecentFilterActive = true;
        return LoadBooksAsync();
    }

    [RelayCommand]
    private async Task LoadTargetLanguageAsync()
    {
        var settings = await settingsManager.LoadSettingsAsync();
        TargetLanguage = settings.TargetLanguage;
    }

    [RelayCommand]
    private async Task LoadModelStatusAsync()
    {
        ModelStatus = await translationManager.GetSelectedModelStatusAsync();
    }

    [RelayCommand]
    private async Task ImportBookAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Selecione um EPUB",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, ["org.idpf.epub-container"] },
                { DevicePlatform.Android, ["application/epub+zip"] },
                { DevicePlatform.WinUI, [".epub"] },
                { DevicePlatform.MacCatalyst, ["epub"] }
            })
        });

        if (result is null)
            return;

        IsBusy = true;
        try
        {
            await libraryManager.ImportBookAsync(result.FullPath);
            await LoadBooksAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBookAsync(BookSummary book)
    {
        var confirmed = await Shell.Current.DisplayAlert(
            "Excluir livro",
            $"Deseja excluir \"{book.Title}\"? Esta ação não pode ser desfeita.",
            "Excluir",
            "Cancelar");

        if (!confirmed)
            return;

        await libraryManager.DeleteBookAsync(book.Id);
        await LoadBooksAsync();
    }

    [RelayCommand]
    private async Task OpenBookAsync(BookSummary book)
    {
        await Shell.Current.GoToAsync($"reader?bookId={book.Id}");
    }

    [RelayCommand]
    private async Task TranslateBookAsync(BookSummary book)
    {
        if (IsTranslatingBook || IsModelDownloading || IsModelLoading) return;

        string source, target;
        var existingJob = await translationManager.GetActiveTranslationJobAsync(book.Id);
        if (existingJob is not null)
        {
            var resume = await Shell.Current.DisplayAlert(
                "Tradução pendente",
                "Deseja retomar a tradução anterior?",
                "Retomar",
                "Nova tradução");
            if (resume)
            {
                source = existingJob.SourceLanguage;
                target = existingJob.TargetLanguage;
            }
            else
            {
                var popup = new Pages.Controls.TranslateBookPopup(book);
                var page = Shell.Current.CurrentPage;
                var popupResult = await page.ShowPopupAsync<(string, string)?>(popup, new PopupOptions
                {
                    CanBeDismissedByTappingOutsideOfPopup = true
                });
                if (popupResult.WasDismissedByTappingOutsideOfPopup || popupResult.Result is not (string s, string t))
                    return;
                source = s;
                target = t;
            }
        }
        else
        {
            var popup = new Pages.Controls.TranslateBookPopup(book);
            var page = Shell.Current.CurrentPage;
            var popupResult = await page.ShowPopupAsync<(string, string)?>(popup, new PopupOptions
            {
                CanBeDismissedByTappingOutsideOfPopup = true
            });
            if (popupResult.WasDismissedByTappingOutsideOfPopup || popupResult.Result is not (string s, string t))
                return;
            source = s;
            target = t;
        }

        _translationCts?.Cancel();
        _translationCts?.Dispose();
        _translationCts = new CancellationTokenSource();
        var ct = _translationCts.Token;

        TranslatingBookTitle = book.Title;
        _translatingBookId = book.Id;
        try
        {
            await EnsureModelReadyAsync(ct);

            IsTranslatingBook = true;
            BookTranslationProgress = 0;

            var tempDirectory = Path.Combine(Path.GetTempPath(), "TranslateReader_temp");
            var progress = new Progress<BookTranslationProgress>(p =>
                MainThread.BeginInvokeOnMainThread(() => BookTranslationProgress = p.OverallProgress));

            var translation = await Task.Run(
                () => translationManager.TranslateBookAsync(book.Id, source, target, tempDirectory, progress, ct), ct);

            try
            {
                await libraryManager.ImportBookAsync(translation.EpubPath);
                await LoadBooksAsync();
            }
            finally
            {
                File.Delete(translation.EpubPath);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error translating book: {ex}");
            await Shell.Current.DisplayAlert("Erro", "Não foi possível traduzir o livro.", "OK");
        }
        finally
        {
            IsTranslatingBook = false;
            BookTranslationProgress = 0;
            TranslatingBookTitle = string.Empty;
            _translatingBookId = null;
            IsModelDownloading = false;
            IsModelLoading = false;
        }
    }

    private async Task EnsureModelReadyAsync(CancellationToken ct)
    {
        IsModelDownloading = true;
        ModelDownloadProgress = 0;
        try
        {
            var progress = new Progress<double>(p =>
                MainThread.BeginInvokeOnMainThread(() => ModelDownloadProgress = p));
            await Task.Run(() => translationManager.DownloadModelIfNeededAsync(progress, ct), ct);
        }
        finally
        {
            IsModelDownloading = false;
        }

        IsModelLoading = true;
        try
        {
            await Task.Run(() => translationManager.InitializeEngineIfNeededAsync(ct), ct);
        }
        finally
        {
            IsModelLoading = false;
        }
    }

    [RelayCommand]
    private async Task PauseBookTranslationAsync()
    {
        if (_translatingBookId.HasValue)
            await translationManager.PauseTranslationAsync(_translatingBookId.Value);

        _translationCts?.Cancel();
        _translationCts?.Dispose();
        _translationCts = null;
    }
}
