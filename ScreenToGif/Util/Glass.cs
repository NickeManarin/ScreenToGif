using System;
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

                #region Set the background to transparent from both the WPF and Win32 perspectives

                window.Background = Brushes.Transparent;
                var hwndSource = HwndSource.FromHwnd(hwnd);
                if (hwndSource != null)
                    hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;

                #endregion

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

        public static bool RetractGlassFrame(Window window)
        {
            if (!Native.DwmIsCompositionEnabled())
                return false;

            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;

                if (hwnd == IntPtr.Zero)
                    throw new InvalidOperationException("The Window must be shown before retracting the glass.");

                #region Set the background to transparent from both the WPF and Win32 perspectives

                window.Background = new SolidColorBrush(Color.FromArgb(255, 241, 241, 241));
                var hwndSource = HwndSource.FromHwnd(hwnd);
                if (hwndSource != null)
                    hwndSource.CompositionTarget.BackgroundColor = Color.FromArgb(255, 241, 241, 241);

                #endregion

                Native.MARGINS margins = new Native.MARGINS(new Thickness(0, 0, 0, 0));
                Native.DwmExtendFrameIntoClientArea(hwnd, ref margins);

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Retracting Glass");
            }

            return false;
        }
    }
}
