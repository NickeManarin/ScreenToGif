using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class Vp9 : VideoCodec
    {
        public Vp9()
        {
            Type = VideoCodecs.Vp9;
            Name = "VP9";
            Command = "libvpx-vp9";
            Parameters = "-tile-columns 6 -frame-parallel 1 -auto-alt-ref 1 -lag-in-frames 25";

            CanSetCrf = true;
            MinimumCrf = 4;
            MaximumCrf = 63;
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
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gbrp, "", "Gbrp", "gbrp"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gbrp10Le, "", "Gbrp10Le", "gbrp10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Gbrp12Le, "", "Gbrp12Le", "gbrp12le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv440p, "", "Yuv440p", "yuv440p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuva420p, "", "Yuva420p", "yuva420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p12Le, "", "Yuv420p12Le", "yuv420p12le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p12Le, "", "Yuv422p12Le", "yuv422p12le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv440p10Le, "", "Yuv440p10Le", "yuv440p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv440p12Le, "", "Yuv440p12Le", "yuv440p12le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv444p12Le, "", "Yuv444p12Le", "yuv444p12le"),
            };
        }
    }
}