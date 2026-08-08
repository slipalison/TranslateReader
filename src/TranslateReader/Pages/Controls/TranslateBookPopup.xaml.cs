using CommunityToolkit.Maui.Extensions;
using TranslateReader.Models;

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

    public TranslateBookPopup(BookSummary book)
    {
        InitializeComponent();
        BookMetaLabel.FormattedText = BuildBookMeta(book);
        SourcePicker.ItemsSource = LanguageOptions;
        TargetPicker.ItemsSource = LanguageOptions;
        SourcePicker.SelectedIndex = 0;
        TargetPicker.SelectedIndex = 1;
    }

    private static FormattedString BuildBookMeta(BookSummary book) =>
        new()
        {
            Spans =
            {
                new Span
                {
                    Text = book.Title,
                    FontSize = 13,
                    TextColor = ResourceColor("ColorText")
                },
                new Span { Text = "\n" },
                new Span
                {
                    Text = BuildSubtitle(book),
                    FontSize = 11,
                    TextColor = ResourceColor("Neutral500")
                }
            }
        };

    private static string BuildSubtitle(BookSummary book)
    {
        var chapterWord = book.TotalChapters == 1 ? "capítulo" : "capítulos";
        return string.IsNullOrWhiteSpace(book.Author)
            ? $"{book.TotalChapters} {chapterWord}"
            : $"{book.Author} · {book.TotalChapters} {chapterWord}";
    }

    private static Color ResourceColor(string key) =>
        Application.Current!.Resources.TryGetValue(key, out var value) && value is Color color
            ? color
            : throw new InvalidOperationException($"Design token '{key}' not found.");

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
