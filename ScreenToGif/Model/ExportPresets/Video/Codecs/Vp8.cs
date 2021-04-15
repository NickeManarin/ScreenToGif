using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class Vp8 : VideoCodec
    {
        public Vp8()
        {
            Type = VideoCodecs.Vp8;
            Name = "VP8";
            Command = "libvpx";

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
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuva420p, "", "Yuva420p", "yuva420p")
            };
        }
    }
}