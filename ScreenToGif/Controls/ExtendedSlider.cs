using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls;

internal class ExtendedSlider : Slider
{
    public static readonly DependencyProperty ShowNumbersProperty = DependencyProperty.Register(nameof(ShowNumbers), typeof(bool), typeof(ExtendedSlider), new PropertyMetadata(default(bool)));

    public bool ShowNumbers
    {
        get => (bool) GetValue(ShowNumbersProperty);
        set => SetValue(ShowNumbersProperty, value);
    }
}