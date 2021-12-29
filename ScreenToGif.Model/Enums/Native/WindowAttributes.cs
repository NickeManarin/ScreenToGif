namespace ScreenToGif.Domain.Enums.Native;

public enum WindowAttributes
{
    /// <summary>
    /// Sets a new address for the window procedure.
    /// You cannot change this attribute if the window does not belong to the same process as the calling thread.
    /// </summary>
    GwlWndproc = -4,

    /// <summary>
    /// Sets a new application instance handle.
    /// </summary>
    GwlHinstance = -6,

    /// <summary>
    /// Changes the owner of a top-level window.
    /// </summary>
    GwlHwndparent = -8,

    /// <summary>
    /// Sets a new window style.
    /// </summary>
    GwlStyle = -16,

    /// <summary>
    /// Sets a new extended window style.
    /// </summary>
    GwlExstyle = -20,

    /// <summary>
    /// Sets the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero.
    /// </summary>
    GwlUserdata = -21,

    /// <summary>
    /// Sets a new identifier of the child window. The window cannot be a top-level window.
    /// </summary>
    GwlId = -12
}