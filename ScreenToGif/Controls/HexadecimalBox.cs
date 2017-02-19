using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenToGif.Controls
{
    public class HexadecimalBox : ExtendedTextBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty RedProperty = DependencyProperty.Register("Red", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(0, Value_PropertyChanged));

        public static readonly DependencyProperty GreenProperty = DependencyProperty.Register("Green", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(0, Value_PropertyChanged));

        public static readonly DependencyProperty BlueProperty = DependencyProperty.Register("Blue", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(0, Value_PropertyChanged));

        public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register("Alpha", typeof(int), typeof(HexadecimalBox), new PropertyMetadata(255, Value_PropertyChanged));

        public static readonly DependencyProperty DisplayGlyphProperty = DependencyProperty.Register("DisplayGlyph", typeof(bool), typeof(HexadecimalBox), new PropertyMetadata(true));

        #endregion

        #region Properties

        public int Red
        {
            get { return (int) GetValue(RedProperty); }
            set { SetValue(RedProperty, value); }
        }

        public int Blue
        {
            get { return (int)GetValue(BlueProperty); }
            set { SetValue(BlueProperty, value); }
        }

        public int Green
        {
            get { return (int)GetValue(GreenProperty); }
            set { SetValue(GreenProperty, value); }
        }

        public int Alpha
        {
            get { return (int)GetValue(AlphaProperty); }
            set { SetValue(AlphaProperty, value); }
        }
        
        public bool DisplayGlyph
        {
            get { return (bool)GetValue(DisplayGlyphProperty); }
            set { SetValue(DisplayGlyphProperty, value); }
        }

        #endregion

        private static void Value_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var hexaBox = o as HexadecimalBox;

            if (hexaBox == null)
                return;

            hexaBox.Text = $"{(hexaBox.DisplayGlyph ? "#" : "")}{hexaBox.Alpha:X2}{hexaBox.Red:X2}{hexaBox.Green:X2}{hexaBox.Blue:X2}";
        }

        //input validation, 

        static HexadecimalBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HexadecimalBox), new FrameworkPropertyMetadata(typeof(HexadecimalBox)));
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AddHandler(DataObject.PastingEvent, new DataObjectPastingEventHandler(OnPasting));

            Text = $"{(DisplayGlyph ? "#" : "")}{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Focus();
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            SelectAll();
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
            {
                e.Handled = true;
                return;
            }

            if (!IsEntryAllowed(this, e.Text))
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewTextInput(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text)) return;
            if (!IsTextAllowed(Text)) return;



            base.OnTextChanged(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            
                if (string.IsNullOrEmpty(Text) || !IsTextAllowed(Text))
                {
                    //Value = DefaultValueIfEmpty;
                    return;
                }


                return;
        }

        #endregion

        #region Base Properties Changed

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

        #endregion

        #region Methods

        private bool IsEntryAllowed(TextBox textBox, string text)
        {
            //Digits, points or commas.
            var regex = new Regex(@"^[0-9]|[A-F]|$");

            //Checks if it's a valid char based on the context.
            return regex.IsMatch(text) && IsEntryAllowedInContext(textBox, text);
        }

        private bool IsEntryAllowedInContext(TextBox textBox, string next)
        {
            //if number, allow.
            //if (char.IsNumber(next.ToCharArray().FirstOrDefault()))
            //    return true;

            //TODO: Validate position

            return true;
        }

        private bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "^#([A-Fa-f0-9]{8})$");
        }

        #endregion
    }
}
