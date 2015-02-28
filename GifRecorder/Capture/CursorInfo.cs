using System.Drawing;
using System.Windows.Input;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// The info of the cursor, position and image.
    /// </summary>
    public class CursorInfo
    {
        #region Getters and Setters

        /// <summary>
        /// The position of the cursor.
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// The image of the icon.
        /// </summary>
        public Icon Icon { get; set; }

        /// <summary>
        /// The image of the icon.
        /// </summary>
        public Bitmap IconImage { get; set; }

        /// <summary>
        /// True if clicked.
        /// </summary>
        public bool Clicked { get; set; }

        #endregion
    }
}
