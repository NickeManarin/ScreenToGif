namespace ScreenToGif.Domain.Enums.Native;

[Flags]
public enum DisplayDeviceStates : int
{
    /// <summary>
    /// The device is part of the desktop.
    /// </summary>
    AttachedToDesktop = 0x1,

    MultiDriver = 0x2,

    /// <summary>
    /// The device is part of the desktop.
    /// </summary>
    PrimaryDevice = 0x4,

    /// <summary>
    /// Represents a pseudo device used to mirror application drawing for remoting or other purposes.
    /// </summary>
    MirroringDriver = 0x8,

    /// <summary>
    /// The device is VGA compatible.
    /// </summary>
    VgaCompatible = 0x10,

    /// <summary>
    /// The device is removable; it cannot be the primary display.
    /// </summary>
    Removable = 0x20,

    /// <summary>
    /// The device has more display modes than its output devices support.
    /// </summary>
    ModesPruned = 0x8000000,

    Remote = 0x4000000,
    Disconnect = 0x2000000
}