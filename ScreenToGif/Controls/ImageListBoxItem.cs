using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// ListBoxItem used by the languages listBox.
    /// </summary>
    public class ImageListBoxItem : ListBoxItem
    {
        #region Variables

        public readonly static DependencyProperty ImageProperty;
        public readonly static DependencyProperty AuthorProperty;
        public readonly static DependencyProperty MaxSizeProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the ListBoxItem.
        /// </summary>
        [Description("The Image of the ListBoxItem.")]
        public UIElement Image
        {
            get { return (UIElement)GetValue(ImageProperty); }
            set { SetCurrentValue(ImageProperty, value); }
        }

        /// <summary>
        /// The author of the ListBoxItem.
        /// </summary>
        [Description("The author of the ListBoxItem.")]
        public string Author
        {
            get { return (string)GetValue(AuthorProperty); }
            set { SetCurrentValue(AuthorProperty, value); }
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Description("The maximum size of the image.")]
        public double MaxSize
        {
            get { return (double)GetValue(MaxSizeProperty); }
            set { SetCurrentValue(MaxSizeProperty, value); }
        }

        #endregion

        static ImageListBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageListBoxItem), new FrameworkPropertyMetadata(typeof(ImageListBoxItem)));

            ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(ImageListBoxItem), new FrameworkPropertyMetadata());
            AuthorProperty = DependencyProperty.Register("Author", typeof(string), typeof(ImageListBoxItem), new FrameworkPropertyMetadata());
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageListBoxItem), new FrameworkPropertyMetadata(20.0));
        }
    }
}
