using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    public class GhostWindow : Window
    {
        private Border _mainBorder;
        private Canvas _mainCanvas;
        private ImageButton _selectButton;
        private Point _latestPosition;

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register("IsPickingRegion", typeof(bool), typeof(GhostWindow), 
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register("IsRecording", typeof(bool), typeof(GhostWindow), 
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register("IsDragging", typeof(bool), typeof(GhostWindow), 
            new PropertyMetadata(false));

        #endregion

        #region Properties

        public bool IsPickingRegion
        {
            get { return (bool)GetValue(IsPickingRegionProperty); }
            set { SetValue(IsPickingRegionProperty, value); }
        }

        public bool IsRecording
        {
            get { return (bool)GetValue(IsRecordingProperty); }
            set { SetValue(IsRecordingProperty, value); }
        }

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        #endregion

        static GhostWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GhostWindow), new FrameworkPropertyMetadata(typeof(GhostWindow)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Loaded += OnLoaded;

            #region Fill entire working space

            Left = 0;
            Top = 0;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            #endregion

            _mainCanvas = Template.FindName("MainCanvas", this) as Canvas;
            _mainBorder = Template.FindName("MainBorder", this) as Border;
            _selectButton = Template.FindName("SelectButton", this) as ImageButton;

            if (_selectButton != null)
                _selectButton.Click += SelectButton_Click;

            if (_mainBorder != null)
            {
                _mainBorder.MouseLeftButtonDown += Control_MouseLeftButtonDown;
                _mainBorder.MouseLeftButtonUp += Control_MouseLeftButtonUp;
                _mainBorder.MouseMove += Control_MouseMove;

                //TODO: Center on current screen.
                Canvas.SetLeft(_mainBorder, 300);
                Canvas.SetTop(_mainBorder, 200);
            }
        }

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var draggableControl = sender as Border;

            if (draggableControl == null)
                return;

            IsDragging = true;

            _latestPosition = e.GetPosition(this);
            draggableControl.CaptureMouse();
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as Border;

            if (!IsDragging || draggableControl == null) return;

            var currentPosition = e.GetPosition(Parent as UIElement);

            var transform = draggableControl.RenderTransform as TranslateTransform;

            if (transform == null)
            {
                transform = new TranslateTransform();
                draggableControl.RenderTransform = transform;
            }

            transform.X = currentPosition.X - _latestPosition.X;
            transform.Y = currentPosition.Y - _latestPosition.Y;
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsDragging = false;

            var draggable = sender as Border;
            draggable?.ReleaseMouseCapture();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            IsPickingRegion = !IsPickingRegion;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            //Display the MainGrid on the main screen. 
        }

        //Start:
        //Small screen with some options.
        //Fill all screens with 20% white background.
        //Change cursor, follow mouse (to display info about position and size)
        //When dragging, create punctured rect.
        //After releasing the mouse capture, show resize adorner and recording controls.
    }
}
