using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using System.Runtime.InteropServices;

namespace ScreenToGif.Linux;

/// <summary>
/// The webcam recorder. Discovery, the FFmpeg frame stream, the recording clock and the PNG writer all
/// live in services; this window is the adapter that binds them to controls, keeps every Avalonia touch
/// on the UI thread, and hands the finished recording to the editor.
/// </summary>
public partial class WebcamWindow : Window, ICaptureShellWindow
{
    private const int CommandBarHeight = 31;

    private static readonly IBrush InformationBrush = Brush.Parse("#72A7E8");
    private static readonly IBrush FailureBrush = Brush.Parse("#E2574C");

    private readonly CameraDeviceCatalog _catalog;
    private readonly IFfmpegTool _ffmpeg;
    private readonly Func<CameraDevice, CameraCaptureFormat, int, CameraFrameStream> _streamFactory;
    private readonly WebcamRecordingSession _session = new(new StopwatchPlaybackClock());
    private readonly DispatcherTimer _captureTimer = new();
    private readonly object _frameLock = new();

    private CameraFrameStream? _stream;
    private CameraDevice? _device;
    private CameraCaptureFormat? _format;
    private WriteableBitmap? _previewBitmap;
    private byte[]? _latestFrame;
    private bool _renderQueued;
    private bool _hasLiveFrame;

    private EditorWorkspace? _workspace;
    private string? _batchPath;
    private WebcamFrameWriter? _writer;
    private LoadedProject? _recording;
    private bool _switchingDevice;
    private bool _switchingResolution;
    private bool _finishing;
    private bool _allowClose;
    private bool _openingCamera;
    private bool _closed;
    private bool _closeWhenFinished;
    private WebcamStatus? _lastStatus;

    public WebcamWindow() : this(new CameraDeviceCatalog(), new FfmpegTool())
    {
    }

    internal WebcamWindow(CameraDeviceCatalog catalog, IFfmpegTool ffmpeg,
        Func<CameraDevice, CameraCaptureFormat, int, CameraFrameStream>? streamFactory = null)
    {
        _catalog = catalog;
        _ffmpeg = ffmpeg;
        _streamFactory = streamFactory ?? ((device, format, rate) => CameraFrameStream.CreateForCamera(device.DevicePath, format, rate));
        InitializeComponent();

        FrequencyInput.Value = LinuxSettings.Current.WebcamFps;
        _captureTimer.Tick += CaptureTick;
        Opened += WebcamOpened;
        Closing += WebcamClosing;
        Closed += WebcamClosed;
    }

    /// <summary>The recording this window produced, handed to the coordinator exactly once after it closes.</summary>
    public LoadedProject? TakeRecording()
    {
        var recording = _recording;
        _recording = null;
        return recording;
    }

    /// <summary>
    /// The frame rate the controls currently show, mirrored into a field so that closing can store it
    /// without reading a control that is on its way out.
    /// </summary>
    private int FrameRate { get; set; } = LinuxSettings.Current.WebcamFps;

    private bool IsRecording => _session.Stage != WebcamRecordingStage.Stopped;

    private async void WebcamOpened(object? sender, EventArgs e) => await RefreshDevicesAsync();

    private async void RefreshClick(object? sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private void OptionsClick(object? sender, RoutedEventArgs e) => App.CurrentApp?.ShowOptions(this);

    private async void DeviceChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_switchingDevice || DeviceSelector.SelectedItem is not CameraDevice device)
            return;

        await OpenDeviceAsync(device);
    }

    private async void ResolutionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_switchingResolution || _openingCamera || IsRecording
            || DeviceSelector.SelectedItem is not CameraDevice device
            || ResolutionSelector.SelectedItem is not CameraResolution resolution)
            return;

        await OpenDeviceAsync(device, resolution);
    }

    /// <summary>
    /// Rediscovers cameras and reopens the preview. The previous stream is always torn down first, so
    /// the camera is released before another process or another node is asked for it.
    /// </summary>
    private async Task RefreshDevicesAsync()
    {
        if (IsRecording || _openingCamera)
            return;

        _openingCamera = true;
        try
        {
            RefreshControls();
            await CloseStreamAsync();

            var discovery = await Task.Run(_catalog.Discover);
            var status = WebcamStatus.FromDiscovery(discovery);

            _switchingDevice = true;
            try
            {
                ClearResolutions();
                DeviceSelector.ItemsSource = discovery.Devices;
                DeviceSelector.SelectedItem = discovery.Devices.FirstOrDefault();
            }
            finally
            {
                _switchingDevice = false;
            }

            ApplyStatus(status);

            if (DeviceSelector.SelectedItem is CameraDevice device)
                await OpenCameraAsync(device);
        }
        finally
        {
            _openingCamera = false;
            RefreshControls();
        }
    }

    /// <summary>
    /// Opens one camera, and is the only caller of the stream-installing path that is not already
    /// holding the gate. Discovery and format listing both await, and the controls that could start a
    /// second open are disabled meanwhile, so two streams can never be installed over each other.
    /// </summary>
    private async Task OpenDeviceAsync(CameraDevice device, CameraResolution? requested = null)
    {
        if (IsRecording || _openingCamera)
            return;

        _openingCamera = true;
        try
        {
            RefreshControls();
            await OpenCameraAsync(device, requested);
        }
        finally
        {
            _openingCamera = false;
            RefreshControls();
        }
    }

    /// <summary>
    /// Holds every control that could start a second open, or a recording with no stream behind it,
    /// for as long as one open is in flight.
    /// </summary>
    /// <summary>
    /// Applies the one derivation of what is usable right now. Every path that changes a control's
    /// state goes through here, so no two of them can disagree about the same button.
    /// </summary>
    private void RefreshControls()
    {
        var controls = WebcamControls.For(
            _lastStatus ?? WebcamStatus.Ready,
            _session.Stage,
            _openingCamera,
            _stream is not null && _hasLiveFrame,
            _session.HasFrames,
            _recording is not null);

        var recording = _session.Stage == WebcamRecordingStage.Recording;
        var paused = _session.Stage == WebcamRecordingStage.Paused;

        RecordButton.IsEnabled = controls.CanRecord;
        RecordIcon.Kind = recording ? "Pause" : "Record";
        RecordLabel.Text = recording ? "Pause" : paused ? "Continue" : "Record";
        ToolTip.SetTip(RecordButton, recording
            ? "Pause the recording. Shortcut: F7"
            : paused ? "Continue the recording. Shortcut: F7" : "Start recording the preview. Shortcut: F7");

        StopButton.IsEnabled = controls.CanStop;
        DiscardButton.IsVisible = controls.ShowDiscard;
        DeviceSelector.IsEnabled = controls.CanChooseDevice;
        ResolutionSelector.IsEnabled = controls.CanChooseDevice && ResolutionSelector.ItemCount > 0;
        RefreshButton.IsEnabled = controls.CanRefresh;
        FrequencyInput.IsEnabled = controls.CanChangeFrameRate;
        ScaleButton.IsEnabled = controls.CanScale;
        OptionsButton.IsEnabled = controls.CanOpenOptions;
        StatusRetryButton.IsVisible = controls.ShowRetry;

        RecordingBadge.IsVisible = recording || paused;
        RecordingGlyph.Kind = recording ? "Record" : "Pause";
        RecordingText.Text = $"{(recording ? "Recording" : "Paused")} - {Frames(_session.FrameCount)}";
    }

    private async Task OpenCameraAsync(CameraDevice device, CameraResolution? requested = null)
    {
        await CloseStreamAsync();
        ClearResolutions();
        if (_closed)
            return;

        StatusPanel.IsVisible = true;
        StatusHeading.Text = "Opening camera";
        StatusHeading.Foreground = InformationBrush;
        StatusMessage.Text = "Checking capture sizes and starting the preview.";
        StatusDetail.IsVisible = false;
        StatusRetryButton.IsVisible = false;

        CameraCaptureFormat? format;
        CameraResolution? selected;
        try
        {
            var listing = await _ffmpeg.RunFfmpegAsync(CameraFormatCatalog.ListArguments(device.DevicePath));
            if (_closed)
                return;
            var formats = CameraFormatCatalog.Parse(listing.StandardError);
            var sizes = CameraFormatCatalog.Sizes(formats);
            LinuxSettings.Current.WebcamResolutions.TryGetValue(device.PreferenceKey, out var saved);
            selected = CameraFormatCatalog.SelectSize(formats,
                requested is not null && sizes.Contains(requested) ? requested : saved);
            _switchingResolution = true;
            try
            {
                ResolutionSelector.ItemsSource = sizes;
                ResolutionSelector.SelectedItem = selected;
            }
            finally
            {
                _switchingResolution = false;
            }

            format = selected is null ? null : CameraFormatCatalog.ChooseAtSize(formats, selected);
        }
        catch (FfmpegUnavailableException ex)
        {
            ApplyStatus(WebcamStatus.FromMissingFfmpeg(device.DevicePath, ex.Message));
            return;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            ApplyStatus(WebcamStatus.FromOpenFailure(device.DevicePath, ex.Message));
            return;
        }

        if (format is null)
        {
            ApplyStatus(WebcamStatus.FromOpenFailure(device.DevicePath, "FFmpeg listed no usable capture format for it."));
            return;
        }

        // Store the first concrete choice and every replacement before opening the stream. A failed
        // mode remains selected so the person can choose another size without a silent fallback.
        if (selected is not null && (!LinuxSettings.Current.WebcamResolutions.TryGetValue(device.PreferenceKey, out var stored)
                                     || stored != selected))
        {
            try
            {
                await LinuxSettings.UpdateAsync(settings => settings.WebcamResolutions[device.PreferenceKey] = selected);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await ShowMessageAsync("Capture size", $"The chosen capture size could not be remembered: {ex.Message}");
            }
        }

        var stream = _streamFactory(device, format, FrameRate);
        stream.FrameArrived += FrameArrived;
        stream.Failed += StreamFailed;
        try
        {
            stream.Start();
        }
        catch (InvalidOperationException ex)
        {
            stream.FrameArrived -= FrameArrived;
            stream.Failed -= StreamFailed;
            await stream.DisposeAsync();
            ApplyStatus(ex is FfmpegUnavailableException
                ? WebcamStatus.FromMissingFfmpeg(device.DevicePath, ex.Message)
                : WebcamStatus.FromOpenFailure(device.DevicePath, ex.Message));
            return;
        }

        if (_closed)
        {
            // The window went away while this open was in flight. Installing the stream now would
            // leave an FFmpeg child holding the camera for the rest of the process, with nothing
            // left to dispose it.
            stream.FrameArrived -= FrameArrived;
            stream.Failed -= StreamFailed;
            await stream.DisposeAsync();
            return;
        }

        _stream = stream;
        _device = device;
        _format = format;
        _previewBitmap = new WriteableBitmap(
            new PixelSize(format.Width, format.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        Preview.Source = _previewBitmap;

        ApplyStatus(WebcamStatus.Ready);
        CaptureSummary.Text = WebcamStatus.DescribeCapture(device, format);
        Title = $"ScreenToGif - Webcam Recorder - {CaptureSummary.Text}";
        ApplyPreviewScale();
    }

    private void ClearResolutions()
    {
        _switchingResolution = true;
        try
        {
            ResolutionSelector.ItemsSource = null;
            ResolutionSelector.SelectedItem = null;
        }
        finally
        {
            _switchingResolution = false;
        }
    }

    private async Task CloseStreamAsync()
    {
        if (_stream is not { } stream)
            return;

        _stream = null;
        _hasLiveFrame = false;
        stream.FrameArrived -= FrameArrived;
        stream.Failed -= StreamFailed;
        await stream.DisposeAsync();

        Preview.Source = null;
        Preview.IsVisible = false;
        _previewBitmap?.Dispose();
        _previewBitmap = null;
        lock (_frameLock)
            _latestFrame = null;
    }

    /// <summary>Runs on the stream's reader thread; the frame buffer is only valid until this returns.</summary>
    private void FrameArrived(object? sender, CameraFrame frame)
    {
        lock (_frameLock)
        {
            if (_latestFrame is null || _latestFrame.Length != frame.ByteLength)
                _latestFrame = new byte[frame.ByteLength];
            frame.Bgra.AsSpan(0, frame.ByteLength).CopyTo(_latestFrame);

            if (_renderQueued)
                return;
            _renderQueued = true;
        }

        Dispatcher.UIThread.Post(RenderLatestFrame, DispatcherPriority.Render);
    }

    private void StreamFailed(object? sender, CameraStreamFailure failure) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(sender, _stream))
                HandleStreamFailure(failure);
        });

    /// <summary>
    /// The child has gone, so the stream is released before anything else: leaving it in place would
    /// let Record turn itself back on and record the last frame over and over from a camera that is no
    /// longer there. Applying the status last leaves the controls describing the failure.
    /// </summary>
    private async void HandleStreamFailure(CameraStreamFailure failure)
    {
        var status = WebcamStatus.FromStreamFailure(failure);
        if (IsRecording)
            await StopRecordingAfterFailureAsync(status.Message);

        await CloseStreamAsync();
        ApplyStatus(status);
    }

    private void RenderLatestFrame()
    {
        if (_previewBitmap is null)
        {
            lock (_frameLock)
                _renderQueued = false;
            return;
        }

        using (var buffer = _previewBitmap.Lock())
        {
            lock (_frameLock)
            {
                _renderQueued = false;
                var stride = buffer.Size.Width * 4;
                // The frame and the bitmap are always the capture size, but they are set from different
                // threads; skipping a frame too small for the bitmap is cheaper than trusting that.
                if (_latestFrame is null || _latestFrame.Length < (long)stride * buffer.Size.Height)
                    return;

                for (var row = 0; row < buffer.Size.Height; row++)
                    Marshal.Copy(_latestFrame, row * stride, buffer.Address + row * buffer.RowBytes, stride);
            }
        }

        Preview.IsVisible = true;
        StatusPanel.IsVisible = false;
        if (!_hasLiveFrame)
        {
            _hasLiveFrame = true;
            RefreshControls();
        }

        Preview.InvalidateVisual();
    }

    private void ApplyStatus(WebcamStatus status)
    {
        _lastStatus = status;
        var unusable = status.Availability != WebcamAvailability.Ready;

        StatusPanel.IsVisible = unusable || !_hasLiveFrame;
        StatusHeading.Text = unusable ? status.Heading : _hasLiveFrame ? string.Empty : "Opening camera";
        StatusHeading.Foreground = status.IsFailure ? FailureBrush : InformationBrush;
        StatusMessage.Text = unusable ? status.Message : _hasLiveFrame ? string.Empty : "Waiting for the first frame.";
        StatusDetailText.Text = status.Detail;
        StatusDetail.IsVisible = status.Detail.Length > 0;
        StatusDetail.IsExpanded = false;

        if (unusable)
        {
            Preview.IsVisible = false;
            CaptureSummary.Text = string.Empty;
            Title = "ScreenToGif - Webcam Recorder";
        }

        RefreshControls();
    }

    private async void RecordOrPauseClick(object? sender, RoutedEventArgs e)
    {
        switch (_session.Stage)
        {
            case WebcamRecordingStage.Stopped:
                await StartRecordingAsync();
                break;
            case WebcamRecordingStage.Recording:
                _session.Pause();
                _captureTimer.Stop();
                break;
            case WebcamRecordingStage.Paused:
                _session.Resume();
                _captureTimer.Start();
                break;
        }

        UpdateRecordingControls();
    }

    private async Task StartRecordingAsync()
    {
        if (_format is null || _stream is not { ChildHasExited: false })
            return;

        // Starting over abandons a recording that is still waiting to be handed to the editor.
        _recording?.Dispose();
        _recording = null;

        try
        {
            _workspace = ProjectArchive.CreateWorkspace();
            _batchPath = _workspace.CreateBatch(EditorArtifactKind.Recordings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _workspace?.Dispose();
            _workspace = null;
            _batchPath = null;
            await ShowMessageAsync("Start recording", $"A workspace for the recording could not be created: {ex.Message}");
            return;
        }

        _writer = new WebcamFrameWriter();
        _session.Start(FrameRate, WebcamRecordingSession.MaximumFramesFor(_format));
        _captureTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(5, _session.IntervalMs / 2));
        _captureTimer.Start();
    }

    private void CaptureTick(object? sender, EventArgs e)
    {
        if (_writer is null || _batchPath is null || _format is null)
            return;

        if (_writer.FirstFailure is { } failure)
        {
            _ = StopRecordingAfterFailureAsync($"A recorded frame could not be written: {failure.Message}");
            return;
        }

        if (!_session.IsCaptureDue)
            return;

        if (_session.IsAtFrameLimit)
        {
            StopAtRecordingLimit();
            return;
        }

        var path = _session.NextFramePath(_batchPath);
        bool queued;
        lock (_frameLock)
        {
            if (_latestFrame is null || _latestFrame.Length < (long)_format.Width * _format.Height * 4)
                return;
            queued = _writer.TryEnqueue(path, new CameraFrame(_format.Width, _format.Height, _latestFrame));
        }

        if (queued)
        {
            _session.RegisterCapture(path);
        }
        else
        {
            _session.RegisterDroppedCapture();
        }

        UpdateRecordingControls();
    }

    /// <summary>Ends a recording that reached the editor's limits, keeping every frame already taken.</summary>
    private async void StopAtRecordingLimit()
    {
        var reached = _session.MaximumFrames;
        _captureTimer.Stop();
        await FinishRecordingAsync(andClose: false);
        UpdateRecordingControls();
        await ShowMessageAsync(
            "Recording stopped",
            $"The recording reached the editor's limit of {reached} frames at this capture size, so it was stopped. "
            + "Every frame recorded so far is kept: press Stop to open them in the editor.");
    }

    private async void StopClick(object? sender, RoutedEventArgs e) => await FinishRecordingAsync(andClose: true);

    /// <summary>
    /// Ends the recording and, when it produced frames, leaves them in <see cref="TakeRecording"/> for
    /// the coordinator to hand to the editor as the window closes.
    /// </summary>
    private async Task FinishRecordingAsync(bool andClose)
    {
        if (_finishing)
        {
            // A close arriving mid-finish must not be dropped; the call already running honours it.
            _closeWhenFinished |= andClose;
            return;
        }

        _finishing = true;

        try
        {
            _captureTimer.Stop();
            var writeFailure = await DrainWriterAsync();
            var frames = _session.Stage == WebcamRecordingStage.Stopped ? [] : _session.Stop();

            if (writeFailure is not null)
            {
                await ShowMessageAsync("Stop recording", writeFailure);
                DeleteRecordedFiles(frames.Select(frame => frame.FilePath));
                ReleaseRecordingWorkspace();
                frames = [];
            }

            if (frames.Count > 0 && _workspace is not null)
            {
                // A recording stopped at the editor's limit stays pending while the window is open, so
                // there can already be one here; releasing it first keeps its workspace off the disk.
                _recording?.Dispose();
                _recording = new LoadedProject(_workspace, frames);
                _workspace = null;
                _batchPath = null;
            }
            else
            {
                ReleaseRecordingWorkspace();
            }

            if (andClose || _closeWhenFinished)
            {
                _allowClose = true;
                _closed = true;
                Close();
                return;
            }

            RefreshControls();
        }
        finally
        {
            _finishing = false;
        }
    }

    /// <summary>Waits for every queued frame to reach disk; returns the failure message when one could not.</summary>
    private async Task<string?> DrainWriterAsync()
    {
        if (_writer is not { } writer)
            return null;

        _writer = null;
        try
        {
            await writer.CompleteAsync();
            return null;
        }
        catch (IOException ex)
        {
            return ex.InnerException?.Message ?? ex.Message;
        }
    }

    private async void DiscardClick(object? sender, RoutedEventArgs e)
    {
        // A recording sealed at the frame limit is held in _recording rather than in the session, and
        // Discard is offered for it too, so it has to look in both places or the button does nothing.
        var pending = _recording?.Frames.Count ?? 0;
        var count = _session.HasFrames ? _session.FrameCount : pending;
        if (count == 0)
            return;

        if (LinuxSettings.Current.AskBeforeDiscardProject && !await new Controls.ConfirmDialog(
                "Discard recording",
                $"Throw away the {Frames(count)} recorded so far? This cannot be undone.",
                "Discard").ShowForAsync(this))
            return;

        _captureTimer.Stop();
        await DrainWriterAsync();
        DeleteRecordedFiles(_session.Discard());
        _recording?.Dispose();
        _recording = null;
        ReleaseRecordingWorkspace();
        RefreshControls();
    }

    /// <summary>Stops a recording that cannot continue and removes what it had already written.</summary>
    private async Task StopRecordingAfterFailureAsync(string message)
    {
        _captureTimer.Stop();
        await DrainWriterAsync();
        DeleteRecordedFiles(_session.Stage == WebcamRecordingStage.Stopped ? [] : _session.Discard());
        ReleaseRecordingWorkspace();
        UpdateRecordingControls();
        await ShowMessageAsync("Recording stopped", message);
    }

    private static string Frames(int count) => count == 1 ? "1 frame" : $"{count} frames";

    private static void DeleteRecordedFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The workspace is removed next, which covers anything left behind here.
            }
        }
    }

    private void ReleaseRecordingWorkspace()
    {
        _workspace?.Dispose();
        _workspace = null;
        _batchPath = null;
    }

    private void UpdateRecordingControls() => RefreshControls();

    private void FrameRateChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        FrameRate = (int)Math.Clamp(
            e.NewValue ?? WebcamRecordingSession.MinimumFps,
            WebcamRecordingSession.MinimumFps,
            WebcamRecordingSession.MaximumFps);

        if (_captureTimer.IsEnabled)
            _captureTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(5, WebcamRecordingSession.IntervalFor(FrameRate) / 2));
    }

    /// <summary>
    /// Scale resizes this window only. The stream keeps the size it was opened with, so recorded frames
    /// stay at the camera's resolution; see the ADR "Record webcam frames at the camera's capture resolution".
    /// </summary>
    private void ScaleChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty)
            return;

        ScaleLabel.Text = $"Preview scale: {ScaleSlider.Value:0.0}";
        ApplyPreviewScale();
    }

    private void ApplyPreviewScale()
    {
        if (_format is null)
            return;

        // Clamped to MinWidth, which is what the command bar needs: a narrower window would push Stop,
        // the action that ends the loop, off the right edge.
        Width = Math.Max(MinWidth, _format.Width * ScaleSlider.Value);
        Height = Math.Max(MinHeight, _format.Height * ScaleSlider.Value + CommandBarHeight);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F7 when RecordButton.IsEnabled:
                RecordOrPauseClick(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.F8 when StopButton.IsEnabled:
                StopClick(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.F9 when DiscardButton.IsVisible:
                DiscardClick(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    /// <summary>
    /// Closing with recorded frames hands them to the editor, as the Windows recorder does; closing with
    /// none simply returns to Startup. Finishing needs to await the frame writer, so the first close is
    /// cancelled and repeated once the recording has been sealed.
    /// </summary>
    private async void WebcamClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
        {
            _closed = true;
            return;
        }

        e.Cancel = true;
        if (_session.HasFrames || IsRecording)
        {
            await FinishRecordingAsync(andClose: true);
            return;
        }

        _allowClose = true;
        _closed = true;
        Close();
    }

    private async void WebcamClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _captureTimer.Stop();
        _captureTimer.Tick -= CaptureTick;
        await CloseStreamAsync();
        await PersistFrameRateAsync();
    }

    /// <summary>Remembers the frame rate for the next session; a rate that did not change is not rewritten.</summary>
    private async Task PersistFrameRateAsync()
    {
        if (LinuxSettings.Current.WebcamFps == FrameRate)
            return;

        try
        {
            await LinuxSettings.UpdateAsync(settings => settings.WebcamFps = FrameRate);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A remembered frame rate is a convenience; failing to store it must not break closing.
        }
    }

    /// <summary>
    /// Reports something the user has to know. A dialog needs a live owner, and every caller reaches
    /// this after awaiting the frame writer, by which time the window may already have closed; the
    /// message then falls back to the window's own status text, which the next opener still sees.
    /// </summary>
    private Task ShowMessageAsync(string title, string message)
    {
        if (_allowClose || !IsVisible)
        {
            StatusHeading.Text = title;
            StatusMessage.Text = message;
            StatusDetail.IsVisible = false;
            StatusRetryButton.IsVisible = false;
            StatusPanel.IsVisible = true;
            return Task.CompletedTask;
        }

        return new Controls.MessageDialog(title, message).ShowForAsync(this);
    }
}
