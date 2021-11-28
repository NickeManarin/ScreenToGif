using System.Windows;
using System.Windows.Media;

namespace ScreenToGif.Windows.Other;

public partial class RegionMagnifier : Window
{
    #region Properties

    public static readonly DependencyProperty LeftPositionProperty = DependencyProperty.Register(nameof(LeftPosition), typeof(double), typeof(RegionMagnifier), new PropertyMetadata(double.NaN));
    public static readonly DependencyProperty TopPositionProperty = DependencyProperty.Register(nameof(TopPosition), typeof(double), typeof(RegionMagnifier), new PropertyMetadata(double.NaN));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(ImageSource), typeof(RegionMagnifier), new PropertyMetadata(null));

    public double LeftPosition
    {
        get => (double)GetValue(LeftPositionProperty);
        set => SetValue(LeftPositionProperty, value);
    }

    public double TopPosition
    {
        get => (double)GetValue(TopPositionProperty);
        set => SetValue(TopPositionProperty, value);
    }

    public ImageSource Image
    {
        get => (ImageSource)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    #endregion

    public RegionMagnifier()
    {
        InitializeComponent();
    }
}