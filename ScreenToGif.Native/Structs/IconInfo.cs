using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct Iconinfo
{
    /// <summary>
    /// Specifies whether this structure defines an icon or a cursor.
    /// A value of TRUE specifies an icon; FALSE specifies a cursor.
    /// </summary>
    public bool fIcon;

    /// <summary>
    /// The x-coordinate of a cursor's hot spot.
    /// If this structure defines an icon, the hot spot is always in the center of the icon, and this member is ignored.
    /// </summary>
    public int xHotspot;

    /// <summary>
    /// The y-coordinate of the cursor's hot spot.
    /// If this structure defines an icon, the hot spot is always in the center of the icon, and this member is ignored.
    /// </summary>
    public int yHotspot;

    /// <summary>
    /// The icon bitmask bitmap.
    /// If this structure defines a black and white icon, this bitmask is formatted so that the upper half is the icon AND bitmask and the lower half is the icon XOR bitmask.
    /// Under this condition, the height should be an even multiple of two. If this structure defines a color icon, this mask only defines the AND bitmask of the icon.
    /// </summary>
    public IntPtr hbmMask;

    /// <summary>
    /// A handle to the icon color bitmap.
    /// This member can be optional if this structure defines a black and white icon.
    /// The AND bitmask of hbmMask is applied with the SRCAND flag to the destination; subsequently, the color bitmap is applied (using XOR) to the destination by using the SRCINVERT flag.
    /// </summary>
    public IntPtr hbmColor;
}