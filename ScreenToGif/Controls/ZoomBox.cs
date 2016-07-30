using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// A zoomable control.
    /// http://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer
    /// http://www.codeproject.com/Articles/85603/A-WPF-custom-control-for-zooming-and-panning
    /// </summary>
    [TemplatePart(Name = "ScrollViewer", Type = typeof(ScrollViewer))]
    public class ZoomBox : Control
    {
        #region Variables

        private Point? _lastCenterPositionOnTarget;
        private Point? _lastMousePositionOnTarget;
        private Point? _lastDragPoint;

        private ScrollViewer _scrollViewer;
        private ScaleTransform _scaleTransform;
        private Grid _grid;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(string), typeof(ZoomBox), 
            new FrameworkPropertyMetadata());

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(double), typeof(ZoomBox), 
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, ZoomPropertyChangedCallback));

        #endregion

        #region Properties

        /// <summary>
        /// The image source.
        /// </summary>
        [Description("The image source.")]
        public string ImageSource
        {
            get { return (string)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        /// <summary>
        /// The zoom level of the control.
        /// </summary>
        [Description("The zoom level of the control.")]
        public double Zoom
        {
            get { return (double)GetValue(ZoomProperty); }
            set
            {
                SetCurrentValue(ZoomProperty, value);

                //Should I control the max-min here?
                if (value < 0.1)
                    Zoom = 0.1;
                if (value > 5.0)
                    Zoom = 5;

                if (_scaleTransform != null)
                {
                    _scaleTransform.ScaleX = Zoom;
                    _scaleTransform.ScaleY = Zoom;
                }

                ZoomChanged?.Invoke(this, new EventArgs());
            }
        }

        #endregion

        #region Events

        public event EventHandler ZoomChanged;
        public static event EventHandler InternalZoomChanged;

        #endregion

        static ZoomBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZoomBox), new FrameworkPropertyMetadata(typeof(ZoomBox)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            _scaleTransform = GetTemplateChild("ScaleTransform") as ScaleTransform;
            _grid = GetTemplateChild("Grid") as Grid;

            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
                _scrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
                _scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
                _scrollViewer.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;
                _scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

                _scrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
                _scrollViewer.MouseMove += OnMouseMove;
            }

            InternalZoomChanged += (sender, args) =>
            {
                _scaleTransform.ScaleX = Zoom;
                _scaleTransform.ScaleY = Zoom;
            };
        }

        #region Events

        private static void ZoomPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            InternalZoomChanged?.Invoke(null, null);
        }

        private void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Reset();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_lastDragPoint.HasValue)
                return;

            var posNow = e.GetPosition(_scrollViewer);

            var dX = posNow.X - _lastDragPoint.Value.X;
            var dY = posNow.Y - _lastDragPoint.Value.Y;

            _lastDragPoint = posNow;

            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - dX);
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - dY);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(_scrollViewer);

            if (mousePos.X <= _scrollViewer.ViewportWidth && mousePos.Y < _scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                _scrollViewer.Cursor = Cursors.Hand;
                _lastDragPoint = mousePos;
                Mouse.Capture(_scrollViewer);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _scrollViewer.Cursor = Cursors.Arrow;
            _scrollViewer.ReleaseMouseCapture();
            _lastDragPoint = null;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _lastMousePositionOnTarget = e.GetPosition(_grid);

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Control:

                    #region Zoom

                    if (e.Delta > 0)
                    {
                        if (Zoom < 5.0)
                            Zoom += 0.1;
                    }
                    if (e.Delta < 0)
                    {
                        if (Zoom > 0.2)
                            Zoom -= 0.1;
                    }

                    _scaleTransform.ScaleX = Zoom;
                    _scaleTransform.ScaleY = Zoom;

                    var centerOfViewport = new Point(_scrollViewer.ViewportWidth / 2, _scrollViewer.ViewportHeight / 2);
                    _lastCenterPositionOnTarget = _scrollViewer.TranslatePoint(centerOfViewport, _grid);

                    #endregion

                    break;

                case ModifierKeys.Alt:

                    var verDelta = e.Delta > 0 ? -10.5 : 10.5;
                    _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + verDelta);

                    break;

                case ModifierKeys.Shift:

                    var horDelta = e.Delta > 0 ? -10.5 : 10.5;
                    _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + horDelta);

                    break;
            }

            e.Handled = true;
        }

        //TODO: Create a zoom selector, like the visual studio combobox
        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _scaleTransform.ScaleX = e.NewValue;
            _scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(_scrollViewer.ViewportWidth / 2, _scrollViewer.ViewportHeight / 2);
            _lastCenterPositionOnTarget = _scrollViewer.TranslatePoint(centerOfViewport, _grid);
        }

        private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0 && e.ExtentWidthChange == 0) return;

            Point? targetBefore = null;
            Point? targetNow = null;

            if (!_lastMousePositionOnTarget.HasValue)
            {
                if (_lastCenterPositionOnTarget.HasValue)
                {
                    var centerOfViewport = new Point(_scrollViewer.ViewportWidth / 2, _scrollViewer.ViewportHeight / 2);
                    var centerOfTargetNow = _scrollViewer.TranslatePoint(centerOfViewport, _grid);

                    targetBefore = _lastCenterPositionOnTarget;
                    targetNow = centerOfTargetNow;
                }
            }
            else
            {
                targetBefore = _lastMousePositionOnTarget;
                targetNow = Mouse.GetPosition(_grid);

                _lastMousePositionOnTarget = null;
            }

            if (targetBefore.HasValue)
            {
                var dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                var dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                var multiplicatorX = e.ExtentWidth / _grid.ActualWidth;
                var multiplicatorY = e.ExtentHeight / _grid.ActualHeight;

                var newOffsetX = _scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                var newOffsetY = _scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

                if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                {
                    return;
                }

                _scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                _scrollViewer.ScrollToVerticalOffset(newOffsetY);
            }
        }

        #endregion

        /// <summary>
        /// Resets the Scale and Position of the Child element.
        /// </summary>
        public void Reset()
        {
            //Resets the zoom.
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;
            Zoom = 1;

            //// reset pan
            //var tt = GetTranslateTransform(_child);
            //tt.X = 0.0;
            //tt.Y = 0.0;
        }

        /// <summary>
        /// Removes the image.
        /// </summary>
        public void Clear()
        {
            ImageSource = null;
        }

        /// <summary>
        /// Gets the ScrollViewer.
        /// </summary>
        /// <returns>A ScrollViewer.</returns>
        public ScrollViewer GetScrollViewer()
        {
            return _scrollViewer;
        }
    }
}
