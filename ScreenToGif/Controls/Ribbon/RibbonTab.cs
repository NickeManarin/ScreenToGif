using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls.Ribbon
{
    public class RibbonTab : TabItem
    {
        #region Dependency Property

        public static readonly DependencyProperty IsDarkProperty = DependencyProperty.Register(nameof(IsDark), typeof(bool), typeof(RibbonTab),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, IsDark_PropertyChanged));

        public static readonly DependencyProperty ShowBackgroundProperty = DependencyProperty.Register(nameof(ShowBackground), typeof(bool), typeof(RibbonTab),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, ShowBackground_PropertyChanged));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(UIElement), typeof(RibbonTab), new FrameworkPropertyMetadata());

        #endregion

        #region Property Accessor

        /// <summary>
        /// True if the titlebar color is dark.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool IsDark
        {
            get => (bool)GetValue(IsDarkProperty);
            set => SetValue(IsDarkProperty, value);
        }

        /// <summary>
        /// True if should display the background of the tab while not selected.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public bool ShowBackground
        {
            get => (bool)GetValue(ShowBackgroundProperty);
            set => SetValue(ShowBackgroundProperty, value);
        }

        /// <summary>
        /// The Image of the tab.
        /// </summary>
        [Description("The Image of the tab.")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetCurrentValue(ImageProperty, value);
        }
        
        #endregion

        static RibbonTab()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTab), new FrameworkPropertyMetadata(typeof(RibbonTab)));
        }

        #region Events

        private static void IsDark_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonTab)d).IsDark = (bool)e.NewValue; //TODO: Why?
        }

        private static void ShowBackground_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonTab)d).ShowBackground = (bool)e.NewValue; //TODO: Why?
        }

        #endregion
    }
}