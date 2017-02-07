using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    public class SmartSelectTextBox : TextBox
    {
        public SmartSelectTextBox()
        {
            AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SmartSelectAll), true);
        }

        void SmartSelectAll(object sender, MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsFocused)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                    e.Handled = true;
                }
                if (e.ClickCount == 3)
                {
                    textBox.SelectAll();
                }
            }
        }
    }
}
