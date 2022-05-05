using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Domain.Events;
using ScreenToGif.Domain.Models;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;

namespace ScreenToGif.Native.Helpers
{
    /// <summary>
    /// This class allows you to tap keyboard and mouse and / or to detect their activity even when an
    /// application runs in background or does not have any user interface at all. This class raises
    /// common .NET events with KeyEventArgs and MouseEventArgs so you can easily retrieve any information you need.
    /// </summary>
    public class InputHook
    {
        #region Windows constants from Winuser.h in Microsoft SDK.

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
        private static Delegates.HookProc _mouseHookProcedure;

        /// <summary>
        /// Declare KeyboardHookProcedure as HookProc type.
        /// </summary>
        private static Delegates.HookProc _keyboardHookProcedure;

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
        private static PointW _dragStartPoint;

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
        /// Creates an instance of UserActivityHook object and installs both or one of mouse and/or keyboard hooks and starts raising events
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
        /// Installs both mouse and keyboard hooks and starts raising events
        /// </summary>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Start()
        {
            Start(true, true);
        }

        /// <summary>
        /// Installs both or one of mouse and/or keyboard hooks and starts raising events
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
                _mouseHookHandle = User32.SetWindowsHookEx(HookMouseLowLevel, _mouseHookProcedure, IntPtr.Zero, 0);

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
                _keyboardHookHandle = User32.SetWindowsHookEx(HookKeyboardLowLevel, _keyboardHookProcedure, IntPtr.Zero, 0);

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
        /// Stops monitoring both mouse and keyboard events and raising events.
        /// </summary>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Stop()
        {
            Stop(true, true, true);
        }

        /// <summary>
        /// Stops monitoring both or one of mouse and/or keyboard events and raising events.
        /// </summary>
        /// <param name="uninstallMouseHook"><b>true</b> if mouse hook must be uninstalled</param>
        /// <param name="uninstallKeyboardHook"><b>true</b> if keyboard hook must be uninstalled</param>
        /// <param name="throwExceptions"><b>true</b> if exceptions which occurred during uninstalling must be thrown</param>
        /// <exception cref="Win32Exception">Any windows problem.</exception>
        public void Stop(bool uninstallMouseHook, bool uninstallKeyboardHook, bool throwExceptions)
        {
            //if mouse hook set and must be uninstalled
            if (_mouseHookHandle != 0 && uninstallMouseHook)
            {
                //Uninstalls the hook.
                var retMouse = User32.UnhookWindowsHookEx(_mouseHookHandle);

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
                var retKeyboard = User32.UnhookWindowsHookEx(_keyboardHookHandle);

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


        private void DetectDoubleClick(NativeMouseEvents type, PointW point)
        {
            var deltaMs = DateTime.Now - _lastClickTime;

            _lastClickTime = DateTime.Now;

            if (deltaMs.TotalMilliseconds <= User32.GetDoubleClickTime())
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
                return User32.CallNextHookEx(_mouseHookHandle, code, type, structure);

            //Marshall the data from callback.
            var mouse = (MouseHook) Marshal.PtrToStructure(structure, typeof(MouseHook));
            var data = new WordLevel.WordUnion { Number = mouse.MouseData };

            #region Mouse actions

            switch ((NativeMouseEvents) type)
            {
                case NativeMouseEvents.MouseMove:
                {
                    if (!_isDragging && _leftButton == MouseButtonState.Pressed)
                    {
                        var isXDragging = Math.Abs(mouse.Point.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance;
                        var isYDragging = Math.Abs(mouse.Point.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

                        _isDragging = isXDragging || isYDragging;

                        if (_isDragging)
                        {
                            OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MouseDragStart, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                            break;
                        }
                    }

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MouseMove, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideLeftButtonDown:
                case NativeMouseEvents.LeftButtonDown:
                {
                    DetectDoubleClick(NativeMouseEvents.LeftButtonDoubleClick, mouse.Point);

                    _leftButton = MouseButtonState.Pressed;
                    _dragStartPoint = mouse.Point;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.LeftButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideLeftButtonUp:
                case NativeMouseEvents.LeftButtonUp:
                {
                    //End drag.
                    if (_isDragging)
                    {
                        OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MouseDragEnd, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                        _isDragging = false;
                    }

                    _leftButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.LeftButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideLeftButtonDoubleClick:
                case NativeMouseEvents.LeftButtonDoubleClick:
                {
                    _leftButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.LeftButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    _leftButton = MouseButtonState.Released;
                    break;
                }

                case NativeMouseEvents.OutsideRightButtonDown:
                case NativeMouseEvents.RightButtonDown:
                {
                    DetectDoubleClick(NativeMouseEvents.RightButtonDoubleClick, mouse.Point);

                    _rightButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.RightButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideRightButtonUp:
                case NativeMouseEvents.RightButtonUp:
                {
                    _rightButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.RightButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideRightButtonDoubleClick:
                case NativeMouseEvents.RightButtonDoubleClick:
                {
                    _rightButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.RightButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    _rightButton = MouseButtonState.Released;
                    break;
                }

                case NativeMouseEvents.OutsideMiddleButtonDown:
                case NativeMouseEvents.MiddleButtonDown:
                {
                    DetectDoubleClick(NativeMouseEvents.MiddleButtonDoubleClick, mouse.Point);

                    _middleButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MiddleButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideMiddleButtonUp:
                case NativeMouseEvents.MiddleButtonUp:
                {
                    _middleButton = MouseButtonState.Released;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MiddleButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideMiddleButtonDoubleClick:
                case NativeMouseEvents.MiddleButtonDoubleClick:
                {
                    _middleButton = MouseButtonState.Pressed;
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MiddleButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    _middleButton = MouseButtonState.Released;
                    break;
                }

                case NativeMouseEvents.MouseWheel:
                {
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MouseWheel, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button, data.High));
                    break;
                }

                case NativeMouseEvents.MouseWheelHorizontal:
                {
                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.MouseWheelHorizontal, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button, data.High));
                    break;
                }

                case NativeMouseEvents.OutsideExtraButtonDown:
                case NativeMouseEvents.ExtraButtonDown:
                {
                    DetectDoubleClick(NativeMouseEvents.ExtraButtonDoubleClick, mouse.Point);

                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Pressed;
                    else
                        _extra2Button = MouseButtonState.Pressed;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.ExtraButtonDown, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
                    break;
                }

                case NativeMouseEvents.OutsideExtraButtonDoubleClick:
                case NativeMouseEvents.ExtraButtonDoubleClick:
                {
                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Pressed;
                    else
                        _extra2Button = MouseButtonState.Pressed;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.ExtraButtonDoubleClick, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));

                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Released;
                    else
                        _extra2Button = MouseButtonState.Released;
                    break;
                }

                case NativeMouseEvents.OutsideExtraButtonUp:
                case NativeMouseEvents.ExtraButtonUp:
                {
                    if (data.High == MouseFirstExtraButton)
                        _extraButton = MouseButtonState.Released;
                    else
                        _extra2Button = MouseButtonState.Released;

                    OnMouseActivity?.Invoke(this, new SimpleMouseGesture(NativeMouseEvents.ExtraButtonUp, mouse.Point.X, mouse.Point.Y, _leftButton, _rightButton, _middleButton, _extraButton, _extra2Button));
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
            return User32.CallNextHookEx(_mouseHookHandle, code, type, structure);
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
            //Indicates if any of the underlying events set the e.Handled flag.
            var handled = false;

            //If it was Ok and there are no listeners.
            if (code < 0 || KeyDown == null && KeyUp == null && KeyPress == null)
                return User32.CallNextHookEx(_keyboardHookHandle, code, wParam, lParam);

            //Read structure KeyboardHookStruct at lParam
            var keyboard = (KeyboardHook)Marshal.PtrToStructure(lParam, typeof(KeyboardHook));
            var isInjected = (keyboard.Flags & 0x10) != 0;

            if (KeyDown != null && (wParam == MessageKeydown || wParam == MessageSystemKeyDown))
            {
                #region Raise KeyDown

                var isDownShift = (User32.GetKeyState(KeyShift) & 0x80) == 0x80;
                var isDownCapslock = User32.GetKeyState(KeyCapital) != 0;


                var e = new CustomKeyEventArgs(KeyInterop.KeyFromVirtualKey(keyboard.KeyCode), isDownCapslock ^ isDownShift, isInjected);

                KeyDown?.Invoke(this, e);

                handled = e.Handled;

                #endregion
            }

            if (KeyPress != null && wParam == MessageKeydown)
            {
                #region Raise KeyPress

                var isDownShift = (User32.GetKeyState(KeyShift) & 0x80) == 0x80;
                var isDownCapslock = User32.GetKeyState(KeyCapital) != 0;

                var keyState = new byte[256];
                User32.GetKeyboardState(keyState);
                var inBuffer = new byte[2];

                if (User32.ToAscii(keyboard.KeyCode, keyboard.ScanCode, keyState, inBuffer, keyboard.Flags) == 1)
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
            return handled ? new IntPtr(1) : User32.CallNextHookEx(_keyboardHookHandle, code, wParam, lParam);
        }

        #endregion
    }
}