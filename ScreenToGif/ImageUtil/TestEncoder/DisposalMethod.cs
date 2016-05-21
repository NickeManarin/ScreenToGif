namespace ScreenToGif.ImageUtil.TestEncoder
{
    /// <summary>
    /// Enumeration of disposal methods that can be found in a Graphic Control
    /// Extension.
    /// See http://www.w3.org/Graphics/GIF/spec-gif89a.txt section 23.
    /// </summary>
    internal enum DisposalMethod
    {
        /// <summary>
        /// 0 - No disposal specified. The decoder is not required to take any 
        /// action.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// 1 - Do not dispose. The graphic is to be left in place.
        /// </summary>
        DoNotDispose = 1,

        /// <summary>
        /// 2 - Restore to background color. The area used by the graphic must 
        /// be restored to the background color.
        /// </summary>
        RestoreToBackgroundColour = 2,

        /// <summary>
        /// 3 - Restore to previous. The decoder is required to restore the 
        /// area overwritten by the graphic with what was there prior to 
        /// rendering the graphic.
        /// </summary>
        RestoreToPrevious = 3,

        /// <summary>
        /// 4 - To be defined.
        /// </summary>
        ToBeDefined4,

        /// <summary>
        /// 5 - To be defined.
        /// </summary>
        ToBeDefined5,

        /// <summary>
        /// 6 - To be defined.
        /// </summary>
        ToBeDefined6,

        /// <summary>
        /// 7 - To be defined.
        /// </summary>
        ToBeDefined7
    }
}