using System.Runtime.InteropServices;

namespace ScreenToGif.Domain.Enums.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct DisplayDevices
{
    public DisplayDevices(bool? filler) : this()
    {
        //Allows automatic initialization of "Size" with "new DisplayDevice(null/true/false)".
        Size = Marshal.SizeOf(typeof(DisplayDevices));
    }

    [MarshalAs(UnmanagedType.U4)]
    public int Size;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceString;

    [MarshalAs(UnmanagedType.U4)]
    public DisplayDeviceStates StateFlags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string DeviceKey;
}