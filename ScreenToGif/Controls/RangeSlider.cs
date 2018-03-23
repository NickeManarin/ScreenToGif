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

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider), 
            new FrameworkPropertyMetadata(0d));

        public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register("LowerValue", typeof(double), typeof(RangeSlider), 
            new FrameworkPropertyMetadata(10d, LowerValue_PropertyChanged));

        public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register("UpperValue", typeof(double), typeof(RangeSlider), 
            new FrameworkPropertyMetadata(90d, UpperValue_PropertyChanged));

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider), 
            new FrameworkPropertyMetadata(100d));

        public static readonly DependencyProperty DisableLowerValueProperty = DependencyProperty.Register("DisableLowerValue", typeof(bool), typeof(RangeSlider), 
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty TickPlacementProperty = DependencyProperty.Register("TickPlacement", typeof(TickPlacement), typeof(RangeSlider), 
            new FrameworkPropertyMetadata(TickPlacement.None));

        #endregion

        #region Properties

        /// <summary>
        /// Minimum value of the slider.
        /// </summary>
        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        /// <summary>
        /// Maximum value of the slider.
        /// </summary>
        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        /// <summary>
        /// Value of the lower Thumb.
        /// </summary>
        public double LowerValue
        {
            get => (double)GetValue(LowerValueProperty);
            set => SetValue(LowerValueProperty, value);
        }

        /// <summary>
        /// Value of the upper Thumb.
        /// </summary>
        public double UpperValue
        {
            get => (double)GetValue(UpperValueProperty);
            set => SetValue(UpperValueProperty, value);
        }

        /// <summary>
        /// True to disable the range of the slider.
        /// </summary>
        public bool DisableLowerValue
        {
            get => (bool)GetValue(DisableLowerValueProperty);
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
            get => (TickPlacement)GetValue(TickPlacementProperty);
            set => SetValue(TickPlacementProperty, value);
        }

        #endregion

        #region Property Changed

        private static void LowerValue_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var range = d as RangeSlider;

            range?.RaiseLowerValueChangedEvent();
        }

        private static void UpperValue_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var range = d as RangeSlider;

            range?.RaiseUpperValueChangedEvent();
        }

        #endregion

        #region Custom Events

        public static readonly RoutedEvent LowerValueChangedEvent = EventManager.RegisterRoutedEvent("LowerValueChanged", RoutingStrategy.Bubble, 
            typeof(RoutedEventHandler), typeof(RangeSlider));

        public static readonly RoutedEvent UpperValueChangedEvent = EventManager.RegisterRoutedEvent("UpperValueChanged", RoutingStrategy.Bubble, 
            typeof(RoutedEventHandler), typeof(RangeSlider));

        public event RoutedEventHandler LowerValueChanged
        {
            add => AddHandler(LowerValueChangedEvent, value);
            remove => RemoveHandler(LowerValueChangedEvent, value);
        }

        public event RoutedEventHandler UpperValueChanged
        {
            add => AddHandler(UpperValueChangedEvent, value);
            remove => RemoveHandler(UpperValueChangedEvent, value);
        }

        public void RaiseLowerValueChangedEvent()
        {
            if (LowerValueChangedEvent == null || !IsLoaded)
                return;

            var newEventArgs = new RoutedEventArgs(LowerValueChangedEvent);
            RaiseEvent(newEventArgs);
        }

        public void RaiseUpperValueChangedEvent()
        {
            if (UpperValueChangedEvent == null || !IsLoaded)
                return;

            var newEventArgs = new RoutedEventArgs(UpperValueChangedEvent);
            RaiseEvent(newEventArgs);
        }

        #endregion

        static RangeSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
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
            var lowerPoint = ActualWidth * (LowerValue - Minimum) / (Maximum - Minimum);
            var upperPoint = ActualWidth * (UpperValue - Minimum) / (Maximum - Minimum);
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