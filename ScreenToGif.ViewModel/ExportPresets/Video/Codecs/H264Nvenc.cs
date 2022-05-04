using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

/// <summary>
/// https://developer.nvidia.com/blog/introducing-video-codec-sdk-10-presets/
/// </summary>
public class H264Nvenc : VideoCodec
{
    public H264Nvenc()
    {
        Type = VideoCodecs.H264Nvenc;
        Name = "H264 NVENC";
        Command = "h264_nvenc";

        IsHardwareAccelerated = true;
        CanSetCrf = true;
        MinimumCrf = 0;
        MaximumCrf = 51;
        CodecPresets = new List<EnumItem<VideoCodecPresets>>
        {
            new(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", ""),
            new(VideoCodecPresets.Default, "S.SaveAs.VideoOptions.CodecPreset.Default", "default"),
            new(VideoCodecPresets.Lossless, "S.SaveAs.VideoOptions.CodecPreset.Lossless", "lossless"),
            new(VideoCodecPresets.LosslessHP, "S.SaveAs.VideoOptions.CodecPreset.LosslessHp", "losslesshp"),
            new(VideoCodecPresets.Slow, "S.SaveAs.VideoOptions.CodecPreset.Slow", "slow"),
            new(VideoCodecPresets.Medium, "S.SaveAs.VideoOptions.CodecPreset.Medium", "medium"),
            new(VideoCodecPresets.Fast, "S.SaveAs.VideoOptions.CodecPreset.Fast", "fast"),
            new(VideoCodecPresets.HP, "S.SaveAs.VideoOptions.CodecPreset.Hp", "hp"),
            new(VideoCodecPresets.HQ, "S.SaveAs.VideoOptions.CodecPreset.Hq", "hq"),
            new(VideoCodecPresets.BD, "S.SaveAs.VideoOptions.CodecPreset.Bd", "bd"),
            new(VideoCodecPresets.LowLatency, "S.SaveAs.VideoOptions.CodecPreset.LowLatency", "ll"),
            new(VideoCodecPresets.LowLatencyHP, "S.SaveAs.VideoOptions.CodecPreset.LowLatencyHp", "llhp"),
            new(VideoCodecPresets.LowLatencyHQ, "S.SaveAs.VideoOptions.CodecPreset.LowLatencyHq", "llhq")
        };
        PixelFormats = new List<EnumItem<VideoPixelFormats>>
        {
            new(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
            new(VideoPixelFormats.Bgr0, "", "Bgr0", "bgr0"),
            new(VideoPixelFormats.Cuda, "", "Cuda", "cuda"),
            new(VideoPixelFormats.D3D11, "", "D3D11", "d3d11"),
            new(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
            new(VideoPixelFormats.P010Le, "", "P010Le", "p010le"),
            new(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
            new(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
            new(VideoPixelFormats.Yuv444p16Le, "", "Yuv444p16Le", "yuv444p16le"),
        };
    }
}