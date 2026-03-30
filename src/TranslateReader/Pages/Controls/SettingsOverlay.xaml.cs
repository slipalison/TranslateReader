using TranslateReader.Models;

namespace TranslateReader.Pages.Controls;

public partial class SettingsOverlay : ContentView
{
    private static readonly string[] FontOptions = ["Georgia", "serif", "sans-serif", "monospace", "OpenDyslexic"];

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

    public event EventHandler? CloseRequested;
    public event EventHandler<ReadingSettings>? SettingsChanged;
    public event EventHandler? DeleteModelRequested;

    private ReadingSettings _settings = new();
    private bool _suppressEvents;
    private bool _isModelAvailable;

    public SettingsOverlay()
    {
        InitializeComponent();
        FontPicker.ItemsSource = FontOptions;
        SourceLanguagePicker.ItemsSource = LanguageOptions;
        TargetLanguagePicker.ItemsSource = LanguageOptions;
    }

    public void ApplySettings(ReadingSettings settings, bool isModelAvailable = false)
    {
        _settings = settings;
        _isModelAvailable = isModelAvailable;
        _suppressEvents = true;

        FontPicker.SelectedItem = settings.FontFamily;
        SourceLanguagePicker.SelectedItem = settings.SourceLanguage;
        TargetLanguagePicker.SelectedItem = settings.TargetLanguage;
        FontSizeSlider.Value = settings.FontSize;
        LineSpacingSlider.Value = settings.LineSpacing;
        LetterSpacingSlider.Value = settings.LetterSpacing;
        WordSpacingSlider.Value = settings.WordSpacing;

        UpdateLabels();
        UpdateThemeButtonBorders(settings.Theme);
        UpdateReadingModeButtonBorders(settings.ReadingMode);
        UpdateModelButtonBorders(settings.TranslationModelName);
        UpdateModelStatus();

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

    private void UpdateReadingModeButtonBorders(ReadingMode mode)
    {
        ScrollModeButton.BorderColor = mode == ReadingMode.Scroll ? Color.FromArgb("#2563EB") : Colors.Transparent;
        PaginatedModeButton.BorderColor = mode == ReadingMode.Paginated ? Color.FromArgb("#2563EB") : Colors.Transparent;
    }

    private void OnScrollModeClicked(object? sender, EventArgs e)
    {
        _settings.ReadingMode = ReadingMode.Scroll;
        UpdateReadingModeButtonBorders(ReadingMode.Scroll);
        NotifySettingsChanged();
    }

    private void OnPaginatedModeClicked(object? sender, EventArgs e)
    {
        _settings.ReadingMode = ReadingMode.Paginated;
        UpdateReadingModeButtonBorders(ReadingMode.Paginated);
        NotifySettingsChanged();
    }

    private void UpdateModelButtonBorders(string modelName)
    {
        GemmaModelButton.BorderColor = modelName == "gemma-2-2b" ? Color.FromArgb("#2563EB") : Colors.Transparent;
        QwenModelButton.BorderColor = modelName == "qwen-2.5-3b" ? Color.FromArgb("#2563EB") : Colors.Transparent;
        PhiModelButton.BorderColor = modelName == "phi-3.5" ? Color.FromArgb("#2563EB") : Colors.Transparent;
    }

    private void UpdateModelStatus()
    {
        ModelStatusLabel.Text = _isModelAvailable ? "Modelo baixado" : "Modelo nao baixado";
        DeleteModelButton.IsVisible = _isModelAvailable;
    }

    private void OnGemmaClicked(object? sender, EventArgs e)
    {
        _settings.TranslationModelName = "gemma-2-2b";
        UpdateModelButtonBorders("gemma-2-2b");
        NotifySettingsChanged();
    }

    private void OnQwenClicked(object? sender, EventArgs e)
    {
        _settings.TranslationModelName = "qwen-2.5-3b";
        UpdateModelButtonBorders("qwen-2.5-3b");
        NotifySettingsChanged();
    }

    private void OnPhiClicked(object? sender, EventArgs e)
    {
        _settings.TranslationModelName = "phi-3.5";
        UpdateModelButtonBorders("phi-3.5");
        NotifySettingsChanged();
    }

    private void OnSourceLanguageChanged(object? sender, EventArgs e)
    {
        if (SourceLanguagePicker.SelectedItem is string lang)
        {
            _settings.SourceLanguage = lang;
            NotifySettingsChanged();
        }
    }

    private void OnTargetLanguageChanged(object? sender, EventArgs e)
    {
        if (TargetLanguagePicker.SelectedItem is string lang)
        {
            _settings.TargetLanguage = lang;
            NotifySettingsChanged();
        }
    }

    private void OnDeleteModelClicked(object? sender, EventArgs e) =>
        DeleteModelRequested?.Invoke(this, EventArgs.Empty);
}
