using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows;
using ScreenToGif.Util;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Model
{
    [DataContract]
    public class FrameInfo
    {
        #region Constructors

        /// <summary>
        /// The parameterless contructor.
        /// </summary>
        public FrameInfo()
        {}

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="path">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        public FrameInfo(string path, int delay)
        {
            Path = path;
            Delay = delay;

            KeyList = new List<SimpleKeyGesture>();
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="path">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="keyList">The list of pressed keys.</param>
        public FrameInfo(string path, int delay, List<SimpleKeyGesture> keyList) : this(path, delay)
        {
            KeyList = keyList != null ? new List<SimpleKeyGesture>(keyList) : new List<SimpleKeyGesture>();
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="clicked">True if the user clicked with the mouse.</param>
        /// <param name="keyList">The list of pressed keys.</param>
        public FrameInfo(bool clicked, List<SimpleKeyGesture> keyList)
        {
            WasClicked = clicked;
            KeyList = keyList != null ? new List<SimpleKeyGesture>(keyList) : new List<SimpleKeyGesture>();
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="path">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="clicked">True if the user clicked with the mouse.</param>
        /// <param name="keyList">The list of pressed keys.</param>
        /// <param name="index">The index of the frame.</param>
        public FrameInfo(string path, int delay, bool clicked, List<SimpleKeyGesture> keyList = null, int index = 0) : this(path, delay)
        {
            WasClicked = clicked;
            KeyList = keyList != null ? new List<SimpleKeyGesture>(keyList) : new List<SimpleKeyGesture>();
            Index = index;
        }

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="path">The Bitmap.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="cursorX">Cursor X position.</param>
        /// <param name="cursorY">Cursor Y positiob</param>
        /// <param name="clicked">True if the user clicked with the mouse.</param>
        /// <param name="keyList">The list of pressed keys.</param>
        /// <param name="index">The index of the frame.</param>
        public FrameInfo(string path, int delay, int cursorX, int cursorY, bool clicked, List<SimpleKeyGesture> keyList = null, int index = 0) : this(path, delay)
        {
            CursorX = cursorX;
            CursorY = cursorY;
            WasClicked = clicked;
            KeyList = keyList != null ? new List<SimpleKeyGesture>(keyList) : new List<SimpleKeyGesture>();
            Index = index;
        }

        #endregion

        #region Properties

        ///// <summary>
        ///// The frame image relative path (relative to the project location).
        ///// </summary>
        //[DataMember]
        //public string RelativePath { get; set; }

        /// <summary>
        /// The frame image path (it may be the full path or the relative path).
        /// </summary>
        [DataMember]
        public string Path { get; set; }

        /// <summary>
        /// The name of the image file.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// The delay of the frame.
        /// </summary>
        [DataMember]
        public int Delay { get; set; }

        /// <summary>
        /// Cursor X position.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int CursorX { get; set; } = int.MinValue;

        /// <summary>
        /// Cursor Y position.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int CursorY { get; set; } = int.MinValue;

        /// <summary>
        /// True if was clicked.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Clicked")]
        public bool WasClicked { get; set; }

        /// <summary>
        /// The frame index.
        /// </summary>
        [IgnoreDataMember]
        public int Index { get; set; }

        /// <summary>
        /// The Rectangle of the frame.
        /// </summary>
        [IgnoreDataMember]
        public Int32Rect Rect { get; set; }

        /// <summary>
        /// The color that will be treated as transparent on this frame.
        /// </summary>
        [IgnoreDataMember]
        public Color ColorKey { get; set; }

        /// <summary>
        /// True if the frame has area, width and height > 0.
        /// </summary>
        [IgnoreDataMember]
        public bool HasArea => Rect.HasArea;

        /// <summary>
        /// List of keys pressed during the recording of this frame.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "Keys")]
        public List<SimpleKeyGesture> KeyList { get; set; }

        /// <summary>
        /// The pixel array data of the frame.
        /// Used only during the recording.
        /// </summary>
        [IgnoreDataMember]
        public byte[] Data { get; set; }

        /// <summary>
        /// True if the capture of the frame failed somehow.
        /// </summary>
        [IgnoreDataMember]
        public bool FrameSkipped { get; set; }

        /// <summary>
        /// The pixel array data length of the frame.
        /// Used only during the recording.
        /// </summary>
        [IgnoreDataMember]
        public long DataLength { get; set; }

        /// <summary>
        /// The image of the frame.
        /// Used only during the recording.
        /// </summary>
        [IgnoreDataMember]
        public Image Image { get; set; }

        #endregion
    }
}