using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Binding = System.Windows.Data.Binding;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Converts the System.Drawing.Color to a WPF Brush.
/// </summary>
public class ColorToBrush : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
            return new SolidColorBrush(Colors.Transparent);

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not SolidColorBrush brush)
            return Binding.DoNothing;

        return brush.Color;
    }
}