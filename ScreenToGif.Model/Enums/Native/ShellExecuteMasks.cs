namespace ScreenToGif.Domain.Enums.Native;

[Flags]
public enum ShellExecuteMasks : uint
{
    Default = 0x00000000,
    ClassName = 0x00000001,
    ClassKey = 0x00000003,
    IdList = 0x00000004,
    InvokeIdList = 0x0000000c, //InvokeIdList(0xC) implies IdList(0x04) 
    HotKey = 0x00000020,
    NoCloseProcess = 0x00000040,
    ConnectNetDrv = 0x00000080,
    NoAsync = 0x00000100,
    FlagDdeWait = NoAsync,
    DeEnvSubst = 0x00000200,
    FlagNoUi = 0x00000400,
    Unicode = 0x00004000,
    NoConsole = 0x00008000,
    Asyncok = 0x00100000,
    HMonitor = 0x00200000,
    NoZoneChecks = 0x00800000,
    NoQueryClassStore = 0x01000000,
    WaitForInputIdle = 0x02000000,
    FlagLogUsage = 0x04000000,
}