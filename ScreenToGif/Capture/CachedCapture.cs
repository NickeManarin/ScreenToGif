using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Model;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Capture;

internal class CachedCapture : ImageCapture
{
    #region Variables

    private FileStream _fileStream;
    private BufferedStream _bufferedStream;
    private DeflateStream _compressStream;

    private BitmapInfoHeader _infoHeader;
    private long _byteLength;

    #endregion

    public override void Start(int delay, int left, int top, int width, int height, double scale, ProjectInfo project)
    {
        base.Start(delay, left, top, width, height, scale, project);

        _infoHeader = new BitmapInfoHeader();
        _infoHeader.biSize = (uint)Marshal.SizeOf(_infoHeader);
        _infoHeader.biBitCount = 24; //Without alpha channel.
        _infoHeader.biClrUsed = 0;
        _infoHeader.biClrImportant = 0;
        _infoHeader.biCompression = 0;
        _infoHeader.biHeight = -StartHeight; //Negative, so the Y-axis will be positioned correctly.
        _infoHeader.biWidth = StartWidth;
        _infoHeader.biPlanes = 1;

        //This was working with 32 bits: 3L * Width * Height;
        _byteLength = (StartWidth * _infoHeader.biBitCount + 31) / 32 * 4 * StartHeight;

        //Due to a strange behavior with the GetDiBits method while the cursor is IBeam, it's best to use 24 bits, to ignore the alpha values.
        //This capture mode ignores the alpha value.
        project.BitDepth = 24;

        _fileStream = new FileStream(project.CachePath, FileMode.Create, FileAccess.Write, FileShare.None);
        _bufferedStream = new BufferedStream(_fileStream, UserSettings.All.MemoryCacheSize * 1048576); //Each 1 MB has 1_048_576 bytes.
        _compressStream = new DeflateStream(_bufferedStream, UserSettings.All.CaptureCompression, true);
    }

    public override int Capture(FrameInfo frame)
    {
        try
        {
            //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
            var success = Gdi32.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

            if (!success)
                return FrameCount;

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = _byteLength;
            frame.Data = new byte[_byteLength];

            if (Gdi32.GetDIBits(WindowDeviceContext, CompatibleBitmap, 0, (uint)StartHeight, frame.Data, ref _infoHeader, DibColorModes.RgbColors) == 0)
                frame.FrameSkipped = true;

            if (IsAcceptingFrames)
                BlockingCollection.Add(frame);
        }
        catch (Exception)
        {
            //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
        }

        return FrameCount;
    }

    public override int CaptureWithCursor(FrameInfo frame)
    {
        try
        {
            //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
            var success = Gdi32.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

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

                                //If the cursor rate needs to be precisely captured.
                                //https://source.winehq.org/source/dlls/user32/cursoricon.c#2325
                                //int rate = 0, num = 0;
                                //var ok1 = Native.GetCursorFrameInfo(cursorInfo.hCursor, IntPtr.Zero, 17, ref rate, ref num);

                                //CursorStep
                                var ok = User32.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);

                                if (!ok)
                                {
                                    CursorStep = 0;
                                    User32.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);
                                }
                                else
                                    CursorStep++;

                                //Set to fix all alpha bits back to 255.
                                //frame.RemoveAnyTransparency = iconInfo.hbmMask != IntPtr.Zero;
                            }

                            Gdi32.DeleteObject(iconInfo.hbmColor);
                            Gdi32.DeleteObject(iconInfo.hbmMask);
                        }

                        User32.DestroyIcon(hicon);
                    }

                    Gdi32.DeleteObject(cursorInfo.hCursor);
                }
            }
            catch (Exception e)
            {
                //LogWriter.Log(e, "Impossible to get the cursor");
            }

            #endregion

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = _byteLength;
            frame.Data = new byte[_byteLength];

            if (Gdi32.GetDIBits(WindowDeviceContext, CompatibleBitmap, 0, (uint)StartHeight, frame.Data, ref _infoHeader, DibColorModes.RgbColors) == 0)
                frame.FrameSkipped = true;

            if (IsAcceptingFrames)
                BlockingCollection.Add(frame);
        }
        catch (Exception e)
        {
            //LogWriter.Log(ex, "Impossible to get the screenshot of the screen");
        }

        return FrameCount;
    }

    public override void Save(FrameInfo info)
    {
        if (UserSettings.All.PreventBlackFrames && info.Data != null && !info.FrameSkipped && info.Data[0] == 0)
        {
            if (!info.Data.Any(a => a > 0))
                info.FrameSkipped = true;
        }

        //If the frame skipped, just increase the delay to the previous frame.
        if (info.FrameSkipped || info.Data == null)
        {
            info.Data = null;

            //Pass the duration to the previous frame, if any.
            if (Project.Frames.Count > 0)
                Project.Frames[Project.Frames.Count - 1].Delay += info.Delay;

            return;
        }

        _compressStream.WriteBytes(info.Data);
        info.Data = null;

        Project.Frames.Add(info);
    }

    public override async Task Stop()
    {
        if (!WasStarted)
            return;

        //Stop the recording first.
        await base.Stop();

        //Then close the streams.
        //_compressStream.Flush();
        await _compressStream.DisposeAsync();

        await _bufferedStream.FlushAsync();
        await _fileStream.FlushAsync();

        await _bufferedStream.DisposeAsync();
        await _fileStream.DisposeAsync();
    }

    [Obsolete("Only for test")]
    public void Other()
    {
        var hDc = User32.GetWindowDC(IntPtr.Zero);
        var hMemDc = Gdi32.CreateCompatibleDC(hDc);

        var bi = new BitmapInfoHeader();
        bi.biSize = (uint)Marshal.SizeOf(bi);
        bi.biBitCount = 24; //Creating RGB bitmap. The following three members don't matter  
        bi.biClrUsed = 0;
        bi.biClrImportant = 0;
        bi.biCompression = 0;
        bi.biHeight = Height;
        bi.biWidth = Width;
        bi.biPlanes = 1;

        var cb = (int)(bi.biHeight * bi.biWidth * bi.biBitCount / 8); //8 is bits per byte.  
        bi.biSizeImage = (uint)(((((bi.biWidth * bi.biBitCount) + 31) & ~31) / 8) * bi.biHeight);
        //bi.biXPelsPerMeter = XPelsPerMeter;
        //bi.biYPelsPerMeter = YPelsPerMeter;
        bi.biXPelsPerMeter = 96;
        bi.biYPelsPerMeter = 96;

        var pBits = IntPtr.Zero;
        //Allocate memory for bitmap bits  
        var pBI = Kernel32.LocalAlloc((uint)LocalMemoryFlags.LPTR, new UIntPtr(bi.biSize));
        // Not sure if this needed - simply trying to keep marshaller happy  
        Marshal.StructureToPtr(bi, pBI, false);
        //This will return IntPtr to actual DIB bits in pBits  
        var hBmp = Gdi32.CreateDIBSection(hDc, ref pBI, 0, out pBits, IntPtr.Zero, 0);
        //Marshall back - now we have BitmapInfoHeader correctly filled in Marshal.PtrToStructure(pBI, bi);

        var biNew = (BitmapInfoHeader)Marshal.PtrToStructure(pBI, typeof(BitmapInfoHeader));
        //Usual stuff  
        var hOldBitmap = Gdi32.SelectObject(hMemDc, hBmp);
        //Grab bitmap  
        var nRet = Gdi32.BitBlt(hMemDc, 0, 0, bi.biWidth, bi.biHeight, hDc, Left, Top, CopyPixelOperations.SourceCopy | CopyPixelOperations.CaptureBlt);

        // Allocate memory for a copy of bitmap bits  
        var realBits = new byte[cb];
        // And grab bits from DIBSestion data  
        Marshal.Copy(pBits, realBits, 0, cb);

        //This simply creates valid bitmap file header, so it can be saved to disk  
        var bfh = new BitmapFileHeader();
        bfh.bfSize = (uint)cb + 0x36; // Size of header + size of Native.BitmapInfoHeader size of bitmap bits
        bfh.bfType = 0x4d42; //BM  
        bfh.bfOffBits = 0x36; //  
        var hdrSize = 14;
        var header = new byte[hdrSize];

        BitConverter.GetBytes(bfh.bfType).CopyTo(header, 0);
        BitConverter.GetBytes(bfh.bfSize).CopyTo(header, 2);
        BitConverter.GetBytes(bfh.bfOffBits).CopyTo(header, 10);
        //Allocate enough memory for complete bitmap file  
        var data = new byte[cb + bfh.bfOffBits];
        //BITMAPFILEHEADER  
        header.CopyTo(data, 0);

        //BitmapInfoHeader  
        header = new byte[Marshal.SizeOf(bi)];
        var pHeader = Kernel32.LocalAlloc((uint)LocalMemoryFlags.LPTR, new UIntPtr((uint)Marshal.SizeOf(bi)));
        Marshal.StructureToPtr(biNew, pHeader, false);
        Marshal.Copy(pHeader, header, 0, Marshal.SizeOf(bi));
        Kernel32.LocalFree(pHeader);
        header.CopyTo(data, hdrSize);
        //Bitmap bits  
        realBits.CopyTo(data, (int)bfh.bfOffBits);

        //Native.SelectObject(_compatibleDeviceContext, _oldBitmap);
        //Native.DeleteObject(_compatibleBitmap);
        //Native.DeleteDC(_compatibleDeviceContext);
        //Native.ReleaseDC(_desktopWindow, _windowDeviceContext);

        Gdi32.SelectObject(hMemDc, hOldBitmap);
        Gdi32.DeleteObject(hBmp);
        Gdi32.DeleteDC(hMemDc);
        User32.ReleaseDC(IntPtr.Zero, hDc);
    }
}