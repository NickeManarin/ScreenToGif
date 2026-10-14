using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;
using ScreenToGif.Linux.Services.Capture;

namespace ScreenToGif.Linux;

/// <summary>
/// The recorder frame: a transparent viewport over whatever is being recorded, with a command bar
/// beneath it. The window is a thin adapter — every rule about stages, pacing, and delays lives in
/// <see cref="RecordingSession"/>, which runs on its own thread so grabbing never stalls the UI.
/// </summary>
public partial class RecorderWindow : Window, ICaptureShellWindow
{
    private const double ViewportBorder = 1;
    private const double CommandBarHeight = 31;

    private readonly X11WindowInputRegion _inputRegion = new();
    private readonly DestructiveActionGuard _destructiveActions = new();

    private RecordingLoop? _loop;
    private EditorWorkspace? _workspace;
    private RecordingStatus _status;
    private PixelRect _region = new(0, 0, RecorderRegion.MinimumSide, RecorderRegion.MinimumSide);
    private bool _inputRegionUpdateQueued;
    private Task _settingsWrites = Task.CompletedTask;
    private bool _suppressFieldSync;
    private bool _suppressRegionSync;
    private bool _regionFieldBeingTyped;
    private bool _isClosed;
    private bool _allowClose;
    private bool _sessionUnavailable;

    public RecorderWindow()
    {
        InitializeComponent();

        ModeSelector.ItemsSource = CaptureModeChoice.All;
        SeedFieldsFromSettings();
        _status = RecordingSession.IdleStatus(SelectedMode);

        Opened += RecorderOpened;
        SizeChanged += RecorderSizeChanged;
        PositionChanged += RecorderPositionChanged;
        Activated += RecorderActivated;
        Closing += RecorderClosing;
        Closed += RecorderClosed;
        KeyDown += RecorderKeyDown;

        RestoreRememberedPosition();
        RenderCommands();
    }

    /// <inheritdoc />
    public bool HandedOffToEditor { get; private set; }

    private RecorderCaptureMode SelectedMode =>
        (ModeSelector.SelectedItem as CaptureModeChoice)?.Mode ?? RecorderCaptureMode.PerSecond;

    private bool IsRecordingUnderWay => _loop is not null;

    // ---- Lifecycle -------------------------------------------------------------------------

    private void RecorderOpened(object? sender, EventArgs e)
    {
        if (!X11ScreenSource.IsSupportedSession(TryGetPlatformHandle()?.HandleDescriptor))
        {
            // ASM-3: no XID means a native Wayland session, which has no X11 view of the desktop.
            _sessionUnavailable = true;
            RecordButton.IsEnabled = false;
            ToolTip.SetTip(RecordButton,
                "Recording needs an X11 session. Under Wayland the screen is only reachable through the " +
                "xdg-desktop-portal ScreenCast backend, which the Linux build does not have yet.");
            ToolTip.SetTip(SnapWindowButton, ToolTip.GetTip(RecordButton));
            SetStatusText("Recording is unavailable under Wayland.");
        }

        RestoreRememberedSize();
        QueueInputRegionUpdate();
        RefreshRegion();
    }

    private void RecorderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        QueueInputRegionUpdate();
        RefreshRegion();
    }

    private void RecorderPositionChanged(object? sender, PixelPointEventArgs e)
    {
        QueueInputRegionUpdate();
        RefreshRegion();
    }

    private void RecorderActivated(object? sender, EventArgs e) => QueueInputRegionUpdate();

    private async void RecorderClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _status.FrameCount == 0)
        {
            RememberPlacement();
            return;
        }

        // Closing with frames in hand throws them away, so ask first.
        e.Cancel = true;
        var confirmed = await _destructiveActions.ConfirmAsync(true, () => new Controls.ConfirmDialog(
            "Close the recorder",
            $"Closing now discards {FrameCountText(_status.FrameCount)}. Stop instead to open the recording in the editor.",
            "Discard and close").ShowForAsync(this));

        if (!confirmed)
            return;

        _allowClose = true;
        Close();
    }

    private async void RecorderClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        _inputRegion.Dispose();
        await TearDownRecordingAsync();
    }

    private void RecorderKeyDown(object? sender, KeyEventArgs e)
    {
        // ASM-4: the recorder's shortcuts act only while it has focus; there is no global hook.
        switch (e.Key)
        {
            case Key.F7:
                RecordClick(sender, new RoutedEventArgs());
                break;
            case Key.F8 when _status.CanStop:
                StopClick(sender, new RoutedEventArgs());
                break;
            case Key.F9 when _status.CanDiscard:
                DiscardClick(sender, new RoutedEventArgs());
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    // ---- Commands --------------------------------------------------------------------------

    private async void RecordClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_sessionUnavailable)
                return;

            if (_status.CanPause)
            {
                _loop?.Post(session => session.Pause());
                return;
            }

            if (!await EnsureRecordingStartedAsync())
                return;

            if (SelectedMode == RecorderCaptureMode.Manual)
                _loop?.Post(session => session.Snap());
            else
                _loop?.Post(session => session.Record());
        }
        catch (Exception exception)
        {
            ReportFailure($"The recording could not be started: {exception.Message}");
        }
    }

    private async void StopClick(object? sender, RoutedEventArgs e)
    {
        if (_loop is null)
            return;

        try
        {
            var frames = await _loop.StopAsync();
            if (frames.Count == 0)
            {
                await TearDownRecordingAsync();
                SetStatusText("Stopped with nothing recorded.");
                RenderCommands();
                return;
            }

            await HandOffToEditorAsync(frames);
        }
        catch (ScreenCaptureException exception)
        {
            ReportFailure(exception.Message);
        }
        catch (Exception exception)
        {
            ReportFailure($"The recording could not be finished: {exception.Message}");
        }
    }

    private async void DiscardClick(object? sender, RoutedEventArgs e)
    {
        if (_loop is null)
            return;

        var confirmed = await _destructiveActions.ConfirmAsync(
            LinuxSettings.Current.RecorderAskBeforeDiscarding,
            () => new Controls.ConfirmDialog(
                "Discard recording",
                $"Permanently discard {FrameCountText(_status.FrameCount)}? This cannot be undone.",
                "Discard").ShowForAsync(this));

        if (!confirmed)
            return;

        await TearDownRecordingAsync();
        SetStatusText("Recording discarded.");
        RenderCommands();
    }

    private async void OptionsClick(object? sender, RoutedEventArgs e)
    {
        var options = App.CurrentApp?.ShowOptions(this, OptionsSection.Recorder);
        if (options is null)
            return;

        await options;

        // Options can change the capture mode, and this window's dropdown is what the next
        // recording starts from. A recording under way keeps the mode it started at, so the
        // fields wait until it ends; TearDownRecordingAsync seeds them then.
        if (_isClosed || IsRecordingUnderWay)
            return;

        SeedFieldsFromSettings();
        _status = RecordingSession.IdleStatus(SelectedMode);
        RenderCommands();
    }

    private void SeedFieldsFromSettings()
    {
        var settings = LinuxSettings.Current;
        _suppressFieldSync = true;
        try
        {
            ModeSelector.SelectedItem = CaptureModeChoice.For(settings.RecorderCaptureMode);
            FrequencyInput.Value = settings.RecorderFramesPerSecond;
        }
        finally
        {
            _suppressFieldSync = false;
        }
    }

    // ---- Recording ownership ---------------------------------------------------------------

    private async Task<bool> EnsureRecordingStartedAsync()
    {
        if (_loop is not null)
            return true;

        EditorWorkspace? workspace = null;
        try
        {
            workspace = ProjectArchive.CreateWorkspace();
            var writer = new PngFrameWriter(workspace.CreateBatch(EditorArtifactKind.Recordings));
            var session = new RecordingSession(new X11ScreenSource(), writer, new StopwatchRecordingClock(),
                CurrentRecordingSettings(), () => _region);

            _workspace = workspace;
            _loop = new RecordingLoop(session, OnStatusChanged, OnRecordingFailed);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            workspace?.Dispose();
            _workspace = null;
            await ShowFailureAsync("The recording workspace could not be created",
                $"Recorded frames are kept under {Path.GetTempPath()} until the recording is saved, and that " +
                "folder must be writable with free space. Free some space or fix its permissions, or start " +
                "ScreenToGif with TMPDIR set to a writable folder, then press Record or Snap again." +
                $"{Environment.NewLine}{Environment.NewLine}Details: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// The settings the next recording runs under, read from the store at the moment it starts.
    /// A copy taken when the window opened would not see an edit the Recorder's own Options button
    /// invited the user to make.
    /// </summary>
    private RecordingSettings CurrentRecordingSettings()
    {
        var settings = LinuxSettings.Current;
        return new RecordingSettings
        {
            Mode = SelectedMode,
            FramesPerSecond = (int)(FrequencyInput.Value ?? settings.RecorderFramesPerSecond),
            FixedFrameRate = settings.RecorderFixedFrameRate,
            ShowCursor = settings.RecorderShowCursor,
            PreStartEnabled = settings.RecorderPreStart,
            PreStartSeconds = settings.RecorderPreStartSeconds,
            ManualPlaybackDelayMs = settings.RecorderManualPlaybackDelayMs
        };
    }

    private async Task HandOffToEditorAsync(IReadOnlyList<EditorFrame> frames)
    {
        var workspace = _workspace;
        if (workspace is null)
            return;

        // Ownership moves to the project, and from there to the editor; the recorder keeps neither.
        _workspace = null;
        var project = new LoadedProject(workspace, frames);

        MainWindow? editor = null;
        var failure = await RecordingHandOff.TransferAsync(project, async handedOver =>
        {
            var window = new MainWindow();
            editor = window;
            window.Closed += (_, _) => App.CurrentApp?.HandleWindowClosed();
            window.Show();
            await window.OpenRecordingAsync(handedOver);

            // Adopting is part of taking ownership: the app has to know which window now holds the
            // recording before the recorder stops being the one that would clean it up.
            App.CurrentApp?.AdoptEditor(window);
        });

        if (failure is not null)
        {
            // The recording is already gone; the window that would have shown it is not.
            editor?.Close();
            await DisposeLoopAsync();
            ReturnToIdle();
            await ShowFailureAsync("The recording could not be opened in the editor", failure.Message);
            RenderCommands();
            return;
        }

        HandedOffToEditor = true;
        await DisposeLoopAsync();
        _allowClose = true;
        Close();
    }

    /// <summary>Ends any recording in progress and deletes whatever it had captured.</summary>
    private async Task TearDownRecordingAsync()
    {
        await DisposeLoopAsync();

        _workspace?.Dispose();
        _workspace = null;
        ReturnToIdle();
    }

    /// <summary>
    /// The fields were held still while the recording ran, so anything Options saved meanwhile has
    /// not reached them yet. Every way a recording ends without closing the window comes through
    /// here, so they are seeded again once they describe the next recording.
    /// </summary>
    private void ReturnToIdle()
    {
        if (!_isClosed)
            SeedFieldsFromSettings();

        _status = RecordingSession.IdleStatus(SelectedMode);
    }

    private async Task DisposeLoopAsync()
    {
        var loop = _loop;
        _loop = null;
        if (loop is not null)
            await loop.DisposeAsync();
    }

    // ---- Status and rendering ---------------------------------------------------------------

    private void OnStatusChanged(RecordingStatus status) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (_isClosed)
                return;

            _status = status;
            RenderCommands();
        });

    private void OnRecordingFailed(RecordingErrorEventArgs error) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (!_isClosed)
                ReportFailure(error.Message);
        });

    private void RenderCommands()
    {
        var recording = IsRecordingUnderWay;
        var manual = SelectedMode == RecorderCaptureMode.Manual;

        DiscardButton.IsVisible = recording && _status.CanDiscard;
        StopButton.IsVisible = recording && _status.CanStop;

        RecordButton.IsEnabled = !_sessionUnavailable && (!recording || _status.CanRecord || _status.CanPause ||
                                                          _status.CanSnap);
        RecordButtonIcon.Kind = _status.CanPause ? "Pause" : manual ? "Camera" : "Record";
        RecordButtonLabel.Text = _status.CanPause ? "Pause" : manual ? "Snap" : recording ? "Resume" : "Record";
        if (!_sessionUnavailable)
            ToolTip.SetTip(RecordButton, _status.CanPause
                ? "Pause recording (F7)"
                : manual
                    ? "Capture one frame (F7)"
                    : recording ? "Resume recording (F7)" : "Start recording (F7)");

        // Size is fixed once a recording starts; frequency re-opens while paused, as on Windows.
        var idle = !recording || _status.CanChangeRegion;
        ModeSelector.IsEnabled = idle;
        CaptureWidthInput.IsEnabled = idle;
        CaptureHeightInput.IsEnabled = idle;
        if (!idle && _regionFieldBeingTyped)
            ApplyRegionFields();
        FrequencyInput.IsEnabled = !recording || _status.CanChangeFrequency;

        // Manual captures only on Snap, so it has no rate to show; the other modes name their unit.
        FrequencyGroup.IsVisible = !manual;
        FrequencyUnit.Text = FrequencyUnitText(SelectedMode);
        ToolTip.SetTip(FrequencyInput, SelectedMode switch
        {
            RecorderCaptureMode.PerMinute => "Frames captured per minute.",
            RecorderCaptureMode.PerHour => "Frames captured per hour.",
            _ => "Frames captured per second."
        });

        // Disabling the fields is not enough on its own: the frame can also be resized by dragging
        // its edge, and a recording whose region changes size yields frames the editor cannot use.
        CanResize = idle;
        ReportRegion();

        SetStatusText(_sessionUnavailable ? StatusLabel.Text ?? string.Empty : _status.StatusText);
    }

    private void SetStatusText(string text) => StatusLabel.Text = text;

    private void ReportFailure(string message)
    {
        SetStatusText(message);
        _ = ShowFailureAsync("Recording problem", message);
    }

    private async Task ShowFailureAsync(string title, string message)
    {
        if (_isClosed)
            return;

        await _destructiveActions.ConfirmAsync(true,
            () => new Controls.ConfirmDialog(title, message, "Close").ShowForAsync(this));
    }

    private static string FrameCountText(int frameCount) =>
        frameCount == 1 ? "1 recorded frame" : $"{frameCount} recorded frames";

    // ---- Region and fields --------------------------------------------------------------------

    private void ModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressFieldSync)
            return;

        var mode = SelectedMode;
        PersistField(settings => settings.RecorderCaptureMode = mode);
        _status = RecordingSession.IdleStatus(mode);
        RenderCommands();
    }

    private void FrequencyChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_suppressFieldSync)
            return;

        var framesPerSecond = (int)(FrequencyInput.Value ?? LinuxSettings.Current.RecorderFramesPerSecond);
        PersistField(settings => settings.RecorderFramesPerSecond = framesPerSecond);

        // A recording keeps the frame rate it started at, as it keeps the frame's size. The field
        // re-opens while paused so the rate is ready for the next recording, not for this one.
        if (_loop is not null)
            SetStatusText($"{framesPerSecond} {FrequencyUnitText(SelectedMode)} applies to the next recording.");
    }

    private static string FrequencyUnitText(RecorderCaptureMode mode) => mode switch
    {
        RecorderCaptureMode.PerMinute => "fpm",
        RecorderCaptureMode.PerHour => "fph",
        _ => "fps"
    };

    private void RegionSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_suppressFieldSync || _suppressRegionSync)
            return;

        // Typing raises a change per keystroke, and resizing the frame to a half-typed number
        // fights the edit, so a field being typed in applies on Enter or when it loses focus.
        if (sender is NumericUpDown { IsKeyboardFocusWithin: true })
        {
            _regionFieldBeingTyped = true;
            return;
        }

        ApplyRegionFields();
    }

    private void RegionFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        ApplyRegionFields();
    }

    private void RegionFieldLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_regionFieldBeingTyped)
            ApplyRegionFields();
    }

    private void ApplyRegionFields()
    {
        _regionFieldBeingTyped = false;
        if (!CaptureWidthInput.IsEnabled)
        {
            // A recording started while a size was still being typed; it records the frame as it
            // stands, so the field goes back to saying so rather than keeping the unapplied number.
            ShowRegionInFields();
            return;
        }

        var requested = RecorderRegion.WindowSizeFor(
            (int)(CaptureWidthInput.Value ?? _region.Width),
            (int)(CaptureHeightInput.Value ?? _region.Height),
            ViewportBorder, CommandBarHeight, RenderScaling);

        _suppressRegionSync = true;
        try
        {
            Width = Math.Max(MinWidth, requested.Width);
            Height = Math.Max(MinHeight, requested.Height);
        }
        finally
        {
            _suppressRegionSync = false;
        }

        // A size below the window's minimum is clamped, and when the clamp leaves the window as it
        // was no resize follows to correct the field; reading the region back once layout has run
        // makes the field show what will be recorded either way.
        Dispatcher.UIThread.Post(RefreshRegion, DispatcherPriority.Background);
    }

    /// <summary>
    /// Recomputes the recorded rectangle from where the viewport actually sits. The origin comes
    /// from the control rather than from <see cref="Window.Position"/>, which excludes whatever
    /// frame the compositor draws around the window.
    /// </summary>
    private void RefreshRegion()
    {
        if (_isClosed || CaptureViewport.Bounds.Width <= 0)
            return;

        PixelPoint origin;
        try
        {
            origin = CaptureViewport.PointToScreen(new Point(ViewportBorder, ViewportBorder));
        }
        catch (InvalidOperationException)
        {
            // The window is not on screen yet; the next layout pass reaches here again.
            return;
        }

        _region = RecorderRegion.Calculate(origin, CaptureViewport.Bounds.Size, ViewportBorder, RenderScaling);
        ReportRegion();

        if (_suppressRegionSync || _regionFieldBeingTyped)
            return;

        ShowRegionInFields();
    }

    private void ShowRegionInFields()
    {
        _suppressFieldSync = true;
        try
        {
            CaptureWidthInput.Value = _region.Width;
            CaptureHeightInput.Value = _region.Height;
        }
        finally
        {
            _suppressFieldSync = false;
        }
    }

    /// <summary>
    /// Says which desktop pixels are being recorded, in the viewport's own tooltip. The width and
    /// height fields give the size; this is where the origin is readable, which is what someone
    /// lining the frame up against another window, or checking the capture independently, needs.
    /// </summary>
    private void ReportRegion() => ToolTip.SetTip(CaptureViewport,
        $"Recording {_region.Width}\u00D7{_region.Height} pixels at {_region.X}, {_region.Y} on the desktop. " +
        (IsRecordingUnderWay
            ? "Drag the window to move this frame; its size is fixed until the recording ends."
            : "Drag the window to move this frame, or resize it from any edge."));

    private void RestoreRememberedPosition()
    {
        var settings = LinuxSettings.Current;
        if (!settings.RecorderRememberPosition || settings.RecorderLeft is not { } left ||
            settings.RecorderTop is not { } top)
            return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint(left, top);
    }

    /// <summary>
    /// The remembered size is a capture region, so it is in physical pixels, while a window's Width
    /// and Height are logical units. The factor between them is this window's render scaling, which
    /// does not exist until the window is on a screen, so the size is restored on open rather than
    /// in the constructor. Restoring it as though the scale were 1 gave back a region that grew by
    /// the scale factor every time the Recorder was reopened on a scaled desktop.
    /// </summary>
    private void RestoreRememberedSize()
    {
        var settings = LinuxSettings.Current;
        if (!settings.RecorderRememberSize)
            return;

        var size = RecorderRegion.WindowSizeFor(settings.RecorderWidth, settings.RecorderHeight,
            ViewportBorder, CommandBarHeight, RenderScaling);
        Width = Math.Max(MinWidth, size.Width);
        Height = Math.Max(MinHeight, size.Height);
    }

    /// <summary>
    /// The frame's size and place are the window's own, so they are written when it closes. The
    /// capture mode and the frame rate are not: they are written as they change, by whichever
    /// window changed them, so nothing here has to be written back over what Options saved.
    /// </summary>
    private void RememberPlacement()
    {
        var current = LinuxSettings.Current;
        var rememberSize = current.RecorderRememberSize &&
                           (current.RecorderWidth != _region.Width || current.RecorderHeight != _region.Height);
        var rememberPosition = current.RecorderRememberPosition &&
                               (current.RecorderLeft != Position.X || current.RecorderTop != Position.Y);

        if (!rememberSize && !rememberPosition)
            return;

        var width = _region.Width;
        var height = _region.Height;
        var left = Position.X;
        var top = Position.Y;

        PersistField(settings =>
        {
            if (rememberSize)
            {
                settings.RecorderWidth = width;
                settings.RecorderHeight = height;
            }

            if (rememberPosition)
            {
                settings.RecorderLeft = left;
                settings.RecorderTop = top;
            }
        });
    }

    /// <summary>
    /// Applies a change to the stored settings on a single chain, so two edits arriving close
    /// together cannot each copy the store before the other's write lands and drop a field. The
    /// copy is taken inside the chain for the same reason.
    /// </summary>
    private void PersistField(Action<LinuxApplicationSettings> change) =>
        _settingsWrites = _settingsWrites.ContinueWith(_ =>
        {
            var updated = LinuxSettings.Current.Copy();
            change(updated);
            return PersistSettingsAsync(updated);
        }, TaskScheduler.Default).Unwrap();

    private static async Task PersistSettingsAsync(LinuxApplicationSettings settings)
    {
        try
        {
            await LinuxSettings.SaveAsync(settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Remembering the frame is a convenience; failing to do so must not break closing.
        }
    }

    // ---- Click-through frame ---------------------------------------------------------------

    private void QueueInputRegionUpdate()
    {
        ApplyInputRegion();
        if (_inputRegionUpdateQueued)
            return;

        _inputRegionUpdateQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _inputRegionUpdateQueued = false;
            if (!_isClosed)
                ApplyInputRegion();
        }, DispatcherPriority.Background);
    }

    private void ApplyInputRegion()
    {
        var handle = TryGetPlatformHandle();
        _inputRegion.TryApply(handle?.Handle ?? IntPtr.Zero, handle?.HandleDescriptor, ClientSize, RenderScaling,
            CommandBar.Bounds.Height);
    }

    /// <summary>The capture-frequency modes, with the wording the Windows recorder uses.</summary>
    private sealed record CaptureModeChoice(RecorderCaptureMode Mode, string Label)
    {
        public static readonly CaptureModeChoice[] All =
        [
            new(RecorderCaptureMode.PerSecond, "Per second"),
            new(RecorderCaptureMode.PerMinute, "Per minute"),
            new(RecorderCaptureMode.PerHour, "Per hour"),
            new(RecorderCaptureMode.Manual, "Manual")
        ];

        public static CaptureModeChoice For(RecorderCaptureMode mode) =>
            All.First(choice => choice.Mode == mode);

        public override string ToString() => Label;
    }
}
