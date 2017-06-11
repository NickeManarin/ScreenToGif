using System;
using System.Globalization;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    public class IntToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value as int?;

            if (!number.HasValue|| number.Value == 0)
                return "";

            return number.Value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}