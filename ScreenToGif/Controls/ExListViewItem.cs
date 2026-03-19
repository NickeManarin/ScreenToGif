using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls;

public class ExListViewItem : ListViewItem
{
    public static readonly DependencyProperty IsPressedProperty = DependencyProperty.Register(nameof(IsPressed), typeof(bool), typeof(ExListViewItem), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(ExListViewItem), new FrameworkPropertyMetadata());

    public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ExListViewItem), new FrameworkPropertyMetadata(26.0));

    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ExListViewItem), new FrameworkPropertyMetadata(26.0));

    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

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

    static ExListViewItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ExListViewItem), new FrameworkPropertyMetadata(typeof(ExListViewItem)));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (IsEnabled)
        {
            IsPressed = true;
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (IsMouseCaptured)
            ReleaseMouseCapture();
        
        IsPressed = false;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        IsPressed = false;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        // Optional: if you want press to cancel when leaving the item
        if (IsMouseCaptured && !Mouse.LeftButton.HasFlag(MouseButtonState.Pressed))
            IsPressed = false;
    }
}