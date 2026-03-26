using System.ComponentModel;
using System.Text.Json;
using TranslateReader.Models;
using TranslateReader.PageModels;

namespace TranslateReader.Pages;

public partial class ReaderPage : ContentPage
{
    private readonly ReaderPageModel _pageModel;
    private int _currentPage;
    private int _totalPages;
    private bool _goToLastPageOnLoad;
    private CancellationTokenSource? _pageTranslationCts;
    private int _translationVersion;

    public ReaderPage(ReaderPageModel pageModel)
    {
        InitializeComponent();
        _pageModel = pageModel;
        BindingContext = pageModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _pageModel.PropertyChanged += OnPageModelPropertyChanged;
        SettingsOverlay.SettingsChanged += OnSettingsChanged;
        SettingsOverlay.CloseRequested += OnSettingsCloseRequested;
        SettingsOverlay.DeleteModelRequested += OnDeleteModelRequested;
        ContentWebView.Navigated += OnWebViewNavigated;
        UpdateWebViewSource(_pageModel.ChapterContent);
        SyncNavigationButtons();
        SyncSettingsOverlay();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        CancelPageTranslation();
        _pageModel.PropertyChanged -= OnPageModelPropertyChanged;
        SettingsOverlay.SettingsChanged -= OnSettingsChanged;
        SettingsOverlay.CloseRequested -= OnSettingsCloseRequested;
        SettingsOverlay.DeleteModelRequested -= OnDeleteModelRequested;
        ContentWebView.Navigated -= OnWebViewNavigated;
        if (IsScrollMode())
        {
            await SaveScrollInfoAsync();
            return;
        }
        if (IsPaginatedMode())
            await _pageModel.SaveProgressAsync(_currentPage, _currentPage, _totalPages);
        else
            await _pageModel.SaveProgressAsync(scrollPosition: 0);
    }

    private void OnPageModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ReaderPageModel.ChapterContent):
                UpdateWebViewSource(_pageModel.ChapterContent);
                break;
            case nameof(ReaderPageModel.HasPreviousChapter):
            case nameof(ReaderPageModel.HasNextChapter):
                Dispatcher.Dispatch(SyncNavigationButtons);
                break;
            case nameof(ReaderPageModel.IsSettingsVisible):
                Dispatcher.Dispatch(SyncSettingsOverlay);
                break;
            case nameof(ReaderPageModel.IsTranslationModeActive):
                Dispatcher.Dispatch(() => OnTranslationModeChanged());
                break;
        }
    }

    private async void OnTranslationModeChanged()
    {
        if (!_pageModel.IsTranslationModeActive)
        {
            CancelPageTranslation();
            await ClearTranslationsAsync();
        }
    }

    private void SyncNavigationButtons()
    {
        if (IsScrollMode())
        {
            PreviousButton.IsVisible = false;
            NextButton.IsVisible = false;
            PageIndicatorLabel.IsVisible = false;
            return;
        }
        if (IsPaginatedMode())
        {
            PreviousButton.IsVisible = _currentPage > 0 || _pageModel.HasPreviousChapter;
            NextButton.IsVisible = _currentPage < _totalPages - 1 || _pageModel.HasNextChapter;
        }
        else
        {
            PreviousButton.IsVisible = _pageModel.HasPreviousChapter;
            NextButton.IsVisible = _pageModel.HasNextChapter;
            PageIndicatorLabel.IsVisible = false;
        }
    }

    private void SyncSettingsOverlay()
    {
        SettingsOverlay.IsVisible = _pageModel.IsSettingsVisible;
        SettingsOverlay.InputTransparent = !_pageModel.IsSettingsVisible;
    }

    private void UpdateWebViewSource(string content)
    {
        if (string.IsNullOrEmpty(content)) return;

        Dispatcher.Dispatch(() =>
        {
            if (content.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                ContentWebView.Source = new UrlWebViewSource { Url = content };
            else
                ContentWebView.Source = new HtmlWebViewSource { Html = content };
        });
    }

    private async void OnPreviousButtonClicked(object? sender, EventArgs e)
    {
        if (IsPaginatedMode())
        {
            if (_currentPage > 0)
            {
                await EvaluatePageActionAsync("prevPage()");
                return;
            }
            if (_pageModel.HasPreviousChapter)
            {
                _goToLastPageOnLoad = true;
                CancelPageTranslation();
                await _pageModel.NavigatePreviousCommand.ExecuteAsync(null);
                return;
            }
            return;
        }
        if (_pageModel.NavigatePreviousCommand.CanExecute(null))
            await _pageModel.NavigatePreviousCommand.ExecuteAsync(null);
    }

    private async void OnNextButtonClicked(object? sender, EventArgs e)
    {
        if (IsPaginatedMode())
        {
            if (_currentPage < _totalPages - 1)
            {
                await EvaluatePageActionAsync("nextPage()");
                return;
            }
            if (_pageModel.HasNextChapter)
            {
                CancelPageTranslation();
                await _pageModel.NavigateNextCommand.ExecuteAsync(null);
                return;
            }
            return;
        }
        if (_pageModel.NavigateNextCommand.CanExecute(null))
            await _pageModel.NavigateNextCommand.ExecuteAsync(null);
    }

    private async void OnTranslateButtonClicked(object? sender, EventArgs e)
    {
        if (IsScrollMode())
        {
            await DisplayAlert("Tradução", "A tradução funciona apenas no modo Paginado. Altere o modo de leitura nas configurações.", "OK");
            return;
        }

        if (_pageModel.IsTranslationModeActive)
        {
            CancelPageTranslation();
            _pageModel.DeactivateTranslationMode();
            await ClearTranslationsAsync();
            return;
        }

        if (_pageModel.IsTranslating || _pageModel.IsModelDownloading || _pageModel.IsModelLoading) return;

        await _pageModel.TranslateCommand.ExecuteAsync(null);

        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task TranslateVisiblePageAsync()
    {
        CancelPageTranslation();
        _pageTranslationCts = new CancellationTokenSource();
        var ct = _pageTranslationCts.Token;
        var version = ++_translationVersion;

        _pageModel.IsTranslating = true;
        _pageModel.TranslationProgress = 0;

        try
        {
            var json = await ContentWebView.EvaluateJavaScriptAsync("getVisibleParagraphs()");
            var paragraphs = ParseVisibleParagraphs(json);
            if (paragraphs.Count == 0) return;

            var results = await _pageModel.TranslateVisibleParagraphsAsync(paragraphs, ct);
            if (ct.IsCancellationRequested || results.Count == 0 || _translationVersion != version) return;

            var translationsJson = SerializeTranslations(results);
            await ContentWebView.EvaluateJavaScriptAsync($"applyTranslations({translationsJson})");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error translating page: {ex}");
        }
        finally
        {
            if (_translationVersion == version)
                _pageModel.IsTranslating = false;
        }
    }

    private async Task ClearTranslationsAsync()
    {
        try
        {
            await ContentWebView.EvaluateJavaScriptAsync("clearTranslations()");
        }
        catch { }
    }

    private void CancelPageTranslation()
    {
        _pageTranslationCts?.Cancel();
        _pageTranslationCts?.Dispose();
        _pageTranslationCts = null;
    }

    private void OnCancelDownloadClicked(object? sender, EventArgs e) =>
        _pageModel.CancelTranslationCommand.Execute(null);

    private void OnSettingsButtonClicked(object? sender, EventArgs e)
    {
        SettingsOverlay.ApplySettings(_pageModel.CurrentSettings, _pageModel.IsModelAvailable);
        _pageModel.IsSettingsVisible = true;
    }

    private async void OnSettingsChanged(object? sender, ReadingSettings settings) =>
        await _pageModel.ApplySettingsAsync(settings);

    private async void OnSettingsCloseRequested(object? sender, EventArgs e)
    {
        _pageModel.IsSettingsVisible = false;
        await _pageModel.SaveCurrentSettingsAsync();
    }

    private async void OnDeleteModelRequested(object? sender, EventArgs e) =>
        await _pageModel.DeleteModelAsync();

    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (IsScrollMode())
        {
            await Task.Delay(300);
            await RestoreScrollPositionAsync();
            return;
        }
        if (!IsPaginatedMode()) return;
        await Task.Delay(200);
        if (_goToLastPageOnLoad)
        {
            _goToLastPageOnLoad = false;
            await EvaluatePageActionAsync("goToLastPage()");
        }
        else if (_pageModel.SavedScrollPosition > 0)
        {
            var page = (int)_pageModel.SavedScrollPosition;
            _pageModel.SavedScrollPosition = 0;
            await EvaluatePageActionAsync($"goToPage({page})");
        }
        else
        {
            await UpdatePageInfoAsync();
            if (_pageModel.IsTranslationModeActive)
                await TranslateVisiblePageAsync();
        }
    }

    private bool IsScrollMode() =>
        _pageModel.CurrentSettings.ReadingMode == ReadingMode.Scroll;

    private bool IsPaginatedMode() =>
        _pageModel.CurrentSettings.ReadingMode == ReadingMode.Paginated;

    private async Task UpdatePageInfoAsync()
    {
        var json = await ContentWebView.EvaluateJavaScriptAsync("getPageInfo()");
        ParsePageInfo(json);
        UpdatePageIndicator();
        Dispatcher.Dispatch(SyncNavigationButtons);
    }

    private async Task EvaluatePageActionAsync(string jsCall)
    {
        var json = await ContentWebView.EvaluateJavaScriptAsync(jsCall);
        ParsePageInfo(json);
        UpdatePageIndicator();
        Dispatcher.Dispatch(SyncNavigationButtons);

        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private void ParsePageInfo(string? json)
    {
        if (string.IsNullOrEmpty(json)) return;
        json = json.Trim('"').Replace("\\\"", "\"");
        try
        {
            var doc = JsonDocument.Parse(json);
            _currentPage = doc.RootElement.GetProperty("current").GetInt32();
            _totalPages = doc.RootElement.GetProperty("total").GetInt32();
        }
        catch { }
    }

    private void UpdatePageIndicator()
    {
        Dispatcher.Dispatch(() =>
        {
            if (IsPaginatedMode() && _totalPages > 0)
            {
                PageIndicatorLabel.Text = $"{_currentPage + 1} / {_totalPages}";
                PageIndicatorLabel.IsVisible = true;
            }
            else
            {
                PageIndicatorLabel.IsVisible = false;
            }
        });
    }

    private async Task SaveScrollInfoAsync()
    {
        try
        {
            var json = await ContentWebView.EvaluateJavaScriptAsync("getScrollInfo()");
            if (string.IsNullOrEmpty(json)) return;
            json = json.Trim('"').Replace("\\\"", "\"");
            var doc = JsonDocument.Parse(json);
            var href = doc.RootElement.GetProperty("chapterHRef").GetString() ?? string.Empty;
            var relScroll = doc.RootElement.GetProperty("relativeScroll").GetDouble();
            await _pageModel.SaveScrollProgressAsync(href, relScroll);
        }
        catch { }
    }

    private async Task RestoreScrollPositionAsync()
    {
        if (_pageModel.Chapters.Count == 0) return;
        var savedHRef = _pageModel.Chapters[_pageModel.CurrentChapterIndex].HRef;
        var savedPos = _pageModel.SavedScrollPosition;
        if (savedPos > 0 || _pageModel.CurrentChapterIndex > 0)
        {
            var jsHRef = savedHRef.Replace("'", "\\'");
            var posStr = savedPos.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await ContentWebView.EvaluateJavaScriptAsync($"scrollToChapter('{jsHRef}',{posStr})");
        }
        _pageModel.SavedScrollPosition = 0;
    }

    private static List<VisibleParagraph> ParseVisibleParagraphs(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        json = json.Trim('"').Replace("\\\"", "\"");
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray()
                .Select(e => new VisibleParagraph(
                    e.GetProperty("index").GetInt32(),
                    e.GetProperty("text").GetString() ?? string.Empty))
                .Where(p => !string.IsNullOrWhiteSpace(p.Text))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeTranslations(IReadOnlyList<TranslatedParagraph> results)
    {
        var items = results.Select(r => new { index = r.Index, translated = r.Translated });
        return JsonSerializer.Serialize(items);
    }
}
