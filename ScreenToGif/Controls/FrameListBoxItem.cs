using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Item of a ListBox, basically an imagem with a number.
    /// </summary>
    public class FrameListBoxItem : ListViewItem
    {
        #region Variables

        public readonly static DependencyProperty ImageProperty;
        public readonly static DependencyProperty IsCheckedProperty;
        public readonly static DependencyProperty MaxSizeProperty;
        public readonly static DependencyProperty FrameNumberProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The Frame of the ListBoxItem.
        /// </summary>
        [Description("The Frame of the ListBoxItem.")]
        public UIElement Image
        {
            get { return (UIElement)GetValue(ImageProperty); }
            set { SetCurrentValue(ImageProperty, value); }
        }

        /// <summary>
        /// True if item is checked.
        /// </summary>
        [Description("True if item is checked.")]
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetCurrentValue(IsCheckedProperty, value); }
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

        /// <summary>
        /// The frame number.
        /// </summary>
        [Description("The frame number.")]
        public int FrameNumber
        {
            get { return (int)GetValue(FrameNumberProperty); }
            set { SetCurrentValue(FrameNumberProperty, value); }
        }

        #endregion

        static FrameListBoxItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FrameListBoxItem), new FrameworkPropertyMetadata(typeof(FrameListBoxItem)));

            ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(FrameListBoxItem), new FrameworkPropertyMetadata());
            IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(FrameListBoxItem), new FrameworkPropertyMetadata(false));
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(FrameListBoxItem), new FrameworkPropertyMetadata(20.0));
            FrameNumberProperty = DependencyProperty.Register("FrameNumber", typeof(int), typeof(FrameListBoxItem), new FrameworkPropertyMetadata(0));
        }
    }
}
