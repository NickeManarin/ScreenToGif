using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Translator.Controls;

/// <summary>
/// MenuItem with an image to the left.
/// </summary>
public class ImageMenuItem : MenuItem
{
    #region Variables

    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(ImageMenuItem), new FrameworkPropertyMetadata());

    public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageMenuItem), new FrameworkPropertyMetadata(15.0));

    public static readonly DependencyProperty HasImageProperty = DependencyProperty.Register("HasImage", typeof(bool), typeof(ImageMenuItem), new FrameworkPropertyMetadata(false));

    #endregion

    #region Properties

    /// <summary>
    /// The Image of the button.
    /// </summary>
    [Description("The Image of the button.")]
    public UIElement Image
    {
        get { return (UIElement)GetValue(ImageProperty); }
        set
        {
            SetCurrentValue(ImageProperty, value);

            //Has Image.
            SetCurrentValue(HasImageProperty, value != null);
        }
    }

    /// <summary>
    /// The maximum size of the image.
    /// </summary>
    [Description("The maximum size of the image.")]
    public double MaxSize
    {
        get { return (double)GetValue(MaxSizeProperty); }
        set { SetCurrentValue(MaxSizeProperty, value); }
    }

    /// <summary>
    /// The maximum size of the image.
    /// </summary>
    [Description("The maximum size of the image.")]
    public bool HasImage
    {
        get { return (bool)GetValue(HasImageProperty); }
        set { SetCurrentValue(HasImageProperty, value); }
    }

    #endregion

    static ImageMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageMenuItem), new FrameworkPropertyMetadata(typeof(ImageMenuItem)));
    }
}
