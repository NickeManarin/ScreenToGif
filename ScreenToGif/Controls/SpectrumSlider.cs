using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
            ("SelectedColor", typeof(Color), typeof(SpectrumSlider), new PropertyMetadata(System.Windows.Media.Colors.Transparent));

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
            }

            UpdateColorSpectrum();
            OnValueChanged(Double.NaN, Value);
        }

        void _colorThumb_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AfterSelecting != null)
            {
                AfterSelecting();
            }
        }

        #endregion

        #region Protected Methods

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            Color theColor = ColorUtilities.ConvertHsvToRgb(360 - newValue, 1, 1, 255);
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

            List<Color> colorsList = ColorUtilities.GenerateHsvSpectrum();
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

    #region Color Utilities

    static class ColorUtilities
    {
        /// <summary>
        /// Converts an RGB color to an HSV color
        /// </summary>
        /// <param name="r">Red</param>
        /// <param name="b">Blue</param>
        /// <param name="g">Green</param>
        /// <returns>A HsvColor object.</returns>
        public static HsvColor ConvertRgbToHsv(int r, int b, int g)
        {
            double h = 0, s;

            double min = Math.Min(Math.Min(r, g), b);
            double v = Math.Max(Math.Max(r, g), b);
            double delta = v - min;

            if (v == 0.0)
            {
                s = 0;
            }
            else
                s = delta / v;

            if (s == 0)
                h = 0.0;
            else
            {
                if (r == v)
                    h = (g - b) / delta;
                else if (g == v)
                    h = 2 + (b - r) / delta;
                else if (b == v)
                    h = 4 + (r - g) / delta;

                h *= 60;
                if (h < 0.0)
                    h = h + 360;
            }

            var hsvColor = new HsvColor();
            hsvColor.H = h;
            hsvColor.S = s;
            hsvColor.V = v / 255;

            return hsvColor;
        }

        /// <summary>
        /// Converts an HSV color to an RGB color.
        /// </summary>
        /// <param name="h">Hue</param>
        /// <param name="s">Saturation</param>
        /// <param name="v">Value</param>
        /// <param name="alpha">Alpha</param>
        /// <returns></returns>
        public static Color ConvertHsvToRgb(double h, double s, double v, double alpha)
        {
            double r = 0, g = 0, b = 0;

            if (s == 0)
            {
                r = v;
                g = v;
                b = v;
            }
            else
            {
                if (h == 360)
                    h = 0;
                else
                    h = h / 60;

                int i = (int)Math.Truncate(h);
                double f = h - i;

                double p = v * (1.0 - s);
                double q = v * (1.0 - (s * f));
                double t = v * (1.0 - (s * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = v;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = v;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = v;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = v;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = v;
                        break;

                    default:
                        r = v;
                        g = p;
                        b = q;
                        break;
                }
            }

            return Color.FromArgb((byte)alpha, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        /// <summary>
        /// Generates a list of colors with hues ranging from 0-360 and a saturation and value of 1.
        /// </summary>
        /// <returns>The List of Colors</returns>
        public static List<Color> GenerateHsvSpectrum()
        {
            var colorsList = new List<Color>(8);

            for (int i = 0; i < 29; i++)
            {
                colorsList.Add(ColorUtilities.ConvertHsvToRgb(i * 12, 1, 1, 255));
            }

            colorsList.Add(ColorUtilities.ConvertHsvToRgb(0, 1, 1, 255));

            return colorsList;
        }
    }

    #endregion ColorUtilities

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
            this.H = h;
            this.S = s;
            this.V = v;
        }
    }

    #endregion HsvColor

    #region ColorThumb

    /// <summary>
    /// The Thumb of the Spectrum Slider.
    /// </summary>
    public class ColorThumb : System.Windows.Controls.Primitives.Thumb
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
