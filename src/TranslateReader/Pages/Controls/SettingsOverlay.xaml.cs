using TranslateReader.Models;

namespace TranslateReader.Pages.Controls;

public partial class SettingsOverlay : ContentView
{
    private const uint AnimationDurationMs = 260;
    private const double BackdropOpacity = 0.4;

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

    private static readonly Color SelectedIndicatorColor = (Color)Application.Current!.Resources["ColorAccent"];

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

    /// <summary>
    /// Reveals the overlay: backdrop fades in and the panel slides in (from the right on
    /// Desktop, from the bottom otherwise), matching the layout <see cref="PanelBorder"/>
    /// takes for the current <see cref="DeviceIdiom"/>.
    /// </summary>
    public async Task ShowAsync()
    {
        IsVisible = true;
        InputTransparent = false;
        Backdrop.Opacity = 0;
        PositionPanelOffscreen();

        await Task.WhenAll(
            Backdrop.FadeToAsync(BackdropOpacity, AnimationDurationMs, Easing.CubicOut),
            PanelBorder.TranslateToAsync(0, 0, AnimationDurationMs, Easing.CubicOut));
    }

    /// <summary>
    /// Hides the overlay: backdrop fades out and the panel slides back off-screen, then the
    /// control is collapsed and made input-transparent.
    /// </summary>
    public async Task HideAsync()
    {
        await Task.WhenAll(
            Backdrop.FadeToAsync(0, AnimationDurationMs, Easing.CubicIn),
            AnimatePanelOffscreenAsync());

        IsVisible = false;
        InputTransparent = true;
    }

    private static bool IsDesktopIdiom => DeviceInfo.Current.Idiom == DeviceIdiom.Desktop;

    private static double ScreenWidth =>
        DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density;

    private static double ScreenHeight =>
        DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density;

    private void PositionPanelOffscreen()
    {
        PanelBorder.TranslationX = IsDesktopIdiom ? ScreenWidth : 0;
        PanelBorder.TranslationY = IsDesktopIdiom ? 0 : ScreenHeight;
    }

    private Task AnimatePanelOffscreenAsync() => IsDesktopIdiom
        ? PanelBorder.TranslateToAsync(ScreenWidth, 0, AnimationDurationMs, Easing.CubicIn)
        : PanelBorder.TranslateToAsync(0, ScreenHeight, AnimationDurationMs, Easing.CubicIn);

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

    private static readonly Color UnselectedCardBorderColor = (Color)Application.Current!.Resources["ColorDivider"];

    private void UpdateThemeButtonBorders(ThemeType theme)
    {
        LightThemeButton.BorderColor = theme == ThemeType.Light ? SelectedIndicatorColor : UnselectedCardBorderColor;
        DarkThemeButton.BorderColor = theme == ThemeType.Dark ? SelectedIndicatorColor : UnselectedCardBorderColor;
        SepiaThemeButton.BorderColor = theme == ThemeType.Sepia ? SelectedIndicatorColor : UnselectedCardBorderColor;
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

    private static readonly Color InactiveSegmentTextColor = (Color)Application.Current!.Resources["ColorText"];

    private void UpdateReadingModeButtonBorders(ReadingMode mode)
    {
        var scrollActive = mode == ReadingMode.Scroll;
        var paginatedActive = mode == ReadingMode.Paginated;

        ScrollModeButton.TextColor = scrollActive ? SelectedIndicatorColor : InactiveSegmentTextColor;
        PaginatedModeButton.TextColor = paginatedActive ? SelectedIndicatorColor : InactiveSegmentTextColor;

        // Mutating FontImageSource.Color on an already-rendered instance does not reliably
        // repaint the glyph on WinUI: the platform handler only re-rasterizes when the whole
        // ImageSource is replaced, not on an in-place property change (see dotnet/maui#8826
        // for the same FontImageSource-color-not-applied family on another platform handler).
        // Reassigning Button.ImageSource with a fresh instance forces the redraw.
        ScrollModeButton.ImageSource = CloneSegmentIcon(ScrollModeIcon, scrollActive);
        PaginatedModeButton.ImageSource = CloneSegmentIcon(PaginatedModeIcon, paginatedActive);
    }

    private static FontImageSource CloneSegmentIcon(FontImageSource template, bool isActive) => new()
    {
        FontFamily = template.FontFamily,
        Glyph = template.Glyph,
        Size = template.Size,
        Color = isActive ? SelectedIndicatorColor : InactiveSegmentTextColor
    };

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

    private static readonly Color UnselectedRowBorderColor = (Color)Application.Current!.Resources["Neutral800"];
    private static readonly Color UnselectedRowIconColor = (Color)Application.Current!.Resources["Neutral500"];
    private static readonly Color SelectedRowBackground = (Color)Application.Current!.Resources["AccentTint08"];

    private void UpdateModelButtonBorders(string modelName)
    {
        SetModelRowSelected(GemmaModelButton, GemmaModelRadioIcon, modelName == "gemma-2-2b");
        SetModelRowSelected(QwenModelButton, QwenModelRadioIcon, modelName == "qwen-2.5-3b");
        SetModelRowSelected(PhiModelButton, PhiModelRadioIcon, modelName == "phi-3.5");
        SetModelRowSelected(HyMtModelButton, HyMtModelRadioIcon, modelName == "hy-mt1.5-1.8b");
        SetModelRowSelected(HyMt2ModelButton, HyMt2ModelRadioIcon, modelName == "hy-mt2-1.8b");
    }

    private static void SetModelRowSelected(Border row, Label radioIcon, bool isSelected)
    {
        row.Stroke = isSelected ? SelectedIndicatorColor : UnselectedRowBorderColor;
        row.BackgroundColor = isSelected ? SelectedRowBackground : Colors.Transparent;
        radioIcon.FontFamily = isSelected ? "PhosphorFill" : "Phosphor";
        radioIcon.Text = isSelected ? "\uE184" : "\uE18A";
        radioIcon.TextColor = isSelected ? SelectedIndicatorColor : UnselectedRowIconColor;
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

    private void OnHyMtClicked(object? sender, EventArgs e)
    {
        _settings.TranslationModelName = "hy-mt1.5-1.8b";
        UpdateModelButtonBorders("hy-mt1.5-1.8b");
        NotifySettingsChanged();
    }

    private void OnHyMt2Clicked(object? sender, EventArgs e)
    {
        _settings.TranslationModelName = "hy-mt2-1.8b";
        UpdateModelButtonBorders("hy-mt2-1.8b");
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
