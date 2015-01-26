using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ScreenToGif.Properties;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// Helper class that gets the info of the current cursor image and position.
    /// </summary>
    public class CaptureScreen
    {
        /// <summary>
        /// Gets the position and Icon of the system cursor.
        /// </summary>
        /// <param name="point"><code>ref</code> parameter, only to return a second value.</param>
        /// <param name="isIBeam">True if it is a IBeam cursor.</param>
        /// <returns>The current Icon of the cursor</returns>
        public Icon CaptureIconCursor(ref Point point, ref bool isIBeam)
        {
            IntPtr hicon;
            var ci = new Win32Stuff.CURSORINFO();
            Win32Stuff.ICONINFO icInfo;
            ci.cbSize = Marshal.SizeOf(ci);

            if (Win32Stuff.GetCursorInfo(out ci))
            {
                if (ci.flags == Win32Stuff.CURSOR_SHOWING)
                {
                    hicon = Win32Stuff.CopyIcon(ci.hCursor);

                    if (Win32Stuff.GetIconInfo(hicon, out icInfo))
                    {
                        point.X = ci.ptScreenPos.x - icInfo.xHotspot;
                        point.Y = ci.ptScreenPos.y - icInfo.yHotspot;

                        //http://stackoverflow.com/questions/918990/c-sharp-capturing-the-mouse-cursor-image?rq=1
                        //If the IBeam, the color image is Zero.
                        isIBeam = icInfo.hbmColor == IntPtr.Zero;

                        if (!isIBeam)
                        {
                            return Icon.FromHandle(hicon);
                        }

                        return Resources.IBeam;
                    }
                }
            }
            return null;
        }
    }
}
