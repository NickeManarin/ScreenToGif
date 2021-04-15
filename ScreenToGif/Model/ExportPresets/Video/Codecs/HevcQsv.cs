using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class HevcQsv : VideoCodec
    {
        public HevcQsv()
        {
            Type = VideoCodecs.HevcQsv;
            Name = "HEVC QSV";
            Command = "hevc_qsv";

            IsHardwareAccelerated = true;
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
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.VeryFast, "S.SaveAs.VideoOptions.CodecPreset.VeryFast", "veryfast")
            };
            PixelFormats = new List<EnumItem<VideoPixelFormats>>
            {
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.P010Le, "", "P010Le", "p010le"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Qsv, "", "Qsv", "qsv")
            };
        }
    }
}