using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TranslateReader.Models;
using TranslateReader.PageModels;
using TranslateReader.Serialization;

namespace TranslateReader.Pages;

public partial class ReaderPage : ContentPage
{
    private const uint TocAnimationDurationMs = 260;
    private const uint TranslationIndicatorFadeMs = 200;

    private readonly ReaderPageModel _pageModel;
    private int _currentPage;
    private int _totalPages;
    private bool _goToLastPageOnLoad;
    private bool _isWebViewReady;
    private bool _needsInjection;
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
        SyncNavigationButtons();
        _ = SyncSettingsOverlayAsync();
        _ = EnsureWebViewReadyAsync();
    }

    private async Task EnsureWebViewReadyAsync()
    {
        await Task.Delay(3000);
        if (!_isWebViewReady)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] Fallback: ready message not received after 3s, forcing injection");
            _isWebViewReady = true;
            await InjectChapterAsync();
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        CancelPageTranslation();
        _pageModel.PropertyChanged -= OnPageModelPropertyChanged;
        SettingsOverlay.SettingsChanged -= OnSettingsChanged;
        SettingsOverlay.CloseRequested -= OnSettingsCloseRequested;
        SettingsOverlay.DeleteModelRequested -= OnDeleteModelRequested;
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

    private async void OnHybridMessageReceived(object? sender, HybridWebViewRawMessageReceivedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Hybrid Message Received: {e.Message}");
        if (e.Message == "ready")
        {
            _isWebViewReady = true;
            await InjectChapterAsync();
        }
    }

    private void OnPageModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ReaderPageModel.ChapterContent):
                Dispatcher.Dispatch(() => _ = InjectChapterAsync());
                break;
            case nameof(ReaderPageModel.HasPreviousChapter):
            case nameof(ReaderPageModel.HasNextChapter):
                Dispatcher.Dispatch(SyncNavigationButtons);
                break;
            case nameof(ReaderPageModel.IsSettingsVisible):
                Dispatcher.Dispatch(() => _ = SyncSettingsOverlayAsync());
                break;
            case nameof(ReaderPageModel.IsTranslationModeActive):
                Dispatcher.Dispatch(() => OnTranslationModeChanged());
                break;
            case nameof(ReaderPageModel.IsTocVisible):
                Dispatcher.Dispatch(() => _ = SyncChaptersPanelAsync());
                break;
            case nameof(ReaderPageModel.CurrentChapterIndex):
                Dispatcher.Dispatch(SyncChaptersSelection);
                break;
        }
    }

    private async Task InjectChapterAsync()
    {
        if (!_isWebViewReady)
        {
            _needsInjection = true;
            System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] WebView not ready, deferring injection");
            return;
        }

        if (string.IsNullOrEmpty(_pageModel.ChapterContent))
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] Chapter content empty, skipping injection");
            return;
        }

        _needsInjection = false;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Injecting chapter content (length: {_pageModel.ChapterContent.Length})");
            var mode = IsScrollMode() ? "scroll" : "paginated";

            await ContentWebView.EvaluateJavaScriptAsync($"setMode({JsStr(mode)})");
            await ContentWebView.EvaluateJavaScriptAsync($"applyCss({JsStr(_pageModel.CurrentCss)})");

            // Give CSS and Mode a tiny bit of time to settle if needed
            await Task.Delay(50);

            if (IsScrollMode())
                await SafeInjectHtmlAsync("loadScrollContent", _pageModel.ChapterContent);
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Calling SafeInjectHtmlAsync with loadChapter");
                await SafeInjectHtmlAsync("loadChapter", _pageModel.ChapterContent);
            }

            System.Diagnostics.Debug.WriteLine("[DEBUG_LOG] Injection successful");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error injecting chapter: {ex.Message}");
            return;
        }

        await Task.Delay(300);

        try
        {
            if (IsScrollMode())
            {
                await RestoreScrollPositionAsync();
                return;
            }

            if (!IsPaginatedMode()) return;

            if (_goToLastPageOnLoad)
            {
                _goToLastPageOnLoad = false;
                await GoToLastPageAsync();
            }
            else if (_pageModel.SavedScrollPosition > 0)
            {
                var page = (int)_pageModel.SavedScrollPosition;
                _pageModel.SavedScrollPosition = 0;
                await GoToPageAsync(page);
            }
            else
            {
                await UpdatePageInfoAsync();
                if (_pageModel.IsTranslationModeActive)
                    await TranslateVisiblePageAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error restoring position: {ex}");
        }
    }

    private async void OnTranslationModeChanged()
    {
        if (_pageModel.IsTranslationModeActive)
        {
            // 3px was too subtle to read as "translation mode is on" (user-reported). A visible
            // fade-in on entry makes the state change perceivable without inventing new business
            // logic - the indicator itself already existed, only the entrance was silent.
            // CancelAnimations guards against a rapid on/off/on toggle overlapping two fades.
            TranslationModeIndicator.CancelAnimations();
            TranslationModeIndicator.Opacity = 0;
            await TranslationModeIndicator.FadeToAsync(1, TranslationIndicatorFadeMs, Easing.CubicOut);
            return;
        }

        CancelPageTranslation();
        await ClearTranslationsAsync();
    }

    private void SyncNavigationButtons()
    {
        // Footer is only shown in Paginated mode (PIXEL-SPEC "Reader - footer").
        ReaderFooter.IsVisible = IsPaginatedMode();

        if (IsScrollMode())
        {
            PreviousButton.IsVisible = false;
            NextButton.IsVisible = false;
            PageIndicatorLabel.IsVisible = false;
            PageProgressBar.IsVisible = false;
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
            PageProgressBar.IsVisible = false;
        }
    }

    private Task SyncSettingsOverlayAsync() => _pageModel.IsSettingsVisible
        ? SettingsOverlay.ShowAsync()
        : SettingsOverlay.HideAsync();

    private void OnTocButtonClicked(object? sender, EventArgs e) =>
        _pageModel.IsTocVisible = !_pageModel.IsTocVisible;

    private async void OnChapterSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Chapter chapter)
            await _pageModel.GoToChapterAsync(chapter.OrderIndex);
    }

    private void SyncChaptersSelection()
    {
        if (_pageModel.CurrentChapterIndex < 0 || _pageModel.CurrentChapterIndex >= _pageModel.Chapters.Count)
            return;

        var chapter = _pageModel.Chapters[_pageModel.CurrentChapterIndex];
        if (!ReferenceEquals(ChaptersCollection.SelectedItem, chapter))
            ChaptersCollection.SelectedItem = chapter;
    }

    private Task SyncChaptersPanelAsync() => _pageModel.IsTocVisible
        ? ShowChaptersPanelAsync()
        : HideChaptersPanelAsync();

    private async Task ShowChaptersPanelAsync()
    {
        ChaptersPanel.IsVisible = true;
        ChaptersPanel.Opacity = 0;
        ChaptersPanel.TranslationX = -ChaptersPanel.WidthRequest;

        await Task.WhenAll(
            ChaptersPanel.FadeToAsync(1, TocAnimationDurationMs, Easing.CubicOut),
            ChaptersPanel.TranslateToAsync(0, 0, TocAnimationDurationMs, Easing.CubicOut));
    }

    private async Task HideChaptersPanelAsync()
    {
        await Task.WhenAll(
            ChaptersPanel.FadeToAsync(0, TocAnimationDurationMs, Easing.CubicIn),
            ChaptersPanel.TranslateToAsync(-ChaptersPanel.WidthRequest, 0, TocAnimationDurationMs, Easing.CubicIn));

        ChaptersPanel.IsVisible = false;
    }

    private async void OnPreviousButtonClicked(object? sender, EventArgs e)
    {
        if (IsPaginatedMode())
        {
            if (_currentPage > 0)
            {
                await PrevPageAsync();
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
                await NextPageAsync();
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
            await DisplayAlert("Traducao", "A traducao funciona apenas no modo Paginado. Altere o modo de leitura nas configuracoes.", "OK");
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
            var paragraphs = await EvalJsAsync("getVisibleParagraphs()", ReaderJsonContext.Default.ListVisibleParagraph);
            if (paragraphs is null || paragraphs.Count == 0) return;

            paragraphs = paragraphs.Where(p => !string.IsNullOrWhiteSpace(p.Text)).ToList();
            if (paragraphs.Count == 0) return;

            var results = await _pageModel.TranslateVisibleParagraphsAsync(paragraphs, ct);
            if (ct.IsCancellationRequested || results.Count == 0 || _translationVersion != version) return;

            var items = results.Select(r => new { index = r.Index, translated = r.Translated });
            var itemsJson = JsonSerializer.Serialize(items, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await ContentWebView.EvaluateJavaScriptAsync($"applyTranslations({itemsJson})");
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

    private bool IsScrollMode() =>
        _pageModel.CurrentSettings.ReadingMode == ReadingMode.Scroll;

    private bool IsPaginatedMode() =>
        _pageModel.CurrentSettings.ReadingMode == ReadingMode.Paginated;

    private async Task UpdatePageInfoAsync()
    {
        var info = await EvalJsAsync("getPageInfo()", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
    }

    private async Task GoToPageAsync(int page)
    {
        var info = await EvalJsAsync($"goToPage({page})", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task GoToLastPageAsync()
    {
        var info = await EvalJsAsync("goToLastPage()", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task NextPageAsync()
    {
        var info = await EvalJsAsync("nextPage()", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task PrevPageAsync()
    {
        var info = await EvalJsAsync("prevPage()", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private void ApplyPageInfo(PageInfo? info)
    {
        if (info is null) return;
        _currentPage = info.Current;
        _totalPages = info.Total;
        UpdatePageIndicator();
        Dispatcher.Dispatch(SyncNavigationButtons);
    }

    private void UpdatePageIndicator()
    {
        Dispatcher.Dispatch(() =>
        {
            if (IsPaginatedMode() && _totalPages > 0)
            {
                var chapterNumber = _pageModel.CurrentChapterIndex + 1;
                var chapterTotal = _pageModel.Chapters.Count;
                PageIndicatorLabel.Text =
                    $"Página {_currentPage + 1} / {_totalPages} · Capítulo {chapterNumber} de {chapterTotal}";
                PageIndicatorLabel.IsVisible = true;
                PageProgressBar.Progress = (double)(_currentPage + 1) / _totalPages;
                PageProgressBar.IsVisible = true;
            }
            else
            {
                PageIndicatorLabel.IsVisible = false;
                PageProgressBar.IsVisible = false;
            }
        });
    }

    private async Task SaveScrollInfoAsync()
    {
        try
        {
            var info = await EvalJsAsync("getScrollInfo()", ReaderJsonContext.Default.ScrollInfo);
            if (info is null) return;
            await _pageModel.SaveScrollProgressAsync(info.ChapterHRef, info.RelativeScroll);
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
            await ContentWebView.EvaluateJavaScriptAsync(
                $"scrollToChapter({JsStr(savedHRef)}, {savedPos})");
        }
        _pageModel.SavedScrollPosition = 0;
    }

    private async Task SafeInjectHtmlAsync(string functionName, string html)
    {
        const int chunkSize = 150000; // ~150KB per chunk
        System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] SafeInjectHtmlAsync for {functionName}, total length: {html.Length}");
        if (html.Length <= chunkSize)
        {
            var res = await ContentWebView.EvaluateJavaScriptAsync($"{functionName}({JsStr(html)})");
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] SafeInjectHtmlAsync direct call result: {res}");
            return;
        }

        await ContentWebView.EvaluateJavaScriptAsync("window.__injectionBuffer = ''");
        int chunks = (int)Math.Ceiling((double)html.Length / chunkSize);
        for (int i = 0; i < html.Length; i += chunkSize)
        {
            var chunk = html.Substring(i, Math.Min(chunkSize, html.Length - i));
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Sending chunk {i / chunkSize + 1} of {chunks}");
            var resChunk = await ContentWebView.EvaluateJavaScriptAsync($"appendChunk({JsStr(chunk)})");
            if (resChunk is null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] FAILED to send chunk {i / chunkSize + 1}");
            }
        }
        System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Flushing all chunks for {functionName}");
        var resFlush = await ContentWebView.EvaluateJavaScriptAsync($"flushChunk('{functionName}')");
        System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] SafeInjectHtmlAsync flush result: {resFlush}");
    }

    private async Task<T?> EvalJsAsync<T>(string expression, JsonTypeInfo<T> typeInfo)
    {
        var json = await ContentWebView.EvaluateJavaScriptAsync(expression);
        if (string.IsNullOrEmpty(json) || json is "null" or "undefined")
            return default;
        return JsonSerializer.Deserialize(json, typeInfo);
    }

    private static string JsStr(string? value) =>
        JsonSerializer.Serialize(value ?? string.Empty);
}

/// <summary>
/// Displays a zero-based <see cref="Chapter.OrderIndex"/> as the 1-based ordinal shown in the
/// TOC panel (chapter 1, 2, 3...), matching the mockup numbering.
/// </summary>
internal sealed class OneBasedIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int index ? (index + 1).ToString(culture) : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
