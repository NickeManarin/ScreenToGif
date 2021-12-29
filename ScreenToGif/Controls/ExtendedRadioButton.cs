using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenToGif.Controls;

public class ExtendedRadioButton : RadioButton
{
    #region Variables

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(ExtendedRadioButton), new FrameworkPropertyMetadata());

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExtendedRadioButton), new FrameworkPropertyMetadata());
        
    public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ExtendedRadioButton), new FrameworkPropertyMetadata(26.0));
        
    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ExtendedRadioButton), new FrameworkPropertyMetadata(26.0));
        
    public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ExtendedRadioButton), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
        FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    /// <summary>
    /// The icon of the radio button.
    /// </summary>
    [Description("The icon of the radio button.")]
    public Brush Icon
    {
        get => (Brush)GetValue(IconProperty);
        set => SetCurrentValue(IconProperty, value);
    }

    /// <summary>
    /// The text of the button.
    /// </summary>
    [Description("The text of the button.")]
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetCurrentValue(TextProperty, value);
    }

    /// <summary>
    /// The maximum size of the image.
    /// </summary>
    [Description("The maximum size of the image.")]
    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        set => SetCurrentValue(ContentWidthProperty, value);
    }

    /// <summary>
    /// The maximum size of the image.
    /// </summary>
    [Description("The maximum size of the image.")]
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetCurrentValue(ContentHeightProperty, value);
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

    static ExtendedRadioButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedRadioButton), new FrameworkPropertyMetadata(typeof(ExtendedRadioButton)));
    }
}