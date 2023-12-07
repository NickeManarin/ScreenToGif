namespace ScreenToGif.Domain.Enums.Native;

public enum SysCommands : uint
{
    Size = 0xF000,
    Move = 0xF010,
    Minimize = 0xF020,
    Maximize = 0xF030,
    NextWindow = 0xF040,
    PreviousWindow = 0xF050,
    Close = 0xF060,
    VScroll = 0xF070,
    HScroll = 0xF080,
    MouseMenu = 0xF090,
    KeyMenu = 0xF100,
    Arrange = 0xF110,
    Restore = 0xF120,
    TaskList = 0xF130,
    ScreenSave = 0xF140,
    HotKey = 0xF150,
    Default = 0xF160,
    MonitorPower = 0xF170,
    ContextHelp = 0xF180,
    Separator = 0xF00F,
    IsSecure = 0x0001,
}