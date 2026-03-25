using System.ComponentModel;
using TranslateReader.Models;
using TranslateReader.PageModels;

namespace TranslateReader.Pages;

public partial class ReaderPage : ContentPage
{
    private readonly ReaderPageModel _pageModel;
    private int _currentPage;
    private int _totalPages;
    private bool _goToLastPageOnLoad;

    public ReaderPage(ReaderPageModel pageModel)
    {
        InitializeComponent();
        _pageModel = pageModel;
        BindingContext = pageModel;
        _pageModel.PropertyChanged += OnPageModelPropertyChanged;
        SettingsOverlay.SettingsChanged += OnSettingsChanged;
        SettingsOverlay.CloseRequested += OnSettingsCloseRequested;
        ContentWebView.Navigated += OnWebViewNavigated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateWebViewSource(_pageModel.ChapterContent);
        SyncNavigationButtons();
        SyncSettingsOverlay();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
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
                await _pageModel.NavigateNextCommand.ExecuteAsync(null);
                return;
            }
            return;
        }
        if (_pageModel.NavigateNextCommand.CanExecute(null))
            await _pageModel.NavigateNextCommand.ExecuteAsync(null);
    }

    private void OnSettingsButtonClicked(object? sender, EventArgs e)
    {
        SettingsOverlay.ApplySettings(_pageModel.CurrentSettings);
        _pageModel.IsSettingsVisible = true;
    }

    private async void OnSettingsChanged(object? sender, ReadingSettings settings) =>
        await _pageModel.ApplySettingsAsync(settings);

    private async void OnSettingsCloseRequested(object? sender, EventArgs e)
    {
        _pageModel.IsSettingsVisible = false;
        await _pageModel.SaveCurrentSettingsAsync();
    }

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
    }

    private void ParsePageInfo(string? json)
    {
        if (string.IsNullOrEmpty(json)) return;
        json = json.Trim('"').Replace("\\\"", "\"");
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(json);
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
            var doc = System.Text.Json.JsonDocument.Parse(json);
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
}
