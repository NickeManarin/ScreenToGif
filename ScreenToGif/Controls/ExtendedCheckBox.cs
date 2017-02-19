using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    public class ExtendedCheckBox : CheckBox
    {
        #region Dependency Properties

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string),
            typeof(ExtendedCheckBox), new PropertyMetadata());

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register("TextWrapping", typeof(TextWrapping),
            typeof(ExtendedCheckBox), new PropertyMetadata(TextWrapping.Wrap));

        #endregion

        #region Properties

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        #endregion

        static ExtendedCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedCheckBox), new FrameworkPropertyMetadata(typeof(ExtendedCheckBox)));
        }
    }
}
