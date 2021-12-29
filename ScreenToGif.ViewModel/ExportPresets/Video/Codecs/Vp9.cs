using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

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
            new(VideoPixelFormats.Gbrp, "", "Gbrp", "gbrp"),
            new(VideoPixelFormats.Gbrp10Le, "", "Gbrp10Le", "gbrp10le"),
            new(VideoPixelFormats.Gbrp12Le, "", "Gbrp12Le", "gbrp12le"),
            new(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
            new(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
            new(VideoPixelFormats.Yuv440p, "", "Yuv440p", "yuv440p"),
            new(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
            new(VideoPixelFormats.Yuva420p, "", "Yuva420p", "yuva420p"),
            new(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
            new(VideoPixelFormats.Yuv420p12Le, "", "Yuv420p12Le", "yuv420p12le"),
            new(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
            new(VideoPixelFormats.Yuv422p12Le, "", "Yuv422p12Le", "yuv422p12le"),
            new(VideoPixelFormats.Yuv440p10Le, "", "Yuv440p10Le", "yuv440p10le"),
            new(VideoPixelFormats.Yuv440p12Le, "", "Yuv440p12Le", "yuv440p12le"),
            new(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le"),
            new(VideoPixelFormats.Yuv444p12Le, "", "Yuv444p12Le", "yuv444p12le"),
        };
    }
}