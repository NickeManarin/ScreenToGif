using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

public class H264Amf : VideoCodec
{
    public H264Amf()
    {
        Type = VideoCodecs.H264Amf;
        Name = "H264 AMF";
        Command = "h264_amf";

        IsHardwareAccelerated = true;
        CanSetCrf = true;
        MinimumCrf = 0;
        MaximumCrf = 51;
        CodecPresets = new List<EnumItem<VideoCodecPresets>>
        {
            new(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", ""),
            new(VideoCodecPresets.Quality, "S.SaveAs.VideoOptions.CodecPreset.Quality", "quality"),
            new(VideoCodecPresets.Balanced, "S.SaveAs.VideoOptions.CodecPreset.Balanced", "balanced"),
            new(VideoCodecPresets.Speed, "S.SaveAs.VideoOptions.CodecPreset.Speed", "speed")
        };
        PixelFormats = new List<EnumItem<VideoPixelFormats>>
        {
            new(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
            new(VideoPixelFormats.D3D11, "", "D3D11", "d3d11"),
            new(VideoPixelFormats.Dxva2Vld, "", "Dxva2Vld", "dxva2_vld"),
            new(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
            new(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p")
        };
    }
}