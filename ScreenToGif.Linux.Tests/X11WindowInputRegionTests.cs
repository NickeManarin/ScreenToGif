using Avalonia;
using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

public sealed class X11WindowInputRegionTests
{
    [Theory]
    [InlineData(530, 278, 1, 0, 247, 530, 31)]
    [InlineData(760, 420, 1, 0, 389, 760, 31)]
    [InlineData(530, 278, 1.25, 0, 309, 663, 39)]
    public void CalculateBottomRegionTracksWindowSizeAndScaling(double width, double height, double scaling,
        short expectedX, short expectedY, ushort expectedWidth, ushort expectedHeight)
    {
        var rectangle = X11WindowInputRegion.CalculateBottomRegion(new Size(width, height), scaling, 31);

        Assert.Equal(new X11WindowInputRegion.XRectangle(expectedX, expectedY, expectedWidth, expectedHeight),
            rectangle);
    }

    [Fact]
    public void UnsupportedHandleFallsBackWithoutOpeningNativeDisplay()
    {
        using var region = new X11WindowInputRegion();

        Assert.False(region.TryApply(new IntPtr(1), "WAYLAND", new Size(530, 278), 1, 31));
        Assert.False(region.TryApply(IntPtr.Zero, "XID", new Size(530, 278), 1, 31));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateBottomRegionRejectsInvalidScaling(double scaling)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            X11WindowInputRegion.CalculateBottomRegion(new Size(530, 278), scaling, 31));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateBottomRegionRejectsInvalidInteractiveHeight(double interactiveHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            X11WindowInputRegion.CalculateBottomRegion(new Size(530, 278), 1, interactiveHeight));
    }

    [Fact]
    public void CalculateBottomRegionClampsToNativeRectangleLimits()
    {
        var rectangle = X11WindowInputRegion.CalculateBottomRegion(new Size(100_000, 100_000), 2, 100_000);

        Assert.Equal(new X11WindowInputRegion.XRectangle(0, 0, ushort.MaxValue, ushort.MaxValue), rectangle);
    }

    [Fact]
    public void CalculateFrameRegionsKeepsChromeAndCommandBarInteractive()
    {
        var rectangles = X11WindowInputRegion.CalculateFrameRegions(558, 344, 14, 49, 530, 278, 31);

        Assert.Equal(
        [
            new X11WindowInputRegion.XRectangle(0, 0, 558, 49),
            new X11WindowInputRegion.XRectangle(0, 327, 558, 17),
            new X11WindowInputRegion.XRectangle(0, 49, 14, 278),
            new X11WindowInputRegion.XRectangle(544, 49, 14, 278),
            new X11WindowInputRegion.XRectangle(14, 296, 530, 31)
        ], rectangles);
    }

    [Theory]
    [InlineData(0, 0, 530, 278, 0, 247, 530, 31)]
    [InlineData(20, 240, 200, 38, 0, 7, 200, 31)]
    public void CalculateChildRegionsIntersectsChildWithCommandBar(int childX, int childY, uint childWidth,
        uint childHeight, short expectedX, short expectedY, ushort expectedWidth, ushort expectedHeight)
    {
        var rectangles = X11WindowInputRegion.CalculateChildRegions(childX, childY, childWidth, childHeight,
            530, 278, 31);

        Assert.Equal(
            [new X11WindowInputRegion.XRectangle(expectedX, expectedY, expectedWidth, expectedHeight)], rectangles);
    }

    [Fact]
    public void CalculateChildRegionsReturnsEmptyForViewportOnlyChild()
    {
        var rectangles = X11WindowInputRegion.CalculateChildRegions(0, 0, 530, 200, 530, 278, 31);

        Assert.Empty(rectangles);
    }
}
