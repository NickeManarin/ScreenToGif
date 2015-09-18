namespace ScreenToGif.Util.Enum
{
    /// <summary>
    /// The types of Panel of the Editor window.
    /// </summary>
    public enum PanelType
    {
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
        /// Crop Panel.
        /// </summary>
        Crop = - 1,

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
    }
}