using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ScreenToGif.Capture;

/// <summary>
/// Frame capture using the DesktopDuplication API and memory cache.
/// Adapted from:
/// https://github.com/ajorkowski/VirtualSpace
/// https://github.com/Microsoft/Windows-classic-samples/blob/master/Samples/DXGIDesktopDuplication
///
/// How to debug:
/// https://walbourn.github.io/dxgi-debug-device/
/// https://walbourn.github.io/direct3d-sdk-debug-layer-tricks/
/// https://devblogs.microsoft.com/cppblog/visual-studio-2015-and-graphics-tools-for-windows-10/
/// </summary>
internal class DirectCachedCapture : DirectImageCapture
{
    #region Variables

    private FileStream _fileStream;
    private BufferedStream _bufferedStream;
    private DeflateStream _compressStream;

    #endregion

    public override void Start(int delay, int left, int top, int width, int height, double dpi, ProjectInfo project)
    {
        base.Start(delay, left, top, width, height, dpi, project);

        _fileStream = new FileStream(project.CachePath, FileMode.Create, FileAccess.Write, FileShare.None);
        _bufferedStream = new BufferedStream(_fileStream, UserSettings.All.MemoryCacheSize * 1048576); //Each 1 MB has 1_048_576 bytes.
        _compressStream = new DeflateStream(_bufferedStream, UserSettings.All.CaptureCompression, true);
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
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource?.Dispose();
                return FrameCount;
            }

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = stream.Length;
            frame.Data = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Data, height * Width * 4, Width * 4);
            }

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

                        //int left, right, top, bottom;
                        //switch (DisplayRotation)
                        //{
                        //    case DisplayModeRotation.Rotate90:
                        //    {
                        //        //TODO:
                        //        left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        //        right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        //        top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        //        bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);

                        //        //left = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);
                        //        //right = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        //        //top = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        //        //bottom = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);

                        //        break;
                        //    }

                        //    case DisplayModeRotation.Rotate180:
                        //    {
                        //        //TODO:
                        //        left = Math.Max(dirtyRectangles[dirtyIndex].Top + OffsetTop, Top);
                        //        right = Math.Min(dirtyRectangles[dirtyIndex].Bottom + OffsetTop, Top + Height);
                        //        top = Math.Min(dirtyRectangles[dirtyIndex].Right + OffsetLeft, Left + Width);
                        //        bottom = Math.Max(dirtyRectangles[dirtyIndex].Left + OffsetLeft, Left);
                        //        break;
                        //    }

                        //    default:
                        //    {
                        //        //In this context, the screen positions are relative to the current screen, not to the whole set of screens (virtual space).
                        //        left = Math.Max(dirtyRectangles[dirtyIndex].Left, Left - OffsetLeft);
                        //        right = Math.Min(dirtyRectangles[dirtyIndex].Right, Left + Width - OffsetLeft);
                        //        top = Math.Max(dirtyRectangles[dirtyIndex].Top, Top - OffsetTop);
                        //        bottom = Math.Min(dirtyRectangles[dirtyIndex].Bottom, Top + Height - OffsetTop);
                        //        break;
                        //    }
                        //}

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

            //Gets the staging texture as a stream.
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource?.Dispose();
                return FrameCount;
            }

            //Sets the frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = stream.Length;
            frame.Data = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Data, height * Width * 4, Width * 4);
            }

            if (IsAcceptingFrames)
                BlockingCollection.Add(frame);

            #endregion

            Device.ImmediateContext?.UnmapSubresource(StagingTexture, 0);
            stream.Dispose();
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
                resource?.Dispose();
                return FrameCount;
            }
            else if (showCursor && info.AccumulatedFrames == 0 && info.LastMouseUpdateTime > LastProcessTime)
            {
                //Gets the cursor shape if the screen hasn't changed in between, so the cursor will be available for the next frame.
                GetCursor(null, info, frame);

                resource.Dispose();
                return FrameCount;

                //TODO: if only the mouse changed, but there's no frame accumulated, but there's data in the texture from the previous frame, I need to merge with the cursor and add to the list.
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
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource.Dispose();
                return FrameCount;
            }

            #region Get image data

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = stream.Length;
            frame.Data = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Data, height * Width * 4, Width * 4);
            }

            BlockingCollection.Add(frame);

            #endregion

            Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
            stream.Dispose();
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


    public override void Save(FrameInfo info)
    {
        System.Diagnostics.Debug.WriteLine("Length:" + info.Data.Length + " " + _fileStream.Length);

        _compressStream.WriteBytes(info.Data);
        _compressStream.Flush();

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
        //await _compressStream.FlushAsync();
        await _compressStream.DisposeAsync();

        await _bufferedStream.FlushAsync();
        await _fileStream.FlushAsync();

        await _bufferedStream.DisposeAsync();
        await _fileStream.DisposeAsync();
    }


    public int Capture2(FrameInfo frame)
    {
        var res = new Result(-1);

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
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);

                stream?.Dispose();
                resource.Dispose();
                return FrameCount;
            }

            #region Get image data

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = stream.Length;
            frame.Data = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Data, height * Width * 4, Width * 4);
            }

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

    public int CaptureWithCursor2(FrameInfo frame)
    {
        var res = new Result(-1);

        try
        {
            //Try to get the duplicated output frame within given time.
            res = DuplicatedOutput.TryAcquireNextFrame(0, out var info, out var resource);

            //Checks how to proceed with the capture. It could have failed, or the screen, cursor or both could have been captured.
            if (res.Failure || resource == null || (info.AccumulatedFrames == 0 && info.LastMouseUpdateTime <= LastProcessTime))
            {
                //Somehow, it was not possible to retrieve the resource, frame or metadata.
                resource?.Dispose();
                return FrameCount;
            }
            else if (info.AccumulatedFrames == 0 && info.LastMouseUpdateTime > LastProcessTime)
            {
                //Gets the cursor shape if the screen hasn't changed in between, so the cursor will be available for the next frame.
                GetCursor(null, info, frame);

                resource.Dispose();
                return FrameCount;

                //TODO: if only the mouse changed, but there's no frame accumulated, but there's data in the texture from the previous frame, I need to merge with the cursor and add to the list.
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
            var data = Device.ImmediateContext.MapSubresource(StagingTexture, 0, MapMode.Read, MapFlags.None, out var stream);

            if (data.IsEmpty)
            {
                Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
                stream?.Dispose();
                resource.Dispose();
                return FrameCount;
            }

            #region Get image data

            //Set frame details.
            FrameCount++;
            frame.Path = $"{Project.FullPath}{FrameCount}.png";
            frame.Delay = FrameRate.GetMilliseconds();
            frame.DataLength = stream.Length;
            frame.Data = new byte[stream.Length];

            //BGRA32 is 4 bytes.
            for (var height = 0; height < Height; height++)
            {
                stream.Position = height * data.RowPitch;
                Marshal.Copy(new IntPtr(stream.DataPointer.ToInt64() + height * data.RowPitch), frame.Data, height * Width * 4, Width * 4);
            }

            BlockingCollection.Add(frame);

            #endregion

            Device.ImmediateContext.UnmapSubresource(StagingTexture, 0);
            stream.Dispose();
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
}