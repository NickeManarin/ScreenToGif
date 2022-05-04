using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;

namespace ScreenToGif.Controls;

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

    private double _previousZoom = 1d;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(string), typeof(ZoomBox),
        new FrameworkPropertyMetadata(ImageSource_PropertyChanged));

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(double), typeof(ZoomBox),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, Zoom_PropertyChanged));

    public static readonly DependencyProperty ImageScaleProperty = DependencyProperty.Register("ImageScale", typeof(double), typeof(ZoomBox),
        new FrameworkPropertyMetadata(0.1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register("PixelSize", typeof(Size), typeof(ZoomBox),
        new FrameworkPropertyMetadata(new Size(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FitImageProperty = DependencyProperty.Register("FitImage", typeof(bool), typeof(ZoomBox),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    /// <summary>
    /// The image source.
    /// </summary>
    [Description("The image source.")]
    public string ImageSource
    {
        get => (string)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    /// <summary>
    /// The zoom level of the control.
    /// </summary>
    [Description("The zoom level of the control.")]
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetCurrentValue(ZoomProperty, value);
    }

    /// <summary>
    /// The scale (dpi/96) of the screen.
    /// </summary>
    [Description("The zoom level of the control.")]
    public double ImageScale
    {
        get => (double)GetValue(ImageScaleProperty);
        set => SetCurrentValue(ImageScaleProperty, value);
    }

    /// <summary>
    /// The pixel size of the image, independently of DPI.
    /// </summary>
    [Description("The pixel size of the image, independently of DPI.")]
    public Size PixelSize
    {
        get => (Size)GetValue(PixelSizeProperty);
        set => SetCurrentValue(PixelSizeProperty, value);
    }

    /// <summary>
    /// Decides if it should fit the image on start.
    /// </summary>
    [Description("Decides if it should fit the image on start.")]
    public bool FitImage
    {
        get => (bool)GetValue(FitImageProperty);
        set => SetCurrentValue(FitImageProperty, value);
    }

    /// <summary>
    /// The DPI of the image.
    /// </summary>
    public double ImageDpi { get; set; }

    /// <summary>
    /// The amount of scale of the image x the visuals. 
    /// (Dpi of the images compared with the dpi of the UIElements).
    /// </summary>
    public double ScaleDiff { get; set; }

    #endregion

    #region Custom Events

    /// <summary>
    /// Create a custom routed event by first registering a RoutedEventID, this event uses the bubbling routing strategy.
    /// </summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble,
        typeof(RoutedEventHandler), typeof(ZoomBox));

    /// <summary>
    /// Event raised when the numeric value is changed.
    /// </summary>
    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public void RaiseValueChangedEvent()
    {
        if (ValueChangedEvent == null || !IsLoaded)
            return;

        var newEventArgs = new RoutedEventArgs(ValueChangedEvent);
        RaiseEvent(newEventArgs);
    }

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
    }

    #region Events

    private static void ImageSource_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is ZoomBox zoomBox))
            return;

        zoomBox.ImageSource = e.NewValue as string;
    }

    private static void Zoom_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is ZoomBox box))
            return;

        if (!(e.NewValue is double value))
            return;

        //Maximum and minimum.
        if (value < 0.1)
            box.Zoom = 0.1;
        if (value > 5.0)
            box.Zoom = 5;

        box.RefreshImage();
    }

    private static void ImageScale_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is ZoomBox box))
            return;

        box.RefreshImage();
    }

    private void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
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

    private void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.ExtentHeightChange) < 0.01 && Math.Abs(e.ExtentWidthChange) < 0.01)
            return;

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

        if (!targetBefore.HasValue)
            return;

        var dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
        var dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

        var multiplicatorX = e.ExtentWidth / _grid.ActualWidth;
        var multiplicatorY = e.ExtentHeight / _grid.ActualHeight;

        var newOffsetX = _scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
        var newOffsetY = _scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

        if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
            return;

        _scrollViewer.ScrollToHorizontalOffset(newOffsetX);
        _scrollViewer.ScrollToVerticalOffset(newOffsetY);
    }

    #endregion

    public void LoadFromPath(string path)
    {
        ImageSource = path;

        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnDemand;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            PixelSize = new Size(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            ImageScale = Math.Round(bitmapImage.DpiX / 96d, 2);
        }

        RefreshImage();
    }

    public void RefreshImage()
    {
        //ImageScale = ImageSource.ScaleOf();

        //Calculates how much bigger or smaller the image should be presented, based on the window and image scale (DPI/96).
        ImageDpi = ImageScale * 96d;
        ScaleDiff = this.Scale() / ImageScale;

        //Apply the zoom, with the scale difference.
        if (_scaleTransform != null)
        {
            _scaleTransform.ScaleX = Zoom / ScaleDiff;
            _scaleTransform.ScaleY = Zoom / ScaleDiff;
        }

        //Raise event.
        RaiseValueChangedEvent();
    }

    /// <summary>
    /// Resets the Scale and Position of the Child element.
    /// </summary>
    public void Reset()
    {
        //Resets the zoom.
        Zoom = 1;

        //Resets the position.
        //var tt = GetTranslateTransform(_child);
        //tt.X = 0.0;
        //tt.Y = 0.0;
    }

    /// <summary>
    /// Save the current zoom level.
    /// </summary>
    public void SaveCurrentZoom()
    {
        _previousZoom = Zoom;
    }

    /// <summary>
    /// Returns to the previously saved zoom level.
    /// </summary>
    public void RestoreSavedZoom()
    {
        //Resets the zoom.
        Zoom = _previousZoom;
    }

    /// <summary>
    /// Removes the image.
    /// </summary>
    public void Clear()
    {
        ImageSource = null;
        GC.Collect(1);
    }

    /// <summary>
    /// Gets the ScrollViewer.
    /// </summary>
    /// <returns>A ScrollViewer.</returns>
    public ScrollViewer GetScrollViewer()
    {
        return _scrollViewer;
    }

    /// <summary>
    /// Gets how the element is displayed, base on current screen DPI versus image DPI.
    /// </summary>
    /// <returns>The actual size * the scale of the element.</returns>
    public Size GetElementSize(bool noScalling = false)
    {
        if (_scrollViewer.Content is not FrameworkElement image)
            return new Size(Math.Max(ActualWidth, 0), Math.Max(ActualHeight, 0));

        var scaleX = noScalling ? 1 : _scaleTransform.ScaleX;
        var scaleY = noScalling ? 1 : _scaleTransform.ScaleY;

        return new Size(image.ActualWidth * scaleX, image.ActualHeight * scaleY);
    }

    /// <summary>
    /// Gets the actual image size.
    /// </summary>
    /// <returns>The actual image size.</returns>
    public Size GetImageSize()
    {
        if (_scrollViewer.Content is not FrameworkElement image)
            return new Size(Math.Max(ActualWidth, 0), Math.Max(ActualHeight, 0));

        //Ignore scale transform?
        return new Size(image.ActualWidth * ImageScale, image.ActualHeight * ImageScale);
    }

    public Size MeasureImageSizeAtZoom100(string path)
    {
        var image = path.SourceFrom();
        var imageScale = Math.Round(image.DpiX / 96d, 2);
        var scaleDiff = this.Scale() / imageScale;
        //var size = new Size(image.Width * imageScale, image.Height * imageScale);

        return new Size(image.Width * 1d / scaleDiff, image.Height * 1d / scaleDiff);
    }
}