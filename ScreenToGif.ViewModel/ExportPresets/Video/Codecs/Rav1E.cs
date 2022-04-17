using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

public class Rav1E : VideoCodec
{
    /// <summary>
    /// Rav1E.
    /// ffmpeg.exe -h encoder=librav1e
    /// </summary>
    public Rav1E()
    {
        Type = VideoCodecs.Rav1E;
        Name = "Rav1E";
        Command = "librav1e";
        Parameters = "";

        CanSetCrf = true;
        MinimumCrf = 0;
        MaximumCrf = 63;
        CodecPresets = new List<EnumItem<VideoCodecPresets>>
        {
            new(VideoCodecPresets.Auto, "S.SaveAs.VideoOptions.CodecPreset.Default", "-1"),
            new(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", "0"),
            new(VideoCodecPresets.VerySlow, "S.SaveAs.VideoOptions.CodecPreset.VerySlow", "1"),
            new(VideoCodecPresets.Slower, "S.SaveAs.VideoOptions.CodecPreset.Slower", "2"),
            new(VideoCodecPresets.Slow, "S.SaveAs.VideoOptions.CodecPreset.Slow", "3"),
            new(VideoCodecPresets.Medium, "S.SaveAs.VideoOptions.CodecPreset.Medium", "4"),
            new(VideoCodecPresets.Fast, "S.SaveAs.VideoOptions.CodecPreset.Fast", "5"),
            new(VideoCodecPresets.Faster, "S.SaveAs.VideoOptions.CodecPreset.Faster", "6"),
            new(VideoCodecPresets.VeryFast, "S.SaveAs.VideoOptions.CodecPreset.VeryFast", "7"),
            new(VideoCodecPresets.SuperFast, "S.SaveAs.VideoOptions.CodecPreset.SuperFast", "8"),
            new(VideoCodecPresets.UltraFast, "S.SaveAs.VideoOptions.CodecPreset.UltraFast", "9")
        };
        PixelFormats = new List<EnumItem<VideoPixelFormats>>
        {
            new(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
            new(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
            new(VideoPixelFormats.Yuvj420p, "", "Yuvj420p", "yuvj420p"),
            new(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
            new(VideoPixelFormats.Yuvj422p, "", "Yuvj422p", "yuvj422p"),
            new(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
            new(VideoPixelFormats.Yuvj444p, "", "Yuvj444p", "yuvj444p"),
            new(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
            new(VideoPixelFormats.Yuv420p12Le, "", "Yuv420p12Le", "yuv420p12le"),
            new(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
            new(VideoPixelFormats.Yuv422p12Le, "", "Yuv422p12Le", "yuv422p12le"),
            new(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le"),
            new(VideoPixelFormats.Yuv444p12Le, "", "Yuv444p12Le", "yuv444p12le"),
        };
    }
}