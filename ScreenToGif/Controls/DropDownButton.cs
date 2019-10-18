﻿using System.ComponentModel;
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

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(DropDownButton), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(DropDownButton), new FrameworkPropertyMetadata());
        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register(nameof(MaxSize), typeof(double), typeof(DropDownButton), new FrameworkPropertyMetadata(26.0));
        public static readonly DependencyProperty IsVerticalProperty = DependencyProperty.Register(nameof(IsVertical), typeof(bool), typeof(DropDownButton), new FrameworkPropertyMetadata(false));

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the button.
        /// </summary>
        [Description("The Image of the DropDownButton."), Category("Common")]
        public UIElement Content
        {
            get => (UIElement)GetValue(ContentProperty);
            set => SetCurrentValue(ContentProperty, value);
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
        /// The text of the control.
        /// </summary>
        [Description("The text of the control."), Category("Common")]
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetCurrentValue(DescriptionProperty, value);
        }

        /// <summary>
        /// True if vertical style.
        /// </summary>
        [Description("True if vertical style."), Category("Common")]
        public bool IsVertical
        {
            get => (bool)GetValue(IsVerticalProperty);
            set => SetCurrentValue(IsVerticalProperty, value);
        }

        #endregion

        static DropDownButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));
        }
    }
}