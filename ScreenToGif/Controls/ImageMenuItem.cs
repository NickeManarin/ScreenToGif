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

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(UIElement), typeof(ImageMenuItem), new FrameworkPropertyMetadata(Image_Changed));

        public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ImageMenuItem), new FrameworkPropertyMetadata(16d));

        public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ImageMenuItem), new FrameworkPropertyMetadata(16d));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ImageMenuItem), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty HasImageProperty = DependencyProperty.Register(nameof(HasImage), typeof(bool), typeof(ImageMenuItem), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty DarkModeProperty = DependencyProperty.Register(nameof(DarkMode), typeof(bool), typeof(ImageMenuItem), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsOverNonClientAreaProperty = DependencyProperty.Register(nameof(IsOverNonClientArea), typeof(bool), typeof(ImageMenuItem), new FrameworkPropertyMetadata(false));

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
        /// True if the menu item contains an image.
        /// </summary>
        [Description("True if the menu item contains an image.")]
        public bool HasImage
        {
            get => (bool)GetValue(HasImageProperty);
            set => SetCurrentValue(HasImageProperty, value);
        }

        /// <summary>
        /// True if the menu should ajust itself for dark mode.
        /// </summary>
        [Description("True if the menu should ajust itself for dark mode.")]
        public bool DarkMode
        {
            get => (bool)GetValue(DarkModeProperty);
            set => SetCurrentValue(DarkModeProperty, value);
        }

        /// <summary>
        /// True if the button is being drawn on top of the non client area.
        /// </summary>
        [Description("True if the button is being drawn on top of the non client area.")]
        public bool IsOverNonClientArea
        {
            get => (bool)GetValue(IsOverNonClientAreaProperty);
            set => SetCurrentValue(IsOverNonClientAreaProperty, value);
        }

        #endregion

        #region Property Changed

        private static void Image_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ImageMenuItem)d).HasImage = e.NewValue != null;
        }

        #endregion

        static ImageMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageMenuItem), new FrameworkPropertyMetadata(typeof(ImageMenuItem)));
        }

        public void RaiseClick()
        {
            OnClick();
        }
    }
}