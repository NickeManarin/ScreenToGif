using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct PointW
{
    public int X;
    public int Y;
}