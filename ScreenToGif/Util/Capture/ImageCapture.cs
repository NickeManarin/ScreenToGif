using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ScreenToGif.Model;
using Image = System.Drawing.Image;

namespace ScreenToGif.Util.Capture
{
    internal class ImageCapture : BaseCapture
    {
        #region Variables

        protected internal readonly IntPtr DesktopWindow = IntPtr.Zero;
        protected internal IntPtr WindowDeviceContext;
        protected internal IntPtr CompatibleDeviceContext;
        protected internal IntPtr CompatibleBitmap;
        protected internal IntPtr OldBitmap;

        protected internal int CursorStep { get; set; }

        protected internal Native.CopyPixelOperation PixelOperation { get; set; }

        #endregion

        public override void Start(int delay, int left, int top, int width, int height, double scale, ProjectInfo project)
        {
            base.Start(delay, left, top, width, height, scale, project);

            #region Pointers

            //http://winprog.org/tutorial/bitmaps.html
            //_desktopWindow = Native.GetDesktopWindow();
            WindowDeviceContext = Native.GetWindowDC(DesktopWindow);
            CompatibleDeviceContext = Native.CreateCompatibleDC(WindowDeviceContext);
            CompatibleBitmap = Native.CreateCompatibleBitmap(WindowDeviceContext, Width, Height);
            OldBitmap = Native.SelectObject(CompatibleDeviceContext, CompatibleBitmap);

            #endregion

            var pixelOp = Native.CopyPixelOperation.SourceCopy;

            //If not in a remote desktop connection or if the improvement was disabled, capture layered windows too.
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession || !UserSettings.All.RemoteImprovement)
                pixelOp |= Native.CopyPixelOperation.CaptureBlt;

            PixelOperation = pixelOp;
        }


        public override int Capture(FrameInfo frame)
        {
            try
            {
                new System.Security.Permissions.UIPermission(System.Security.Permissions.UIPermissionWindow.AllWindows).Demand();

                //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
                var success = Native.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, PixelOperation);

                if (!success)
                    return FrameCount;

                //Set frame details.
                FrameCount++;
                frame.Path = $"{Project.FullPath}{FrameCount}.png";
                frame.Delay = FrameRate.GetMilliseconds();
                frame.Image = Image.FromHbitmap(CompatibleBitmap);

                BlockingCollection.Add(frame);
            }
            catch (Exception)
            {
                //LogWriter.Log(ex, "Impossible to get the screenshot of the screen");
            }

            return FrameCount;
        }

        public override async Task<int> CaptureAsync(FrameInfo frame)
        {
            return await Task.Factory.StartNew(() => Capture(frame));
        }

        public override int CaptureWithCursor(FrameInfo frame)
        {
            try
            {
                new System.Security.Permissions.UIPermission(System.Security.Permissions.UIPermissionWindow.AllWindows).Demand();

                //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
                var success = Native.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, PixelOperation);

                if (!success)
                    return FrameCount;

                #region Cursor

                try
                {
                    var cursorInfo = new Native.CursorInfo();
                    cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                    if (Native.GetCursorInfo(out cursorInfo))
                    {
                        if (cursorInfo.flags == Native.CursorShowing)
                        {
                            var hicon = Native.CopyIcon(cursorInfo.hCursor);

                            if (hicon != IntPtr.Zero)
                            {
                                if (Native.GetIconInfo(hicon, out var iconInfo))
                                {
                                    frame.CursorX = cursorInfo.ptScreenPos.X - Left;
                                    frame.CursorY = cursorInfo.ptScreenPos.Y - Top;

                                    //(int)(SystemParameters.CursorHeight * Scale)
                                    //(int)(SystemParameters.CursorHeight * Scale)

                                    var ok = Native.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);

                                    if (!ok)
                                    {
                                        CursorStep = 0;
                                        Native.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);
                                    }
                                    else
                                        CursorStep++;
                                }

                                Native.DeleteObject(iconInfo.hbmColor);
                                Native.DeleteObject(iconInfo.hbmMask);
                            }

                            Native.DestroyIcon(hicon);
                        }

                        Native.DeleteObject(cursorInfo.hCursor);
                    }
                }
                catch (Exception)
                {
                    //LogWriter.Log(e, "Impossible to get the cursor");
                }

                #endregion

                //Set frame details.
                FrameCount++;
                frame.Path = $"{Project.FullPath}{FrameCount}.png";
                frame.Delay = FrameRate.GetMilliseconds();
                frame.Image = Image.FromHbitmap(CompatibleBitmap);

                BlockingCollection.Add(frame);
            }
            catch (Exception)
            {
                //LogWriter.Log(ex, "Impossible to get the screenshot of the screen");
            }

            return FrameCount;
        }

        public override async Task<int> CaptureWithCursorAsync(FrameInfo frame)
        {
            return await Task.Factory.StartNew(() => CaptureWithCursor(frame));
        }


        public override void Save(FrameInfo frame)
        {
            frame.Image.Save(frame.Path);
            frame.Image.Dispose();
            frame.Image = null;

            Project.Frames.Add(frame);
        }

        public override async Task Stop()
        {
            if (!WasStarted)
                return;

            try
            {
                Native.SelectObject(CompatibleDeviceContext, OldBitmap);
                Native.DeleteObject(CompatibleBitmap);
                Native.DeleteDC(CompatibleDeviceContext);
                Native.ReleaseDC(DesktopWindow, WindowDeviceContext);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to stop and clean resources used by the recording.");
            }

            await base.Stop();
        }
    }
}