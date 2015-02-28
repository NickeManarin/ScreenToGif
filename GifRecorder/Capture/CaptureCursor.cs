using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ScreenToGif.Properties;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// Helper class that gets the info of the current cursor image and position.
    /// </summary>
    public class CaptureCursor
    {
        /// <summary>
        /// Gets the position and Icon of the system cursor.
        /// </summary>
        /// <param name="point"><code>ref</code> parameter, only to return a second value.</param>
        /// <param name="isIBeam">True if it is a IBeam cursor.</param>
        /// <returns>The current Icon of the cursor</returns>
        public Icon CaptureIconCursor(ref Point point, ref bool isIBeam)
        {
            var ci = new Win32Stuff.CURSORINFO();

            ci.cbSize = Marshal.SizeOf(ci);

            if (Win32Stuff.GetCursorInfo(out ci))
            {
                if (ci.flags == Win32Stuff.CURSOR_SHOWING)
                {
                    var hicon = Win32Stuff.CopyIcon(ci.hCursor);

                    Win32Stuff.ICONINFO icInfo;
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

        /// <summary>
        /// Gets the position and Bitmap of the system cursor.
        /// </summary>
        /// <param name="point"><code>ref</code> parameter, only to return a second value.</param>
        /// <returns>The current Icon of the cursor</returns>
        public Bitmap CaptureImageCursor(ref Point point)
        {
            var cursorInfo = new Win32Stuff.CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

            if (!Win32Stuff.GetCursorInfo(out cursorInfo))
                return null;

            if (cursorInfo.flags != Win32Stuff.CURSOR_SHOWING)
                return null;

            IntPtr hicon = Win32Stuff.CopyIcon(cursorInfo.hCursor);
            if (hicon == IntPtr.Zero)
                return null;

            Win32Stuff.ICONINFO iconInfo;
            if (!Win32Stuff.GetIconInfo(hicon, out iconInfo))
                return null;

            point.X = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot);
            point.Y = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot);

            using (Bitmap maskBitmap = Image.FromHbitmap(iconInfo.hbmMask))
            {
                //Is this a monochrome cursor?
                if (maskBitmap.Height == maskBitmap.Width * 2)
                {
                    var resultBitmap = new Bitmap(maskBitmap.Width, maskBitmap.Width);

                    Graphics desktopGraphics = Graphics.FromHwnd(Win32Stuff.GetDesktopWindow());
                    IntPtr desktopHdc = desktopGraphics.GetHdc();

                    IntPtr maskHdc = Win32Stuff.CreateCompatibleDC(desktopHdc);
                    IntPtr oldPtr = Win32Stuff.SelectObject(maskHdc, maskBitmap.GetHbitmap());

                    using (Graphics resultGraphics = Graphics.FromImage(resultBitmap))
                    {
                        IntPtr resultHdc = resultGraphics.GetHdc();

                        // These two operation will result in a black cursor over a white background.
                        // Later in the code, a call to MakeTransparent() will get rid of the white background.
                        Win32Stuff.BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 32, Win32Stuff.TernaryRasterOperations.SRCCOPY);
                        Win32Stuff.BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 0, Win32Stuff.TernaryRasterOperations.SRCINVERT);

                        resultGraphics.ReleaseHdc(resultHdc);
                    }

                    IntPtr newPtr = Win32Stuff.SelectObject(maskHdc, oldPtr);
                    //Win32Stuff.DeleteDC(newPtr);
                    Win32Stuff.DeleteObject(newPtr);
                    Win32Stuff.DeleteDC(maskHdc);
                    desktopGraphics.ReleaseHdc(desktopHdc);

                    // Remove the white background from the BitBlt calls,
                    // resulting in a black cursor over a transparent background.
                    resultBitmap.MakeTransparent(Color.White);
                    return resultBitmap;
                }
            }

            Icon icon = Icon.FromHandle(hicon);
            return icon.ToBitmap();
        }
    }
}
