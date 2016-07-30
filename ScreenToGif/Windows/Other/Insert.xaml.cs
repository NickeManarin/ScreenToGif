using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Writers;

namespace ScreenToGif.Windows.Other
{
    /// <summary>
    /// Interaction logic for Insert.xaml
    /// </summary>
    public partial class Insert : Window
    {
        #region Variables

        /// <summary>
        /// The current list of frames.
        /// </summary>
        public List<FrameInfo> ActualList { get; set; }
        private List<FrameInfo> NewList { get; set; }

        private bool _isRunning;
        private bool _isCancelled;

        private int _insertIndex;
        AdornerLayer _adornerLayer;
        private Point _lastPosition;

        private double _zoom = 1;
        UIElement _selectedElement = null;

        #endregion

        #region Contructors

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="actualList">The current list.</param>
        /// <param name="newList">The list to be inserted.</param>
        /// <param name="insertAt">The index to insert the list.</param>
        public Insert(List<FrameInfo> actualList, List<FrameInfo> newList, int insertAt)
        {
            InitializeComponent();

            LeftImage.Source = actualList[0].ImageLocation.SourceFrom();
            RightImage.Source = newList[0].ImageLocation.SourceFrom();

            ActualList = actualList;
            NewList = newList;
            _insertIndex = insertAt;

            FrameNumberLabel.Content = insertAt;
        }

        #endregion

        #region Mouse Events

        /// <summary>
        ///  Release the mouse capture of the image element.
        /// </summary>
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement != null)
            {
                _selectedElement.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handler for providing drag operation with selected element.
        /// </summary>
        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedElement == null) return;

            if (_selectedElement.GetType() == typeof(Image) && e.LeftButton == MouseButtonState.Pressed)
            {
                _selectedElement.MouseMove -= Image_MouseMove;

                var currentPosition = e.GetPosition(ContentGrid);

                Canvas.SetLeft(_selectedElement, Canvas.GetLeft(_selectedElement) +
                    (currentPosition.X - _lastPosition.X));
                Canvas.SetTop(_selectedElement, Canvas.GetTop(_selectedElement) +
                    (currentPosition.Y - _lastPosition.Y));

                _lastPosition = currentPosition;

                _selectedElement.MouseMove += Image_MouseMove;
            }
        }

        /// <summary>
        /// Handler for clearing element selection, adorner removal.
        /// </summary>
        private void Unselect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement != null)
            {
                _adornerLayer.Remove(_adornerLayer.GetAdorners(_selectedElement)[0]);
                _selectedElement = null;
            }
        }

        /// <summary>
        ///  Handler for element selection on the canvas providing resizing adorner.
        /// </summary>
        private void Select_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            #region Remove elsewhere before adding the layer.

            if (_selectedElement != null)
            {
                //Remove the adorner from the selected element
                _adornerLayer.Remove(_adornerLayer.GetAdorners(_selectedElement)[0]);
                _selectedElement = null;
            }

            #endregion

            _selectedElement = e.Source as UIElement;

            if (_selectedElement != null)
            {
                _adornerLayer = AdornerLayer.GetAdornerLayer(_selectedElement);
                _adornerLayer.Add(new ResizingAdorner(_selectedElement));

                if (_selectedElement.GetType() == typeof(Image) && _selectedElement.CaptureMouse())
                {
                    _lastPosition = e.GetPosition(ContentGrid);
                }
            }

            e.Handled = true;
        }

        #endregion

        #region Content Events

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
            }

            LeftCanvas.SizeChanged += Canvas_SizeChanged;
            RightCanvas.SizeChanged += Canvas_SizeChanged;
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

                    var verDelta = e.Delta > 0 ? -10.5 : 10.5;
                    scroller.ScrollToVerticalOffset(scroller.VerticalOffset + verDelta);

                    break;

                case ModifierKeys.Shift:

                    var horDelta = e.Delta > 0 ? -10.5 : 10.5;
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

        private void ResetLeftButton_Click(object sender, RoutedEventArgs e)
        {
            LeftImage.Width = ActualList[0].ImageLocation.SourceFrom().Width;
            LeftImage.Height = ActualList[0].ImageLocation.SourceFrom().Height;
            Canvas.SetTop(LeftImage, 0);
            Canvas.SetLeft(LeftImage, 0);
        }

        private void ResetRightButton_Click(object sender, RoutedEventArgs e)
        {
            RightImage.Width = NewList[0].ImageLocation.SourceFrom().Width;
            RightImage.Height = NewList[0].ImageLocation.SourceFrom().Height;
            Canvas.SetTop(RightImage, 0);
            Canvas.SetLeft(RightImage, 0);
        }

        private void ResetCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            LeftCanvas.Height = LeftImage.ActualHeight + Canvas.GetTop(LeftImage);
            LeftCanvas.Width = LeftImage.ActualWidth + Canvas.GetLeft(LeftImage);

            RightCanvas.Height = RightImage.ActualHeight + Canvas.GetTop(RightImage);
            RightCanvas.Width = RightImage.ActualWidth + Canvas.GetLeft(RightImage);

            EqualizeSizes();
        }

        #endregion

        #region Events

        private void Window_Activated(object sender, EventArgs e)
        {
            SizeToContent = SizeToContent.Manual;
            Activated -= Window_Activated;

            #region Set as Maximized if the window gets big enough

            var size = Native.ScreenSizeFromWindow(this);

            if (size.Height - Height < 200 || size.Width - Width < 200)
            {
                WindowState = WindowState.Maximized;
            }

            #endregion
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Size Diff

            if (Math.Abs(LeftImage.ActualWidth - RightImage.ActualWidth) > 0 || Math.Abs(LeftImage.ActualHeight - RightImage.ActualHeight) > 0)
            {
                StatusBand.Warning(FindResource("InsertFrames.DifferentSizes") as string);
            }

            #endregion

            #region Initial Sizing

            LeftCanvas.Width = LeftImage.ActualWidth;
            LeftCanvas.Height = LeftImage.ActualHeight;

            RightCanvas.Width = RightImage.ActualWidth;
            RightCanvas.Height = RightImage.ActualHeight;

            EqualizeSizes();

            #endregion

            MouseLeftButtonDown += Unselect_MouseLeftButtonDown;

            LeftImage.MouseMove += Image_MouseMove;
            RightImage.MouseMove += Image_MouseMove;

            LeftImage.PreviewMouseLeftButtonUp += Image_MouseUp;
            RightImage.PreviewMouseLeftButtonUp += Image_MouseUp;

            LeftImage.PreviewMouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;
            LeftCanvas.PreviewMouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;

            RightImage.PreviewMouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;
            RightCanvas.PreviewMouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;

            LeftCanvas.SizeChanged += Canvas_SizeChanged;
            RightCanvas.SizeChanged += Canvas_SizeChanged;

            UpdateLayout();
        }

        private void FillColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(Settings.Default.InsertFillColor, false) { Owner = this };
            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Settings.Default.InsertFillColor = colorDialog.SelectedColor;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _isRunning = true;

            #region Update UI

            Cursor = Cursors.AppStarting;

            LeftScrollViewer.IsEnabled = false;
            RightScrollViewer.IsEnabled = false;
            OkButton.IsEnabled = false;

            #endregion

            _insertDel = InsertFrames;
            _insertDel.BeginInvoke(AfterRadioButton.IsChecked.Value, InsertCallback, null);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;

            if (!_isRunning)
                DialogResult = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();

            GC.Collect();
        }

        #endregion

        #region Async Insert

        private delegate bool InsertDelegate(bool after);

        private InsertDelegate _insertDel;

        private bool InsertFrames(bool after)
        {
            try
            {
                ActionStack.Did(ActualList);

                #region Actual List

                var actualFrame = ActualList[0].ImageLocation.SourceFrom();
                double oldWidth = actualFrame.PixelWidth;
                double oldHeight = actualFrame.PixelHeight;
                var scale = Math.Round(actualFrame.DpiX, MidpointRounding.AwayFromZero) / 96d;

                //If the canvas size changed.
                if (Math.Abs(LeftCanvas.ActualWidth * scale - oldWidth) > 0 || Math.Abs(LeftCanvas.ActualHeight * scale - oldHeight) > 0 ||
                    Math.Abs(LeftImage.ActualWidth * scale - oldWidth) > 0 || Math.Abs(LeftImage.ActualHeight * scale - oldHeight) > 0)
                {
                    StartProgress(ActualList.Count, FindResource("Editor.UpdatingFrames").ToString());

                    foreach (var frameInfo in ActualList)
                    {
                        #region Resize Images

                        // Draws the images into a DrawingVisual component
                        var drawingVisual = new DrawingVisual();
                        using (var context = drawingVisual.RenderOpen())
                        {
                            //The back canvas.
                            context.DrawRectangle(new SolidColorBrush(Settings.Default.InsertFillColor), null,
                                new Rect(new Point(0, 0), new Point(Math.Round(RightCanvas.ActualWidth, MidpointRounding.AwayFromZero), Math.Round(RightCanvas.ActualHeight, MidpointRounding.AwayFromZero))));

                            var topPoint = Dispatcher.Invoke(() => Canvas.GetTop(LeftImage));
                            var leftPoint = Dispatcher.Invoke(() => Canvas.GetLeft(LeftImage));

                            //The image.
                            context.DrawImage(frameInfo.ImageLocation.SourceFrom(),
                                new Rect(leftPoint, topPoint, LeftImage.ActualWidth, LeftImage.ActualHeight));

                            //context.DrawText(new FormattedText("Hi!", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 32, Brushes.Black), new Point(0, 0));
                        }

                        // Converts the Visual (DrawingVisual) into a BitmapSource
                        var bmp = new RenderTargetBitmap((int)(LeftCanvas.ActualWidth * scale), (int)(LeftCanvas.ActualHeight * scale),
                            actualFrame.DpiX, actualFrame.DpiX, PixelFormats.Pbgra32);
                        bmp.Render(drawingVisual);

                        #endregion

                        #region Save

                        // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                        var encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));

                        // Saves the image into a file using the encoder
                        using (Stream stream = File.Create(frameInfo.ImageLocation))
                            encoder.Save(stream);

                        #endregion

                        if (_isCancelled)
                            return false;

                        UpdateProgress(ActualList.IndexOf(frameInfo));
                    }
                }

                #endregion

                #region New List

                var newFrame = NewList[0].ImageLocation.SourceFrom();
                oldWidth = newFrame.PixelWidth;
                oldHeight = newFrame.PixelHeight;
                scale = Math.Round(newFrame.DpiX, MidpointRounding.AwayFromZero) / 96d;

                StartProgress(ActualList.Count, FindResource("Editor.ImportingFrames").ToString());

                var folder = Path.GetDirectoryName(ActualList[0].ImageLocation);
                var insertFolder = Path.GetDirectoryName(NewList[0].ImageLocation);

                //If the canvas size changed.
                if (Math.Abs(RightCanvas.ActualWidth * scale - oldWidth) > 0 ||
                    Math.Abs(RightCanvas.ActualHeight * scale - oldHeight) > 0 ||
                    Math.Abs(RightImage.ActualWidth * scale - oldWidth) > 0 ||
                    Math.Abs(RightImage.ActualHeight * scale - oldHeight) > 0)
                {
                    foreach (var frameInfo in NewList)
                    {
                        #region Resize Images

                        // Draws the images into a DrawingVisual component
                        var drawingVisual = new DrawingVisual();
                        using (var context = drawingVisual.RenderOpen())
                        {
                            //The back canvas.
                            context.DrawRectangle(new SolidColorBrush(Settings.Default.InsertFillColor), null,
                                new Rect(new Point(0, 0),
                                    new Point(Math.Round(RightCanvas.ActualWidth, MidpointRounding.AwayFromZero), Math.Round(RightCanvas.ActualHeight, MidpointRounding.AwayFromZero))));

                            var topPoint = Dispatcher.Invoke(() => Canvas.GetTop(RightImage));
                            var leftPoint = Dispatcher.Invoke(() => Canvas.GetLeft(RightImage));

                            //The front image.
                            context.DrawImage(frameInfo.ImageLocation.SourceFrom(),
                                new Rect(leftPoint, topPoint, RightImage.ActualWidth,
                                    RightImage.ActualHeight));
                        }

                        // Converts the Visual (DrawingVisual) into a BitmapSource
                        var bmp = new RenderTargetBitmap((int)(RightCanvas.ActualWidth * scale),
                            (int)(RightCanvas.ActualHeight * scale),
                            newFrame.DpiX, newFrame.DpiX, PixelFormats.Pbgra32);
                        bmp.Render(drawingVisual);

                        #endregion

                        #region Save

                        // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                        var encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));

                        File.Delete(frameInfo.ImageLocation);

                        var fileName = Path.Combine(folder,
                            $"{_insertIndex}-{NewList.IndexOf(frameInfo)} {DateTime.Now.ToString("hh-mm-ss")}.bmp");

                        // Saves the image into a file using the encoder
                        using (Stream stream = File.Create(fileName))
                            encoder.Save(stream);

                        frameInfo.ImageLocation = fileName;

                        #endregion

                        if (_isCancelled)
                            return false;

                        UpdateProgress(NewList.IndexOf(frameInfo));
                    }
                }
                else
                {
                    foreach (var frameInfo in NewList)
                    {
                        #region Move

                        var fileName = Path.Combine(folder, $"{_insertIndex}-{NewList.IndexOf(frameInfo)} {DateTime.Now.ToString("hh-mm-ss")}.png");

                        File.Move(frameInfo.ImageLocation, fileName);

                        frameInfo.ImageLocation = fileName;

                        #endregion

                        if (_isCancelled)
                            return false;

                        UpdateProgress(NewList.IndexOf(frameInfo));
                    }
                }

                Directory.Delete(insertFolder, true);

                #endregion

                if (_isCancelled)
                    return false;

                #region Merge the Lists

                if (after)
                    _insertIndex++;

                ActualList.InsertRange(_insertIndex, NewList);

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Insert Error");
                Dispatcher.Invoke(() => Dialog.Ok("Insert Error", "Something Wrong Happened", ex.Message));

                return false;
            }
        }

        private void InsertCallback(IAsyncResult r)
        {
            var result = _insertDel.EndInvoke(r);

            if (result)
            {
                GC.Collect();

                Dispatcher.Invoke(() => DialogResult = true);
                return;
            }

            _isCancelled = false;
            GC.Collect();

            #region Update UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.Arrow;

                LeftScrollViewer.IsEnabled = true;
                RightScrollViewer.IsEnabled = true;
                OkButton.IsEnabled = true;

                DialogResult = false;
            });

            HideProgress();

            #endregion
        }

        #endregion

        #region Methods

        #region Progress

        private void StartProgress(int maximum, string description)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressGrid.Visibility = Visibility.Visible;

                InsertProgressBar.Maximum = maximum;
                InsertProgressBar.Value = 0;

                StatusLabel.Content = description;
            });
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                InsertProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                InsertProgressBar.Value = 0;
                ProgressGrid.Visibility = Visibility.Hidden;
            });
        }

        #endregion

        private void EqualizeSizes()
        {
            if (RightCanvas.Width >= LeftCanvas.Width)
                LeftCanvas.Width = RightCanvas.Width;
            else
                RightCanvas.Width = LeftCanvas.Width;

            if (RightCanvas.Height >= LeftCanvas.Height)
                LeftCanvas.Height = RightCanvas.Height;
            else
                RightCanvas.Height = LeftCanvas.Height;
        }

        #endregion
    }
}
