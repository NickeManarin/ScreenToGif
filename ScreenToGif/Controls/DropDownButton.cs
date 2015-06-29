using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// A non-editable ComboBox style.
    /// </summary>
    public class DropDownButton : ComboBox
    {
        #region Variables

        public readonly static DependencyProperty ContentProperty;
        public readonly static DependencyProperty DescriptionProperty;
        public readonly static DependencyProperty MaxSizeProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the button.
        /// </summary>
        [Description("The Image of the DropDownButton."), Category("Common")]
        public UIElement Content
        {
            get { return (UIElement)GetValue(ContentProperty); }
            set { SetCurrentValue(ContentProperty, value); }
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

        /// <summary>
        /// The text of the control.
        /// </summary>
        [Description("The text of the control."), Category("Common")]
        public String Description
        {
            get { return (String)GetValue(DescriptionProperty); }
            set { SetCurrentValue(DescriptionProperty, value); }
        }

        #endregion

        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));

            ContentProperty = DependencyProperty.Register("Content", typeof(UIElement), typeof(DropDownButton), new FrameworkPropertyMetadata());
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(DropDownButton), new FrameworkPropertyMetadata(26.0));
            DescriptionProperty = DependencyProperty.Register("Description", typeof(String), typeof(DropDownButton), new FrameworkPropertyMetadata());
        }
    }
}
