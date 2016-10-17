using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters
{
    public class ColorOrTransparent : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selection = value as bool?;

            if (!selection.HasValue)
                return new SolidColorBrush(Colors.Transparent);

            return new SolidColorBrush(selection.Value ? Colors.Transparent : UserSettings.All.RecorderBackground);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
