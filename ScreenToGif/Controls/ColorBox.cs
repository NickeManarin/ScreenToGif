using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Controls;

public class ColorBox : ButtonBase
{
    #region Properties

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(ColorBox), new PropertyMetadata(default(Color), SelectedColor_Changed));
    public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(nameof(SelectedBrush), typeof(SolidColorBrush), typeof(ColorBox), new PropertyMetadata(default(SolidColorBrush)));
    public static readonly DependencyProperty AllowTransparencyProperty = DependencyProperty.Register(nameof(AllowTransparency), typeof(bool), typeof(ColorBox), new PropertyMetadata(true));
    public static readonly DependencyProperty IgnoreEventProperty = DependencyProperty.Register(nameof(IgnoreEvent), typeof(bool), typeof(ColorBox), new PropertyMetadata(false));

    public Color SelectedColor
    {
        get => (Color) GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public SolidColorBrush SelectedBrush
    {
        get => (SolidColorBrush)GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    public bool AllowTransparency
    {
        get => (bool)GetValue(AllowTransparencyProperty);
        set => SetValue(AllowTransparencyProperty, value);
    }

    public bool IgnoreEvent
    {
        get => (bool)GetValue(IgnoreEventProperty);
        set => SetValue(IgnoreEventProperty, value);
    }

    #endregion

    static ColorBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorBox), new FrameworkPropertyMetadata(typeof(ColorBox)));
    }

    protected override void OnClick()
    {
        SelectColor();

        base.OnClick();
    }

    private void SelectColor()
    {
        var colorPicker = new ColorSelector(SelectedColor, AllowTransparency);
        var result = colorPicker.ShowDialog();

        if (result.HasValue && result.Value)
            SelectedColor = colorPicker.SelectedColor;
    }

    private static void SelectedColor_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is ColorBox box))
            return;

        box.SelectedBrush = new SolidColorBrush(box.SelectedColor);
        box.RaiseColorChangedEvent();
    }

    #region Custom Events

    public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(nameof(ColorChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColorBox));

    public event RoutedEventHandler ColorChanged
    {
        add => AddHandler(ColorChangedEvent, value);
        remove => RemoveHandler(ColorChangedEvent, value);
    }

    public void RaiseColorChangedEvent()
    {
        if (ColorChangedEvent == null || IgnoreEvent)
            return;

        RaiseEvent(new RoutedEventArgs(ColorChangedEvent));
    }

    #endregion
}