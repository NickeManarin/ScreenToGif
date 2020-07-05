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
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(ImageListBoxItem), new FrameworkPropertyMetadata(0));
        public static readonly DependencyProperty ShowMarkOnSelectionProperty = DependencyProperty.Register(nameof(ShowMarkOnSelection), typeof(bool), typeof(ImageListBoxItem), new FrameworkPropertyMetadata(true));
        
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

        /// <summary>
        /// The index of the item on the list. Must be manually set.
        /// </summary>
        [Description("The index of the item on the list. Must be manually set.")]
        public int Index
        {
            get => (int)GetValue(IndexProperty);
            set => SetCurrentValue(IndexProperty, value);
        }

        /// <summary>
        /// True if the item must show the checkmark on selection.
        /// </summary>
        [Description("True if the item must show the checkmark on selection.")]
        public bool ShowMarkOnSelection
        {
            get => (bool)GetValue(ShowMarkOnSelectionProperty);
            set => SetCurrentValue(ShowMarkOnSelectionProperty, value);
        }

        #endregion

        static ImageListBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageListBoxItem), new FrameworkPropertyMetadata(typeof(ImageListBoxItem)));
        }
    }
}