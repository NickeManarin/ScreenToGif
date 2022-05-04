using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ScreenToGif.Domain.Enums;

/// <summary>
/// FFmpeg pixel formats.
/// https://github.com/FFmpeg/FFmpeg/blob/b7b73e83e3d5c78a5fea96a6bcae02e1f0a5c45f/libavutil/pixdesc.c
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum VideoPixelFormats
{
    NotSelected,
    Auto,
    Bgr0,
    [Description("bgr4_byte")] Bgr4Byte, //https://stackoverflow.com/questions/8588384/how-to-define-an-enum-with-string-value
    Bgr8,
    BgrA,
    Cuda,
    D3D11,
    Dxva2Vld,
    Gbrp,
    Gbrp10Le,
    Gbrp12Le,
    Gray,
    Gray10Le,
    Gray12Le,
    Gray16Be,
    MonoB,
    Nv12,
    Nv16,
    Nv20Le,
    Nv21,
    P010Le,
    Pal8,
    Qsv,
    Rgb24,
    Rgb48Be,
    Rgb8,
    Rgba64Be,
    RgbA,
    [Description("bgr4_byte")] Rgb4Byte,
    Ya8,
    Ya16Be,
    Yuv420p,
    Yuv420p10Le,
    Yuv420p12Le,
    Yuv422p,
    Yuv422p10Le,
    Yuv422p12Le,
    Yuv440p,
    Yuv444p,
    Yuv440p10Le,
    Yuv440p12Le,
    Yuv444p10Le,
    Yuv444p12Le,
    Yuv444p16Le,
    Yuva420p,
    Yuvj420p,
    Yuvj422p,
    Yuvj444p,
}