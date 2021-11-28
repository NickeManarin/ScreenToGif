using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct CursorInfo
{
    /// <summary>
    /// Specifies the size, in bytes, of the structure. 
    /// </summary>
    public int cbSize;

    /// <summary>
    /// Specifies the cursor state. This parameter can be one of the following values:
    /// </summary>
    public int flags;

    ///<summary>
    ///Handle to the cursor. 
    ///</summary>
    public IntPtr hCursor;

    /// <summary>
    /// A POINT structure that receives the screen coordinates of the cursor. 
    /// </summary>
    public PointW ptScreenPos;
}