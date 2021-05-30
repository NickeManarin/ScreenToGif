using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using ScreenToGif.Settings;
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
        private ExtendedUniformGrid _statusControlGrid;

        /// <summary>
        /// The pre-calculated size of the horizontal and vertical versions of the status control grid.
        /// </summary>
        private Size _statusHorizontalSize, _statusVerticalSize;

        /// <summary>
        /// The grids that holds the zoomed image and size info.
        /// </summary>
        private Grid _zoomGrid, _sizeGrid;

        //private readonly RegionMagnifier _regionMagnifier;

        /// <summary>
        /// The zoomed image.
        /// </summary>
        private Image _croppedImage;

        /// <summary>
        /// The textblock that lies at the bottom of the zoom view.
        /// </summary>
        private TextBlock _zoomTextBlock;

        /// <summary>
        /// The main canvas, the root element.
        /// </summary>
        private Canvas _mainCanvas;

        /// <summary>
        /// Status control buttons.
        /// </summary>
        private ExtendedButton _acceptButton, _retryButton, _cancelButton;

        ///// <summary>
        ///// The texblock that shows the size of the selection.
        ///// </summary>
        //private TextBlock _sizeTextBlock, _sizeNativeTextBlock;

        ///// <summary>
        ///// The grid that holds the sizing controls.
        ///// </summary>
        //private Grid _rectGrid;

        ///// <summary>
        ///// The button that closes the sizing widget.
        ///// </summary>
        //private ImageButton _closeRectButton;

        ///// <summary>
        ///// The grid that enables the movement of the sizing widget.
        ///// </summary>
        //private Grid _moveSizeWidgetGrid;

        /// <summary>
        /// The start point for the drag operation.
        /// </summary>
        private Point _startPoint;

        /// <summary>
        /// Blind spots for the ZoomView. If the cursor is on top of any of this spots, the zoom view should not appear.
        /// </summary>
        private readonly List<Rect> _blindSpots = new List<Rect>();

        /// <summary>
        /// The latest window that contains the mouse cursor on top of it.
        /// </summary>
        private DetectedRegion _hitTestWindow;

        /// <summary>
        /// True when this control is ready to process mouse input when using the Screen/Window selection mode.
        /// This was added because the event MouseMove was being fired before the method that adjusts the other controls finished. (TL;DR It was a race condition)
        /// </summary>
        private bool _ready;

        /// <summary>
        /// True if the hover focus was changed to this selector.
        /// Other selectors must lose the hover focus.
        /// This makes the zoom view to be hidden everywhere else.
        /// </summary>
        private bool _wasHoverFocusChanged;
        
        public List<DetectedRegion> Windows = new List<DetectedRegion>();

        public BitmapSource BackImage;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty ParentLeftProperty = DependencyProperty.Register(nameof(ParentLeft), typeof(double), typeof(SelectControl), new PropertyMetadata(0d));
        
        public static readonly DependencyProperty ParentTopProperty = DependencyProperty.Register(nameof(ParentTop), typeof(double), typeof(SelectControl), new PropertyMetadata(0d));

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register(nameof(IsPickingRegion), typeof(bool), typeof(SelectControl), new PropertyMetadata(true));

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(Rect), typeof(SelectControl), new PropertyMetadata(Rect.Empty, Selected_PropertyChanged));

        public static readonly DependencyProperty NonExpandedSelectionProperty = DependencyProperty.Register(nameof(NonExpandedSelection), typeof(Rect), typeof(SelectControl), new PropertyMetadata(Rect.Empty));
        
        public static readonly DependencyProperty NonExpandedNativeSelectionProperty = DependencyProperty.Register(nameof(NonExpandedNativeSelection), typeof(Rect), typeof(SelectControl), new PropertyMetadata(Rect.Empty));

        public static readonly DependencyProperty FinishedSelectionProperty = DependencyProperty.Register(nameof(FinishedSelection), typeof(bool), typeof(SelectControl), new PropertyMetadata(false));

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(ModeType), typeof(SelectControl), new PropertyMetadata(ModeType.Region));

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(SelectControl), new PropertyMetadata(1d));

        public static readonly DependencyProperty EmbeddedModeProperty = DependencyProperty.Register(nameof(EmbeddedMode), typeof(bool), typeof(SelectControl), new PropertyMetadata(false));
        
        public static readonly DependencyProperty AnimateBorderProperty = DependencyProperty.Register(nameof(AnimateBorder), typeof(bool), typeof(SelectControl), new PropertyMetadata(false));


        public static readonly RoutedEvent MouseHoveringEvent = EventManager.RegisterRoutedEvent(nameof(MouseHovering), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        public static readonly RoutedEvent SelectionAcceptedEvent = EventManager.RegisterRoutedEvent(nameof(SelectionAccepted), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        public static readonly RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        public static readonly RoutedEvent SelectionCanceledEvent = EventManager.RegisterRoutedEvent(nameof(SelectionCanceled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SelectControl));

        #endregion

        #region Properties

        public double ParentLeft
        {
            get => (double)GetValue(ParentLeftProperty);
            set => SetValue(ParentLeftProperty, value);
        }

        public double ParentTop
        {
            get => (double)GetValue(ParentTopProperty);
            set => SetValue(ParentTopProperty, value);
        }

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

        public Rect NonExpandedSelection
        {
            get => (Rect)GetValue(NonExpandedSelectionProperty);
            set => SetValue(NonExpandedSelectionProperty, value);
        }

        public Rect NonExpandedNativeSelection
        {
            get => (Rect)GetValue(NonExpandedNativeSelectionProperty);
            set => SetValue(NonExpandedNativeSelectionProperty, value);
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

        public bool EmbeddedMode
        {
            get => (bool)GetValue(EmbeddedModeProperty);
            set => SetValue(EmbeddedModeProperty, value);
        }

        public bool AnimateBorder
        {
            get => (bool)GetValue(AnimateBorderProperty);
            set => SetValue(AnimateBorderProperty, value);
        }


        public event RoutedEventHandler MouseHovering
        {
            add => AddHandler(MouseHoveringEvent, value);
            remove => RemoveHandler(MouseHoveringEvent, value);
        }

        public event RoutedEventHandler SelectionAccepted
        {
            add => AddHandler(SelectionAcceptedEvent, value);
            remove => RemoveHandler(SelectionAcceptedEvent, value);
        }

        public event RoutedEventHandler SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
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
            _statusControlGrid = Template.FindName("StatusControlGrid", this) as ExtendedUniformGrid;
            _acceptButton = Template.FindName("AcceptButton", this) as ExtendedButton;
            _retryButton = Template.FindName("RetryButton", this) as ExtendedButton;
            _cancelButton = Template.FindName("CancelButton", this) as ExtendedButton;

            _zoomGrid = Template.FindName("ZoomGrid", this) as Grid;
            _croppedImage = Template.FindName("CroppedImage", this) as Image;
            _zoomTextBlock = Template.FindName("ZoomTextBlock", this) as TextBlock;
            _sizeGrid = Template.FindName("SizeGrid", this) as Grid;
            //_sizeTextBlock = Template.FindName("SizeTextBlock", this) as TextBlock;
            //_sizeNativeTextBlock = Template.FindName("NativeSizeTextBlock", this) as TextBlock;
            
            //_rectGrid = Template.FindName("RectGrid", this) as Grid;
            //_closeRectButton = Template.FindName("CloseSizeWidgetButton", this) as ImageButton;
            //_moveSizeWidgetGrid = Template.FindName("MoveSizeWidgetGrid", this) as Grid;

            Loaded += Control_Loaded;
            Unloaded += Control_Unloaded;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

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

            _acceptButton.Click += (sender, e) => { Accept(); };
            _retryButton.Click += (sender, e) => { Retry(); };
            _cancelButton.Click += (sender, e) => { Cancel(); };

            //Replace with singleton property.
            //if (_regionMagnifier == null)
            //_regionMagnifier = new RegionMagnifier();

            //Enable sizing controls.
            //if (!EmbeddedMode)
            //{
            //    _sizeTextBlock.PreviewMouseLeftButtonDown += SizeTextBlock_MouseUp;
            //    _sizeTextBlock.IsHitTestVisible = true;
            //    _sizeTextBlock.Cursor = Cursors.Hand;
            //}
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);

            if (Mode == ModeType.Region)
            {
                Selected = new Rect(e.GetPosition(this), new Size(0, 0));
                FinishedSelection = false;

                CaptureMouse();

                AdjustStatusControls();
                AdjustFlowControls();
                DetectBlindSpots();
            }
            else
            {
                if (Selected.Width > 0 && Selected.Height > 0)
                {
                    if (Mode == ModeType.Window && _hitTestWindow != null)
                        Util.Native.SetForegroundWindow(_hitTestWindow.Handle);

                    Selected = Selected.Offset(-1);
                    RaiseAcceptedEvent();
                }
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
                var current = e.GetPosition(this);

                AdjustZoomView(current);

                if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
                    return;

                //Move 1 pixel to corrent the position of the selection to the cursor.
                current.X++;
                current.Y++;

                if (current.X < -1)
                    current.X = -1;

                if (current.Y < -1)
                    current.Y = -1;

                if (current.X > ActualWidth)
                    current.X = ActualWidth;

                if (current.Y > ActualHeight)
                    current.Y = ActualHeight;

                Selected = new Rect(Math.Min(current.X, _startPoint.X), Math.Min(current.Y, _startPoint.Y), Math.Abs(current.X - _startPoint.X), Math.Abs(current.Y - _startPoint.Y));

                AdjustInfo(current);
            }
            else if (_ready)
            {
                var current = e.GetPosition(this);

                _hitTestWindow = Windows.FirstOrDefault(x => x.Bounds.Contains(current));
                Selected = _hitTestWindow?.Bounds ?? Rect.Empty;

                AdjustInfo(current);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (Mode == ModeType.Region)
            {
                ReleaseMouseCapture();

                if (Selected.Width < 30 || Selected.Height < 30)
                {
                    OnMouseRightButtonDown(e);
                    return;
                }

                FinishedSelection = true;

                AdjustThumbs();
                AdjustStatusControls(e.GetPosition(this));
                AdjustFlowControls();
                DetectBlindSpots();
            }

            //e.Handled = true;
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            //Apparently, this event is not triggered.
            if (e.Key == Key.Escape)
                Cancel();

            if (e.Key == Key.Enter || e.Key == Key.Return)
                Accept();

            e.Handled = true;
            base.OnPreviewKeyDown(e);

            if (Mode != ModeType.Region || Selected.IsEmpty)
                return;

            var step = (Keyboard.Modifiers & ModifierKeys.Alt) != 0 ? 5 : 1;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            //Control + Shift: Expand both ways.
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                switch (key)
                {
                    case Key.Up:
                        HandleBottom(_bottom, new DragDeltaEventArgs(0, step));
                        HandleTop(_top, new DragDeltaEventArgs(0, -step));
                        break;
                    case Key.Down:
                        HandleBottom(_bottom, new DragDeltaEventArgs(0, -step));
                        HandleTop(_top, new DragDeltaEventArgs(0, step));
                        break;
                    case Key.Left:
                        HandleRight(_right, new DragDeltaEventArgs(-step, 0));
                        HandleLeft(_left, new DragDeltaEventArgs(step, 0));
                        break;
                    case Key.Right:
                        HandleRight(_right, new DragDeltaEventArgs(step, 0));
                        HandleLeft(_left, new DragDeltaEventArgs(-step, 0));
                        break;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) //If the Shift key is pressed, the sizing mode is enabled (bottom right).
            {
                switch (key)
                {
                    case Key.Up:
                        HandleBottom(_bottom, new DragDeltaEventArgs(0, -step));
                        break;
                    case Key.Down:
                        HandleBottom(_bottom, new DragDeltaEventArgs(0, step));
                        break;
                    case Key.Left:
                        HandleRight(_right, new DragDeltaEventArgs(-step, 0));
                        break;
                    case Key.Right:
                        HandleRight(_right, new DragDeltaEventArgs(step, 0));
                        break;
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) //If the Control key is pressed, the sizing mode is enabled (top left).
            {
                switch (key)
                {
                    case Key.Up:
                        HandleTop(_top, new DragDeltaEventArgs(0, -step));
                        break;
                    case Key.Down:
                        HandleTop(_top, new DragDeltaEventArgs(0, step));
                        break;
                    case Key.Left:
                        HandleLeft(_left, new DragDeltaEventArgs(-step, 0));
                        break;
                    case Key.Right:
                        HandleLeft(_left, new DragDeltaEventArgs(step, 0));
                        break;
                }
            }
            else
            {
                switch (key) //If no other key is pressed, the movement mode is enabled.
                {
                    case Key.Up:
                        HandleCenter(new DragDeltaEventArgs(0, -step));
                        break;
                    case Key.Down:
                        HandleCenter(new DragDeltaEventArgs(0, step));
                        break;
                    case Key.Left:
                        HandleCenter(new DragDeltaEventArgs(-step, 0));
                        break;
                    case Key.Right:
                        HandleCenter(new DragDeltaEventArgs(step, 0));
                        break;
                }
            }
        }

        #endregion

        #region Methods

        private void AdjustSelection()
        {
            //If already opened with a region selected, treat as "already selected".
            if (Selected == Rect.Empty)
                return;

            FinishedSelection = true;

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            AdjustFlowControls();
            DetectBlindSpots();
            AdjustInfo(point);
        }

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

        private void AdjustZoomView(Point point)
        {
            //_bottom.IsVisible
            if (BackImage == null || Mode != ModeType.Region || !UserSettings.All.Magnifier || (Selected.Width > 10 && Selected.Height > 10 && Selected.Offset(5).Contains(point)) || _blindSpots.Any(x => x.Contains(point)))
            {
                _zoomGrid.Visibility = Visibility.Hidden;
                return;
            }

            //If this selector got the hover, the other selectors must hide their zoom views.
            if (!_wasHoverFocusChanged)
            {
                _wasHoverFocusChanged = true;
                RaiseMouseHoveringEvent();
            }

            var scaledPoint = point.Scale(Scale);
            var scaledSize = (int)Math.Round(15 * Scale, MidpointRounding.AwayFromZero);

            try
            {
                //When using multiple monitors, the mouse cursor can paqss to another screen. This makes sure that to only get a valid screen position.
                if (scaledPoint.X < 0 || scaledPoint.Y < 0 || scaledPoint.X + scaledSize > BackImage.PixelWidth || scaledPoint.Y + scaledSize > BackImage.PixelHeight)
                {
                    _zoomGrid.Visibility = Visibility.Hidden;
                    return;
                }

                //The image is already 7 pixels offset of the current position.
                _croppedImage.Source = new CroppedBitmap(BackImage, new Int32Rect((int)scaledPoint.X, (int)scaledPoint.Y, scaledSize, scaledSize));
            }
            catch (Exception)
            { }

            var left = point.X + 20;
            var top = point.Y - _zoomGrid.ActualHeight - 20;

            //Right overflow, adjust to the left.
            if (ActualWidth - point.X < _zoomGrid.ActualWidth + 20)
                left = point.X - _zoomGrid.ActualWidth - 20;

            //Top overflow, adjust to the bottom.
            if (point.Y - _zoomGrid.ActualHeight - 20 < 0)
                top = point.Y + 20;

            Canvas.SetLeft(_zoomGrid, left);
            Canvas.SetTop(_zoomGrid, top);

            _zoomTextBlock.Text = $"X: {Math.Round(point.X + ParentLeft, 2)} ◇ Y: {Math.Round(point.Y + ParentTop, 2)}";
            _zoomGrid.Visibility = Visibility.Visible;
        }

        private void AdjustZoomViewDetached(Point point)
        {
            //If it should not display the zoom view.
            if (BackImage == null || Mode != ModeType.Region || !UserSettings.All.Magnifier || (Selected.Width > 10 && Selected.Height > 10 && Selected.Offset(5).Contains(point)) || _blindSpots.Any(x => x.Contains(point)))
            {
                //_regionMagnifier.Hide();
                return;
            }

            //If this selector got the hover, the other selectors must hide their zoom views.
            if (!_wasHoverFocusChanged)
            {
                _wasHoverFocusChanged = true;
                RaiseMouseHoveringEvent();
            }

            #region Get the zoommed-in part of the image

            //var scaledPoint = point.Scale(Scale);
            //var scaledSize = (int)Math.Round(15 * Scale, MidpointRounding.AwayFromZero);

            try
            {
                //The image is already 7 pixels offset of the current position.
                //_regionMagnifier.Image = new CroppedBitmap(BackImage, new Int32Rect((int)scaledPoint.X, (int)scaledPoint.Y, scaledSize, scaledSize));
            }
            catch (Exception)
            { }

            #endregion

            //if (!_regionMagnifier.IsVisible)
            //    _regionMagnifier.Show();

            #region Reposition the zoom view

            //var left = point.X + 20;
            //var top = point.Y - _regionMagnifier.ActualHeight - 20;

            ////Right overflow, adjust to the left.
            //if (ActualWidth - point.X < _regionMagnifier.ActualWidth + 20)
            //    left = point.X - _regionMagnifier.ActualWidth - 20;

            ////Top overflow, adjust to the bottom.
            //if (point.Y - _regionMagnifier.ActualHeight - 20 < 0)
            //    top = point.Y + 20;

            //_regionMagnifier.Left = left + ParentLeft;
            //_regionMagnifier.Top = top + ParentTop;
            //_regionMagnifier.LeftPosition = point.X + ParentLeft;
            //_regionMagnifier.TopPosition = point.Y + ParentTop;

            #endregion
        }

        private void AdjustStatusControls(Point? point = null)
        {
            if (_statusControlGrid == null)
                return;

            if (!FinishedSelection || EmbeddedMode)
            {
                _statusControlGrid.Visibility = Visibility.Hidden;
                return;
            }

            //Show the controls always closest to the given point, if there's no space on the current monitor, 
            //try finding the second closest point, or else show inside the selection rectangle.

            if (!point.HasValue)
                return;

            //var absolutePoint = new Point(point.Value.X, point.Value.Y);

            //If there's no space at the sides, show inside the rectangle.
            if (Selected.Width > ActualWidth - _statusVerticalSize.Width * 2 && Selected.Height > ActualHeight - _statusHorizontalSize.Height * 2)
            {
                _statusControlGrid.Rows = 1;
                _statusControlGrid.Columns = 3;
                _statusControlGrid.IsReversed = false;
                _statusControlGrid.UpdateLayout();

                Canvas.SetLeft(_statusControlGrid, Selected.Left + Selected.Width / 2 - _statusControlGrid.ActualWidth / 2);
                Canvas.SetTop(_statusControlGrid, Selected.Top + Selected.Height / 2 - _statusControlGrid.ActualHeight / 2);
            }
            else
            {
                //Out of 4 Points, get the one that is closest to the current mouse position.
                var distances = new[] { (Selected.TopLeft - point.Value).Length, (Selected.TopRight - point.Value).Length, (Selected.BottomLeft - point.Value).Length, (Selected.BottomRight - point.Value).Length };
                var index = Array.IndexOf(distances, distances.Min());

                const int margin = 10;

                var canTopLeft = Selected.Top > _statusHorizontalSize.Height + margin || Selected.Left > _statusVerticalSize.Width + margin;
                var canBottomLeft = ActualHeight - Selected.Bottom > _statusHorizontalSize.Height + margin || Selected.Left > _statusVerticalSize.Width + margin;

                switch (index)
                {
                    case 0: //Top Left.
                        if (Selected.Top > _statusHorizontalSize.Height + margin)
                        {
                            //On top.
                            _statusControlGrid.Rows = 1;
                            _statusControlGrid.Columns = 3;
                            _statusControlGrid.IsReversed = false;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Left);
                            Canvas.SetTop(_statusControlGrid, Selected.Top - _statusControlGrid.ActualHeight - margin);
                            break;
                        }
                        else if (Selected.Left > _statusVerticalSize.Width + margin)
                        {
                            //To the left.
                            _statusControlGrid.Rows = 3;
                            _statusControlGrid.Columns = 1;
                            _statusControlGrid.IsReversed = false;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Left - _statusControlGrid.ActualWidth - margin);
                            Canvas.SetTop(_statusControlGrid, Selected.Top);
                            break;
                        }

                        if (Selected.Width > Selected.Height && canBottomLeft)
                            goto case 2; //Bottom left.
                        else
                            goto case 1; //Top right.

                    case 1: //Top Right.
                        if (Selected.Top > _statusHorizontalSize.Height + margin)
                        {
                            //On top.
                            _statusControlGrid.Rows = 1;
                            _statusControlGrid.Columns = 3;
                            _statusControlGrid.IsReversed = true;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Right - _statusControlGrid.ActualWidth);
                            Canvas.SetTop(_statusControlGrid, Selected.Top - _statusControlGrid.ActualHeight - margin);
                            break;
                        }
                        else if (ActualWidth - Selected.Right > _statusVerticalSize.Width + margin)
                        {
                            //To the right.
                            _statusControlGrid.Rows = 3;
                            _statusControlGrid.Columns = 1;
                            _statusControlGrid.IsReversed = false;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Right + margin);
                            Canvas.SetTop(_statusControlGrid, Selected.Top);
                            break;
                        }

                        if (Selected.Width > Selected.Height && !canTopLeft)
                            goto case 3; //Bottom right.
                        else
                            goto case 0; //Top left.

                    case 2: //Bottom Left.
                        if (ActualHeight - Selected.Bottom > _statusHorizontalSize.Height + margin)
                        {
                            //On the bottom.
                            _statusControlGrid.Rows = 1;
                            _statusControlGrid.Columns = 3;
                            _statusControlGrid.IsReversed = false;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Left.Clamp(0, ActualWidth - _statusControlGrid.ActualWidth));
                            Canvas.SetTop(_statusControlGrid, Selected.Bottom + margin);
                            break;
                        }
                        else if (Selected.Left > _statusVerticalSize.Width + margin)
                        {
                            //To the left.
                            _statusControlGrid.Rows = 3;
                            _statusControlGrid.Columns = 1;
                            _statusControlGrid.IsReversed = true;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Left - _statusControlGrid.ActualWidth - margin);
                            Canvas.SetTop(_statusControlGrid, Selected.Bottom - _statusControlGrid.ActualHeight);
                            break;
                        }

                        if (Selected.Width > Selected.Height && canTopLeft)
                            goto case 0; //Top left.
                        else
                            goto case 3; //Bottom right.

                    case 3: //Bottom Right.
                        if (ActualHeight - Selected.Bottom > _statusHorizontalSize.Height + margin)
                        {
                            //On the bottom.
                            _statusControlGrid.Rows = 1;
                            _statusControlGrid.Columns = 3;
                            _statusControlGrid.IsReversed = true;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, (Selected.Right - _statusControlGrid.ActualWidth).Clamp(0, ActualWidth - _statusControlGrid.ActualWidth));
                            Canvas.SetTop(_statusControlGrid, Selected.Bottom + margin);
                            break;
                        }
                        else if (ActualWidth - Selected.Right > _statusVerticalSize.Width + margin)
                        {
                            //To the right.
                            _statusControlGrid.Rows = 3;
                            _statusControlGrid.Columns = 1;
                            _statusControlGrid.IsReversed = true;
                            _statusControlGrid.UpdateLayout();

                            Canvas.SetLeft(_statusControlGrid, Selected.Right + margin);
                            Canvas.SetTop(_statusControlGrid, Selected.Bottom - _statusControlGrid.ActualHeight);
                            break;
                        }

                        if (Selected.Width > Selected.Height && !canBottomLeft)
                            goto case 1; //Top right.
                        else
                            goto case 2; //Bottom left.
                }
            }

            _statusControlGrid.Visibility = Visibility.Visible;
        }

        private void AdjustFlowControls()
        {
            if (_mainCanvas == null)
                return;

            foreach (var button in _mainCanvas.Children.OfType<ExtendedButton>())
                button.Visibility = FinishedSelection ? Visibility.Hidden : Visibility.Visible;
        }

        private void AdjustInfo(Point? point = null)
        {
            if (_sizeGrid == null)
                return;

            if (point == null || Selected.IsEmpty || Selected.Width < _sizeGrid.ActualWidth || Selected.Height < _sizeGrid.ActualHeight)
            {
                _sizeGrid.Visibility = Visibility.Hidden;
                return;
            }

            //Out of 4 Points, get the one that is farthest from the current mouse position.
            var distances = new[] { (Selected.TopLeft - point.Value).Length, (Selected.TopRight - point.Value).Length, (Selected.BottomLeft - point.Value).Length, (Selected.BottomRight - point.Value).Length };
            var index = Array.IndexOf(distances, distances.Max());

            switch (index)
            {
                case 0:
                    Canvas.SetTop(_sizeGrid, Selected.Top);
                    Canvas.SetLeft(_sizeGrid, Selected.Left);
                    break;
                case 1:
                    Canvas.SetTop(_sizeGrid, Selected.Top);
                    Canvas.SetLeft(_sizeGrid, Selected.Right - _sizeGrid.ActualWidth);
                    break;
                case 2:
                    Canvas.SetTop(_sizeGrid, Selected.Bottom - _sizeGrid.ActualHeight);
                    Canvas.SetLeft(_sizeGrid, Selected.Left);
                    break;
                case 3:
                    Canvas.SetTop(_sizeGrid, Selected.Bottom - _sizeGrid.ActualHeight);
                    Canvas.SetLeft(_sizeGrid, Selected.Right - _sizeGrid.ActualWidth);
                    break;
            }

            _sizeGrid.Visibility = Visibility.Visible;
        }

        private void DetectBlindSpots()
        {
            _blindSpots.Clear();

            if (Mode != ModeType.Region || !UserSettings.All.Magnifier)
                return;

            //If nothing selected, only the Close button will appear.
            if (Selected.IsEmpty) // || !FinishedSelection)
            {
                _blindSpots.Add(new Rect(new Point(ActualWidth - 40, 0), new Size(40, 40)));
                return;
            }

            if (_statusControlGrid.Visibility == Visibility.Visible)
                _blindSpots.Add(new Rect(new Point(Canvas.GetLeft(_statusControlGrid), Canvas.GetTop(_statusControlGrid)), new Size(_statusControlGrid.ActualWidth, _statusControlGrid.ActualHeight)));
        }


        internal void Accept()
        {
            if (!FinishedSelection)
                return;

            //Selected = Selected.Offset(-1);
            RaiseAcceptedEvent();
        }

        public void Retry()
        {
            Selected = Rect.Empty;

            FinishedSelection = false;

            AdjustStatusControls();
            AdjustFlowControls();
            DetectBlindSpots();
            AdjustInfo();
        }

        public void Cancel()
        {
            Selected = Rect.Empty;

            FinishedSelection = false;

            AdjustStatusControls();
            DetectBlindSpots();
            RaiseCanceledEvent();
        }

        public void HideZoom()
        {
            _wasHoverFocusChanged = false;
            _zoomGrid.Visibility = Visibility.Hidden;
            //_regionMagnifier.Hide();
        }


        public void RaiseMouseHoveringEvent()
        {
            if (MouseHoveringEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(MouseHoveringEvent));
        }

        public void RaiseAcceptedEvent()
        {
            if (SelectionAcceptedEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionAcceptedEvent));
        }

        public void RaiseChangedEvent()
        {
            if (SelectionChangedEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
        }

        public void RaiseCanceledEvent()
        {
            if (SelectionCanceledEvent == null || !IsLoaded)
                return;

            RaiseEvent(new RoutedEventArgs(SelectionCanceledEvent));
        }

        private void CalculateStatusGridSizes()
        {
            _statusControlGrid.Rows = 3;
            _statusControlGrid.Columns = 1;
            _statusControlGrid.UpdateLayout();

            _statusVerticalSize = new Size(_statusControlGrid.ActualWidth, _statusControlGrid.ActualHeight);

            _statusControlGrid.Rows = 1;
            _statusControlGrid.Columns = 3;
            _statusControlGrid.UpdateLayout();

            _statusHorizontalSize = new Size(_statusControlGrid.ActualWidth, _statusControlGrid.ActualHeight);
        }

        #endregion

        #region Events

        public void Control_Loaded(object o, RoutedEventArgs routedEventArgs)
        {
            _ready = false;

            Keyboard.Focus(this);

            _blindSpots.Clear();

            if (EmbeddedMode)
            {
                var viewBox = new Viewbox
                {
                    Height = Height,
                    Width = Width,
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.Both,
                    Tag = "T",
                    ClipToBounds = true,
                    IsHitTestVisible = false,
                    Child = new TextPath
                    {
                        IsHitTestVisible = false,
                        Text = LocalizationHelper.Get("S.Recorder.SelectArea.Embedded"),
                        Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                        Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        StrokeThickness = 1.6,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 80,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(80),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        ClipToBounds = true
                    }
                };

                _mainCanvas.Children.Insert(0, viewBox);

                Canvas.SetLeft(viewBox, 0);
                Canvas.SetTop(viewBox, 0);
                Panel.SetZIndex(viewBox, 0);

                AdjustSelection();
                return;
            }

            if (IsMouseOver)
                AdjustZoomView(Mouse.GetPosition(this));

            CalculateStatusGridSizes();

            #region Close button

            //Close button.
            var button = new ExtendedButton
            {
                Name = "CancelButton",
                Width = 40,
                Height = 40,
                ContentHeight = 25,
                ContentWidth = 25,
                ToolTip = LocalizationHelper.Get("S.Recorder.CancelSelection"),
                Icon = TryFindResource("Vector.Cancel") as Brush,
                Style = TryFindResource("Style.Button.NoText.White") as Style,
                Cursor = Cursors.Arrow,
                Tag = "T"
            };

            button.Click += (sender, e) => { Cancel(); };

            _mainCanvas.Children.Add(button);

            Canvas.SetLeft(button, ActualWidth - 40);
            Canvas.SetTop(button, 0);
            Panel.SetZIndex(button, 8);

            _blindSpots.Add(new Rect(new Point(ActualWidth - 40, 0), new Size(40, 40)));

            #endregion

            if (Mode == ModeType.Fullscreen)
            {
                var viewBox = new Viewbox
                {
                    Height = ActualHeight,
                    Width = ActualWidth,
                    Stretch = Stretch.Uniform,
                    Tag = "T",
                    IsHitTestVisible = false,
                    Child = new TextPath
                    {
                        IsHitTestVisible = false,
                        Text = "👆 " + LocalizationHelper.Get("S.Recorder.SelectScreen"),
                        Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                        Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        StrokeThickness = 3,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 72,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(50)
                    }
                };

                _mainCanvas.Children.Insert(0, viewBox);

                Canvas.SetLeft(viewBox, 0);
                Canvas.SetTop(viewBox, 0);
                Panel.SetZIndex(viewBox, 0);
            }
            else if (Mode == ModeType.Window)
            {
                foreach (var window in Windows)
                {
                    var border = new Border
                    {
                        Tag = "T",
                        ClipToBounds = true,
                        IsHitTestVisible = false,
                        Height = window.Bounds.Height,
                        Width = window.Bounds.Width,
                        Child = new Viewbox
                        {
                            Stretch = Stretch.Uniform,
                            StretchDirection = StretchDirection.Both,
                            VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextPath
                            {
                                IsHitTestVisible = false,
                                Text = window.Bounds.Width < 400 || window.Bounds.Height < 100 ? "👆" : "👆 " + LocalizationHelper.Get("S.Recorder.SelectWindow"),
                                Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                                Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                                StrokeThickness = 3,
                                FontFamily = new FontFamily("Segoe UI"),
                                FontSize = 80,
                                FontWeight = FontWeights.SemiBold,
                                Margin = new Thickness(20),
                                VerticalAlignment = VerticalAlignment.Stretch,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                            }
                        }
                    };

                    border.UpdateLayout();

                    var top = Windows.Where(x => x.Order < window.Order).Select(x => x.Bounds).ToList();
                    var geo = new RectangleGeometry { Rect = new Rect(new Size(window.Bounds.Width, window.Bounds.Height)) }.GetFlattenedPathGeometry(0, ToleranceType.Absolute);

                    if (top.Any())
                    {
                        foreach (var region in top)
                        {
                            geo = Geometry.Combine(geo, new RectangleGeometry { Rect = new Rect(new Point(region.X - window.Bounds.X, region.Y - window.Bounds.Y), new Size(region.Width, region.Height)) },
                                GeometryCombineMode.Exclude, Transform.Identity);
                        }

                        border.Clip = geo;
                    }

                    _mainCanvas.Children.Insert(0, border);

                    Canvas.SetLeft(border, window.Bounds.Left);
                    Canvas.SetTop(border, window.Bounds.Top);
                    Panel.SetZIndex(border, 0);
                }
            }
            else
            {
                var viewBox = new Viewbox
                {
                    Height = ActualHeight,
                    Width = ActualWidth,
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.Both,
                    Tag = "T",
                    ClipToBounds = true,
                    IsHitTestVisible = false,
                    Child = new TextPath
                    {
                        IsHitTestVisible = false,
                        Text = LocalizationHelper.Get("S.Recorder.SelectArea"),
                        Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                        Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        StrokeThickness = 3,
                        FontFamily = new FontFamily("Segoe UI"),
                        FontSize = 80,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(80),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        ClipToBounds = true
                    }
                };

                _mainCanvas.Children.Insert(0, viewBox);

                Canvas.SetLeft(viewBox, 0);
                Canvas.SetTop(viewBox, 0);
                Panel.SetZIndex(viewBox, 0);
            }

            AdjustSelection();

            _ready = true;

            //Triggers the mouse event to detect the mouse hit at start.
            OnMouseMove(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
        }

        private void SystemEvents_DisplaySettingsChanged(object o, EventArgs eventArgs)
        {
            Scale = this.Scale();
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            if (_mainCanvas == null)
                return;

            var list = _mainCanvas.Children.OfType<FrameworkElement>().Where(x => x.Tag as string == "T").ToList();

            foreach (var element in list)
                _mainCanvas.Children.Remove(element);

            //_regionMagnifier.Close();
        }


        private static void Selected_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (!(o is SelectControl control))
                return;

            //If nothing selected, simply ignore.
            if (control.Selected.IsEmpty)
            {
                control.NonExpandedSelection = control.Selected;
                control.NonExpandedNativeSelection = control.Selected;
                return;
            }

            //In a predetermined selection mode (window or screen) 
            if (control.Mode == ModeType.Fullscreen || control.Mode == ModeType.Window)
            {
                control.NonExpandedSelection = control.Selected.Offset(0); //In this case Offset is just rounding the selection points.
                control.NonExpandedNativeSelection = control.Selected.Scale(control.Scale);
                control.RaiseChangedEvent();
                return;
            }

            #region Region selection mode

            //For way too small regions, avoid applying the offset. That would throw an exception.
            if (control.Selected.Width < 5 || control.Selected.Height < 5)
            {
                control.NonExpandedSelection = control.Selected;
                control.NonExpandedNativeSelection = control.Selected;
                return;
            }

            control.NonExpandedSelection = control.Selected.Offset(1);
            control.NonExpandedNativeSelection = control.Selected.Scale(control.Scale).Offset(Other.RoundUpValue(control.Scale));
            control.RaiseChangedEvent();

            #endregion
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mode != ModeType.Region)
                return;

            _startPoint = e.GetPosition(this);

            _rectangle.CaptureMouse();

            FinishedSelection = false;

            AdjustStatusControls();
            DetectBlindSpots();
            AdjustInfo();

            RaiseChangedEvent(); //Check if makes sense.

            e.Handled = true;
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mode != ModeType.Region || !_rectangle.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) 
                return;

            //A quick double click will fire this event, when it should fire the OnMouseLeftButtonUp.
            if (Selected.IsEmpty || Selected.Width < 10 || Selected.Height < 10)
                return;

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

            AdjustInfo();

            _rectangle.MouseMove += Rectangle_MouseMove;
            _zoomGrid.Visibility = Visibility.Collapsed;
        }

        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Mode != ModeType.Region)
                return;

            if (_rectangle.IsMouseCaptured)
                _rectangle?.ReleaseMouseCapture();

            //A quick double quick will fire this event, whe it should fire the OnMouseLeftButtonUp.
            if (Selected.IsEmpty || Selected.Width < 10 || Selected.Height < 10)
                return;

            FinishedSelection = true;

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
            AdjustZoomView(point);

            e.Handled = true;
        }

        private void SizeTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //Open dialog asking for left/top/width/height.
            //_rectGrid.Visibility = Visibility.Visible;
        }


        ///<summary>
        ///Handler for resizing from the top-left.
        ///</summary>
        private void HandleTopLeft(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

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

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        ///  Handler for resizing from the top-right.
        /// </summary>
        private void HandleTopRight(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

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

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        ///  Handler for resizing from the bottom-left.
        /// </summary>
        private void HandleBottomLeft(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

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

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        /// Handler for resizing from the bottom-right.
        /// </summary>
        private void HandleBottomRight(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(Selected.Left, Selected.Top, width, height);

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        /// Handler for resizing from the left-middle.
        /// </summary>
        private void HandleLeft(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

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

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        /// Handler for resizing from the top-middle.
        /// </summary>
        private void HandleTop(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

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

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        ///  Handler for resizing from the right-middle.
        /// </summary>
        private void HandleRight(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var width = Math.Max(Selected.Width + e.HorizontalChange, 10);

            if (Selected.Left + width > ActualWidth)
                width = ActualWidth - Selected.Left;

            Selected = new Rect(Selected.Left, Selected.Top, width, Selected.Height);

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            DetectBlindSpots();
            AdjustStatusControls(point);
            AdjustInfo(point);
        }

        /// <summary>
        /// Handler for resizing from the bottom-middle.
        /// </summary>
        private void HandleBottom(object sender, DragDeltaEventArgs e)
        {
            if (!(sender is Thumb)) return;

            e.Handled = true;

            //Change the size by the amount the user drags the cursor.
            var height = Math.Max(Selected.Height + e.VerticalChange, 10);

            if (Selected.Top + height > ActualHeight)
                height = ActualHeight - Selected.Top;

            Selected = new Rect(Selected.Left, Selected.Top, Selected.Width, height);

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        /// <summary>
        /// Handler for moving the selection.
        /// </summary>
        private void HandleCenter(DragDeltaEventArgs e)
        {
            e.Handled = true;

            var sel = new Rect(Selected.Left + e.HorizontalChange, Selected.Top + e.VerticalChange, Selected.Width, Selected.Height);

            #region Limit the drag to inside the bounds

            if (sel.Left < 0)
                sel.X = 0;

            if (sel.Top < 0)
                sel.Y = 0;

            if (sel.Right > ActualWidth)
                sel.X = ActualWidth - sel.Width;

            if (sel.Bottom > ActualHeight)
                sel.Y = ActualHeight - sel.Height;

            #endregion

            Selected = new Rect(sel.Left, sel.Top, sel.Width, sel.Height);

            var point = Mouse.GetPosition(this);

            AdjustThumbs();
            AdjustStatusControls(point);
            DetectBlindSpots();
            AdjustInfo(point);
        }

        #endregion
    }
}