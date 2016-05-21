using System;
using System.Windows;
using Point = System.Drawing.Point;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Frame info class.
    /// </summary>
    [Serializable]
    public class FrameInfo
    {
        #region Constructors

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        public FrameInfo(string bitmap, int delay)
        {
            ImageLocation = bitmap;
            Delay = delay;
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="cursorInfo">All cursor information.</param>
        public FrameInfo(string bitmap, int delay, CursorInfo cursorInfo)
        {
            ImageLocation = bitmap;
            Delay = delay;
            CursorInfo = cursorInfo;
        }

        #endregion

        #region Auto Properties

        /// <summary>
        /// The frame image full path.
        /// </summary>
        public string ImageLocation { get; set; }

        /// <summary>
        /// The delay of the frame.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// The Rectangle of the frame.
        /// </summary>
        public Int32Rect Rect { get; set; }

        /// <summary>
        /// True if the frame has area, width and height > 0.
        /// </summary>
        public bool HasArea => Rect.HasArea;

        /// <summary>
        /// The Cursor information of the frame.
        /// </summary>
        public CursorInfo CursorInfo { get; set; }

        #endregion
    }
}
