using System.Diagnostics.CodeAnalysis;

namespace ScreenToGif.Domain.Enums;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum VideoCodecPresets
{
    Auto = -1,
    NotSelected,
    None,

    VerySlow,
    Slower,
    Slow,
    Medium,
    Fast,
    Faster,
    VeryFast,
    SuperFast,
    UltraFast,

    Quality,
    Balanced,
    Speed,

    Default,
    Lossless,
    LosslessHP,
    HP,
    HQ,
    BD,
    LowLatency,
    LowLatencyHP,
    LowLatencyHQ,

    Picture, //Digital picture, like portrait, inner shot.
    Photo, //Outdoor photograph, with natural lighting.
    Drawing, //Hand or line drawing, with high-contrast details.
    Icon, //Small-sized colorful images.
    Text //Text-like.
}