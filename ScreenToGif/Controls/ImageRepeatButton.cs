using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Button with a image inside.
    /// </summary>
    public class ImageRepeatButton : RepeatButton
    {
        #region Variables

        public readonly static DependencyProperty ChildProperty;
        public readonly static DependencyProperty TextProperty;
        public readonly static DependencyProperty MaxSizeProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the button.
        /// </summary>
        [Description("The Image of the button."), Category("Common")]
        public UIElement Child
        {
            get { return (UIElement)GetValue(ChildProperty); }
            set { SetCurrentValue(ChildProperty, value); }
        }

        /// <summary>
        /// The text of the button.
        /// </summary>
        [Description("The text of the button."), Category("Common")]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetCurrentValue(TextProperty, value); }
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image."), Category("Common")]
        public double MaxSize
        {
            get { return (double)GetValue(MaxSizeProperty); }
            set { SetCurrentValue(MaxSizeProperty, value); }
        }

        #endregion

        static ImageRepeatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageRepeatButton), new FrameworkPropertyMetadata(typeof(ImageRepeatButton)));

            ChildProperty = DependencyProperty.Register("Child", typeof(UIElement), typeof(ImageRepeatButton), new FrameworkPropertyMetadata());
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ImageRepeatButton), new FrameworkPropertyMetadata("Button"));
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageRepeatButton), new FrameworkPropertyMetadata(26.0));
        }
    }
}
