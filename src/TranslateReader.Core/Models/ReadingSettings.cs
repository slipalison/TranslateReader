namespace TranslateReader.Models;

public class ReadingSettings
{
    public ThemeType Theme { get; set; } = ThemeType.Light;
    public string FontFamily { get; set; } = "Georgia";
    public double FontSize { get; set; } = 18;
    public double LineSpacing { get; set; } = 1.6;
    public double LetterSpacing { get; set; } = 0;
    public double WordSpacing { get; set; } = 0;
    public ReadingMode ReadingMode { get; set; } = ReadingMode.Scroll;
}
