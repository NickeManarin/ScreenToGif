using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Double Numeric only TextBox.
    /// </summary>
    [Description("Double Numeric only TextBox")]
    public class DoubleNumericBox : TextBox
    {
        #region Variables

        private TextBox _textBox;

        public readonly static DependencyProperty MinValueProperty;
        public readonly static DependencyProperty ValueProperty;
        public readonly static DependencyProperty MaxValueProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The minimum value of the numeric text box.
        /// </summary>
        [Description("The minimum value of the numeric text box.")]
        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetCurrentValue(MinValueProperty, value); }
        }

        /// <summary>
        /// The actual value of the numeric text box.
        /// </summary>
        [Description("The actual value of the numeric text box.")]
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
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
        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetCurrentValue(MaxValueProperty, value); }
        }

        #endregion

        #region Events

        /// <summary>
        /// Create a custom routed event by first registering a RoutedEventID, this event uses the bubbling routing strategy.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent;

        /// <summary>
        /// Event raised when the numeric value is changed.
        /// </summary>
        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }  //Provide CLR accessors for the event 
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        public void RaiseValueChangedEvent()
        {
            var newEventArgs = new RoutedEventArgs(ValueChangedEvent);
            RaiseEvent(newEventArgs);
        }

        #endregion

        static DoubleNumericBox()
        {
            MinValueProperty = DependencyProperty.Register("MinValue", typeof(double), typeof(DoubleNumericBox), new FrameworkPropertyMetadata(0D));
            ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(DoubleNumericBox), new FrameworkPropertyMetadata(0D, ValueCallback));
            MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double), typeof(DoubleNumericBox), new FrameworkPropertyMetadata(Double.MaxValue));

            ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DoubleNumericBox));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            PreviewTextInput += DoubleNumericBox_PreviewTextInput;
            ValueChanged += DoubleNumericBox_ValueChanged;
            TextChanged += DoubleNumericBox_TextChanged;
            LostFocus += DoubleNumericBox_LostFocus;

            AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(PastingEvent));
        }

        #region Control Events

        private static void ValueCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = d as DoubleNumericBox;
            if (textBox == null) return;
            textBox.RaiseValueChangedEvent();
        }

        private void DoubleNumericBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            var textBox = sender as DoubleNumericBox;

            if (textBox == null) return;

            ValueChanged -= DoubleNumericBox_ValueChanged;
            TextChanged -= DoubleNumericBox_TextChanged;

            if (Value > MaxValue)
                Value = MaxValue;

            else if (Value < MinValue)
                Value = MinValue;

            textBox.Text = String.Format("{0:###,###,##0.0#}", Value);

            ValueChanged += DoubleNumericBox_ValueChanged;
            TextChanged += DoubleNumericBox_TextChanged;
        }

        private void DoubleNumericBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox == null) return;
            if (String.IsNullOrEmpty(textBox.Text)) return;
            if (IsTextDisallowed(textBox.Text)) return;

            ValueChanged -= DoubleNumericBox_ValueChanged;

            var newValue = Convert.ToDouble(textBox.Text);

            if (newValue > MaxValue)
                Value = MaxValue;
            else if (newValue < MinValue)
                Value = MinValue;
            else
            {
                Value = newValue;
            }

            ValueChanged += DoubleNumericBox_ValueChanged;
        }

        private void DoubleNumericBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
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

        private void DoubleNumericBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextChanged -= DoubleNumericBox_TextChanged;

            Text = String.Format("{0:###,###,##0.0#}", Value);

            TextChanged += DoubleNumericBox_TextChanged;
        }

        #endregion

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

                //Verifica em qual posição a seleção está e decide se pode ou não inserir um ponto.
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

            //Pode ter apenas 1 vírgula.
            if (next.Equals(","))
            {
                return !textBox.Text.Any(x => x.Equals(','));
            }

            return true;
        }

        #endregion
    }
}
