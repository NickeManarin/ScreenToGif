using System.Collections.Generic;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video.Codecs
{
    public class HevcAmf : VideoCodec
    {
        public HevcAmf()
        {
            Type = VideoCodecs.HevcAmf;
            Name = "HEVC AMF";
            Command = "hevc_amf";

            IsHardwareAccelerated = true;
            CanSetCrf = true;
            MinimumCrf = 0;
            MaximumCrf = 51;
            CodecPresets = new List<EnumItem<VideoCodecPresets>>
            {
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", ""),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Quality, "S.SaveAs.VideoOptions.CodecPreset.Quality", "quality"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Balanced, "S.SaveAs.VideoOptions.CodecPreset.Balanced", "balanced"),
                new EnumItem<VideoCodecPresets>(VideoCodecPresets.Speed, "S.SaveAs.VideoOptions.CodecPreset.Speed", "speed")
            };
            PixelFormats = new List<EnumItem<VideoPixelFormats>>
            {
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.D3D11, "", "D3D11", "d3d11"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Dxva2Vld, "", "Dxva2Vld", "dxva2_vld"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
                new EnumItem<VideoPixelFormats>(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
            };
        }
    }
}