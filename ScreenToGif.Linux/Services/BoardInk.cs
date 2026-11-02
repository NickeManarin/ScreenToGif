using Avalonia;
using Avalonia.Media;

namespace ScreenToGif.Linux.Services;

/// <summary>The four mutually exclusive Board tools, matching the Windows Board's editing modes.</summary>
public enum BoardTool
{
    Pen,
    PointEraser,
    Selection,
    StrokeEraser
}

/// <summary>The shape a stylus leaves at a single point of a stroke.</summary>
public enum BoardStylusTip
{
    Ellipse,
    Rectangle
}

/// <summary>Everything that decides how one stroke looks, captured when the stroke starts.</summary>
public sealed record BoardStrokeAttributes(
    Color Color,
    double Width,
    double Height,
    BoardStylusTip Tip,
    bool FitToCurve,
    bool IsHighlighter)
{
    /// <summary>The opacity a highlighter stroke is filled at; see docs/board.md for why this is a constant.</summary>
    public const double HighlighterOpacity = 0.5;

    public static BoardStrokeAttributes Default { get; } =
        new(Colors.Black, 10, 10, BoardStylusTip.Ellipse, FitToCurve: false, IsHighlighter: false);

    public double HalfWidth => Width / 2;

    public double HalfHeight => Height / 2;

    /// <summary>The farthest the drawn ink reaches from the stroke's centerline.</summary>
    public double Reach => Math.Max(HalfWidth, HalfHeight);
}

/// <summary>
/// One drawn stroke as the polyline that is actually rendered. Curve fitting and input
/// thinning happen once, when the stroke is built from pointer input, so that erasing
/// a stroke splits exactly the line the user can see.
/// </summary>
public sealed class BoardStroke
{
    private const double MinimumInputSpacing = 0.5;

    public BoardStroke(IEnumerable<Point> points, BoardStrokeAttributes attributes)
    {
        Points = points.ToArray();
        Attributes = attributes;

        if (Points.Count == 0)
            throw new ArgumentException("A stroke needs at least one point.", nameof(points));
    }

    public IReadOnlyList<Point> Points { get; }

    public BoardStrokeAttributes Attributes { get; }

    /// <summary>Builds a stroke from raw pointer input, thinning it and fitting a curve when the attributes ask for one.</summary>
    public static BoardStroke FromInput(IEnumerable<Point> rawPoints, BoardStrokeAttributes attributes)
    {
        var thinned = Thin(rawPoints);
        return new BoardStroke(attributes.FitToCurve ? BoardStrokeSmoothing.Smooth(thinned) : thinned, attributes);
    }

    private static IReadOnlyList<Point> Thin(IEnumerable<Point> rawPoints)
    {
        var thinned = new List<Point>();
        foreach (var point in rawPoints)
        {
            if (thinned.Count > 0 && Distance(thinned[^1], point) < MinimumInputSpacing)
                continue;
            thinned.Add(point);
        }

        return thinned;
    }

    internal static double Distance(Point first, Point second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

/// <summary>Chaikin corner cutting, the Linux stand-in for the Windows ink renderer's Fit to curve.</summary>
public static class BoardStrokeSmoothing
{
    private const int Iterations = 2;

    public static IReadOnlyList<Point> Smooth(IReadOnlyList<Point> points)
    {
        var current = points;
        for (var iteration = 0; iteration < Iterations && current.Count > 2; iteration++)
            current = CutCorners(current);
        return current;
    }

    private static IReadOnlyList<Point> CutCorners(IReadOnlyList<Point> points)
    {
        var cut = new List<Point>(points.Count * 2) { points[0] };
        for (var index = 0; index < points.Count - 1; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            cut.Add(new Point(start.X * 0.75 + end.X * 0.25, start.Y * 0.75 + end.Y * 0.25));
            cut.Add(new Point(start.X * 0.25 + end.X * 0.75, start.Y * 0.25 + end.Y * 0.75));
        }

        cut.Add(points[^1]);
        return cut;
    }
}

/// <summary>
/// The strokes on a Board, and the two erasers that act on them. Every operation here is
/// plain geometry so the drawing contract can be tested without an Avalonia render surface.
/// </summary>
public sealed class BoardStrokeCollection
{
    private readonly List<BoardStroke> _strokes = [];

    public IReadOnlyList<BoardStroke> Strokes => _strokes;

    public int Count => _strokes.Count;

    /// <summary>Raised whenever the strokes change, so a view can invalidate itself.</summary>
    public event EventHandler? Changed;

    public void Add(BoardStroke stroke)
    {
        _strokes.Add(stroke);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Clear()
    {
        if (_strokes.Count == 0)
            return false;

        _strokes.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Removes every stroke the eraser segment touches, whole.</summary>
    public bool EraseByStroke(Point from, Point to)
    {
        var survivors = _strokes.Where(stroke => !Touches(stroke, from, to)).ToList();
        if (survivors.Count == _strokes.Count)
            return false;

        _strokes.Clear();
        _strokes.AddRange(survivors);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Removes the part of each stroke the eraser tip sweeps over, splitting a stroke into the
    /// pieces that survive on either side of the sweep.
    /// </summary>
    public bool EraseByPoint(Point from, Point to, double eraserWidth, double eraserHeight, BoardStylusTip tip)
    {
        var halfWidth = Math.Max(eraserWidth, 1) / 2;
        var halfHeight = Math.Max(eraserHeight, 1) / 2;

        var replacement = new List<BoardStroke>(_strokes.Count);
        var changed = false;

        foreach (var stroke in _strokes)
        {
            if (!MayTouch(stroke, from, to, halfWidth, halfHeight))
            {
                replacement.Add(stroke);
                continue;
            }

            var survivingRuns = SplitAroundSweep(stroke, from, to, halfWidth, halfHeight, tip);
            if (survivingRuns is null)
            {
                replacement.Add(stroke);
                continue;
            }

            changed = true;
            foreach (var run in survivingRuns)
                replacement.Add(new BoardStroke(run, stroke.Attributes));
        }

        if (!changed)
            return false;

        _strokes.Clear();
        _strokes.AddRange(replacement);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static bool MayTouch(BoardStroke stroke, Point from, Point to, double halfWidth, double halfHeight)
    {
        var reachX = halfWidth + stroke.Attributes.HalfWidth;
        var reachY = halfHeight + stroke.Attributes.HalfHeight;
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var point in stroke.Points)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        return minX <= Math.Max(from.X, to.X) + reachX &&
               maxX >= Math.Min(from.X, to.X) - reachX &&
               minY <= Math.Max(from.Y, to.Y) + reachY &&
               maxY >= Math.Min(from.Y, to.Y) - reachY;
    }

    /// <summary>
    /// A sparse input segment still paints a continuous line. Sample the rendered centerline before
    /// splitting it, and include the pen tip when testing whether the eraser touches painted ink.
    /// Null means the stroke was not touched, so callers keep its original point list unchanged.
    /// </summary>
    private static List<List<Point>>? SplitAroundSweep(
        BoardStroke stroke,
        Point from,
        Point to,
        double halfWidth,
        double halfHeight,
        BoardStylusTip tip)
    {
        var runs = new List<List<Point>>();
        List<Point>? run = null;
        var touched = false;

        foreach (var point in SampleStroke(stroke.Points))
        {
            if (IsCovered(point, from, to, halfWidth, halfHeight, tip, stroke.Attributes))
            {
                touched = true;
                run = null;
                continue;
            }

            if (run is null)
            {
                run = [];
                runs.Add(run);
            }

            run.Add(point);
        }

        return touched ? runs : null;
    }

    private static IEnumerable<Point> SampleStroke(IReadOnlyList<Point> points)
    {
        yield return points[0];
        for (var index = 0; index < points.Count - 1; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var steps = Math.Max(1, (int)Math.Ceiling(BoardStroke.Distance(start, end) / 0.5));
            for (var step = 1; step <= steps; step++)
            {
                var progress = (double)step / steps;
                yield return new Point(start.X + (end.X - start.X) * progress, start.Y + (end.Y - start.Y) * progress);
            }
        }
    }

    private static bool IsCovered(
        Point point,
        Point from,
        Point to,
        double halfWidth,
        double halfHeight,
        BoardStylusTip tip,
        BoardStrokeAttributes stroke)
    {
        var rectangleWidth = (tip == BoardStylusTip.Rectangle ? halfWidth : 0) +
                             (stroke.Tip == BoardStylusTip.Rectangle ? stroke.HalfWidth : 0);
        var rectangleHeight = (tip == BoardStylusTip.Rectangle ? halfHeight : 0) +
                              (stroke.Tip == BoardStylusTip.Rectangle ? stroke.HalfHeight : 0);
        var ellipseWidth = (tip == BoardStylusTip.Ellipse ? halfWidth : 0) +
                           (stroke.Tip == BoardStylusTip.Ellipse ? stroke.HalfWidth : 0);
        var ellipseHeight = (tip == BoardStylusTip.Ellipse ? halfHeight : 0) +
                            (stroke.Tip == BoardStylusTip.Ellipse ? stroke.HalfHeight : 0);

        if (ellipseWidth == 0)
            return SegmentIntersectsRectangle(from, to, point, rectangleWidth, rectangleHeight);

        if (rectangleWidth == 0)
        {
            var start = new Point(from.X / ellipseWidth, from.Y / ellipseHeight);
            var end = new Point(to.X / ellipseWidth, to.Y / ellipseHeight);
            var center = new Point(point.X / ellipseWidth, point.Y / ellipseHeight);
            return DistanceToSegment(center, start, end) <= 1;
        }

        // The distance from a point to a swept rectangle-plus-ellipse is convex along the
        // eraser's segment, so a bounded ternary search finds its closest approach.
        double DistanceAt(double progress)
        {
            var x = Math.Max(0, Math.Abs(point.X - (from.X + (to.X - from.X) * progress)) - rectangleWidth) / ellipseWidth;
            var y = Math.Max(0, Math.Abs(point.Y - (from.Y + (to.Y - from.Y) * progress)) - rectangleHeight) / ellipseHeight;
            return x * x + y * y;
        }

        if (from == to)
            return DistanceAt(0) <= 1;

        var low = 0d;
        var high = 1d;
        for (var index = 0; index < 24; index++)
        {
            var first = low + (high - low) / 3;
            var second = high - (high - low) / 3;
            if (DistanceAt(first) < DistanceAt(second))
                high = second;
            else
                low = first;
        }

        return Math.Min(DistanceAt(0), Math.Min(DistanceAt(1), DistanceAt((low + high) / 2))) <= 1;
    }

    private static bool SegmentIntersectsRectangle(Point from, Point to, Point center, double halfWidth, double halfHeight)
    {
        var low = 0d;
        var high = 1d;
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        if (!Clip(-dx, from.X - (center.X - halfWidth), ref low, ref high) ||
            !Clip(dx, center.X + halfWidth - from.X, ref low, ref high) ||
            !Clip(-dy, from.Y - (center.Y - halfHeight), ref low, ref high) ||
            !Clip(dy, center.Y + halfHeight - from.Y, ref low, ref high))
            return false;

        return low <= high;
    }

    private static bool Clip(double direction, double distance, ref double low, ref double high)
    {
        if (Math.Abs(direction) <= double.Epsilon)
            return distance >= 0;

        var boundary = distance / direction;
        if (direction < 0)
        {
            if (boundary > high)
                return false;
            low = Math.Max(low, boundary);
        }
        else
        {
            if (boundary < low)
                return false;
            high = Math.Min(high, boundary);
        }

        return true;
    }

    private static bool Touches(BoardStroke stroke, Point from, Point to)
    {
        var reach = stroke.Attributes.Reach;

        if (stroke.Points.Count == 1)
            return DistanceToSegment(stroke.Points[0], from, to) <= reach;

        for (var index = 0; index < stroke.Points.Count - 1; index++)
        {
            if (SegmentDistance(stroke.Points[index], stroke.Points[index + 1], from, to) <= reach)
                return true;
        }

        return false;
    }

    internal static double SegmentDistance(Point firstStart, Point firstEnd, Point secondStart, Point secondEnd)
    {
        if (SegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd))
            return 0;

        return Math.Min(
            Math.Min(DistanceToSegment(firstStart, secondStart, secondEnd), DistanceToSegment(firstEnd, secondStart, secondEnd)),
            Math.Min(DistanceToSegment(secondStart, firstStart, firstEnd), DistanceToSegment(secondEnd, firstStart, firstEnd)));
    }

    private static bool SegmentsIntersect(Point firstStart, Point firstEnd, Point secondStart, Point secondEnd)
    {
        var d1 = Cross(secondStart, secondEnd, firstStart);
        var d2 = Cross(secondStart, secondEnd, firstEnd);
        var d3 = Cross(firstStart, firstEnd, secondStart);
        var d4 = Cross(firstStart, firstEnd, secondEnd);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static double Cross(Point origin, Point first, Point second) =>
        (first.X - origin.X) * (second.Y - origin.Y) - (first.Y - origin.Y) * (second.X - origin.X);

    internal static double DistanceToSegment(Point point, Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = dx * dx + dy * dy;

        if (lengthSquared <= double.Epsilon)
            return BoardStroke.Distance(point, start);

        var projection = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
        projection = Math.Clamp(projection, 0, 1);
        return BoardStroke.Distance(point, new Point(start.X + dx * projection, start.Y + dy * projection));
    }
}
