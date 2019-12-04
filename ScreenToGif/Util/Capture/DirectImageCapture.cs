using System;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ScreenToGif.Model;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ScreenToGif.Util.Capture
{
    /// <summary>
    /// Frame capture using the DesktopDuplication API.
    /// Adapted from:
    /// https://github.com/ajorkowski/VirtualSpace
    /// https://github.com/Microsoft/Windows-classic-samples/blob/master/Samples/DXGIDesktopDuplication
    /// </summary>
    internal class DirectImageCapture : BaseCapture
    {
        #region Variables

        /// <summary>
        /// The current device being duplicated.
        /// </summary>
        protected internal Device Device;

        /// <summary>
        /// The dektop duplication interface.
        /// </summary>
        protected internal OutputDuplication DuplicatedOutput;

        /// <summary>
        /// The texture used to copy the pixel data from the desktop to the destination image. 
        /// </summary>
        protected internal Texture2D StagingTexture;
        
        /// <summary>
        /// The texture used exclusively to be a backing texture when capturing the cursor shape.
        /// This texture will always hold only the desktop texture, without the cursor.
        /// </summary>
        protected internal Texture2D BackingTexture;

        /// <summary>
        /// Texture used to merge the cursor with the background image (desktop).
        /// </summary>
        protected internal Texture2D CursorStagingTexture;

        /// <summary>
        /// The buffer that holds all pixel data of the cursor.
        /// </summary>
        protected internal byte[] CursorShapeBuffer;

        /// <summary>
        /// The details of the cursor.
        /// </summary>
        protected internal OutputDuplicatePointerShapeInformation CursorShapeInfo;
        
        /// <summary>
        /// The previous position of the mouse cursor.
        /// </summary>
        protected internal OutputDuplicatePointerPosition PreviousPosition;

        /// <summary>
        /// The latest time in which a frame or metadata was captured.
        /// </summary>
        protected internal long LastProcessTime = 0;

        protected internal int OffsetLeft { get; set; }
        protected internal int OffsetTop { get; set; }
        protected internal int TrueLeft => Left + OffsetLeft;
        protected internal int TrueRight => Left + OffsetLeft + Width;
        protected internal int TrueTop => Top + OffsetTop;
        protected internal int TrueBottom => Top + OffsetTop + Height;

        #endregion

        public override void Start(int delay, int left, int top, int width, int height, double dpi, ProjectInfo project)
        {
            base.Start(delay, left, top, width, height, dpi, project);

            //Only set as Started after actually finishing starting.
            WasStarted = false;

#if DEBUG
            Device = new Device(DriverType.Hardware, DeviceCreationFlags.VideoSupport | DeviceCreationFlags.Debug);
#else
            Device = new Device(DriverType.Hardware, DeviceCreationFlags.VideoSupport);
#endif

            using (var multiThread = Device.QueryInterface<Multithread>())
                multiThread.SetMultithreadProtected(true);

            //Texture used to copy contents from the GPU to be accesible by the CPU.
            StagingTexture = new Texture2D(Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = Format.B8G8R8A8_UNorm,
                Width = Width,
                Height = Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging
            });

            //Texture that is used to recieve the pixel data from the GPU.
            BackingTexture = new Texture2D(Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = Width,
                Height = Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            });

            using (var factory = new Factory1())
            {
                //Get the Output1 based on the current capture region position.
                using (var output1 = GetOutput(factory))
                {
                    try
                    {
                        //Make sure to run with the integrated graphics adapter if using a Microsoft hybrid system. https://stackoverflow.com/a/54196789/1735672
                        DuplicatedOutput = output1.DuplicateOutput(Device);
                    }
                    catch (SharpDXException e) when (e.Descriptor == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable)
                    {
                        throw new Exception("Too many applications using the Desktop Duplication API. Please close one of the applications and try again.", e);
                    }
                    catch (SharpDXException e) when (e.Descriptor == SharpDX.DXGI.ResultCode.Unsupported)
                    {
                        throw new NotSupportedException("The Desktop Duplication API is not supported on this computer. If you have multiple graphic cards, try running ScreenToGif on integrated graphics.", e);
                    }
                }
            }

            WasStarted = true;
        }

        /// <summary>
        /// Get the correct Output1 based on region to be captured.
        /// TODO: Get the correct output when the user moves the capture region to other screen.
        /// TODO: Capture multiple screens at the same time.
        /// </summary>
        private Output1 GetOutput(Factory1 factory)
        {
            try
            {
                //Gets the output with the bigger area being intersected.
                var output = factory.Adapters1.SelectMany(s => s.Outputs).OrderByDescending(f =>
                {
                    var x = Math.Max(Left, f.Description.DesktopBounds.Left);
                    var num1 = Math.Min(Left + Width, f.Description.DesktopBounds.Right);
                    var y = Math.Max(Top, f.Description.DesktopBounds.Top);
                    var num2 = Math.Min(Top + Height, f.Description.DesktopBounds.Bottom);

                    if (num1 >= x && num2 >= y)
                        return num1 - x + num2 - y;

                    return 0;
                }).FirstOrDefault();

                if (output == null)
                    throw new Exception($"Could not find a proper output device for the area of L: {Left}, T: {Top}, Width: {Width}, Height: {Height}.");

                //Position adjustments, so the correct region is captured.
                OffsetLeft = output.Description.DesktopBounds.Left;
                OffsetTop = output.Description.DesktopBounds.Top;

                return output.QueryInterface<Output1>();
            }
            catch (SharpDXException ex)
            {
                throw new Exception("Could not find the specified output device.", ex);
            }
        }

        public override int Capture(FrameInfo frame)
        {
            var res = Result.Ok;

            try
            {
                //Try to get the duplicated output frame within given time.
                res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);

                //Somehow, it was not possible to retrieve the resource or any frame.
                if (res.Failure || resource == null || info.AccumulatedFrames == 0)
                {
                    resource?.Dispose();
                    return FrameCount;
                }

                //Copy resource into memory that can be accessed by the CPU.
                using (var screenTexture = resource.QueryInterface<Texture2D>())
                {
                    //Copies from the screen texture only the area which the user wants to capture.
                    Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(TrueLeft, TrueTop, 0, TrueRight, TrueBottom, 1), StagingTexture, 0);
                }

                //Get the desktop capture texture.
                var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None);

                if (data.IsEmpty)
                {
                    //frame.WasDropped = true;
                    //BlockingCollection.Add(frame);

                    Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                    resource.Dispose();
                    return FrameCount;
                }

                #region Get image data

                var bitmap = new System.Drawing.Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                var boundsRect = new System.Drawing.Rectangle(0, 0, Width, Height);

                //Copy pixels from screen capture Texture to the GDI bitmap.
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var sourcePtr = data.DataPointer;
                var destPtr = mapDest.Scan0;

                for (var y = 0; y < Height; y++)
                {
                    //Copy a single line.
                    Utilities.CopyMemory(destPtr, sourcePtr, Width * 4);

                    //Advance pointers.
                    sourcePtr = IntPtr.Add(sourcePtr, data.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                //Release source and dest locks.
                bitmap.UnlockBits(mapDest);

                //Set frame details.
                FrameCount++;
                frame.Path = $"{Project.FullPath}{FrameCount}.png";
                frame.Delay = FrameRate.GetMilliseconds(SnapDelay);
                frame.Image = bitmap;
                BlockingCollection.Add(frame);

                #endregion

                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);

                resource.Dispose();
                return FrameCount;
            }
            catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
            {
                return FrameCount;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

                OnError.Invoke(ex);
                return FrameCount;
            }
            finally
            {
                try
                {
                    //Only release the frame if there was a sucess in capturing it.
                    if (res.Success)
                        DuplicatedOutput.ReleaseFrame();
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "It was not possible to release the frame.");
                }
            }
        }

        public override int CaptureWithCursor(FrameInfo frame)
        {
            var res = Result.Ok;

            try
            {
                //Try to get the duplicated output frame within given time.
                res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);
                
                //Checks how to proceed with the capture. It could have failed, or the screen, cursor or both could have been captured.
                if (res.Failure || resource == null || (info.AccumulatedFrames == 0 && info.LastMouseUpdateTime <= LastProcessTime))
                {
                    //Somehow, it was not possible to retrieve the resource, frame or metadata.
                    //frame.WasDropped = true;
                    //BlockingCollection.Add(frame);

                    resource?.Dispose();
                    return FrameCount;
                }
                else if (info.AccumulatedFrames == 0 && info.LastMouseUpdateTime > LastProcessTime)
                {
                    //Gets the cursor shape if the screen hasn't changed in between, so the cursor will be available for the next frame.
                    GetCursor(null, info, frame);
                    return FrameCount;
                }

                //Saves the most recent capture time.
                LastProcessTime = Math.Max(info.LastPresentTime, info.LastMouseUpdateTime);

                //Copy resource into memory that can be accessed by the CPU.
                using (var screenTexture = resource.QueryInterface<Texture2D>())
                {
                    //Copies from the screen texture only the area which the user wants to capture.
                    Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(TrueLeft, TrueTop, 0, TrueRight, TrueBottom, 1), BackingTexture, 0);

                    //Copy the captured desktop texture into a staging texture, in order to show the mouse cursor and not make the captured texture dirty with it.
                    Device.ImmediateContext.CopyResource(BackingTexture, StagingTexture);

                    //Gets the cursor image and merges with the staging texture.
                    GetCursor(StagingTexture, info, frame);
                }

                //Get the desktop capture texture.
                var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None);

                if (data.IsEmpty)
                {
                    //frame.WasDropped = true;
                    //BlockingCollection.Add(frame);

                    Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                    resource.Dispose();
                    return FrameCount;
                }

                #region Get image data

                var bitmap = new System.Drawing.Bitmap(Width, Height, PixelFormat.Format32bppArgb);
                var boundsRect = new System.Drawing.Rectangle(0, 0, Width, Height);

                //Copy pixels from screen capture Texture to the GDI bitmap.
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var sourcePtr = data.DataPointer;
                var destPtr = mapDest.Scan0;

                for (var y = 0; y < Height; y++)
                {
                    //Copy a single line.
                    Utilities.CopyMemory(destPtr, sourcePtr, Width * 4);

                    //Advance pointers.
                    sourcePtr = IntPtr.Add(sourcePtr, data.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                //Release source and dest locks.
                bitmap.UnlockBits(mapDest);

                //Set frame details.
                FrameCount++;
                frame.Path = $"{Project.FullPath}{FrameCount}.png";
                frame.Delay = FrameRate.GetMilliseconds(SnapDelay);
                frame.Image = bitmap;
                BlockingCollection.Add(frame);

                #endregion

                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);

                resource.Dispose();
                return FrameCount;
            }
            catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
            {
                return FrameCount;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

                OnError.Invoke(ex);
                return FrameCount;
            }
            finally
            {
                try
                {
                    //Only release the frame if there was a sucess in capturing it.
                    if (res.Success)
                        DuplicatedOutput.ReleaseFrame();
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "It was not possible to release the frame.");
                }
            }
        }

        protected internal void GetCursor(Texture2D screenTexture, OutputDuplicateFrameInformation info, FrameInfo frame)
        {
            //Prepare buffer array to hold the cursor shape.
            if (CursorShapeBuffer == null || info.PointerShapeBufferSize > CursorShapeBuffer.Length)
                CursorShapeBuffer = new byte[info.PointerShapeBufferSize];

            //If there's a cursor shape available to be captured.
            if (info.PointerShapeBufferSize > 0)
            {
                //Pin the buffer in order to pass the address as parameter later.
                var pinnedBuffer = GCHandle.Alloc(CursorShapeBuffer, GCHandleType.Pinned);
                var cursorBufferAddress = pinnedBuffer.AddrOfPinnedObject();

                //Load the cursor shape into the buffer.
                DuplicatedOutput.GetFramePointerShape(info.PointerShapeBufferSize, cursorBufferAddress, out _, out CursorShapeInfo);

                //If the cursor is monochrome, it will return the cursor shape twice, one is the mask.
                if (CursorShapeInfo.Type == 1)
                    CursorShapeInfo.Height /= 2;

                //The buffer must be unpinned, to free resources.
                pinnedBuffer.Free();
            }

            //Store the current cursor position, if it was moved.
            if (info.LastMouseUpdateTime != 0)
                PreviousPosition = info.PointerPosition;

            //TODO: In a future version, don't merge the cursor image in here, let the editor do that.
            //Saves the position of the cursor, so the editor can add the mouse clicks overlay later.
            frame.CursorX = PreviousPosition.Position.X - Left;
            frame.CursorY = PreviousPosition.Position.Y - Top;

            //If the method is supposed to simply the get the cursor shape no shape was loaded before, there's nothing else to do.
            //if (CursorShapeBuffer?.Length == 0 || (info.LastPresentTime == 0 && info.LastMouseUpdateTime == 0) || !info.PointerPosition.Visible)
            if (screenTexture == null || CursorShapeBuffer?.Length == 0)// || !info.PointerPosition.Visible)
                return;

            //Don't let it bleed beyond the top-left corner.
            var left = Math.Max(frame.CursorX, 0);
            var top = Math.Max(frame.CursorY, 0);
            var offsetX = Math.Abs(Math.Min(0, frame.CursorX));
            var offsetY = Math.Abs(Math.Min(0, frame.CursorY));

            //Adjust the offset, so it's possible to add the highlight correctly later.
            frame.CursorX += CursorShapeInfo.HotSpot.X;
            frame.CursorY += CursorShapeInfo.HotSpot.Y;

            if (CursorShapeInfo.Width - offsetX < 0 || CursorShapeInfo.Height - offsetY < 0)
                return;

            //The staging texture must be able to hold all pixels.
            if (CursorStagingTexture == null || CursorStagingTexture.Description.Width < CursorShapeInfo.Width - offsetX || CursorStagingTexture.Description.Height < CursorShapeInfo.Height - offsetY)
            {
                //In order to change the size of the texture, I need to instantiate it again with the new size.
                CursorStagingTexture?.Dispose();
                CursorStagingTexture = new Texture2D(Device, new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    Height = CursorShapeInfo.Height - offsetY,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = CursorShapeInfo.Width - offsetX,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging
                });
            }

            //The region where the cursor is located is copied to the staging texture to act as the background when dealing with masks and transparency.
            var region = new ResourceRegion
            {
                Left = left,
                Top = top,
                Front = 0,
                Right = left + CursorStagingTexture.Description.Width,
                Bottom = top + CursorStagingTexture.Description.Height,
                Back = 1
            };

            //Copy from the screen the region in which the cursor is located.
            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, region, CursorStagingTexture, 0);

            //Get cursor details and draw it to the staging texture.
            DrawCursorShape(CursorStagingTexture, CursorShapeInfo, CursorShapeBuffer, offsetX, offsetY);

            //Copy back the cursor texture to the screen texture.
            Device.ImmediateContext.CopySubresourceRegion(CursorStagingTexture, 0, null, screenTexture, 0, left, top);
        }

        private void DrawCursorShape(Texture2D texture, OutputDuplicatePointerShapeInformation info, byte[] buffer, int offsetX, int offsetY)
        {
            using (var surface = texture.QueryInterface<Surface>())
            {
                //Maps the surface, indicating that the CPU needs access to the data.
                var rect = surface.Map(SharpDX.DXGI.MapFlags.Write);

                //Cursors can be divided into 3 types:
                switch (info.Type)
                {
                    //Masked monochrome, a cursor which reacts with the background.
                    case (int)OutputDuplicatePointerShapeType.Monochrome:
                        DrawMonochromeCursor(offsetX, offsetY, info.Width, info.Height, rect, info.Pitch, buffer);
                        break;

                    //Color, a colored cursor which supports transparency.
                    case (int)OutputDuplicatePointerShapeType.Color:
                        DrawColorCursor(offsetX, offsetY, info.Width, info.Height, rect, info.Pitch, buffer);
                        break;

                    //Masked color, a mix of both previous types.
                    case (int)OutputDuplicatePointerShapeType.MaskedColor:
                        DrawMaskedColorCursor(offsetX, offsetY, info.Width, info.Height, rect, info.Pitch, buffer);
                        break;
                }

                surface.Unmap();
            }
        }

        private void DrawMonochromeCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer)
        {
            for (var row = offsetY; row < height; row++)
            {
                //128 in binary.
                byte mask = 0x80;

                //Simulate the offset, adjusting the mask.
                for (var off = 0; off < offsetX; off++)
                {
                    if (mask == 0x01)
                        mask = 0x80;
                    else
                        mask = (byte)(mask >> 1);
                }

                for (var col = offsetX; col < width; col++)
                {
                    var pos = (row - offsetY) * rect.Pitch + (col - offsetX) * 4;
                    var and = (buffer[row * pitch + col / 8] & mask) == mask; //Mask is take from the first half of the cursor image.
                    var xor = (buffer[row * pitch + col / 8 + height * pitch] & mask) == mask; //Mask is taken from the second half of the cursor image, hence the "+ height * pitch". 

                    //Reads current pixel and applies AND and XOR. (AND/XOR ? White : Black)
                    Marshal.WriteByte(rect.DataPointer, pos, (byte)((Marshal.ReadByte(rect.DataPointer, pos) & (and ? 255 : 0)) ^ (xor ? 255 : 0)));
                    Marshal.WriteByte(rect.DataPointer, pos + 1, (byte)((Marshal.ReadByte(rect.DataPointer, pos + 1) & (and ? 255 : 0)) ^ (xor ? 255 : 0)));
                    Marshal.WriteByte(rect.DataPointer, pos + 2, (byte)((Marshal.ReadByte(rect.DataPointer, pos + 2) & (and ? 255 : 0)) ^ (xor ? 255 : 0)));
                    Marshal.WriteByte(rect.DataPointer, pos + 3, (byte)((Marshal.ReadByte(rect.DataPointer, pos + 3) & 255) ^ 0));

                    //Shifts the mask around until it reaches 1, then resets it back to 128.
                    if (mask == 0x01)
                        mask = 0x80;
                    else
                        mask = (byte)(mask >> 1);
                }
            }
        }

        private void DrawColorCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer)
        {
            for (var row = offsetY; row < height; row++)
            {
                for (var col = offsetX; col < width; col++)
                {
                    var surfaceIndex = (row - offsetY) * rect.Pitch + (col - offsetX) * 4;
                    var bufferIndex = row * pitch + col * 4;
                    var alpha = buffer[bufferIndex + 3] + 1;

                    if (alpha == 1)
                        continue;

                    //Premultiplied alpha values.
                    var invAlpha = 256 - alpha;
                    alpha += 1;

                    Marshal.WriteByte(rect.DataPointer, surfaceIndex, (byte)((alpha * buffer[bufferIndex] + invAlpha * Marshal.ReadByte(rect.DataPointer, surfaceIndex)) >> 8));
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex + 1, (byte)((alpha * buffer[bufferIndex + 1] + invAlpha * Marshal.ReadByte(rect.DataPointer, surfaceIndex + 1)) >> 8));
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex + 2, (byte)((alpha * buffer[bufferIndex + 2] + invAlpha * Marshal.ReadByte(rect.DataPointer, surfaceIndex + 2)) >> 8));
                }
            }
        }

        private void DrawMaskedColorCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer)
        {
            //ImageUtil.ImageMethods.SavePixelArrayToFile(buffer, width, height, 4, System.IO.Path.GetFullPath(".\\MaskedColor.png"));

            for (var row = offsetY; row < height; row++)
            {
                for (var col = offsetX; col < width; col++)
                {
                    var surfaceIndex = (row - offsetY) * rect.Pitch + (col - offsetX) * 4;
                    var bufferIndex = row * pitch + col * 4;
                    var maskFlag = buffer[bufferIndex + 3];

                    //Just copies the pixel color.
                    if (maskFlag == 0)
                    {
                        Marshal.WriteByte(rect.DataPointer, surfaceIndex, buffer[bufferIndex]);
                        Marshal.WriteByte(rect.DataPointer, surfaceIndex + 1, buffer[bufferIndex + 1]);
                        Marshal.WriteByte(rect.DataPointer, surfaceIndex + 2, buffer[bufferIndex + 2]);
                        return;
                    }

                    //Applies the XOR opperation with the current color.
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex, (byte)(buffer[bufferIndex] ^ Marshal.ReadByte(rect.DataPointer, surfaceIndex)));
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex + 1, (byte)(buffer[bufferIndex + 1] ^ Marshal.ReadByte(rect.DataPointer, surfaceIndex + 1)));
                    Marshal.WriteByte(rect.DataPointer, surfaceIndex + 2, (byte)(buffer[bufferIndex + 2] ^ Marshal.ReadByte(rect.DataPointer, surfaceIndex + 2)));
                }
            }
        }

        public override void Save(FrameInfo frame)
        {
            frame.Image?.Save(frame.Path);
            frame.Image?.Dispose();
            frame.Image = null;

            Project.Frames.Add(frame);
        }

        public override async Task<int> CaptureAsync(FrameInfo frame)
        {
            return await Task.Factory.StartNew(() => Capture(frame));
        }

        public override async Task<int> CaptureWithCursorAsync(FrameInfo frame)
        {
            return await Task.Factory.StartNew(() => CaptureWithCursor(frame));
        }

        [Obsolete("Try using this method if everything else fails.")]
        private void CursorCapture(FrameInfo frame)
        {
            //if (_justStarted && (CursorShapeBuffer?.Length ?? 0) == 0)
            {
                //_justStarted = false;

                //https://stackoverflow.com/a/6374151/1735672
                //Bitmap struct, is used to get the cursor shape when SharpDX fails to do so. 
                var _infoHeader = new Native.BitmapInfoHeader();
                _infoHeader.biSize = (uint)Marshal.SizeOf(_infoHeader);
                _infoHeader.biBitCount = 32;
                _infoHeader.biClrUsed = 0;
                _infoHeader.biClrImportant = 0;
                _infoHeader.biCompression = 0;
                _infoHeader.biHeight = -Height; //Negative, so the Y-axis will be positioned correctly.
                _infoHeader.biWidth = Width;
                _infoHeader.biPlanes = 1;

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

                                    var bitmap = new Native.Bitmap();
                                    var hndl = GCHandle.Alloc(bitmap, GCHandleType.Pinned);
                                    var ptrToBitmap = hndl.AddrOfPinnedObject();
                                    Native.GetObject(iconInfo.hbmColor, Marshal.SizeOf<Native.Bitmap>(), ptrToBitmap);
                                    bitmap = Marshal.PtrToStructure<Native.Bitmap>(ptrToBitmap);
                                    hndl.Free();

                                    //https://microsoft.public.vc.mfc.narkive.com/H1CZeqUk/how-can-i-get-bitmapinfo-object-from-bitmap-or-hbitmap
                                    _infoHeader.biHeight = bitmap.bmHeight;
                                    _infoHeader.biWidth = bitmap.bmWidth;
                                    _infoHeader.biBitCount = (ushort)bitmap.bmBitsPixel;

                                    var w = (bitmap.bmWidth * bitmap.bmBitsPixel + 31) / 8;
                                    CursorShapeBuffer = new byte[w * bitmap.bmHeight];

                                    var windowDeviceContext = Native.GetWindowDC(IntPtr.Zero);
                                    var compatibleBitmap = Native.CreateCompatibleBitmap(windowDeviceContext, Width, Height);

                                    Native.GetDIBits(windowDeviceContext, compatibleBitmap, 0, (uint)_infoHeader.biHeight, CursorShapeBuffer, ref _infoHeader, Native.DibColorMode.DibRgbColors);

                                    //if (frame.CursorX > 0 && frame.CursorY > 0)
                                    //    Native.DrawIconEx(_compatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, 0, IntPtr.Zero, 0x0003);
                                    
                                    //Clean objects here.
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
                    LogWriter.Log(e, "Impossible to get the cursor");
                }
            }
        }

        public override void Stop()
        {
            if (!WasStarted)
                return;

            Device.Dispose();
            BackingTexture.Dispose();
            StagingTexture.Dispose();
            DuplicatedOutput.Dispose();

            CursorStagingTexture?.Dispose();

            base.Stop();
        }
    }
}