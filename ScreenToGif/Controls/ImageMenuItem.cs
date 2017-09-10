using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// MenuItem with an image to the left.
    /// </summary>
    public class ImageMenuItem : MenuItem
    {
        #region Variables

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(ImageMenuItem), new FrameworkPropertyMetadata());

        public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register("ContentHeight", typeof(double), typeof(ImageMenuItem), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register("ContentWidth", typeof(double), typeof(ImageMenuItem), new FrameworkPropertyMetadata(double.NaN));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ImageMenuItem), new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(ImageMenuItem), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageMenuItem), new FrameworkPropertyMetadata(15.0));

        public static readonly DependencyProperty HasImageProperty = DependencyProperty.Register("HasImage", typeof(bool), typeof(ImageMenuItem), new FrameworkPropertyMetadata(false));

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the button.
        /// </summary>
        [Description("The Image of the button.")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set
            {
                SetCurrentValue(ImageProperty, value);

                //Has Image.
                SetCurrentValue(HasImageProperty, value != null);
            }
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
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image.")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetCurrentValue(MaxSizeProperty, value);
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image.")]
        public bool HasImage
        {
            get => (bool)GetValue(HasImageProperty);
            set => SetCurrentValue(HasImageProperty, value);
        }

        #endregion

        static ImageMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageMenuItem), new FrameworkPropertyMetadata(typeof(ImageMenuItem)));
        }
    }
}
