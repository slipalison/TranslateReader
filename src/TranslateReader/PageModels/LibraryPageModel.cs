using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateReader.Contracts.Managers;
using TranslateReader.Models;

namespace TranslateReader.PageModels;

public partial class LibraryPageModel(ILibraryManager libraryManager) : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<BookSummary> _books = [];

    [ObservableProperty]
    private bool _isBusy;

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
}
