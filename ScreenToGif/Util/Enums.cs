namespace ScreenToGif.Util
{
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
        /// Any type of video.
        /// </summary>
        Video,
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
        SaveAs = 0,

        /// <summary>
        /// New Animation Panel.
        /// </summary>
        NewAnimation = 1,

        /// <summary>
        /// Clipboard Panel.
        /// </summary>
        Clipboard = 2,

        /// <summary>
        /// Resize Panel.
        /// </summary>
        Resize = 3,

        /// <summary>
        /// Flip/Rotate Panel.
        /// </summary>
        FlipRotate = 4,

        /// <summary>
        /// Override Delay Panel.
        /// </summary>
        OverrideDelay = 5,

        /// <summary>
        /// Change Delay Panel.
        /// </summary>
        ChangeDelay = 6,

        /// <summary>
        /// Fade Transition Panel.
        /// </summary>
        Fade = 7,

        /// <summary>
        /// Slide Transition Panel.
        /// </summary>
        Slide = 8,

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
        /// Watermark Panel.
        /// </summary>
        Watermark = -6,

        /// <summary>
        /// Border Panel.
        /// </summary>
        Border = -7,

        /// <summary>
        /// Cinemagraph Panel.
        /// </summary>
        Cinemagraph = -8,

        /// <summary>
        /// Progress Panel.
        /// </summary>
        Progress = -9,
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
        Snapping = 4
    }

    /// <summary>
    /// EncoderListBox Item Status.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Normal encoding status.
        /// </summary>
        Encoding,

        /// <summary>
        /// The Encoding was cancelled.
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
    /// The icon of the left side of a Dialog window.
    /// </summary>
    public enum MessageIcon
    {
        Success,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Type of the Flip/Rotate action.
    /// </summary>
    public enum FlipRotateType
    {
        FlipHorizontal,
        FlipVertical,
        RotateRight90,
        RotateLeft90,
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
        IncreaseDecrease
    }

    /// <summary>
    /// Type of the gif encoder.
    /// </summary>
    public enum GifEncoderType
    {
        Legacy,
        ScreenToGif,
        PaintNet
    }

    /// <summary>
    /// Type of the video encoder.
    /// </summary>
    public enum VideoEncoderType
    {
        AviStandalone,
        Ffmpg,
    }
}
