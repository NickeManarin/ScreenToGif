using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Button with a image inside.
    /// </summary>
    public class ImageToggleButton : ToggleButton
    {
        #region Variables

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ImageToggleButton), new FrameworkPropertyMetadata("Button"));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageToggleButton), new FrameworkPropertyMetadata(26.0));

        public static readonly DependencyProperty KeyGestureProperty = DependencyProperty.Register("KeyGesture", typeof(string), typeof(ImageToggleButton), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register("ContentHeight", typeof(double), typeof(ImageToggleButton), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register("ContentWidth", typeof(double), typeof(ImageToggleButton), new FrameworkPropertyMetadata(double.NaN));

        /// <summary> 
        /// DependencyProperty for <see cref="TextWrapping" /> property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(ImageToggleButton),
            new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region Properties

        /// <summary>
        /// The text of the button.
        /// </summary>
        [Description("The text of the button."), Category("Common")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetCurrentValue(TextProperty, value);
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image."), Category("Common")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetCurrentValue(MaxSizeProperty, value);
        }

        /// <summary>
        /// The KeyGesture of the button.
        /// </summary>
        [Description("The KeyGesture of the button."), Category("Common")]
        public string KeyGesture
        {
            get => (string)GetValue(KeyGestureProperty);
            set => SetCurrentValue(KeyGestureProperty, value);
        }

        /// <summary>
        /// The height of the button content.
        /// </summary>
        [Description("The height of the button content."), Category("Common")]
        public double ContentHeight
        {
            get => (double)GetValue(ContentHeightProperty);
            set => SetCurrentValue(ContentHeightProperty, value);
        }

        /// <summary>
        /// The width of the button content.
        /// </summary>
        [Description("The width of the button content."), Category("Common")]
        public double ContentWidth
        {
            get => (double)GetValue(ContentWidthProperty);
            set => SetCurrentValue(ContentWidthProperty, value);
        }

        /// <summary>
        /// The TextWrapping property controls whether or not text wraps 
        /// when it reaches the flow edge of its containing block box. 
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        #endregion

        static ImageToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageToggleButton), new FrameworkPropertyMetadata(typeof(ImageToggleButton)));
        }
    }
}