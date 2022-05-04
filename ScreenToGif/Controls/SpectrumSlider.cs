using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Controls;
//Bug: If the user drags quickly the Thumb and release afterwards, the OnAfterSelection event is not triggered.

#region SpectrumSlider

/// <summary>
/// Spectrum Slider.
/// </summary>
public class SpectrumSlider : Slider
{
    #region Private Fields

    private ColorThumb _colorThumb;
    private Rectangle _spectrumRectangle;
    private LinearGradientBrush _pickerBrush;

    public delegate void AfterSelectingEventHandler();

    public event AfterSelectingEventHandler AfterSelecting;

    #endregion

    #region Properties

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(SpectrumSlider), new PropertyMetadata(Colors.Transparent));
    public static readonly DependencyProperty IsAlphaSpectrumProperty = DependencyProperty.Register(nameof(IsAlphaSpectrum), typeof(bool), typeof(SpectrumSlider), new PropertyMetadata(false));
    public static readonly DependencyProperty SpectrumColorProperty = DependencyProperty.Register(nameof(SpectrumColor), typeof(Color), typeof(SpectrumSlider), new PropertyMetadata(default(Color), SpectrumColor_ChangedCallback));

    /// <summary>
    /// Current selected Color.
    /// </summary>
    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    /// <summary>
    /// True if the spectrum will display the same color but under different alpha values.
    /// </summary>
    public bool IsAlphaSpectrum
    {
        get => (bool)GetValue(IsAlphaSpectrumProperty);
        set => SetValue(IsAlphaSpectrumProperty, value);
    }

    /// <summary>
    /// The color used by the alpha sectrum.
    /// </summary>
    public Color SpectrumColor
    {
        get => (Color)GetValue(SpectrumColorProperty);
        set => SetValue(SpectrumColorProperty, value);
    }

    #endregion

    static SpectrumSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumSlider), new FrameworkPropertyMetadata(typeof(SpectrumSlider)));
    }

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _spectrumRectangle = GetTemplateChild("PART_SpectrumDisplay") as Rectangle;
        _colorThumb = GetTemplateChild("Thumb") as ColorThumb;

        if (_colorThumb != null)
        {
            _colorThumb.PreviewMouseLeftButtonUp += ColorThumb_MouseLeftButtonUp;
            _colorThumb.MouseEnter += ColorThumb_MouseEnter;
        }

        UpdateColorSpectrum();

        OnValueChanged(double.NaN, Value);
    }

    protected override void OnValueChanged(double oldValue, double newValue)
    {
        base.OnValueChanged(oldValue, newValue);

        SetValue(SelectedColorProperty, ColorExtensions.ConvertHsvToRgb(360 - newValue, 1, 1, 255));
    }

    #endregion

    #region Events

    private void ColorThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        AfterSelecting?.Invoke();
    }

    private void ColorThumb_MouseEnter(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
        {
            //https://social.msdn.microsoft.com/Forums/vstudio/en-US/5fa7cbc2-c99f-4b71-b46c-f156bdf0a75a/making-the-slider-slide-with-one-click-anywhere-on-the-slider?forum=wpf
            //The left button is pressed on mouse enter, but the mouse isn't captured, so the thumb
            //must have been moved under the mouse in response to a click on the track thanks to IsMoveToPointEnabled.

            //Generate a MouseLeftButtonDown event.
            _colorThumb.RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
            {
                RoutedEvent = MouseLeftButtonDownEvent
            });
        }
    }

    private static void SpectrumColor_ChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = d as SpectrumSlider;
        box?.UpdateColorSpectrum();            
    }

    #endregion

    #region Private Methods

    private void UpdateColorSpectrum()
    {
        if (_spectrumRectangle == null)
            return;

        _pickerBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0.5, 0),
            EndPoint = new Point(0.5, 1),
            ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation
        };

        var colorsList = IsAlphaSpectrum ? ColorExtensions.GenerateAlphaSpectrum(SpectrumColor) : ColorExtensions.GenerateHsvSpectrum(40);
        var stopIncrement = 1d / colorsList.Count;
        var isDecimal = stopIncrement % 1 > 0;

        for (var i = 0; i < (isDecimal ? colorsList.Count - 1 : colorsList.Count); i++)
            _pickerBrush.GradientStops.Add(new GradientStop(colorsList[i], i * stopIncrement));

        if (isDecimal)
            _pickerBrush.GradientStops.Add(new GradientStop(colorsList[colorsList.Count - 1], 1d));

        _spectrumRectangle.Fill = _pickerBrush;
    }

    #endregion
}

#endregion

#region HsvColor

/// <summary>
/// Describes a color in terms of Hue, Saturation, and Value (brightness)
/// </summary>
internal struct HsvColor
{
    public double H;
    public double S;
    public double V;

    public HsvColor(double h, double s, double v)
    {
        H = h;
        S = s;
        V = v;
    }
}

#endregion

#region ColorThumb

/// <summary>
/// The Thumb of the Spectrum Slider.
/// </summary>
public class ColorThumb : Thumb
{
    static ColorThumb()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorThumb), new FrameworkPropertyMetadata(typeof(ColorThumb)));
    }

    public static readonly DependencyProperty ThumbColorProperty = DependencyProperty.Register(nameof(ThumbColor), typeof(Color), typeof(ColorThumb), new FrameworkPropertyMetadata(Colors.Transparent));
    public static readonly DependencyProperty PointerOutlineThicknessProperty = DependencyProperty.Register(nameof(PointerOutlineThickness), typeof(double), typeof(ColorThumb), new FrameworkPropertyMetadata(1.0));
    public static readonly DependencyProperty PointerOutlineBrushProperty = DependencyProperty.Register(nameof(PointerOutlineBrush), typeof(Brush), typeof(ColorThumb), new FrameworkPropertyMetadata(null));

    /// <summary>
    /// The color of the Thumb.
    /// </summary>
    public Color ThumbColor
    {
        get => (Color)GetValue(ThumbColorProperty);
        set => SetValue(ThumbColorProperty, value);
    }

    public double PointerOutlineThickness
    {
        get => (double)GetValue(PointerOutlineThicknessProperty);
        set => SetValue(PointerOutlineThicknessProperty, value);
    }

    public Brush PointerOutlineBrush
    {
        get => (Brush)GetValue(PointerOutlineBrushProperty);
        set => SetValue(PointerOutlineBrushProperty, value);
    }
}

#endregion