using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct BitmapInfoHeader
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public BitmapCompressionModes biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;

    public BitmapInfoHeader Init()
    {
        return new BitmapInfoHeader { biSize = (uint)Marshal.SizeOf(this) };
    }
}