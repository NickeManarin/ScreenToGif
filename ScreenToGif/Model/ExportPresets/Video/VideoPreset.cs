using System.Runtime.Serialization;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.Video
{
    public class VideoPreset : ExportPreset
    {
        private VideoSettingsMode _settingsMode;
        private string _parameters;
        private VideoCodecs _videoCodec;
        private VideoCodecPresets _codecPreset;
        private HardwareAcceleration _hardwareAcceleration = HardwareAcceleration.Auto;
        private int _pass = 1;
        private bool _isVariableBitRate = false;
        private int? _constantRateFactor;
        private decimal _bitRate;
        private int _qualityLevel = 5;
        private RateUnit _bitRateUnit = RateUnit.Megabits;
        private decimal _minimumBitRate;
        private RateUnit _minimumBitRateUnit = RateUnit.Megabits;
        private decimal _maximumBitRate;
        private RateUnit _maximumBitRateUnit = RateUnit.Megabits;
        private decimal _rateControlBuffer;
        private RateUnit _rateControlBufferUnit = RateUnit.Megabits;
        private VideoPixelFormats _pixelFormat;
        private Framerates _framerate = Framerates.Auto;
        private decimal _customFramerate = 25M;
        private Vsyncs _vsync = Vsyncs.Passthrough;
        private bool _isAncientContainer;


        public VideoSettingsMode SettingsMode
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

        
        public VideoCodecs VideoCodec
        {
            get => _videoCodec;
            set => SetProperty(ref _videoCodec, value);
        }

        public VideoCodecPresets CodecPreset
        {
            get => _codecPreset;
            set => SetProperty(ref _codecPreset, value);
        }

        /// <summary>
        /// Hardware acceleration mode.
        /// https://trac.ffmpeg.org/wiki/HWAccelIntro
        /// </summary>
        public HardwareAcceleration HardwareAcceleration
        {
            get => _hardwareAcceleration;
            set => SetProperty(ref _hardwareAcceleration, value);
        }
        
        public int Pass
        {
            get => _pass;
            set => SetProperty(ref _pass, value);
        }

        public bool IsVariableBitRate
        {
            get => _isVariableBitRate;
            set => SetProperty(ref _isVariableBitRate, value);
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ConstantRateFactor
        {
            get => _constantRateFactor;
            set => SetProperty(ref _constantRateFactor, value);
        }

        public decimal BitRate
        {
            get => _bitRate;
            set => SetProperty(ref _bitRate, value);
        }

        /// <summary>
        /// Quality level (-q:v, -qscale:v), in use when having the bitrate mode set to variable.
        /// </summary>
        public int QualityLevel
        {
            get => _qualityLevel;
            set => SetProperty(ref _qualityLevel, value);
        }

        public RateUnit BitRateUnit
        {
            get => _bitRateUnit;
            set => SetProperty(ref _bitRateUnit, value);
        }

        public decimal MinimumBitRate
        {
            get => _minimumBitRate;
            set => SetProperty(ref _minimumBitRate, value);
        }

        public RateUnit MinimumBitRateUnit
        {
            get => _minimumBitRateUnit;
            set => SetProperty(ref _minimumBitRateUnit, value);
        }

        public decimal MaximumBitRate
        {
            get => _maximumBitRate;
            set => SetProperty(ref _maximumBitRate, value);
        }

        public RateUnit MaximumBitRateUnit
        {
            get => _maximumBitRateUnit;
            set => SetProperty(ref _maximumBitRateUnit, value);
        }

        public decimal RateControlBuffer
        {
            get => _rateControlBuffer;
            set => SetProperty(ref _rateControlBuffer, value);
        }

        public RateUnit RateControlBufferUnit
        {
            get => _rateControlBufferUnit;
            set => SetProperty(ref _rateControlBufferUnit, value);
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

        public decimal CustomFramerate
        {
            get => _customFramerate;
            set => SetProperty(ref _customFramerate, value);
        }

        public Vsyncs Vsync
        {
            get => _vsync;
            set => SetProperty(ref _vsync, value);
        }

        public bool IsAncientContainer
        {
            get => _isAncientContainer;
            set => SetProperty(ref _isAncientContainer, value);
        }


        public VideoPreset()
        {
            OutputFilenameKey = "S.Preset.Filename.Video";
            IsEncoderExpanded = false;
        }   
    }
}