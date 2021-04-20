using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Determines the app's theme.
    /// </summary>
    public enum AppTheme
    {
        Light,
        Medium,
        Dark,
        VeryDark,
    }

    /// <summary>
    /// The direction in which the window should be resized.
    /// </summary>
    public enum ResizeDirection
    {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8
    }

    /// <summary>
    /// Determines how the past bahaves.
    /// </summary>
    public enum PasteBehavior
    {
        /// <summary>
        /// It will paste before the selected frame.
        /// </summary>
        BeforeSelected,

        /// <summary>
        /// It will paste after the selected frame.
        /// </summary>
        AfterSelected
    }

    /// <summary>
    /// Animation export type.
    /// </summary>
    public enum Export
    {
        /// <summary>
        /// Graphics Interchange Format.
        /// </summary>
        Gif,

        /// <summary>
        /// Animated Portable Network Graphics.
        /// </summary>
        Apng,

        /// <summary>
        /// Web Picture.
        /// </summary>
        Webp,

        /// <summary>
        /// Better portable graphics.
        /// </summary>
        Bpg,


        /// <summary>
        /// Audio Video Interleaved.
        /// </summary>
        Avi,
        
        /// <summary>
        /// Matroska.
        /// </summary>
        Mkv,
        
        /// <summary>
        /// Quicktime movie.
        /// </summary>
        Mov,

        /// <summary>
        /// MPEG-4 Part 14.
        /// </summary>
        Mp4,
        
        /// <summary>
        /// Web Movie.
        /// </summary>
        Webm,
        

        /// <summary>
        /// Bitmap.
        /// </summary>
        Bmp,

        /// <summary>
        /// Joint Photographic Experts Group.
        /// </summary>
        Jpeg,

        /// <summary>
        /// Portable Network Graphics.
        /// </summary>
        Png,
        

        /// <summary>
        /// Project file, .stg or .zip.
        /// </summary>
        Stg,

        /// <summary>
        /// Photoshop file.
        /// </summary>
        Psd,

        /// <summary>
        /// Compressed file.
        /// Not in directly use by the encoder, but as an option for the images and the project.
        /// </summary>
        Zip
    }

    /// <summary>
    /// Partial export type.
    /// </summary>
    public enum PartialExportType
    {
        /// <summary>
        /// An expression like '4, 5, 9 - 11'.
        /// </summary>
        FrameExpression,
        
        /// <summary>
        /// Start and end frame number.
        /// </summary>
        FrameRange,

        /// <summary>
        /// Start and end times.
        /// </summary>
        TimeRange,

        /// <summary>
        /// All selected frames in the timeline.
        /// </summary>
        Selection
    }

    /// <summary>
    /// Upload destination type.
    /// </summary>
    public enum UploadType
    {
        NotDefined = 0,
        Imgur,
        Gfycat,
        Yandex,
        YouTrack,
        Custom
    }

    /// <summary>
    /// The types of Panel of the Editor window.
    /// Positive values means that there's no preview overlay.
    /// </summary>
    public enum PanelType
    {
        /// <summary>
        /// Save As Panel.
        /// </summary>
        SaveAs = 1,

        /// <summary>
        /// New Animation Panel.
        /// </summary>
        NewAnimation = 2,

        /// <summary>
        /// Clipboard Panel.
        /// </summary>
        Clipboard = 3,

        /// <summary>
        /// Resize Panel.
        /// </summary>
        Resize = 4,

        /// <summary>
        /// Flip/Rotate Panel.
        /// </summary>
        FlipRotate = 5,

        /// <summary>
        /// Override Delay Panel.
        /// </summary>
        OverrideDelay = 6,

        /// <summary>
        /// Change Delay Panel.
        /// </summary>
        IncreaseDecreaseDelay = 7,

        ScaleDelay = 8,

        /// <summary>
        /// Fade Transition Panel.
        /// </summary>
        Fade = 9,

        /// <summary>
        /// Slide Transition Panel.
        /// </summary>
        Slide = 10,

        /// <summary>
        /// Reduce Frame Count Panel.
        /// </summary>
        ReduceFrames = 11,

        /// <summary>
        /// Load Recent Panel.
        /// </summary>
        LoadRecent = 12,

        /// <summary>
        /// Remove Duplicates Panel.
        /// </summary>
        RemoveDuplicates = 13,

        /// <summary>
        /// Mouse Clicks Panel.
        /// </summary>
        MouseClicks = 14,

        /// <summary>
        /// Crop Panel.
        /// </summary>
        Crop = -1,

        /// <summary>
        /// Caption Panel.
        /// </summary>
        Caption = -2,

        /// <summary>
        /// Free Text Panel.
        /// </summary>
        FreeText = -3,

        /// <summary>
        /// Title Frame Panel.
        /// </summary>
        TitleFrame = -4,

        /// <summary>
        /// Free Drawing Panel.
        /// </summary>
        FreeDrawing = -5,

        /// <summary>
        /// Shapes Panel.
        /// </summary>
        Shapes = -6,

        /// <summary>
        /// Watermark Panel.
        /// </summary>
        Watermark = -7,

        /// <summary>
        /// Border Panel.
        /// </summary>
        Border = -8,

        /// <summary>
        /// Cinemagraph Panel.
        /// </summary>
        Cinemagraph = -9,

        /// <summary>
        /// Progress Panel.
        /// </summary>
        Progress = -10,

        /// <summary>
        /// Key Strokes Panel.
        /// </summary>
        KeyStrokes = -11,

        /// <summary>
        /// Obfuscate Panel.
        /// </summary>
        Obfuscate = -12,

        /// <summary>
        /// Shadow Panel.
        /// </summary>
        Shadow = -13,
    }

    /// <summary>
    /// The type of fade transition.
    /// </summary>
    public enum FadeToType
    {
        /// <summary>
        /// The next frame of the recording.
        /// </summary>
        NextFrame,

        /// <summary>
        /// A solid color.
        /// </summary>
        Color
    }

    /// <summary>
    /// Transition animation.
    /// </summary>
    public enum SlideFrom
    {
        Right,
        Top,
        Left,
        Bottom
    }

    /// <summary>
    /// Stage status of the recording process.
    /// </summary>
    [Flags]
    public enum Stage
    {
        /// <summary>
        /// Recording stopped, but selecting the region to record.
        /// </summary>
        [Obsolete]
        SelectingRegion = 0, //Removed later.



        /// <summary>
        /// Recording stopped.
        /// </summary>
        Stopped = 1, //1 << 0, 0b_000001

        /// <summary>
        /// Recording active.
        /// </summary>
        Recording = 2, //1 << 1, 0b_000010

        /// <summary>
        /// Recording paused.
        /// </summary>
        Paused = 4, //1 << 2, 0b_000100

        /// <summary>
        /// Pre start countdown active.
        /// </summary>
        PreStarting = 8, //1 << 3, 0b_001000

        /// <summary>
        /// The recording is being discarded.
        /// </summary>
        Discarding = 16, //1 << 4, 0b_010000



        /// <summary>
        /// Single shot mode.
        /// </summary>
        [Obsolete]
        Snapping = 32, //1 << 5, 0b_100000 //Remove later.
    }

    /// <summary>
    /// Encoding status.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Processing encoding/uploading status.
        /// </summary>
        Processing,

        /// <summary>
        /// The Encoding was canceled. So aparently "cancelled" (with two L's) is also a valid grammar. Huh, that's strange.
        /// </summary>
        Canceled,

        /// <summary>
        /// An error hapenned with the encoding process.
        /// </summary>
        Error,

        /// <summary>
        /// Encoding done.
        /// </summary>
        Completed,

        /// <summary>
        /// File deleted or Moved.
        /// </summary>
        FileDeletedOrMoved
    }

    /// <summary>
    /// Type of the Flip/Rotate action.
    /// </summary>
    public enum FlipRotateType
    {
        FlipHorizontal,
        FlipVertical,
        RotateRight90,
        RotateLeft90
    }

    /// <summary>
    /// Exit actions after closing the Recording Window.
    /// </summary>
    public enum ExitAction
    {
        /// <summary>
        /// Return to the StartUp Window.
        /// </summary>
        Return = 0,

        /// <summary>
        /// Something was recorded. Go to the Editor.
        /// </summary>
        Recorded = 1,

        /// <summary>
        /// Exit the application.
        /// </summary>
        Exit = 2,
    }

    /// <summary>
    /// Type of delay change action.
    /// </summary>
    public enum DelayChangeType
    {
        Override,
        IncreaseDecrease,
        Scale
    }

    /// <summary>
    /// Type of the encoder.
    /// </summary>
    public enum EncoderType
    {
        ScreenToGif, //Gif, Apng
        System, //Gif, Video
        FFmpeg, //Gif, Webp, Apng, Video
        Gifski //Gif
    }

    /// <summary>
    /// Type of color quantization methods of the gif encoder.
    /// </summary>
    public enum ColorQuantizationType
    {
        Neural = 0,
        Octree = 1,
        MedianCut = 2,
        Grayscale = 3,
        MostUsed = 4,
        Palette = 5,
    }
    
    /// <summary>
    /// Type of the progress indicator.
    /// </summary>
    public enum ProgressType
    {
        Bar,
        Text
    }

    /// <summary>
    /// Specifies the placement of the adorner in related to the adorned control.
    /// </summary>
    public enum AdornerPlacement
    {
        Inside,
        Outside
    }

    /// <summary>
    /// Specifies the type of copy operation.
    /// </summary>
    public enum CopyType
    {
        File,
        FolderPath,
        FilePath,
        Link
    }
    
    /// <summary>
    /// Specifies the status of the image card control.
    /// </summary>
    public enum ExtrasStatus
    {
        NotAvailable,
        Available,
        Processing,
        Ready,
        Error
    }

    /// <summary>
    /// Specifies the type of frame delay adjustment for the 'Reduce Framerate'.
    /// </summary>
    public enum ReduceDelayType
    {
        DontAdjust = 0,
        Previous = 1,
        Evenly = 2
    }

    /// <summary>
    /// Specifies the type of frame removal.
    /// </summary>
    public enum DuplicatesRemovalType
    {
        First = 0,
        Last = 1
    }

    /// <summary>
    /// Specifies the type of frame delay adjustment.
    /// </summary>
    public enum DuplicatesDelayType
    {
        DontAdjust = 0,
        Average = 1,
        Sum = 2
    }

    /// <summary>
    /// Event flags for mouse-related events.
    /// </summary>
    public enum MouseEventType
    {
        MouseMove,
        IconRightMouseDown,
        IconLeftMouseDown,
        IconRightMouseUp,
        IconLeftMouseUp,
        IconMiddleMouseDown,
        IconMiddleMouseUp,
        IconLeftDoubleClick
    }


    /// <summary>
    /// Dialog Icons.
    /// </summary>
    public enum Icons
    {
        /// <summary>
        /// Information. Blue.
        /// </summary>
        Info,

        /// <summary>
        /// Warning, yellow.
        /// </summary>
        Warning,

        /// <summary>
        /// Error, red.
        /// </summary>
        Error,

        /// <summary>
        /// Success, green.
        /// </summary>
        Success,

        /// <summary>
        /// A question mark, blue.
        /// </summary>
        Question,
    }

    /// <summary>
    /// The proxy method, used for uploading files.
    /// </summary>
    public enum ProxyType
    {
        Disabled = 0,
        Manual = 1,
        System = 2
    }

    /// <summary>
    /// The upload service.
    /// </summary>
    public enum UploadService
    {
        None = 0,
        ImgurAnonymous = 1,
        Imgur = 2,
        GfycatAnonymous = 3,
        Gfycat = 4,
        Yandex = 5,
    }

    public enum StatusType : int
    {
        None = 0,
        Info,
        Update,
        Warning,
        Error
    }

    public enum StatusReasons : int
    {
        None,
        EmptyProperty,
        InvalidState,
        FileAlreadyExists,
        MissingFfmpeg,
        MissingGifski,
        UploadServiceUnauthorized
    }

    /// <summary>
    /// The types of source of project creation.
    /// </summary>
    public enum ProjectByType
    {
        Unknown = 0,
        ScreenRecorder = 1,
        WebcamRecorder = 2,
        BoardRecorder = 3,
        Editor = 4
    }

    /// <summary>
    /// The types of drawings.
    /// </summary>
    public enum DrawingModeType
    {
        None = 0,
        Ink,
        Select,
        EraseByPoint,
        EraseByObject,
        Rectangle,
        Circle,
        Triangle,
        Arrow,
        Baloon,
    }

    /// <summary>
    /// Delay update type.
    /// </summary>
    public enum DelayUpdateType
    {
        Override = 0,
        IncreaseDecrease = 1,
        Scale = 2
    }

    /// <summary>
    /// Type of capture frequency mode for the screen recorder.
    /// </summary>
    public enum CaptureFrequency
    {
        Manual,
        Interaction,
        PerSecond,
        PerMinute,
        PerHour
    }

    public enum ObfuscationMode
    {
        Pixelation,
        Blur,
        Darken,
        Lighten
    }

    /// <summary>
    /// Scaling quality options for resizing
    /// This enum is a subset of <seealso cref="System.Windows.Media.BitmapScalingMode"/>.
    /// It is used to expose this enum to the Editor and choose which options are availabe
    /// </summary>
    public enum ScalingMethod
    {
        Fant = System.Windows.Media.BitmapScalingMode.Fant,
        Linear = System.Windows.Media.BitmapScalingMode.Linear,
        NearestNeighbor = System.Windows.Media.BitmapScalingMode.NearestNeighbor
    }

    /// <summary>
    /// The type of capture area selection.
    /// </summary>
    public enum ModeType
    {
        Region = 0,
        Window = 1,
        Fullscreen = 2
    }

    public enum VideoSettingsMode
    {
        Normal,
        Advanced
    }

    /// <summary>
    /// Png prediction methods used by FFmpeg.
    /// </summary>
    public enum PredictionMethods
    {
        None,
        Sub,
        Up,
        Avg,
        Paeth,
        Mixed
    }

    /// <summary>
    /// Dither methods, currently being used by FFmpeg.
    /// </summary>
    public enum DitherMethods
    {
        [Description("bayer")]
        Bayer,

        [Description("heckbert")]
        Heckbert,

        [Description("floyd_steinberg")]
        FloydSteinberg,

        [Description("sierra2")]
        Sierra2,

        [Description("sierra2_4a")]
        Sierra2Lite,
    }

    public enum VideoCodecs
    {
        NotSelected,

        [Description("mpeg2video")]
        Mpeg2,

        [Description("mpeg4")]
        Mpeg4,

        [Description("libx264")]
        X264,

        [Description("h264_amf")]
        H264Amf,

        [Description("h264_nvenc")]
        H264Nvenc,

        [Description("h264_qsv")]
        H264Qsv,

        [Description("libx265")]
        X265,

        [Description("hevc_amf")]
        HevcAmf,

        [Description("hevc_nvenc")]
        HevcNvenc,

        [Description("hevc_qsv")]
        HevcQsv,

        [Description("libvpx")]
        Vp8,

        [Description("libvpx-vp9")]
        Vp9
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VideoCodecPresets
    {
        NotSelected,
        None,

        VerySlow,
        Slower,
        Slow,
        Medium,
        Fast,
        Faster,
        VeryFast,
        SuperFast,
        UltraFast,

        Quality,
        Balanced,
        Speed,

        Default,
        Lossless,
        LosslessHP,
        HP,
        HQ,
        BD,
        LowLatency,
        LowLatencyHP,
        LowLatencyHQ,
        
        Picture, //Digital picture, like portrait, inner shot.
        Photo, //Outdoor photograph, with natural lighting.
        Drawing, //Hand or line drawing, with high-contrast details.
        Icon, //Small-sized colorful images.
        Text //Text-like.
    }

    public enum HardwareAcceleration
    {
        Off, //Only lets you select non-hardware backed encoders. 
        On, //Lets you select hardware backed encoders too. -hwaccel auto
        Auto //Only lets you select non-hardware backed encoders, but switches to one if possible. -hwaccel auto
    }

    public enum RateUnit
    {
        [Description("B")]
        Bits,

        [Description("K")]
        Kilobits,

        [Description("M")]
        Megabits
    }

    /// <summary>
    /// FFmpeg pixel formats.
    /// https://github.com/FFmpeg/FFmpeg/blob/b7b73e83e3d5c78a5fea96a6bcae02e1f0a5c45f/libavutil/pixdesc.c
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum VideoPixelFormats
    {
        NotSelected,
        Auto,
        Bgr0,
        [Description("bgr4_byte")] Bgr4Byte, //https://stackoverflow.com/questions/8588384/how-to-define-an-enum-with-string-value
        Bgr8,
        BgrA,
        Cuda,
        D3D11,
        Dxva2Vld,
        Gbrp,
        Gbrp10Le,
        Gbrp12Le,
        Gray,
        Gray10Le,
        Gray16Be,
        MonoB,
        Nv12,
        Nv16,
        Nv20Le,
        Nv21,
        P010Le,
        Pal8,
        Qsv,
        Rgb24,
        Rgb48Be,
        Rgb8,
        Rgba64Be,
        RgbA,
        [Description("bgr4_byte")] Rgb4Byte,
        Ya8,
        Ya16Be,
        Yuv420p,
        Yuv420p10Le,
        Yuv420p12Le,
        Yuv422p,
        Yuv422p10Le,
        Yuv422p12Le,
        Yuv440p,
        Yuv444p,
        Yuv440p10Le,
        Yuv440p12Le,
        Yuv444p10Le,
        Yuv444p12Le,
        Yuv444p16Le,
        Yuva420p,
        Yuvj420p,
        Yuvj422p,
        Yuvj444p,
    }

    public enum Framerates
    {
        Auto,
        Custom,
        Film,
        Ntsc,
        Pal
    }

    public enum Vsyncs
    {
        Off,
        Auto,
        Passthrough,
        Cfr,
        Vfr,
        Drop
    }
}