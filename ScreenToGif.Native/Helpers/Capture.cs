using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;

namespace ScreenToGif.Native.Helpers
{
    public static class Capture
    {
        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="width">The size of the final image.</param>
        /// <param name="height">The size of the final image.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <returns>A bitmap with the capture rectangle.</returns>
        public static BitmapSource CaptureScreenAsBitmapSource(int width, int height, int positionX, int positionY)
        {
            var hDesk = User32.GetDesktopWindow();
            var hSrce = User32.GetWindowDC(hDesk);
            var hDest = Gdi32.CreateCompatibleDC(hSrce);
            var hBmp = Gdi32.CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            try
            {
                var b = Gdi32.BitBlt(hDest, 0, 0, width, height, hSrce, positionX, positionY, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

                //return Image.FromHbitmap(hBmp);
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                Gdi32.SelectObject(hDest, hOldBmp);
                Gdi32.DeleteObject(hBmp);
                Gdi32.DeleteDC(hDest);
                User32.ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        /// <summary>
        /// Captures the screen using the SourceCopy | CaptureBlt.
        /// </summary>
        /// <param name="height">Height of the capture region.</param>
        /// <param name="positionX">Source capture Left position.</param>
        /// <param name="positionY">Source capture Top position.</param>
        /// <param name="width">Width of the capture region.</param>
        /// <returns>A bitmap with the capture rectangle.</returns>
        public static Image CaptureScreenAsBitmap(int width, int height, int positionX, int positionY)
        {
            var hDesk = User32.GetDesktopWindow();
            var hSrce = User32.GetWindowDC(hDesk);
            var hDest = Gdi32.CreateCompatibleDC(hSrce);
            var hBmp = Gdi32.CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            try
            {
                var b = Gdi32.BitBlt(hDest, 0, 0, width, height, hSrce, positionX, positionY, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

                return b ? Image.FromHbitmap(hBmp) : null;
            }
            catch (Exception)
            {
                //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                Gdi32.SelectObject(hDest, hOldBmp);
                Gdi32.DeleteObject(hBmp);
                Gdi32.DeleteDC(hDest);
                User32.ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static Image CaptureWindow(IntPtr handle, double scale)
        {
            var rectangle = Windows.GetWindowRect(handle);
            var posX = (int)((rectangle.X + Util.Constants.LeftOffset) * scale);
            var posY = (int)((rectangle.Y + Util.Constants.TopOffset) * scale);
            var width = (int)((rectangle.Width - Util.Constants.HorizontalOffset) * scale);
            var height = (int)((rectangle.Height - Util.Constants.VerticalOffset) * scale);

            var hDesk = User32.GetDesktopWindow();
            var hSrce = User32.GetWindowDC(hDesk);
            var hDest = Gdi32.CreateCompatibleDC(hSrce);
            var hBmp = Gdi32.CreateCompatibleBitmap(hSrce, width, height);
            var hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            var b = Gdi32.BitBlt(hDest, 0, 0, width, height, hSrce, posX, posY, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

            try
            {
                return Image.FromHbitmap(hBmp);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }
            finally
            {
                Gdi32.SelectObject(hDest, hOldBmp);
                Gdi32.DeleteObject(hBmp);
                Gdi32.DeleteDC(hDest);
                User32.ReleaseDC(hDesk, hSrce);
            }

            return null;
        }

        public static System.Drawing.Bitmap CaptureImageCursor(ref System.Windows.Point point, double scale)
        {
            try
            {
                var cursorInfo = new CursorInfo();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (!User32.GetCursorInfo(out cursorInfo))
                    return null;

                if (cursorInfo.flags != Constants.CursorShowing)
                    return null;

                var hicon = User32.CopyIcon(cursorInfo.hCursor);
                if (hicon == IntPtr.Zero)
                    return null;

                if (!User32.GetIconInfo(hicon, out var iconInfo))
                {
                    Gdi32.DeleteObject(hicon);
                    return null;
                }

                point.X = cursorInfo.ptScreenPos.X - iconInfo.xHotspot;
                point.Y = cursorInfo.ptScreenPos.Y - iconInfo.yHotspot;

                using (var maskBitmap = Image.FromHbitmap(iconInfo.hbmMask))
                {
                    //Is this a monochrome cursor?  
                    if (maskBitmap.Height == maskBitmap.Width * 2 && iconInfo.hbmColor == IntPtr.Zero)
                    {
                        var final = new System.Drawing.Bitmap(maskBitmap.Width, maskBitmap.Width);
                        var hDesktop = User32.GetDesktopWindow();
                        var dcDesktop = User32.GetWindowDC(hDesktop);

                        using (var resultGraphics = Graphics.FromImage(final))
                        {
                            var resultHdc = resultGraphics.GetHdc();
                            var offsetX = (int)((point.X + 3) * scale);
                            var offsetY = (int)((point.Y + 3) * scale);

                            Gdi32.BitBlt(resultHdc, 0, 0, final.Width, final.Height, dcDesktop, offsetX, offsetY, CopyPixelOperations.SourceCopy);
                            User32.DrawIconEx(resultHdc, 0, 0, cursorInfo.hCursor, 0, 0, 0, IntPtr.Zero, 0x0003);

                            //TODO: I have to try removing the background of this cursor capture.
                            //Gdi32.BitBlt(resultHdc, 0, 0, final.Width, final.Height, dcDesktop, (int)point.X + 3, (int)point.Y + 3, CopyPixelOperations.SourceErase);

                            //Original, ignores the screen as background.
                            //Gdi32.BitBlt(resultHdc, 0, 0, resultBitmap.Width, resultBitmap.Height, maskHdc, 0, resultBitmap.Height, CopyPixelOperations.SourceCopy); //SourceCopy
                            //Gdi32.BitBlt(resultHdc, 0, 0, resultBitmap.Width, resultBitmap.Height, maskHdc, 0, 0, CopyPixelOperations.PatInvert); //SourceInvert

                            resultGraphics.ReleaseHdc(resultHdc);
                            User32.ReleaseDC(hDesktop, dcDesktop);
                        }

                        Gdi32.DeleteObject(iconInfo.hbmMask);
                        Gdi32.DeleteDC(dcDesktop);

                        return final;
                    }

                    Gdi32.DeleteObject(iconInfo.hbmColor);
                    Gdi32.DeleteObject(iconInfo.hbmMask);
                    Gdi32.DeleteObject(hicon);
                }

                var icon = Icon.FromHandle(hicon);
                return icon.ToBitmap();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to get the cursor.");
            }

            return null;
        }
    }
}