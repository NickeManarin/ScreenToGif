using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;

namespace ScreenToGif.Native.Helpers;

public static class NotifyIconHelper
{
    /// <summary>
    /// Updates the taskbar icons.
    /// </summary>
    /// <param name="data">Configuration settings for the NotifyIcon.</param>
    /// <param name="command">Operation on the icon (e.g. delete the icon).</param>
    /// <param name="flags">Defines which members of the <paramref name="data"/> structure are set.</param>
    /// <returns>True if the data was successfully written.</returns>
    /// <remarks>See Shell_NotifyIcon documentation on MSDN for details.</remarks>
    public static bool WriteIconData(ref NotifyIconData data, NotifyCommands command, IconDataMembers flags)
    {
        if (VisualHelper.IsInDesignMode())
            return true;

        data.ValidMembers = flags;

        lock (VisualHelper.LockObject)
            return Native.External.Shell32.Shell_NotifyIcon(command, ref data);
    }
}