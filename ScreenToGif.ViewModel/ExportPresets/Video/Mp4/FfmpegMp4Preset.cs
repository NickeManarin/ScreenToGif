using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Mp4;

public class FfmpegMp4Preset : Mp4Preset, IFfmpegPreset
{
    public FfmpegMp4Preset()
    {
        Encoder = EncoderTypes.FFmpeg;
        ImageId = "Vector.Ffmpeg";
        RequiresFfmpeg = true;

        VideoCodec = VideoCodecs.X264;
        CodecPreset = VideoCodecPresets.Fast;
        HardwareAcceleration = HardwareAccelerationModes.Auto;
        Pass = 1;
        ConstantRateFactor = 23;
        PixelFormat = VideoPixelFormats.Yuv420p;
        Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 23 \n-f mp4 \n{O}";
    }

    public static List<FfmpegMp4Preset> Defaults => new()
    {
        new FfmpegMp4Preset
        {
            TitleKey = "S.Preset.Default.Title",
            DescriptionKey = "S.Preset.Default.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            VideoCodec = VideoCodecs.X264,
            CodecPreset = VideoCodecPresets.Fast,
            HardwareAcceleration = HardwareAccelerationModes.Auto,
            Pass = 1,
            ConstantRateFactor = 23,
            PixelFormat = VideoPixelFormats.Yuv420p,
            Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 23 \n-f mp4 \n{O}"
        },

        new FfmpegMp4Preset
        {
            TitleKey = "S.Preset.Twitter.Title",
            DescriptionKey = "S.Preset.Twitter.Description",
            HasAutoSave = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 05, 09),

            VideoCodec = VideoCodecs.X264,
            CodecPreset = VideoCodecPresets.Fast,
            HardwareAcceleration = HardwareAccelerationModes.Auto,
            Pass = 2,
            ConstantRateFactor = null,
            PixelFormat = VideoPixelFormats.Yuv420p,
            Vsync = Vsyncs.Vfr,
            Framerate = Framerates.Custom,
            CustomFramerate = 40,
            BitRate = 15,
            BitRateUnit = RateUnits.Megabits,
            MaximumBitRate = 25,
            MaximumBitRateUnit = RateUnits.Megabits,
            RateControlBuffer = 8,
            RateControlBufferUnit = RateUnits.Megabits,
            Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx264 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-b:v 15M \n-maxrate 25M \n-bufsize 8M \n-pass 2 \n-f mp4 \n{O}"
        },

        new FfmpegMp4Preset
        {
            TitleKey = "S.Preset.Hevc.Title",
            DescriptionKey = "S.Preset.Hevc.Description",
            HasAutoSave = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            VideoCodec = VideoCodecs.X265,
            CodecPreset = VideoCodecPresets.Fast,
            HardwareAcceleration = HardwareAccelerationModes.Auto,
            Pass = 1,
            ConstantRateFactor = 28,
            PixelFormat = VideoPixelFormats.Yuv420p,
            Parameters = "-vsync passthrough \n-hwaccel auto \n{I} \n-c:v libx265 \n-preset fast \n-pix_fmt yuv420p \n-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" \n-crf 28 \n-f mp4 \n{O}"
        },
    };
}