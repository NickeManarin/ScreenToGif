using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class ExtendedTextBox : TextBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty AllowSpacingyProperty = DependencyProperty.Register("AllowSpacing", typeof(bool), typeof(ExtendedTextBox), new PropertyMetadata(true));

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register("Watermark", typeof(string), typeof(ExtendedTextBox), new PropertyMetadata(""));

        #endregion

        #region Properties

        public bool AllowSpacing
        {
            get => (bool)GetValue(AllowSpacingyProperty);
            set => SetValue(AllowSpacingyProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        #endregion

        static ExtendedTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedTextBox), new FrameworkPropertyMetadata(typeof(ExtendedTextBox)));
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!AllowSpacing && e.Key == Key.Space)
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Focus();
            }

            if (UserSettings.All.TripleClickSelection && e.ClickCount == 3)
                SelectAll();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            if (!UserSettings.All.TripleClickSelection)
                SelectAll();
        }
    }
}