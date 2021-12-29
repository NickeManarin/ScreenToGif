using System.Windows;

namespace ScreenToGif.Native.Structs;

public struct Margins
{
    public Margins(Thickness t)
    {
        Left = (int)t.Left;
        Right = (int)t.Right;
        Top = (int)t.Top;
        Bottom = (int)t.Bottom;
    }

    public int Left;
    public int Right;
    public int Top;
    public int Bottom;
}