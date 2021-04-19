using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace ScreenToGif.SystemCapture
{
    public static class CaptureHelper
    {
        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        public static void SetWindow(this GraphicsCapturePicker picker, IntPtr hwnd)
        {
            var interop = (IInitializeWithWindow)(object)picker;
            interop.Initialize(hwnd);
        }
    }
}