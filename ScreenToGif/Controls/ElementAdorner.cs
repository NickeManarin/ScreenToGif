using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Domain.Events;

namespace ScreenToGif.Controls;

internal class ElementAdorner : Adorner
{
    #region Dependency properties

    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(ElementAdorner), new PropertyMetadata(0d, Angle_PropertyChanged));

    private static void Angle_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var adorner = d as ElementAdorner;

        if (adorner == null || !adorner.CanRotate)
            return;

        //adorner._adornedElement.RenderTransformOrigin = new Point(0.5, 0.5);
        adorner._adornedElement.RenderTransform = new RotateTransform(adorner.Angle);
        //adorner._rotationThumb.Angle = adorner.Angle;
    }

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    #endregion

    #region Variables and Properties

    private readonly VisualCollection _visualChildren;
    private readonly FrameworkElement _adornedElement;
    private readonly FrameworkElement _parent;

    /// <summary>
    /// The rectangle that surrounds the adorned element.
    /// </summary>
    private Rectangle _borderRectangle;

    /// <summary>
    /// Resizing adorner uses Thumbs for visual elements.
    /// The Thumbs have built-in mouse input handling.
    /// </summary>
    private readonly Thumb _topLeft, _topRight, _bottomLeft, _bottomRight, _middleBottom, _middleTop, _leftMiddle, _rightMiddle;

    /// <summary>
    /// The thumb that allows the rotation of the adorned element.
    /// </summary>
    private Thumb _rotationThumb;

    private Vector _startVector;
    private Point _centerPoint;
    private double _startAngle;

    /// <summary>
    /// The start point for the drag operation.
    /// </summary>
    internal Point StartPoint { get; set; }

    internal bool CanMove { get; set; }

    internal bool CanResize { get; set; }

    internal bool CanRotate { get; set; }

    #endregion

    public ElementAdorner(FrameworkElement adornedElement, bool canMove, bool canResize, bool canRotate, FrameworkElement parent, Point startPoint) : base(adornedElement)
    {
        #region Properties

        _visualChildren = new VisualCollection(this);
        _adornedElement = adornedElement;
        _parent = parent ?? _adornedElement?.Parent as FrameworkElement;
        StartPoint = startPoint;

        CanMove = canMove;
        CanResize = canResize;
        CanRotate = canRotate;

        #endregion

        #region Refresh size

        if (double.IsNaN(_adornedElement.Width))
        {
            _adornedElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _adornedElement.Arrange(new Rect(Canvas.GetLeft(_adornedElement), Canvas.GetTop(_adornedElement), _adornedElement.DesiredSize.Width, _adornedElement.DesiredSize.Height));
        }

        #endregion

        #region Build inner elements

        //Creates the dashed rectangle around the adorned element.
        BuildAdornerBorder();

        if (CanMove)
        {
            //Allows the drag operation to move the adorned object.
            _borderRectangle.PreviewMouseLeftButtonDown += AdornedElement_PreviewMouseLeftButtonDown;
            _borderRectangle.MouseMove += AdornedElement_MouseMove;
            _borderRectangle.MouseUp += AdornedElement_MouseUp;
        }

        if (CanResize)
        {
            //Call a helper method to initialize the Thumbs with a customized cursors.
            BuildAdornerThumb(ref _topLeft, Cursors.SizeNWSE);
            BuildAdornerThumb(ref _topRight, Cursors.SizeNESW);
            BuildAdornerThumb(ref _bottomLeft, Cursors.SizeNESW);
            BuildAdornerThumb(ref _bottomRight, Cursors.SizeNWSE);

            BuildAdornerThumb(ref _middleBottom, Cursors.SizeNS);
            BuildAdornerThumb(ref _middleTop, Cursors.SizeNS);
            BuildAdornerThumb(ref _leftMiddle, Cursors.SizeWE);
            BuildAdornerThumb(ref _rightMiddle, Cursors.SizeWE);

            //Add handlers for resizing • Corners
            _bottomLeft.DragDelta += HandleBottomLeft;
            _bottomRight.DragDelta += HandleBottomRight;
            _topLeft.DragDelta += HandleTopLeft;
            _topRight.DragDelta += HandleTopRight;

            //Add handlers for resizing • Sides
            _middleBottom.DragDelta += HandleBottom;
            _middleTop.DragDelta += HandleTop;
            _leftMiddle.DragDelta += HandleLeft;
            _rightMiddle.DragDelta += HandleRight;
        }

        if (CanRotate)
        {
            //Creates the thumb that allows the rotation of the adorned element.
            BuildAdornerRotator();
        }

        #endregion
    }

    #region Overrides

    /// <summary>
    ///  Arrange the Adorners.
    /// </summary>
    /// <param name="finalSize">The final Size</param>
    /// <returns>The final size</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
        // These will be used to place the ResizingAdorner at the corners of the adorned element.
        var desiredWidth = AdornedElement.DesiredSize.Width;
        var desiredHeight = AdornedElement.DesiredSize.Height;

        // adornerWidth & adornerHeight are used for placement as well.
        var adornerWidth = this.DesiredSize.Width;
        var adornerHeight = this.DesiredSize.Height;

        _topLeft.Arrange(new Rect(-adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
        _topRight.Arrange(new Rect(adornerWidth / 2, -adornerHeight / 2, adornerWidth, adornerHeight));
        _bottomLeft.Arrange(new Rect(-adornerWidth / 2, adornerHeight / 2, adornerWidth, adornerHeight));
        _bottomRight.Arrange(new Rect(adornerWidth / 2, adornerHeight / 2, adornerWidth, adornerHeight));

        _middleBottom.Arrange(new Rect(0, adornerHeight / 2, adornerWidth, adornerHeight));
        _middleTop.Arrange(new Rect(0, -adornerHeight / 2, adornerWidth, adornerHeight));
        _leftMiddle.Arrange(new Rect(-adornerWidth / 2, 0, adornerWidth, adornerHeight));
        _rightMiddle.Arrange(new Rect(adornerWidth / 2, 0, adornerWidth, adornerHeight));

        //var zoomFactor = GetCanvasZoom(AdornedElement);

        //_borderRectangle.Arrange(new Rect(0, 0, adornerWidth * zoomFactor, adornerHeight * zoomFactor));
        _borderRectangle.Arrange(new Rect(0, 0, adornerWidth, adornerHeight));

        _rotationThumb.Arrange(new Rect(0, (-adornerHeight / 2) - 15, adornerWidth, adornerHeight));

        return finalSize;
    }

    /// <summary>
    /// Override the VisualChildrenCount and GetVisualChild properties to interface with the adorner's visual collection.
    /// </summary>
    protected override int VisualChildrenCount => _visualChildren.Count;

    /// <summary>
    /// Gets the VisualChildren at given position.
    /// </summary>
    /// <param name="index">The Index to look for.</param>
    /// <returns>The VisualChildren</returns>
    protected override Visual GetVisualChild(int index)
    {
        return _visualChildren[index];
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        //Propagate the event to the parent control.
        var args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
        {
            RoutedEvent = e.RoutedEvent,
            Source = e.Source,
        };
        _parent.RaiseEvent(args);

        base.OnPreviewKeyDown(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        //Propagate the event to the parent control.
        var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = e.RoutedEvent,
            Source = this,
        };
        _parent.RaiseEvent(args);

        base.OnMouseWheel(e);
    }

    #endregion

    #region Events

    public static readonly RoutedEvent ManipulatedEvent = EventManager.RegisterRoutedEvent("Manipulated", RoutingStrategy.Bubble, typeof(ManipulatedEventHandler), typeof(ElementAdorner));

    public static readonly RoutedEvent RotationResetRequestedEvent = EventManager.RegisterRoutedEvent("RotationResetRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ElementAdorner));

    public static readonly RoutedEvent DuplicatedEvent = EventManager.RegisterRoutedEvent("Duplicated", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ElementAdorner));

    public static readonly RoutedEvent RemovedEvent = EventManager.RegisterRoutedEvent("Removed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ElementAdorner));

    public event ManipulatedEventHandler Manipulated
    {
        add => AddHandler(ManipulatedEvent, value);
        remove => RemoveHandler(ManipulatedEvent, value);
    }

    public event RoutedEventHandler RotationResetRequested
    {
        add => AddHandler(RotationResetRequestedEvent, value);
        remove => RemoveHandler(RotationResetRequestedEvent, value);
    }

    public event RoutedEventHandler Duplicated
    {
        add => AddHandler(DuplicatedEvent, value);
        remove => RemoveHandler(DuplicatedEvent, value);
    }

    public event RoutedEventHandler Removed
    {
        add => AddHandler(RemovedEvent, value);
        remove => RemoveHandler(RemovedEvent, value);
    }

    void RaiseManipulatedEvent(double angleDiff)
    {
        if (ManipulatedEvent == null)
            return;

        RaiseEvent(new ManipulatedEventArgs(ManipulatedEvent, angleDiff, 0, 0, 0, 0));
    }

    void RaiseManipulatedEvent(double widthDiff, double heightDiff, double topDiff = 0, double leftDiff = 0)
    {
        if (ManipulatedEvent == null || (Math.Abs(widthDiff) < 0.001 && Math.Abs(heightDiff) < 0.001 && Math.Abs(leftDiff) < 0.001 && Math.Abs(topDiff) < 0.001))
            return;

        RaiseEvent(new ManipulatedEventArgs(ManipulatedEvent, widthDiff, heightDiff, topDiff, leftDiff));
    }

    void RaiseRotationResetRequestedEvent()
    {
        if (RotationResetRequestedEvent == null)
            return;

        RaiseEvent(new RoutedEventArgs(RotationResetRequestedEvent));
    }

    void RaiseDuplicatedEvent()
    {
        if (DuplicatedEvent == null)
            return;

        RaiseEvent(new RoutedEventArgs(DuplicatedEvent));
    }

    void RaiseRemovedEvent()
    {
        if (RemovedEvent == null)
            return;

        RaiseEvent(new RoutedEventArgs(RemovedEvent));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Creates the dashed border around the adorned element.
    /// </summary>
    private void BuildAdornerBorder()
    {
        var resetMenu = new ExtendedMenuItem { Header = "Reset rotation", Icon = TryFindResource("Vector.Repeat") as Brush };
        resetMenu.SetResourceReference(HeaderedItemsControl.HeaderProperty, "S.Shapes.Shapes.ResetRotatio");

        //var duplicateMenu = new ImageMenuItem { Header = "Duplicate", Image = TryFindResource("Vector.Copy") as Canvas };

        var removeMenu = new ExtendedMenuItem { Header = "Remove", Icon = TryFindResource("Vector.Cancel") as Brush };
        removeMenu.SetResourceReference(HeaderedItemsControl.HeaderProperty, "S.Shapes.Shapes.Remove");

        resetMenu.Click += (sender, args) => RaiseRotationResetRequestedEvent();
        //duplicateMenu.Click += (sender, args) => RaiseDuplicatedEvent();
        removeMenu.Click += (sender, args) => RaiseRemovedEvent();

        _borderRectangle = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(171, 171, 171)),
            StrokeThickness = 1,
            Fill = Brushes.Transparent,
            StrokeDashArray = new DoubleCollection { 5 },
            Cursor = Cursors.SizeAll,
            UseLayoutRounding = true,
            SnapsToDevicePixels = true,
            ContextMenu = new ContextMenu { Items = { resetMenu, removeMenu } }
        };

        _visualChildren.Add(_borderRectangle);
    }

    /// <summary>
    /// Instantiates the corner Thumbs, setting the Cursor property, some appearance properties, and add the elements to the visual tree.
    /// </summary>
    /// <param name="thumb">The Thumb to Instantiate.</param>
    /// <param name="cursor">The custom cursor.</param>
    private void BuildAdornerThumb(ref Thumb thumb, Cursor cursor)
    {
        if (thumb != null) return;

        thumb = new Thumb
        {
            Cursor = cursor,
            Height = 10,
            Width = 10,
            Style = (Style)FindResource("ScrollBar.Thumb"),
        };

        _visualChildren.Add(thumb);
    }

    /// <summary>
    /// Creates the element that allows the adorned element to be rotated.
    /// </summary>
    private void BuildAdornerRotator()
    {
        _rotationThumb = new Thumb
        {
            Height = 10,
            Width = 10,
            Cursor = Cursors.SizeAll,
            Style = FindResource("Style.Thumb.Ellipse") as Style
        };

        _rotationThumb.DragStarted += RotationThumb_DragStarted;
        _rotationThumb.DragDelta += RotationThumb_DragDelta;

        _adornedElement.RenderTransformOrigin = new Point(0.5, 0.5);

        if (_adornedElement.RenderTransform is RotateTransform transform)
            Angle = transform.Angle;

        _visualChildren.Add(_rotationThumb);
    }

    private void AfterManipulation()
    {
        InvalidateVisual();
        UpdateLayout();
    }

    #endregion

    #region Events

    private void AdornedElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_parent == null)
            return;

        if (_borderRectangle.CaptureMouse())
            StartPoint = e.GetPosition(_parent);
    }

    private void AdornedElement_MouseMove(object sender, MouseEventArgs e)
    {
        if (_parent == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        _borderRectangle.MouseMove -= AdornedElement_MouseMove;

        var currentPosition = e.GetPosition(_parent);

        var x = Canvas.GetLeft(_adornedElement) + (currentPosition.X - StartPoint.X);
        var y = Canvas.GetTop(_adornedElement) + (currentPosition.Y - StartPoint.Y);

        if (x < -1)
            x = -1;

        if (y < -1)
            y = -1;

        if (x + _adornedElement.DesiredSize.Width > _parent.ActualWidth + 1)
            x = _parent.ActualWidth + 1 - _adornedElement.DesiredSize.Width;

        if (y + _adornedElement.DesiredSize.Height > _parent.ActualHeight + 1)
            y = _parent.ActualHeight + 1 - _adornedElement.DesiredSize.Height;

        RaiseManipulatedEvent(0, 0, y - Canvas.GetTop(_adornedElement), x - Canvas.GetLeft(_adornedElement));

        Canvas.SetLeft(_adornedElement, x);
        Canvas.SetTop(_adornedElement, y);

        StartPoint = currentPosition;
        e.Handled = true;

        _borderRectangle.MouseMove += AdornedElement_MouseMove;

        AfterManipulation();
    }

    private void AdornedElement_MouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        _borderRectangle?.ReleaseMouseCapture();
    }

    ///<summary>
    ///Handler for resizing from the top-left.
    ///</summary>
    private void HandleTopLeft(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(_adornedElement.DesiredSize.Width - e.HorizontalChange, 10);
        var left = Canvas.GetLeft(_adornedElement) - (width - _adornedElement.DesiredSize.Width);
        var height = Math.Max(_adornedElement.DesiredSize.Height - e.VerticalChange, 10);
        var top = Canvas.GetTop(_adornedElement) - (height - _adornedElement.DesiredSize.Height);

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

        RaiseManipulatedEvent(width - _adornedElement.Width, height - _adornedElement.Height, top - Canvas.GetTop(_adornedElement), left - Canvas.GetLeft(_adornedElement));

        Canvas.SetLeft(_adornedElement, left);
        Canvas.SetTop(_adornedElement, top);
        _adornedElement.Height = height;
        _adornedElement.Width = width;

        //TODO: Maybe trap mouse while dragging with ClipCursor(ref r);

        AfterManipulation();
    }

    /// <summary>
    ///  Handler for resizing from the top-right.
    /// </summary>
    private void HandleTopRight(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(_adornedElement.DesiredSize.Width + e.HorizontalChange, 10);
        var height = Math.Max(_adornedElement.DesiredSize.Height - e.VerticalChange, 10);
        var top = Canvas.GetTop(_adornedElement) - (height - _adornedElement.DesiredSize.Height);
        var left = Canvas.GetLeft(_adornedElement);

        if (top < 0)
        {
            height -= top * -1;
            top = 0;
        }

        if (left + width > _parent.ActualWidth)
            width = _parent.ActualWidth - left;

        RaiseManipulatedEvent(width - _adornedElement.Width, height - _adornedElement.Height, top - Canvas.GetTop(_adornedElement));

        Canvas.SetTop(_adornedElement, top);
        _adornedElement.Height = height;
        _adornedElement.Width = width;

        AfterManipulation();
    }

    /// <summary>
    ///  Handler for resizing from the bottom-left.
    /// </summary>
    private void HandleBottomLeft(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(_adornedElement.DesiredSize.Width - e.HorizontalChange, 10);
        var left = Canvas.GetLeft(_adornedElement) - (width - _adornedElement.DesiredSize.Width);
        var height = Math.Max(_adornedElement.DesiredSize.Height + e.VerticalChange, 10);

        if (left < 0)
        {
            width -= left * -1;
            left = 0;
        }

        if (Canvas.GetLeft(_adornedElement) + width > _parent.ActualWidth)
            width = _parent.ActualWidth - Canvas.GetLeft(_adornedElement);

        if (Canvas.GetTop(_adornedElement) + height > _parent.ActualHeight)
            height = _parent.ActualHeight - Canvas.GetTop(_adornedElement);

        RaiseManipulatedEvent(width - _adornedElement.Width, height - _adornedElement.Height, 0, left - Canvas.GetLeft(_adornedElement));

        Canvas.SetLeft(_adornedElement, left);
        _adornedElement.Height = height;
        _adornedElement.Width = width;

        AfterManipulation();
    }

    /// <summary>
    /// Handler for resizing from the bottom-right.
    /// </summary>
    private void HandleBottomRight(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(_adornedElement.DesiredSize.Width + e.HorizontalChange, 10);
        var height = Math.Max(_adornedElement.DesiredSize.Height + e.VerticalChange, 10);
        var top = Canvas.GetTop(_adornedElement);
        var left = Canvas.GetLeft(_adornedElement);

        if (left + width > _parent.ActualWidth)
            width = _parent.ActualWidth - left;

        if (top + height > _parent.ActualHeight)
            height = _parent.ActualHeight - top;

        RaiseManipulatedEvent(width - _adornedElement.Width, height - _adornedElement.Height);

        _adornedElement.Height = height;
        _adornedElement.Width = width;

        AfterManipulation();
    }

    /// <summary>
    /// Handler for resizing from the left-middle.
    /// </summary>
    private void HandleLeft(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(_adornedElement.DesiredSize.Width - e.HorizontalChange, 10);
        var left = Canvas.GetLeft(_adornedElement) - (width - _adornedElement.DesiredSize.Width);

        if (left < 0)
        {
            width -= left * -1;
            left = 0;
        }

        RaiseManipulatedEvent(width - _adornedElement.Width, 0, 0, left - Canvas.GetLeft(_adornedElement));

        Canvas.SetLeft(_adornedElement, left);
        _adornedElement.Width = width;

        AfterManipulation();
    }

    /// <summary>
    /// Handler for resizing from the top-middle.
    /// </summary>
    private void HandleTop(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var height = Math.Max(_adornedElement.DesiredSize.Height - e.VerticalChange, 10);
        var top = Canvas.GetTop(_adornedElement) - (height - _adornedElement.DesiredSize.Height);

        if (top < 0)
        {
            height -= top * -1;
            top = 0;
        }

        RaiseManipulatedEvent(0, height - _adornedElement.Height, top - Canvas.GetTop(_adornedElement));

        Canvas.SetTop(_adornedElement, top);
        _adornedElement.Height = height;

        AfterManipulation();
    }

    /// <summary>
    ///  Handler for resizing from the right-middle.
    /// </summary>
    private void HandleRight(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var width = Math.Max(_adornedElement.DesiredSize.Width + e.HorizontalChange, 10);

        if (Canvas.GetLeft(_adornedElement) + width > _parent.ActualWidth)
            width = _parent.ActualWidth - Canvas.GetLeft(_adornedElement);

        RaiseManipulatedEvent(width - _adornedElement.Width, 0);

        _adornedElement.Width = width;

        AfterManipulation();
    }

    /// <summary>
    /// Handler for resizing from the bottom-middle.
    /// </summary>
    private void HandleBottom(object sender, DragDeltaEventArgs e)
    {
        if (!(sender is Thumb)) return;

        e.Handled = true;

        //Change the size by the amount the user drags the cursor.
        var height = Math.Max(_adornedElement.DesiredSize.Height + e.VerticalChange, 10);

        if (Canvas.GetTop(_adornedElement) + height > _parent.ActualHeight)
            height = _parent.ActualHeight - Canvas.GetTop(_adornedElement);

        RaiseManipulatedEvent(0, height - _adornedElement.Height);

        _adornedElement.Height = height;

        AfterManipulation();
    }

    /// <summary>
    /// Handler for the start of the drag operation of the thumb that allows the rotation of the shape.
    /// </summary>
    private void RotationThumb_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (_adornedElement == null || _parent == null)
            return;

        _centerPoint = _adornedElement.TranslatePoint(new Point(_adornedElement.ActualWidth * _adornedElement.RenderTransformOrigin.X, _adornedElement.ActualHeight * _adornedElement.RenderTransformOrigin.Y), _parent);
        _startVector = Point.Subtract(Mouse.GetPosition(_parent), _centerPoint);

        _startAngle = Angle;
    }

    /// <summary>
    /// Handler for the drag operation of the thumb that allows the rotation of the shape.
    /// </summary>
    private void RotationThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_adornedElement == null || _parent == null)
            return;

        var deltaVector = Point.Subtract(Mouse.GetPosition(_parent), _centerPoint);
        var angle = Vector.AngleBetween(_startVector, deltaVector);
        var newAngle = _startAngle + Math.Round(angle, 0);

        RaiseManipulatedEvent(newAngle - Angle);

        Angle = newAngle;

        _adornedElement.InvalidateMeasure();
    }

    #endregion
}