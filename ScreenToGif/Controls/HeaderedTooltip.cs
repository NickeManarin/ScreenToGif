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
    public class HeaderedTooltip : ToolTip
    {
        #region Variables

        public readonly static DependencyProperty HeaderProperty;
        public readonly static DependencyProperty TextProperty;
        public readonly static DependencyProperty MaxSizeProperty;

        #endregion

        #region Properties

        /// <summary>
        /// The header of the tooltip.
        /// </summary>
        [Description("The header of the tooltip.")]
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetCurrentValue(HeaderProperty, value); }
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

        static HeaderedTooltip()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedTooltip), new FrameworkPropertyMetadata(typeof(HeaderedTooltip)));

            HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(HeaderedTooltip), new FrameworkPropertyMetadata("Header"));
            TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(HeaderedTooltip), new FrameworkPropertyMetadata("Description"));
            MaxSizeProperty = DependencyProperty.Register("MaxSize", typeof(double), typeof(HeaderedTooltip), new FrameworkPropertyMetadata(200.0));
        }
    }
}
