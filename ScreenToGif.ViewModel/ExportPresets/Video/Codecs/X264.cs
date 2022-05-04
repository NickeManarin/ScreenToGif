using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

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
            new(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", ""),
            new(VideoCodecPresets.VerySlow, "S.SaveAs.VideoOptions.CodecPreset.VerySlow", "veryslow"),
            new(VideoCodecPresets.Slower, "S.SaveAs.VideoOptions.CodecPreset.Slower", "slower"),
            new(VideoCodecPresets.Slow, "S.SaveAs.VideoOptions.CodecPreset.Slow", "slow"),
            new(VideoCodecPresets.Medium, "S.SaveAs.VideoOptions.CodecPreset.Medium", "medium"),
            new(VideoCodecPresets.Fast, "S.SaveAs.VideoOptions.CodecPreset.Fast", "fast"),
            new(VideoCodecPresets.Faster, "S.SaveAs.VideoOptions.CodecPreset.Faster", "faster"),
            new(VideoCodecPresets.VeryFast, "S.SaveAs.VideoOptions.CodecPreset.VeryFast", "veryfast"),
            new(VideoCodecPresets.SuperFast, "S.SaveAs.VideoOptions.CodecPreset.SuperFast", "superfast"),
            new(VideoCodecPresets.UltraFast, "S.SaveAs.VideoOptions.CodecPreset.UltraFast", "ultrafast")
        };
        PixelFormats = new List<EnumItem<VideoPixelFormats>>
        {
            new(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
            new(VideoPixelFormats.Gray, "", "Gray", "gray"),
            new(VideoPixelFormats.Gray10Le, "", "Gray10Le", "gray10le"),
            new(VideoPixelFormats.Nv12, "", "Nv12", "nv12"),
            new(VideoPixelFormats.Nv16, "", "Nv16", "nv16"),
            new(VideoPixelFormats.Nv20Le, "", "Nv20Le", "nv20le"),
            new(VideoPixelFormats.Nv21, "", "Nv21", "nv21"),
            new(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
            new(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
            new(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
            new(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
            new(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
            new(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le"),
            new(VideoPixelFormats.Yuvj420p, "", "Yuvj420p", "yuvj420p"),
            new(VideoPixelFormats.Yuvj422p, "", "Yuvj422p", "yuvj422p"),
            new(VideoPixelFormats.Yuvj444p, "", "Yuvj444p", "yuvj444p")
        };
    }
}