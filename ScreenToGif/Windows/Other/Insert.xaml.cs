using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    /// <summary>
    /// Interaction logic for Insert.xaml
    /// </summary>
    public partial class Insert : Window
    {
        AdornerLayer aLayer;

        private double _zoom = 1;
        bool _isDown;
        bool _isDragging;
        bool selected = false;
        UIElement selectedElement = null;

        Point _startPoint;
        private double _originalLeft;
        private double _originalTop;


        public Insert()
        {
            InitializeComponent();
        }

        public Insert(List<FrameInfo> oldFrame, List<FrameInfo> newList)
        {
            InitializeComponent();

            LeftImage.Source = oldFrame[0].ImageLocation.SourceFrom();
            RightImage.Source = newList[0].ImageLocation.SourceFrom();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Initial Sizing

            LeftCanvas.Width = LeftImage.ActualWidth;
            LeftCanvas.Height = LeftImage.ActualHeight;

            RightCanvas.Width = RightImage.ActualWidth;
            RightCanvas.Height = RightImage.ActualHeight;

            EqualizeSizes();

            #endregion

            #region Size Diff

            if (LeftImage.Width != RightImage.Width || LeftImage.Width != RightImage.Width)
            {
                WarningGrid.Visibility = Visibility.Visible;
            }

            #endregion

            MouseLeftButtonDown += Window1_MouseLeftButtonDown;

            LeftImage.MouseMove += Image_MouseMove;
            RightImage.MouseMove += Image_MouseMove;
            LeftImage.MouseLeave += Image_MouseLeave;
            RightImage.MouseLeave += Image_MouseLeave;

            LeftImage.PreviewMouseLeftButtonDown += myCanvas_PreviewMouseLeftButtonDown;
            LeftCanvas.PreviewMouseLeftButtonDown += myCanvas_PreviewMouseLeftButtonDown;

            RightImage.PreviewMouseLeftButtonDown += myCanvas_PreviewMouseLeftButtonDown;
            RightCanvas.PreviewMouseLeftButtonDown += myCanvas_PreviewMouseLeftButtonDown;

            LeftCanvas.SizeChanged += Canvas_SizeChanged;
            RightCanvas.SizeChanged += Canvas_SizeChanged;

            //LeftImage.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
            //LeftCanvas.MouseLeftButtonUp += new MouseButtonEventHandler(DragFinishedMouseHandler);
        }

        private void EqualizeSizes()
        {
            if (RightCanvas.ActualWidth >= LeftCanvas.Width)
                LeftCanvas.Width = RightCanvas.ActualWidth;
            else
                RightCanvas.Width = LeftCanvas.ActualWidth;

            if (RightCanvas.ActualHeight >= LeftCanvas.Height)
                LeftCanvas.Height = RightCanvas.ActualHeight;
            else
                RightCanvas.Height = LeftCanvas.ActualHeight;

            //if (canvas.ActualWidth >= RightCanvas.Width)
            //    RightCanvas.Width = canvas.ActualWidth;
            //else
            //    canvas.Width = RightCanvas.ActualWidth;

            //if (canvas.ActualHeight >= RightCanvas.Height)
            //    RightCanvas.Height = canvas.ActualHeight;
            //else
            //    canvas.Height = RightCanvas.ActualHeight;
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LeftCanvas.SizeChanged -= Canvas_SizeChanged;
            RightCanvas.SizeChanged -= Canvas_SizeChanged;

            var canvas = sender as Canvas;

            if (canvas != null)
            {
                if (canvas.Name.StartsWith("Right"))
                {
                    #region If Right, changes the Left Sizes

                    LeftCanvas.Width = canvas.ActualWidth;
                    LeftCanvas.Height = canvas.ActualHeight;

                    #endregion
                }
                else
                {
                    #region Else Left, changes the Right Sizes

                    RightCanvas.Width = canvas.ActualWidth;
                    RightCanvas.Height = canvas.ActualHeight;

                    #endregion
                }

                WidthCanvasTextBox.Value = (long)canvas.ActualWidth;
                HeightCanvasTextBox.Value = (long)canvas.ActualHeight;
            }

            LeftCanvas.SizeChanged += Canvas_SizeChanged;
            RightCanvas.SizeChanged += Canvas_SizeChanged;
        }

        // Handler for drag stopping on leaving the window
        void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            StopDragging();
            e.Handled = true;
        }

        // Handler for drag stopping on user choise
        void DragFinishedMouseHandler(object sender, MouseButtonEventArgs e)
        {
            //StopDragging();
            //e.Handled = true;
        }

        // Method for stopping dragging
        private void StopDragging()
        {
            if (_isDown)
            {
                _isDown = false;
                _isDragging = false;
            }
        }

        // Hanler for providing drag operation with selected element
        void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDown) return;

            if (!_isDragging)
            {
                var hor = Math.Abs(e.GetPosition(LeftImage).X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
                var ver = Math.Abs(e.GetPosition(LeftImage).Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

                _isDragging = hor || ver;

            }

            if (_isDragging)
            {
                Point position = Mouse.GetPosition(LeftCanvas);

                Canvas.SetTop(selectedElement, position.Y - (_startPoint.Y - _originalTop));
                Canvas.SetLeft(selectedElement, position.X - (_startPoint.X - _originalLeft));
            }
        }

        // Handler for clearing element selection, adorner removal
        void Window1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selected)
            {
                selected = false;

                if (selectedElement != null)
                {
                    aLayer.Remove(aLayer.GetAdorners(selectedElement)[0]);
                    selectedElement = null;
                }
            }
        }

        // Handler for element selection on the canvas providing resizing adorner
        void myCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Remove selection on clicking anywhere the window
            if (selected)
            {
                selected = false;
                if (selectedElement != null)
                {
                    // Remove the adorner from the selected element
                    aLayer.Remove(aLayer.GetAdorners(selectedElement)[0]);
                    selectedElement = null;
                }
            }

            //_isDown = true;

            selectedElement = e.Source as UIElement;

            //_originalLeft = Canvas.GetLeft(selectedElement);
            //_originalTop = Canvas.GetTop(selectedElement);

            aLayer = AdornerLayer.GetAdornerLayer(selectedElement);
            aLayer.Add(new ResizingAdorner(selectedElement));
            selected = true;
            e.Handled = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SupressButton_Click(object sender, RoutedEventArgs e)
        {
            WarningGrid.Visibility = Visibility.Collapsed;
        }

        private void LeftImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WidthLeftTextBox.Value = (long)LeftImage.ActualWidth;
            HeightLeftTextBox.Value = (long)LeftImage.ActualHeight;
        }

        private void FillColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(Settings.Default.InsertFillColor, false);
            colorDialog.Owner = this;
            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.InsertFillColor = colorDialog.SelectedColor;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
        }


        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scroller = sender as ScrollViewer;

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Control:

                #region Zoom

                if (e.Delta > 0)
                {
                    if (_zoom < 5.0)
                        _zoom += 0.1;
                }
                if (e.Delta < 0)
                {
                    if (_zoom > 0.2)
                        _zoom -= 0.1;
                }

                LeftCanvas.LayoutTransform = new ScaleTransform(_zoom, _zoom);
                RightCanvas.LayoutTransform = new ScaleTransform(_zoom, _zoom);

                var centerOfViewport = new Point(scroller.ViewportWidth / 2, scroller.ViewportHeight / 2);
                //_lastCenterPositionOnTarget = _scrollViewer.TranslatePoint(centerOfViewport, _grid);

                #endregion

                    break;

                case ModifierKeys.Alt:

                    double verDelta = e.Delta > 0 ? -10.5 : 10.5;
                    scroller.ScrollToVerticalOffset(scroller.VerticalOffset + verDelta);

                    break;

                case ModifierKeys.Shift:

                    double horDelta = e.Delta > 0 ? -10.5 : 10.5;
                    scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + horDelta);

                    break; 
            }

        }

        private void ScrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _zoom = 1;
            LeftCanvas.LayoutTransform = new ScaleTransform(_zoom, _zoom);
            RightCanvas.LayoutTransform = new ScaleTransform(_zoom, _zoom);
        }
    }
}
