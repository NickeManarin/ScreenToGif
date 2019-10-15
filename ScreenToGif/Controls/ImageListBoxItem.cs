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

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(UIElement), typeof(ImageListBoxItem), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty MainAuthorProperty = DependencyProperty.Register(nameof(MainAuthor), typeof(string), typeof(ImageListBoxItem), new FrameworkPropertyMetadata(""));
        public static readonly DependencyProperty AuthorProperty = DependencyProperty.Register(nameof(Author), typeof(string), typeof(ImageListBoxItem), new FrameworkPropertyMetadata(""));
        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register(nameof(MaxSize), typeof(double), typeof(ImageListBoxItem), new FrameworkPropertyMetadata(20.0));

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the ListBoxItem.
        /// </summary>
        [Description("The Image of the ListBoxItem.")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetCurrentValue(ImageProperty, value);
        }

        /// <summary>
        /// The author of the ListBoxItem.
        /// </summary>
        [Description("The main author of the ListBoxItem.")]
        public string MainAuthor
        {
            get => (string)GetValue(MainAuthorProperty);
            set => SetCurrentValue(MainAuthorProperty, value);
        }

        /// <summary>
        /// The author of the ListBoxItem.
        /// </summary>
        [Description("The author of the ListBoxItem.")]
        public string Author
        {
            get => (string)GetValue(AuthorProperty);
            set => SetCurrentValue(AuthorProperty, value);
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

        static ImageListBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageListBoxItem), new FrameworkPropertyMetadata(typeof(ImageListBoxItem)));
        }
    }
}