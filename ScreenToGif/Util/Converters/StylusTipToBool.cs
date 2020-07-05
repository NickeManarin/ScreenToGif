using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Ink;

namespace ScreenToGif.Util.Converters
{
    public class StylusTipToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tip = value as StylusTip?;
            var param = parameter as string;

            if (!tip.HasValue || param == null)
                return DependencyProperty.UnsetValue;

            return tip.Value.ToString().Contains(param);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selection = value as bool?;
            var param = parameter as string;

            if (!selection.HasValue || !selection.Value)
                return DependencyProperty.UnsetValue;

            if (string.IsNullOrEmpty(param))
                return StylusTip.Rectangle;

            return param.Equals("Ellipse") ? StylusTip.Ellipse : StylusTip.Rectangle;
        }
    }
}