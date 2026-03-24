using TranslateReader.Models;

namespace TranslateReader.Pages.Controls;

public partial class SettingsOverlay : ContentView
{
    private static readonly string[] FontOptions = ["Georgia", "serif", "sans-serif", "monospace", "OpenDyslexic"];

    public event EventHandler? CloseRequested;
    public event EventHandler<ReadingSettings>? SettingsChanged;

    private ReadingSettings _settings = new();
    private bool _suppressEvents;

    public SettingsOverlay()
    {
        InitializeComponent();
        FontPicker.ItemsSource = FontOptions;
    }

    public void ApplySettings(ReadingSettings settings)
    {
        _settings = settings;
        _suppressEvents = true;

        FontPicker.SelectedItem = settings.FontFamily;
        FontSizeSlider.Value = settings.FontSize;
        LineSpacingSlider.Value = settings.LineSpacing;
        LetterSpacingSlider.Value = settings.LetterSpacing;
        WordSpacingSlider.Value = settings.WordSpacing;

        UpdateLabels();
        UpdateThemeButtonBorders(settings.Theme);

        _suppressEvents = false;
    }

    private void UpdateLabels()
    {
        FontSizeLabel.Text = $"{_settings.FontSize:F0}px";
        LineSpacingLabel.Text = $"{_settings.LineSpacing:F1}";
        LetterSpacingLabel.Text = $"{_settings.LetterSpacing:F1}px";
        WordSpacingLabel.Text = $"{_settings.WordSpacing:F1}px";
    }

    private void UpdateThemeButtonBorders(ThemeType theme)
    {
        LightThemeButton.BorderColor = theme == ThemeType.Light ? Color.FromArgb("#2563EB") : Colors.Transparent;
        DarkThemeButton.BorderColor = theme == ThemeType.Dark ? Color.FromArgb("#60A5FA") : Colors.Transparent;
        SepiaThemeButton.BorderColor = theme == ThemeType.Sepia ? Color.FromArgb("#8B6914") : Colors.Transparent;
    }

    private void NotifySettingsChanged()
    {
        if (!_suppressEvents)
            SettingsChanged?.Invoke(this, _settings);
    }

    private void OnBackdropTapped(object? sender, EventArgs e) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);

    private void OnCloseClicked(object? sender, EventArgs e) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);

    private void OnLightThemeClicked(object? sender, EventArgs e)
    {
        _settings.Theme = ThemeType.Light;
        UpdateThemeButtonBorders(ThemeType.Light);
        NotifySettingsChanged();
    }

    private void OnDarkThemeClicked(object? sender, EventArgs e)
    {
        _settings.Theme = ThemeType.Dark;
        UpdateThemeButtonBorders(ThemeType.Dark);
        NotifySettingsChanged();
    }

    private void OnSepiaThemeClicked(object? sender, EventArgs e)
    {
        _settings.Theme = ThemeType.Sepia;
        UpdateThemeButtonBorders(ThemeType.Sepia);
        NotifySettingsChanged();
    }

    private void OnFontPickerChanged(object? sender, EventArgs e)
    {
        if (FontPicker.SelectedItem is string font)
        {
            _settings.FontFamily = font;
            NotifySettingsChanged();
        }
    }

    private void OnFontSizeChanged(object? sender, ValueChangedEventArgs e)
    {
        _settings.FontSize = Math.Round(e.NewValue);
        FontSizeLabel.Text = $"{_settings.FontSize:F0}px";
        NotifySettingsChanged();
    }

    private void OnLineSpacingChanged(object? sender, ValueChangedEventArgs e)
    {
        _settings.LineSpacing = Math.Round(e.NewValue, 1);
        LineSpacingLabel.Text = $"{_settings.LineSpacing:F1}";
        NotifySettingsChanged();
    }

    private void OnLetterSpacingChanged(object? sender, ValueChangedEventArgs e)
    {
        _settings.LetterSpacing = Math.Round(e.NewValue, 1);
        LetterSpacingLabel.Text = $"{_settings.LetterSpacing:F1}px";
        NotifySettingsChanged();
    }

    private void OnWordSpacingChanged(object? sender, ValueChangedEventArgs e)
    {
        _settings.WordSpacing = Math.Round(e.NewValue, 1);
        WordSpacingLabel.Text = $"{_settings.WordSpacing:F1}px";
        NotifySettingsChanged();
    }
}
