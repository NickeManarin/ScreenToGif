namespace ScreenToGif.Domain.Enums.Native;

public enum WindowStyles : uint
{
    Overlapped = 0,
    Popup = 0x80000000,
    Child = 0x40000000,
    Minimize = 0x20000000,
    Visible = 0x10000000,
    Disabled = 0x8000000,
    Clipsiblings = 0x4000000,
    Clipchildren = 0x2000000,
    Maximize = 0x1000000,
    Caption = 0xC00000, //WS_BORDER or WS_DLGFRAME  
    Border = 0x800000,
    Dlgframe = 0x400000,
    Vscroll = 0x200000,
    Hscroll = 0x100000,
    Sysmenu = 0x80000,
    Thickframe = 0x40000,
    Group = 0x20000,
    Tabstop = 0x10000,
    Minimizebox = 0x20000,
    Maximizebox = 0x10000,
    Tiled = Overlapped,
    Iconic = Minimize,
    Sizebox = Thickframe,
}