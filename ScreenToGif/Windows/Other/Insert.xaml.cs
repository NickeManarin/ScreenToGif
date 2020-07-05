using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class Insert : Window
    {
        #region Variables

        /// <summary>
        /// The current list of frames.
        /// </summary>
        public List<FrameInfo> CurrentList { get; set; }
        private List<FrameInfo> NewList { get; set; }

        private bool _isRunning;
        private bool _isCancelled;

        private int _insertIndex;
        AdornerLayer _adornerLayer;

        private double _zoom = 1;
        UIElement _selectedElement = null;

        private double _leftWidth = 0;
        private double _leftHeight = 0;

        private double _rightWidth = 0;
        private double _rightHeight = 0;

        #endregion


        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="currentList">The current list.</param>
        /// <param name="newList">The list to be inserted.</param>
        /// <param name="insertAt">The index to insert the list.</param>
        public Insert(List<FrameInfo> currentList, List<FrameInfo> newList, int insertAt)
        {
            InitializeComponent();

            CurrentList = currentList;
            NewList = newList;
            _insertIndex = insertAt;

            FrameNumberLabel.Content = insertAt;
        }


        #region Mouse Events

        /// <summary>
        /// Handler for clearing element selection, adorner removal.
        /// </summary>
        private void Unselect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement == null)
                return;

            var adorner = _adornerLayer.GetAdorners(_selectedElement)?.FirstOrDefault();

            if (adorner != null)
                _adornerLayer.Remove(adorner);

            _selectedElement = null;
        }

        /// <summary>
        ///  Handler for element selection on the canvas providing resizing adorner.
        /// </summary>
        private void Select_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Ignore this event if it's the same element.
            if (Equals(_selectedElement, sender as UIElement))
                return;

            #region Remove elsewhere before adding the layer.

            if (_selectedElement != null)
            {
                var adornerList = _adornerLayer.GetAdorners(_selectedElement);

                var adorner = adornerList?.OfType<ResizingAdorner>().FirstOrDefault();

                if (adorner != null)
                {
                    adorner.Destroy();

                    //Remove the adorner from the selected element
                    _adornerLayer.Remove(adorner);
                    _selectedElement = null;
                }
            }

            #endregion

            #region Add 

            _selectedElement = e.Source as UIElement;

            if (_selectedElement != null)
            {
                _adornerLayer = AdornerLayer.GetAdornerLayer(_selectedElement);
                _adornerLayer?.Add(new ResizingAdorner(_selectedElement, _selectedElement is Image, ContentGrid, e.GetPosition(ContentGrid)));
            }

            #endregion
        }

        #endregion

        #region Content Events

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LeftCanvas.SizeChanged -= Canvas_SizeChanged;
            RightCanvas.SizeChanged -= Canvas_SizeChanged;

            if (sender is Canvas canvas)
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
            if (!(sender is ScrollViewer scroller))
                return;

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
            LeftImage.Width = _leftWidth;
            LeftImage.Height = _leftHeight;

            Canvas.SetTop(LeftImage, 0);
            Canvas.SetLeft(LeftImage, 0);
        }

        private void ResetRightButton_Click(object sender, RoutedEventArgs e)
        {
            RightImage.Width = _rightWidth;
            RightImage.Height = _rightHeight;

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
                WindowState = WindowState.Maximized;

            #endregion
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Size check

            var left = NewList[0].Path.SourceFrom();
            var right = CurrentList[0].Path.SourceFrom();

            //Left: New, Right: Current
            LeftImage.Source = left;
            RightImage.Source = right;

            //The image should be displayed based on the scale of the screen.
            var scaleLeft = Math.Round(left.DpiX / 96d, 2);
            var scaleRight = Math.Round(right.DpiX / 96d, 2);
            var scale = this.Scale();

            var scaleDiffLeft = Math.Round(scale / scaleLeft, 2);
            var scaleDiffRight = Math.Round(scale / scaleRight, 2);

            LeftImage.Width = _leftWidth = left.Width / scaleDiffLeft;
            LeftImage.Height = _leftHeight = left.Height / scaleDiffLeft;

            RightImage.Width = _rightWidth = right.Width / scaleDiffRight;
            RightImage.Height = _rightHeight = right.Height / scaleDiffRight;

            #endregion

            #region Initial sizing

            LeftImage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            LeftImage.Arrange(new Rect(new Point(0,0), LeftImage.DesiredSize));
            RightImage.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            RightImage.Arrange(new Rect(new Point(0, 0), RightImage.DesiredSize));

            if (Math.Abs(LeftImage.ActualWidth - RightImage.ActualWidth) > 0.01 || Math.Abs(LeftImage.ActualHeight - RightImage.ActualHeight) > 0.01)
                StatusBand.Warning(LocalizationHelper.Get("S.InsertFrames.DifferentSizes"));

            LeftCanvas.Width = LeftImage.ActualWidth;
            LeftCanvas.Height = LeftImage.ActualHeight;

            RightCanvas.Width = RightImage.ActualWidth;
            RightCanvas.Height = RightImage.ActualHeight;

            EqualizeSizes();

            #endregion

            MouseLeftButtonDown += Unselect_MouseLeftButtonDown;

            LeftImage.MouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;
            LeftCanvas.MouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;

            RightImage.MouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;
            RightCanvas.MouseLeftButtonDown += Select_PreviewMouseLeftButtonDown;

            LeftCanvas.SizeChanged += Canvas_SizeChanged;
            RightCanvas.SizeChanged += Canvas_SizeChanged;

            UpdateLayout();
        }

        private void FillColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(UserSettings.All.InsertFillColor, false) { Owner = this };
            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
                UserSettings.All.InsertFillColor = colorDialog.SelectedColor;
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
            _insertDel.BeginInvoke(AfterRadioButton.IsChecked == true, this.Scale(), InsertCallback, null);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;

            if (!_isRunning)
                DialogResult = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            UserSettings.Save();

            GC.Collect();
        }

        #endregion

        #region Async Insert

        private delegate bool InsertDelegate(bool after, double screenScale);

        private InsertDelegate _insertDel;

        private bool InsertFrames(bool after, double screenScale)
        {
            try
            {
                //Sizes.
                var left = NewList[0].Path.SourceFrom();
                var right = CurrentList[0].Path.SourceFrom();

                //The image should be displayed based on the scale of the project.
                var scaleLeft = left.DpiX / 96d;
                var scaleRight = right.DpiX / 96d; //Math.Round(right.DpiX / 96d, 2);

                var scaleDiffLeft = screenScale / scaleLeft;
                var scaleDiffRight = screenScale / scaleRight;

                #region Current frames

                //If the canvas size changed.
                if (Math.Abs(RightCanvas.ActualWidth - _rightWidth) > 0.1 || Math.Abs(RightCanvas.ActualHeight - _rightHeight) > 0.1 ||
                    Math.Abs(RightImage.ActualWidth - _rightWidth) > 0.1 || Math.Abs(RightImage.ActualHeight - _rightHeight) > 0.1)
                {
                    StartProgress(CurrentList.Count, FindResource("S.Editor.UpdatingFrames").ToString());

                    //Saves the state before resizing the images.
                    ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, CurrentList, Util.Other.ListOfIndexes(0, CurrentList.Count));

                    foreach (var frameInfo in CurrentList)
                    {
                        #region Resize Images

                        //Draws the images into a DrawingVisual component.
                        var drawingVisual = new DrawingVisual();
                        using (var context = drawingVisual.RenderOpen())
                        {
                            //The back canvas.
                            context.DrawRectangle(new SolidColorBrush(UserSettings.All.InsertFillColor), null,
                                new Rect(new Point(0, 0), new Point(Math.Round(RightCanvas.ActualWidth, MidpointRounding.AwayFromZero), Math.Round(RightCanvas.ActualHeight, MidpointRounding.AwayFromZero))));

                            var topPoint = Dispatcher.Invoke(() => Canvas.GetTop(RightImage)) * scaleDiffRight;
                            var leftPoint = Dispatcher.Invoke(() => Canvas.GetLeft(RightImage)) * scaleDiffRight;

                            //The image.
                            context.DrawImage(frameInfo.Path.SourceFrom(), new Rect(leftPoint, topPoint, RightImage.ActualWidth * scaleDiffRight, RightImage.ActualHeight * scaleDiffRight));
                            //context.DrawText(new FormattedText("Hi!", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 32, Brushes.Black), new Point(0, 0));
                        }

                        //Converts the Visual (DrawingVisual) into a BitmapSource.
                        var bmp = new RenderTargetBitmap((int)Math.Round(RightCanvas.ActualWidth * screenScale, MidpointRounding.AwayFromZero),
                            (int)Math.Round(RightCanvas.ActualHeight * screenScale, MidpointRounding.AwayFromZero), right.DpiX, right.DpiX, PixelFormats.Pbgra32);
                        bmp.Render(drawingVisual);

                        #endregion

                        #region Save

                        //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));

                        //Saves the image into a file using the encoder
                        using (Stream stream = File.Create(frameInfo.Path))
                            encoder.Save(stream);

                        #endregion

                        if (_isCancelled)
                            return false;

                        UpdateProgress(CurrentList.IndexOf(frameInfo));
                    }
                }

                #endregion

                #region New frames

                StartProgress(CurrentList.Count, FindResource("S.Editor.ImportingFrames").ToString());

                var folder = Path.GetDirectoryName(CurrentList[0].Path);
                var insertFolder = Path.GetDirectoryName(NewList[0].Path);

                //If the canvas size changed.
                if (Math.Abs(LeftCanvas.ActualWidth - _leftWidth) > 0.1 || Math.Abs(LeftCanvas.ActualHeight - _leftHeight) > 0.1 ||
                    Math.Abs(LeftImage.ActualWidth - _leftWidth) > 0.1 || Math.Abs(LeftImage.ActualHeight - _leftHeight) > 0.1 || Math.Abs(left.DpiX - right.DpiX) > 0.1)
                {
                    foreach (var frameInfo in NewList)
                    {
                        #region Resize Images

                        //Draws the images into a DrawingVisual component.
                        var drawingVisual = new DrawingVisual();
                        using (var context = drawingVisual.RenderOpen())
                        {
                            //The back canvas.
                            context.DrawRectangle(new SolidColorBrush(UserSettings.All.InsertFillColor), null,
                                new Rect(new Point(0, 0), new Point(Math.Round(RightCanvas.ActualWidth * scaleDiffRight, MidpointRounding.AwayFromZero), 
                                Math.Round(RightCanvas.ActualHeight * scaleDiffRight, MidpointRounding.AwayFromZero))));

                            var topPoint = Dispatcher.Invoke(() => Canvas.GetTop(LeftImage)) * scaleDiffRight;
                            var leftPoint = Dispatcher.Invoke(() => Canvas.GetLeft(LeftImage)) * scaleDiffRight;

                            //The front image.
                            context.DrawImage(frameInfo.Path.SourceFrom(), new Rect(leftPoint, topPoint, LeftImage.ActualWidth * scaleDiffRight, LeftImage.ActualHeight * scaleDiffRight)); // * scaleDiffLeft
                        }

                        //Converts the Visual (DrawingVisual) into a BitmapSource. Using the actual frame dpi.
                        var bmp = new RenderTargetBitmap((int)Math.Round(LeftCanvas.ActualWidth * screenScale, MidpointRounding.AwayFromZero),
                            (int)Math.Round(LeftCanvas.ActualHeight * screenScale, MidpointRounding.AwayFromZero), right.DpiX, right.DpiX, PixelFormats.Pbgra32);
                        bmp.Render(drawingVisual);

                        #endregion

                        #region Save

                        //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));

                        File.Delete(frameInfo.Path);

                        var fileName = Path.Combine(folder, $"{_insertIndex}-{NewList.IndexOf(frameInfo)} {DateTime.Now:hh-mm-ss}.png");

                        //Saves the image into a file using the encoder.
                        using (Stream stream = File.Create(fileName))
                            encoder.Save(stream);

                        frameInfo.Path = fileName;

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

                        var fileName = Path.Combine(folder, $"{_insertIndex}-{NewList.IndexOf(frameInfo)} {DateTime.Now:hh-mm-ss}.png");

                        File.Move(frameInfo.Path, fileName);

                        frameInfo.Path = fileName;

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

                #region Merge the lists

                if (after)
                    _insertIndex++;

                //Saves the state before inserting the images. This was removed because it was causing a crash when applying undo twice.
                //ActionStack.SaveState(ActionStack.EditAction.Add, _insertIndex, NewList.Count);

                CurrentList.InsertRange(_insertIndex, NewList);

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Insert Error");
                Dispatcher.Invoke(() => ErrorDialog.Ok("Insert Error", "Something Wrong Happened", ex.Message, ex));

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