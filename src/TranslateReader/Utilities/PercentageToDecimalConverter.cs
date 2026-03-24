using System.Globalization;

namespace TranslateReader.Utilities;

public class PercentageToDecimalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
            return percentage / 100.0;
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
