using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ScreenToGif.Util.Model;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// True only if Count > 0.
    /// </summary>
    public class CountToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = value as List<FrameInfo>;

            if (list == null)
                return false;
                //return DependencyProperty.UnsetValue;

            return list.Count > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
