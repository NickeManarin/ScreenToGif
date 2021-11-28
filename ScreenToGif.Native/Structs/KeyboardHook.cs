using System.Runtime.InteropServices;

namespace ScreenToGif.Native.Structs;

/// <summary>
/// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
/// </summary>
/// <remarks>
/// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal class KeyboardHook
{
    /// <summary>
    /// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
    /// </summary>
    public int KeyCode;

    /// <summary>
    /// Specifies a hardware scan code for the key. 
    /// </summary>
    public int ScanCode;

    /// <summary>
    /// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
    /// </summary>
    public int Flags;

    /// <summary>
    /// Specifies the time stamp for this message.
    /// </summary>
    public int Time;

    /// <summary>
    /// Specifies extra information associated with the message. 
    /// </summary>
    public int ExtraInfo;
}