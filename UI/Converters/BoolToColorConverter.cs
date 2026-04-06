using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NMSE.UI.Converters;

/// <summary>
/// Converts a bool to one of two colors. Use ConverterParameter to specify
/// the true color as a hex string (e.g. "#FFDD44"). False returns transparent.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string colorStr)
        {
            if (Color.TryParse(colorStr, out var color))
                return new SolidColorBrush(color);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a double (0-1 range) to an opacity value.
/// </summary>
public class DamageToOpacityConverter : IValueConverter
{
    public static readonly DamageToOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d && d > 0)
            return 0.4;
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
