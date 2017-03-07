using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class ExtendedTextBox : TextBox
    {
        static ExtendedTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedTextBox), new FrameworkPropertyMetadata(typeof(ExtendedTextBox)));
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
