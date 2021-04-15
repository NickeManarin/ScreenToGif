using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class X265 : VideoCodec
    {
        public X265()
        {
            Type = VideoCodecs.X265;
            Name = "x265";
            Command = "libx265";

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
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gbrp, "", "Gbrp", "gbrp"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gbrp10Le, "", "Gbrp10Le", "gbrp10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gray, "", "Gray", "gray"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gray10Le, "", "Gray10Le", "gray10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuvj420p, "", "Yuvj420p", "yuvj420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuvj422p, "", "Yuvj422p", "yuvj422p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuvj444p, "", "Yuvj444p", "yuvj444p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le")
            };
        }
    }
}