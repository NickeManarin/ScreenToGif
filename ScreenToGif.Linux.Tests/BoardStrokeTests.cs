using Avalonia;
using Avalonia.Media;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class BoardStrokeTests
{
    private static readonly BoardStrokeAttributes Pen =
        new(Colors.Black, 10, 10, BoardStylusTip.Ellipse, FitToCurve: false, IsHighlighter: false);

    [Fact]
    public void PointEraserSplitsAVisibleLineBetweenSparseInputPoints()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(new BoardStroke([new Point(0, 50), new Point(100, 50)], Pen));

        Assert.True(strokes.EraseByPoint(new Point(50, 50), new Point(50, 50), 10, 10, BoardStylusTip.Ellipse));

        Assert.Equal(2, strokes.Count);
        Assert.True(strokes.Strokes[0].Points[^1].X < 50);
        Assert.True(strokes.Strokes[1].Points[0].X > 50);
    }

    [Fact]
    public void PointEraserTouchesThePaintedEdgeOfAThickStroke()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(new BoardStroke([new Point(0, 50), new Point(100, 50)], Pen with { Width = 40, Height = 40 }));

        Assert.True(strokes.EraseByPoint(new Point(50, 68), new Point(50, 68), 10, 10, BoardStylusTip.Ellipse));
        Assert.Equal(2, strokes.Count);
    }

    [Fact]
    public void ASecondEraseSplitsTheSurvivingPieceWithoutChangingItsInk()
    {
        var strokes = new BoardStrokeCollection();
        var attributes = Pen with { Color = Colors.Red, Width = 12 };
        strokes.Add(new BoardStroke([new Point(0, 50), new Point(100, 50)], attributes));

        Assert.True(strokes.EraseByPoint(new Point(30, 50), new Point(30, 50), 8, 8, BoardStylusTip.Ellipse));
        Assert.True(strokes.EraseByPoint(new Point(70, 50), new Point(70, 50), 8, 8, BoardStylusTip.Ellipse));

        Assert.Equal(3, strokes.Count);
        Assert.All(strokes.Strokes, stroke => Assert.Equal(attributes, stroke.Attributes));
        Assert.True(strokes.Strokes[0].Points[^1].X < 30);
        Assert.True(strokes.Strokes[1].Points[0].X > 30);
        Assert.True(strokes.Strokes[1].Points[^1].X < 70);
        Assert.True(strokes.Strokes[2].Points[0].X > 70);
    }

    private static BoardStroke HorizontalStroke(double y, double fromX, double toX, double step = 1)
    {
        var points = new List<Point>();
        for (var x = fromX; x <= toX; x += step)
            points.Add(new Point(x, y));
        return new BoardStroke(points, Pen);
    }

    [Fact]
    public void FromInputKeepsTheChosenAttributes()
    {
        var attributes = new BoardStrokeAttributes(Colors.Red, 24, 8, BoardStylusTip.Rectangle, FitToCurve: false, IsHighlighter: true);

        var stroke = BoardStroke.FromInput([new Point(0, 0), new Point(20, 0)], attributes);

        Assert.Equal(attributes, stroke.Attributes);
        Assert.Equal(12, stroke.Attributes.HalfWidth);
        Assert.Equal(4, stroke.Attributes.HalfHeight);
        Assert.Equal(12, stroke.Attributes.Reach);
    }

    [Fact]
    public void FromInputDropsPointsTooCloseToBeVisible()
    {
        var stroke = BoardStroke.FromInput(
            [new Point(0, 0), new Point(0.1, 0), new Point(0.2, 0), new Point(5, 0)],
            Pen);

        Assert.Equal([new Point(0, 0), new Point(5, 0)], stroke.Points);
    }

    [Fact]
    public void FitToCurveSmoothsAJaggedPathAndLeavesTheEndsWhereTheyWere()
    {
        IReadOnlyList<Point> jagged =
            [new(0, 0), new(10, 20), new(20, 0), new(30, 20), new(40, 0)];

        var raw = new BoardStroke(jagged, Pen);
        var fitted = BoardStroke.FromInput(jagged, Pen with { FitToCurve = true });

        Assert.True(SharpestTurn(fitted.Points) < SharpestTurn(raw.Points) / 2);
        Assert.Equal(jagged[0], fitted.Points[0]);
        Assert.Equal(jagged[^1], fitted.Points[^1]);
    }

    [Fact]
    public void FitToCurveLeavesAStraightLineAlone()
    {
        var fitted = BoardStroke.FromInput([new Point(0, 0), new Point(10, 0), new Point(20, 0)], Pen with { FitToCurve = true });

        Assert.All(fitted.Points, point => Assert.Equal(0, point.Y, 6));
    }

    [Fact]
    public void StrokeEraserRemovesOnlyTheStrokeItCrosses()
    {
        var strokes = new BoardStrokeCollection();
        var first = HorizontalStroke(y: 10, fromX: 0, toX: 100);
        var second = HorizontalStroke(y: 200, fromX: 0, toX: 100);
        strokes.Add(first);
        strokes.Add(second);

        Assert.True(strokes.EraseByStroke(new Point(50, 0), new Point(50, 30)));

        Assert.Equal([second], strokes.Strokes);
    }

    [Fact]
    public void StrokeEraserThatMissesEverythingChangesNothing()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(HorizontalStroke(y: 10, fromX: 0, toX: 100));
        var changes = 0;
        strokes.Changed += (_, _) => changes++;

        Assert.False(strokes.EraseByStroke(new Point(0, 400), new Point(100, 400)));

        Assert.Equal(1, strokes.Count);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void StrokeEraserCountsTheStrokesOwnThicknessAsPartOfIt()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(new BoardStroke([new Point(0, 0), new Point(100, 0)], Pen with { Width = 40, Height = 40 }));

        // The eraser passes 15px below the centerline, still inside the 20px reach of the drawn ink.
        Assert.True(strokes.EraseByStroke(new Point(50, 15), new Point(60, 15)));
        Assert.Equal(0, strokes.Count);
    }

    [Fact]
    public void PointEraserThroughTheMiddleLeavesBothEndsAtTheirOriginalAttributes()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(HorizontalStroke(y: 50, fromX: 0, toX: 100));

        Assert.True(strokes.EraseByPoint(new Point(50, 50), new Point(50, 50), 10, 10, BoardStylusTip.Ellipse));

        Assert.Equal(2, strokes.Count);
        var left = strokes.Strokes[0];
        var right = strokes.Strokes[1];
        Assert.Equal(0, left.Points[0].X);
        Assert.True(left.Points[^1].X < 50);
        Assert.True(right.Points[0].X > 50);
        Assert.Equal(100, right.Points[^1].X);
        Assert.All(strokes.Strokes, stroke => Assert.Equal(Pen, stroke.Attributes));
    }

    [Fact]
    public void PointEraserAcrossAStrokeEndLeavesOnePiece()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(HorizontalStroke(y: 50, fromX: 0, toX: 100));

        Assert.True(strokes.EraseByPoint(new Point(0, 50), new Point(0, 50), 20, 20, BoardStylusTip.Ellipse));

        var survivor = Assert.Single(strokes.Strokes);
        Assert.True(survivor.Points[0].X > 0);
        Assert.Equal(100, survivor.Points[^1].X);
    }

    [Fact]
    public void PointEraserThatMissesChangesNothing()
    {
        var strokes = new BoardStrokeCollection();
        var stroke = HorizontalStroke(y: 50, fromX: 0, toX: 100);
        strokes.Add(stroke);

        Assert.False(strokes.EraseByPoint(new Point(50, 200), new Point(60, 200), 10, 10, BoardStylusTip.Ellipse));

        Assert.Equal([stroke], strokes.Strokes);
    }

    [Fact]
    public void PointEraserSweepErasesEveryPointAlongItsTravelNotJustItsEnds()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(HorizontalStroke(y: 50, fromX: 0, toX: 100));

        Assert.True(strokes.EraseByPoint(new Point(20, 50), new Point(80, 50), 10, 10, BoardStylusTip.Ellipse));

        Assert.Equal(2, strokes.Count);
        Assert.True(strokes.Strokes[0].Points[^1].X < 20);
        Assert.True(strokes.Strokes[1].Points[0].X > 80);
    }

    [Fact]
    public void PointEraserRespectsAnAnisotropicTip()
    {
        var wide = new BoardStrokeCollection();
        wide.Add(HorizontalStroke(y: 50, fromX: 0, toX: 100));
        var tall = new BoardStrokeCollection();
        tall.Add(HorizontalStroke(y: 50, fromX: 0, toX: 100));

        // A 40x4 tip centred 8px above the line cannot reach it; a 4x40 tip can.
        Assert.False(wide.EraseByPoint(new Point(50, 42), new Point(50, 42), 40, 4, BoardStylusTip.Rectangle));
        Assert.True(tall.EraseByPoint(new Point(50, 42), new Point(50, 42), 4, 40, BoardStylusTip.Rectangle));
    }

    [Fact]
    public void RectangleTipErasesTheCornersAnEllipseTipLeaves()
    {
        var rectangle = new BoardStrokeCollection();
        rectangle.Add(new BoardStroke([new Point(9, 9)], Pen with { Width = 1, Height = 1 }));
        var ellipse = new BoardStrokeCollection();
        ellipse.Add(new BoardStroke([new Point(9, 9)], Pen with { Width = 1, Height = 1 }));

        Assert.True(rectangle.EraseByPoint(new Point(0, 0), new Point(0, 0), 20, 20, BoardStylusTip.Rectangle));
        Assert.False(ellipse.EraseByPoint(new Point(0, 0), new Point(0, 0), 20, 20, BoardStylusTip.Ellipse));
    }

    [Fact]
    public void ClearRemovesEverythingAndReportsWhetherItHadTo()
    {
        var strokes = new BoardStrokeCollection();
        strokes.Add(HorizontalStroke(y: 10, fromX: 0, toX: 10));

        Assert.True(strokes.Clear());
        Assert.Equal(0, strokes.Count);
        Assert.False(strokes.Clear());
    }

    [Fact]
    public void AStrokeNeedsAtLeastOnePoint() =>
        Assert.Throws<ArgumentException>(() => new BoardStroke([], Pen));

    /// <summary>The worst kink in a polyline, in radians; curve fitting should soften it.</summary>
    private static double SharpestTurn(IReadOnlyList<Point> points)
    {
        var sharpest = 0d;
        for (var index = 1; index < points.Count - 1; index++)
        {
            var incoming = Math.Atan2(points[index].Y - points[index - 1].Y, points[index].X - points[index - 1].X);
            var outgoing = Math.Atan2(points[index + 1].Y - points[index].Y, points[index + 1].X - points[index].X);
            var turn = Math.Abs(outgoing - incoming);
            sharpest = Math.Max(sharpest, Math.Min(turn, Math.Tau - turn));
        }

        return sharpest;
    }
}
