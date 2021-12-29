using System.Windows;
using System.Windows.Interop;

namespace ScreenToGif.Util;

public class ScreenHelper
{
    public static System.Windows.Forms.Screen GetScreen(Window window)
    {
        return System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
    }
}