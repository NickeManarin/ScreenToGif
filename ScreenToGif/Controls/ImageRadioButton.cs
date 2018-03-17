using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    public class ImageRadioButton : RadioButton
    {
        #region Variables

        public Viewbox _viewbox;
        public TextBlock _label;

        public static readonly DependencyProperty ChildProperty;
        public static readonly DependencyProperty TextProperty;
        public static readonly DependencyProperty MaxSizeProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the button.
        /// </summary>
        [Description("The Image of the button.")]
        public UIElement Child
        {
            get => (UIElement)GetValue(ChildProperty);
            set => SetCurrentValue(ChildProperty, value);
        }

        /// <summary>
        /// The text of the button.
        /// </summary>
        [Description("The text of the button.")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetCurrentValue(TextProperty, value);
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image.")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetCurrentValue(MaxSizeProperty, value);
        }

        #endregion

        static ImageRadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageRadioButton), new FrameworkPropertyMetadata(typeof(ImageRadioButton)));

            ChildProperty = DependencyProperty.Register("Child", typeof(UIElement), typeof(ImageRadioButton), new FrameworkPropertyMetadata());
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ImageRadioButton), new FrameworkPropertyMetadata("Button"));
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageRadioButton), new FrameworkPropertyMetadata(26.0));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _viewbox = Template.FindName("ViewBoxInternal", this) as Viewbox;
            _label = Template.FindName("TextBlockInternal", this) as TextBlock;
        }
    }
}