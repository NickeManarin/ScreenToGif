using System.Runtime.InteropServices;

namespace ScreenToGif.Domain.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct GifskiSettings
{
    public GifskiSettings(uint width, uint height, byte quality, bool looped, bool fast)
    {
        Width = width;
        Height = height;
        Quality = quality;
        Once = !looped;
        Fast = fast;
    }

    /// <summary>
    /// Resize to max this width if non-0.
    /// </summary>
    internal uint Width;

    /// <summary>
    /// Resize to max this height if width is non-0. Note that aspect ratio is not preserved.
    /// </summary>
    internal uint Height;

    /// <summary>
    /// 1-100. Recommended to set to 100.
    /// </summary>
    internal byte Quality;

    /// <summary>
    /// If true, looping is disabled.
    /// </summary>
    internal bool Once;

    /// <summary>
    /// Lower quality, but faster encode.
    /// </summary>
    internal bool Fast;
}