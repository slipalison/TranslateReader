using TranslateReader.Pages;

namespace TranslateReader;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("reader", typeof(ReaderPage));
    }
}
