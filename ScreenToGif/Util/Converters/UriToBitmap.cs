using System;
using System.IO;
using System.Windows.Data;
using ScreenToGif.ImageUtil;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// URI to BitmapImage converter.
    /// </summary>
    public class UriToBitmap : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var stringValue = value as string;
            var size = parameter as string;

            if (string.IsNullOrEmpty(stringValue))
                return null; 

            if (!File.Exists(stringValue))
                return null;

            if (!string.IsNullOrEmpty(size))
                return stringValue.SourceFrom(System.Convert.ToInt32(size));

            return stringValue.SourceFrom();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}