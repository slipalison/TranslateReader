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
    private const string SnipPrefix = "snip|";
    private const string SnipTogglePrefix = "snip-toggle|";
    private const string SnipRemovePrefix = "snip-remove|";

    private static readonly IReadOnlyDictionary<string, string> SnippetLanguageShortCodes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["English"] = "EN",
            ["Brazilian Portuguese (PT-BR)"] = "PT-BR",
            ["Spanish"] = "ES",
            ["French"] = "FR",
            ["German"] = "DE",
            ["Italian"] = "IT",
            ["Japanese"] = "JA",
            ["Korean"] = "KO",
            ["Chinese (Simplified)"] = "ZH",
            ["Russian"] = "RU",
        };

    private readonly ReaderPageModel _pageModel;
    private int _currentPage;
    private int _totalPages;
    private bool _goToLastPageOnLoad;
    private bool _isWebViewReady;
    private bool _needsInjection;
    private CancellationTokenSource? _pageTranslationCts;
    private CancellationTokenSource? _snippetCts;
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
        _snippetCts?.Cancel();
        _snippetCts?.Dispose();
        _snippetCts = null;
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
            return;
        }

        try
        {
            if (e.Message.StartsWith(SnipPrefix, StringComparison.Ordinal))
                await HandleSnipRequestAsync(e.Message[SnipPrefix.Length..]);
            else if (e.Message.StartsWith(SnipTogglePrefix, StringComparison.Ordinal))
                await HandleSnipToggleAsync(e.Message[SnipTogglePrefix.Length..]);
            else if (e.Message.StartsWith(SnipRemovePrefix, StringComparison.Ordinal))
                await HandleSnipRemoveAsync(e.Message[SnipRemovePrefix.Length..]);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error handling snippet message: {ex}");
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

        try
        {
            await SetupSnippetLayerAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Error setting up snippet layer: {ex}");
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
            // D-2026-08-09-snippet-translation-3: the two layers never overlay the same paragraph
            // at once, so the snippet layer steps out before the paragraph translation steps in.
            await ContentWebView.EvaluateJavaScriptAsync("unmountSnippetLayer()");
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
            await ContentWebView.EvaluateJavaScriptAsync("mountSnippetLayer()");
        }
        catch { }
    }

    private void CancelPageTranslation()
    {
        _pageTranslationCts?.Cancel();
        _pageTranslationCts?.Dispose();
        _pageTranslationCts = null;
    }

    // A run's chapterHRef arrives null from the WebView only in paginated mode (a single
    // always-mounted root that never learns which chapter it currently holds); scroll mode's
    // roots already know their own href, so this only ever fills the paginated gap.
    private List<SnippetRequest> FillCurrentChapter(IReadOnlyList<SnippetRequest> requests)
    {
        if (requests.All(r => r.ChapterHRef is not null))
            return requests.ToList();

        var chapterHRef = ResolveCurrentChapterHRef();
        return requests
            .Select(r => r.ChapterHRef is null ? r with { ChapterHRef = chapterHRef } : r)
            .ToList();
    }

    private string ResolveCurrentChapterHRef() =>
        _pageModel.Chapters.Count == 0
            ? string.Empty
            : _pageModel.Chapters[_pageModel.CurrentChapterIndex].HRef;

    private async Task HandleSnipRequestAsync(string json)
    {
        var requests = JsonSerializer.Deserialize(json, ReaderJsonContext.Default.ListSnippetRequest);
        if (requests is null || requests.Count == 0) return;

        var filled = FillCurrentChapter(requests);
        var keys = filled.Select(r => $"{r.ChapterHRef}:{r.ParagraphIndex}:{r.SentenceStart}:{r.SentenceEnd}").ToList();
        var keysJson = JsonSerializer.Serialize(keys, ReaderJsonContext.Default.ListString);
        await ContentWebView.EvaluateJavaScriptAsync($"setSnippetLoading({keysJson})");

        _snippetCts?.Cancel();
        _snippetCts?.Dispose();
        _snippetCts = new CancellationTokenSource();
        var ct = _snippetCts.Token;

        IReadOnlyList<SnippetTranslation> results;
        try
        {
            results = await _pageModel.TranslateSnippetsAsync(filled, ct);
        }
        catch (OperationCanceledException)
        {
            // This run was superseded by a newer selection (or the model download overlay's
            // Cancel button); its own placeholders must stop pulsing even though the app keeps
            // going for whatever replaced it. The exception still flows to the caller (csharp.md
            // §1: cancellation is never swallowed here, only cleaned up after).
            await ContentWebView.EvaluateJavaScriptAsync($"clearSnippetLoading({keysJson})");
            throw;
        }

        if (results.Count == 0)
        {
            // The PageModel already converted the failure into a friendly DisplayAlert at its
            // single exception boundary (csharp.md §1); this only undoes the loading placeholder
            // so it does not pulse forever with no feedback.
            await ContentWebView.EvaluateJavaScriptAsync($"clearSnippetLoading({keysJson})");
            return;
        }

        var resultsJson = JsonSerializer.Serialize(results.ToList(), ReaderJsonContext.Default.ListSnippetTranslation);
        await ContentWebView.EvaluateJavaScriptAsync($"applySnippetTranslation({resultsJson})");
    }

    private async Task HandleSnipToggleAsync(string json)
    {
        var request = JsonSerializer.Deserialize(json, ReaderJsonContext.Default.SnippetToggleRequest);
        if (request is null) return;
        await _pageModel.SetSnippetShowingOriginalAsync(request);
    }

    private async Task HandleSnipRemoveAsync(string json)
    {
        var request = JsonSerializer.Deserialize(json, ReaderJsonContext.Default.SnippetRemoveRequest);
        if (request is null) return;
        await _pageModel.RemoveSnippetAsync(request);
    }

    private SnippetLabels BuildSnippetLabels()
    {
        var isPhone = DeviceInfo.Current.Idiom == DeviceIdiom.Phone;
        var theme = _pageModel.CurrentThemeColors;
        return new SnippetLabels(
            SelectHint: isPhone
                ? "Toque em um período; outro toque adiciona"
                : "Toque em um período; toque em outro para estender a seleção",
            ExtendTip: "toque em outro período para estender",
            SentenceOne: isPhone ? "período" : "período selecionado",
            SentenceMany: isPhone ? "períodos" : "períodos selecionados",
            TranslateSnip: isPhone ? "Traduzir" : "Traduzir trecho",
            ExtendSel: "Estender ao próximo período",
            ShrinkSel: "Reduzir seleção",
            OnlySentence: "único período deste parágrafo",
            ToggleSnip: "Alternar original / tradução",
            RemoveSnip: "Descartar tradução",
            LangMap: SnippetLanguageShortCodes,
            Theme: new SnippetTheme(theme.Background, theme.Accent),
            SourceLanguage: _pageModel.CurrentSettings.SourceLanguage,
            TargetLanguage: _pageModel.CurrentSettings.TargetLanguage);
    }

    // Scroll mode mounts every chapter's content at once, so a snippet saved in any of them must
    // come back; paginated mode only ever has the current chapter's paragraphs on screen.
    private async Task<List<SnippetTranslation>> LoadVisibleSnippetsAsync()
    {
        var results = new List<SnippetTranslation>();
        if (IsScrollMode())
        {
            foreach (var chapter in _pageModel.Chapters)
                results.AddRange(await _pageModel.LoadSnippetsAsync(chapter.HRef));
            return results;
        }

        var chapterHRef = ResolveCurrentChapterHRef();
        if (!string.IsNullOrEmpty(chapterHRef))
            results.AddRange(await _pageModel.LoadSnippetsAsync(chapterHRef));
        return results;
    }

    private async Task SetupSnippetLayerAsync()
    {
        var idiom = DeviceInfo.Current.Idiom == DeviceIdiom.Phone ? "phone" : "desktop";
        await ContentWebView.EvaluateJavaScriptAsync("clearSnippetSelection()");
        await ContentWebView.EvaluateJavaScriptAsync(
            $"document.documentElement.dataset.idiom = {JsStr(idiom)}");

        var labelsJson = JsonSerializer.Serialize(BuildSnippetLabels(), ReaderJsonContext.Default.SnippetLabels);
        await ContentWebView.EvaluateJavaScriptAsync($"setSnippetLabels({labelsJson})");
        await ContentWebView.EvaluateJavaScriptAsync("mountSnippetLayer()");

        var snippets = await LoadVisibleSnippetsAsync();
        var snippetsJson = JsonSerializer.Serialize(snippets, ReaderJsonContext.Default.ListSnippetTranslation);
        await ContentWebView.EvaluateJavaScriptAsync($"restoreSnippets({snippetsJson})");
    }

    private void OnCancelDownloadClicked(object? sender, EventArgs e)
    {
        // The download/load overlay is shared by paragraph translation and snippet translation
        // (ReaderPageModel.TranslateSnippetsAsync reuses IsModelDownloading/IsModelLoading), so
        // cancelling it must also cancel an in-flight snippet run, not just the paragraph one.
        _snippetCts?.Cancel();
        _pageModel.CancelTranslationCommand.Execute(null);
    }

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
        await ContentWebView.EvaluateJavaScriptAsync("clearSnippetSelection()");
        var info = await EvalJsAsync($"goToPage({page})", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task GoToLastPageAsync()
    {
        await ContentWebView.EvaluateJavaScriptAsync("clearSnippetSelection()");
        var info = await EvalJsAsync("goToLastPage()", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task NextPageAsync()
    {
        await ContentWebView.EvaluateJavaScriptAsync("clearSnippetSelection()");
        var info = await EvalJsAsync("nextPage()", ReaderJsonContext.Default.PageInfo);
        ApplyPageInfo(info);
        if (_pageModel.IsTranslationModeActive)
            await TranslateVisiblePageAsync();
    }

    private async Task PrevPageAsync()
    {
        await ContentWebView.EvaluateJavaScriptAsync("clearSnippetSelection()");
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
