using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters
{
    public class StageToCanvas : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Stage stage))
                return (Brush)Application.Current.FindResource("Vector.Record");

            switch (stage)
            {
                case Stage.Stopped:
                    return (Brush)Application.Current.FindResource("Vector.Record");
                case Stage.Recording:
                    return (Brush)Application.Current.FindResource("Vector.Pause");
                case Stage.Paused:
                    return (Brush)Application.Current.FindResource("Vector.Record");
                case Stage.Snapping:
                    return (Brush)Application.Current.FindResource("Vector.Camera.Add");
            }

            return (Brush)Application.Current.FindResource("Vector.Record");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}