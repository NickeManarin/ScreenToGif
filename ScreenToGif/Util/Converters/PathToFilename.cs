using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace ScreenToGif.Util.Converters
{
    public class PathToFilename : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;

            return string.IsNullOrEmpty(path) ? LocalizationHelper.Get("S.Watermark.File.Nothing") : Path.GetFileName(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}