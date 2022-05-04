using System.ComponentModel;

namespace ScreenToGif.Domain.Enums;

/// <summary>
/// Dither methods, currently being used by FFmpeg.
/// </summary>
public enum DitherMethods
{
    [Description("bayer")]
    Bayer,

    [Description("heckbert")]
    Heckbert,

    [Description("floyd_steinberg")]
    FloydSteinberg,

    [Description("sierra2")]
    Sierra2,

    [Description("sierra2_4a")]
    Sierra2Lite,
}