using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ScreenToGif.Util.Converters
{
    public class FontToSupportedGliph : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = value as IReadOnlyCollection<FontFamily>;

            if (list == null)
                return DependencyProperty.UnsetValue;

            var returnList = new List<FontFamily>();
            foreach (FontFamily font in list)
            {
                try
                {
                    // Instantiate a TypeFace object with the font settings you want to use
                    Typeface ltypFace = new Typeface(font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                    // Try to create a GlyphTypeface object from the TypeFace object
                    GlyphTypeface lglyphTypeFace;
                    if (ltypFace.TryGetGlyphTypeface(out lglyphTypeFace))
                    {
                        returnList.Add(font);
                    }
                }
                catch (Exception) {}
            }

            return returnList;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
