namespace ScreenToGif.Domain.Enums.Native;

public enum GetWindowType : uint
{
    /// <summary>
    /// The retrieved handle identifies the window of the same type that is highest in the Z order.
    /// <para/>
    /// If the specified window is a topmost window, the handle identifies a topmost window.
    /// If the specified window is a top-level window, the handle identifies a top-level window.
    /// If the specified window is a child window, the handle identifies a sibling window.
    /// </summary>
    HwndFirst = 0,

    /// <summary>
    /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
    /// <para />
    /// If the specified window is a topmost window, the handle identifies a topmost window.
    /// If the specified window is a top-level window, the handle identifies a top-level window.
    /// If the specified window is a child window, the handle identifies a sibling window.
    /// </summary>
    HwdnLast = 1,

    /// <summary>
    /// The retrieved handle identifies the window below the specified window in the Z order.
    /// <para />
    /// If the specified window is a topmost window, the handle identifies a topmost window.
    /// If the specified window is a top-level window, the handle identifies a top-level window.
    /// If the specified window is a child window, the handle identifies a sibling window.
    /// </summary>
    HwndNext = 2,

    /// <summary>
    /// The retrieved handle identifies the window above the specified window in the Z order.
    /// <para />
    /// If the specified window is a topmost window, the handle identifies a topmost window.
    /// If the specified window is a top-level window, the handle identifies a top-level window.
    /// If the specified window is a child window, the handle identifies a sibling window.
    /// </summary>
    HwndPrev = 3,

    /// <summary>
    /// The retrieved handle identifies the specified window's owner window, if any.
    /// </summary>
    Owner = 4,

    /// <summary>
    /// The retrieved handle identifies the child window at the top of the Z order,
    /// if the specified window is a parent window; otherwise, the retrieved handle is NULL.
    /// The function examines only child windows of the specified window. It does not examine descendant windows.
    /// </summary>
    Child = 5,

    /// <summary>
    /// The retrieved handle identifies the enabled popup window owned by the specified window (the
    /// search uses the first such window found using HwndNext); otherwise, if there are no enabled
    /// popup windows, the retrieved handle is that of the specified window.
    /// </summary>
    EnabledPopup = 6
}