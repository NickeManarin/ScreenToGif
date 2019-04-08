using System.ComponentModel;
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

        public static readonly DependencyProperty IsObligatoryProperty = DependencyProperty.Register("IsObligatory", typeof(bool), typeof(ExtendedTextBox));

        #endregion

        #region Properties

        [Bindable(true), Category("Common")]
        public bool AllowSpacing
        {
            get => (bool)GetValue(AllowSpacingyProperty);
            set => SetValue(AllowSpacingyProperty, value);
        }

        [Bindable(true), Category("Common")]
        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        [Bindable(true), Category("Common")]
        public bool IsObligatory
        {
            get => (bool)GetValue(IsObligatoryProperty);
            set => SetValue(IsObligatoryProperty, value);
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