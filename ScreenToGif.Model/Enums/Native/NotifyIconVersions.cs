namespace ScreenToGif.Domain.Enums.Native;

/// <summary>
/// The notify icon version that is used. The higher
/// the version, the more capabilities are available.
/// </summary>
public enum NotifyIconVersions
{
    /// <summary>
    /// Default behavior (legacy Win95).
    /// Expects a <see cref="NotifyIconData"/> size of 488.
    /// </summary>
    Win95 = 0x0,

    /// <summary>
    /// Behavior representing Win2000 an higher.
    /// Expects a <see cref="NotifyIconData"/> size of 504.
    /// </summary>
    Win2000 = 0x3,

    /// <summary>
    /// Extended tooltip support, which is available for Vista and later.
    /// </summary>
    Vista = 0x4
}