#region Usings

using System;
using System.Globalization;
using System.Windows.Data;

using ScreenToGif.ViewModel;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

#endregion

namespace ScreenToGif.Util.Converters;

// NOTE: This class is needed because by default ExportPanel uses the preset as a data context (which is actually a model-like class)
// instead of real view model classes (and actual VM functionality such as data sources of selections are just in the codebehind).
// This converter returns a specific ViewModel class for presets whose View (eg. separated user controls) rely on a real view model;
// otherwise, returns the original preset instance.
public class PresetToSubViewModelConverter : IValueConverter
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