using System;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Domain.Exceptions;
using ScreenToGif.Model;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ScreenToGif.Capture;

/// <summary>
/// Frame capture using the DesktopDuplication API.
/// Adapted from:
/// https://github.com/ajorkowski/VirtualSpace
/// https://github.com/Microsoft/Windows-classic-samples/blob/master/Samples/DXGIDesktopDuplication
///
/// How to debug:
/// https://walbourn.github.io/dxgi-debug-device/
/// https://walbourn.github.io/direct3d-sdk-debug-layer-tricks/
/// https://devblogs.microsoft.com/cppblog/visual-studio-2015-and-graphics-tools-for-windows-10/
/// </summary>
internal class DirectImageCapture : BaseCapture
{
    #region Variables

    /// <summary>
    /// The current device being duplicated.
    /// </summary>
    protected internal Device Device;

    /// <summary>
    /// The desktop duplication interface.
    /// </summary>
    protected internal OutputDuplication DuplicatedOutput;

    /// <summary>
    /// The rotation of the screen.
    /// </summary>
    protected internal DisplayModeRotation DisplayRotation;

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
    /// The texture used exclusively to be a backing texture when capturing screens which are rotated.
    /// </summary>
    protected internal Texture2D TransformTexture;

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

    /// <summary>
    /// Flag that holds the information whether the previous capture had a major crash.
    /// </summary>
    protected internal bool MajorCrashHappened = false;

    #endregion

    public override void Start(int delay, int left, int top, int width, int height, double dpi, ProjectInfo project)
    {
        base.Start(delay, left, top, width, height, dpi, project);

        //Only set as Started after actually finishing starting.
        WasStarted = false;

        Initialize();

        WasStarted = true;
    }

    public override void ResetConfiguration()
    {
        DisposeInternal();
        Initialize();
    }

    internal void Initialize()
    {
        MajorCrashHappened = false;

#if DEBUG
        Device = new Device(DriverType.Hardware, DeviceCreationFlags.Debug);

        var debug = SharpDX.DXGI.InfoQueue.TryCreate();
        debug?.SetBreakOnSeverity(DebugId.All, InformationQueueMessageSeverity.Corruption, true);
        debug?.SetBreakOnSeverity(DebugId.All, InformationQueueMessageSeverity.Error, true);
        debug?.SetBreakOnSeverity(DebugId.All, InformationQueueMessageSeverity.Warning, true);

        var debug2 = DXGIDebug.TryCreate();
        debug2?.ReportLiveObjects(DebugId.Dx, DebugRloFlags.Summary | DebugRloFlags.Detail);

#else
            Device = new Device(DriverType.Hardware, DeviceCreationFlags.VideoSupport);
#endif

        using (var multiThread = Device.QueryInterface<Multithread>())
            multiThread.SetMultithreadProtected(true);

        //Texture used to copy contents from the GPU to be accessible by the CPU.
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

        //Texture that is used to receive the pixel data from the GPU.
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
                    throw new GraphicsConfigurationException("The Desktop Duplication API is not supported on this computer.", e);
                }
                catch (SharpDXException e) when (e.Descriptor == SharpDX.DXGI.ResultCode.InvalidCall)
                {
                    throw new GraphicsConfigurationException("The Desktop Duplication API is not supported on this screen.", e);
                }
                catch (SharpDXException e) when (e.Descriptor.NativeApiCode == "E_INVALIDARG")
                {
                    throw new GraphicsConfigurationException("Looks like that the Desktop Duplication API is not supported on this screen.", e);
                }
            }
        }
    }

    /// <summary>
    /// Get the correct Output1 based on region to be captured.
    /// </summary>
    private Output1 GetOutput(Factory1 factory)
    {
        try
        {
            //Gets the output with the bigger area being intersected.
            var output = factory.Adapters1.SelectMany(s => s.Outputs).FirstOrDefault(f => f.Description.DeviceName == DeviceName) ??
                         factory.Adapters1.SelectMany(s => s.Outputs).OrderByDescending(f =>
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
            DisplayRotation = output.Description.Rotation;

            if (DisplayRotation != DisplayModeRotation.Identity)
            {
                //Texture that is used to receive the pixel data from the GPU.
                TransformTexture = new Texture2D(Device, new Texture2DDescription
                {
                    ArraySize = 1,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = Height,
                    Height = Width,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default
                });
            }

            //Create textures in here, after detecting the orientation?

            return output.QueryInterface<Output1>();
        }
        catch (SharpDXException ex)
        {
            throw new Exception("Could not find the specified output device.", ex);
        }
    }


    public override int Capture(FrameInfo frame)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);

            if (FrameCount == 0 && (res.Failure || resource == null))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                resource?.Dispose();
                return FrameCount;
            }

            #region Process changes

            //Something on screen was moved or changed.
            if (info.TotalMetadataBufferSize > 0)
            {
                //Copy resource into memory that can be accessed by the CPU.
                using (var screenTexture = resource.QueryInterface<Texture2D>())
                {
                    #region Moved rectangles

                    var movedRectangles = new OutputDuplicateMoveRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out var movedRegionsLength);

                    for (var movedIndex = 0; movedIndex < movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle)); movedIndex++)
                    {
                        //Crop the destination rectangle to the screen area rectangle.
                        var left = Math.Max(movedRectangles[movedIndex].DestinationRect.Left, Left - OffsetLeft);
                        var right = Math.Min(movedRectangles[movedIndex].DestinationRect.Right, Left + Width - OffsetLeft);
                        var top = Math.Max(movedRectangles[movedIndex].DestinationRect.Top, Top - OffsetTop);
                        var bottom = Math.Min(movedRectangles[movedIndex].DestinationRect.Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                        {
                            //Limit the source rectangle to the available size within the destination rectangle.
                            var sourceWidth = movedRectangles[movedIndex].SourcePoint.X + (right - left);
                            var sourceHeight = movedRectangles[movedIndex].SourcePoint.Y + (bottom - top);

                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0,
                                new ResourceRegion(movedRectangles[movedIndex].SourcePoint.X, movedRectangles[movedIndex].SourcePoint.Y, 0, sourceWidth, sourceHeight, 1),
                                StagingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                        }
                    }

                    #endregion

                    #region Dirty rectangles

                    var dirtyRectangles = new RawRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out var dirtyRegionsLength);

                    for (var dirtyIndex = 0; dirtyIndex < dirtyRegionsLength / Marshal.SizeOf(typeof(RawRectangle)); dirtyIndex++)
                    {
                        //Crop screen positions and size to frame sizes.
                        var left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        var right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        var top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        var bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(left, top, 0, right, bottom, 1), StagingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                    }

                    #endregion
                }
            }

            #endregion

            #region Gets the image data

            //Gets the staging texture as a stream.
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                resource?.Dispose();
                return FrameCount;
            }

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
            frame.Delay = FrameRate.GetMilliseconds();
            frame.Image = bitmap;

            if (IsAcceptingFrames)
                BlockingCollection.Add(frame);

            #endregion

            Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);

            resource?.Dispose();
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
        {
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code || se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
        {
            //When the device gets lost or reset, the resources should be instantiated again.
            DisposeInternal();
            Initialize();

            return FrameCount;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

            MajorCrashHappened = true;

            if (IsAcceptingFrames)
                Application.Current.Dispatcher.Invoke(() => OnError.Invoke(ex));

            return FrameCount;
        }
        finally
        {
            try
            {
                //Only release the frame if there was a success in capturing it.
                if (res.Success)
                    DuplicatedOutput.ReleaseFrame();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to release the frame.");
            }
        }
    }

    public override async Task<int> CaptureAsync(FrameInfo frame)
    {
        return await Task.Factory.StartNew(() => Capture(frame));
    }

    public override int CaptureWithCursor(FrameInfo frame)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);

            //Checks how to proceed with the capture. It could have failed, or the screen, cursor or both could have been captured.
            if (FrameCount == 0 && info.LastMouseUpdateTime == 0 && (res.Failure || resource == null))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                resource?.Dispose();
                return FrameCount;
            }
            else if (FrameCount == 0 && info.TotalMetadataBufferSize == 0 && info.LastMouseUpdateTime > 0)
            {
                //Sometimes, the first frame has cursor info, but no screen changes.
                GetCursor(null, info, frame);
                resource?.Dispose();
                return FrameCount;
            }

            #region Process changes

            //Something on screen was moved or changed.
            if (info.TotalMetadataBufferSize > 0)
            {
                //Copies the screen data into memory that can be accessed by the CPU.
                using (var screenTexture = resource.QueryInterface<Texture2D>())
                {
                    #region Moved rectangles

                    var movedRectangles = new OutputDuplicateMoveRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameMoveRects(movedRectangles.Length, movedRectangles, out var movedRegionsLength);

                    for (var movedIndex = 0; movedIndex < movedRegionsLength / Marshal.SizeOf(typeof(OutputDuplicateMoveRectangle)); movedIndex++)
                    {
                        //Crop the destination rectangle to the screen area rectangle.
                        var left = Math.Max(movedRectangles[movedIndex].DestinationRect.Left, Left - OffsetLeft);
                        var right = Math.Min(movedRectangles[movedIndex].DestinationRect.Right, Left + Width - OffsetLeft);
                        var top = Math.Max(movedRectangles[movedIndex].DestinationRect.Top, Top - OffsetTop);
                        var bottom = Math.Min(movedRectangles[movedIndex].DestinationRect.Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                        {
                            //Limit the source rectangle to the available size within the destination rectangle.
                            var sourceWidth = movedRectangles[movedIndex].SourcePoint.X + (right - left);
                            var sourceHeight = movedRectangles[movedIndex].SourcePoint.Y + (bottom - top);

                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0,
                                new ResourceRegion(movedRectangles[movedIndex].SourcePoint.X, movedRectangles[movedIndex].SourcePoint.Y, 0, sourceWidth, sourceHeight, 1),
                                BackingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                        }
                    }

                    #endregion

                    #region Dirty rectangles

                    var dirtyRectangles = new RawRectangle[info.TotalMetadataBufferSize];
                    DuplicatedOutput.GetFrameDirtyRects(dirtyRectangles.Length, dirtyRectangles, out var dirtyRegionsLength);

                    for (var dirtyIndex = 0; dirtyIndex < dirtyRegionsLength / Marshal.SizeOf(typeof(RawRectangle)); dirtyIndex++)
                    {
                        //Crop screen positions and size to frame sizes.
                        var left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        var right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        var top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        var bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);

                        //Copies from the screen texture only the area which the user wants to capture.
                        if (right > left && bottom > top)
                            Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(left, top, 0, right, bottom, 1), BackingTexture, 0, left - (Left - OffsetLeft), top - (Top - OffsetTop));
                    }

                    #endregion
                }
            }

            if (info.TotalMetadataBufferSize > 0 || info.LastMouseUpdateTime > 0)
            {
                //Copy the captured desktop texture into a staging texture, in order to show the mouse cursor and not make the captured texture dirty with it.
                Device.ImmediateContext.CopyResource(BackingTexture, StagingTexture);

                //Gets the cursor image and merges with the staging texture.
                GetCursor(StagingTexture, info, frame);
            }

            //Saves the most recent capture time.
            LastProcessTime = Math.Max(info.LastPresentTime, info.LastMouseUpdateTime);

            #endregion

            #region Gets the image data

            //Get the desktop capture texture.
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                resource?.Dispose();
                return FrameCount;
            }

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

            //Releases the source and dest locks.
            bitmap.UnlockBits(mapDest);

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.Image = bitmap;

            if (IsAcceptingFrames)
                BlockingCollection.Add(frame);

            #endregion

            Device.ImmediateContext?.UnmapSubresource(StagingTexture, 0);

            resource?.Dispose();
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
        {
            return FrameCount;
        }
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code || se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
        {
            //When the device gets lost or reset, the resources should be instantiated again.
            DisposeInternal();
            Initialize();

            return FrameCount;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

            MajorCrashHappened = true;

            if (IsAcceptingFrames)
                Application.Current.Dispatcher.Invoke(() => OnError.Invoke(ex));

            return FrameCount;
        }
        finally
        {
            try
            {
                //Only release the frame if there was a success in capturing it.
                if (res.Success)
                    DuplicatedOutput.ReleaseFrame();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to release the frame.");
            }
        }
    }

    public override async Task<int> CaptureWithCursorAsync(FrameInfo frame)
    {
        return await Task.Factory.StartNew(() => CaptureWithCursor(frame));
    }

    public override int ManualCapture(FrameInfo frame, bool showCursor = false)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(1000, out var info, out var resource);

            //Checks how to proceed with the capture. It could have failed, or the screen, cursor or both could have been captured.
            if (res.Failure || resource == null || (!showCursor && info.AccumulatedFrames == 0) || (showCursor && info.AccumulatedFrames == 0 && info.LastMouseUpdateTime <= LastProcessTime))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                //frame.WasDropped = true;
                //BlockingCollection.Add(frame);

                resource?.Dispose();
                return FrameCount;
            }
            else if (showCursor && info.AccumulatedFrames == 0 && info.LastMouseUpdateTime > LastProcessTime)
            {
                //Gets the cursor shape if the screen hasn't changed in between, so the cursor will be available for the next frame.
                GetCursor(null, info, frame);

                resource.Dispose();
                return FrameCount;
            }

            //Saves the most recent capture time.
            LastProcessTime = Math.Max(info.LastPresentTime, info.LastMouseUpdateTime);

            //Copy resource into memory that can be accessed by the CPU.
            using (var screenTexture = resource.QueryInterface<Texture2D>())
            {
                if (showCursor)
                {
                    //Copies from the screen texture only the area which the user wants to capture.
                    Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(TrueLeft, TrueTop, 0, TrueRight, TrueBottom, 1), BackingTexture, 0);

                    //Copy the captured desktop texture into a staging texture, in order to show the mouse cursor and not make the captured texture dirty with it.
                    Device.ImmediateContext.CopyResource(BackingTexture, StagingTexture);

                    //Gets the cursor image and merges with the staging texture.
                    GetCursor(StagingTexture, info, frame);
                }
                else
                {
                    //Copies from the screen texture only the area which the user wants to capture.
                    Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, new ResourceRegion(TrueLeft, TrueTop, 0, TrueRight, TrueBottom, 1), StagingTexture, 0);
                }
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
            frame.Delay = FrameRate.GetMilliseconds();
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
        catch (SharpDXException se) when (se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code || se.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
        {
            //When the device gets lost or reset, the resources should be instantiated again.
            DisposeInternal();
            Initialize();

            return FrameCount;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "It was not possible to finish capturing the frame with DirectX.");

            MajorCrashHappened = true;
            OnError.Invoke(ex);
            return FrameCount;
        }
        finally
        {
            try
            {
                //Only release the frame if there was a success in capturing it.
                if (res.Success)
                    DuplicatedOutput.ReleaseFrame();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to release the frame.");
            }
        }
    }

    public override async Task<int> ManualCaptureAsync(FrameInfo frame, bool showCursor = false)
    {
        return await Task.Factory.StartNew(() => ManualCapture(frame, showCursor));
    }


    protected internal bool GetCursor(Texture2D screenTexture, OutputDuplicateFrameInformation info, FrameInfo frame)
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
        //Saves the position of the cursor, so the editor can add the mouse events overlay later.
        frame.CursorX = PreviousPosition.Position.X - (Left - OffsetLeft);
        frame.CursorY = PreviousPosition.Position.Y - (Top - OffsetTop);

        //If the method is supposed to simply the get the cursor shape no shape was loaded before, there's nothing else to do.
        //if (CursorShapeBuffer?.Length == 0 || (info.LastPresentTime == 0 && info.LastMouseUpdateTime == 0) || !info.PointerPosition.Visible)
        if (screenTexture == null || CursorShapeBuffer?.Length == 0)// || !info.PointerPosition.Visible)
        {
            //FallbackCursorCapture(frame);

            //if (CursorShapeBuffer != null)
            return false;
        }

        //Don't let it bleed beyond the top-left corner, calculate the dimensions of the portion of the cursor that will appear.
        var leftCut = frame.CursorX;
        var topCut = frame.CursorY;
        var rightCut = screenTexture.Description.Width - (frame.CursorX + CursorShapeInfo.Width);
        var bottomCut = screenTexture.Description.Height - (frame.CursorY + CursorShapeInfo.Height);

        //Adjust to the hotspot offset, so it's possible to add the highlight correctly later.
        frame.CursorX += CursorShapeInfo.HotSpot.X;
        frame.CursorY += CursorShapeInfo.HotSpot.Y;

        //Don't try merging the textures if the cursor is out of bounds.
        if (leftCut + CursorShapeInfo.Width < 1 || topCut + CursorShapeInfo.Height < 1 || rightCut + CursorShapeInfo.Width < 1 || bottomCut + CursorShapeInfo.Height < 1)
            return false;

        var cursorLeft = Math.Max(leftCut, 0);
        var cursorTop = Math.Max(topCut, 0);
        var cursorWidth = leftCut < 0 ? CursorShapeInfo.Width + leftCut : rightCut < 0 ? CursorShapeInfo.Width + rightCut : CursorShapeInfo.Width;
        var cursorHeight = topCut < 0 ? CursorShapeInfo.Height + topCut : bottomCut < 0 ? CursorShapeInfo.Height + bottomCut : CursorShapeInfo.Height;

        //The staging texture must be able to hold all pixels.
        if (CursorStagingTexture == null || CursorStagingTexture.Description.Width != cursorWidth || CursorStagingTexture.Description.Height != cursorHeight)
        {
            //In order to change the size of the texture, I need to instantiate it again with the new size.
            CursorStagingTexture?.Dispose();
            CursorStagingTexture = new Texture2D(Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Write,
                Height = cursorHeight,
                Format = Format.B8G8R8A8_UNorm,
                Width = cursorWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging
            });
        }

        //The region where the cursor is located is copied to the staging texture to act as the background when dealing with masks and transparency.
        //The cutout must be the exact region needed and it can't overflow. It's not allowed to try to cut outside of the screenTexture region.
        var region = new ResourceRegion
        {
            Left = cursorLeft,
            Top = cursorTop,
            Front = 0,
            Right = cursorLeft + cursorWidth,
            Bottom = cursorTop + cursorHeight,
            Back = 1
        };

        //Copy from the screen the region in which the cursor is located.
        Device.ImmediateContext.CopySubresourceRegion(screenTexture, 0, region, CursorStagingTexture, 0);

        //Get cursor details and draw it to the staging texture.
        DrawCursorShape(CursorStagingTexture, CursorShapeInfo, CursorShapeBuffer, leftCut < 0 ? leftCut * -1 : 0, topCut < 0 ? topCut * -1 : 0, cursorWidth, cursorHeight);

        //Copy back the cursor texture to the screen texture.
        Device.ImmediateContext.CopySubresourceRegion(CursorStagingTexture, 0, null, screenTexture, 0, cursorLeft, cursorTop);

        return true;
    }

    private void DrawCursorShape(Texture2D texture, OutputDuplicatePointerShapeInformation info, byte[] buffer, int leftCut, int topCut, int cursorWidth, int cursorHeight)
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
                    DrawMonochromeCursor(leftCut, topCut, cursorWidth, cursorHeight, rect, info.Pitch, buffer, info.Height);
                    break;

                //Color, a colored cursor which supports transparency.
                case (int)OutputDuplicatePointerShapeType.Color:
                    DrawColorCursor(leftCut, topCut, cursorWidth, cursorHeight, rect, info.Pitch, buffer);
                    break;

                //Masked color, a mix of both previous types.
                case (int)OutputDuplicatePointerShapeType.MaskedColor:
                    DrawMaskedColorCursor(leftCut, topCut, cursorWidth, cursorHeight, rect, info.Pitch, buffer);
                    break;
            }

            surface.Unmap();
        }
    }

    private void DrawMonochromeCursor(int offsetX, int offsetY, int width, int height, DataRectangle rect, int pitch, byte[] buffer, int actualHeight)
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
                var xor = (buffer[row * pitch + col / 8 + actualHeight * pitch] & mask) == mask; //Mask is taken from the second half of the cursor image, hence the "+ height * pitch".

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

                //Applies the XOR operation with the current color.
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

    public override async Task Stop()
    {
        if (!WasStarted)
            return;

        DisposeInternal();

        await base.Stop();
    }

    internal void DisposeInternal()
    {
        Device.Dispose();

        if (MajorCrashHappened)
            return;

        BackingTexture.Dispose();
        StagingTexture.Dispose();
        DuplicatedOutput.Dispose();

        CursorStagingTexture?.Dispose();
    }


    [Obsolete]
    private void FallbackCursorCapture(FrameInfo frame)
    {
        //if (_justStarted && (CursorShapeBuffer?.Length ?? 0) == 0)
        {
            //_justStarted = false;

            //https://stackoverflow.com/a/6374151/1735672
            //Bitmap struct, is used to get the cursor shape when SharpDX fails to do so.
            var infoHeader = new BitmapInfoHeader();
            infoHeader.biSize = (uint)Marshal.SizeOf(infoHeader);
            infoHeader.biBitCount = 32;
            infoHeader.biClrUsed = 0;
            infoHeader.biClrImportant = 0;
            infoHeader.biCompression = 0;
            infoHeader.biHeight = -Height; //Negative, so the Y-axis will be positioned correctly.
            infoHeader.biWidth = Width;
            infoHeader.biPlanes = 1;

            try
            {
                var cursorInfo = new CursorInfo();
                cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);

                if (!User32.GetCursorInfo(out cursorInfo))
                    return;

                if (cursorInfo.flags == Native.Constants.CursorShowing)
                {
                    var hicon = User32.CopyIcon(cursorInfo.hCursor);

                    if (hicon != IntPtr.Zero)
                    {
                        if (User32.GetIconInfo(hicon, out var iconInfo))
                        {
                            frame.CursorX = cursorInfo.ptScreenPos.X - Left;
                            frame.CursorY = cursorInfo.ptScreenPos.Y - Top;

                            var bitmap = new Bitmap();
                            var hndl = GCHandle.Alloc(bitmap, GCHandleType.Pinned);
                            var ptrToBitmap = hndl.AddrOfPinnedObject();
                            Gdi32.GetObject(iconInfo.hbmColor, Marshal.SizeOf<Bitmap>(), ptrToBitmap);
                            bitmap = Marshal.PtrToStructure<Bitmap>(ptrToBitmap);
                            hndl.Free();

                            //https://microsoft.public.vc.mfc.narkive.com/H1CZeqUk/how-can-i-get-bitmapinfo-object-from-bitmap-or-hbitmap
                            infoHeader.biHeight = bitmap.bmHeight;
                            infoHeader.biWidth = bitmap.bmWidth;
                            infoHeader.biBitCount = (ushort)bitmap.bmBitsPixel;

                            var w = (bitmap.bmWidth * bitmap.bmBitsPixel + 31) / 8;
                            CursorShapeBuffer = new byte[w * bitmap.bmHeight];

                            var windowDeviceContext = User32.GetWindowDC(IntPtr.Zero);
                            var compatibleBitmap = Gdi32.CreateCompatibleBitmap(windowDeviceContext, Width, Height);

                            Gdi32.GetDIBits(windowDeviceContext, compatibleBitmap, 0, (uint)infoHeader.biHeight, CursorShapeBuffer, ref infoHeader, DibColorModes.RgbColors);

                            //CursorShapeInfo = new OutputDuplicatePointerShapeInformation();
                            //CursorShapeInfo.Type = (int)OutputDuplicatePointerShapeType.Color;
                            //CursorShapeInfo.Width = bitmap.bmWidth;
                            //CursorShapeInfo.Height = bitmap.bmHeight;
                            //CursorShapeInfo.Pitch = w;
                            //CursorShapeInfo.HotSpot = new RawPoint(0, 0);

                            //if (frame.CursorX > 0 && frame.CursorY > 0)
                            //    Native.DrawIconEx(_compatibleDeviceContext, frame.CursorX - iconInfo.xHotspot, frame.CursorY - iconInfo.yHotspot, cursorInfo.hCursor, 0, 0, 0, IntPtr.Zero, 0x0003);

                            //Native.SelectObject(CompatibleDeviceContext, OldBitmap);
                            //Native.DeleteObject(compatibleBitmap);
                            //Native.DeleteDC(CompatibleDeviceContext);
                            //Native.ReleaseDC(IntPtr.Zero, windowDeviceContext);
                        }

                        Gdi32.DeleteObject(iconInfo.hbmColor);
                        Gdi32.DeleteObject(iconInfo.hbmMask);
                    }

                    User32.DestroyIcon(hicon);
                }

                Gdi32.DeleteObject(cursorInfo.hCursor);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to get the cursor");
            }
        }
    }
}