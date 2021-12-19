#region Usings

using System.Windows;
using System.Windows.Controls;

using ScreenToGif.ViewModel;

#endregion

namespace ScreenToGif.UserControls
{
    /// <summary>
    /// Interaction logic for KGySoftGifOptionsPanel.xaml
    /// </summary>
    public partial class KGySoftGifOptionsPanel : UserControl
    {
        public static readonly DependencyProperty CurrentFrameProperty = DependencyProperty.Register(nameof(CurrentFrame), typeof(FrameViewModel), typeof(KGySoftGifOptionsPanel), new PropertyMetadata(default(FrameViewModel)));

        #region Properties

        public FrameViewModel CurrentFrame
        {
            get => (FrameViewModel)GetValue(CurrentFrameProperty);
            set => SetValue(CurrentFrameProperty, value);
        }

        private KGySoftGifOptionsViewModel ViewModel => DataContext as KGySoftGifOptionsViewModel;

        #endregion

        #region Constructors

        public KGySoftGifOptionsPanel() => InitializeComponent();

        #endregion

        #region Methods

        #region Protected Methods

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == CurrentFrameProperty && ViewModel != null)
                ViewModel.CurrentFramePath = CurrentFrame?.Image;
        }

        #endregion

        #region Event Handlers

        private void KGySoftGifOptionsPanel_OnUnloaded(object sender, RoutedEventArgs e) => ViewModel?.Dispose();

        private void KGySoftGifOptionsPanel_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (e.OldValue as KGySoftGifOptionsViewModel)?.Dispose();
            if (e.NewValue is KGySoftGifOptionsViewModel vm)
                vm.CurrentFramePath = CurrentFrame?.Image;
        }

        #endregion

        #endregion
    }
}