using System.Runtime.InteropServices;
using ScreenToGif.Domain.Models.Native;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct TitlebarInfo
{
    /// <summary>
    /// The size, in bytes, of the structure. The caller must set this member to sizeof(TITLEBARINFO).
    /// </summary>
    public int cbSize;

    /// <summary>
    /// The coordinates of the title bar.
    /// These coordinates include all title-bar elements except the window menu.
    /// </summary>
    public NativeRect rcTitleBar;

    /// <summary>
    /// An array that receives a value for each element of the title bar.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.CChildrenTitlebar + 1)]
    public int[] rgstate;

    public TitlebarInfo(bool? filler) : this()
    {
        //Allows automatic initialization of "cbSize" with "new TitlebarInfo(null/true/false)".
        cbSize = (int)Marshal.SizeOf(typeof(TitlebarInfo));
    }
}