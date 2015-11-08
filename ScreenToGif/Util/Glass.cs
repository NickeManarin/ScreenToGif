using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenToGif.Util.Writers;

namespace ScreenToGif.Util
{
    class Glass
    {
        public static bool ExtendGlassFrame(Window window, Thickness margin)
        {
            if (!Native.DwmIsCompositionEnabled())
                return false;

            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;

                if (hwnd == IntPtr.Zero)
                    throw new InvalidOperationException("The Window must be shown before extending glass.");

                // Set the background to transparent from both the WPF and Win32 perspectives
                window.Background = Brushes.Transparent;
                HwndSource.FromHwnd(hwnd).CompositionTarget.BackgroundColor = Colors.Transparent;

                Native.MARGINS margins = new Native.MARGINS(margin);
                Native.DwmExtendFrameIntoClientArea(hwnd, ref margins);

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Glass");
            }

            return false;
        }
    }
}
