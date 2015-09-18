using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Numeric only control with up and down buttons to change the value.
    /// </summary>
    public class DoubleNumericUpDown : Control
    {
        #region Variables

        private RepeatButton _upButton;
        private RepeatButton _downButton;
        private TextBox _textBox;

        public readonly static DependencyProperty MaximumProperty;
        public readonly static DependencyProperty MinimumProperty;
        public readonly static DependencyProperty ValueProperty;
        public readonly static DependencyProperty StepProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The maximum value of the numeric up and down.
        /// </summary>
        [Description("The maximum value of the numeric up and down.")]
        public Double Maximum
        {
            get { return (Double)GetValue(MaximumProperty); }
            set
            {
                //Change de actual value to the range.
                if (value < Value)
                {
                    Value = value;
                }

                SetValue(MaximumProperty, value);
            }
        }

        /// <summary>
        /// The minimu value of the numeric up and down.
        /// </summary>
        [Description("The minimum value of the numeric up and down.")]
        public Double Minimum
        {
            get { return (Double)GetValue(MinimumProperty); }
            set
            {
                //Change de actual value to the range.
                if (value > Value)
                {
                    Value = value;
                }

                SetValue(MinimumProperty, value);
            }
        }

        /// <summary>
        /// The actual value of the numeric up and down.
        /// </summary>
        [Description("The actual value of the numeric up and down.")]
        public Double Value
        {
            get { return (Double)GetValue(ValueProperty); }
            set
            {
                SetCurrentValue(ValueProperty, value);

                //if (_textBox != null)
                //    _textBox.Text = Value.ToString();

                if (ValueChanged != null)
                    ValueChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// The Increment/Decrement value.
        /// </summary>
        [Description("The Increment/Decrement value.")]
        public Double StepValue
        {
            get { return (Double)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        #endregion

        #region Events

        public event EventHandler ValueChanged;
        public static event EventHandler InternalValueChanged;
        public static event EventHandler InternalMaximumChanged;
        public static event EventHandler InternalMinimumChanged;

        #endregion

        static DoubleNumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DoubleNumericUpDown), new FrameworkPropertyMetadata(typeof(DoubleNumericUpDown)));

            MaximumProperty = DependencyProperty.Register("Maximum", typeof(Double), typeof(DoubleNumericUpDown), new UIPropertyMetadata(100d, MaximumPropertyChangedCallback));
            MinimumProperty = DependencyProperty.Register("Minimum", typeof(Double), typeof(DoubleNumericUpDown), new UIPropertyMetadata(0d, MinimumPropertyChangedCallback));
            StepProperty = DependencyProperty.Register("StepValue", typeof(Double), typeof(DoubleNumericUpDown), new FrameworkPropertyMetadata(1d));
            ValueProperty = DependencyProperty.Register("Value", typeof(Double), typeof(DoubleNumericUpDown), new UIPropertyMetadata(0d, ValuePropertyChangedCallback));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //Internal controls.
            _upButton = Template.FindName("UpButton", this) as RepeatButton;
            _downButton = Template.FindName("DownButton", this) as RepeatButton;
            _textBox = Template.FindName("InternalBox", this) as TextBox;

            if (_upButton != null)
                _upButton.Click += UpButton_Click;

            if (_downButton != null)
                _downButton.Click += DownButton_Click;

            if (_textBox != null)
            {
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.PreviewTextInput += TextBox_PreviewTextInput;
                _textBox.MouseWheel += TextBox_MouseWheel;
                _textBox.LostFocus += TextBox_LostFocus;
                _textBox.TextChanged += TextBox_TextChanged;
            }

            Value = Value == 0d ? Minimum : Value;

            AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(PastingEvent));

            InternalValueChanged += (sender, args) => { CheckValue(); };
            InternalMaximumChanged += (sender, args) => { CheckMaximum(); };
            InternalMinimumChanged += (sender, args) => { CheckMinimum(); };
        }

        #region Methods

        private bool IsEntryDisallowed(object sender, string text)
        {
            var regex = new Regex(@"^[0-9]|\.|\,$");

            //Pontuation.
            if (regex.IsMatch(text))
            {
                return !AnalizePontuation(sender, text);
            }

            //Not a number dot/comma.
            return true;
        }

        private bool IsTextDisallowed(string text)
        {
            var regex = new Regex(@"^((\d+)|(\d{1,3}(\.\d{3})+)|(\d{1,3}(\.\d{3})(\,\d{3})+))((\,\d{2})|(\,\d{1})|(\,))?$");
            return !regex.IsMatch(text); //\d+(?:,\d{1,2})?
        }

        private bool AnalizePontuation(object sender, string next)
        {
            var textBox = sender as TextBox;

            if (textBox == null) return true;

            if (Char.IsNumber(next.ToCharArray()[0]))
                return true;

            if (next.Equals("."))
            {
                var texto = textBox.Text;

                if (!String.IsNullOrEmpty(textBox.SelectedText))
                    texto = texto.Replace(textBox.SelectedText, "");

                //Checks the position, decides if puts a point or not.
                var before = texto.Substring(0, textBox.SelectionStart);
                var after = texto.Substring(textBox.SelectionStart);

                //If no text, return true.
                if (String.IsNullOrEmpty(before) && String.IsNullOrEmpty(after)) return true;

                if (!String.IsNullOrEmpty(before))
                {
                    if (before.Contains(',')) return false;

                    if (after.Contains("."))
                    {
                        var split = before.Split('.');

                        if (split.Last().Length != 3) return false;
                    }
                }

                if (!String.IsNullOrEmpty(after))
                {
                    var split = after.Split('.', ',');

                    if (split.First().Length != 3) return false;
                }

                return true;
            }

            //Only one comma.
            if (next.Equals(","))
            {
                return !textBox.Text.Any(x => x.Equals(','));
            }

            return true;
        }

        private double TryGetValue(string text)
        {
            double newValue;
            Double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out newValue);

            return newValue; 
        }

        private void CheckValue()
        {
            #region MinMax

            if (Value > Maximum)
                Value = Maximum;
            else if (Value < Minimum)
                Value = Minimum;

            #endregion

            _textBox.Text = String.Format(CultureInfo.InvariantCulture, "{0:###,###,##0.0#}", Value);
        }

        private void CheckMaximum()
        {
            if (Maximum < Minimum)
                Maximum = Minimum;

            if (Value < Minimum)
                Value = Minimum;
            else if (Value > Maximum)
                Value = Maximum;
        }

        private void CheckMinimum()
        {
            if (Minimum > Maximum)
                Minimum = Maximum;

            if (Value < Minimum)
                Value = Minimum;
            else if (Value > Maximum)
                Value = Maximum;
        }

        #endregion

        #region Event Handlers

        private static void ValuePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (InternalValueChanged != null)
                InternalValueChanged(null, null);
        }
        
        private static void MaximumPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (InternalMaximumChanged != null)
                InternalMaximumChanged(null, null);
        }

        private static void MinimumPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (InternalMinimumChanged != null)
                InternalMinimumChanged(null, null);
        }


        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value -= StepValue;
                if (Value < Minimum)
                    Value = Minimum;
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value += StepValue;
                if (Value > Maximum)
                    Value = Maximum;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox_LostFocus(sender, e);
                e.Handled = true;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (String.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (IsEntryDisallowed(sender, e.Text))
            {
                e.Handled = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox == null) return;

            //If empty.
            if (String.IsNullOrEmpty(textBox.Text.Trim()))
            {
                textBox.Text = String.Format(CultureInfo.InvariantCulture, "{0:###,###,##0.0#}", Value);
                return;
            }

            Value = TryGetValue(textBox.Text); //Convert.ToDouble(textBox.Text, CultureInfo.InvariantCulture);
        }

        private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox == null) return;

            var step = Keyboard.Modifiers == ModifierKeys.Shift ? StepValue + 10d : StepValue;

            #region Add or decrease the step.

            if (e.Delta > 0)
            {
                Value = TryGetValue(textBox.Text) + step;
            }
            else
            {
                Value = TryGetValue(textBox.Text) -step;
            }

            #endregion
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox == null) return;
            if (String.IsNullOrEmpty(textBox.Text)) return;
            if (IsTextDisallowed(textBox.Text)) return;

            Value = TryGetValue(textBox.Text); 
        }

        private void PastingEvent(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                var text = (String)e.DataObject.GetData(typeof(String));

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
    }
}
