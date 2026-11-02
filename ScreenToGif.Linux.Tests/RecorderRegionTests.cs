using Avalonia;
using ScreenToGif.Linux.Services.Capture;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// The arithmetic that turns the recorder frame on screen into a rectangle of desktop pixels,
/// and back again when the width and height fields are edited.
/// </summary>
public sealed class RecorderRegionTests
{
    [Fact]
    public void The_region_is_the_viewport_interior_excluding_its_border()
    {
        var region = RecorderRegion.Calculate(new PixelPoint(101, 51), new Size(502, 203),
            borderThickness: 1, renderScaling: 1);

        Assert.Equal(new PixelRect(101, 51, 500, 201), region);
    }

    [Fact]
    public void A_scaled_display_maps_logical_size_to_physical_pixels()
    {
        var region = RecorderRegion.Calculate(new PixelPoint(0, 0), new Size(402, 202),
            borderThickness: 1, renderScaling: 2);

        Assert.Equal(800, region.Width);
        Assert.Equal(400, region.Height);
    }

    [Fact]
    public void A_fractional_scale_rounds_rather_than_truncates()
    {
        var region = RecorderRegion.Calculate(new PixelPoint(0, 0), new Size(102, 52),
            borderThickness: 1, renderScaling: 1.25);

        Assert.Equal(125, region.Width);
        Assert.Equal(63, region.Height);
    }

    [Fact]
    public void A_frame_smaller_than_its_own_border_still_yields_a_recordable_region()
    {
        var region = RecorderRegion.Calculate(new PixelPoint(7, 9), new Size(1, 1),
            borderThickness: 1, renderScaling: 1);

        Assert.Equal(RecorderRegion.MinimumSide, region.Width);
        Assert.Equal(RecorderRegion.MinimumSide, region.Height);
        Assert.Equal(7, region.X);
    }

    [Fact]
    public void An_oversized_frame_is_clamped_to_the_editor_media_limit()
    {
        var region = RecorderRegion.Calculate(new PixelPoint(0, 0), new Size(20_000, 20_000),
            borderThickness: 1, renderScaling: 1);

        Assert.Equal(RecorderRegion.MaximumSide, region.Width);
        Assert.Equal(RecorderRegion.MaximumSide, region.Height);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(2.0)]
    public void The_window_size_for_a_region_reproduces_that_region(double renderScaling)
    {
        var windowSize = RecorderRegion.WindowSizeFor(640, 360, borderThickness: 1, commandBarHeight: 31,
            renderScaling);

        // The viewport is the window minus the command bar.
        var viewportSize = new Size(windowSize.Width, windowSize.Height - 31);
        var region = RecorderRegion.Calculate(new PixelPoint(0, 0), viewportSize, borderThickness: 1, renderScaling);

        Assert.Equal(640, region.Width);
        Assert.Equal(360, region.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void A_nonsensical_scale_is_refused_rather_than_producing_a_wrong_region(double renderScaling)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecorderRegion.Calculate(new PixelPoint(0, 0), new Size(100, 100), 1, renderScaling));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecorderRegion.WindowSizeFor(100, 100, 1, 31, renderScaling));
    }
}
