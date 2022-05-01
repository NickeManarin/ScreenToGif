using System.ComponentModel;

namespace ScreenToGif.Domain.Enums;

public enum VideoCodecs
{
    NotSelected,

    [Description("mpeg2video")]
    Mpeg2,

    [Description("mpeg4")]
    Mpeg4,

    [Description("libx264")]
    X264,

    [Description("h264_amf")]
    H264Amf,

    [Description("h264_nvenc")]
    H264Nvenc,

    [Description("h264_qsv")]
    H264Qsv,

    [Description("libx265")]
    X265,

    [Description("hevc_amf")]
    HevcAmf,

    [Description("hevc_nvenc")]
    HevcNvenc,

    [Description("hevc_qsv")]
    HevcQsv,

    [Description("libvpx")]
    Vp8,

    [Description("libvpx-vp9")]
    Vp9,

    [Description("libaom-av1")]
    LibAom,

    [Description("libsvtav1")]
    SvtAv1,

    [Description("librav1e")]
    Rav1E,
}