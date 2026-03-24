using System.Globalization;

namespace TranslateReader.Utilities;

public class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
            return null;

        return ImageSource.FromFile(path);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
