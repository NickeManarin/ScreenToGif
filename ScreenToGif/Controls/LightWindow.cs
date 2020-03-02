using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Light Window used by some recorder windows.
    /// </summary>
    public class LightWindow : RecorderWindow
    {
        private HwndSource _hwndSource;

        #region Dependency Property

        public static readonly DependencyProperty ChildProperty = DependencyProperty.Register(nameof(Child), typeof(UIElement), typeof(LightWindow),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxSizeProperty = DependencyProperty.Register(nameof(MaxSize), typeof(double), typeof(LightWindow),
            new FrameworkPropertyMetadata(26.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackVisibilityProperty = DependencyProperty.Register(nameof(BackVisibility), typeof(Visibility), typeof(LightWindow),
            new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MinimizeVisibilityProperty = DependencyProperty.Register(nameof(MinimizeVisibility), typeof(Visibility), typeof(LightWindow),
            new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(LightWindow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsThinProperty = DependencyProperty.Register(nameof(IsThin), typeof(bool), typeof(LightWindow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsFullScreenProperty = DependencyProperty.Register(nameof(IsFullScreen), typeof(bool), typeof(LightWindow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsFollowingProperty = DependencyProperty.Register(nameof(IsFollowing), typeof(bool), typeof(LightWindow), 
            new PropertyMetadata(false, IsFollowing_PropertyChanged));

        #endregion

        #region Property Accessor

        /// <summary>
        /// The Image of the caption bar.
        /// </summary>
        [Bindable(true), Category("Common"), Description("The Image of the caption bar.")]
        public UIElement Child
        {
            get => (UIElement)GetValue(ChildProperty);
            set => SetCurrentValue(ChildProperty, value);
        }

        /// <summary>
        /// The maximum size of the image.
        /// </summary>
        [Bindable(true), Category("Common"), Description("The maximum size of the image.")]
        public double MaxSize
        {
            get => (double)GetValue(MaxSizeProperty);
            set => SetCurrentValue(MaxSizeProperty, value);
        }

        /// <summary>
        /// Back button visibility.
        /// </summary>
        [Bindable(true), Category("Common"), Description("Back button visibility.")]
        public Visibility BackVisibility
        {
            get => (Visibility)GetValue(BackVisibilityProperty);
            set => SetCurrentValue(BackVisibilityProperty, value);
        }

        /// <summary>
        /// Minimize button visibility.
        /// </summary>
        [Bindable(true), Category("Common"), Description("Minimize button visibility.")]
        public Visibility MinimizeVisibility
        {
            get => (Visibility)GetValue(MinimizeVisibilityProperty);
            set => SetCurrentValue(MinimizeVisibilityProperty, value);
        }

        /// <summary>
        /// If in recording mode.
        /// </summary>
        [Bindable(true), Category("Common"), Description("If in recording mode.")]
        public bool IsRecording
        {
            get => (bool)GetValue(IsRecordingProperty);
            set => SetCurrentValue(IsRecordingProperty, value);
        }

        /// <summary>
        /// Thin mode (hides the title bar).
        /// </summary>
        [Bindable(true), Category("Common"), Description("Thin mode (hides the title bar).")]
        public bool IsThin
        {
            get => (bool)GetValue(IsThinProperty);
            set => SetCurrentValue(IsThinProperty, value);
        }

        /// <summary>
        /// Fullscreen mode (hides the title bar).
        /// </summary>
        [Bindable(true), Category("Common"), Description("Fullscreen mode (hides the title bar and a few other things).")]
        public bool IsFullScreen
        {
            get => (bool)GetValue(IsFullScreenProperty);
            set => SetCurrentValue(IsFullScreenProperty, value);
        }

        /// <summary>
        /// True if the window should follow the mouse cursor.
        /// </summary>
        public bool IsFollowing
        {
            get => (bool)GetValue(IsFollowingProperty);
            set => SetValue(IsFollowingProperty, value);
        }

        #endregion


        static LightWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LightWindow), new FrameworkPropertyMetadata(typeof(LightWindow)));
        }


        #region Overrides

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("BackButton") is ImageButton backButton)
                backButton.Click += BackClick;

            if (GetTemplateChild("MinimizeButton") is Button minimizeButton)
                minimizeButton.Click += MinimizeClick;

            if (GetTemplateChild("CloseButton") is Button closeButton)
                closeButton.Click += CloseClick;

            if (GetTemplateChild("MoveGrid") is Grid moveGrid)
                moveGrid.PreviewMouseDown += MoveGrid_PreviewMouseDown;

            if (GetTemplateChild("MainGrid") is Grid resizeGrid)
            {
                foreach (var element in resizeGrid.Children.OfType<Rectangle>())
                    element.PreviewMouseDown += ResizeRectangle_PreviewMouseDown;
            }

            base.OnApplyTemplate();
        }

        protected override void OnInitialized(EventArgs e)
        {
            SourceInitialized += Window_SourceInitialized;

            base.OnInitialized(e);
        }

        #endregion

        #region Methods

        private void ResizeWindow(ResizeDirection direction)
        {
            Native.SendMessage(_hwndSource.Handle, 0x112, (IntPtr)(61440 + direction), IntPtr.Zero);
        }

        internal void HideInternals()
        {
            if (GetTemplateChild("MainBorder") is Border border)
                border.Visibility = Visibility.Hidden;
        }

        internal void ShowInternals()
        {
            if (GetTemplateChild("MainBorder") is Border border)
                border.Visibility = Visibility.Visible;
        }
        
        internal virtual void OnFollowingChanged() 
        { }

        #endregion

        #region Events

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        }

        private void BackClick(object sender, RoutedEventArgs routedEventArgs)
        {
            DialogResult = false;
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void RestoreClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;

                if (sender is Button button) 
                    button.Content = FindResource("Vector.Restore");
            }
            else
            {
                WindowState = WindowState.Normal;

                if (sender is Button button) 
                    button.Content = FindResource("Vector.Maximize");
            }
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MoveGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void ResizeRectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (!(sender is Rectangle rectangle))
                return;

            switch (rectangle.Name)
            {
                case "TopRectangle":
                    ResizeWindow(ResizeDirection.Top);
                    break;
                case "BottomRectangle":
                    ResizeWindow(ResizeDirection.Bottom);
                    break;
                case "LeftRectangle":
                    ResizeWindow(ResizeDirection.Left);
                    break;
                case "RightRectangle":
                    ResizeWindow(ResizeDirection.Right);
                    break;
                case "TopLeftRectangle":
                    ResizeWindow(ResizeDirection.TopLeft);
                    break;
                case "TopRightRectangle":
                    ResizeWindow(ResizeDirection.TopRight);
                    break;
                case "BottomLeftRectangle":
                    ResizeWindow(ResizeDirection.BottomLeft);
                    break;
                case "BottomRightRectangle":
                    ResizeWindow(ResizeDirection.BottomRight);
                    break;
            }
        }

        private static void IsFollowing_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is LightWindow win))
                return;

            win.OnFollowingChanged();
        }

        #endregion
    }
}