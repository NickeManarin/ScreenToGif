using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
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

        static SpectrumSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumSlider), new FrameworkPropertyMetadata(typeof(SpectrumSlider)));
        }

        #region Public Properties

        /// <summary>
        /// Current selected Color.
        /// </summary>
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        #endregion

        #region Dependency Property Fields

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register
            ("SelectedColor", typeof(Color), typeof(SpectrumSlider), new PropertyMetadata(Colors.Transparent));

        #endregion

        #region Public Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _spectrumRectangle = GetTemplateChild("PART_SpectrumDisplay") as Rectangle;

            _colorThumb = GetTemplateChild("Thumb") as ColorThumb;
            if (_colorThumb != null)
            {

                _colorThumb.PreviewMouseLeftButtonUp += _colorThumb_MouseLeftButtonUp;
                _colorThumb.MouseEnter += _colorThumb_MouseEnter;
            }

            UpdateColorSpectrum();
            OnValueChanged(Double.NaN, Value);
        }

        void _colorThumb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AfterSelecting?.Invoke();
        }

        private void _colorThumb_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.MouseDevice.Captured == null)
            {
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/5fa7cbc2-c99f-4b71-b46c-f156bdf0a75a/making-the-slider-slide-with-one-click-anywhere-on-the-slider?forum=wpf
                // the left button is pressed on mouse enter
                // but the mouse isn't captured, so the thumb
                // must have been moved under the mouse in response
                // to a click on the track thanks to IsMoveToPointEnabled.
                // Generate a MouseLeftButtonDown event.
                _colorThumb.RaiseEvent(
                    new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
                    {
                        RoutedEvent = MouseLeftButtonDownEvent
                    });
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            Color theColor = ColorExtensions.ConvertHsvToRgb(360 - newValue, 1, 1, 255);
            SetValue(SelectedColorProperty, theColor);
        }

        #endregion

        #region Private Methods

        private void UpdateColorSpectrum()
        {
            if (_spectrumRectangle != null)
            {
                CreateSpectrum();
            }
        }

        private void CreateSpectrum()
        {
            _pickerBrush = new LinearGradientBrush();
            _pickerBrush.StartPoint = new Point(0.5, 0);
            _pickerBrush.EndPoint = new Point(0.5, 1);
            _pickerBrush.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;

            List<Color> colorsList = ColorExtensions.GenerateHsvSpectrum();
            double stopIncrement = (double)1 / colorsList.Count;

            int i;
            for (i = 0; i < colorsList.Count; i++)
            {
                _pickerBrush.GradientStops.Add(new GradientStop(colorsList[i], i * stopIncrement));
            }

            _pickerBrush.GradientStops[i - 1].Offset = 1.0;
            _spectrumRectangle.Fill = _pickerBrush;
        }

        #endregion
    }

    #endregion SpectrumSlider

    #region HsvColor

    /// <summary>
    /// Describes a color in terms of Hue, Saturation, and Value (brightness)
    /// </summary>
    struct HsvColor
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

    #endregion HsvColor

    #region ColorThumb

    /// <summary>
    /// The Thumb of the Spectrum Slider.
    /// </summary>
    public class ColorThumb : Thumb
    {
        static ColorThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorThumb),
                new FrameworkPropertyMetadata(typeof(ColorThumb)));
        }

        public static readonly DependencyProperty ThumbColorProperty =
        DependencyProperty.Register("ThumbColor", typeof(Color), typeof(ColorThumb),
            new FrameworkPropertyMetadata(Colors.Transparent));

        public static readonly DependencyProperty PointerOutlineThicknessProperty =
        DependencyProperty.Register("PointerOutlineThickness", typeof(double), typeof(ColorThumb),
            new FrameworkPropertyMetadata(1.0));

        public static readonly DependencyProperty PointerOutlineBrushProperty =
        DependencyProperty.Register("PointerOutlineBrush", typeof(Brush), typeof(ColorThumb),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The color of the Thumb.
        /// </summary>
        public Color ThumbColor
        {
            get
            {
                return (Color)GetValue(ThumbColorProperty);
            }
            set
            {

                SetValue(ThumbColorProperty, value);
            }
        }

        public double PointerOutlineThickness
        {
            get
            {
                return (double)GetValue(PointerOutlineThicknessProperty);
            }
            set
            {
                SetValue(PointerOutlineThicknessProperty, value);
            }
        }

        public Brush PointerOutlineBrush
        {
            get
            {
                return (Brush)GetValue(PointerOutlineBrushProperty);
            }
            set
            {
                SetValue(PointerOutlineBrushProperty, value);
            }
        }
    }

    #endregion ColorThumb
}
