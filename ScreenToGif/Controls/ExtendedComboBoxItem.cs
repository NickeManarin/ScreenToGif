using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    public class ExtendedComboBoxItem : ComboBoxItem
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ExtendedComboBoxItem), new PropertyMetadata(default(string)));

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(ExtendedComboBoxItem), new PropertyMetadata(default(object)));

        public object Value
        {
            get { return (object) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        static ExtendedComboBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedComboBoxItem), new FrameworkPropertyMetadata(typeof(ExtendedComboBoxItem)));
        }
    }
}
