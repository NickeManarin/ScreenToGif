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

        #endregion

        #region Property Accessor

        [Bindable(true), Category("Appearance")]
        public bool IsDark
        {
            get { return (bool)GetValue(IsDarkProperty); }
            set { SetValue(IsDarkProperty, value); }
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
    }
}
