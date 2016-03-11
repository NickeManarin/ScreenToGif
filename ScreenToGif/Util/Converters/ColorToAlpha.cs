using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters
{
    /// <summary>
    /// Gets the Color given by the binding engine and sets the given alpha value (as hexadecimal).
    /// </summary>
    public class ColorToAlpha : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = value as Color?;
            var alphaAux = parameter as string;

            if (!color.HasValue)
                return value;

            if (String.IsNullOrEmpty(alphaAux))
                return value;

            int alpha = 0;
            if (!int.TryParse(alphaAux, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out alpha))
                return value;

            return Color.FromArgb((byte)alpha, color.Value.R, color.Value.G, color.Value.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
