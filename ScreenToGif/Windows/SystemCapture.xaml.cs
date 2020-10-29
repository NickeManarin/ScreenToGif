using ScreenToGif.Model;
using ScreenToGif.SystemCapture;
using ScreenToGif.Util;
using ScreenToGif.Util.InputHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;

namespace ScreenToGif.Windows
{
    public partial class SystemCapture
    {
        private readonly GraphicsCaptureItem _item;

        /// <summary>
        /// Processing frame tasks
        /// </summary>
        private readonly List<Task> _processFrameTasks = new List<Task>();

        /// <summary>
        /// Keyboard hooks.
        /// </summary>
        private InputHook _actHook;

        private IDirect3DDevice _device;
        private int _fps;
        private int _frameCount;
        private Direct3D11CaptureFramePool _framePool;

        /// <summary>
        /// Last frame size.
        /// </summary>
        private SizeInt32 _lastSize;

        private TimeSpan? _lastTimestamp;
        private GraphicsCaptureSession _session;

        public SystemCapture(GraphicsCaptureItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            item.Closed += GraphicsCaptureItem_Closed;
            _item = item;
            _lastSize = item.Size;

            InitializeComponent();

            InitKeyboardHook();
        }

        private void FramePool_FrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            using (var frame = _framePool.TryGetNextFrame())
            {
                ProcessFrame(frame);
            }
        }

        private void GraphicsCaptureItem_Closed(GraphicsCaptureItem sender, object args)
        {
            Close();
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

        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
            {
                RecordButton_Click(null, null);
            }
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut)
            {
                StopButton_Click(null, null);
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

            if (frame.ContentSize.Width != _lastSize.Width ||
                frame.ContentSize.Height != _lastSize.Height)
            {
                _lastSize = frame.ContentSize;
                ResetFramePool(frame.ContentSize);
            }

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
            Project = new ProjectInfo().CreateProjectFolder(ProjectByType.SystemCapture);

            Stage = Stage.Recording;

            StartCaptureAsync();
        }

        private async void ResetFramePool(SizeInt32 size)
        {
            // Don't know why need to use async, if sync it will block the UI.
            await Dispatcher.InvokeAsync(() =>
            {
                _framePool.Recreate(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, size);
            });
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
            await WaitForAllFrameProcessed();

            StopCapture();

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

        private async Task WaitForAllFrameProcessed()
        {
            await Task.WhenAll(_processFrameTasks);
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