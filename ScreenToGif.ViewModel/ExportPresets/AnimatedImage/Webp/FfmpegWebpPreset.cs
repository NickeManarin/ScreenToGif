using System.Runtime.Serialization;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Webp;

/// <summary>
/// Webp FFmpeg encoder preset.
/// ffmpeg -h muxer=webp
/// ffmpeg -h encoder=libwebp_anim
/// </summary>
public class FfmpegWebpPreset : WebpPreset, IFfmpegPreset
{
    private VideoSettingsModes _settingsMode = VideoSettingsModes.Normal;
    private string _parameters = "-vsync passthrough \n{I} \n-c:v libwebp_anim \n-lossless 0 \n-quality 75 \n-loop 0 \n-f webp \n{O}";
    private VideoCodecPresets _codecPreset = VideoCodecPresets.Default;
    private int _quality = 75;
    private bool _lossless = true;
    private VideoPixelFormats _pixelFormat = VideoPixelFormats.Auto;
    private Framerates _framerate = Framerates.Auto;
    private double _customFramerate = 25d;
    private Vsyncs _vsync = Vsyncs.Passthrough;

    public VideoSettingsModes SettingsMode
    {
        get => _settingsMode;
        set => SetProperty(ref _settingsMode, value);
    }

    [DataMember(EmitDefaultValue = false)]
    public string Parameters
    {
        get => _parameters;
        set => SetProperty(ref _parameters, value);
    }

    public VideoCodecPresets CodecPreset
    {
        get => _codecPreset;
        set => SetProperty(ref _codecPreset, value);
    }

    public int Quality
    {
        get => _quality;
        set => SetProperty(ref _quality, value);
    }

    public bool Lossless
    {
        get => _lossless;
        set => SetProperty(ref _lossless, value);
    }

    public VideoPixelFormats PixelFormat
    {
        get => _pixelFormat;
        set => SetProperty(ref _pixelFormat, value);
    }

    public Framerates Framerate
    {
        get => _framerate;
        set => SetProperty(ref _framerate, value);
    }

    public double CustomFramerate
    {
        get => _customFramerate;
        set => SetProperty(ref _customFramerate, value);
    }

    public Vsyncs Vsync
    {
        get => _vsync;
        set => SetProperty(ref _vsync, value);
    }


    public FfmpegWebpPreset()
    {
        Encoder = EncoderTypes.FFmpeg;
        ImageId = "Vector.Ffmpeg";
        IsEncoderExpanded = false;
        RequiresFfmpeg = true;
    }

    public static List<FfmpegWebpPreset> Defaults => new()
    {
        new FfmpegWebpPreset
        {
            TitleKey = "S.Preset.Default.Title",
            DescriptionKey = "S.Preset.Default.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20)
        },

        new FfmpegWebpPreset
        {
            TitleKey = "S.Preset.Webp.Ffmpeg.High.Title",
            DescriptionKey = "S.Preset.Webp.Ffmpeg.High.Description",
            HasAutoSave = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20),

            Quality = 100,
            Parameters = "-vsync passthrough \n{I} \n-c:v libwebp_anim \n-lossless 0 \n-quality 100 \n-loop 0 \n-f webp \n{O}"
        }
    };
}