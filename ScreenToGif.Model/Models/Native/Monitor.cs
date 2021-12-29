using System.Windows;

namespace ScreenToGif.Domain.Models.Native;

public class Monitor
{
    public IntPtr Handle { get; set; }

    public Rect Bounds { get; set; }

    public Rect NativeBounds { get; set; }

    public Rect WorkingArea { get; set; }

    public string Name { get; set; }

    public string AdapterName { get; set; }

    public string FriendlyName { get; set; }

    public int Dpi { get; set; }

    public double Scale => Dpi / 96d;

    public bool IsPrimary { get; set; }
}