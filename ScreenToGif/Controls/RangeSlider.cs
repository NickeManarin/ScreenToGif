using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Range Slider control.
    /// </summary>
    public class RangeSlider : Control
    {
        #region Variables

        private Slider _lowerSlider;
        private Slider _upperSlider;
        private Border _progressBorder;

        #endregion

        #region Dependency

        public static readonly DependencyProperty MinimumProperty;
        public static readonly DependencyProperty LowerValueProperty;
        public static readonly DependencyProperty UpperValueProperty;
        public static readonly DependencyProperty MaximumProperty;
        public static readonly DependencyProperty DisableLowerValueProperty;
        public static readonly DependencyProperty TickPlacementProperty;

        #endregion

        #region Properties

        /// <summary>
        /// Minimum value of the slider.
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Maximum value of the slider.
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Value of the lower Thumb.
        /// </summary>
        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set
            {
                SetValue(LowerValueProperty, value);

                if (LowerValueChanged != null)
                    LowerValueChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// Value of the upper Thumb.
        /// </summary>
        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set
            {
                SetValue(UpperValueProperty, value);

                if (UpperValueChanged != null)
                    UpperValueChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// True to disable the range of the slider.
        /// </summary>
        public bool DisableLowerValue
        {
            get { return (bool)GetValue(DisableLowerValueProperty); }
            set
            {
                SetValue(DisableLowerValueProperty, value);

                LowerValue = Minimum;

                if (_lowerSlider != null)
                    _lowerSlider.Visibility = DisableLowerValue ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// The Tick placement position.
        /// </summary>
        public TickPlacement TickPlacement
        {
            get { return (TickPlacement)GetValue(TickPlacementProperty); }
            set { SetValue(TickPlacementProperty, value); }
        }

        #endregion

        #region Events Properties

        public event EventHandler LowerValueChanged;
        public event EventHandler UpperValueChanged;

        #endregion

        static RangeSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));

            MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(0d));
            LowerValueProperty = DependencyProperty.Register("LowerValue", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(10d));
            UpperValueProperty = DependencyProperty.Register("UpperValue", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(90d));
            MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(100d));
            DisableLowerValueProperty = DependencyProperty.Register("DisableLowerValue", typeof(bool), typeof(RangeSlider), new FrameworkPropertyMetadata(false));
            TickPlacementProperty = DependencyProperty.Register("TickPlacement", typeof(TickPlacement), typeof(RangeSlider), new FrameworkPropertyMetadata(TickPlacement.None));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            LayoutUpdated += RangeSlider_LayoutUpdated;
            _lowerSlider = Template.FindName("LowerSlider", this) as Slider;
            _upperSlider = Template.FindName("UpperSlider", this) as Slider;
            _progressBorder = Template.FindName("ProgressBorder", this) as Border;

            if (_lowerSlider != null)
            {
                _lowerSlider.Value = LowerValue;
                _lowerSlider.PreviewMouseUp += LowerSlider_MouseUp;
            }

            if (_upperSlider != null)
            {
                _upperSlider.Value = UpperValue;
                _upperSlider.PreviewMouseUp += UpperSlider_PreviewMouseUp;
            }
        }

        private void SetProgressBorder()
        {
            double lowerPoint = (ActualWidth * (LowerValue - Minimum)) / (Maximum - Minimum);
            double upperPoint = (ActualWidth * (UpperValue - Minimum)) / (Maximum - Minimum);
            upperPoint = ActualWidth - upperPoint;

            _progressBorder.Margin = new Thickness(lowerPoint, 0, upperPoint, 0);
        }

        #region Event Handlers

        private void UpperSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            UpperValue = Math.Max(_upperSlider.Value, _lowerSlider.Value);
            SetProgressBorder();
        }

        private void LowerSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LowerValue = Math.Min(_upperSlider.Value, _lowerSlider.Value);
            SetProgressBorder();
        }

        private void RangeSlider_LayoutUpdated(object sender, EventArgs e)
        {
            SetProgressBorder();
        }
        
        #endregion
    }
}
