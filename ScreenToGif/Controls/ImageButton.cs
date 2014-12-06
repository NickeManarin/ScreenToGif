using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Button with a image inside.
    /// </summary>
    public class ImageButton : Button
    {
        #region Variables

        public Viewbox _viewbox;
        public TextBlock _label;

        public readonly static DependencyProperty ChildProperty;
        public readonly static DependencyProperty TextProperty;
        public readonly static DependencyProperty MaxSizeProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The Image of the button.
        /// </summary>
        [Description("The Image of the button.")]
        public UIElement Child
        {
            get { return (UIElement)GetValue(ChildProperty); }
            set { SetCurrentValue(ChildProperty, value); }
        }

        /// <summary>
        /// The text of the button.
        /// </summary>
        [Description("The text of the button.")]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetCurrentValue(TextProperty, value); }
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

        static ImageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton), new FrameworkPropertyMetadata(typeof(ImageButton)));

            ChildProperty = DependencyProperty.Register("Child", typeof(UIElement), typeof(ImageButton), new FrameworkPropertyMetadata());
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ImageButton), new FrameworkPropertyMetadata("Button"));
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(ImageButton), new FrameworkPropertyMetadata(26.0));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _viewbox = Template.FindName("ViewBoxInternal", this) as Viewbox;
            _label = Template.FindName("TextBlockInternal", this) as TextBlock;
        }
    }
}
