using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace ScreenToGif.Util.InputHook
{
    /// <summary>
    /// This class allows you to tap keyboard and mouse and / or to detect their activity even when an 
    /// application runs in background or does not have any user interface at all. This class raises 
    /// common .NET events with KeyEventArgs and MouseEventArgs so you can easily retrive any information you need.
    /// </summary>
    public class InputHook
    {
        #region Windows constants from Winuser.h in Microsoft SDK.

        /// <summary>
        /// User32 library name.
        /// </summary>
        private const string User32 = "user32.dll";

        /// <summary>
        /// Hook id for monitoring low-level mouse input events.
        /// </summary>
        private const int HookMouseLowLevel = 14;

        /// <summary>
        /// Hook id for monitoring low-level keyboard input events.
        /// </summary>
        private const int HookKeyboardLowLevel = 13;

        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard focus when a nonsystem 
        /// key is pressed. A nonsystem key is a key that is pressed when the ALT key is not pressed.
        /// </summary>
        private const int MessageKeydown = 0x100;

        /// <summary>
        /// The WM_KEYUP message is posted to the window with the keyboard focus when a nonsystem 
        /// key is released. A nonsystem key is a key that is pressed when the ALT key is not pressed, 
        /// or a keyboard key that is pressed when a window has the keyboard focus.
        /// </summary>
        private const int MessageKeyUp = 0x101;

        /// <summary>
        /// The WM_SYSKEYDOWN message is posted to the window with the keyboard focus when the user 
        /// presses the F10 key (which activates the menu bar) or holds down the ALT key and then 
        /// presses another key. It also occurs when no window currently has the keyboard focus; 
        /// in this case, the WM_SYSKEYDOWN message is sent to the active window. The window that 
        /// receives the message can distinguish between these two contexts by checking the context 
        /// code in the lParam parameter. 
        /// </summary>
        private const int MessageSystemKeyDown = 0x104;

        /// <summary>
        /// The WM_SYSKEYUP message is posted to the window with the keyboard focus when the user 
        /// releases a key that was pressed while the ALT key was held down. It also occurs when no 
        /// window currently has the keyboard focus; in this case, the WM_SYSKEYUP message is sent 
        /// to the active window. The window that receives the message can distinguish between 
        /// these two contexts by checking the context code in the lParam parameter. 
        /// </summary>
        private const int MessageSystemKeyUp = 0x105;

        private const byte KeyShift = 0x10;
        private const byte KeyCapital = 0x14;
        private const byte KeyNumLock = 0x90;

        private const byte MouseFirstExtraButton = 0x0001;
        private const byte MouseSecondExtraButton = 0x0002;

        #endregion
        
        #region Windows structure definitions

        /// <summary>
        /// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard input event.
        /// </summary>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-msllhookstruct
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private class MouseHookStruct
        {
            /// <summary>
            /// Specifies a POINT structure that contains the X and Y coordinates of the cursor, in screen coordinates. 
            /// </summary>
            public Native.PointW Point;

            /// <summary>
            /// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta. 
            /// The low-order word is reserved. A positive value indicates that the wheel was rotated forward, 
            /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user. 
            /// One wheel click is defined as WHEEL_DELTA, which is 120. 
            ///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
            /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
            /// and the low-order word is reserved.
            /// </summary>
            public uint MouseData;

            /// <summary>
            /// Specifies the event-injected flag. An application can use the following value to test the mouse flags.
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

        public enum MouseEventType
        {
            MouseMove = 0x200,
            MouseDragStart = 0x00AE,
            MouseDragEnd = 0x00AF,

            LeftButtonDown = 0x201,
            LeftButtonUp = 0x202,
            LeftButtonDoubleClick = 0x203,
            OutsideLeftButtonDown = 0x00A1,
            OutsideLeftButtonUp = 0x00A2,
            OutsideLeftButtonDoubleClick = 0x00A3,

            RightButtonDown = 0x204,
            RightButtonUp = 0x205,
            RightButtonDoubleClick = 0x206,
            OutsideRightButtonDown = 0x00A4,
            OutsideRightButtonUp = 0x00A5,
            OutsideRightButtonDoubleClick = 0x00A6,

            MiddleButtonDown = 0x207,
            MiddleButtonUp = 0x208,
            MiddleButtonDoubleClick = 0x209,
            OutsideMiddleButtonDown = 0x00A7,
            OutsideMiddleButtonUp = 0x00A8,
            OutsideMiddleButtonDoubleClick = 0x00A9,
            
            MouseWheel = 0x020A,
            MouseWheelHorizontal = 0x020E,

            ExtraButtonDown = 0x020B,
            ExtraButtonUp = 0x020C,
            ExtraButtonDoubleClick = 0x020D,
            OutsideExtraButtonDown = 0x00AB,
            OutsideExtraButtonUp = 0x00AC,
            OutsideExtraButtonDoubleClick = 0x00AD
        }

        #endregion

        #region Windows function imports

        /// <summary>
        /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain. 
        /// You would install a hook procedure to monitor the system for certain types of events. These events 
        /// are associated either with a specific thread or with all threads in the same desktop as the calling thread. 
        /// </summary>
        /// <param name="idHook">
        /// Specifies the type of hook procedure to be installed. This parameter can be one of the following values.
        /// </param>
        /// <param name="lpfn">
        /// Pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a 
        /// thread created by a different process, the lpfn parameter must point to a hook procedure in a dynamic-link 
        /// library (DLL). Otherwise, lpfn can point to a hook procedure in the code associated with the current process.
        /// </param>
        /// <param name="hMod">
        /// Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. 
        /// The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by 
        /// the current process and if the hook procedure is within the code associated with the current process. 
        /// </param>
        /// <param name="dwThreadId">
        /// Specifies the identifier of the thread with which the hook procedure is to be associated. 
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
        [DllImport(User32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
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
        [DllImport(User32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
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
        [DllImport(User32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CallNextHookEx(int idHook, int nCode, uint wParam, IntPtr lParam);

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
        private delegate IntPtr HookProc(int nCode, uint wParam, IntPtr lParam);

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
        [DllImport(User32)]
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
        [DllImport(User32)]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport(User32, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern short GetKeyState(int vKey);

        #endregion


        #region Variables

        /// <summary>
        /// Custom Mouse Event Handler.
        /// </summary>
        public delegate void CustomMouseEventHandler(object sender, SimpleMouseGesture e);

        /// <summary>
        /// Custom Key Event Handler.
        /// </summary>
        public delegate void CustomKeyEventHandler(object sender, CustomKeyEventArgs e);

        /// <summary>
        /// Custom KeyPress Event Handler.
        /// </summary>
        public delegate void CustomKeyPressEventHandler(object sender, CustomKeyPressEventArgs e);

        /// <summary>
        /// Custom KeyUp Event Handler.
        /// </summary>
        public delegate void CustomKeyUpEventHandler(object sender, CustomKeyEventArgs e);


        /// <summary>
        /// Occurs when the user moves the mouse, presses any mouse button or scrolls the wheel.
        /// </summary>
        public event CustomMouseEventHandler OnMouseActivity;

        /// <summary>
        /// Occurs when the user presses a key.
        /// </summary>
        public event CustomKeyEventHandler KeyDown;

        ///// <summary>
        ///// Occurs when the user presses and releases.
        ///// </summary>
        public event CustomKeyPressEventHandler KeyPress;

        /// <summary>
        /// Occurs when the user releases a key.
        /// </summary>
        public event CustomKeyEventHandler KeyUp;

        /// <summary>
        /// Stores the handle to the mouse hook procedure.
        /// </summary>
        private int _mouseHookHandle = 0;

        /// <summary>
        /// Stores the handle to the keyboard hook procedure.
        /// </summary>
        private int _keyboardHookHandle = 0;

        /// <summary>
        /// Declare MouseHookProcedure as HookProc type.
        /// </summary>
        private static HookProc _mouseHookProcedure;

        /// <summary>
        /// Declare KeyboardHookProcedure as HookProc type.
        /// </summary>
        private static HookProc _keyboardHookProcedure;

        private MouseButtonState _leftButton = MouseButtonState.Released;
        private MouseButtonState _rightButton = MouseButtonState.Released;
        private MouseButtonState _middleButton = MouseButtonState.Released;
        private MouseButtonState _extraButton = MouseButtonState.Released;
        private MouseButtonState _extra2Button = MouseButtonState.Released;

        private static DateTime _lastClickTime;
        private static int _clickCount;
        private static bool _isDragging;
        private static double _horizontalDragThreshold;
        private static double _verticalDragThreshold;
        private static Native.PointW _dragStartPoint;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of UserActivityHook object and sets mouse and keyboard hooks.
        /// </summary>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public InputHook()
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
        public InputHook(bool installMouseHook, bool installKeyboardHook)
        {
            Start(installMouseHook, installKeyboardHook);
        }

        /// <summary>
        /// Destruction.
        /// </summary>
        ~InputHook()
        {
            //uninstall hooks and do not throw exceptions
            Stop(true, true, false);
        }

        #endregion

        #region Methods

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
            //Install Mouse hook only if it is not installed and must be installed
            if (_mouseHookHandle == 0 && installMouseHook)
            {
                //Get minimum drag thresholds
                _horizontalDragThreshold = SystemParameters.MinimumHorizontalDragDistance;
                _verticalDragThreshold = SystemParameters.MinimumVerticalDragDistance;

                //Create an instance of HookProc.
                _mouseHookProcedure = MouseHookProc;

                //Install hook.
                _mouseHookHandle = SetWindowsHookEx(HookMouseLowLevel, _mouseHookProcedure, IntPtr.Zero, 0);

                //If SetWindowsHookEx fails.
                if (_mouseHookHandle == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    
                    //Cleans up.
                    Stop(true, false, false);
                    
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }

            //Install Keyboard hook only if it is not installed and must be installed
            if (_keyboardHookHandle == 0 && installKeyboardHook)
            {
                //Create an instance of HookProc.
                _keyboardHookProcedure = KeyboardHookProc;

                //Install hook
                _keyboardHookHandle = SetWindowsHookEx(HookKeyboardLowLevel, _keyboardHookProcedure, IntPtr.Zero, 0);

                //If SetWindowsHookEx fails.
                if (_keyboardHookHandle == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    
                    //Cleans up.
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
            if (_mouseHookHandle != 0 && uninstallMouseHook)
            {
                //Uninstalls the hook.
                var retMouse = UnhookWindowsHookEx(_mouseHookHandle);
                
                //Resets the invalid handle.
                _mouseHookHandle = 0;
                
                //if failed and exception must be thrown.
                if (retMouse == 0 && throwExceptions)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                    var errorCode = Marshal.GetLastWin32Error();
                    
                    //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                    throw new Win32Exception(errorCode);
                }
            }

            //If keyboard hook set and must be uninstalled
            if (_keyboardHookHandle != 0 && uninstallKeyboardHook)
            {
                //Uninstalls the hook.
                var retKeyboard = UnhookWindowsHookEx(_keyboardHookHandle);

                //Resets the invalid handle.
                _keyboardHookHandle = 0;

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


        private void DetectDoubleClick(MouseEventType type, Native.PointW point)
        {
            var deltaMs = DateTime.Now - _lastClickTime;

            _lastClickTime = DateTime.Now;

            if (deltaMs.TotalMilliseconds <= Native.GetDoubleClickTime())
                _clickCount++;
            else
                _clickCount = 1;

            if (_clickCount != 2)
                return;
            
            OnMouseActivity?.Invoke(this, new SimpleMouseGesture(type, point.X, point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
            _clickCount = 0;
        }

        /// <summary>
        /// A callback function which will be called every time a mouse activity detected.
        /// https://docs.microsoft.com/en-us/windows/win32/winmsg/lowlevelmouseproc
        /// </summary>
        /// <param name="code">
        /// Specifies whether the hook procedure must process the message. 
        /// If code is HC_ACTION, the hook procedure must process the message. 
        /// If code is less than zero, the hook procedure must pass the message to the 
        /// CallNextHookEx function without further processing and must return the value returned by CallNextHookEx.
        /// </param>
        /// <param name="type">
        /// Same as wParam. Specifies whether the message was sent by the current thread. 
        /// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
        /// </param>
        /// <param name="structure">
        /// Same as lParam. Pointer to a CWPSTRUCT structure that contains details about the message. 
        /// </param>
        /// <returns>
        /// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
        /// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
        /// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
        /// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
        /// procedure does not call CallNextHookEx, the return value should be zero. 
        /// </returns>
        private IntPtr MouseHookProc(int code, uint type, IntPtr structure)
        {
            //If it's not Ok or no one listens to this event, call next hook.
            if (code < 0 || OnMouseActivity == null)
                return CallNextHookEx(_mouseHookHandle, code, type, structure);

            //Marshall the data from callback.
            var mouse = (MouseHookStruct) Marshal.PtrToStructure(structure, typeof(MouseHookStruct));
            var data = new WordLevel.WordUnion { Number = mouse.MouseData };
            
            #region Mouse actions

            switch ((MouseEventType) type)
            {
                case MouseEventType.MouseMove:
                {
                    if (!_isDragging && _leftButton == MouseButtonState.Pressed)
                    {
                        var isXDragging = Math.Abs(mouse.Point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
                        var isYDragging = Math.Abs(mouse.Point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;
                        
                        _isDragging = isXDragging || isYDragging;

                        if (_isDragging)
                        {
                            OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MouseDragStart, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                            break;
                        }
                    }
                    
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MouseMove, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideLeftButtonDown:
                case MouseEventType.LeftButtonDown:
                {
                    DetectDoubleClick(MouseEventType.LeftButtonDoubleClick, mouse.Point);

                    _leftButton = MouseButtonState.Pressed;
                    _dragStartPoint = mouse.Point;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.LeftButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideLeftButtonUp:
                case MouseEventType.LeftButtonUp:
                {
                    //End drag.
                    if (_isDragging)
                    {
                        OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MouseDragEnd, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                        _isDragging = false;
                    }

                    _leftButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.LeftButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideLeftButtonDoubleClick:
                case MouseEventType.LeftButtonDoubleClick:
                {
                    _leftButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.LeftButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    _leftButton = MouseButtonState.Released;
                    break;
                }

                case MouseEventType.OutsideRightButtonDown:
                case MouseEventType.RightButtonDown:
                {
                    DetectDoubleClick(MouseEventType.RightButtonDoubleClick, mouse.Point);

                    _rightButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.RightButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideRightButtonUp:
                case MouseEventType.RightButtonUp:
                {
                    _rightButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.RightButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideRightButtonDoubleClick:
                case MouseEventType.RightButtonDoubleClick:
                {
                    _rightButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.RightButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    _rightButton = MouseButtonState.Released;
                    break;
                }

                case MouseEventType.OutsideMiddleButtonDown:
                case MouseEventType.MiddleButtonDown:
                {
                    DetectDoubleClick(MouseEventType.MiddleButtonDoubleClick, mouse.Point);
                    
                    _middleButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MiddleButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideMiddleButtonUp:
                case MouseEventType.MiddleButtonUp:
                {
                    _middleButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MiddleButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideMiddleButtonDoubleClick:
                case MouseEventType.MiddleButtonDoubleClick:
                {
                    _middleButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MiddleButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    _middleButton = MouseButtonState.Released;
                    break;
                }

                case MouseEventType.MouseWheel:
                {
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MouseWheel, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button, data.High));
                    break;
                }

                case MouseEventType.MouseWheelHorizontal:
                {
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.MouseWheelHorizontal, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button, data.High));
                    break;
                }

                case MouseEventType.OutsideExtraButtonDown:
                case MouseEventType.ExtraButtonDown:
                {
                    DetectDoubleClick(MouseEventType.ExtraButtonDoubleClick, mouse.Point);

                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Pressed;
                    else
                        _extra2Button = MouseButtonState.Pressed;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.ExtraButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case MouseEventType.OutsideExtraButtonDoubleClick:
                case MouseEventType.ExtraButtonDoubleClick:
                {
                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Pressed;
                    else
                        _extra2Button = MouseButtonState.Pressed;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.ExtraButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));

                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Released;
                    else
                        _extra2Button = MouseButtonState.Released;
                    break;
                }

                case MouseEventType.OutsideExtraButtonUp:
                case MouseEventType.ExtraButtonUp:
                {
                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Released;
                    else
                        _extra2Button = MouseButtonState.Released;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(MouseEventType.ExtraButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                //default: I can't return now, it will break the click detector.
                //return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
                //HU3HU3 - A little funny momment: I just frooze my cursor by returning 1 instead of calling the next hook. - Nicke
                //Congrats to myself. ;D 
                //05:24 AM 01/02/2014 (day-month-year)
            }

            #endregion

            //Call the next hook.
            return CallNextHookEx(_mouseHookHandle, code, type, structure);
        }

        /// <summary>
        /// A callback function which will be called every time a keyboard activity detected.
        /// https://docs.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc
        /// </summary>
        /// <param name="code">
        /// Specifies whether the hook procedure must process the message. 
        /// If code is HC_ACTION, the hook procedure must process the message. 
        /// If code is less than zero, the hook procedure must pass the message to the 
        /// CallNextHookEx function without further processing and must return the 
        /// value returned by CallNextHookEx.
        /// </param>
        /// <param name="wParam">
        /// Specifies whether the message was sent by the current thread. 
        /// If the message was sent by the current thread, it is nonzero; otherwise, it is zero. 
        /// </param>
        /// <param name="lParam">
        /// Pointer to a CWPSTRUCT structure that contains details about the message. 
        /// </param>
        /// <returns>
        /// If code is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
        /// If code is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
        /// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
        /// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
        /// procedure does not call CallNextHookEx, the return value should be zero. 
        /// </returns>
        private IntPtr KeyboardHookProc(int code, uint wParam, IntPtr lParam)
        {
            //Indicates if any of the underlaying events set the e.Handled flag.
            var handled = false;

            //If it was Ok and there are no listeners.
            if (code < 0 || KeyDown == null && KeyUp == null && KeyPress == null)
                return CallNextHookEx(_keyboardHookHandle, code, wParam, lParam);

            //Read structure KeyboardHookStruct at lParam
            var keyboard = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
            var isInjected = (keyboard.Flags & 0x10) != 0;

            if (KeyDown != null && (wParam == MessageKeydown || wParam == MessageSystemKeyDown))
            {
                #region Raise KeyDown

                var isDownShift = (GetKeyState(KeyShift) & 0x80) == 0x80;
                var isDownCapslock = GetKeyState(KeyCapital) != 0;
             

                var e = new CustomKeyEventArgs(KeyInterop.KeyFromVirtualKey(keyboard.KeyCode), isDownCapslock ^ isDownShift, isInjected);

                KeyDown?.Invoke(this, e);

                handled = e.Handled;

                #endregion
            }

            if (KeyPress != null && wParam == MessageKeydown)
            {
                #region Raise KeyPress

                var isDownShift = (GetKeyState(KeyShift) & 0x80) == 0x80;
                var isDownCapslock = GetKeyState(KeyCapital) != 0;

                var keyState = new byte[256];
                GetKeyboardState(keyState);
                var inBuffer = new byte[2];

                if (ToAscii(keyboard.KeyCode, keyboard.ScanCode, keyState, inBuffer, keyboard.Flags) == 1)
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

            if (KeyUp != null && (wParam == MessageKeyUp || wParam == MessageSystemKeyUp))
            {
                #region Raise KeyUp

                var e = new CustomKeyEventArgs(KeyInterop.KeyFromVirtualKey(keyboard.KeyCode), false, isInjected);

                KeyUp?.Invoke(this, e);

                handled = handled || e.Handled;

                #endregion
            }

            //If event handled in application do not handoff to other listeners.
            return handled ? new IntPtr(1) : CallNextHookEx(_keyboardHookHandle, code, wParam, lParam);
        }

        #endregion
    }
}