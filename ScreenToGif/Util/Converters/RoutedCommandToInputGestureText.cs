using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenToGif.Util.Converters
{
    public class RoutedCommandToInputGestureTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            RoutedUICommand command = value as RoutedUICommand;

            InputGestureCollection col = command?.InputGestures;

            if (col != null && (col.Count >= 1))
            {
                // Search for the first key gesture
                for (int i = 0; i < col.Count; i++)
                {
                    KeyGesture keyGesture = ((IList)col)[i] as KeyGesture;

                    if (keyGesture != null)
                    {
                        return String.Format("{0}\n({1})", command.Text, keyGesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture));
                    }
                } 
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
