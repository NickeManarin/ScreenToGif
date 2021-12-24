using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TimeCaps
{
    public readonly uint MinimumResolution;
    public readonly uint MaximumResolution;
};