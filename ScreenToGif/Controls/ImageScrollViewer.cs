using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenToGif.Controls
{
    public class ImageScrollViewer : ScrollViewer
    {
        #region Variables

        public readonly static DependencyProperty ImageSourceProperty;
        public readonly static DependencyProperty ZoomProperty;

        private Point? _lastCenterPositionOnTarget;
        private Point? _lastMousePositionOnTarget;
        private Point? _lastDragPoint;

        private ScaleTransform _scaleTransform;
        private Grid _grid;

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
        public Double Zoom
        {
            get { return (Double)GetValue(ZoomProperty); }
            set
            {
                SetCurrentValue(ZoomProperty, value);

                //Should I control the max-min here?
                if (value < 0.1)
                {
                    Zoom = 0.1;
                }
                if (value > 5.0)
                {
                    Zoom = 5;
                }

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

        static ImageScrollViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageScrollViewer), new FrameworkPropertyMetadata(typeof(ImageScrollViewer)));

            ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(string), typeof(ImageScrollViewer), new FrameworkPropertyMetadata());
            ZoomProperty = DependencyProperty.Register("Zoom", typeof(Double), typeof(ImageScrollViewer), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.None, ZoomPropertyChangedCallback));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _scaleTransform = Template.FindName("ScaleTransform", this) as ScaleTransform;
            _grid = Template.FindName("Grid", this) as Grid;

           ScrollChanged += OnScrollViewerScrollChanged;
           MouseLeftButtonUp += OnMouseLeftButtonUp;
           PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
           PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;
           PreviewMouseWheel += OnPreviewMouseWheel;

           PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
           MouseMove += OnMouseMove;

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

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(this);

                double dX = posNow.X - _lastDragPoint.Value.X;
                double dY = posNow.Y - _lastDragPoint.Value.Y;

                _lastDragPoint = posNow;

                ScrollToHorizontalOffset(HorizontalOffset - dX);
                ScrollToVerticalOffset(VerticalOffset - dY);
            }
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(this);

            if (mousePos.X <= ViewportWidth && mousePos.Y < ViewportHeight) //make sure we still can use the scrollbars
            {
                Cursor = Cursors.Hand;
                _lastDragPoint = mousePos;
                Mouse.Capture(this);
            }
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Cursor = Cursors.Arrow;
            ReleaseMouseCapture();
            _lastDragPoint = null;
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

                    var centerOfViewport = new Point(ViewportWidth / 2, ViewportHeight / 2);
                    _lastCenterPositionOnTarget = TranslatePoint(centerOfViewport, _grid);

                    #endregion

                    break;

                case ModifierKeys.Alt:

                    double verDelta = e.Delta > 0 ? -10.5 : 10.5;
                    ScrollToVerticalOffset(VerticalOffset + verDelta);

                    break;

                case ModifierKeys.Shift:

                    double horDelta = e.Delta > 0 ? -10.5 : 10.5;
                    ScrollToHorizontalOffset(HorizontalOffset + horDelta);

                    break;
            }

            e.Handled = true;
        }

        //TODO: Create a zoom selector, like the visual studio combobox
        void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _scaleTransform.ScaleX = e.NewValue;
            _scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(ViewportWidth / 2, ViewportHeight / 2);
            _lastCenterPositionOnTarget = TranslatePoint(centerOfViewport, _grid);
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0 && e.ExtentWidthChange == 0) return;

            Point? targetBefore = null;
            Point? targetNow = null;

            if (!_lastMousePositionOnTarget.HasValue)
            {
                if (_lastCenterPositionOnTarget.HasValue)
                {
                    var centerOfViewport = new Point(ViewportWidth / 2, ViewportHeight / 2);
                    Point centerOfTargetNow = TranslatePoint(centerOfViewport, _grid);

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
                double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                double multiplicatorX = e.ExtentWidth / _grid.ActualWidth;
                double multiplicatorY = e.ExtentHeight / _grid.ActualHeight;

                double newOffsetX = HorizontalOffset - dXInTargetPixels * multiplicatorX;
                double newOffsetY = VerticalOffset - dYInTargetPixels * multiplicatorY;

                if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                {
                    return;
                }

                ScrollToHorizontalOffset(newOffsetX);
                ScrollToVerticalOffset(newOffsetY);
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

            ////Resets the Translate
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
    }
}
