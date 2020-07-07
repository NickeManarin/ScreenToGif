using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ScreenToGif.Model;

namespace ScreenToGif.Util.Capture
{
    internal class CachedCapture : ImageCapture
    {
        #region Variables

        private FileStream _fileStream;
        private BufferedStream _bufferedStream;
        private DeflateStream _compressStream;

        private Native.BitmapInfoHeader _infoHeader;
        private long _byteLength;

        #endregion

        public override void Start(int delay, int left, int top, int width, int height, double scale, ProjectInfo project)
        {
            base.Start(delay, left, top, width, height, scale, project);

            _infoHeader = new Native.BitmapInfoHeader();
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
                new System.Security.Permissions.UIPermission(System.Security.Permissions.UIPermissionWindow.AllWindows).Demand();

                //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
                var success = Native.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);

                if (!success)
                    return FrameCount;

                //Set frame details.
                FrameCount++;
                frame.Path = $"{Project.FullPath}{FrameCount}.png";
                frame.Delay = FrameRate.GetMilliseconds();
                frame.DataLength = _byteLength;
                frame.Data = new byte[_byteLength];

                if (Native.GetDIBits(WindowDeviceContext, CompatibleBitmap, 0, (uint)StartHeight, frame.Data, ref _infoHeader, Native.DibColorMode.DibRgbColors) == 0)
                    frame.FrameSkipped = true;

                BlockingCollection.Add(frame);
            }
            catch (Exception e)
            {
                //LogWriter.Log(ex, "Impossible to get screenshot of the screen");
            }

            return FrameCount;
        }

        public override int CaptureWithCursor(FrameInfo frame)
        {
            try
            {
                new System.Security.Permissions.UIPermission(System.Security.Permissions.UIPermissionWindow.AllWindows).Demand();

                //var success = Native.BitBlt(CompatibleDeviceContext, 0, 0, Width, Height, WindowDeviceContext, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);
                var success = Native.StretchBlt(CompatibleDeviceContext, 0, 0, StartWidth, StartHeight, WindowDeviceContext, Left, Top, Width, Height, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);

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

                                    //If the cursor rate needs to be precisely captured.
                                    //https://source.winehq.org/source/dlls/user32/cursoricon.c#2325
                                    //int rate = 0, num = 0;
                                    //var ok1 = Native.GetCursorFrameInfo(cursorInfo.hCursor, IntPtr.Zero, 17, ref rate, ref num);

                                    //CursorStep
                                    var ok = Native.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);

                                    if (!ok)
                                    {
                                        CursorStep = 0;
                                        Native.DrawIconEx(CompatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, CursorStep, IntPtr.Zero, 0x0003);
                                    }
                                    else
                                        CursorStep++;

                                    //Set to fix all alpha bits back to 255.
                                    //frame.RemoveAnyTransparency = iconInfo.hbmMask != IntPtr.Zero;
                                }

                                Native.DeleteObject(iconInfo.hbmColor);
                                Native.DeleteObject(iconInfo.hbmMask);
                            }

                            Native.DestroyIcon(hicon);
                        }

                        Native.DeleteObject(cursorInfo.hCursor);
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

                if (Native.GetDIBits(WindowDeviceContext, CompatibleBitmap, 0, (uint)StartHeight, frame.Data, ref _infoHeader, Native.DibColorMode.DibRgbColors) == 0)
                    frame.FrameSkipped = true;

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
            //If the frame skipped, just increase the delay to the previous frame.
            if (info.FrameSkipped)
            {
                info.Data = null;
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
            _compressStream.Dispose();

            _bufferedStream.Flush();
            _fileStream.Flush();
            
            _bufferedStream.Dispose();
            _fileStream.Dispose();
        }

        [Obsolete("Only for test")]
        public void Other()
        {
            var hDC = Native.GetWindowDC(IntPtr.Zero);
            var hMemDC = Native.CreateCompatibleDC(hDC);

            var bi = new Native.BitmapInfoHeader();
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
            var pBI = Native.LocalAlloc((uint)Native.LocalMemoryFlags.LPTR, new UIntPtr(bi.biSize));
            // Not sure if this needed - simply trying to keep marshaller happy  
            Marshal.StructureToPtr(bi, pBI, false);
            //This will return IntPtr to actual DIB bits in pBits  
            var hBmp = Native.CreateDIBSection(hDC, ref pBI, 0, out pBits, IntPtr.Zero, 0);
            //Marshall back - now we have BitmapInfoHeader correctly filled in Marshal.PtrToStructure(pBI, bi);

            var biNew = (Native.BitmapInfoHeader)Marshal.PtrToStructure(pBI, typeof(Native.BitmapInfoHeader));
            //Usual stuff  
            var hOldBitmap = Native.SelectObject(hMemDC, hBmp);
            //Grab bitmap  
            var nRet = Native.BitBlt(hMemDC, 0, 0, bi.biWidth, bi.biHeight, hDC, Left, Top, Native.CopyPixelOperation.SourceCopy | Native.CopyPixelOperation.CaptureBlt);

            // Allocate memory for a copy of bitmap bits  
            var RealBits = new byte[cb];
            // And grab bits from DIBSestion data  
            Marshal.Copy(pBits, RealBits, 0, cb);

            //This simply creates valid bitmap file header, so it can be saved to disk  
            var bfh = new Native.BitmapFileHeader();
            bfh.bfSize = (uint)cb + 0x36; // Size of header + size of Native.BitmapInfoHeader size of bitmap bits
            bfh.bfType = 0x4d42; //BM  
            bfh.bfOffBits = 0x36; //  
            var HdrSize = 14;
            var header = new byte[HdrSize];

            BitConverter.GetBytes(bfh.bfType).CopyTo(header, 0);
            BitConverter.GetBytes(bfh.bfSize).CopyTo(header, 2);
            BitConverter.GetBytes(bfh.bfOffBits).CopyTo(header, 10);
            //Allocate enough memory for complete bitmap file  
            var data = new byte[cb + bfh.bfOffBits];
            //BITMAPFILEHEADER  
            header.CopyTo(data, 0);

            //BitmapInfoHeader  
            header = new byte[Marshal.SizeOf(bi)];
            var pHeader = Native.LocalAlloc((uint)Native.LocalMemoryFlags.LPTR, new UIntPtr((uint)Marshal.SizeOf(bi)));
            Marshal.StructureToPtr(biNew, pHeader, false);
            Marshal.Copy(pHeader, header, 0, Marshal.SizeOf(bi));
            Native.LocalFree(pHeader);
            header.CopyTo(data, HdrSize);
            //Bitmap bits  
            RealBits.CopyTo(data, (int)bfh.bfOffBits);

            //Native.SelectObject(_compatibleDeviceContext, _oldBitmap);
            //Native.DeleteObject(_compatibleBitmap);
            //Native.DeleteDC(_compatibleDeviceContext);
            //Native.ReleaseDC(_desktopWindow, _windowDeviceContext);

            Native.SelectObject(hMemDC, hOldBitmap);
            Native.DeleteObject(hBmp);
            Native.DeleteDC(hMemDC);
            Native.ReleaseDC(IntPtr.Zero, hDC);
        }
    }
}