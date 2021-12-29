namespace ScreenToGif.Domain.Enums.Native;

/// <summary>
/// Flags that define the icon that is shown on a balloon
/// tooltip.
/// </summary>
public enum BalloonFlags
{
    /// <summary>
    /// No icon is displayed.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// An information icon is displayed.
    /// </summary>
    Info = 0x01,

    /// <summary>
    /// A warning icon is displayed.
    /// </summary>
    Warning = 0x02,

    /// <summary>
    /// An error icon is displayed.
    /// </summary>
    Error = 0x03,

    /// <summary>
    /// Windows XP Service Pack 2 (SP2) and later.
    /// Use a custom icon as the title icon.
    /// </summary>
    User = 0x04,

    /// <summary>
    /// Windows XP (Shell32.dll version 6.0) and later.
    /// Do not play the associated sound. Applies only to balloon ToolTips.
    /// </summary>
    NoSound = 0x10,

    /// <summary>
    /// Windows Vista (Shell32.dll version 6.0.6) and later. The large version
    /// of the icon should be used as the balloon icon. This corresponds to the
    /// icon with dimensions SM_CXICON x SM_CYICON. If this flag is not set,
    /// the icon with dimensions XM_CXSMICON x SM_CYSMICON is used.<br/>
    /// - This flag can be used with all stock icons.<br/>
    /// - Applications that use older customized icons (NIIF_USER with hIcon) must
    ///   provide a new SM_CXICON x SM_CYICON version in the tray icon (hIcon). These
    ///   icons are scaled down when they are displayed in the System Tray or
    ///   System Control Area (SCA).<br/>
    /// - New customized icons (NIIF_USER with hBalloonIcon) must supply an
    ///   SM_CXICON x SM_CYICON version in the supplied icon (hBalloonIcon).
    /// </summary>
    LargeIcon = 0x20,

    /// <summary>
    /// Windows 7 and later.
    /// </summary>
    RespectQuietTime = 0x80
}