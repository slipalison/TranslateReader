using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateReader.Contracts.Managers;
using TranslateReader.Models;

namespace TranslateReader.PageModels;

public partial class LibraryPageModel(ILibraryManager libraryManager, ITranslationManager translationManager) : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<BookSummary> _books = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isTranslatingBook;

    [ObservableProperty]
    private double _bookTranslationProgress;

    [ObservableProperty]
    private string _translatingBookTitle = string.Empty;

    [ObservableProperty]
    private bool _isModelDownloading;

    [ObservableProperty]
    private double _modelDownloadProgress;

    [ObservableProperty]
    private bool _isModelLoading;

    private CancellationTokenSource? _translationCts;
    private int? _translatingBookId;

    [RelayCommand]
    private async Task LoadBooksAsync()
    {
        IsBusy = true;
        try
        {
            Books = await libraryManager.ListBookSummariesAsync();
        }
        finally
        {
            IsBusy = false;
        }
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
                var popup = new Pages.Controls.TranslateBookPopup(book.Title);
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
            var popup = new Pages.Controls.TranslateBookPopup(book.Title);
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

            var translatedEpubPath = await Task.Run(
                () => translationManager.TranslateBookAsync(book.Id, source, target, tempDirectory, progress, ct), ct);

            try
            {
                await libraryManager.ImportBookAsync(translatedEpubPath);
                await LoadBooksAsync();
            }
            finally
            {
                File.Delete(translatedEpubPath);
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
