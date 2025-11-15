using System.Runtime.InteropServices;

namespace ScreenToGif.Domain.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct GifskiSettings
{
    public GifskiSettings(uint width, uint height, byte quality, bool fast, short repeat)
    {
        Width = width;
        Height = height;
        Quality = quality;
        Fast = fast;
        Repeat = repeat;
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
    /// If negative, looping is disabled. The number of times the sequence is repeated. 0 to loop forever.
    /// </summary>
    internal short Repeat;

    /// <summary>
    /// Lower quality, but faster encode.
    /// </summary>
    internal bool Fast;
}