using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class GhostWindow : Window
    {
        #region Variables

        private Border _mainBorder;
        private ImageButton _selectButton;
        private Point _latestPosition;
        private SelectControl _selectControl;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register("IsPickingRegion", typeof(bool), typeof(GhostWindow),
            new PropertyMetadata(false));

        public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(Stage), typeof(GhostWindow), new FrameworkPropertyMetadata(Stage.Stopped));

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

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage
        {
            get { return (Stage)GetValue(StageProperty); }
            set { SetValue(StageProperty, value); }
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

            //_mainCanvas = Template.FindName("MainCanvas", this) as Canvas;
            _mainBorder = Template.FindName("MainBorder", this) as Border;
            _selectControl = Template.FindName("SelectControl", this) as SelectControl;
            _selectButton = Template.FindName("SelectButton", this) as ImageButton;

            if (_mainBorder != null)
            {
                _mainBorder.MouseLeftButtonDown += MainBorder_MouseLeftButtonDown;
                _mainBorder.MouseLeftButtonUp += MainBorder_MouseLeftButtonUp;
                _mainBorder.MouseMove += MainBorder_MouseMove;

                var screen = Monitor.AllMonitors.FirstOrDefault(x => x.Bounds.Contains(Mouse.GetPosition(this)));

                //TODO: what of I couldn't find the screen?
                if (screen != null)
                {
                    Canvas.SetLeft(_mainBorder, screen.WorkingArea.Left + screen.WorkingArea.Width / 2 - _mainBorder.ActualWidth / 2);
                    Canvas.SetTop(_mainBorder, screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - _mainBorder.ActualHeight / 2);
                }
            }

            if (_selectButton != null)
                _selectButton.Click += SelectButton_Click;

            if (_selectControl != null)
            {
                _selectControl.SelectionAccepted += SelectControl_SelectionAccepted;
                _selectControl.SelectionCanceled += SelectControl_SelectionCanceled;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!IsPickingRegion)
                    Close();

                IsPickingRegion = false;
            }

            base.OnKeyDown(e);
        }

        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            //TODO: enable capture.
        }

        private void SelectControl_SelectionCanceled(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            //TODO: ?
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
            //Reset the values.
            _selectControl.Retry();

            IsPickingRegion = true;
        }

        private void EndPickRegion()
        {
            IsPickingRegion = false;

        }

        #endregion
    }
}
