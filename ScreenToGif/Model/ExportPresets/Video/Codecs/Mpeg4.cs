using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    /// <summary>
    /// https://trac.ffmpeg.org/wiki/Encode/MPEG-4
    /// </summary>
    public class Mpeg4 : VideoCodec
    {
        public Mpeg4()
        {
            Type = VideoCodecs.Mpeg4;
            Name = "MPEG-4";
            Command = "mpeg4";
            Parameters = "-vtag xvid";

            CodecPresets = new List<EnumItem<VideoCodecPresets>>
            {
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", "")
            };
            PixelFormats = new List<EnumItem<VideoPixelFormats>>
            {
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p")
            };
        }
    }
}