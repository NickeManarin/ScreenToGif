#region Usings

#region Used Namespaces

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

#endregion

#region Used Aliases

using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

#endregion

#endregion

#nullable enable

namespace ScreenToGif.UserControls;

/// <summary>
/// Represents an image display control with zooming and panning.
/// Some solutions are ported from here (keyboard interaction is not implemented though): https://github.com/koszeggy/KGySoft.WinForms/blob/master/KGySoft.WinForms/Controls/ImageViewer.cs
/// </summary>
public partial class ImageViewer : UserControl
{
    #region Constants

    private const double _maxZoom = 16d;

    #endregion

    #region Fields

    #region Static Fields

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageViewer), new PropertyMetadata(default(ImageSource)));

    #endregion

    #region Instance Fields

    private ImageSource? _lastSource;
    private double _zoom = 1d;
    private Point? _zoomingOrigin;
    private Point? _draggingOrigin;

    #endregion

    #endregion

    #region Properties

    public ImageSource? Source
    {
        get => (ImageSource)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    #endregion

    #region Constructors

    public ImageViewer() => InitializeComponent();

    #endregion

    #region Methods

    #region Protected Methods

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == SourceProperty)
        {
            // Resetting auto size only if a new source is set, but keeping it if resetting the last value after a null Source
            if (e.NewValue is not null && !ReferenceEquals(_lastSource, e.NewValue))
                ResetAutoSize();
            _lastSource ??= e.NewValue as ImageSource;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets an auto value for zoom, which is:
    /// - 0, if Source is not set
    /// - 1, if the image fits in the boundaries
    /// - less than 1, if it has to be shrunk to fit in the bounds
    /// </summary>
    private double GetAutoZoom()
    {
        var image = Source;
        if (image == null)
            return 0d;

        var width = MaxWidth < Double.PositiveInfinity ? MaxWidth : ScrollContainer.ActualWidth;
        var height = MaxHeight < Double.PositiveInfinity ? MaxHeight : ScrollContainer.ActualHeight;
        var imageWidth = image.Width;
        var imageHeight = image.Height;
        double aspectRatio;
        double result;

        if (imageWidth <= width && imageHeight <= height)
            return 1;

        if (imageWidth > imageHeight)
        {
            aspectRatio = width / imageWidth;
            result = aspectRatio;
            if (height < imageHeight * result)
            {
                aspectRatio = height / imageHeight;
                result = aspectRatio;
            }

            return result;
        }

        aspectRatio = height / imageHeight;
        result = aspectRatio;
        if (width < imageWidth * result)
        {
            aspectRatio = width / imageWidth;
            result = aspectRatio;
        }

        return result;
    }

    private void ResetAutoSize()
    {
        var autoZoom = GetAutoZoom();
        if (autoZoom <= 0)
            return;

        SetZoom(autoZoom);
        ScrollContainer.UpdateLayout();
    }

    private void ApplyZoomChange(double delta)
    {
        if (delta.Equals(0d))
            return;
        delta += 1;
        SetZoom(Math.Clamp(_zoom * delta, GetAutoZoom(), _maxZoom));
    }

    private void SetZoom(double zoom)
    {
        var scale = new ScaleTransform(zoom, zoom);
        DisplayImage.LayoutTransform = scale;
        var quality = zoom switch
        {
            1d or >= 2d => BitmapScalingMode.NearestNeighbor, // intended, so magnified pixels are not blurred
            > 1d => BitmapScalingMode.Linear, // (1..2) - nearest neighbor is just too ugly in this range
            _ => BitmapScalingMode.HighQuality // < 1
        };
        RenderOptions.SetBitmapScalingMode(DisplayImage, quality);
        _zoom = zoom;
    }

    #endregion

    #region Event handlers

    private void ImageViewer_OnUnloaded(object sender, RoutedEventArgs e) => _lastSource = null;

    private void ScrollContainer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // If Control is pressed while scrolling, or the vertical scrollbar is visible, letting the event reach the display image
        if (Keyboard.Modifiers == ModifierKeys.Control || ScrollContainer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
            return;

        // Otherwise, forwarding the scroll to the self user control.
        // Without this, regular scrolling would be swallowed for the ImageViewer, while it works for other controls, which feels strange.
        e.Handled = true;
        var forwardedArgs = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = MouseWheelEvent };
        RaiseEvent(forwardedArgs);
    }

    private void DisplayImage_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Normal scrolling
        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;

        // Ctrl + MouseWheel: zooming
        var p = Mouse.GetPosition(DisplayImage);
        _zoomingOrigin = p;
        var delta = (double)e.Delta / SystemInformation.MouseWheelScrollDelta / 5d;
        ApplyZoomChange(delta);
    }

    private void ScrollContainer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_zoomingOrigin is null || Source is null || e is { ExtentHeightChange: 0, ExtentWidthChange: 0 })
            return;

        ScrollContainer.UpdateLayout();

        var origin = _zoomingOrigin;
        Point? pos = Mouse.GetPosition(DisplayImage);
        _zoomingOrigin = null;

        // making sure that zooming happens at the mouse cursor position
        var diff = new Vector(pos.Value.X - origin.Value.X, pos.Value.Y - origin.Value.Y);
        var scale = new Vector(e.ExtentWidth / DisplayImage.ActualWidth, e.ExtentHeight / DisplayImage.ActualHeight);
        var offset = new Vector(ScrollContainer.HorizontalOffset - (diff.X * scale.X), ScrollContainer.VerticalOffset - (diff.Y * scale.Y));

        if (Double.IsNaN(offset.X) || Double.IsNaN(offset.Y) || Double.IsInfinity(offset.X) || Double.IsInfinity(offset.Y))
            return;

        ScrollContainer.ScrollToHorizontalOffset(offset.X);
        ScrollContainer.ScrollToVerticalOffset(offset.Y);
    }

    private void ScrollContainer_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var mousePosition = e.GetPosition(ScrollContainer);
        if (mousePosition.X >= ScrollContainer.ViewportWidth || mousePosition.Y >= ScrollContainer.ViewportHeight)
            return;

        // start dragging
        ScrollContainer.Cursor = Cursors.SizeAll;
        _draggingOrigin = mousePosition;
        Mouse.Capture(ScrollContainer);
    }

    private void ScrollContainer_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingOrigin is null)
            return;

        // dragging
        var pos = e.GetPosition(ScrollContainer);
        var diff = new Vector(pos.X - _draggingOrigin.Value.X, pos.Y - _draggingOrigin.Value.Y);
        _draggingOrigin = pos;
        ScrollContainer.ScrollToHorizontalOffset(ScrollContainer.HorizontalOffset - diff.X);
        ScrollContainer.ScrollToVerticalOffset(ScrollContainer.VerticalOffset - diff.Y);
    }

    private void ScrollContainer_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingOrigin is null)
            return;

        // finish dragging
        ScrollContainer.Cursor = Cursors.Arrow;
        ScrollContainer.ReleaseMouseCapture();
        _draggingOrigin = null;
    }

    #endregion

    #endregion
}