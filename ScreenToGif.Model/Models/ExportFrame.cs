using System.Windows;

namespace ScreenToGif.Domain.Models;

public class ExportFrame
{
    /// <summary>
    /// The position of the frame within the list.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The position of the frame on the pixel array.
    /// </summary>
    public long DataPosition { get; set; }

    /// <summary>
    /// The pixel array data length of the frame.
    /// </summary>
    public long DataLength { get; set; }

    /// <summary>
    /// The delay of the frame.
    /// </summary>
    public int Delay { get; set; }

    /// <summary>
    /// The Rectangle of the frame.
    /// </summary>
    public Int32Rect Rect { get; set; }

    /// <summary>
    /// The depth in bits of the frame.
    /// </summary>
    public int ImageDepth { get; set; }

    /// <summary>
    /// True if the frame has area, width and height > 0.
    /// </summary>
    public bool HasArea => Rect.HasArea;
}