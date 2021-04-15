using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class X264 : VideoCodec
    {
        public X264()
        {
            Type = VideoCodecs.X264;
            Name = "x264";
            Command = "libx264";

            CanSetCrf = true;
            MinimumCrf = 0;
            MaximumCrf = 51;
            CodecPresets = new List<EnumItem<VideoCodecPresets>>
            {
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", ""),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.VerySlow, "S.SaveAs.VideoOptions.CodecPreset.VerySlow", "veryslow"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Slower, "S.SaveAs.VideoOptions.CodecPreset.Slower", "slower"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Slow, "S.SaveAs.VideoOptions.CodecPreset.Slow", "slow"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Medium, "S.SaveAs.VideoOptions.CodecPreset.Medium", "medium"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Fast, "S.SaveAs.VideoOptions.CodecPreset.Fast", "fast"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Faster, "S.SaveAs.VideoOptions.CodecPreset.Faster", "faster"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.VeryFast, "S.SaveAs.VideoOptions.CodecPreset.VeryFast", "veryfast"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.SuperFast, "S.SaveAs.VideoOptions.CodecPreset.SuperFast", "superfast"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.UltraFast, "S.SaveAs.VideoOptions.CodecPreset.UltraFast", "ultrafast")
            };
            PixelFormats = new List<EnumItem<VideoPixelFormats>>
            {
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gray, "", "Gray", "gray"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gray10Le, "", "Gray10Le", "gray10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv16, "", "Nv16", "nv16"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv20Le, "", "Nv20Le", "nv20le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv21, "", "Nv21", "nv21"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuvj420p, "", "Yuvj420p", "yuvj420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuvj422p, "", "Yuvj422p", "yuvj422p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuvj444p, "", "Yuvj444p", "yuvj444p")
            };
        }
    }
}