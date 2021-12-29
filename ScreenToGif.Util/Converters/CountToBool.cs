using ScreenToGif.Domain.Interfaces;
using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// True only if Count > 0.
/// </summary>
public class CountToBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var list = value as List<IFrame>;

        return list?.Count > 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}