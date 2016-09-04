using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using ScreenToGif.FileWriters;

namespace ScreenToGif.Util
{
    public class Glass
    {
        public static bool UsesColor
        {
            get
            {
                try
                {
                    //Start menu: HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize
                    var colorPrevalence = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\DWM", "ColorPrevalence", "0").ToString();
                    //var autoColorization = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "AutoColorization", "0").ToString();

                    return colorPrevalence.Equals("1");
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static Color GlassColor
        {
            get
            {
                try
                {
                    var autoColorization = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "AutoColorization", "0").ToString();

                    if (autoColorization.Equals("0"))
                    {
                        return SystemParameters.WindowGlassColor;
                    }

                    var colorString = ((int)Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "ImageColor", "0xFFFFFFFF")).ToString("X").Replace("0x", "");

                    //bgr?
                    var a = int.Parse(colorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    var r = int.Parse(colorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    var g = int.Parse(colorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    var b = int.Parse(colorString.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    
                    //36 B2 CC
                    //54 178 204
                    //36 9F B3 Check
                    //54 159 179
                    return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
                }
                catch (Exception)
                {
                    return Color.FromArgb(255, 255, 255, 255);
                }
            }
        }

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

                Native.Margins margins = new Native.Margins(margin);
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

                Native.Margins margins = new Native.Margins(new Thickness(0, 0, 0, 0));
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
