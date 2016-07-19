using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    public class StageToCanvas : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stage = value as Stage?;

            if (!stage.HasValue)
                return (Canvas)Application.Current.FindResource("Vector.Record");

            switch (stage)
            {
                case Stage.Stopped:
                    return (Canvas)Application.Current.FindResource("Vector.Record");
                case Stage.Recording:
                    return (Canvas)Application.Current.FindResource("Vector.Pause");
                case Stage.Paused:
                    return (Canvas)Application.Current.FindResource("Vector.Record");
                case Stage.Snapping:
                    return (Canvas)Application.Current.FindResource("Vector.Camera.Add");
            }

            return (Canvas)Application.Current.FindResource("Vector.Record");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
