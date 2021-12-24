namespace ScreenToGif.Domain.Enums.Native;

/// <summary>
/// Indicates which members of a <see cref="NotifyIconData"/> structure
/// were set, and thus contain valid data or provide additional information
/// to the ToolTip as to how it should display.
/// </summary>
[Flags]
public enum IconDataMembers
{
    /// <summary>
    /// The message ID is set.
    /// </summary>
    Message = 0x01,

    /// <summary>
    /// The notification icon is set.
    /// </summary>
    Icon = 0x02,

    /// <summary>
    /// The tooltip is set.
    /// </summary>
    Tip = 0x04,

    /// <summary>
    /// State information (<see cref="IconState"/>) is set. This
    /// applies to both <see cref="NotifyIconData.IconState"/> and
    /// <see cref="NotifyIconData.StateMask"/>.
    /// </summary>
    State = 0x08,

    /// <summary>
    /// The balloon ToolTip is set. Accordingly, the following
    /// members are set: <see cref="NotifyIconData.BalloonText"/>,
    /// <see cref="NotifyIconData.BalloonTitle"/>, <see cref="NotifyIconData.BalloonFlags"/>,
    /// and <see cref="NotifyIconData.VersionOrTimeout"/>.
    /// </summary>
    Info = 0x10,

    // Internal identifier is set. Reserved, thus commented out.
    //Guid = 0x20,

    /// <summary>
    /// Windows Vista (Shell32.dll version 6.0.6) and later. If the ToolTip
    /// cannot be displayed immediately, discard it.<br/>
    /// Use this flag for ToolTips that represent real-time information which
    /// would be meaningless or misleading if displayed at a later time.
    /// For example, a message that states "Your telephone is ringing."<br/>
    /// This modifies and must be combined with the <see cref="Info"/> flag.
    /// </summary>
    Realtime = 0x40,

    /// <summary>
    /// Windows Vista (Shell32.dll version 6.0.6) and later.
    /// Use the standard ToolTip. Normally, when uVersion is set
    /// to NOTIFYICON_VERSION_4, the standard ToolTip is replaced
    /// by the application-drawn pop-up user interface (UI).
    /// If the application wants to show the standard tooltip
    /// in that case, regardless of whether the on-hover UI is showing,
    /// it can specify NIF_SHOWTIP to indicate the standard tooltip
    /// should still be shown.<br/>
    /// Note that the NIF_SHOWTIP flag is effective until the next call 
    /// to Shell_NotifyIcon.
    /// </summary>
    UseLegacyToolTips = 0x80
}