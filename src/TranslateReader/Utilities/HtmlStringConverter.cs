using System.Globalization;

namespace TranslateReader.Utilities;

public class HtmlStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string content || string.IsNullOrEmpty(content))
            return new HtmlWebViewSource();
        if (content.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return new UrlWebViewSource { Url = content };
        return new HtmlWebViewSource { Html = content };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
