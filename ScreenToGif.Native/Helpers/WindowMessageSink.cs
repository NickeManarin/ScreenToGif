using System.ComponentModel;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.Helpers
{
    public class WindowMessageSink : IDisposable
    {
        #region Variables/Properties

        private readonly object _lock = new();

        /// <summary>
        /// The ID of messages that are received from the taskbar icon.
        /// </summary>
        public const int CallbackMessageId = 0x400;

        /// <summary>
        /// The ID of the message that is being received if the taskbar is (re)started.
        /// </summary>
        private uint _taskbarRestartMessageId;

        /// <summary>
        /// The number of clicks between the first click and all clicks in between the maximum amount of time of SystemInformation.DoubleClickTime.
        /// </summary>
        private int _clickCount = 0;

        /// <summary>
        /// A delegate that processes messages of the hidden native window that receives window messages. Storing
        /// this reference makes sure we don't loose our reference to the message window.
        /// </summary>
        private Delegates.WindowProcedureHandler _messageHandler;

        /// <summary>
        /// Timer used to detect double clicks and ignore unwanted single click events.
        /// </summary>
        private readonly System.Windows.Forms.Timer _doubleClick = new();

        /// <summary>
        /// Window class ID.
        /// </summary>
        internal string WindowId { get; private set; }

        /// <summary>
        /// Handle for the message window.
        /// </summary> 
        public IntPtr MessageWindowHandle { get; set; } = IntPtr.Zero;

        public bool IsDisposed { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// The custom tooltip should be closed or hidden.
        /// </summary>
        public event Action<bool> ChangeToolTipStateRequest;

        /// <summary>
        /// Fired in case the user clicked or moved within the taskbar icon area.
        /// </summary>
        public event Action<MouseEventType> MouseEventReceived;

        /// <summary>
        /// Fired if the taskbar was created or restarted. Requires the taskbar icon to be reset.
        /// </summary>
        public event Action TaskbarCreated;

        #endregion

        public WindowMessageSink()
        {
            CreateMessageWindow();

            _doubleClick.Interval = SystemInformation.DoubleClickTime;
            _doubleClick.Tick += DoubleClick_Tick;
        }

        private void DoubleClick_Tick(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (_clickCount <= 0)
                    return;

                MouseEventReceived?.Invoke(_clickCount > 1 ? MouseEventType.IconLeftDoubleClick : MouseEventType.IconLeftMouseUp);

                _clickCount = 0;
                _doubleClick.Stop();
            }
        }

        ~WindowMessageSink()
        {
            Dispose(false);
        }

        #region Methods

        /// <summary>
        /// Creates the helper message window that is used to receive messages from the taskbar icon.
        /// </summary>
        private void CreateMessageWindow()
        {
            //Generates a unique ID for the window.
            WindowId = "NotifyIcon_" + Guid.NewGuid();

            //Register window message handler.
            _messageHandler = OnWindowMessageReceived;

            //Creates a simple window class which is reference through the messageHandler delegate.
            WindowClass wc;
            wc.style = 0;
            wc.lpfnWndProc = _messageHandler;
            wc.cbClsExtra = 0;
            wc.cbWndExtra = 0;
            wc.hInstance = IntPtr.Zero;
            wc.hIcon = IntPtr.Zero;
            wc.hCursor = IntPtr.Zero;
            wc.hbrBackground = IntPtr.Zero;
            wc.lpszMenuName = "";
            wc.lpszClassName = WindowId;

            User32.RegisterClass(ref wc);

            //Gets the message used to indicate the taskbar has been restarted. This is used to re-add icons when the taskbar restarts;
            _taskbarRestartMessageId = User32.RegisterWindowMessage("TaskbarCreated");

            MessageWindowHandle = User32.CreateWindowEx(0, WindowId, "", 0, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (MessageWindowHandle == IntPtr.Zero)
                throw new Win32Exception("Message window handle was not a valid pointer.");
        }

        /// <summary>
        /// Callback method that receives messages from the taskbar area.
        /// </summary>
        private IntPtr OnWindowMessageReceived(IntPtr hwnd, uint messageId, IntPtr wparam, IntPtr lparam)
        {
            if (messageId == _taskbarRestartMessageId)
            {
                //Recreate the icon if the taskbar was restarted (for example due to Windows Explorer shutdown).
                var listener = TaskbarCreated;
                listener?.Invoke();
            }

            //Forward the message.
            ProcessWindowMessage(messageId, wparam, lparam);

            //Pass the message to the default window procedure.
            return User32.DefWindowProc(hwnd, messageId, wparam, lparam);
        }

        /// <summary>
        /// Processes incoming system messages.
        /// </summary>
        /// <param name="msg">Callback ID.</param>
        /// <param name="wParam">This parameter can be used to resolve mouse coordinates.</param>
        /// <param name="lParam">Provides information about the event.</param>
        private void ProcessWindowMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg != CallbackMessageId)
                return;

            switch (lParam.ToInt32())
            {
                case 0x200:
                    MouseEventReceived(MouseEventType.MouseMove);
                    break;

                case 0x201:
                    MouseEventReceived(MouseEventType.IconLeftMouseDown);
                    break;

                case 0x202: //Left click.
                    _clickCount++;

                    if (_clickCount == 1)
                        _doubleClick.Start();

                    break;

                case 0x203:
                    lock (_lock)
                    {
                        _clickCount = -1; //Puts down to -1 to avoid a third call by the mouse up.
                        _doubleClick.Stop();

                        MouseEventReceived(MouseEventType.IconLeftDoubleClick);
                    }
                    break;

                case 0x204:
                    MouseEventReceived(MouseEventType.IconRightMouseDown);
                    break;

                case 0x205:
                    MouseEventReceived(MouseEventType.IconRightMouseUp);
                    break;

                case 0x206:
                    //Double click with right mouse button, ignored.
                    break;

                case 0x207:
                    MouseEventReceived(MouseEventType.IconMiddleMouseDown);
                    break;

                case 520:
                    MouseEventReceived(MouseEventType.IconMiddleMouseUp);
                    break;

                case 0x209:
                    //Double click with middle mouse button, ignored.
                    break;

                case 0x405:
                    //BaloonTooltip clicked, ignored.
                    break;

                case 0x406:
                    var listener = ChangeToolTipStateRequest;
                    listener?.Invoke(true);
                    break;

                case 0x407:
                    listener = ChangeToolTipStateRequest;
                    listener?.Invoke(false);
                    break;
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            User32.DestroyWindow(MessageWindowHandle);

            _messageHandler = null;

            _doubleClick.Tick -= DoubleClick_Tick;
            _doubleClick.Stop();
            _doubleClick.Dispose();
        }
    }
}