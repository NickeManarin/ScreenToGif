using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.Helpers;

namespace ScreenToGif.Native.Structs;

/// <summary>
/// A struct that is submitted in order to configure the taskbar icon.
/// Provides various members that can be configured partially, according to the
/// values of the <see cref="IconDataMembers"/> that were defined.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct NotifyIconData
{
    /// <summary>
    /// Size of this structure, in bytes.
    /// </summary>
    public uint cbSize;

    /// <summary>
    /// Handle to the window that receives notification messages associated with an icon in the taskbar status area.
    /// The Shell uses hWnd and uID to identify which icon to operate on when Shell_NotifyIcon is invoked.
    /// </summary>
    public IntPtr WindowHandle;

    /// <summary>
    /// Application-defined identifier of the taskbar icon.
    /// The Shell uses hWnd and uID to identify which icon to operate on when Shell_NotifyIcon is invoked.
    /// You can have multiple icons associated with a single hWnd by assigning each a different uID.
    /// This feature, however is currently not used.
    /// </summary>
    public uint TaskbarIconId;

    /// <summary>
    /// Flags that indicate which of the other members contain valid data.
    /// This member can be a combination of the NIF_XXX constants.
    /// </summary>
    public IconDataMembers ValidMembers;

    /// <summary>
    /// Application-defined message identifier.
    /// The system uses this identifier to send notifications to the window identified in hWnd.
    /// </summary>
    public uint CallbackMessageId;

    /// <summary>
    /// A handle to the icon that should be displayed.
    /// Just Icon.Handle.
    /// </summary>
    public IntPtr IconHandle;

    /// <summary>
    /// String with the text for a standard ToolTip.
    /// It can have a maximum of 128 characters, including the terminating NULL.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string ToolTipText;

    /// <summary>
    /// State of the icon. Remember to also set the <see cref="StateMask"/>.
    /// </summary>
    public IconStates IconState;

    /// <summary>
    /// A value that specifies which bits of the state member are retrieved or modified.
    /// For example, setting this member to <see cref="IconStates.Hidden"/> causes only the item's hidden state to be retrieved.
    /// </summary>
    public IconStates StateMask;

    /// <summary>
    /// String with the text for a balloon ToolTip. It can have a maximum of 255 characters.
    /// To remove the ToolTip, set the NIF_INFO flag in uFlags and set szInfo to an empty string.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string BalloonText;

    /// <summary>
    /// Mainly used to set the version when Shell_NotifyIcon is invoked with <see cref="NotifyCommands.SetVersion"/>.
    /// However, for legacy operations, the same member is also used to set timouts for balloon ToolTips.
    /// </summary>
    public uint VersionOrTimeout;

    /// <summary>
    /// String containing a title for a balloon ToolTip.
    /// This title appears in boldface above the text.
    /// It can have a maximum of 63 characters.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string BalloonTitle;

    /// <summary>
    /// Adds an icon to a balloon ToolTip, which is placed to the left of the title.
    /// If the <see cref="BalloonTitle"/> member is zero-length, the icon is not shown.
    /// </summary>
    public BalloonFlags BalloonFlags;

    /// <summary>
    /// A registered GUID that identifies the icon. 
    /// This value overrides uID and is the recommended method of identifying the icon.
    /// </summary>
    public Guid TaskbarIconGuid;

    /// <summary>
    /// The handle of a customized balloon icon provided by the application that should
    /// be used independently of the tray icon.
    /// If this member is non-NULL and the User flag is set, this icon is used as the balloon icon.
    /// If this member is NULL, the legacy behavior is carried out.
    /// </summary>
    public IntPtr CustomBalloonIconHandle;


    /// <summary>
    /// Creates a default data structure that provides a hidden taskbar icon without the icon being set.
    /// </summary>
    public static NotifyIconData CreateDefault(IntPtr handle)
    {
        var data = new NotifyIconData();

        data.cbSize = (uint)Marshal.SizeOf(data);
        data.WindowHandle = handle;
        data.TaskbarIconId = 0x0;
        data.CallbackMessageId = WindowMessageSink.CallbackMessageId;
        data.VersionOrTimeout = (uint)NotifyIconVersions.Vista;
        data.IconHandle = IntPtr.Zero;

        //hide initially
        data.IconState = IconStates.Hidden;
        data.StateMask = IconStates.Hidden;

        //set flags
        data.ValidMembers = IconDataMembers.Message | IconDataMembers.Icon | IconDataMembers.Tip;

        //reset strings
        data.ToolTipText = data.BalloonText = data.BalloonTitle = string.Empty;

        return data;
    }
}