using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ScreenToGif.Controls;

public class ExtendedRepeatButton : RepeatButton
{
    #region Variables

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(ExtendedRepeatButton));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExtendedRepeatButton), new FrameworkPropertyMetadata());

    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ExtendedRepeatButton), new FrameworkPropertyMetadata(double.NaN));

    public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ExtendedRepeatButton), new FrameworkPropertyMetadata(double.NaN));

    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ExtendedRepeatButton), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    /// <summary>
    /// The icon of the button as a brush.
    /// </summary>
    [Description("The icon of the button as a brush."), Category("Common")]
    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetCurrentValue(IconProperty, value);
    }

    /// <summary>
    /// The text of the button.
    /// </summary>
    [Description("The text of the button."), Category("Common")]
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetCurrentValue(TextProperty, value);
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

    /// <summary>
    /// The TextWrapping property controls whether or not text wraps 
    /// when it reaches the flow edge of its containing block box. 
    /// </summary>
    public TextWrapping TextWrapping
    {
        get => (TextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    #endregion

    static ExtendedRepeatButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedRepeatButton), new FrameworkPropertyMetadata(typeof(ExtendedRepeatButton)));
    }
}