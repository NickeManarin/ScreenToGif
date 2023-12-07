namespace ScreenToGif.Domain.Enums.Native;

[Flags]
public enum MenuFunctions
{
    ByCommand = 0x00000000,
    ByPosition = 0x00000400,
    Enabled = 0x00000000,
    Grayed = 0x00000001,
    Disabled = 0x00000002
}