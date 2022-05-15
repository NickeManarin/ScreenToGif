using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Util;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Model;

[DataContract]
[KnownType(typeof(SimpleKeyGesture))]
public class FrameInfo : IFrame
{
    #region Constructors

    /// <summary>
    /// The parameterless constructor.
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

        KeyList = new List<IKeyGesture>();
    }

    /// <summary>
    /// Initialises a FrameInfo instance.
    /// </summary>
    /// <param name="path">The Bitmap.</param>
    /// <param name="delay">The delay.</param>
    /// <param name="keyList">The list of pressed keys.</param>
    public FrameInfo(string path, int delay, List<IKeyGesture> keyList) : this(path, delay)
    {
        KeyList = keyList != null ? new List<IKeyGesture>(keyList) : new List<IKeyGesture>();
    }

    /// <summary>
    /// Initialises a FrameInfo instance.
    /// </summary>
    /// <param name="button">Type of mouse button clicked with the mouse.</param>
    /// <param name="keyList">The list of pressed keys.</param>
    public FrameInfo(MouseButtons button, List<IKeyGesture> keyList)
    {
        ButtonClicked = button;
        KeyList = keyList != null ? new List<IKeyGesture>(keyList) : new List<IKeyGesture>();
    }

    /// <summary>
    /// Initialises a FrameInfo instance.
    /// </summary>
    /// <param name="path">The Bitmap.</param>
    /// <param name="delay">The delay.</param>
    /// <param name="button">Type of mouse button the user clicked with the mouse.</param>
    /// <param name="keyList">The list of pressed keys.</param>
    /// <param name="index">The index of the frame.</param>
    public FrameInfo(string path, int delay, MouseButtons button, List<IKeyGesture> keyList = null, int index = 0) : this(path, delay)
    {
        ButtonClicked = button;
        KeyList = keyList != null ? new List<IKeyGesture>(keyList) : new List<IKeyGesture>();
        Index = index;
    }

    /// <summary>
    /// Initialises a FrameInfo instance.
    /// </summary>
    /// <param name="path">The Bitmap.</param>
    /// <param name="delay">The delay.</param>
    /// <param name="cursorX">Cursor X position.</param>
    /// <param name="cursorY">Cursor Y position</param>
    /// <param name="button">Type of mouse button user clicked with the mouse.</param>
    /// <param name="keyList">The list of pressed keys.</param>
    /// <param name="index">The index of the frame.</param>
    public FrameInfo(string path, int delay, int cursorX, int cursorY, MouseButtons button, List<IKeyGesture> keyList = null, int index = 0) : this(path, delay)
    {
        CursorX = cursorX;
        CursorY = cursorY;
        ButtonClicked = button;
        KeyList = keyList != null ? new List<IKeyGesture>(keyList) : new List<IKeyGesture>();
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
    /// Type of the button that was clicked.
    /// </summary>
    [DataMember(EmitDefaultValue = false, Name = "ButtonClicked")]
    public MouseButtons ButtonClicked { get; set; }

    /// <summary>
    /// If the button was clicked (legacy projects)
    /// </summary>
    [DataMember(Name = "Clicked")]
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

    //Temporary.
    [DataMember(EmitDefaultValue = false, Name = "Keys")]
    public List<SimpleKeyGesture> TemporaryKeyList { get; set; }

    /// <summary>
    /// List of keys pressed during the recording of this frame.
    /// </summary>
    [IgnoreDataMember]
    public List<IKeyGesture> KeyList { get; set; }

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


    /// <summary>
    /// This works as a migration method for mouse events. Before storing the button
    /// type only bool was stored to mark the clicks. During opening old project it will
    /// be converted to Left mouse button click losing some info unfortunately.
    /// </summary>
    /// <param name="context"></param>
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (ButtonClicked == MouseButtons.None)
            ButtonClicked = WasClicked ? MouseButtons.Left : MouseButtons.None;

        if (TemporaryKeyList?.Count > 0 && KeyList == null)
            KeyList = new List<IKeyGesture>(TemporaryKeyList);
    }

    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
        if (KeyList != null)
            TemporaryKeyList = KeyList?.OfType<SimpleKeyGesture>().ToList();
    }

    #endregion
}