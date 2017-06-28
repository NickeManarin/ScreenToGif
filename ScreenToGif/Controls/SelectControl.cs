using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Util;

namespace ScreenToGif.Controls
{
    public class SelectControl : Control
    {
        #region Variables

        /// <summary>
        /// Resizing adorner uses Thumbs for visual elements.  
        /// The Thumbs have built-in mouse input handling.
        /// </summary>
        private Thumb _topLeft, _topRight, _bottomLeft, _bottomRight, _top, _bottom, _left, _right;

        /// <summary>
        /// The selection rectangle, used to drag the selection Rect elsewhere.
        /// </summary>
        private Rectangle _rectangle;

        /// <summary>
        /// The grid that holds the three buttons to control the selection.
        /// </summary>
        private Grid _statusControlGrid;

        /// <summary>
        /// The main canvas, the root element.
        /// </summary>
        private Canvas _mainCanvas;

        /// <summary>
        /// Status control buttons.
        /// </summary>
        private ImageButton _acceptButton, _retryButton, _cancelButton;

        /// <summary>
        /// The start point for the drag operation.
        /// </summary>
        private Point _startPoint;

        public enum ModeType
        {
            Region,
            Window,
            Fullscreen
        }

        public List<DetectedRegion> Windows = new List<DetectedRegion>();

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register("IsPickingRegion", typeof(bool), typeof(SelectControl), new PropertyMetadata(true));

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(Rect), typeof(SelectControl), new PropertyMetadata(new Rect(-1, -1, 0, 0)));

        public static readonly DependencyProperty FinishedSelectionProperty = DependencyProperty.Register("FinishedSelection", typeof(bool), typeof(SelectControl), new PropertyMetadata(false));

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register("Mode", typeof(ModeType), typeof(SelectControl), new PropertyMetadata(ModeType.Region, Mode_Changed));

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register("Scale", typeof(double), typeof(SelectControl), new PropertyMetadata(1d, Mode_Changed));

        public static readonly RoutedEvent SelectionAcceptedEvent = EventManager.RegisterRoutedEvent("SelectionAccepted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        public static readonly RoutedEvent SelectionCanceledEvent = EventManager.RegisterRoutedEvent("SelectionCanceled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        #endregion

        #region Properties

        public bool IsPickingRegion
        {
            get => (bool)GetValue(IsPickingRegionProperty);
            set => SetValue(IsPickingRegionProperty, value);
        }

        public Rect Selected
        {
            get => (Rect)GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }

        public bool FinishedSelection
        {
            get => (bool)GetValue(FinishedSelectionProperty);
            set => SetValue(FinishedSelectionProperty, value);
        }

        public ModeType Mode
        {
            get => (ModeType)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public event RoutedEventHandler SelectionAccepted
        {
            add => AddHandler(SelectionAcceptedEvent, value);
            remove => RemoveHandler(SelectionAcceptedEvent, value);
        }

        public event RoutedEventHandler SelectionCanceled
        {
            add => AddHandler(SelectionCanceledEvent, value);
            remove => RemoveHandler(SelectionCanceledEvent, value);
        }

        #endregion

        static SelectControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SelectControl), new FrameworkPropertyMetadata(typeof(SelectControl)));
        }

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _mainCanvas = Template.FindName("MainCanvas", this) as Canvas;

            _topLeft = Template.FindName("TopLeftThumb", this) as Thumb;
            _topRight = Template.FindName("TopRightThumb", this) as Thumb;
            _bottomLeft = Template.FindName("BottomLeftThumb", this) as Thumb;
            _bottomRight = Template.FindName("BottomRightThumb", this) as Thumb;

            _top = Template.FindName("TopThumb", this) as Thumb;
            _bottom = Template.FindName("BottomThumb", this) as Thumb;
            _left = Template.FindName("LeftThumb", this) as Thumb;
            _right = Template.FindName("RightThumb", this) as Thumb;

            _rectangle = Template.FindName("SelectRectangle", this) as Rectangle;
            _statusControlGrid = Template.FindName("StatusControlGrid", this) as Grid;
            _acceptButton = Template.FindName("AcceptButton", this) as ImageButton;
            _retryButton = Template.FindName("RetryButton", this) as ImageButton;
            _cancelButton = Template.FindName("CancelButton", this) as ImageButton;

            if (_topLeft == null || _topRight == null || _bottomLeft == null || _bottomRight == null ||
                _top == null || _bottom == null || _left == null || _right == null || _rectangle == null || _mainCanvas == null)
                return;

            //Add handlers for resizing • Corners.
            _topLeft.DragDelta += HandleTopLeft;
            _topRight.DragDelta += HandleTopRight;
            _bottomLeft.DragDelta += HandleBottomLeft;
            _bottomRight.DragDelta += HandleBottomRight;

            //Add handlers for resizing • Sides.
            _top.DragDelta += HandleTop;
            _bottom.DragDelta += HandleBottom;
            _left.DragDelta += HandleLeft;
            _right.DragDelta += HandleRight;

            //Drag to move.
            _rectangle.MouseLeftButtonDown += Rectangle_MouseLeftButtonDown;
            _rectangle.MouseMove += Rectangle_MouseMove;
            _rectangle.MouseLeftButtonUp += Rectangle_MouseLeftButtonUp;

            if (_acceptButton == null || _retryButton == null || _cancelButton == null)
                return;

            _acceptButton.Click += (sender, e) => { Accept(); };
            _retryButton.Click += (sender, e) => { Retry(); };
            _cancelButton.Click += (sender, e) => { Cancel(); };

            #region Esc to cancel

            //TODO: I should do this elsewhere. What if the user adds/removes a window after this was created?
            foreach (var monitor in Monitor.AllMonitorsScaled(Scale))
            {
                var textPath = new TextPath
                {
                    IsHitTestVisible = false,
                    Text = TryFindResource("S.Recorder.EscToCancel") as string ?? "",
                    Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                    Stroke = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 90,
                    FontWeight = FontWeights.SemiBold
                };

                textPath.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                textPath.Arrange(new Rect(textPath.DesiredSize));

                _mainCanvas.Children.Add(textPath);

                Canvas.SetLeft(textPath, monitor.Bounds.Left + (monitor.Bounds.Width / 2) - textPath.ActualWidth / 2);
                Canvas.SetTop(textPath, monitor.Bounds.Top + (monitor.Bounds.Height / 2) - textPath.ActualHeight / 2);
            }

            #endregion
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);

            if (Mode == ModeType.Region)
            {
                Selected = new Rect(e.GetPosition(this), new Size(0, 0));

                CaptureMouse();
                HideStatusControls();

                FinishedSelection = false;
            }
            else
            {
                if (Selected.Width > 0 && Selected.Height > 0)
                    RaiseAcceptedEvent();
            }

            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (Mode == ModeType.Region)
                Retry();

            e.Handled = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Mode == ModeType.Region)
            {
                if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
                    return;

                var current = e.GetPosition(this);

                Selected = new Rect(Math.Min(current.X, _startPoint.X), Math.Min(current.Y, _startPoint.Y),
                    Math.Abs(current.X - _startPoint.X), Math.Abs(current.Y - _startPoint.Y));
            }
            else
            {
                var current = e.GetPosition(this);

                Selected = Windows.FirstOrDefault(x => x.Bounds.Contains(current))?.Bounds ?? new Rect(-1, -1, 0, 0);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (Mode == ModeType.Region)
            {
                ReleaseMouseCapture();

                if (Selected.Width < 10 || Selected.Height < 10)
                {
                    OnMouseRightButtonDown(e);
                    return;
                }

                AdjustThumbs();
                ShowStatusControls();

                FinishedSelection = true;
            }

            //e.Handled = true;
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Cancel();

            if (e.Key == Key.Enter)
                Accept();

            e.Handled = true;

            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region Methods

        private void AdjustThumbs()
        {
            //Top left.
            Canvas.SetLeft(_topLeft, Selected.Left - _topLeft.Width / 2);
            Canvas.SetTop(_topLeft, Selected.Top - _topLeft.Height / 2);

            //Top right.
            Canvas.SetLeft(_topRight, Selected.Right - _topRight.Width / 2);
            Canvas.SetTop(_topRight, Selected.Top - _topRight.Height / 2);

            //Bottom left.
            Canvas.SetLeft(_bottomLeft, Selected.Left - _bottomLeft.Width / 2);
            Canvas.SetTop(_bottomLeft, Selected.Bottom - _bottomLeft.Height / 2);

            //Bottom right.
            Canvas.SetLeft(_bottomRight, Selected.Right - _bottomRight.Width / 2);
            Canvas.SetTop(_bottomRight, Selected.Bottom - _bottomRight.Height / 2);

            //Top.
            Canvas.SetLeft(_top, Selected.Left + Selected.Width / 2 - _top.Width / 2);
            Canvas.SetTop(_top, Selected.Top - _top.Height / 2);

            //Left.
            Canvas.SetLeft(_left, Selected.Left - _left.Width / 2);
            Canvas.SetTop(_left, Selected.Top + Selected.Height / 2 - _left.Height / 2);

            //Right.
            Canvas.SetLeft(_right, Selected.Right - _right.Width / 2);
            Canvas.SetTop(_right, Selected.Top + Selected.Height / 2 - _right.Height / 2);

            //Bottom.
            Canvas.SetLeft(_bottom, Selected.Left + Selected.Width / 2 - _bottom.Width / 2);
            Canvas.SetTop(_bottom, Selected.Bottom - _bottom.Height / 2);
        }

        private void ShowStatusControls()
        {
            if (_statusControlGrid == null)
                return;

            if (Selected.Width > 100 && Selected.Height > 100)
            {
                //Show inside the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2 - 50);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - 15);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (ActualHeight - (Selected.Top + Selected.Height) > 100)
            {
                //Show at the bottom of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2 - 50);
                Canvas.SetTop(_statusControlGrid, Selected.Bottom + 10);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (Selected.Top > 100)
            {
                //Show on top of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2 - 50);
                Canvas.SetTop(_statusControlGrid, Selected.Top - 40);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (Selected.Left > 100)
            {
                //Show to the left of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Left - 110);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - 15);

                _statusControlGrid.Visibility = Visibility.Visible;
                return;
            }

            if (ActualWidth - (Selected.Left + Selected.Width) > 100)
            {
                //Show to the right of the main rectangle.
                Canvas.SetLeft(_statusControlGrid, Selected.Right + 10);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - 15);

                _statusControlGrid.Visibility = Visibility.Visible;
            }
        }

        private void HideStatusControls()
        {
            if (_statusControlGrid == null)
                return;

            _statusControlGrid.Visibility = Visibility.Collapsed;
        }

        private void Accept()
        {
            if (!FinishedSelection)
                return;

            HideStatusControls();
            RaiseAcceptedEvent();
        }

        public void Retry()
        {
            Selected = new Rect(-1, -1, 0, 0);

            FinishedSelection = false;

            AdjustMode();
            HideStatusControls();
        }

        public void Cancel()
        {
            Selected = new Rect(-1, -1, 0, 0);

            FinishedSelection = false;

            HideStatusControls();
            RaiseCanceledEvent();
        }

        public void RaiseAcceptedEvent()
        {
            if (SelectionAcceptedEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionAcceptedEvent));
        }

        public void RaiseCanceledEvent()
        {
            if (SelectionCanceledEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionCanceledEvent));
        }

        public void AdjustMode()
        {
            if (Mode == ModeType.Window)
                Windows = Native.EnumerateWindows(Scale);
            else if (Mode == ModeType.Fullscreen)
                Windows = Monitor.AllMonitorsScaled(Scale).Select(x => new DetectedRegion(x.Handle, x.Bounds.Offset(-1), x.Name)).ToList();
            else
                Windows.Clear();
        }

        #endregion

        #region Events

        private static void Mode_Changed(DependencyObject o, DependencyPropertyChangedEventArgs d)
        {
            var control = o as SelectControl;

            control?.AdjustMode();
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mode != ModeType.Region)
                return;

            _startPoint = e.GetPosition(this);

            _rectangle.CaptureMouse();

            HideStatusControls();

            e.Handled = true;
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mode != ModeType.Region || !_rectangle.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;

            _rectangle.MouseMove -= Rectangle_MouseMove;

            var currentPosition = e.GetPosition(this);

            var x = Selected.X + (currentPosition.X - _startPoint.X);
            var y = Selected.Y + (currentPosition.Y - _startPoint.Y);

            if (x < -1)
                x = -1;

            if (y < -1)
                y = -1;

            if (x + Selected.Width > ActualWidth + 1)
                x = ActualWidth + 1 - Selected.Width;

            if (y + Selected.Height > ActualHeight + 1)
                y = ActualHeight + 1 - Selected.Height;

            Selected = new Rect(x, y, Selected.Width, Selected.Height);

            _startPoint = currentPosition;
            e.Handled = true;

            AdjustThumbs();

            _rectangle.MouseMove += Rectangle_MouseMove;
        }

        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Mode != ModeType.Region)
                return;

            if (_rectangle.IsMouseCaptured)
                _rectangle?.ReleaseMouseCapture();

            AdjustThumbs();
            ShowStatusControls();

            e.Handled = true;
        }

        ///<summary>
        ///Handler for resizing from the top-left.
        ///</summary>
        private void HandleTopLeft(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
            var left = Selected.Left - (width - Selected.Width);
            var height = Math.Max(Selected.Height - e.VerticalChange, 10);
            var top = Selected.Top - (height - Selected.Height);

            if (top < 0)
            {
                height -= top * -1;
                top = 0;
            }

            if (left < 0)
            {
                width -= left * -1;
                left = 0;
            }

            Selected = new Rect(left, top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        ///  Handler for resizing from the top-right.
        /// </summary>
        private void HandleTopRight(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
            var height = Math.Max(Selected.Height - e.VerticalChange, 10);
            var top = Selected.Top - (height - Selected.Height);

            if (top < 0)
            {
                height -= top * -1;
                top = 0;
            }

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            Selected = new Rect(Selected.Left, top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        ///  Handler for resizing from the bottom-left.
        /// </summary>
        private void HandleBottomLeft(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
            var left = Selected.Left - (width - Selected.Width);
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (left < 0)
            {
                width -= left * -1;
                left = 0;
            }

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(left, Selected.Top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the bottom-right.
        /// </summary>
        private void HandleBottomRight(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(Selected.Left, Selected.Top, width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the left-middle.
        /// </summary>
        private void HandleLeft(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width - e.HorizontalChange, 10);
            var left = Selected.Left - (width - Selected.Width);

            if (left < 0)
            {
                width -= left * -1;
                left = 0;
            }

            Selected = new Rect(left, Selected.Top, width, Selected.Height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the top-middle.
        /// </summary>
        private void HandleTop(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var height = Math.Max(Selected.Height - e.VerticalChange, 10);
            var top = Selected.Top - (height - Selected.Height);

            if (top < 0)
            {
                height -= top * -1;
                top = 0;
            }

            Selected = new Rect(Selected.Left, top, Selected.Width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        ///  Handler for resizing from the right-middle.
        /// </summary>
        private void HandleRight(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            Selected = new Rect(Selected.Left, Selected.Top, width, Selected.Height);

            AdjustThumbs();
            ShowStatusControls();
        }

        /// <summary>
        /// Handler for resizing from the bottom-middle.
        /// </summary>
        private void HandleBottom(object sender, DragDeltaEventArgs e)
        {
            var hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(Selected.Left, Selected.Top, Selected.Width, height);

            AdjustThumbs();
            ShowStatusControls();
        }

        #endregion
    }
}