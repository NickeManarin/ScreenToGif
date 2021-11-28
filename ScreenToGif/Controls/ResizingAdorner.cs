using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenToGif.Controls;

/// <summary>
/// The Resizing Adorner controls. https://social.msdn.microsoft.com/Forums/vstudio/en-US/274bc547-dadf-42b5-b3f1-6d29407f9e79/resize-adorner-scale-problem?forum=wpf
/// </summary>
public class ResizingAdorner : Adorner
{
    #region Variables

    /// <summary>
    /// Resizing adorner uses Thumbs for visual elements.  
    /// The Thumbs have built-in mouse input handling.
    /// </summary>
    private readonly Thumb _topLeft, _topRight, _bottomLeft, _bottomRight, _middleBottom, _middleTop, _leftMiddle, _rightMiddle;

    /// <summary>
    /// The dashed border.
    /// </summary>
    private Rectangle _rectangle;

    /// <summary>
    /// To store and manage the adorner's visual children.
    /// </summary>
    readonly VisualCollection _visualChildren;

    /// <summary>
    /// The current adorned element.
    /// </summary>
    private UIElement _adornedElement;

    /// <summary>
    /// The parent of the element.
    /// </summary>
    private readonly UIElement _parent;

    /// <summary>
    /// The latest position of the element. Used by the drag operation.
    /// </summary>
    private Point _lastestPosition;

    #endregion

    /// <summary>
    /// Initialize the ResizingAdorner.
    /// </summary>
    /// <param name="adornedElement">The element to be adorned.</param>
    /// <param name="isMovable">True if it's available the drag to move action.</param>
    /// <param name="parent">The parent of the element.</param>
    /// <param name="startPosition">The start position of the first click.</param>
    public ResizingAdorner(UIElement adornedElement, bool isMovable = true, UIElement parent = null, Point? startPosition = null)
        : base(adornedElement)
    {
        _visualChildren = new VisualCollection(this);

        #region Default values

        _adornedElement = adornedElement;
        _parent = parent ?? (_adornedElement as FrameworkElement)?.Parent as UIElement;

        if (startPosition.HasValue)
            _lastestPosition = startPosition.Value;

        #endregion

        //Creates the dashed rectangle around the adorned element.
        BuildAdornerBorder();

        //Call a helper method to initialize the Thumbs with a customized cursors.
        BuildAdornerCorner(ref _topLeft, Cursors.SizeNWSE);
        BuildAdornerCorner(ref _topRight, Cursors.SizeNESW);
        BuildAdornerCorner(ref _bottomLeft, Cursors.SizeNESW);
        BuildAdornerCorner(ref _bottomRight, Cursors.SizeNWSE);

        BuildAdornerCorner(ref _middleBottom, Cursors.SizeNS);
        BuildAdornerCorner(ref _middleTop, Cursors.SizeNS);
        BuildAdornerCorner(ref _leftMiddle, Cursors.SizeWE);
        BuildAdornerCorner(ref _rightMiddle, Cursors.SizeWE);

        //Drag to move.
        if (isMovable)
        {
            _adornedElement.PreviewMouseLeftButtonDown += AdornedElement_PreviewMouseLeftButtonDown;
            _adornedElement.MouseMove += AdornedElement_MouseMove;
            _adornedElement.MouseUp += AdornedElement_MouseUp;
        }

        //Add handlers for resizing • Corners
        _bottomLeft.DragDelta += HandleBottomLeft;
        _bottomRight.DragDelta += HandleBottomRight;
        _topLeft.DragDelta += HandleTopLeft;
        _topRight.DragDelta += HandleTopRight;

        //Add handlers for resizing • Sides
        _middleBottom.DragDelta += HandleBottomMiddle;
        _middleTop.DragDelta += HandleTopMiddle;
        _leftMiddle.DragDelta += HandleLeftMiddle;
        _rightMiddle.DragDelta += HandleRightMiddle;
    }

    private void AdornedElement_MouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        _adornedElement?.ReleaseMouseCapture();
    }

    private void AdornedElement_MouseMove(object sender, MouseEventArgs e)
    {
        if (_parent == null)
            return;

        if (_adornedElement is Image && e.LeftButton == MouseButtonState.Pressed)
        {
            _adornedElement.MouseMove -= AdornedElement_MouseMove;

            var currentPosition = e.GetPosition(_parent);

            Canvas.SetLeft(_adornedElement, Canvas.GetLeft(_adornedElement) + (currentPosition.X - _lastestPosition.X));
            Canvas.SetTop(_adornedElement, Canvas.GetTop(_adornedElement) + (currentPosition.Y - _lastestPosition.Y));

            _lastestPosition = currentPosition;

            _adornedElement.MouseMove += AdornedElement_MouseMove;
        }
    }

    private void AdornedElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_parent == null)
            return;

        if (_adornedElement is Image && _adornedElement.CaptureMouse())
            _lastestPosition = e.GetPosition(_parent);
    }

    #region DragDelta Event Handlers

    /// <summary>
    /// Handler for resizing from the bottom-right.
    /// </summary>
    private void HandleBottomRight(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = this.AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;
        var parentElement = adornedElement.Parent as FrameworkElement;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger 
        // than the width or height of an adorner, respectively.
        adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
        adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height, hitThumb.DesiredSize.Height);

        if (adornedElement.Width > parentElement.Width)
            parentElement.Width = adornedElement.Width;

        if (adornedElement.Height > parentElement.Height)
            parentElement.Height = adornedElement.Height;
    }

    /// <summary>
    ///  Handler for resizing from the top-right.
    /// </summary>
    private void HandleTopRight(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = this.AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;
        var parentElement = adornedElement.Parent as FrameworkElement;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger 
        // than the width or height of an adorner, respectively.
        adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
        //adornedElement.Height = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);

        var heightOld = adornedElement.Height;
        var heightNew = Math.Max(adornedElement.Height - args.VerticalChange, hitThumb.DesiredSize.Height);
        var topOld = Canvas.GetTop(adornedElement);
        adornedElement.Height = heightNew;

        Canvas.SetTop(adornedElement, topOld - (heightNew - heightOld));
    }

    /// <summary>
    ///  Handler for resizing from the top-left.
    /// </summary>
    private void HandleTopLeft(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        var zoomFactor = GetCanvasZoom(AdornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger 
        // than the width or height of an adorner, respectively.
        var widthOld = adornedElement.Width;
        var widthNew = Math.Max(adornedElement.Width - args.HorizontalChange / zoomFactor, hitThumb.DesiredSize.Width);
        var leftOld = Canvas.GetLeft(adornedElement);

        adornedElement.Width = widthNew;
        Canvas.SetLeft(adornedElement, leftOld - (widthNew - widthOld));

        var heightOld = adornedElement.Height;
        var heightNew = Math.Max(adornedElement.Height - args.VerticalChange / zoomFactor, hitThumb.DesiredSize.Height);
        var topOld = Canvas.GetTop(adornedElement);

        adornedElement.Height = heightNew;
        Canvas.SetTop(adornedElement, topOld - (heightNew - heightOld));
    }

    /// <summary>
    ///  Handler for resizing from the bottom-left.
    /// </summary>
    private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        var zoomFactor = GetCanvasZoom(AdornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger 
        // than the width or height of an adorner, respectively.
        //adornedElement.Width = Math.Max(adornedElement.Width - args.HorizontalChange, hitThumb.DesiredSize.Width);
        adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height/zoomFactor, hitThumb.DesiredSize.Height);

        var widthOld = adornedElement.Width;
        var widthNew = Math.Max(adornedElement.Width - args.HorizontalChange / zoomFactor, hitThumb.DesiredSize.Width);
        var leftOld = Canvas.GetLeft(adornedElement);

        adornedElement.Width = widthNew;
        Canvas.SetLeft(adornedElement, leftOld - (widthNew - widthOld));
    }

    /// <summary>
    /// Handler for resizing from the bottom-middle.
    /// </summary>
    private void HandleBottomMiddle(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = this.AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;
        var parentElement = adornedElement.Parent as FrameworkElement;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        var zoomFactor = GetCanvasZoom(AdornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger than the height of an adorner.
        adornedElement.Height = Math.Max(args.VerticalChange + adornedElement.Height / zoomFactor, hitThumb.DesiredSize.Height);

        if (adornedElement.Height > parentElement.Height)
            parentElement.Height = adornedElement.Height;
    }

    /// <summary>
    /// Handler for resizing from the top-middle.
    /// </summary>
    private void HandleTopMiddle(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = this.AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;
        var parentElement = adornedElement.Parent as FrameworkElement;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        var zoomFactor = GetCanvasZoom(AdornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger than the height of an adorner.
        var heightOld = adornedElement.Height;
        var heightNew = Math.Max(adornedElement.Height - args.VerticalChange / zoomFactor, hitThumb.DesiredSize.Height);
        var topOld = Canvas.GetTop(adornedElement);

        adornedElement.Height = heightNew;
        Canvas.SetTop(adornedElement, topOld - (heightNew - heightOld));
    }

    /// <summary>
    /// Handler for resizing from the left-middle.
    /// </summary>
    private void HandleLeftMiddle(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = this.AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;
        var parentElement = adornedElement.Parent as FrameworkElement;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        var zoomFactor = GetCanvasZoom(AdornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger than the height of an adorner.
        var widthOld = adornedElement.Width;
        var widthNew = Math.Max(adornedElement.Width - args.HorizontalChange / zoomFactor, hitThumb.DesiredSize.Width);
        var leftOld = Canvas.GetLeft(adornedElement);

        adornedElement.Width = widthNew;
        Canvas.SetLeft(adornedElement, leftOld - (widthNew - widthOld));
    }

    /// <summary>
    ///  Handler for resizing from the right-middle.
    /// </summary>
    private void HandleRightMiddle(object sender, DragDeltaEventArgs args)
    {
        var adornedElement = this.AdornedElement as FrameworkElement;
        var hitThumb = sender as Thumb;

        if (adornedElement == null || hitThumb == null) return;
        var parentElement = adornedElement.Parent as FrameworkElement;

        // Ensure that the Width and Height are properly initialized after the resize.
        EnforceSize(adornedElement);

        var zoomFactor = GetCanvasZoom(AdornedElement);

        // Change the size by the amount the user drags the mouse, as long as it's larger than the width of the adorner.
        adornedElement.Width = Math.Max(adornedElement.Width + args.HorizontalChange / zoomFactor, hitThumb.DesiredSize.Width);
    }

    #endregion

    #region Private Methods

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

        var zoomFactor = GetCanvasZoom(AdornedElement);

        _rectangle.Arrange(new Rect(0, 0, adornerWidth * zoomFactor, adornerHeight * zoomFactor));

        return finalSize;
    }

    /// <summary>
    /// Instantiates the corner Thumbs, setting the Cursor property, 
    /// some appearance properties, and add the elements to the visual tree.
    /// </summary>
    /// <param name="cornerThumb">The Thumb to Instantiate.</param>
    /// <param name="customizedCursor">The custom cursor.</param>
    private void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
    {
        if (cornerThumb != null) return;

        cornerThumb = new Thumb { Cursor = customizedCursor };
        cornerThumb.Height = cornerThumb.Width = 10;
        cornerThumb.Style = (Style)FindResource("ScrollBar.Thumb");

        _visualChildren.Add(cornerThumb);
    }

    /// <summary>
    /// Creates the dashed border around the adorned element.
    /// </summary>
    private void BuildAdornerBorder()
    {
        _rectangle = new Rectangle();
        _rectangle.StrokeDashArray.Add(5);
        _rectangle.Stroke = new SolidColorBrush(Color.FromRgb(171, 171, 171));
        _rectangle.StrokeThickness = 1;

        _visualChildren.Add(_rectangle);
    }

    // This method ensures that the Widths and Heights are initialized.  Sizing to content produces
    // Width and Height values of Double.NaN.  Because this Adorner explicitly resizes, the Width and Height
    // need to be set first.  It also sets the maximum size of the adorned element.
    private void EnforceSize(FrameworkElement adornedElement)
    {
        if (adornedElement.Width.Equals(Double.NaN))
            adornedElement.Width = adornedElement.DesiredSize.Width;
        if (adornedElement.Height.Equals(Double.NaN))
            adornedElement.Height = adornedElement.DesiredSize.Height;

        var parent = adornedElement.Parent as FrameworkElement;

        if (parent != null)
        {
            adornedElement.MaxHeight = parent.ActualHeight;
            adornedElement.MaxWidth = parent.ActualWidth;
        }
    }

    // Override the VisualChildrenCount and GetVisualChild properties to interface with 
    // the adorner's visual collection.
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

    #endregion

    private double GetCanvasZoom(Visual referenceVisual)
    {
        if (referenceVisual.GetType() == typeof(Canvas))
            return (referenceVisual as Canvas).LayoutTransform.Value.M11;

        var parent = VisualTreeHelper.GetParent(referenceVisual) as Visual;

        if (parent.GetType() == typeof(Canvas))
            return (parent as Canvas).LayoutTransform.Value.M11;

        return 1;
    }

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var zoomFactor = GetCanvasZoom(AdornedElement);

        _topLeft.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _topRight.RenderTransformOrigin = new Point(0.5, 0.5);
        _topRight.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _topRight.RenderTransformOrigin = new Point(0.5, 0.5);
        _bottomLeft.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _bottomLeft.RenderTransformOrigin = new Point(0.5, 0.5);
        _bottomRight.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _bottomRight.RenderTransformOrigin = new Point(0.5, 0.5);

        _middleBottom.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _middleBottom.RenderTransformOrigin = new Point(0.5, 0.5);
        _middleTop.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _middleTop.RenderTransformOrigin = new Point(0.5, 0.5);
        _rightMiddle.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _rightMiddle.RenderTransformOrigin = new Point(0.5, 0.5);
        _leftMiddle.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        _leftMiddle.RenderTransformOrigin = new Point(0.5, 0.5);
        _rectangle.RenderTransform = new ScaleTransform(1 / zoomFactor, 1 / zoomFactor);
        //_rectangle.RenderTransformOrigin = new Point(0.5, 0.5);

        return base.GetDesiredTransform(transform);
    }

    public void Destroy()
    {
        _adornedElement.PreviewMouseLeftButtonDown -= AdornedElement_PreviewMouseLeftButtonDown;
        _adornedElement.MouseMove -= AdornedElement_MouseMove;
        _adornedElement.MouseUp -= AdornedElement_MouseUp;

        _adornedElement = null;
    }
}