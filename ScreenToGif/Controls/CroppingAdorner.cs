using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Point = System.Drawing.Point;

namespace ScreenToGif.Controls;

public class CroppingAdorner : Adorner
{
    #region Private variables

    /// <summary>
    /// The size of the Thumb in pixels.
    /// </summary>
    private const double ThumbWidth = 10;

    /// <summary>
    /// Rectangle Shape, visual aid for the cropping selection.
    /// </summary>
    private readonly PuncturedRect _cropMask;

    /// <summary>
    /// Canvas that holds the Thumb collection.
    /// </summary>
    private readonly Canvas _thumbCanvas;

    /// <summary>
    /// Corner Thumbs used to change the crop selection.
    /// </summary>
    private readonly Thumb _thumbTopLeft, _thumbTopRight, _thumbBottomLeft,
        _thumbBottomRight, _thumbTop, _thumbLeft, _thumbBottom, _thumbRight, _thumbCenter;

    /// <summary>
    /// Stores and manages the adorner's visual children.
    /// </summary>
    private readonly VisualCollection _visualCollection;

    /// <summary>
    /// Screen DPI.
    /// </summary>
    private static readonly double DpiX, DpiY;

    #endregion

    #region Routed Events

    public static readonly RoutedEvent CropChangedEvent = EventManager.RegisterRoutedEvent(
        "CropChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CroppingAdorner));

    public event RoutedEventHandler CropChanged
    {
        add => AddHandler(CropChangedEvent, value);
        remove => RemoveHandler(CropChangedEvent, value);
    }

    #endregion

    #region Dependency Properties

    public static DependencyProperty FillProperty = Shape.FillProperty.AddOwner(typeof(CroppingAdorner),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(110, 0, 0, 0)), FillPropChanged));

    public static readonly DependencyProperty ClipRectangleProperty = DependencyProperty.Register("ClipRectangle", typeof(Rect), typeof(CroppingAdorner),
        new FrameworkPropertyMetadata(new Rect(0, 0, 0, 0), ClipRectanglePropertyChanged));

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Rect ClipRectangle
    {
        get => _cropMask.Interior;
        set => SetValue(ClipRectangleProperty, value);
    }

    private static void FillPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CroppingAdorner crp)
            crp._cropMask.Fill = (Brush) e.NewValue;
    }

    private static void ClipRectanglePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is CroppingAdorner crp))
            return;

        crp._cropMask.Interior = (Rect)e.NewValue;
        crp.SetThumbs(crp._cropMask.Interior);
        crp.RaiseEvent(new RoutedEventArgs(CropChangedEvent, crp));
    }

    #endregion

    #region Constructor

    static CroppingAdorner()
    {
        using (var g = System.Drawing.Graphics.FromHwnd((IntPtr)0))
        {
            DpiX = g.DpiX;
            DpiY = g.DpiY;
        }
    }

    public CroppingAdorner(UIElement adornedElement, Rect rcInit)
        : base(adornedElement)
    {
        _cropMask = new PuncturedRect
        {
            IsHitTestVisible = false,
            Interior = rcInit,
            Fill = Fill,
            Focusable = true
        };

        _thumbCanvas = new Canvas
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _visualCollection = new VisualCollection(this) { _cropMask, _thumbCanvas };

        BuildCorner(ref _thumbTop, Cursors.SizeNS);
        BuildCorner(ref _thumbBottom, Cursors.SizeNS);
        BuildCorner(ref _thumbLeft, Cursors.SizeWE);
        BuildCorner(ref _thumbRight, Cursors.SizeWE);
        BuildCorner(ref _thumbTopLeft, Cursors.SizeNWSE);
        BuildCorner(ref _thumbTopRight, Cursors.SizeNESW);
        BuildCorner(ref _thumbBottomLeft, Cursors.SizeNESW);
        BuildCorner(ref _thumbBottomRight, Cursors.SizeNWSE);
        BuildCenter(ref _thumbCenter);

        _cropMask.PreviewKeyDown += CropMask_PreviewKeyDown;

        //Cropping handlers.
        _thumbBottomLeft.DragDelta += HandleBottomLeft;
        _thumbBottomRight.DragDelta += HandleBottomRight;
        _thumbTopLeft.DragDelta += HandleTopLeft;
        _thumbTopRight.DragDelta += HandleTopRight;
        _thumbTop.DragDelta += HandleTop;
        _thumbBottom.DragDelta += HandleBottom;
        _thumbRight.DragDelta += HandleRight;
        _thumbLeft.DragDelta += HandleLeft;
        _thumbCenter.DragDelta += HandleCenter;

        //Clipping interior should be within the bounds of the adorned element.
        if (adornedElement is FrameworkElement element)
            element.SizeChanged += AdornedElement_SizeChanged;
    }

    private void CropMask_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        //Control + Shift: Expand both ways.
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            switch (e.Key)
            {
                case Key.Up:
                    HandleBottom(_thumbCenter, new DragDeltaEventArgs(0, 1));
                    HandleTop(_thumbCenter, new DragDeltaEventArgs(0, -1));
                    break;
                case Key.Down:
                    HandleBottom(_thumbCenter, new DragDeltaEventArgs(0, -1));
                    HandleTop(_thumbCenter, new DragDeltaEventArgs(0, 1));
                    break;
                case Key.Left:
                    HandleRight(_thumbCenter, new DragDeltaEventArgs(-1, 0));
                    HandleLeft(_thumbCenter, new DragDeltaEventArgs(1, 0));
                    break;
                case Key.Right:
                    HandleRight(_thumbCenter, new DragDeltaEventArgs(1, 0));
                    HandleLeft(_thumbCenter, new DragDeltaEventArgs(-1, 0));
                    break;
            }

            return;
        }

        //If the Shift key is pressed, the sizing mode is enabled (bottom right).
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            switch (e.Key)
            {
                case Key.Up:
                    HandleBottom(_thumbCenter, new DragDeltaEventArgs(0, -1));
                    break;
                case Key.Down:
                    HandleBottom(_thumbCenter, new DragDeltaEventArgs(0, 1));
                    break;
                case Key.Left:
                    HandleRight(_thumbCenter, new DragDeltaEventArgs(-1, 0));
                    break;
                case Key.Right:
                    HandleRight(_thumbCenter, new DragDeltaEventArgs(1, 0));
                    break;
            }

            return;
        }

        //If the Control key is pressed, the sizing mode is enabled (top left).
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            switch (e.Key)
            {
                case Key.Up:
                    HandleTop(_thumbCenter, new DragDeltaEventArgs(0, -1));
                    break;
                case Key.Down:
                    HandleTop(_thumbCenter, new DragDeltaEventArgs(0, 1));
                    break;
                case Key.Left:
                    HandleLeft(_thumbCenter, new DragDeltaEventArgs(-1, 0));
                    break;
                case Key.Right:
                    HandleLeft(_thumbCenter, new DragDeltaEventArgs(1, 0));
                    break;
            }

            return;
        }

        //If no other key is pressed, the movement mode is enabled.
        switch (e.Key)
        {
            case Key.Up:
                HandleCenter(_thumbCenter, new DragDeltaEventArgs(0, -1));
                break;
            case Key.Down:
                HandleCenter(_thumbCenter, new DragDeltaEventArgs(0, 1));
                break;
            case Key.Left:
                HandleCenter(_thumbCenter, new DragDeltaEventArgs(-1, 0));
                break;
            case Key.Right:
                HandleCenter(_thumbCenter, new DragDeltaEventArgs(1, 0));
                break;
        }
    }

    #endregion

    #region Thumb handlers

    private void HandleThumb(double drcL, double drcT, double drcW, double drcH, double dx, double dy)
    {
        var interior = _cropMask.Interior;

        if (interior.Width + drcW * dx < 0)
            dx = -interior.Width / drcW;

        if (interior.Height + drcH * dy < 0)
            dy = -interior.Height / drcH;

        interior = new Rect(interior.Left + drcL * dx,
            interior.Top + drcT * dy,
            interior.Width + drcW * dx,
            interior.Height + drcH * dy);

        //Minimum of 10x10.
        if (interior.Width < 10)
        {
            if (interior.X + interior.Width > _cropMask.Exterior.Width)
                interior.X = interior.Right - interior.Width;
            else
                interior.Width = 10;
        }

        if (interior.Height < 10)
        {
            if (interior.Y + interior.Height > _cropMask.Exterior.Height)
                interior.Y = interior.Bottom - interior.Height;
            else
                interior.Height = 10;
        }

        _cropMask.Interior = interior;

        SetThumbs(_cropMask.Interior);
        RaiseEvent(new RoutedEventArgs(CropChangedEvent, this));

        Keyboard.Focus(_cropMask);
    }

    //Cropping from the bottom-left.
    private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(1, 0, -1, 1, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the bottom-right.
    private void HandleBottomRight(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(0, 0, 1, 1, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the top-right.
    private void HandleTopRight(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(0, 1, 1, -1, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the top-left.
    private void HandleTopLeft(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(1, 1, -1, -1, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the top.
    private void HandleTop(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(0, 1, 0, -1, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the left.
    private void HandleLeft(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(1, 0, -1, 0, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the right.
    private void HandleRight(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(0, 0, 1, 0, args.HorizontalChange, args.VerticalChange);
    }

    //Cropping from the bottom.
    private void HandleBottom(object sender, DragDeltaEventArgs args)
    {
        if (sender is Thumb)
            HandleThumb(0, 0, 0, 1, args.HorizontalChange, args.VerticalChange);
    }

    //Dragging the cropping selection.
    private void HandleCenter(object sender, DragDeltaEventArgs args)
    {
        if (!(sender is Thumb))
            return;

        //Creates a new Rect based on the drag.
        var interior = new Rect(_cropMask.Interior.Left + args.HorizontalChange,
            _cropMask.Interior.Top + args.VerticalChange,
            _cropMask.Interior.Width, _cropMask.Interior.Height);

        #region Limit the drag to inside the bounds

        if (interior.Left < 0)
            interior.X = 0;

        if (interior.Top < 0)
            interior.Y = 0;

        if (interior.Right > _thumbCanvas.ActualWidth)
            interior.X = _thumbCanvas.ActualWidth - interior.Width;

        if (interior.Bottom > _thumbCanvas.ActualHeight)
            interior.Y = _thumbCanvas.ActualHeight - interior.Height;

        #endregion

        _cropMask.Interior = interior;

        SetThumbs(_cropMask.Interior);
        RaiseEvent(new RoutedEventArgs(CropChangedEvent, this));

        Keyboard.Focus(_cropMask);
    }

    #endregion

    #region Other handlers

    private void AdornedElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!(sender is FrameworkElement element))
            return;

        var wasChanged = false;

        double intLeft = ClipRectangle.Left, intTop = ClipRectangle.Top,
            intWidth = ClipRectangle.Width, intHeight = ClipRectangle.Height;

        if (ClipRectangle.Left > element.RenderSize.Width)
        {
            intLeft = element.RenderSize.Width;
            intWidth = 0;
            wasChanged = true;
        }

        if (ClipRectangle.Top > element.RenderSize.Height)
        {
            intTop = element.RenderSize.Height;
            intHeight = 0;
            wasChanged = true;
        }

        if (ClipRectangle.Right > element.RenderSize.Width)
        {
            intWidth = Math.Max(0, element.RenderSize.Width - intLeft);
            wasChanged = true;
        }

        if (ClipRectangle.Bottom > element.RenderSize.Height)
        {
            intHeight = Math.Max(0, element.RenderSize.Height - intTop);
            wasChanged = true;
        }

        if (wasChanged)
            ClipRectangle = new Rect(intLeft, intTop, intWidth, intHeight);
    }

    #endregion

    #region Arranging

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rcExterior = new Rect(0, 0, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);
        _cropMask.Exterior = rcExterior;

        var rcInterior = _cropMask.Interior;
        _cropMask.Arrange(rcExterior);

        SetThumbs(rcInterior);
        _thumbCanvas.Arrange(rcExterior);
        return finalSize;
    }

    #endregion

    #region Public Methods

    public BitmapSource CropImage()
    {
        var margin = AdornerMargin();
        var rcInterior = _cropMask.Interior;

        var pxFromSize = UnitsToPx(rcInterior.Width, rcInterior.Height);

        //CroppedBitmap indexes from the upper left of the margin whereas RenderTargetBitmap renders the
        //control exclusive of the margin.  Hence our need to take the margins into account here...

        var pxFromPos = UnitsToPx(rcInterior.Left + margin.Left, rcInterior.Top + margin.Top);
        var pxWhole = UnitsToPx(AdornedElement.RenderSize.Width + margin.Left, AdornedElement.RenderSize.Height + margin.Left);
        pxFromSize.X = Math.Max(Math.Min(pxWhole.X - pxFromPos.X, pxFromSize.X), 0);
        pxFromSize.Y = Math.Max(Math.Min(pxWhole.Y - pxFromPos.Y, pxFromSize.Y), 0);

        if (pxFromSize.X == 0 || pxFromSize.Y == 0)
            return null;

        var rcFrom = new Int32Rect(pxFromPos.X, pxFromPos.Y, pxFromSize.X, pxFromSize.Y);

        var rtb = new RenderTargetBitmap(pxWhole.X, pxWhole.Y, DpiX, DpiY, PixelFormats.Default);
        rtb.Render(AdornedElement);

        return new CroppedBitmap(rtb, rcFrom);
    }

    #endregion

    #region Private Methods

    private void SetThumbs(Rect rc)
    {
        SetPosition(_thumbBottomRight, rc.Right, rc.Bottom);
        SetPosition(_thumbTopLeft, rc.Left, rc.Top);
        SetPosition(_thumbTopRight, rc.Right, rc.Top);
        SetPosition(_thumbBottomLeft, rc.Left, rc.Bottom);
        SetPosition(_thumbTop, rc.Left + rc.Width / 2, rc.Top);
        SetPosition(_thumbBottom, rc.Left + rc.Width / 2, rc.Bottom);
        SetPosition(_thumbLeft, rc.Left, rc.Top + rc.Height / 2);
        SetPosition(_thumbRight, rc.Right, rc.Top + rc.Height / 2);

        //Central thumb, used to drag the whole cropping selection.
        SetPosition(_thumbCenter, rc.Left + 5, rc.Top + 5);
        _thumbCenter.Width = rc.Right - rc.Left;
        _thumbCenter.Height = rc.Bottom - rc.Top;
    }

    private Thickness AdornerMargin()
    {
        var thick = new Thickness(0);

        if (AdornedElement is FrameworkElement element)
            thick = element.Margin;

        return thick;
    }

    private void BuildCorner(ref Thumb thumb, Cursor cursor)
    {
        if (thumb != null) return;

        thumb = new Thumb
        {
            Cursor = cursor,
            Style = (Style)FindResource("ScrollBar.Thumb"),
            Width = ThumbWidth,
            Height = ThumbWidth
        };

        _thumbCanvas.Children.Add(thumb);
    }

    private void BuildCenter(ref Thumb thumb)
    {
        if (thumb != null) return;

        thumb = new Thumb
        {
            Style = (Style)FindResource("ThumbTranparent"),
        };

        _thumbCanvas.Children.Add(thumb);
    }

    private Point UnitsToPx(double x, double y)
    {
        return new Point((int)(x * DpiX / 96), (int)(y * DpiY / 96));
    }

    private void SetPosition(Thumb thumb, double x, double y)
    {
        Canvas.SetTop(thumb, y - ThumbWidth / 2);
        Canvas.SetLeft(thumb, x - ThumbWidth / 2);
    }

    #endregion

    #region Visual Tree Override

    // Override the VisualChildrenCount and GetVisualChild properties to interface with
    // the adorner's visual collection.
    protected override int VisualChildrenCount => _visualCollection.Count;

    protected override Visual GetVisualChild(int index)
    {
        return _visualCollection[index];
    }

    #endregion
}