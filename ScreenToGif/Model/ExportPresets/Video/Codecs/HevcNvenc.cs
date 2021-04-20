using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    /// <summary>
    /// https://developer.nvidia.com/blog/introducing-video-codec-sdk-10-presets/
    /// </summary>
    public class HevcNvenc : VideoCodec
    {
        public HevcNvenc()
        {
            Type = VideoCodecs.HevcNvenc;
            Name = "HEVC NVENC";
            Command = "hevc_nvenc";

            IsHardwareAccelerated = true;
            CanSetCrf = true;
            MinimumCrf = 0;
            MaximumCrf = 51;
            CodecPresets = new List<EnumItem<VideoCodecPresets>>
            {
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", ""),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Default, "S.SaveAs.VideoOptions.CodecPreset.Default", "default"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Lossless, "S.SaveAs.VideoOptions.CodecPreset.Lossless", "lossless"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.LosslessHP, "S.SaveAs.VideoOptions.CodecPreset.LosslessHp", "losslesshp"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Slow, "S.SaveAs.VideoOptions.CodecPreset.Slow", "slow"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Medium, "S.SaveAs.VideoOptions.CodecPreset.Medium", "medium"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Fast, "S.SaveAs.VideoOptions.CodecPreset.Fast", "fast"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.HP, "S.SaveAs.VideoOptions.CodecPreset.Hp", "hp"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.HQ, "S.SaveAs.VideoOptions.CodecPreset.Hq", "hq"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.BD, "S.SaveAs.VideoOptions.CodecPreset.Bd", "bd"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.LowLatency, "S.SaveAs.VideoOptions.CodecPreset.LowLatency", "ll"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.LowLatencyHP, "S.SaveAs.VideoOptions.CodecPreset.LowLatencyHp", "llhp"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.LowLatencyHQ, "S.SaveAs.VideoOptions.CodecPreset.LowLatencyHq", "llhq")
            };
            PixelFormats = new List<EnumItem<VideoPixelFormats>>
            {
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Bgr0, "", "Bgr0", "bgr0"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Cuda, "", "Cuda", "cuda"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.D3D11, "", "D3D11", "d3d11"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.P010Le, "", "P010Le", "p010le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p16Le, "", "Yuv444p16Le", "yuv444p16le")
            };
        }
    }
}