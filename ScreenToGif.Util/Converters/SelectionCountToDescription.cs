using System.Globalization;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;

namespace ScreenToGif.Util.Converters;

public class SelectionCountToDescription : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int count)
            return Binding.DoNothing;

        return count > 1 ? LocalizationHelper.GetWithFormat("S.SaveAs.Partial.Mode.Selection.Plural", "{0} frames selected.", count) : 
            count == 1 ? LocalizationHelper.Get("S.SaveAs.Partial.Mode.Selection.Singular") : LocalizationHelper.Get("S.SaveAs.Partial.Mode.Selection.None");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}