using System;
using System.Collections.Generic;
using System.Windows;

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

            KeyList = new List<SimpleKeyGesture>();
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="cursorInfo">All cursor information.</param>
        public FrameInfo(string bitmap, int delay, CursorInfo cursorInfo) : this(bitmap, delay)
        {
            CursorInfo = cursorInfo;
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="cursorInfo">All cursor information.</param>
        /// <param name="keyList">The list of pressed keys.</param>
        public FrameInfo(string bitmap, int delay, CursorInfo cursorInfo, List<SimpleKeyGesture> keyList) : this(bitmap, delay, cursorInfo)
        {
            KeyList = keyList;
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="cursorX">Cursor X position.</param>
        /// <param name="cursorY">Cursor Y positiob</param>
        /// <param name="clicked">True if clicked.</param>
        /// <param name="keyList">The list of pressed keys.</param>
        public FrameInfo(string bitmap, int delay, int cursorX, int cursorY, bool clicked, List<SimpleKeyGesture> keyList = null) : this(bitmap, delay)
        {
            CursorX = cursorX;
            CursorY = cursorY;
            WasClicked = clicked;
            KeyList = keyList;
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
        /// Cursor X position.
        /// </summary>
        public int CursorX { get; set; }

        /// <summary>
        /// Cursor Y position.
        /// </summary>
        public int CursorY { get; set; }

        /// <summary>
        /// True if was clicked.
        /// </summary>
        public bool WasClicked { get; set; }

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

        /// <summary>
        /// List of keys pressed during the recording of this frame.
        /// </summary>
        public List<SimpleKeyGesture> KeyList { get; set; }

        /// <summary>
        /// ?
        /// </summary>
        internal bool HasChanged { get; set; }

        #endregion
    }
}
