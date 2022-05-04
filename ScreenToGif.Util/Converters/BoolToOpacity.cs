using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// Bool to Opacity property converter. Returns 0 if true.
/// </summary>
public class BoolToOpacity : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolean)
            return DependencyProperty.UnsetValue;

        return boolean ? 0 : 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}