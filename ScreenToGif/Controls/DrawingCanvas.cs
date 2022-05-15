using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenToGif.Controls.Shapes;
using ScreenToGif.Domain.Events;

namespace ScreenToGif.Controls;

internal class DrawingCanvas : Control
{
    internal enum DrawingModes
    {
        None = 0,
        Ink,
        Select,
        EraseByPoint,
        EraseByObject,
        Shape
    }

    internal enum Shapes
    {
        Rectangle,
        Ellipse,
        Triangle,
        Arrow,
        Line,
    }


    #region Variables

    private Canvas _mainCanvas;
    private InkCanvas _mainInkCanvas;

    private AdornerLayer _adornerLayer;

    /// <summary>
    /// The start point for the drag operation.
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// The list of currently selected shapes. All selected shapes will have their own element adorner.
    /// </summary>
    private readonly List<Shape> _selectedShapes = new List<Shape>();

    /// <summary>
    /// The current shape being drawn.
    /// </summary>
    private Shape _currentShape;

    /// <summary>
    /// The most distant point within the shape's boundary for the resize operation.
    /// </summary>
    private Point _mostDistantPoint;

    /// <summary>
    /// The less distant point (current point) within the shape's boundary for the resize operation.
    /// </summary>
    private Point _currentPoint;

    /// <summary>
    /// Horizontal orientation of the resize operation.
    /// </summary>
    private bool _isRightToLeft;

    /// <summary>
    /// Vertical orientation of the resize operation.
    /// </summary>
    private bool _isBottomToTop;

    #endregion

    #region Dependency properties

    internal static readonly DependencyProperty DrawingModeProperty = DependencyProperty.Register(nameof(DrawingMode), typeof(DrawingModes), typeof(DrawingCanvas), new PropertyMetadata(default(DrawingModes), DrawingMode_PropertyChanged));

    internal static readonly DependencyProperty CurrentShapeProperty = DependencyProperty.Register(nameof(CurrentShape), typeof(Shapes), typeof(DrawingCanvas), new PropertyMetadata(default(Shapes)));

    internal static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(nameof(Selection), typeof(Rect), typeof(DrawingCanvas), new PropertyMetadata(default(Rect)));

    internal static readonly DependencyProperty RenderRegionProperty = DependencyProperty.Register(nameof(RenderRegion), typeof(Rect), typeof(DrawingCanvas), new PropertyMetadata(default(Rect)));

    internal static readonly DependencyProperty IsDrawingProperty = DependencyProperty.Register(nameof(IsDrawing), typeof(bool), typeof(DrawingCanvas), new PropertyMetadata(false));

    internal static readonly DependencyProperty ControlsZIndexProperty = DependencyProperty.Register(nameof(ControlsZIndex), typeof(long), typeof(DrawingCanvas), new PropertyMetadata(1L));


    internal static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(DrawingCanvas), new PropertyMetadata(2d, Visual_PropertyChanged));

    internal static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(DrawingCanvas), new PropertyMetadata(Brushes.Black, Visual_PropertyChanged));

    internal static readonly DependencyProperty FillProperty = DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(DrawingCanvas), new PropertyMetadata(Brushes.Transparent, Visual_PropertyChanged));

    internal static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(nameof(Radius), typeof(double), typeof(DrawingCanvas),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, Visual_PropertyChanged));

    public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register(nameof(StrokeDashArray), typeof(DoubleCollection), typeof(DrawingCanvas),
        new FrameworkPropertyMetadata(new DoubleCollection(), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, Visual_PropertyChanged));

    #endregion

    #region Properties

    internal DrawingModes DrawingMode
    {
        get => (DrawingModes)GetValue(DrawingModeProperty);
        set => SetValue(DrawingModeProperty, value);
    }

    internal Shapes CurrentShape
    {
        get => (Shapes)GetValue(CurrentShapeProperty);
        set => SetValue(CurrentShapeProperty, value);
    }

    internal Rect Selection
    {
        get => (Rect)GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    internal Rect RenderRegion
    {
        get => (Rect)GetValue(RenderRegionProperty);
        set => SetValue(RenderRegionProperty, value);
    }

    internal bool IsDrawing
    {
        get => (bool)GetValue(IsDrawingProperty);
        set => SetValue(IsDrawingProperty, value);
    }

    internal long ControlsZIndex
    {
        get => (long)GetValue(ControlsZIndexProperty);
        set => SetValue(ControlsZIndexProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    [TypeConverter(typeof(LengthConverter))]
    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public DoubleCollection StrokeDashArray
    {
        get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    public int ShapesCount => _mainCanvas?.Children.Count ?? 0;

    #endregion

    static DrawingCanvas()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingCanvas), new FrameworkPropertyMetadata(typeof(DrawingCanvas)));
    }

    #region Overrides

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _mainCanvas = Template.FindName("MainCanvas", this) as Canvas;
        _mainInkCanvas = Template.FindName("MainInkCanvas", this) as InkCanvas;

        if (_mainInkCanvas != null)
        {
            _mainInkCanvas.PreviewMouseLeftButtonDown += MainInkCanvas_MouseLeftButtonDown;
            _mainInkCanvas.StrokeCollected += MainInkCanvas_StrokeCollected;
        }

        _adornerLayer = AdornerLayer.GetAdornerLayer(this);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Keyboard.Focus(this);

        _startPoint = e.GetPosition(this);

        switch (DrawingMode)
        {
            case DrawingModes.Select:
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                {
                    RemoveAllAdorners();
                    _selectedShapes.Clear();
                }

                //When the user clicks exactly on top of a shape, it will be selected.
                var hitTest = _mainCanvas.Children.OfType<Shape>().Where(w => w.Tag == null).FirstOrDefault(f => f.RenderedGeometry.FillContains(e.GetPosition(f)));

                if (hitTest != null)
                {
                    SelectShape(hitTest);
                }
                else
                {
                    //Starts drawing selection retangle.
                    Selection = new Rect(_startPoint, new Size(0, 0));

                    CaptureMouse();
                }

                break;
            }
            case DrawingModes.Shape:
            {
                RemoveAllAdorners();

                RenderRegion = new Rect(_startPoint, new Size(0, 0));
                IsDrawing = true;

                CaptureMouse();

                CalculateOrientation(_startPoint, _startPoint);
                RenderShape();

                break;
            }
        }

        e.Handled = true;
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed)
            return;

        if (DrawingMode == DrawingModes.Select && ((_selectedShapes?.Count ?? 0) == 0 || (Keyboard.Modifiers & ModifierKeys.Control) != 0))
        {
            var current = GetBoundedCoordinates(e);

            Selection = new Rect(Math.Min(current.X, _startPoint.X), Math.Min(current.Y, _startPoint.Y), Math.Abs(current.X - _startPoint.X), Math.Abs(current.Y - _startPoint.Y));
        }
        else if (DrawingMode == DrawingModes.Shape)
        {
            var current = GetBoundedCoordinates(e);

            RenderRegion = Rect.Inflate(new Rect(Math.Min(current.X, _startPoint.X), Math.Min(current.Y, _startPoint.Y), Math.Abs(current.X - _startPoint.X), Math.Abs(current.Y - _startPoint.Y)), -0.6d, -0.6d);

            CalculateOrientation(_startPoint, current);
            RenderShape();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (DrawingMode == DrawingModes.Select)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                RemoveAllAdorners();
                _selectedShapes.Clear();
            }

            var selectedShapes = GetSelectedShapes(_mainCanvas, new RectangleGeometry(Selection)); //_mainCanvas.Children.OfType<Shape>().Where(w => Selection.Contains(w.)).ToList();

            if (selectedShapes.Any())
            {
                foreach (var shape in selectedShapes)
                    SelectShape(shape);
            }

            Selection = Rect.Empty;
            ReleaseMouseCapture();
        }
        else if (DrawingMode == DrawingModes.Shape)
        {
            ReleaseMouseCapture();

            RenderShape();
            RemoveIfTooSmall();

            IsDrawing = false;

            _selectedShapes?.Clear();
            SelectShape(_currentShape);

            _currentShape = null;
        }

        base.OnMouseLeftButtonUp(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Back:
            case Key.Delete:
                RemoveAllAdorners();
                RemoveAllSelectedShapes();

                if (_selectedShapes.Count > 0)
                    e.Handled = true;
                break;

            //TODO: Cntrl + C, Ctrl + V,
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        double step;

        switch (Keyboard.Modifiers)
        {
            case ModifierKeys.Alt:
                step = 90;
                break;
            case ModifierKeys.Control:
                step = 1;
                break;
            case ModifierKeys.Shift:
                step = 20;
                break;
            default:
                return;
        }

        RotateAllSelectedShapes(e.Delta > 0 ? step : step * -1);

        base.OnMouseWheel(e);
    }

    #endregion

    #region Methods

    private Point GetBoundedCoordinates(MouseEventArgs e)
    {
        var current = e.GetPosition(this);

        if (current.X < -1)
            current.X = -1;

        if (current.Y < -1)
            current.Y = -1;

        if (current.X > ActualWidth)
            current.X = ActualWidth;

        if (current.Y > ActualHeight)
            current.Y = ActualHeight;

        return current;
    }

    private void RemoveAllAdorners()
    {
        if (_selectedShapes == null)
            return;

        foreach (var shape in _selectedShapes.Where(w => w != null))
        {
            foreach (var adorner in _adornerLayer?.GetAdorners(shape)?.OfType<ElementAdorner>() ?? new List<ElementAdorner>())
                _adornerLayer?.Remove(adorner);
        }
    }

    private void RemoveAllSelectedShapes()
    {
        if (_selectedShapes == null)
            return;

        foreach (var shape in _selectedShapes)
            _mainCanvas.Children.Remove(shape);

        _selectedShapes.Clear();
    }

    private void RotateAllSelectedShapes(double angleDifference)
    {
        if (_selectedShapes == null)
            return;

        foreach (var shape in _selectedShapes)
            if (_adornerLayer.GetAdorners(shape)?[0] is ElementAdorner ad)
                ad.Angle += angleDifference;
    }

    private void CalculateOrientation(Point start, Point current)
    {
        _isBottomToTop = start.Y < current.Y;
        _isRightToLeft = start.X < current.X;
        _mostDistantPoint = start;
        _currentPoint = current;
    }

    private void RenderShape()
    {
        if (RenderRegion.IsEmpty)
        {
            if (_currentShape != null)
                _mainCanvas.Children.Remove(_currentShape);

            return;
        }

        if (_currentShape != null)
        {
            Canvas.SetTop(_currentShape, RenderRegion.Top);
            Canvas.SetLeft(_currentShape, RenderRegion.Left);
            _currentShape.Width = RenderRegion.Width;
            _currentShape.Height = RenderRegion.Height;

            if (_currentShape is Arrow arrow)
            {
                arrow.X1 = RenderRegion.Left - _mostDistantPoint.X;
                arrow.X2 = RenderRegion.Left - Math.Abs(_isRightToLeft ? _mostDistantPoint.X - _currentPoint.X : _currentPoint.X - _mostDistantPoint.X);
                arrow.Y1 = RenderRegion.Top - _mostDistantPoint.Y;
                arrow.Y2 = RenderRegion.Top - Math.Abs(_mostDistantPoint.Y - _currentPoint.Y);
            }
            return;
        }

        switch (CurrentShape)
        {
            case Shapes.Rectangle:
                _currentShape = new Rectangle
                {
                    Width = RenderRegion.Width,
                    Height = RenderRegion.Height,
                    Stroke = Stroke,
                    StrokeThickness = StrokeThickness,
                    StrokeDashArray = StrokeDashArray,
                    Fill = Fill,
                    RadiusX = Radius,
                    RadiusY = Radius
                };
                break;
            case Shapes.Ellipse:
                _currentShape = new Ellipse
                {
                    Width = RenderRegion.Width,
                    Height = RenderRegion.Height,
                    Stroke = Stroke,
                    StrokeThickness = StrokeThickness,
                    StrokeDashArray = StrokeDashArray,
                    Fill = Fill,
                };
                break;
            case Shapes.Triangle:
                _currentShape = new Triangle
                {
                    Width = RenderRegion.Width,
                    Height = RenderRegion.Height,
                    Stroke = Stroke,
                    StrokeThickness = StrokeThickness,
                    StrokeDashArray = StrokeDashArray,
                    Fill = Fill,
                    //RadiusX = Radius,
                    //RadiusY = Radius
                };
                break;
            case Shapes.Arrow:
                _currentShape = new Arrow
                {
                    Width = RenderRegion.Width,
                    Height = RenderRegion.Height,
                    Stroke = Stroke,
                    StrokeThickness = StrokeThickness,
                    StrokeDashArray = StrokeDashArray,
                    Fill = Fill,
                    Stretch = Stretch.Fill,
                    HeadHeight = 10,
                    HeadWidth = 10,
                    X1 = RenderRegion.Left - _mostDistantPoint.X,
                    X2 = RenderRegion.Left - Math.Abs(_isRightToLeft ? _mostDistantPoint.X - _currentPoint.X : _currentPoint.X - _mostDistantPoint.X),
                    Y1 = RenderRegion.Top - _mostDistantPoint.Y,
                    Y2 = RenderRegion.Top - Math.Abs(_mostDistantPoint.Y - _currentPoint.Y)
                };
                break;
        }

        if (_currentShape == null)
            return;

        _mainCanvas.Children.Add(_currentShape);

        Canvas.SetLeft(_currentShape, RenderRegion.Left);
        Canvas.SetTop(_currentShape, RenderRegion.Top);
        Panel.SetZIndex(_currentShape, _mainCanvas.Children.OfType<Shape>().Where(w => w.Tag == null).Max(Panel.GetZIndex) + 1);
    }

    private void RemoveIfTooSmall()
    {
        if (!(RenderRegion.Width + RenderRegion.Height < 10))
            return;

        _mainCanvas.Children.Remove(_currentShape);
    }

    private List<Shape> GetSelectedShapes(Visual element, Geometry geometry)
    {
        var shapes = new List<Shape>();

        VisualTreeHelper.HitTest(element, null, result =>
            {
                if (result.VisualHit is Shape shape && shape.Tag == null)
                    shapes.Add(shape);

                return HitTestResultBehavior.Continue;
            },
            new GeometryHitTestParameters(geometry));

        return shapes;
    }

    private void SelectShape(Shape shape)
    {
        if (shape == null)
            return;

        if (!_selectedShapes.Contains(shape))
            _selectedShapes.Add(shape);

        AdjustDepth();

        var adorner = new ElementAdorner(shape, true, true, true, _mainCanvas, _startPoint);
        adorner.Manipulated += Adorner_Manipulated;
        adorner.RotationResetRequested += Adorner_RotationResetRequested;
        adorner.Removed += Adorner_Removed;
        adorner.MouseLeftButtonDown += Adorner_MouseLeftButtonDown;
        _adornerLayer.Add(adorner);
    }

    private void DeselectShape(Shape shape)
    {
        if (shape == null)
            return;

        if (!_selectedShapes.Contains(shape))
            return;

        _selectedShapes.Remove(shape);

        foreach (var adorner in _adornerLayer?.GetAdorners(shape)?.OfType<ElementAdorner>() ?? new List<ElementAdorner>())
            _adornerLayer?.Remove(adorner);
    }

    private void AdjustDepth()
    {
        //0 = Further behind.
        //999 = Further in front.
        var indexes = _mainCanvas.Children.OfType<Shape>().Where(w => w.Tag == null).Select(Panel.GetZIndex).OrderBy(o => o).ToList();

        if (indexes.Count == 0)
            return;

        //Make all shapes go 1 step behind.
        foreach (var shape in _mainCanvas.Children.OfType<Shape>().Where(w => w.Tag == null))
            Panel.SetZIndex(shape, indexes.IndexOf(Panel.GetZIndex(shape)));

        //In order to show the selected shapes in front, the Z order should be greater than the rest of the shapes.
        var max = _mainCanvas.Children.OfType<Shape>().Where(w => w.Tag == null).Max(Panel.GetZIndex);

        //Make all selected shapes go 1 step to the front, making sure to respect the current Z order.
        foreach (var shape in _selectedShapes.OrderBy(Panel.GetZIndex))
            Panel.SetZIndex(shape, ++max);

        //All design controls should be at the top.
        ControlsZIndex = ++max;
    }

    public void DeselectAll()
    {
        RemoveAllAdorners();

        _selectedShapes?.Clear();
    }

    public void RemoveAllShapes()
    {
        DeselectAll();

        _mainCanvas.Children.Clear();
    }

    #endregion

    #region Events

    private static void DrawingMode_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DrawingCanvas canvas || canvas._mainInkCanvas == null)
            return;

        canvas._mainInkCanvas.Visibility = canvas.DrawingMode == DrawingModes.Ink ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void Visual_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DrawingCanvas canvas)
            return;

        if (canvas._mainInkCanvas != null)
        {
            canvas._mainInkCanvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = canvas.Stroke is SolidColorBrush color ? color.Color : Colors.Black,
                Height = Math.Max(canvas.StrokeThickness, 1),
                Width = Math.Max(canvas.StrokeThickness, 1),
            };
        }

        if (canvas._selectedShapes == null)
            return;

        //Change the settings of the selected shapes.
        foreach (var shape in canvas._selectedShapes)
        {
            shape.Stroke = canvas.Stroke;
            shape.StrokeThickness = canvas.StrokeThickness;
            shape.StrokeDashArray = canvas.StrokeDashArray;
            shape.Fill = canvas.Fill;

            if (shape is Rectangle rect)
                rect.RadiusX = rect.RadiusY = canvas.Radius;
        }
    }

    private void Adorner_Manipulated(object sender, ManipulatedEventArgs args)
    {
        if (sender is not ElementAdorner adorner)
            return;

        foreach (var shape in _selectedShapes)
        {
            if (Equals(shape, adorner.AdornedElement)) continue;

            if (_adornerLayer.GetAdorners(shape)?[0] is ElementAdorner ad)
                ad.Angle += args.AngleDifference;

            if (Math.Abs(args.HeightDifference) > 0.1 && shape.ActualHeight + args.HeightDifference > 10 && shape.ActualHeight + args.HeightDifference <= _mainCanvas.ActualHeight)
                shape.Height += args.HeightDifference;
            //shape.Height = shape.ActualHeight + args.HeightDifference;

            if (Math.Abs(args.WidthDifference) > 0.1 && shape.ActualWidth + args.WidthDifference > 10 && shape.ActualWidth + args.WidthDifference <= _mainCanvas.ActualWidth)
                shape.Width += args.WidthDifference;
            //shape.Width = shape.ActualWidth + args.WidthDifference;

            if (Canvas.GetTop(shape) + args.TopDifference >= 0 && Canvas.GetTop(shape) + args.TopDifference + shape.ActualHeight < _mainCanvas.ActualHeight)
                Canvas.SetTop(shape, Canvas.GetTop(shape) + args.TopDifference);

            if (Canvas.GetLeft(shape) + args.LeftDifference >= 0 && Canvas.GetLeft(shape) + args.LeftDifference + shape.ActualWidth < _mainCanvas.ActualWidth)
                Canvas.SetLeft(shape, Canvas.GetLeft(shape) + args.LeftDifference);
        }
    }

    private void Adorner_RotationResetRequested(object sender, RoutedEventArgs e)
    {
        if (sender is not ElementAdorner adorner)
            return;

        foreach (var shape in _selectedShapes)
        {
            if (_adornerLayer.GetAdorners(shape)?[0] is ElementAdorner ad)
                ad.Angle = 0;
        }
    }

    private void Adorner_Removed(object sender, RoutedEventArgs e)
    {
        RemoveAllAdorners();
        RemoveAllSelectedShapes();
    }

    private void Adorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        var adorner = sender as ElementAdorner;
        var shape = adorner?.AdornedElement as Shape;

        if (_selectedShapes.Contains(shape))
            DeselectShape(shape);
    }

    private void MainInkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        RemoveAllAdorners();
        _selectedShapes.Clear();
    }

    private void MainInkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
    {
        foreach (var stroke in _mainInkCanvas.Strokes)
        {
            if (stroke.GetBounds().Width < 12 || stroke.GetBounds().Height < 12)
                continue;

            var shape = new Polyline
            {
                Stroke = new SolidColorBrush(stroke.DrawingAttributes.Color),
                StrokeThickness = StrokeThickness, //How? These strokes can receive pressure info.
                StrokeDashArray = StrokeDashArray,
                FillRule = FillRule.EvenOdd,
                Stretch = Stretch.Fill
            };

            var points = new PointCollection();
            var minTop = stroke.StylusPoints.Min(m => m.Y);
            var minLeft = stroke.StylusPoints.Min(m => m.X);

            foreach (var point in stroke.StylusPoints)
            {
                var x = point.X - minLeft;
                var y = point.Y - minTop;

                points.Add(new Point(x, y));
            }

            shape.Points = points;

            _mainCanvas.Children.Add(shape);
            SelectShape(shape);

            Canvas.SetLeft(shape, minLeft);
            Canvas.SetTop(shape, minTop);

            AdjustDepth();
        }

        _mainInkCanvas.Strokes.Clear();

        Keyboard.Focus(this);
    }

    #endregion
}