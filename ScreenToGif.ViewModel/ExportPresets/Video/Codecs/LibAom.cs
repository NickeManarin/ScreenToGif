using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Codecs;

public class LibAom : VideoCodec
{
    /// <summary>
    /// Alliance for Open Media AV1.
    /// ffmpeg.exe -h encoder=libaom-av1
    /// </summary>
    public LibAom()
    {
        Type = VideoCodecs.LibAom;
        Name = "LibAOM AV1";
        Command = "libaom-av1";
        Parameters = "";

        CanSetCrf = true;
        MinimumCrf = 0;
        MaximumCrf = 63;
        CodecPresets = new List<EnumItem<VideoCodecPresets>>
        {
            new(VideoCodecPresets.None, "S.SaveAs.VideoOptions.CodecPreset.None", "")
        };
        PixelFormats = new List<EnumItem<VideoPixelFormats>>
        {
            new(VideoPixelFormats.Auto, "S.SaveAs.VideoOptions.PixelFormat.Auto", ""),
            new(VideoPixelFormats.Gbrp, "", "Gbrp", "gbrp"),
            new(VideoPixelFormats.Gbrp10Le, "", "Gbrp10Le", "gbrp10le"),
            new(VideoPixelFormats.Gbrp12Le, "", "Gbrp12Le", "gbrp12le"),
            new(VideoPixelFormats.Gray, "", "Gray", "gray"),
            new(VideoPixelFormats.Gray10Le, "", "Gray10LE", "gray10le"),
            new(VideoPixelFormats.Gray12Le, "", "Gray12LE", "gray12le"),
            new(VideoPixelFormats.Yuv420p, "", "Yuv420p", "yuv420p"),
            new(VideoPixelFormats.Yuv422p, "", "Yuv422p", "yuv422p"),
            new(VideoPixelFormats.Yuv420p10Le, "", "Yuv420p10Le", "yuv420p10le"),
            new(VideoPixelFormats.Yuv420p12Le, "", "Yuv420p12Le", "yuv420p12le"),
            new(VideoPixelFormats.Yuv422p10Le, "", "Yuv422p10Le", "yuv422p10le"),
            new(VideoPixelFormats.Yuv422p12Le, "", "Yuv422p12Le", "yuv422p12le"),
            new(VideoPixelFormats.Yuv444p10Le, "", "Yuv444p10Le", "yuv444p10le"),
            new(VideoPixelFormats.Yuv444p12Le, "", "Yuv444p12Le", "yuv444p12le"),
            new(VideoPixelFormats.Yuv444p, "", "Yuv444p", "yuv444p"),
            new(VideoPixelFormats.Yuva420p, "", "Yuva420p", "yuva420p")
        };
    }
}