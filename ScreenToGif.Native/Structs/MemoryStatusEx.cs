using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

public struct MemoryStatusEx
{
    public uint Length;
    public uint MemoryLoad;
    public ulong TotalPhysicalMemory;
    public ulong AvailablePhysicalMemory;
    public ulong TotalPageFile;
    public ulong AvailablePageFile;
    public ulong TotalVirtualMemory;
    public ulong AvailableVirtualMemory;
    public ulong AvailableExtendedVirtual;

    public MemoryStatusEx(bool? filler) : this()
    {
        Length = checked((uint)Marshal.SizeOf(typeof(MemoryStatusEx)));
    }
}