using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Translator.Controls;

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
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);

        SelectAll();
    }
}