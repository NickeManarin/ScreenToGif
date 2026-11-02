using Avalonia;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// Maps between the recorder frame on screen and the rectangle of desktop pixels it records.
/// Kept separate from the window so the arithmetic is testable without a display.
/// </summary>
public static class RecorderRegion
{
    /// <summary>Smallest recordable edge, in physical pixels.</summary>
    public const int MinimumSide = 16;

    /// <summary>Largest recordable edge, matching the editor's media limit.</summary>
    public const int MaximumSide = 8192;

    /// <summary>
    /// The desktop rectangle inside the viewport's border.
    /// <paramref name="interiorOrigin"/> is the viewport interior's top-left in physical desktop
    /// pixels, which the caller reads from the control rather than from the window position: a
    /// window's own position excludes the decorations the compositor draws around it.
    /// </summary>
    public static PixelRect Calculate(PixelPoint interiorOrigin, Size viewportSize, double borderThickness,
        double renderScaling)
    {
        if (!double.IsFinite(renderScaling) || renderScaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderScaling), renderScaling,
                "Render scaling must be a positive, finite number.");
        if (!double.IsFinite(borderThickness) || borderThickness < 0)
            throw new ArgumentOutOfRangeException(nameof(borderThickness), borderThickness,
                "Border thickness cannot be negative.");

        var interiorWidth = Math.Max(0, viewportSize.Width - 2 * borderThickness);
        var interiorHeight = Math.Max(0, viewportSize.Height - 2 * borderThickness);

        return new PixelRect(
            interiorOrigin.X,
            interiorOrigin.Y,
            ClampSide(interiorWidth * renderScaling),
            ClampSide(interiorHeight * renderScaling));
    }

    /// <summary>
    /// The window size that yields a recording region of the given physical size: the inverse of
    /// <see cref="Calculate"/>, used when the width and height fields are edited.
    /// </summary>
    public static Size WindowSizeFor(int regionWidth, int regionHeight, double borderThickness,
        double commandBarHeight, double renderScaling)
    {
        if (!double.IsFinite(renderScaling) || renderScaling <= 0)
            throw new ArgumentOutOfRangeException(nameof(renderScaling), renderScaling,
                "Render scaling must be a positive, finite number.");

        return new Size(
            regionWidth / renderScaling + 2 * borderThickness,
            regionHeight / renderScaling + 2 * borderThickness + commandBarHeight);
    }

    // Away from zero rather than to even: a half pixel always grows the region, so a frame the
    // user sized never silently records one row or column less than it shows.
    private static int ClampSide(double physicalPixels) =>
        (int)Math.Clamp(Math.Round(physicalPixels, MidpointRounding.AwayFromZero), MinimumSide, MaximumSide);
}
