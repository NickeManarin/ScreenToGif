using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Webp;
using System.Runtime.Serialization;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Avif;

public class FfmpegAvifPreset : AvifPreset, IFfmpegPreset
{
    private VideoSettingsModes _settingsMode = VideoSettingsModes.Normal;
    private string _parameters = "-vsync passthrough \n{I} \n-c:v libaom-av1 \n-quality 75 \n-loop 0 \n-f avif \n{O}";
    private VideoCodecPresets _codecPreset = VideoCodecPresets.Default;
    private int _quality = 75; // A lower value may be better for the default
    private VideoCodecs _videoCodec = VideoCodecs.LibAom;
    private HardwareAccelerationModes _hardwareAcceleration = HardwareAccelerationModes.Auto;
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

    public VideoCodecs VideoCodec
    {
        get => _videoCodec;
        set => SetProperty(ref _videoCodec, value);
    }

    public HardwareAccelerationModes HardwareAcceleration
    {
        get => _hardwareAcceleration;
        set => SetProperty(ref _hardwareAcceleration, value);
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


    public FfmpegAvifPreset()
    {
        Encoder = EncoderTypes.FFmpeg;
        ImageId = "Vector.Ffmpeg";
        IsEncoderExpanded = false;
        RequiresFfmpeg = true;
    }

    public static List<FfmpegAvifPreset> Defaults => new()
    {
        new FfmpegAvifPreset
        {
            TitleKey = "S.Preset.Default.Title",
            DescriptionKey = "S.Preset.Default.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2024, 09, 18)
        },
    };
}