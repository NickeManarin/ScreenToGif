using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenToGif.Controls;

/// <summary>
/// MenuItem with an image to the left.
/// </summary>
public class ExtendedMenuItem : MenuItem
{
    #region Variables

    public new static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(Icon_Changed));

    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(16d));

    public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(16d));

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HasIconProperty = DependencyProperty.Register(nameof(HasIcon), typeof(bool), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty DarkModeProperty = DependencyProperty.Register(nameof(DarkMode), typeof(bool), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsOverNonClientAreaProperty = DependencyProperty.Register(nameof(IsOverNonClientArea), typeof(bool), typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(false));

    #endregion

    #region Properties

    /// <summary>
    /// The icon of the button as a Brush.
    /// </summary>
    [Description("The icon of the button as a Brush.")]
    public new Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set
        {
            SetCurrentValue(IconProperty, value);
            SetCurrentValue(HasIconProperty, value != null);
        }
    }

    /// <summary>
    /// The height of the button content.
    /// </summary>
    [Description("The height of the button content."), Category("Common")]
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetCurrentValue(ContentHeightProperty, value);
    }

    /// <summary>
    /// The width of the button content.
    /// </summary>
    [Description("The width of the button content."), Category("Common")]
    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        set => SetCurrentValue(ContentWidthProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetCurrentValue(TextWrappingProperty, value);
    }

    /// <summary>
    /// True if the menu item contains an icon.
    /// </summary>
    [Description("True if the menu item contains an icon.")]
    public bool HasIcon
    {
        get => (bool)GetValue(HasIconProperty);
        set => SetCurrentValue(HasIconProperty, value);
    }

    /// <summary>
    /// True if the menu should adjust itself for dark mode.
    /// </summary>
    [Description("True if the menu should adjust itself for dark mode.")]
    public bool DarkMode
    {
        get => (bool)GetValue(DarkModeProperty);
        set => SetCurrentValue(DarkModeProperty, value);
    }

    /// <summary>
    /// True if the button is being drawn on top of the non client area.
    /// </summary>
    [Description("True if the button is being drawn on top of the non client area.")]
    public bool IsOverNonClientArea
    {
        get => (bool)GetValue(IsOverNonClientAreaProperty);
        set => SetCurrentValue(IsOverNonClientAreaProperty, value);
    }

    #endregion

    #region Property Changed

    private static void Icon_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ExtendedMenuItem)d).HasIcon = e.NewValue != null;
    }

    #endregion

    static ExtendedMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedMenuItem), new FrameworkPropertyMetadata(typeof(ExtendedMenuItem)));
    }
}