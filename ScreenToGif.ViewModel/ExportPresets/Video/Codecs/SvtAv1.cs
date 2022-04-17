using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

public class SvtAv1 : VideoCodec
{
    /// <summary>
    /// SVT-AV1, Scalable Video Technology for AV1.
    /// ffmpeg.exe -h encoder=libsvtav1
    /// </summary>
    public SvtAv1()
    {
        Type = VideoCodecs.SvtAv1;
        Name = "SVT-AV1";
        Command = "libsvtav1";
        Parameters = "";

        CanSetCrf = true;
        MinimumCrf = 0;
        MaximumCrf = 63;
        CodecPresets = new List<EnumItem<VideoCodecPresets>>
        {
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
            new(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le")
        };
    }
}