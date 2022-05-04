using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

/// <summary>
/// MaximumValue MultiValueConverter.
/// [0]: Minimum based on itself.
/// [1]: Maximum based on another control.
/// [2]: Offset (It's used as Maximum - Offset).
/// </summary>
public class MaximumValue : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return DependencyProperty.UnsetValue;

        var minVar = (int)Math.Round(values[0] as double? ?? values[0] as int? ?? 0, MidpointRounding.AwayFromZero);
        var maxVar = (int)Math.Round(values[1] as double? ?? values[1] as int? ?? 0, MidpointRounding.AwayFromZero);

        if (values.Length == 2)
            return minVar > maxVar || double.IsNaN(maxVar) ? minVar : maxVar;

        var offset = values[2] as double? ?? values[2] as int? ?? 0;

        var result = (int)Math.Round(maxVar - offset, MidpointRounding.AwayFromZero);
        return minVar > result || double.IsNaN(maxVar) ? minVar : result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}