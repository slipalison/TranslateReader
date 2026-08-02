namespace TranslateReader;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // D-...-3 (app-redesign): the mockups only exist in dark, so app chrome is dark-only.
        // The reading theme (Light/Dark/Sepia) is a separate concern rendered inside the WebView
        // by ThemeEngine and is not affected by this.
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
