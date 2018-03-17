using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Numeric only TextBox.
    /// </summary>
    [Obsolete]
    [Description("Numeric only TextBox")]
    public class NumericTextBox : TextBox
    {
        #region Variables

        private TextBox _textBox;
        private bool _ignore = false;

        public static readonly DependencyProperty MinValueProperty;
        public static readonly DependencyProperty ValueProperty;
        public static readonly DependencyProperty MaxValueProperty;
        public static readonly DependencyProperty IsHexProperty;
        public static readonly DependencyProperty IsBoundProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The minimum value of the numeric text box.
        /// </summary>
        [Description("The minimum value of the numeric text box.")]
        public long MinValue
        {
            get => (long)GetValue(MinValueProperty);
            set => SetCurrentValue(MinValueProperty, value);
        }

        /// <summary>
        /// The actual value of the numeric text box.
        /// </summary>
        [Description("The actual value of the numeric text box.")]
        public long Value
        {
            get => (long)GetValue(ValueProperty);
            set
            {
                SetCurrentValue(ValueProperty, value);
                RaiseValueChangedEvent();
            }
        }

        /// <summary>
        /// The maximum value of the numeric text box.
        /// </summary>
        [Description("The maximum value of the numeric text box.")]
        public long MaxValue
        {
            get => (long)GetValue(MaxValueProperty);
            set => SetCurrentValue(MaxValueProperty, value);
        }

        /// <summary>
        /// True if this TextBox is using the Hexadecimal format.
        /// </summary>
        [Description("True if this TextBox is using the Hexadecimal format.")]
        public bool IsHex
        {
            get => (bool)GetValue(IsHexProperty);
            set => SetCurrentValue(IsHexProperty, value);
        }

        /// <summary>
        /// True if this TextBox is bound to the size of the recorder window. I know, this is a quick hack.
        /// </summary>
        [Description("True if this TextBox is bound to the recording window size.")]
        public bool IsBound
        {
            get => (bool)GetValue(IsBoundProperty);
            set => SetCurrentValue(IsBoundProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Create a custom routed event by first registering a RoutedEventID, this event uses the bubbling routing strategy.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent;

        /// <summary>
        /// Event raised when the numeric value is changed.
        /// Provide CLR accessors for the event.
        /// </summary>
        public event RoutedEventHandler ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        void RaiseValueChangedEvent()
        {
            var newEventArgs = new RoutedEventArgs(ValueChangedEvent);
            RaiseEvent(newEventArgs);
        }

        #endregion

        static NumericTextBox()
        {
            MinValueProperty = DependencyProperty.Register("MinValue", typeof(long), typeof(NumericTextBox), new FrameworkPropertyMetadata((long)1));
            ValueProperty = DependencyProperty.Register("Value", typeof(long), typeof(NumericTextBox), new FrameworkPropertyMetadata());
            MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(long), typeof(NumericTextBox), new FrameworkPropertyMetadata((long)2000));
            IsHexProperty = DependencyProperty.Register("IsHex", typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(false));
            IsBoundProperty = DependencyProperty.Register("IsBound", typeof(bool), typeof(NumericTextBox), new FrameworkPropertyMetadata(false));

            ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NumericTextBox));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //this.TextChanged += TextBox_TextChanged;
            //this.MouseWheel += TextBox_MouseWheel;

            AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(PastingEvent));
            AddHandler(TextBox.PreviewTextInputEvent, new TextCompositionEventHandler(TextBox_PreviewTextInput));
            AddHandler(ValueChangedEvent, new RoutedEventHandler(NumericTextBox_ValueChanged));
        }

        #region Events

        private void NumericTextBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (_ignore) return;
            var textBox = sender as NumericTextBox;
            if (textBox == null) return;

            //ValueChanged -= NumericTextBox_ValueChanged;
            _ignore = true;

            if (Value > MaxValue)
                Value = MaxValue;

            else if (Value < MinValue)
                Value = MinValue;

            //ValueChanged += NumericTextBox_ValueChanged;
            _ignore = false;

            if (IsHex)
            {
                textBox.Text = "#" + Value.ToString("X");
                return;
            }

            if (!textBox.IsBound)
            {
                textBox.Text = Value.ToString();
                return;
            }
            
            //TODO: test with high dpi, and change this!
            textBox.Text = (Value - (textBox.Name.StartsWith("H") ? Constants.VerticalOffset : Constants.HorizontalOffset)).ToString();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            #region Changes the value of the Numeric Textbox

            var textBox = sender as TextBox;

            if (textBox == null) return;

            int newValue = Convert.ToInt32(textBox.Text);

            if (newValue > MaxValue)
                Value = MaxValue;
            else if (newValue < MinValue)
                Value = MinValue;
            else
            {
                Value = newValue;
            }

            #endregion
        }

        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (Value < MaxValue)
                    Value++;
            }
            else
            {
                if (Value > MinValue)
                    Value -= 1;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (IsTextDisallowed(e.Text))
            {
                e.Handled = true;
            }
        }

        private void PastingEvent(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));

                if (IsTextDisallowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        #endregion

        private bool IsTextDisallowed(string text)
        {
            if (!IsHex)
            {
                var regex = new Regex("[^0-9]+");
                return regex.IsMatch(text);
            }

            var regexHex = new Regex("^#([A-Fa-f0-9]{8})$");
            return regexHex.IsMatch(text);
        }
    }
}