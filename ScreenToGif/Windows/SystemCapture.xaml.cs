using ScreenToGif.Model;
using ScreenToGif.SystemCapture;
using ScreenToGif.Util;
using ScreenToGif.Util.InputHook;
using ScreenToGif.Windows.Other;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace ScreenToGif.Windows
{
    public partial class SystemCapture
    {
        /// <summary>
        /// The capture item.
        /// </summary>
        private readonly GraphicsCaptureItem _item;

        /// <summary>
        /// Processing frame tasks
        /// </summary>
        private readonly List<Task> _processFrameTasks = new List<Task>();

        /// <summary>
        /// Keyboard hooks.
        /// </summary>
        private InputHook _actHook;

        /// <summary>
        /// The Direct3D device.
        /// </summary>
        private IDirect3DDevice _device;

        /// <summary>
        /// Frames per second.
        /// </summary>
        private int _fps;

        /// <summary>
        /// Total frame count.
        /// </summary>
        private int _frameCount;

        /// <summary>
        /// The Direct3D frame pool.
        /// </summary>
        private Direct3D11CaptureFramePool _framePool;

        /// <summary>
        /// Indicate current stage is paused.
        /// </summary>
        private bool _isPaused;

        /// <summary>
        /// The last frame captured timestamp.
        /// </summary>
        private TimeSpan? _lastTimestamp;

        /// <summary>
        /// The last pause operation occured time.
        /// </summary>
        private DateTime? _pauseTime;

        /// <summary>
        /// The capture session.
        /// </summary>
        private GraphicsCaptureSession _session;

        public SystemCapture(GraphicsCaptureItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            item.Closed += GraphicsCaptureItem_Closed;
            _item = item;

            InitializeComponent();

            InitKeyboardHook();
        }

        private async void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.NotifyRecordingDiscard &&
                !Dialog.Ask(LocalizationHelper.Get("S.Recorder.Discard.Title"),
                    LocalizationHelper.Get("S.Recorder.Discard.Instruction"),
                    LocalizationHelper.Get("S.Recorder.Discard.Message"),
                    yesAsDefault: false))
            {
                return;
            }

            Stage = Stage.Discarding;

            await WaitForStopCapture();

            await Task.Run(() =>
            {
                try
                {
                    // Remove all the files
                    foreach (var frame in Project.Frames)
                    {
                        try
                        {
                            if (File.Exists(frame.Path))
                            {
                                File.Delete(frame.Path);
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    try
                    {
                        Directory.Delete(Project.FullPath, true);
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Delete temp path");
                    }

                    Project.Frames.Clear();
                }
                catch (IOException ioException)
                {
                    LogWriter.Log(ioException, "Error while trying to discard the recording");
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
                    LogWriter.Log(ex, "Error while trying to discard the recording");
                }
            });

            Stage = Stage.Stopped;

            // Reset all controls status.
            RecordButton.IsEnabled = true;
            RecordButton.Visibility = Visibility.Visible;
            PauseButton.IsEnabled = true;
            PauseButton.Visibility = Visibility.Collapsed;
            StopButton.IsEnabled = true;
            StopButton.Visibility = Visibility.Collapsed;
            DiscardButton.Visibility = Visibility.Collapsed;
            DiscardButton.IsEnabled = true;
            FpsNumbericUpDown.IsEnabled = true;
        }

        private void FramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            using (var frame = _framePool.TryGetNextFrame())
            {
                if (!_isPaused)
                {
                    ProcessFrame(frame);
                }
            }
        }

        private void GraphicsCaptureItem_Closed(GraphicsCaptureItem sender, object args)
        {
            // The capture item closed then stop the capture.
            // If StopButton.IsEnabled = false that means waiting to process the remain frames.
            if (StopButton.IsEnabled)
            {
                StopButton_Click(null, null);
            }
        }

        private void InitKeyboardHook()
        {
            try
            {
                _actHook = new InputHook(false, true);
                _actHook.KeyDown += KeyHookTarget;
            }
            catch
            {
                // ignored
            }
        }

        private bool IsCursorCaptureSupported()
        {
            return ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsCursorCaptureEnabled");
        }

        private async void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            // No idea why need to call Task.Yield, Dispatcher.Invoke is not working.

            if (Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
            {
                if (Stage == Stage.Stopped || Stage == Stage.Paused)
                {
                    await Task.Yield();
                    if (RecordButton.IsEnabled && RecordButton.Visibility == Visibility.Visible)
                    {
                        RecordButton_Click(null, null);
                    }
                }
                else
                {
                    if (PauseButton.IsEnabled && PauseButton.Visibility == Visibility.Visible)
                    {
                        PauseButton_Click(null, null);
                    }
                }
            }
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut)
            {
                await Task.Yield();
                if (StopButton.IsEnabled && StopButton.Visibility == Visibility.Visible)
                {
                    StopButton_Click(null, null);
                }
            }
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
            {
                await Task.Yield();
                if (DiscardButton.IsEnabled && DiscardButton.Visibility == Visibility.Visible)
                {
                    DiscardButton_Click(null, null);
                }
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Stage == Stage.Recording)
            {
                _pauseTime = DateTime.Now;
                _isPaused = true;
                Stage = Stage.Paused;

                RecordButton.Visibility = Visibility.Visible;
                PauseButton.Visibility = Visibility.Collapsed;
                DiscardButton.Visibility = Visibility.Visible;
            }
        }

        private async void ProcessFrame(Direct3D11CaptureFrame frame)
        {
            var currentTimestamp = frame.SystemRelativeTime;
            var delay = 0;
            if (_lastTimestamp.HasValue)
            {
                var timestampDelta = currentTimestamp - _lastTimestamp.Value;
                if (timestampDelta.TotalSeconds < 1d / _fps)
                {
                    return;
                }

                delay = (int)timestampDelta.TotalMilliseconds;
            }

            _lastTimestamp = currentTimestamp;

            var tcs = new TaskCompletionSource<object>();
            try
            {
                _processFrameTasks.Add(tcs.Task);

                var fileName = $"{Project.FullPath}{_frameCount}.png";
                _frameCount++;
                Project.Frames.Add(new FrameInfo(fileName, delay));
                using (var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface))
                {
                    using (var fileStream = File.Create(fileName))
                    {
                        var randomAccessStream = fileStream.AsRandomAccessStream();
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
                        encoder.SetSoftwareBitmap(softwareBitmap);
                        await encoder.FlushAsync();
                    }
                }
            }
            finally
            {
                tcs.SetResult(null);
                _processFrameTasks.Remove(tcs.Task);
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (Stage == Stage.Stopped)
            {
                Project = new ProjectInfo().CreateProjectFolder(ProjectByType.SystemCapture);

                Stage = Stage.Recording;

                RecordButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Visible;
                DiscardButton.Visibility = Visibility.Collapsed;

                StartCaptureAsync();
            }
            else if (Stage == Stage.Paused)
            {
                // Calculate how long has been paused and add the duration to the last frame timestamp.
                if (_pauseTime.HasValue)
                {
                    var now = DateTime.Now;
                    var pausedDuration = now - _pauseTime.Value;
                    if (_lastTimestamp.HasValue)
                    {
                        _lastTimestamp = _lastTimestamp.Value + pausedDuration;
                    }
                    else
                    {
                        _lastTimestamp = pausedDuration;
                    }

                    _pauseTime = null;
                }

                _isPaused = false;
                Stage = Stage.Recording;

                RecordButton.Visibility = Visibility.Collapsed;
                PauseButton.Visibility = Visibility.Visible;
            }
        }

        private void StartCaptureAsync()
        {
            _fps = FpsNumbericUpDown.Value;
            FpsNumbericUpDown.IsEnabled = false;

            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, _item.Size);
            _framePool.FrameArrived += FramePool_FrameArrived;
            _session = _framePool.CreateCaptureSession(_item);
            if (IsCursorCaptureSupported())
            {
                _session.IsCursorCaptureEnabled = UserSettings.All.ShowCursor;
            }
            _session.StartCapture();
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            await WaitForStopCapture();

            Close();
        }

        private void StopCapture()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _session = null;
            _framePool = null;
        }

        private void SystemCapture_Unloaded(object sender, RoutedEventArgs e)
        {
            StopCapture();
        }

        private async Task WaitForAllFramesProcessed()
        {
            await Task.WhenAll(_processFrameTasks);
        }

        private async Task WaitForStopCapture()
        {
            // Unregister the event to avoid continue to capture new frames while waiting for processing remain frames.
            _framePool.FrameArrived -= FramePool_FrameArrived;

            // Make all buttons disabled to avoid click again.
            RecordButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            DiscardButton.IsEnabled = false;

            await WaitForAllFramesProcessed();

            StopCapture();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _actHook.Stop();
            }
            catch
            {
                // ignored
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _device = Direct3D11Helper.CreateDevice();
        }
    }
}