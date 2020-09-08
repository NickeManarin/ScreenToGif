using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenToGif.Controls.Ribbon
{
    public class RibbonTab : TabItem
    {
        #region Properties

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Image), typeof(Brush), typeof(RibbonTab));
        
        public static readonly DependencyProperty DisplayAccentProperty = DependencyProperty.Register(nameof(DisplayAccent), typeof(bool), typeof(RibbonTab), new PropertyMetadata(true));

        /// <summary>
        /// The icon of the tab.
        /// </summary>
        [Description("The icon of the tab.")]
        public Brush Icon
        {
            get => (Brush)GetValue(IconProperty);
            set => SetCurrentValue(IconProperty, value);
        }        
        
        public bool DisplayAccent
        {
            get => (bool)GetValue(DisplayAccentProperty);
            set => SetCurrentValue(DisplayAccentProperty, value);
        }
        
        #endregion

        static RibbonTab()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTab), new FrameworkPropertyMetadata(typeof(RibbonTab)));
        }
    }
}