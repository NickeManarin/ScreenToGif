using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Model;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using Image = System.Drawing.Image;

namespace ScreenToGif.Capture;

internal class ImageCapture : BaseCapture
{
    #region Variables

    private readonly IntPtr _desktopWindow = IntPtr.Zero;
    protected IntPtr WindowDeviceContext;
    protected IntPtr CompatibleDeviceContext;
    protected IntPtr CompatibleBitmap;
    private IntPtr _oldBitmap;

    protected int CursorStep { get; set; }

    private CopyPixelOperations PixelOperations { get; set; }

    #endregion

    public override void Start(int delay, int left, int top, int width, int height, double scale, ProjectInfo project)
    {
        base.Start(delay, left, top, width, height, scale, project);

        #region Pointers

        //http://winprog.org/tutorial/bitmaps.html
        //_desktopWindow = User32.GetDesktopWindow();
        WindowDeviceContext = User32.GetWindowDC(_desktopWindow);
        CompatibleDeviceContext = Gdi32.CreateCompatibleDC(WindowDeviceContext);
        CompatibleBitmap = Gdi32.CreateCompatibleBitmap(WindowDeviceContext, Width, Height);
        _oldBitmap = Gdi32.SelectObject(CompatibleDeviceContext, CompatibleBitmap);

        #endregion

        var pixelOp = CopyPixelOperations.SourceCopy;

        //If not in a remote desktop connection or if the improvement was disabled, capture layered windows too.
        if (!System.Windows.Forms.SystemInformation.TerminalServerSession || !UserSettings.All.RemoteImprovement)
            pixelOp |= CopyPixelOperations.CaptureBlt;

        PixelOperations = pixelOp;
    }


    public override int Capture(FrameInfo frame)
    {
        try
        {
            //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
            var success = Gdi32.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, PixelOperations);

            if (!success)
                return FrameCount;

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.Image = Image.FromHbitmap(CompatibleBitmap);

            if (IsAcceptingFrames)
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
            //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
            var success = Gdi32.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, PixelOperations);

            if (!success)
                return FrameCount;

            #region Cursor

            try
            {
                var cursorInfo = new CursorInfo();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (User32.GetCursorInfo(out cursorInfo))
                {
                    if (cursorInfo.flags == Native.Constants.CursorShowing)
                    {
                        var hicon = User32.CopyIcon(cursorInfo.hCursor);

                        if (hicon != IntPtr.Zero)
                        {
                            if (User32.GetIconInfo(hicon, out var iconInfo))
                            {
                                frame.CursorX = cursorInfo.ptScreenPos.X - Left;
                                frame.CursorY = cursorInfo.ptScreenPos.Y - Top;

                                //(int)(SystemParameters.CursorHeight * Scale)
                                //(int)(SystemParameters.CursorHeight * Scale)

                                var ok = User32.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);

                                if (!ok)
                                {
                                    CursorStep = 0;
                                    User32.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);
                                }
                                else
                                    CursorStep++;
                            }

                            Gdi32.DeleteObject(iconInfo.hbmColor);
                            Gdi32.DeleteObject(iconInfo.hbmMask);
                        }

                        User32.DestroyIcon(hicon);
                    }

                    Gdi32.DeleteObject(cursorInfo.hCursor);
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

            if (IsAcceptingFrames)
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

        await base.Stop();

        try
        {
            Gdi32.SelectObject(CompatibleDeviceContext, _oldBitmap);
            Gdi32.DeleteObject(CompatibleBitmap);
            Gdi32.DeleteDC(CompatibleDeviceContext);
            User32.ReleaseDC(_desktopWindow, WindowDeviceContext);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to stop and clean resources used by the recording.");
        }
    }
}