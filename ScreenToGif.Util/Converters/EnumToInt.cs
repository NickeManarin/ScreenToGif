using System;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters;

public class EnumToInt: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value == null)
            return null;

        //Converts int to enum.
        if (targetType.IsEnum)
            return Enum.ToObject(targetType, value);

        //Converts enum to int.
        return value.GetType().IsEnum ? System.Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType())) : null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        //Performs the same conversion in both directions.
        return Convert(value, targetType, parameter, culture);
    }
}