using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>
/// Makes Avalonia's imaging usable outside a running application. Encoding a bitmap needs a
/// registered render interface, which the app installs at startup but a test host does not.
/// </summary>
public static class AvaloniaImaging
{
    private static readonly Lock Gate = new();
    private static bool _initialized;

    /// <summary>
    /// Registers Skia as the render interface once, if nothing has registered one yet. Safe to call
    /// from the running application, where it finds an interface already present and does nothing.
    /// </summary>
    public static void EnsureRenderInterface()
    {
        if (_initialized)
            return;

        lock (Gate)
        {
            if (_initialized)
                return;

            if (!HasRenderInterface())
                SkiaPlatform.Initialize();

            _initialized = true;
        }
    }

    private static bool HasRenderInterface()
    {
        try
        {
            using var probe = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Bgra8888,
                AlphaFormat.Opaque);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
