using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.External
{
    public static class Shell32
    {
        /// <summary>
        /// "open"       - Opens a file or a application.
        /// "openas"     - Opens dialog when no program is associated to the extension.
        /// "opennew"    - see MSDN.
        /// "runas"      - In Windows 7 and Vista, opens the UAC dialog and in others, open the Run as... Dialog.
        /// "null"       - Specifies that the operation is the default for the selected file type.
        /// "edit"       - Opens the default text editor for the file.    
        /// "explore"    - Opens the Windows Explorer in the folder specified in lpDirectory.
        /// "properties" - Opens the properties window of the file.
        /// "copy"       - see MSDN.
        /// "cut"        - see MSDN.
        /// "paste"      - see MSDN.
        /// "pastelink"  - pastes a shortcut.
        /// "delete"     - see MSDN.
        /// "print"      - Start printing the file with the default application.
        /// "printto"    - see MSDN.
        /// "find"       - Start a search.
        /// </summary>
        [DllImport(Constants.Shell32, CharSet = CharSet.Auto)]
        internal static extern bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);

        /// <summary>
        /// Creates, updates or deletes the taskbar icon.
        /// </summary>
        [DllImport(Constants.Shell32, CharSet = CharSet.Unicode)]
        public static extern bool Shell_NotifyIcon(NotifyCommands cmd, [In] ref NotifyIconData data);
    }
}