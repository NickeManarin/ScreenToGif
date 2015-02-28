using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

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

            if (String.IsNullOrEmpty(stringValue))
                return DependencyProperty.UnsetValue;

            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.DecodePixelHeight = 100;
            bi.CacheOption = BitmapCacheOption.OnDemand;
            bi.UriSource = new Uri(stringValue);
            bi.EndInit();
            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }
    }
}
