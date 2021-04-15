using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class Mpeg2 : VideoCodec
    {
        public Mpeg2()
        {
            Type = VideoCodecs.Mpeg2;
            Name = "MPEG-2";
            Command = "mpeg2video";

            CodecPresets = new List<EnumItem<VideoCodecPresets>>
            {
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", "")
            };
            PixelFormats = new List<EnumItem<VideoPixelFormats>>
            {
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p")
            };
        }
    }
}