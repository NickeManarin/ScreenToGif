using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ScreenToGif.FileWriters;
using ScreenToGif.Util;
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
            try
            {
                var cursorInfo = new Native.CursorInfo();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (!Native.GetCursorInfo(out cursorInfo))
                    return null;

                if (cursorInfo.flags != Native.CursorShowing)
                    return null;

                var hicon = Native.CopyIcon(cursorInfo.hCursor);
                if (hicon == IntPtr.Zero)
                    return null;

                Native.Iconinfo iconInfo;
                if (!Native.GetIconInfo(hicon, out iconInfo))
                    return null;

                point.X = cursorInfo.ptScreenPos.X - iconInfo.xHotspot;
                point.Y = cursorInfo.ptScreenPos.Y - iconInfo.yHotspot;

                using (var maskBitmap = Image.FromHbitmap(iconInfo.hbmMask))
                {
                    //Is this a monochrome cursor?
                    //if (maskBitmap.Height == maskBitmap.Width * 2)
                    if (maskBitmap.Height == maskBitmap.Width * 2 && iconInfo.hbmColor == IntPtr.Zero)
                    {
                        var resultBitmap = new Bitmap(maskBitmap.Width, maskBitmap.Width);

                        using (var desktopGraphics = Graphics.FromHwnd(Native.GetDesktopWindow()))
                        {
                            var desktopHdc = desktopGraphics.GetHdc();

                            var maskHdc = Native.CreateCompatibleDC(desktopHdc);
                            var oldPtr = Native.SelectObject(maskHdc, maskBitmap.GetHbitmap());

                            using (var resultGraphics = Graphics.FromImage(resultBitmap))
                            {
                                var resultHdc = resultGraphics.GetHdc();

                                // These two operation will result in a black cursor over a white background.
                                // Later in the code, a call to MakeTransparent() will get rid of the white background.
                                // They take two pieces from a single image and merge into one.

                                //Bottom part
                                Native.BitBlt(resultHdc, 0, 0, resultBitmap.Width, resultBitmap.Height, maskHdc, 0, resultBitmap.Height, Native.CopyPixelOperation.SourceCopy); //SourceCopy
                                //Top part.
                                Native.BitBlt(resultHdc, 0, 0, resultBitmap.Width, resultBitmap.Height, maskHdc, 0, 0, Native.CopyPixelOperation.PatInvert); //SourceInvert

                                //BUG: It still don't take into account the background color (from the desktop) with the I-bean cursor, the one that inverts its color.
                                resultGraphics.ReleaseHdc(resultHdc);
                            }

                            var newPtr = Native.SelectObject(maskHdc, oldPtr);

                            Native.DeleteObject(newPtr);
                            Native.DeleteDC(maskHdc);
                            desktopGraphics.ReleaseHdc(desktopHdc);
                        }

                        // Remove the white background from the BitBlt calls,
                        // resulting in a black cursor over a transparent background.
                        resultBitmap.MakeTransparent(Color.White);
                        return resultBitmap;
                    }
                }

                var icon = Icon.FromHandle(hicon);
                return icon.ToBitmap();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get the icon.");
            }

            return null;
        }

        //private static int count = 0; 
        //private static int first = 0;
        //private static int sec = 0;
        //private static CopyPixelOperation TestThings1()
        //{
        //    /*0 0, 0 1, 0 2...0 9, 1 0 */

        //    Console.WriteLine(count);
        //    count++;

        //    switch (first)
        //    {
        //        case 0:
                    
        //            return CopyPixelOperation.NoMirrorBitmap;
        //        case 1:
                    
        //            return CopyPixelOperation.Blackness;
        //        case 2:
                    
        //            return CopyPixelOperation.NotSourceErase;
        //        case 3:
                    
        //            return CopyPixelOperation.NotSourceCopy;
        //        case 4:
                    
        //            return CopyPixelOperation.SourceErase;
        //        case 5:
                    
        //            return CopyPixelOperation.DestinationInvert;
        //        case 6:
                    
        //            return CopyPixelOperation.PatInvert;
        //        case 7:
                    
        //            return CopyPixelOperation.SourceInvert;
        //        case 8:
                    
        //            return CopyPixelOperation.SourceAnd;
        //        case 9:
                    
        //            return CopyPixelOperation.MergePaint;
        //        case 10:
                    
        //            return CopyPixelOperation.MergeCopy;
        //        case 11:
                    
        //            return CopyPixelOperation.SourceCopy;
        //        case 12:
                    
        //            return CopyPixelOperation.SourcePaint;
        //        case 13:
                    
        //            return CopyPixelOperation.PatCopy;
        //        case 14:
                    
        //            return CopyPixelOperation.PatPaint;
        //        case 15:
                    
        //            return CopyPixelOperation.Whiteness;
        //        case 16:
        //            first = 0;
        //            return CopyPixelOperation.CaptureBlt;
        //    }

        //    return CopyPixelOperation.SourceCopy;
        //}

        //private static CopyPixelOperation TestThings2()
        //{
        //    switch (sec)
        //    {
        //        case 0:
        //            sec++;
        //            return CopyPixelOperation.NoMirrorBitmap;
        //        case 1:
        //            sec++;
        //            return CopyPixelOperation.Blackness;
        //        case 2:
        //            sec++;
        //            return CopyPixelOperation.NotSourceErase;
        //        case 3:
        //            sec++;
        //            return CopyPixelOperation.NotSourceCopy;
        //        case 4:
        //            sec++;
        //            return CopyPixelOperation.SourceErase;
        //        case 5:
        //            sec++;
        //            return CopyPixelOperation.DestinationInvert;
        //        case 6:
        //            sec++;
        //            return CopyPixelOperation.PatInvert;
        //        case 7:
        //            sec++;
        //            return CopyPixelOperation.SourceInvert;
        //        case 8:
        //            sec++;
        //            return CopyPixelOperation.SourceAnd;
        //        case 9:
        //            sec++;
        //            return CopyPixelOperation.MergePaint;
        //        case 10:
        //            sec++;
        //            return CopyPixelOperation.MergeCopy;
        //        case 11:
        //            sec++;
        //            return CopyPixelOperation.SourceCopy;
        //        case 12:
        //            sec++;
        //            return CopyPixelOperation.SourcePaint;
        //        case 13:
        //            sec++;
        //            return CopyPixelOperation.PatCopy;
        //        case 14:
        //            sec++;
        //            return CopyPixelOperation.PatPaint;
        //        case 15:
        //            sec++;
        //            return CopyPixelOperation.Whiteness;
        //        case 16:
        //            sec = 0;
        //            first++;
        //            return CopyPixelOperation.CaptureBlt;
        //    }

        //    return CopyPixelOperation.SourceInvert;
        //}
    }
}
