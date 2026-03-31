using CommunityToolkit.Maui.Extensions;

namespace TranslateReader.Pages.Controls;

public partial class TranslateBookPopup : ContentView
{
    private static readonly string[] LanguageOptions =
    [
        "English",
        "Brazilian Portuguese (PT-BR)",
        "Spanish",
        "French",
        "German",
        "Italian",
        "Japanese",
        "Korean",
        "Chinese (Simplified)",
        "Russian"
    ];

    public TranslateBookPopup(string bookTitle)
    {
        InitializeComponent();
        BookTitleLabel.Text = bookTitle;
        SourcePicker.ItemsSource = LanguageOptions;
        TargetPicker.ItemsSource = LanguageOptions;
        SourcePicker.SelectedIndex = 0;
        TargetPicker.SelectedIndex = 1;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        var page = GetParentPage();
        if (page is not null)
            await page.ClosePopupAsync<(string, string)?>(null);
    }

    private async void OnTranslateClicked(object? sender, EventArgs e)
    {
        if (SourcePicker.SelectedItem is not string source || TargetPicker.SelectedItem is not string target)
            return;

        var page = GetParentPage();
        if (page is not null)
            await page.ClosePopupAsync<(string, string)?>((source, target));
    }

    private Page? GetParentPage()
    {
        Element? current = this;
        while (current is not null)
        {
            if (current is Page page)
                return page;
            current = current.Parent;
        }
        return Shell.Current?.CurrentPage;
    }
}
