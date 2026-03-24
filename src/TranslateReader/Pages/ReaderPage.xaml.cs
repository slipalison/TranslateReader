using System.ComponentModel;
using TranslateReader.Models;
using TranslateReader.PageModels;

namespace TranslateReader.Pages;

public partial class ReaderPage : ContentPage
{
    private readonly ReaderPageModel _pageModel;

    public ReaderPage(ReaderPageModel pageModel)
    {
        InitializeComponent();
        _pageModel = pageModel;
        BindingContext = pageModel;
        _pageModel.PropertyChanged += OnPageModelPropertyChanged;
        SettingsOverlay.SettingsChanged += OnSettingsChanged;
        SettingsOverlay.CloseRequested += OnSettingsCloseRequested;
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
        PreviousButton.IsVisible = _pageModel.HasPreviousChapter;
        NextButton.IsVisible = _pageModel.HasNextChapter;
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
        if (_pageModel.NavigatePreviousCommand.CanExecute(null))
            await _pageModel.NavigatePreviousCommand.ExecuteAsync(null);
    }

    private async void OnNextButtonClicked(object? sender, EventArgs e)
    {
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
}
