namespace ScreenToGif.Domain.Enums.Native;

/// <summary>
/// The main operations performed on the <see cref="Shell_NotifyIcon"/> function.
/// </summary>
public enum NotifyCommands
{
    /// <summary>
    /// The taskbar icon is being created.
    /// </summary>
    Add = 0x00,

    /// <summary>
    /// The settings of the taskbar icon are being updated.
    /// </summary>
    Modify = 0x01,

    /// <summary>
    /// The taskbar icon is deleted.
    /// </summary>
    Delete = 0x02,

    /// <summary>
    /// Focus is returned to the taskbar icon.
    /// </summary>
    SetFocus = 0x03,

    /// <summary>
    /// Shell32.dll version 5.0 and later only. Instructs the taskbar
    /// to behave according to the version number specified in the 
    /// uVersion member of the structure pointed to by lpdata.
    /// This message allows you to specify whether you want the version
    /// 5.0 behavior found on Microsoft Windows 2000 systems, or the
    /// behavior found on earlier Shell versions. The default value for
    /// uVersion is zero.
    /// </summary>
    SetVersion = 0x04
}