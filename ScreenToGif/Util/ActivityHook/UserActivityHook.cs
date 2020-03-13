using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace ScreenToGif.Util.ActivityHook
{
    /// <summary>
    /// This class allows you to tap keyboard and mouse and / or to detect their activity even when an 
    /// application runes in background or does not have any user interface at all. This class raises 
    /// common .NET events with KeyEventArgs and MouseEventArgs so you can easily retrive any information you need.
    /// </summary>
    public class UserActivityHook
    {
        #region Windows structure definitions

        /// <summary>
        /// The MOUSEHOOKSTRUCT structure contains information about a mouse event passed to a WH_MOUSE hook procedure, MouseProc. 
        /// </summary>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            /// <summary>
            /// Specifies a POINT structure that contains the x- and y-coordinates of the cursor, in screen coordinates. 
            /// </summary>
            public Native.PointW pt;

            /// <summary>
            /// Handle to the window that will receive the mouse message corresponding to the mouse event. 
            /// </summary>
            public int hwnd;

            /// <summary>
            /// Specifies the hit-test value. For a list of hit-test values, see the description of the WM_NCHITTEST message. 
            /// </summary>
            public int wHitTestCode;

            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int dwExtraInfo;
        }

        /// <summary>
        /// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private class MouseLLHookStruct
        {
            /// <summary>
            /// Specifies a POINT structure that contains the x- and y-coordinates of the cursor, in screen coordinates. 
            /// </summary>
            public Native.PointW pt;

            /// <summary>
            /// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. 
            /// The low-order word is reserved. A positive value indicates that the wheel was rotated forward, 
            /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user. 
            /// One wheel click is defined as WHEEL_DELTA, which is 120. 
            ///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
            /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
            /// and the low-order word is reserved. This value can be one or more of the following values. Otherwise, mouseData is not used. 
            ///XBUTTON1
            ///The first X button was pressed or released.
            ///XBUTTON2
            ///The second X button was pressed or released.
            /// </summary>
            public int mouseData;

            /// <summary>
            /// Specifies the event-injected flag. An application can use the following value to test the mouse flags. Value Purpose 
            ///LLMHF_INJECTED Test the event-injected flag.  
            ///0
            ///Specifies whether the event was injected. The value is 1 if the event was injected; otherwise, it is 0.
            ///1-15
            ///Reserved.
            /// </summary>
            public int flags;

            /// <summary>
            /// Specifies the time stamp for this message.
            /// </summary>
            public int time;

            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int dwExtraInfo;
        }

        /// <summary>
        /// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
        /// </summary>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private class KeyboardHookStruct
        {
            /// <summary>
            /// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
            /// </summary>
            public int vkCode;
            /// <summary>
            /// Specifies a hardware scan code for the key. 
            /// </summary>
            public int scanCode;
            /// <summary>
            /// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
            /// </summary>
            public int flags;
            /// <summary>
            /// Specifies the time stamp for this message.
            /// </summary>
            public int time;
            /// <summary>
            /// Specifies extra information associated with the message. 
            /// </summary>
            public int dwExtraInfo;
        }

        #endregion

        #region Windows function imports

        /// <summary>
        /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain. 
        /// You would install a hook procedure to monitor the system for certain types of events. These events 
        /// are associated either with a specific thread or with all threads in the same desktop as the calling thread. 
        /// </summary>
        /// <param name="idHook">
        /// [in] Specifies the type of hook procedure to be installed. This parameter can be one of the following values.
        /// </param>
        /// <param name="lpfn">
        /// [in] Pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a 
        /// thread created by a different process, the lpfn parameter must point to a hook procedure in a dynamic-link 
        /// library (DLL). Otherwise, lpfn can point to a hook procedure in the code associated with the current process.
        /// </param>
        /// <param name="hMod">
        /// [in] Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. 
        /// The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by 
        /// the current process and if the hook procedure is within the code associated with the current process. 
        /// </param>
        /// <param name="dwThreadId">
        /// [in] Specifies the identifier of the thread with which the hook procedure is to be associated. 
        /// If this parameter is zero, the hook procedure is associated with all existing threads running in the 
        /// same desktop as the calling thread. 
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is the handle to the hook procedure.
        /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        /// <summary>
        /// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. 
        /// </summary>
        /// <param name="idHook">
        /// [in] Handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx. 
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int UnhookWindowsHookEx(int idHook);

        /// <summary>
        /// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain. 
        /// A hook procedure can call this function either before or after processing the hook information. 
        /// </summary>
        /// <param name="idHook">Ignored.</param>
        /// <param name="nCode">
        /// [in] Specifies the hook code passed to the current hook procedure. 
        /// The next hook procedure uses this code to determine how to process the hook information.
        /// </param>
        /// <param name="wParam">
        /// [in] Specifies the wParam value passed to the current hook procedure. 
        /// The meaning of this parameter depends on the type of hook associated with the current hook chain. 
        /// </param>
        /// <param name="lParam">
        /// [in] Specifies the lParam value passed to the current hook procedure. 
        /// The meaning of this parameter depends on the type of hook associated with the current hook chain. 
        /// </param>
        /// <returns>
        /// This value is returned by the next hook procedure in the chain. 
        /// The current hook procedure must also return this value. The meaning of the return value depends on the hook type. 
        /// For more information, see the descriptions of the individual hook procedures.
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        /// <summary>
        /// The CallWndProc hook procedure is an application-defined or library-defined callback 
        /// function used with the SetWindowsHookEx function. The HOOKPROC type defines a pointer 
        /// to this callback function. CallWndProc is a placeholder for the application-defined 
        /// or library-defined function name.
        /// </summary>
        /// <param name="nCode">
        /// [in] Specifies whether the hook procedure must process the message. 
        /// If nCode is HC_ACTION, the hook procedure must process the message. 
        /// If nCode is less than zero, the hook procedure must pass the message to the 
        /// CallNextHookEx function without further processing and must return the 
        /// value returned by CallNextHookEx.
        /// </param>
        /// <param name="wParam">
        /// [in] Specifies whether the message was sent by the current thread. 
        /// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
        /// </param>
        /// <param name="lParam">
        /// [in] Pointer to a CWPSTRUCT structure that contains details about the message. 
        /// </param>
        /// <returns>
        /// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
        /// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
        /// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
        /// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
        /// procedure does not call CallNextHookEx, the return value should be zero. 
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/callwndproc.asp
        /// </remarks>
        private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        /// <summary>
        /// The ToAscii function translates the specified virtual-key code and keyboard 
        /// state to the corresponding character or characters. The function translates the code 
        /// using the input language and physical keyboard layout identified by the keyboard layout handle.
        /// </summary>
        /// <param name="uVirtKey">
        /// [in] Specifies the virtual-key code to be translated. 
        /// </param>
        /// <param name="uScanCode">
        /// [in] Specifies the hardware scan code of the key to be translated. 
        /// The high-order bit of this value is set if the key is up (not pressed). 
        /// </param>
        /// <param name="lpbKeyState">
        /// [in] Pointer to a 256-byte array that contains the current keyboard state. 
        /// Each element (byte) in the array contains the state of one key. 
        /// If the high-order bit of a byte is set, the key is down (pressed). 
        /// The low bit, if set, indicates that the key is toggled on. In this function, 
        /// only the toggle bit of the CAPS LOCK key is relevant. The toggle state 
        /// of the NUM LOCK and SCROLL LOCK keys is ignored.
        /// </param>
        /// <param name="lpwTransKey">
        /// [out] Pointer to the buffer that receives the translated character or characters. 
        /// </param>
        /// <param name="fuState">
        /// [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise. 
        /// </param>
        /// <returns>
        /// If the specified key is a dead key, the return value is negative. Otherwise, it is one of the following values. 
        /// Value Meaning 
        /// 0 The specified virtual key has no translation for the current state of the keyboard. 
        /// 1 One character was copied to the buffer. 
        /// 2 Two characters were copied to the buffer. This usually happens when a dead-key character 
        /// (accent or diacritic) stored in the keyboard layout cannot be composed with the specified 
        /// virtual key to form a single character. 
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/userinput/keyboardinput/keyboardinputreference/keyboardinputfunctions/toascii.asp
        /// </remarks>
        [DllImport("user32")]
        private static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);

        /// <summary>
        /// The GetKeyboardState function copies the status of the 256 virtual keys to the 
        /// specified buffer. 
        /// </summary>
        /// <param name="pbKeyState">
        /// [in] Pointer to a 256-byte array that contains keyboard key states. 
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError. 
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/userinput/keyboardinput/keyboardinputreference/keyboardinputfunctions/toascii.asp
        /// </remarks>
        [DllImport("user32")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        #endregion

        public enum MouseEventType
        {
            MouseMove = 0x200,
            LeftButtonDown = 0x201,
            LeftButtonUp = 0x202,
            LeftButtonDoubleClick = 0x203,
            RightButtonDown = 0x204,
            RightButtonUp = 0x205,
            RightButtonDoubleClick = 0x206,
            MiddleButtonDown = 0x207,
            MiddleButtonUp = 0x208,
            MiddleButtonDoubleClick = 0x209,
            MouseWheel = 0x020A,

            ExtraButtonDown = 0x020B,
            ExtraButtonUp = 0x020C,
            ExtraButtonDoubleClick = 0x020D,
            Extra2ButtonDown = 0x00AB,
            Extra2ButtonUp = 0x00AC,
            Extra2ButtonDoubleClick = 0x00AD,
        }

        #region Windows constants from Winuser.h in Microsoft SDK.

        /// <summary>
        /// Windows NT/2000/XP: Installs a hook procedure that monitors low-level mouse input events.
        /// </summary>
        private const int WH_MOUSE_LL = 14;
        /// <summary>
        /// Windows NT/2000/XP: Installs a hook procedure that monitors low-level keyboard  input events.
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook procedure. 
        /// </summary>
        private const int WH_MOUSE = 7;
        /// <summary>
        /// Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc hook procedure. 
        /// </summary>
        private const int WH_KEYBOARD = 2;

        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard focus when a nonsystem 
        /// key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed.
        /// </summary>
        private const int WM_KEYDOWN = 0x100;

        /// <summary>
        /// The WM_KEYUP message is posted to the window with the keyboard focus when a nonsystem 
        /// key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, 
        /// or a keyboard key that is pressed when a window has the keyboard focus.
        /// </summary>
        private const int WM_KEYUP = 0x101;

        /// <summary>
        /// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when the user 
        /// presses the F10 key (which activates the menu bar) or holds down the ALT key and then 
        /// presses another key. It also occurs when no window currently has the keyboard focus; 
        /// in this case, the WM_SYSKEYDOWN message is sent to the active window. The window that 
        /// receives the message can distinguish between these two contexts by checking the context 
        /// code in the lParam parameter. 
        /// </summary>
        private const int WM_SYSKEYDOWN = 0x104;

        /// <summary>
        /// The WM_SYSKEYUP message is posted to the window with the keyboard focus when the user 
        /// releases a key that was pressed while the ALT key was held down. It also occurs when no 
        /// window currently has the keyboard focus; in this case, the WM_SYSKEYUP message is sent 
        /// to the active window. The window that receives the message can distinguish between 
        /// these two contexts by checking the context code in the lParam parameter. 
        /// </summary>
        private const int WM_SYSKEYUP = 0x105;

        private const byte VK_SHIFT = 0x10;
        private const byte VK_CAPITAL = 0x14;
        private const byte VK_NUMLOCK = 0x90;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of UserActivityHook object and sets mouse and keyboard hooks.
        /// </summary>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public UserActivityHook()
        {
            Start();
        }

        /// <summary>
        /// Creates an instance of UserActivityHook object and installs both or one of mouse and/or keyboard hooks and starts rasing events
        /// </summary>
        /// <param name="installMouseHook"><b>true</b> if mouse events must be monitored</param>
        /// <param name="installKeyboardHook"><b>true</b> if keyboard events must be monitored</param>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        /// <remarks>
        /// To create an instance without installing hooks call new UserActivityHook(false, false)
        /// </remarks>
        public UserActivityHook(bool installMouseHook, bool installKeyboardHook)
        {
            Start(installMouseHook, installKeyboardHook);
        }

        /// <summary>
        /// Destruction.
        /// </summary>
        ~UserActivityHook()
        {
            //uninstall hooks and do not throw exceptions
            Stop(true, true, false);
        }

        #endregion

        #region Variables

        /// <summary>
        /// Custom Mouse Event Handler, Since the WPF version it's way different from the WinForms.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event Args.</param>
        public delegate void CustomMouseEventHandler(object sender, CustomMouseEventArgs e);

        /// <summary>
        /// Custom Key Event Handler, Since the WPF version it's way different from the WinForms.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event Args.</param>
        public delegate void CustomKeyEventHandler(object sender, CustomKeyEventArgs e);

        /// <summary>
        /// Custom KeyPress Event Handler, Since the WPF version it's way different from the WinForms.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event Args.</param>
        public delegate void CustomKeyPressEventHandler(object sender, CustomKeyPressEventArgs e);

        /// <summary>
        /// Custom KeyUp Event Handler, Since the WPF version it's way different from the WinForms.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event Args.</param>
        public delegate void CustomKeyUpEventHandler(object sender, CustomKeyEventArgs e);

        /// <summary>
        /// Occurs when the user moves the mouse, presses any mouse button or scrolls the wheel
        /// </summary>
        public event CustomMouseEventHandler OnMouseActivity;
        //public event MouseEventHandler OnMouseActivity;

        /// <summary>
        /// Occurs when the user presses a key
        /// </summary>
        public event CustomKeyEventHandler KeyDown;

        ///// <summary>
        ///// Occurs when the user presses and releases 
        ///// </summary>
        public event CustomKeyPressEventHandler KeyPress;

        /// <summary>
        /// Occurs when the user releases a key
        /// </summary>
        public event CustomKeyEventHandler KeyUp;

        /// <summary>
        /// Stores the handle to the mouse hook procedure.
        /// </summary>
        private int _hMouseHook = 0;
        /// <summary>
        /// Stores the handle to the keyboard hook procedure.
        /// </summary>
        private int _hKeyboardHook = 0;

        /// <summary>
        /// Declare MouseHookProcedure as HookProc type.
        /// </summary>
        private static HookProc _mouseHookProcedure;
        /// <summary>
        /// Declare KeyboardHookProcedure as HookProc type.
        /// </summary>
        private static HookProc _keyboardHookProcedure;

        #endregion

        #region Start/Stop

        /// <summary>
        /// Installs both mouse and keyboard hooks and starts rasing events
        /// </summary>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Start()
        {
            Start(true, true);
        }

        /// <summary>
        /// Installs both or one of mouse and/or keyboard hooks and starts rasing events
        /// </summary>
        /// <param name="installMouseHook"><b>true</b> if mouse events must be monitored</param>
        /// <param name="installKeyboardHook"><b>true</b> if keyboard events must be monitored</param>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Start(bool installMouseHook, bool installKeyboardHook)
        {
            //Gets the system info
            var osInfo = Environment.OSVersion;

            //Install Mouse hook only if it is not installed and must be installed
            if (_hMouseHook == 0 && installMouseHook)
            {
                //Create an instance of HookProc.
                _mouseHookProcedure = MouseHookProc;

                //XP bug... - Nicke SM
                if (osInfo.Version.Major < 6)
                    _hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
                else
                    _hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProcedure, IntPtr.Zero, 0);

                //If SetWindowsHookEx fails.
                if (_hMouseHook == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    //Do cleanup
                    Stop(true, false, false);
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }

            //Install Keyboard hook only if it is not installed and must be installed
            if (_hKeyboardHook == 0 && installKeyboardHook)
            {
                //Create an instance of HookProc.
                _keyboardHookProcedure = KeyboardHookProc;

                //Install hook
                //XP bug... - Nicke SM
                if (osInfo.Version.Major < 6)
                    _hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
                else
                    _hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProcedure, IntPtr.Zero, 0);

                //If SetWindowsHookEx fails.
                if (_hKeyboardHook == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    //do cleanup
                    Stop(false, true, false);
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }
        }

        /// <summary>
        /// Stops monitoring both mouse and keyboard events and rasing events.
        /// </summary>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Stop()
        {
            Stop(true, true, true);
        }

        /// <summary>
        /// Stops monitoring both or one of mouse and/or keyboard events and rasing events.
        /// </summary>
        /// <param name="uninstallMouseHook"><b>true</b> if mouse hook must be uninstalled</param>
        /// <param name="uninstallKeyboardHook"><b>true</b> if keyboard hook must be uninstalled</param>
        /// <param name="throwExceptions"><b>true</b> if exceptions which occured during uninstalling must be thrown</param>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Stop(bool uninstallMouseHook, bool uninstallKeyboardHook, bool throwExceptions)
        {
            //if mouse hook set and must be uninstalled
            if (_hMouseHook != 0 && uninstallMouseHook)
            {
                //uninstall hook
                var retMouse = UnhookWindowsHookEx(_hMouseHook);
                //reset invalid handle
                _hMouseHook = 0;
                //if failed and exception must be thrown
                if (retMouse == 0 && throwExceptions)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }

            //If keyboard hook set and must be uninstalled
            if (_hKeyboardHook != 0 && uninstallKeyboardHook)
            {
                //Uninstall hook
                var retKeyboard = UnhookWindowsHookEx(_hKeyboardHook);

                //Reset invalid handle
                _hKeyboardHook = 0;

                //If failed and exception must be thrown
                if (retKeyboard == 0 && throwExceptions)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }
        }

        #endregion

        #region Event Triggers

        private MouseButtonState _leftButton = MouseButtonState.Released;
        private MouseButtonState _rightButton = MouseButtonState.Released;
        private MouseButtonState _middleButton = MouseButtonState.Released;
        static DateTime _lastClickTime; //For double click detection.
        static int _clickCount; //For double click detection.

        /// <summary>
        /// A callback function which will be called every time a mouse activity detected.
        /// </summary>
        /// <param name="nCode">
        /// [in] Specifies whether the hook procedure must process the message. 
        /// If nCode is HC_ACTION, the hook procedure must process the message. 
        /// If nCode is less than zero, the hook procedure must pass the message to the 
        /// CallNextHookEx function without further processing and must return the 
        /// value returned by CallNextHookEx.
        /// </param>
        /// <param name="wParam">
        /// [in] Specifies whether the message was sent by the current thread. 
        /// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
        /// </param>
        /// <param name="lParam">
        /// [in] Pointer to a CWPSTRUCT structure that contains details about the message. 
        /// </param>
        /// <returns>
        /// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
        /// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
        /// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
        /// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
        /// procedure does not call CallNextHookEx, the return value should be zero. 
        /// </returns>
        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            //If not ok and no one listens to our events, call next hook.
            if (nCode < 0 || OnMouseActivity == null)
                return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);

            //Marshall the data from callback.
            var mouse = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

            #region Switch Mouse Actions

            switch ((MouseEventType)wParam)
            {
                case MouseEventType.MouseMove:
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.MouseMove, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.ExtraButtonDown:
                case MouseEventType.LeftButtonDown:
                    var deltaMs = DateTime.Now - _lastClickTime;
                    _lastClickTime = DateTime.Now;

                    if (deltaMs.TotalMilliseconds <= Native.GetDoubleClickTime())
                        _clickCount++;
                    else
                        _clickCount = 1;

                    if (_clickCount == 2)
                    {
                        OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.LeftButtonDoubleClick, _leftButton, _rightButton, _middleButton));
                        _clickCount = 0;
                    }

                    _leftButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.LeftButtonDown, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.ExtraButtonUp:
                case MouseEventType.LeftButtonUp:
                    _leftButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.LeftButtonUp, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.ExtraButtonDoubleClick:
                case MouseEventType.LeftButtonDoubleClick:
                    _leftButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.LeftButtonDoubleClick, _leftButton, _rightButton, _middleButton));
                    _leftButton = MouseButtonState.Released;
                    break;

                case MouseEventType.Extra2ButtonDown:
                case MouseEventType.RightButtonDown:
                    _rightButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.RightButtonDown, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.Extra2ButtonUp:
                case MouseEventType.RightButtonUp:
                    _rightButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.RightButtonUp, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.Extra2ButtonDoubleClick:
                case MouseEventType.RightButtonDoubleClick:
                    _rightButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.RightButtonDoubleClick, _leftButton, _rightButton, _middleButton));
                    _rightButton = MouseButtonState.Released;
                    break;

                case MouseEventType.MiddleButtonDown:
                    _middleButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.MiddleButtonDown, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.MiddleButtonUp:
                    _middleButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.MiddleButtonUp, _leftButton, _rightButton, _middleButton));
                    break;

                case MouseEventType.MiddleButtonDoubleClick:
                    _middleButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.MiddleButtonDoubleClick, _leftButton, _rightButton, _middleButton));
                    _middleButton = MouseButtonState.Released;
                    break;

                case MouseEventType.MouseWheel:
                    //If the message is WM_MOUSEWHEEL, the high-order word of mouseData member is the wheel delta. 
                    //One wheel click is defined as WHEEL_DELTA, which is 120. 
                    //(value >> 16) & 0xffff; retrieves the high-order word from the given 32-bit value
                    OnMouseActivity?.Invoke(this, new CustomMouseEventArgs(mouse.pt.X, mouse.pt.Y, MouseEventType.MouseWheel, _leftButton, _rightButton, _middleButton, (short)((mouse.mouseData >> 16) & 0xffff)));
                    break;

                    //If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, 
                    //or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
                    //and the low-order word is reserved. This value can be one or more of the following values. 
                    //Otherwise, mouseData is not used. 

                    //default: I can't return now, it will break the click detector.
                    //return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
                    //HU3HU3 - A little funny momment: I just frooze my cursor by returning 1 instead of calling the next hook. - Nicke
                    //Congrats to myself. ;D 
                    //05:24 AM 01/02/2014 (day-month-year)
            }

            #endregion

            //Call next hook
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam); //Not sure why, but it throws me an error.
        }

        /// <summary>
        /// A callback function which will be called every time a keyboard activity detected.
        /// </summary>
        /// <param name="nCode">
        /// [in] Specifies whether the hook procedure must process the message. 
        /// If nCode is HC_ACTION, the hook procedure must process the message. 
        /// If nCode is less than zero, the hook procedure must pass the message to the 
        /// CallNextHookEx function without further processing and must return the 
        /// value returned by CallNextHookEx.
        /// </param>
        /// <param name="wParam">
        /// [in] Specifies whether the message was sent by the current thread. 
        /// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
        /// </param>
        /// <param name="lParam">
        /// [in] Pointer to a CWPSTRUCT structure that contains details about the message. 
        /// </param>
        /// <returns>
        /// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
        /// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
        /// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
        /// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
        /// procedure does not call CallNextHookEx, the return value should be zero. 
        /// </returns>
        private int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            //Indicates if any of underlaing events set e.Handled flag
            var handled = false;

            //If was Ok and someone listens to events
            if (nCode < 0 || KeyDown == null && KeyUp == null && KeyPress == null)
                return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);

            //Read structure KeyboardHookStruct at lParam
            var myKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

            if (KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
            {
                #region Raise KeyDown

                var isDownShift = (GetKeyState(VK_SHIFT) & 0x80) == 0x80;
                var isDownCapslock = GetKeyState(VK_CAPITAL) != 0;

                var e = new CustomKeyEventArgs(KeyInterop.KeyFromVirtualKey(myKeyboardHookStruct.vkCode), isDownCapslock ^ isDownShift);

                KeyDown?.Invoke(this, e);

                handled = e.Handled;

                #endregion
            }

            if (KeyPress != null && wParam == WM_KEYDOWN)
            {
                #region Raise KeyPress

                var isDownShift = (GetKeyState(VK_SHIFT) & 0x80) == 0x80;
                var isDownCapslock = GetKeyState(VK_CAPITAL) != 0;

                var keyState = new byte[256];
                GetKeyboardState(keyState);
                var inBuffer = new byte[2];

                if (ToAscii(myKeyboardHookStruct.vkCode, myKeyboardHookStruct.scanCode, keyState, inBuffer, myKeyboardHookStruct.flags) == 1)
                {
                    var key = (char)inBuffer[0];
                    if (isDownCapslock ^ isDownShift && char.IsLetter(key))
                        key = char.ToUpper(key);

                    var e = new CustomKeyPressEventArgs(key);
                    KeyPress?.Invoke(this, e);

                    handled = handled || e.Handled;
                }

                #endregion
            }

            if (KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
            {
                #region Raise KeyUp

                var e = new CustomKeyEventArgs(KeyInterop.KeyFromVirtualKey(myKeyboardHookStruct.vkCode));

                KeyUp?.Invoke(this, e);

                handled = handled || e.Handled;

                #endregion
            }

            //If event handled in application do not handoff to other listeners
            return handled ? 1 : CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }

        #endregion
    }
}