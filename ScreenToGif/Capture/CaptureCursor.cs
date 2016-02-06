using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ScreenToGif.Util;
using ScreenToGif.Util.Writers;
using Point = System.Windows.Point;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// Helper class that gets the info of the current cursor image and position.
    /// </summary>
    public static class CaptureCursor
    {
        /// <summary>
        /// Gets the position and Bitmap of the system cursor.
        /// </summary>
        /// <param name="point"><code>ref</code> parameter, only to return a second value.</param>
        /// <returns>The current Icon of the cursor</returns>
        public static Bitmap CaptureImageCursor(ref Point point)
        {
            var cursorInfo = new Native.CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

            if (!Native.GetCursorInfo(out cursorInfo))
                return null;

            if (cursorInfo.flags != Native.CURSOR_SHOWING)
                return null;

            IntPtr hicon = Native.CopyIcon(cursorInfo.hCursor);
            if (hicon == IntPtr.Zero)
                return null;

            Native.ICONINFO iconInfo;
            if (!Native.GetIconInfo(hicon, out iconInfo))
                return null;

            point.X = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot);
            point.Y = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot);

            try
            {
                using (Bitmap maskBitmap = Image.FromHbitmap(iconInfo.hbmMask))
                {
                    //Is this a monochrome cursor?
                    if (maskBitmap.Height == maskBitmap.Width * 2)
                    {
                        var resultBitmap = new Bitmap(maskBitmap.Width, maskBitmap.Width);

                        Graphics desktopGraphics = Graphics.FromHwnd(Native.GetDesktopWindow());
                        IntPtr desktopHdc = desktopGraphics.GetHdc();

                        IntPtr maskHdc = Native.CreateCompatibleDC(desktopHdc);
                        IntPtr oldPtr = Native.SelectObject(maskHdc, maskBitmap.GetHbitmap());

                        using (Graphics resultGraphics = Graphics.FromImage(resultBitmap))
                        {
                            IntPtr resultHdc = resultGraphics.GetHdc();

                            // These two operation will result in a black cursor over a white background.
                            // Later in the code, a call to MakeTransparent() will get rid of the white background.
                            Native.BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 32, CopyPixelOperation.SourceCopy);
                            Native.BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 0, CopyPixelOperation.SourceInvert);

                            resultGraphics.ReleaseHdc(resultHdc);
                        }

                        IntPtr newPtr = Native.SelectObject(maskHdc, oldPtr);

                        Native.DeleteObject(newPtr);
                        Native.DeleteDC(maskHdc);
                        desktopGraphics.ReleaseHdc(desktopHdc);

                        // Remove the white background from the BitBlt calls,
                        // resulting in a black cursor over a transparent background.
                        resultBitmap.MakeTransparent(Color.White);
                        return resultBitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get the icon.");
            }

            Icon icon = Icon.FromHandle(hicon);
            return icon.ToBitmap();
        }
    }
}
