using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux.Controls;

/// <summary>
/// The drawing surface of the Board: it renders a <see cref="BoardStrokeCollection"/> and turns
/// pointer input into strokes or eraser sweeps. Every geometric decision lives in the stroke
/// model; this control only draws the result and reports when a gesture starts and ends.
/// </summary>
public sealed class BoardCanvas : Control
{
    private readonly Dictionary<BoardStroke, Geometry> _geometryCache = [];
    private List<Point>? _activePoints;
    private Point? _lastErasePoint;

    public BoardCanvas()
    {
        Strokes.Changed += StrokesChanged;
        Cursor = new Cursor(StandardCursorType.Cross);
    }

    /// <summary>Raised when the pointer goes down on the canvas, whichever tool is active.</summary>
    public event EventHandler? GestureStarted;

    /// <summary>Raised when the pointer is released or capture is lost.</summary>
    public event EventHandler? GestureEnded;

    public BoardStrokeCollection Strokes { get; } = new();

    /// <summary>The Board paints on white, as the WPF ink canvas it replaces did.</summary>
    private static readonly IBrush Surface = Brushes.White;

    public BoardTool Tool { get; set; } = BoardTool.Pen;

    public BoardStrokeAttributes PenAttributes { get; set; } = BoardStrokeAttributes.Default;

    public double EraserWidth { get; set; } = 10;

    public double EraserHeight { get; set; } = 10;

    public BoardStylusTip EraserTip { get; set; } = BoardStylusTip.Rectangle;

    private bool IsGestureActive => _activePoints is not null || _lastErasePoint is not null;

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Surface, new Rect(Bounds.Size));

        foreach (var stroke in Strokes.Strokes)
            DrawStroke(context, stroke);

        if (_activePoints is { Count: > 0 })
            DrawStroke(context, new BoardStroke(_activePoints, PenAttributes), cache: false);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var position = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;

        switch (Tool)
        {
            case BoardTool.Pen:
                _activePoints = [position];
                break;
            case BoardTool.PointEraser:
            case BoardTool.StrokeEraser:
                _lastErasePoint = position;
                Erase(position, position);
                break;
            case BoardTool.Selection:
                return;
        }

        GestureStarted?.Invoke(this, EventArgs.Empty);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var position = e.GetPosition(this);

        if (_activePoints is not null)
        {
            _activePoints.Add(position);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_lastErasePoint is not { } previous)
            return;

        Erase(previous, position);
        _lastErasePoint = position;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndGesture();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndGesture();
    }

    private void EndGesture()
    {
        var wasActive = IsGestureActive;
        var drawn = _activePoints;
        _activePoints = null;
        _lastErasePoint = null;

        if (drawn is { Count: > 0 })
            Strokes.Add(BoardStroke.FromInput(drawn, PenAttributes));

        if (!wasActive)
            return;

        InvalidateVisual();
        GestureEnded?.Invoke(this, EventArgs.Empty);
    }

    private void Erase(Point from, Point to)
    {
        var erased = Tool == BoardTool.StrokeEraser
            ? Strokes.EraseByStroke(from, to)
            : Strokes.EraseByPoint(from, to, EraserWidth, EraserHeight, EraserTip);

        if (erased)
            InvalidateVisual();
    }

    private void StrokesChanged(object? sender, EventArgs e)
    {
        var live = new HashSet<BoardStroke>(Strokes.Strokes);
        foreach (var stroke in _geometryCache.Keys.Where(stroke => !live.Contains(stroke)).ToArray())
            _geometryCache.Remove(stroke);

        InvalidateVisual();
    }

    private void DrawStroke(DrawingContext context, BoardStroke stroke, bool cache = true)
    {
        var attributes = stroke.Attributes;
        var opacity = attributes.IsHighlighter ? BoardStrokeAttributes.HighlighterOpacity : 1;
        var brush = new SolidColorBrush(attributes.Color, opacity);
        var geometry = cache ? CachedGeometry(stroke) : BuildGeometry(stroke);

        // Each stroke is drawn in a single operation so a highlighter stroke does not darken
        // where it overlaps itself, only where it overlaps another stroke — the Windows behavior.
        if (geometry is PolylineGeometry polyline)
        {
            context.DrawGeometry(null, StrokePen(brush, attributes), polyline);
            return;
        }

        context.DrawGeometry(brush, null, geometry);
    }

    private static Pen StrokePen(IBrush brush, BoardStrokeAttributes attributes) => new(
        brush,
        attributes.Width,
        lineCap: attributes.Tip == BoardStylusTip.Ellipse ? PenLineCap.Round : PenLineCap.Square,
        lineJoin: attributes.Tip == BoardStylusTip.Ellipse ? PenLineJoin.Round : PenLineJoin.Miter);

    private Geometry CachedGeometry(BoardStroke stroke)
    {
        if (_geometryCache.TryGetValue(stroke, out var cached))
            return cached;

        var geometry = BuildGeometry(stroke);
        _geometryCache[stroke] = geometry;
        return geometry;
    }

    /// <summary>
    /// A stroke whose tip is as wide as it is tall is one stroked polyline, which is both exact
    /// and a single draw call. An anisotropic tip cannot be expressed as a pen, so that stroke
    /// is the union of the tip stamped along its path instead.
    /// </summary>
    private static Geometry BuildGeometry(BoardStroke stroke)
    {
        var attributes = stroke.Attributes;

        if (Math.Abs(attributes.Width - attributes.Height) > double.Epsilon || stroke.Points.Count < 2)
            return StampedGeometry(stroke);

        return new PolylineGeometry(stroke.Points.ToArray(), isFilled: false);
    }

    private static Geometry StampedGeometry(BoardStroke stroke)
    {
        var attributes = stroke.Attributes;
        var group = new GeometryGroup { FillRule = FillRule.NonZero };

        foreach (var center in StampCenters(stroke.Points, Math.Min(attributes.HalfWidth, attributes.HalfHeight)))
            group.Children.Add(Stamp(center, attributes));

        return group;
    }

    private static Geometry Stamp(Point center, BoardStrokeAttributes attributes)
    {
        var bounds = new Rect(
            center.X - attributes.HalfWidth,
            center.Y - attributes.HalfHeight,
            attributes.Width,
            attributes.Height);

        return attributes.Tip == BoardStylusTip.Ellipse
            ? new EllipseGeometry(bounds)
            : new RectangleGeometry(bounds);
    }

    /// <summary>
    /// Walks the stroke laying the stylus tip down a third of a half-extent apart, close enough
    /// that consecutive tips overlap deeply and the sweep reads as one mark rather than a bead chain.
    /// </summary>
    private static IEnumerable<Point> StampCenters(IReadOnlyList<Point> points, double smallestHalfExtent)
    {
        var spacing = Math.Max(smallestHalfExtent / 3, 0.35);
        yield return points[0];

        for (var index = 0; index < points.Count - 1; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var steps = (int)Math.Ceiling(BoardStroke.Distance(start, end) / spacing);

            for (var step = 1; step <= steps; step++)
            {
                var progress = (double)step / steps;
                yield return new Point(start.X + (end.X - start.X) * progress, start.Y + (end.Y - start.Y) * progress);
            }
        }
    }
}
