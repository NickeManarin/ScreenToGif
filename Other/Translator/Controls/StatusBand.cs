using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Translator.Controls
{
    public class StatusBand : Control
    {
        #region Variables

        public enum StatusType
        {
            Info,
            Warning,
            Error
        }

        private Grid _warningGrid;
        private Button _supressButton;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(StatusType), typeof(StatusBand),
            new FrameworkPropertyMetadata(StatusType.Warning, OnTypePropertyChanged));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(StatusBand),
            new FrameworkPropertyMetadata("", OnTextPropertyChanged));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(StatusBand),
            new FrameworkPropertyMetadata(null, OnImagePropertyChanged));

        public static readonly DependencyProperty StartingProperty = DependencyProperty.Register("Starting", typeof(bool), typeof(StatusBand), 
            new PropertyMetadata(default(bool)));

        #endregion

        #region Properties

        [Bindable(true), Category("Common")]
        public StatusType Type
        {
            get { return (StatusType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        [Bindable(true), Category("Common")]
        public UIElement Image
        {
            get { return (UIElement)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        /// <summary>
        /// True if started to display the message.
        /// </summary>
        [Bindable(true), Category("Common")]
        public bool Starting
        {
            get { return (bool)GetValue(StartingProperty); }
            set { SetValue(StartingProperty, value); }
        }

        #endregion

        #region Property Changed

        private static void OnTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var band = d as StatusBand;

            if (band == null)
                return;

            band.Type = (StatusType)e.NewValue;
            band.Image = (Canvas)band.FindResource(band.Type == StatusType.Info ? "Vector.Info" : band.Type == StatusType.Warning ? "Vector.Warning" : "Vector.Error");
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var band = d as StatusBand;

            if (band == null)
                return;

            band.Text = (string)e.NewValue;
        }

        private static void OnImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var band = d as StatusBand;

            if (band == null)
                return;

            band.Image = (UIElement)e.NewValue;
        }

        #endregion

        static StatusBand()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusBand), new FrameworkPropertyMetadata(typeof(StatusBand)));
        }

        public override void OnApplyTemplate()
        {
            _warningGrid = GetTemplateChild("WarningGrid") as Grid;
            _supressButton = GetTemplateChild("SuppressButton") as ImageButton;

            if (_supressButton != null)
            {
                _supressButton.Click += SupressButton_Click;
            }

            base.OnApplyTemplate();
        }

        #region Methods

        public void Show(StatusType type, string text, UIElement image = null)
        {
            //Collapsed-by-default elements do not apply templates.
            //http://stackoverflow.com/a/2115873/1735672
            //So it's necessary to do this here.
            ApplyTemplate();

            Starting = true;
            Type = type;
            Text = text;
            Image = image;

            var show = _warningGrid?.FindResource("ShowWarningStoryboard") as Storyboard;

            if (show != null)
                BeginStoryboard(show);
        }

        public void Info(string text, UIElement image = null)
        {
            Show(StatusType.Info, text, image ?? (Canvas)FindResource("Vector.Info"));
        }

        public void Warning(string text, UIElement image = null)
        {
            Show(StatusType.Warning, text, image ?? (Canvas)FindResource("Vector.Warning"));
        }

        public void Error(string text, UIElement image = null)
        {
            Show(StatusType.Error, text, image ?? (Canvas)FindResource("Vector.Error"));
        }

        public void Hide()
        {
            Starting = false;

            if (_warningGrid?.Visibility == Visibility.Collapsed)
                return;

            var show = _warningGrid?.FindResource("HideWarningStoryboard") as Storyboard;

            if (show != null)
                BeginStoryboard(show);
        }

        #endregion

        private void SupressButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
