using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using ScreenToGif.Util;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;

namespace ScreenToGif.Controls
{
    public class StatusBand : Control
    {
        #region Variables

        private Grid _warningGrid;
        private Button _supressButton;

        #endregion

        #region Dependency Properties/Events

        public static readonly DependencyProperty IdProperty = DependencyProperty.Register("Id", typeof(int), typeof(StatusBand),
            new FrameworkPropertyMetadata(0));

        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(StatusType), typeof(StatusBand),
            new FrameworkPropertyMetadata(StatusType.Warning, OnTypePropertyChanged));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(StatusBand),
            new FrameworkPropertyMetadata("", OnTextPropertyChanged));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(UIElement), typeof(StatusBand),
            new FrameworkPropertyMetadata(null, OnImagePropertyChanged));

        public static readonly DependencyProperty IsLinkProperty = DependencyProperty.Register("IsLink", typeof(bool), typeof(StatusBand),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty StartingProperty = DependencyProperty.Register("Starting", typeof(bool), typeof(StatusBand), 
            new PropertyMetadata(default(bool)));

        public static readonly RoutedEvent DismissedEvent = EventManager.RegisterRoutedEvent("Dismissed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(StatusBand));

        #endregion

        #region Properties

        [Bindable(true), Category("Common")]
        public int Id
        {
            get => (int)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        [Bindable(true), Category("Common")]
        public StatusType Type
        {
            get => (StatusType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        [Bindable(true), Category("Common")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        [Bindable(true), Category("Common")]
        public UIElement Image
        {
            get => (UIElement)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        [Bindable(true), Category("Common")]
        public bool IsLink
        {
            get => (bool)GetValue(IsLinkProperty);
            set => SetValue(IsLinkProperty, value);
        }

        /// <summary>
        /// True if started to display the message.
        /// </summary>
        [Bindable(true), Category("Common")]
        public bool Starting
        {
            get => (bool)GetValue(StartingProperty);
            set => SetValue(StartingProperty, value);
        }

        /// <summary>
        /// Event raised when the StatusBand gets dismissed/supressed.
        /// </summary>
        public event RoutedEventHandler Dismissed
        {
            add => AddHandler(DismissedEvent, value);
            remove => RemoveHandler(DismissedEvent, value);
        }

        public Action Action { get; set; }

        #endregion

        #region Property Changed

        private static void OnTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var band = d as StatusBand;

            if (band == null)
                return;

            band.Type = (StatusType)e.NewValue;
            band.Image = (Canvas)band.FindResource(band.Type == StatusType.Info ? "Vector.Info" : band.Type == StatusType.Warning ? "Vector.Warning" : "Vector.Cancel.Round");
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
            var link = GetTemplateChild("MainHyperlink") as Hyperlink;
            _supressButton = GetTemplateChild("SuppressButton") as ImageButton;

            if (_supressButton != null)
                _supressButton.Click += SupressButton_Click;

            if (Action != null && link != null)
                link.Click += (sender, args) => Action.Invoke();

            base.OnApplyTemplate();
        }

        #region Methods

        public void Show(StatusType type, string text, UIElement image = null, Action action = null)
        {
            Action = action;

            //Collapsed-by-default elements do not apply templates.
            //http://stackoverflow.com/a/2115873/1735672
            //So it's necessary to do this here.
            ApplyTemplate();

            Starting = true;
            Type = type;
            Text = text;
            Image = image;
            IsLink = action != null;

            if (_warningGrid?.FindResource("ShowWarningStoryboard") is Storyboard show)
                BeginStoryboard(show);
        }

        public void Update(string text, UIElement image = null, Action action = null)
        {
            Show(StatusType.Update, text, image ?? (Canvas)FindResource("Vector.Synchronize"), action);
        }

        public void Info(string text, UIElement image = null, Action action = null)
        {
            Show(StatusType.Info, text, image ?? (Canvas)FindResource("Vector.Info"), action);
        }

        public void Warning(string text, UIElement image = null, Action action = null)
        {
            Show(StatusType.Warning, text, image ?? (Canvas)FindResource("Vector.Warning"), action);
        }

        public void Error(string text, UIElement image = null, Action action = null)
        {
            Show(StatusType.Error, text, image ?? (Canvas)FindResource("Vector.Cancel.Round"), action);
        }

        public void Hide()
        {
            Starting = false;

            if (_warningGrid?.Visibility == Visibility.Collapsed)
                return;

            if (_warningGrid?.FindResource("HideWarningStoryboard") is Storyboard hide)
                BeginStoryboard(hide);

            RaiseDismissedEvent();
        }

        public void RaiseDismissedEvent()
        {
            if (DismissedEvent == null || !IsLoaded)
                return;

            var newEventArgs = new RoutedEventArgs(DismissedEvent);
            RaiseEvent(newEventArgs);
        }

        public static string KindToString(StatusType kind)
        {
            return "Vector." + (kind == StatusType.None ? "Tag" : kind == StatusType.Info ? "Info" : kind == StatusType.Update ? "Synchronize" : kind == StatusType.Warning ? "Warning" : "Error");
        }

        #endregion

        private void SupressButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}