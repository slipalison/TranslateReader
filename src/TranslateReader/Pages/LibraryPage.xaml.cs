using TranslateReader.PageModels;

namespace TranslateReader.Pages;

public partial class LibraryPage : ContentPage
{
    // 167px card + 20px gap (D-...-6, PIXEL-SPEC "Library - grid de capas").
    private const double AdaptiveCardSlotWidth = 187;
    private readonly LibraryPageModel _pageModel;

    public LibraryPage(LibraryPageModel pageModel)
    {
        InitializeComponent();
        _pageModel = pageModel;
        BindingContext = pageModel;
        SizeChanged += OnPageSizeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _pageModel.LoadBooksCommand.Execute(null);
        _pageModel.LoadTargetLanguageCommand.Execute(null);
        _pageModel.LoadModelStatusCommand.Execute(null);
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone) return;
        if (BooksCollection.Width <= 0) return;

        var span = Math.Max(3, (int)((BooksCollection.Width + 20) / AdaptiveCardSlotWidth));
        if (BooksCollection.ItemsLayout is GridItemsLayout grid && grid.Span != span)
            grid.Span = span;
    }

    // MAUI's FlyoutBase has no cross-platform "show programmatically" API (D-...-7): the
    // MenuFlyout stays native/WinUI-only, so the new dots-three-vertical button opens it via
    // the underlying WinUI FlyoutBase.ShowAttachedFlyout on the tapped element's platform view.
    private void OnCardMenuTapped(object? sender, TappedEventArgs e)
    {
#if WINDOWS
        if (sender is VisualElement view && view.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement platformView)
            Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(platformView);
#endif
    }
}
