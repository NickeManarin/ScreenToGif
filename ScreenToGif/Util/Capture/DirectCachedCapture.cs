using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Model;
using SharpDX;
using SharpDX.Direct3D11;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace ScreenToGif.Util.Capture
{
    /// <summary>
    /// Frame capture using the DesktopDuplication API and memory cache.
    /// Adapted from:
    /// https://github.com/ajorkowski/VirtualSpace
    /// https://github.com/Microsoft/Windows-classic-samples/blob/master/Samples/DXGIDesktopDuplication
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
                Application.Current.Dispatcher.Invoke(() => OnError.Invoke(ex));
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

        public override async Task<int> ManualCaptureAsync(FrameInfo frame, bool showCursor = false)
        {
            return await Task.Factory.StartNew(() => ManualCapture(frame, showCursor));
        }


        public override void Save(FrameInfo info)
        {
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
    }
}