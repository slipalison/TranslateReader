using TranslateReader.PageModels;

namespace TranslateReader.Pages;

public partial class LibraryPage : ContentPage
{
    private const double DesiredItemWidth = 150;
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
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0) return;
        var span = Math.Max(2, (int)(Width / DesiredItemWidth));
        if (BooksCollection.ItemsLayout is GridItemsLayout grid && grid.Span != span)
            grid.Span = span;
    }
}
