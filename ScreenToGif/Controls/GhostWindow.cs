using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    public class GhostWindow : Window
    {
        private AdornerLayer _adornerLayer;
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

        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register("Region", typeof(Rect), typeof(GhostWindow),
            new PropertyMetadata(Rect.Empty));

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

        public Rect Region
        {
            get { return (Rect)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        private CroppingAdorner _cropAdorner;

        #endregion

        static GhostWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GhostWindow), new FrameworkPropertyMetadata(typeof(GhostWindow)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

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
                _mainBorder.MouseLeftButtonDown += MainBorder_MouseLeftButtonDown;
                _mainBorder.MouseLeftButtonUp += MainBorder_MouseLeftButtonUp;
                _mainBorder.MouseMove += MainBorder_MouseMove;

                //TODO: Center on current screen.
                Canvas.SetLeft(_mainBorder, 300);
                Canvas.SetTop(_mainBorder, 200);
            }

            if (_mainCanvas != null)
                _adornerLayer = AdornerLayer.GetAdornerLayer(_mainCanvas);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsPickingRegion)
                return;

            Region = new Rect(e.GetPosition(this), new Size(0, 0));

            _cropAdorner = new CroppingAdorner(_mainCanvas, Region)
            {
                Fill = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
            };

            _adornerLayer.Add(_cropAdorner);

            CaptureMouse();

            e.Handled = true;

            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!IsPickingRegion || e.LeftButton == MouseButtonState.Released)
                return;

            var current = e.GetPosition(this);

            var topLeft = new Point();
            var bottomRight = new Point();

            //Y smaller than Location.
            topLeft.Y = current.Y < Region.Location.Y ? current.Y : Region.Location.Y;
            bottomRight.Y = current.Y < Region.Location.Y ? Region.Location.Y : current.Y;

            //X smaller than location.
            topLeft.X = current.X < Region.Location.X ? current.X : Region.Location.X;
            bottomRight.X = current.X < Region.Location.X ? Region.Location.X : current.X;

            //X and Y greater than Location.
            Region = new Rect(topLeft, bottomRight);

            _cropAdorner.ClipRectangle = Region;

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!IsPickingRegion)
                return;

            ReleaseMouseCapture();
            EndPickRegion();

            e.Handled = true;

            base.OnMouseLeftButtonUp(e);
        }

        #region Events

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var draggableControl = sender as Border;

            if (draggableControl == null)
                return;

            IsDragging = true;

            _latestPosition = e.GetPosition(_mainBorder);
            draggableControl.CaptureMouse();

            e.Handled = true;
        }

        private void MainBorder_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as Border;

            if (!IsDragging || draggableControl == null) return;

            var currentPosition = e.GetPosition(Parent as UIElement);

            Canvas.SetLeft(_mainBorder, currentPosition.X - _latestPosition.X);
            Canvas.SetTop(_mainBorder, currentPosition.Y - _latestPosition.Y);

            e.Handled = true;
        }

        private void MainBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsDragging = false;

            var draggable = sender as Border;
            draggable?.ReleaseMouseCapture();

            e.Handled = true;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            PickRegion();
        }

        #endregion

        #region Methods

        private void PickRegion()
        {
            IsPickingRegion = true;
        }

        private void EndPickRegion()
        {
            IsPickingRegion = false;

        }

        private void UpdateAdorner()
        {
            
        }

        #endregion

        //Start:
        //Small screen with some options.
        //Fill all screens with 20% white background.
        //Change cursor, follow mouse (to display info about position and size)
        //When dragging, create punctured rect.
        //After releasing the mouse capture, show resize adorner and recording controls.
    }
}
