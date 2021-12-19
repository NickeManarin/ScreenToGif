#region Usings

using System;
using System.Globalization;
using System.Windows.Data;

using ScreenToGif.Model.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.ViewModel;

#endregion

namespace ScreenToGif.Util.Converters
{
    // NOTE: This class is needed because ExportPanel uses the preset model classes by default so there is no clean ViewModel layer
    // This converter returns a ViewModel class for presets whose View relies on a ViewModel; otherwise, returns the original model instance.
    public class PresetToViewModelConverter : IValueConverter
    {
        #region Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is KGySoftGifPreset kGySoftGifPreset
                ? new KGySoftGifOptionsViewModel(kGySoftGifPreset)
                : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        #endregion
    }
}