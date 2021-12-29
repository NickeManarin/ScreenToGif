using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Avi;

public class FfmpegAviPreset : AviPreset, IFfmpegPreset
{
    public FfmpegAviPreset()
    {
        Encoder = EncoderTypes.FFmpeg;
        ImageId = "Vector.Ffmpeg";
        RequiresFfmpeg = true;

        //Defaults.
        VideoCodec = VideoCodecs.Mpeg4;
        CodecPreset = VideoCodecPresets.None;
        HardwareAcceleration = HardwareAccelerationModes.Auto;
        Pass = 2;
        BitRate = 5;
        BitRateUnit = RateUnits.Megabits;
        PixelFormat = VideoPixelFormats.Yuv420p;
        Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v mpeg4 -vtag xvid \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-b:v 5M \n-pass 2 \n-f avi \n{O}";
    }

    public static FfmpegAviPreset Default => new()
    {
        TitleKey = "S.Preset.Default.Title",
        DescriptionKey = "S.Preset.Default.Description",
        HasAutoSave = true,
        IsSelectedForEncoder = true,
        IsDefault = true,
        CreationDate = new DateTime(2021, 02, 20),

        VideoCodec = VideoCodecs.Mpeg4,
        CodecPreset = VideoCodecPresets.None,
        HardwareAcceleration = HardwareAccelerationModes.Auto,
        Pass = 2,
        BitRate = 5,
        BitRateUnit = RateUnits.Megabits,
        PixelFormat = VideoPixelFormats.Yuv420p,
        Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v mpeg4 -vtag xvid \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-b:v 5M \n-pass 2 \n-f avi \n{O}"
    };
}