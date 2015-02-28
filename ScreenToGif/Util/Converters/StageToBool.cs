using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ScreenToGif.Util.Enum;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// True when the Stage is Stopped.
    /// </summary>
    public class StageToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stage = value as Stage?;

            if (!stage.HasValue)
                return DependencyProperty.UnsetValue;

            return stage == Stage.Stopped;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
