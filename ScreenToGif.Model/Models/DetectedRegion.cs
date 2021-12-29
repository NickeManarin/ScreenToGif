using System.Windows;

namespace ScreenToGif.Domain.Models;

public class DetectedRegion
{
    public IntPtr Handle { get; private set; }

    public Rect Bounds { get; set; }

    public string Name { get; private set; }

    /// <summary>
    /// The Z-Index of the window, higher means that the window will be on top.
    /// </summary>
    public int Order { get; private set; }

    public DetectedRegion(IntPtr handle, Rect bounds, string name, int order = 0)
    {
        Handle = handle;
        Bounds = bounds;
        Name = name;
        Order = order;
    }
}