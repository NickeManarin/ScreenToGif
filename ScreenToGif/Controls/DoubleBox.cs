using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Controls
{
    public class DoubleBox : TextBox
    {
        #region Variables

        private bool _ignore;

        #endregion

        #region Dependency Property

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(DoubleBox),
            new FrameworkPropertyMetadata(Double.MaxValue, OnMaximumPropertyChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(DoubleBox),
            new FrameworkPropertyMetadata(0D, OnValuePropertyChanged));

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(DoubleBox),
            new FrameworkPropertyMetadata(0D, OnMinimumPropertyChanged));

        public static readonly DependencyProperty UpdateOnInputProperty = DependencyProperty.Register("UpdateOnInput", typeof(bool), typeof(DoubleBox),
            new FrameworkPropertyMetadata(false, OnUpdateOnInputPropertyChanged));

        #endregion

        #region Property Accessor

        [Bindable(true), Category("Common")]
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public bool UpdateOnInput
        {
            get { return (bool)GetValue(UpdateOnInputProperty); }
            set { SetValue(UpdateOnInputProperty, value); }
        }

        #endregion

        #region Properties Changed

        private static void OnMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var doubleBox = d as DoubleBox;

            if (doubleBox?.Value > doubleBox?.Maximum)
                doubleBox.Value = doubleBox.Maximum;
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var doubleBox = d as DoubleBox;

            if (doubleBox == null) return;

            if (doubleBox.Value > doubleBox.Maximum)
                doubleBox.Value = doubleBox.Maximum;

            else if (doubleBox.Value < doubleBox.Minimum)
                doubleBox.Value = doubleBox.Minimum;

            if (!doubleBox._ignore)
                doubleBox.Text = doubleBox.Text = String.Format(CultureInfo.CurrentCulture, "{0:###,###,##0.0###}", doubleBox.Value);

            doubleBox.RaiseValueChangedEvent();
        }

        private static void OnMinimumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var doubleBox = d as DoubleBox;

            if (doubleBox?.Value < doubleBox?.Minimum)
                doubleBox.Value = doubleBox.Minimum;
        }

        private static void OnUpdateOnInputPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DoubleBox)d).UpdateOnInput = (bool)e.NewValue;
        }

        #endregion

        #region Custom Events

        /// <summary>
        /// Create a custom routed event by first registering a RoutedEventID, this event uses the bubbling routing strategy.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent;

        /// <summary>
        /// Event raised when the numeric value is changed.
        /// </summary>
        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); } 
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        public void RaiseValueChangedEvent()
        {
            var newEventArgs = new RoutedEventArgs(ValueChangedEvent);
            RaiseEvent(newEventArgs);
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AddHandler(PreviewTextInputEvent, new TextCompositionEventHandler(OnPreviewTextInput));
            AddHandler(TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
            AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(OnPasting));
            AddHandler(LostFocusEvent, new RoutedEventHandler(OnLostFocus));

            //TODO: Culture change?
        }

        #region Base Properties Changed

        /// <summary>
        /// Check and validade input based on the current text.
        /// </summary>
        /// <param name="sender">The TextBox.</param>
        /// <param name="e">Arguments.</param>
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (String.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            var textBox = sender as TextBox;

            if (textBox == null)
            {
                e.Handled = true;
                return;
            }

            if (!IsEntryAllowed(textBox, e.Text))
            {
                e.Handled = true;
                return;
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!UpdateOnInput)
                return;

            var textBox = sender as TextBox;

            if (String.IsNullOrEmpty(textBox?.Text)) return;
            if (!IsTextAllowed(textBox.Text)) return;

            _ignore = true;

            Value = Convert.ToDouble(textBox.Text, CultureInfo.CurrentCulture);

            _ignore = false;
        }

        private void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = e.DataObject.GetData(typeof(string)) as string;

                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!UpdateOnInput)
            {
                var textBox = sender as TextBox;

                if (String.IsNullOrEmpty(textBox?.Text)) return;
                if (!IsTextAllowed(textBox.Text)) return;

                _ignore = true;

                Value = Convert.ToDouble(textBox.Text, CultureInfo.CurrentCulture);

                _ignore = false;
            }

            Text = String.Format(CultureInfo.CurrentCulture, "{0:###,###,##0.0###}", Value);
        }

        #endregion

        #region Methods

        private bool IsEntryAllowed(TextBox textBox, string text)
        {
            //Digits, points or commas.
            var regex = new Regex(@"^[0-9]|\.|\,$");

            //Checks if it's a valid char based on the context.
            return regex.IsMatch(text) && IsEntryAllowedInContext(textBox, text);
        }

        private bool IsEntryAllowedInContext(TextBox textBox, string next)
        {
            //if number, allow.
            if (Char.IsNumber(next.ToCharArray().FirstOrDefault()))
                return true;

            #region Thousands

            var thousands = Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberGroupSeparator;
            var thousandsChar = thousands.ToCharArray().FirstOrDefault();
            var decimals = Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            var decimalsChar = decimals.ToCharArray().FirstOrDefault();

            if (next.Equals(thousands))
            {
                var textAux = textBox.Text;

                if (!String.IsNullOrEmpty(textBox.SelectedText))
                    textAux = textAux.Replace(textBox.SelectedText, "");

                var before = textAux.Substring(0, textBox.SelectionStart);
                var after = textAux.Substring(textBox.SelectionStart);

                //If there's no text, is not allowed to add a thousand separator.
                if (String.IsNullOrEmpty(after + before)) return false;

                //Before the carret.
                if (!String.IsNullOrEmpty(before))
                {
                    //You can't add a thousand separator after the decimal.
                    if (before.Contains(decimals)) return false;

                    //Check the previous usage of a thousand separator.
                    if (before.Contains(thousands))
                    {
                        var split = before.Split(thousandsChar);

                        //You can't add a thousand separators closer than 3 chars from each other.
                        if (split.Last().Length != 3) return false;
                    }
                }

                //After the carret.
                if (!String.IsNullOrEmpty(after))
                {
                    var split = after.Split(thousandsChar, decimalsChar);

                    //You can't add a thousand separators closer than 3 chars from another separator, decimal or thousands.
                    if (split.First().Length != 3) return true;
                }

                return false;
            }

            #endregion

            #region Decimal

            if (next.Equals(decimals))
            {
                return !textBox.Text.Any(x => x.Equals(decimalsChar));
            }

            #endregion

            return true;
        }

        private bool IsTextAllowed(string text)
        {
            var regex = new Regex(@"^((\d+)|(\d{1,3}(\.\d{3})+)|(\d{1,3}(\.\d{3})(\,\d{3})+))((\,\d{4})|(\,\d{3})|(\,\d{2})|(\,\d{1})|(\,))?$");
            return regex.IsMatch(text);
        }

        #endregion
    }
}
