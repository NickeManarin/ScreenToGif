using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenToGif.Controls
{
    public class AwareTabItem : TabItem
    {
        #region Dependency Property

        public static readonly DependencyProperty IsDarkProperty = DependencyProperty.Register("IsDark", typeof(bool), typeof(AwareTabItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnPropertyChanged));

        public static readonly DependencyProperty ShowBackgroundProperty = DependencyProperty.Register("ShowBackground", typeof(bool), typeof(AwareTabItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, ShowBackground_OnPropertyChanged));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(AwareTabItem), new FrameworkPropertyMetadata());

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

        static AwareTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AwareTabItem), new FrameworkPropertyMetadata(typeof(AwareTabItem)));
        }

        /// <summary>
        /// This method is called when any of our dependency properties change.
        /// </summary>
        /// <param name="d">Depedency Object</param>
        /// <param name="e">EventArgs</param>
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AwareTabItem)d).IsDark = (bool)e.NewValue;
        }

        /// <summary>
        /// This method is called when any of our dependency properties change.
        /// </summary>
        /// <param name="d">Depedency Object</param>
        /// <param name="e">EventArgs</param>
        private static void ShowBackground_OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AwareTabItem)d).ShowBackground = (bool)e.NewValue;
        }
    }
}