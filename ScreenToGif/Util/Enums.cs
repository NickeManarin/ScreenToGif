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
        /// Any type of video.
        /// </summary>
        Video,

        /// <summary>
        /// Portable Network Graphics.
        /// </summary>
        Images,

        /// <summary>
        /// Project file, .stg or .zip.
        /// </summary>
        Project,

        /// <summary>
        /// PSD file.
        /// </summary>
        Photoshop,
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
    public enum Stage
    {
        /// <summary>
        /// Recording stopped, but selecting the region to record.
        /// </summary>
        SelectingRegion = -1,

        /// <summary>
        /// Recording stopped.
        /// </summary>
        Stopped = 0,

        /// <summary>
        /// Recording active.
        /// </summary>
        Recording = 1,

        /// <summary>
        /// Recording paused.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// Pre start countdown active.
        /// </summary>
        PreStarting = 3,

        /// <summary>
        /// Single shot mode.
        /// </summary>
        Snapping = 4,

        /// <summary>
        /// The recording is being discarded.
        /// </summary>
        Discarding = 5
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
    /// Type of the gif encoder.
    /// </summary>
    public enum GifEncoderType
    {
        ScreenToGif,
        PaintNet,
        FFmpeg,
        Gifski
    }

    /// <summary>
    /// Type of the apng encoder.
    /// </summary>
    public enum ApngEncoderType
    {
        ScreenToGif,
        FFmpeg,
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
    /// Type of the video encoder.
    /// </summary>
    public enum VideoEncoderType
    {
        AviStandalone,
        Ffmpg,
    }

    /// <summary>
    /// Type of the progress indicator.
    /// </summary>
    public enum ProgressType
    {
        Bar,
        Text,
    }

    /// <summary>
    /// The type of directory, used to decide the icon of the folder inside the SelectFolderDialog.
    /// </summary>
    public enum DirectoryType
    {
        ThisComputer,
        Drive,
        Folder,
        File,

        Desktop,
        Documents,
        Images,
        Music,
        Videos,
        Downloads
    }

    /// <summary>
    /// The type of path.
    /// </summary>
    public enum PathType
    {
        VirtualFolder,
        Folder,
        File
    }

    /// <summary>
    /// The type of the output.
    /// </summary>
    public enum OutputType
    {
        Video,
        Gif,
        Apng,
        Image,
        Project
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

    /// <summary>
    /// The types of source of project creation.
    /// </summary>
    public enum ProjectByType
    {
        Unknown = 0,
        ScreenRecorder = 1,
        WebcamRecorder = 2,
        BoardRecorder = 3,
        Editor = 4,
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
        Scale = 2,
    }

    /// <summary>
    /// Type of capture frequency mode for the screen recorder.
    /// </summary>
    public enum CaptureFrequency
    {
        Manual,
        PerSecond,
        PerMinute,
        PerHour
    }
}