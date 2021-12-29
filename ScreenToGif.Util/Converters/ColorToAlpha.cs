using System.Globalization;
using System.Windows.Data;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Gets the Color given by the binding engine and sets the given alpha value (as hexadecimal).
/// </summary>
public class ColorToAlpha : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var alphaAux = parameter as string;

        if (value is not Color color)
            return value;

        if (string.IsNullOrEmpty(alphaAux))
            return value;

        if (!int.TryParse(alphaAux, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var alpha))
            return value;

        return Color.FromArgb((byte)alpha, color.R, color.G, color.B);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}