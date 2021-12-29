using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct BitmapFileHeader
{
    public ushort bfType;
    public uint bfSize;
    public ushort bfReserved1;
    public ushort bfReserved2;
    public uint bfOffBits;
}