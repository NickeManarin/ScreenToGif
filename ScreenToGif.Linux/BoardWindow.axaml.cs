using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ScreenToGif.Linux.Controls;
using ScreenToGif.Linux.Models;
using ScreenToGif.Linux.Services;

namespace ScreenToGif.Linux;

/// <summary>
/// The Board recorder: a whiteboard that captures what is drawn on it as frames and hands them
/// to the editor. Drawing geometry lives in <see cref="BoardStrokeCollection"/> and the timing
/// contract in <see cref="BoardRecordingSession"/>; this window is the adapter between them and
/// the Avalonia controls.
/// </summary>
public partial class BoardWindow : Window, ICaptureShellWindow
{
    private const int ChromeWidth = 2;
    private const int ChromeHeight = 33 + 31 + 2;

    private readonly DestructiveActionGuard _destructiveActions = new();
    private readonly DispatcherTimer _captureTimer = new();
    private readonly BoardCanvasFrameSink _frameSink;
    private readonly BoardRecordingSession _session;

    private Color _brushColor = Colors.Black;
    private string? _failure;
    private bool _loadingSettings = true;
    private bool _mirroringAutoRecord;

    public BoardWindow()
    {
        InitializeComponent();

        _frameSink = new BoardCanvasFrameSink(BoardSurface);
        _session = new BoardRecordingSession(new StopwatchPlaybackClock(), _frameSink, CurrentPlan, BoardRecording.Create);

        _captureTimer.Tick += CaptureTick;
        BoardSurface.GestureStarted += CanvasGestureStarted;
        BoardSurface.GestureEnded += CanvasGestureEnded;
        BoardSurface.SizeChanged += CanvasSizeChanged;
        KeyDown += BoardKeyDown;
        KeyUp += BoardKeyUp;
        Deactivated += BoardDeactivated;
        Closing += BoardClosing;
        Closed += BoardClosed;

        LoadSettingsIntoControls();
        var settings = LinuxSettings.Current;
        Width = Math.Max(MinWidth, settings.BoardWidth + ChromeWidth);
        Height = Math.Max(MinHeight, settings.BoardHeight + ChromeHeight);
        UpdateRecordingUi();
    }

    /// <inheritdoc />
    public LoadedProject? TakeRecording() => _session.TakeProject();

    #region Settings

    /// <summary>
    /// Fills the controls from the settings store once. From then on the controls hold the Board's
    /// live tool state and each change commits only its own field, so the Board never writes a
    /// stale copy back over what Options saved meanwhile. See
    /// ADRs/20260922-recorder-settings-have-one-owner.md, which the Board follows.
    /// </summary>
    private void LoadSettingsIntoControls()
    {
        var settings = LinuxSettings.Current;
        _loadingSettings = true;

        BrushWidthInput.Value = settings.BoardBrushWidth;
        BrushHeightInput.Value = settings.BoardBrushHeight;
        EraserWidthInput.Value = settings.BoardEraserWidth;
        EraserHeightInput.Value = settings.BoardEraserHeight;
        FrequencyInput.Value = settings.BoardFps;
        BoardWidthInput.Value = settings.BoardWidth;
        BoardHeightInput.Value = settings.BoardHeight;
        FitToCurveCheckBox.IsChecked = settings.BoardFitToCurve;
        HighlighterCheckBox.IsChecked = settings.BoardHighlighter;

        EllipseTipButton.IsChecked = settings.BoardBrushTip == BoardStylusTip.Ellipse;
        RectangleTipButton.IsChecked = settings.BoardBrushTip == BoardStylusTip.Rectangle;
        EraserEllipseTipButton.IsChecked = settings.BoardEraserTip == BoardStylusTip.Ellipse;
        EraserRectangleTipButton.IsChecked = settings.BoardEraserTip == BoardStylusTip.Rectangle;
        _brushColor = ColorPickerDialog.TryParse(settings.BoardBrushColor, out var color) ? color : Colors.Black;
        BrushColorSwatch.Background = new SolidColorBrush(_brushColor);

        _loadingSettings = false;
        ApplyToolSettings();
    }

    private void ApplyToolSettings()
    {
        BoardSurface.PenAttributes = new BoardStrokeAttributes(
            _brushColor,
            Field(BrushWidthInput),
            Field(BrushHeightInput),
            RectangleTipButton.IsChecked == true ? BoardStylusTip.Rectangle : BoardStylusTip.Ellipse,
            FitToCurveCheckBox.IsChecked == true,
            HighlighterCheckBox.IsChecked == true);
        BoardSurface.EraserWidth = Field(EraserWidthInput);
        BoardSurface.EraserHeight = Field(EraserHeightInput);
        BoardSurface.EraserTip = EraserRectangleTipButton.IsChecked == true ? BoardStylusTip.Rectangle : BoardStylusTip.Ellipse;
    }

    /// <summary>
    /// Puts a tool change into effect and commits that one field. The change runs on the store's
    /// write chain, off this thread, so callers capture the value first rather than read a
    /// control from inside it. A failed write is reported and the tool keeps the new value.
    /// </summary>
    private async void CommitSetting(Action<LinuxApplicationSettings> change)
    {
        if (_loadingSettings)
            return;

        ApplyToolSettings();

        try
        {
            await LinuxSettings.UpdateAsync(change);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _failure = $"Board settings could not be saved: {FirstLine(ex)}";
            UpdateRecordingUi();
        }
    }

    #endregion

    #region Tools

    private void PenModeClick(object? sender, RoutedEventArgs e) => SelectTool(BoardTool.Pen);

    private void PointEraserModeClick(object? sender, RoutedEventArgs e) => SelectTool(BoardTool.PointEraser);

    private void StrokeEraserModeClick(object? sender, RoutedEventArgs e) => SelectTool(BoardTool.StrokeEraser);

    private void SelectTool(BoardTool tool)
    {
        BoardSurface.Tool = tool;

        PenButton.IsChecked = tool == BoardTool.Pen;
        EraserButton.IsChecked = tool == BoardTool.PointEraser;
        SelectionButton.IsChecked = tool == BoardTool.Selection;
        StrokeEraserButton.IsChecked = tool == BoardTool.StrokeEraser;

        var eraserSettings = tool == BoardTool.PointEraser;
        BrushSettingsPanel.IsVisible = !eraserSettings;
        BrushTipPanel.IsVisible = !eraserSettings;
        BrushOptionsPanel.IsVisible = !eraserSettings;
        EraserSettingsPanel.IsVisible = eraserSettings;
        EraserTipPanel.IsVisible = eraserSettings;
    }

    private async void BrushColorClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var chosen = await new ColorPickerDialog(_brushColor).ShowForAsync(this);
            if (chosen is not { } color)
                return;

            _brushColor = color;
            BrushColorSwatch.Background = new SolidColorBrush(color);
            var hex = ColorPickerDialog.ToHex(color);
            CommitSetting(settings => settings.BoardBrushColor = hex);
        }
        catch (Exception ex)
        {
            SetStatus($"The color could not be chosen: {FirstLine(ex)}");
        }
    }

    private void BrushWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var width = Field(BrushWidthInput);
        CommitSetting(settings => settings.BoardBrushWidth = width);
    }

    private void BrushHeightChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var height = Field(BrushHeightInput);
        CommitSetting(settings => settings.BoardBrushHeight = height);
    }

    private void EraserWidthChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var width = Field(EraserWidthInput);
        CommitSetting(settings => settings.BoardEraserWidth = width);
    }

    private void EraserHeightChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var height = Field(EraserHeightInput);
        CommitSetting(settings => settings.BoardEraserHeight = height);
    }

    private void EllipseTipClick(object? sender, RoutedEventArgs e) => SelectBrushTip(BoardStylusTip.Ellipse);

    private void RectangleTipClick(object? sender, RoutedEventArgs e) => SelectBrushTip(BoardStylusTip.Rectangle);

    private void SelectBrushTip(BoardStylusTip tip)
    {
        EllipseTipButton.IsChecked = tip == BoardStylusTip.Ellipse;
        RectangleTipButton.IsChecked = tip == BoardStylusTip.Rectangle;
        CommitSetting(settings => settings.BoardBrushTip = tip);
    }

    private void EraserEllipseTipClick(object? sender, RoutedEventArgs e) => SelectEraserTip(BoardStylusTip.Ellipse);

    private void EraserRectangleTipClick(object? sender, RoutedEventArgs e) => SelectEraserTip(BoardStylusTip.Rectangle);

    private void SelectEraserTip(BoardStylusTip tip)
    {
        EraserEllipseTipButton.IsChecked = tip == BoardStylusTip.Ellipse;
        EraserRectangleTipButton.IsChecked = tip == BoardStylusTip.Rectangle;
        CommitSetting(settings => settings.BoardEraserTip = tip);
    }

    private void FitToCurveChanged(object? sender, RoutedEventArgs e)
    {
        var on = FitToCurveCheckBox.IsChecked == true;
        CommitSetting(settings => settings.BoardFitToCurve = on);
    }

    private void HighlighterChanged(object? sender, RoutedEventArgs e)
    {
        var on = HighlighterCheckBox.IsChecked == true;
        CommitSetting(settings => settings.BoardHighlighter = on);
    }

    #endregion

    #region Canvas size

    private void CanvasSizeFieldKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyTypedCanvasSize();
        e.Handled = true;
    }

    private void CanvasSizeFieldLostFocus(object? sender, RoutedEventArgs e) => ApplyTypedCanvasSize();

    /// <summary>
    /// Resizes the window to give the canvas the typed size, as far as the window's minimum
    /// allows, and writes back the size the canvas actually gets. Typing applies only on Enter or
    /// focus loss: resizing on every keystroke would resize to 3 on the way to typing 300. A
    /// clamp that leaves the window unchanged raises no resize, so the size is read back here.
    /// </summary>
    private void ApplyTypedCanvasSize()
    {
        if (_session.Stage != BoardRecordingStage.Stopped)
            return;

        Width = Math.Max(MinWidth, Field(BoardWidthInput) + ChromeWidth);
        Height = Math.Max(MinHeight, Field(BoardHeightInput) + ChromeHeight);
        BoardWidthInput.Value = (int)Width - ChromeWidth;
        BoardHeightInput.Value = (int)Height - ChromeHeight;
    }

    /// <summary>
    /// The canvas fills the window, so dragging the window resizes the canvas and the fields
    /// follow — except one that is being typed in, which applies its own value when it is done.
    /// </summary>
    private void CanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!BoardWidthInput.IsKeyboardFocusWithin)
            BoardWidthInput.Value = (int)Math.Round(e.NewSize.Width);

        if (!BoardHeightInput.IsKeyboardFocusWithin)
            BoardHeightInput.Value = (int)Math.Round(e.NewSize.Height);
    }

    /// <summary>
    /// The canvas size is the window's own, so it is written when the window closes, as the
    /// Recorder writes its frame. Failing to remember it must not stand in the way of closing.
    /// </summary>
    private void RememberCanvasSize()
    {
        var width = (int)Math.Round(BoardSurface.Bounds.Width);
        var height = (int)Math.Round(BoardSurface.Bounds.Height);
        var current = LinuxSettings.Current;

        if (width is < 100 or > 3000 || height is < 100 or > 3000 ||
            (current.BoardWidth == width && current.BoardHeight == height))
            return;

        LinuxSettings.UpdateAsync(settings =>
        {
            settings.BoardWidth = width;
            settings.BoardHeight = height;
        }).ContinueWith(write => _ = write.Exception, TaskContinuationOptions.OnlyOnFaulted);
    }

    #endregion

    #region Recording

    private void FrequencyChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        var fps = Field(FrequencyInput);
        CommitSetting(settings => settings.BoardFps = fps);
    }

    /// <summary>The frame rate and frame size a recording starting now is fixed to.</summary>
    private BoardRecordingPlan CurrentPlan()
    {
        var pixels = _frameSink.FramePixels();
        return new BoardRecordingPlan(Field(FrequencyInput), pixels.Width, pixels.Height);
    }

    private void AutoRecordChanged(object? sender, RoutedEventArgs e)
    {
        if (!_mirroringAutoRecord)
            _session.SetAutoRecord(AutoRecordButton.IsChecked == true);

        UpdateRecordingUi();
    }

    private void CanvasGestureStarted(object? sender, EventArgs e) => Step(_session.PointerPressed);

    private void CanvasGestureEnded(object? sender, EventArgs e) => Step(_session.PointerReleased);

    private void RecordPauseClick(object? sender, RoutedEventArgs e) =>
        Step(() => _session.Stage == BoardRecordingStage.Recording ? _session.Pause() : _session.Start());

    /// <summary>
    /// Runs one session transition and brings the timer and the controls in line with it. A start
    /// that cannot open its workspace is reported rather than thrown into the dispatcher.
    /// </summary>
    private void Step(Func<bool> transition)
    {
        try
        {
            transition();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _failure = $"Recording could not start: {FirstLine(ex)}";
        }

        if (_session.Stage == BoardRecordingStage.Recording)
            _failure = null;

        SyncCaptureTimer();
        UpdateRecordingUi();
    }

    private void CaptureTick(object? sender, EventArgs e)
    {
        if (_session.Stage != BoardRecordingStage.Recording)
            return;

        try
        {
            _session.Tick();
        }
        catch (Exception ex)
        {
            _failure = $"Recording paused: the frame could not be saved ({FirstLine(ex)}). {FramesKept()}";
        }

        if (_session.IsAtFrameLimit)
        {
            _failure = $"Recording paused at {_session.FrameLimit:N0} frames, the most the editor can save at this size. " +
                       "Stop to keep what was recorded.";
        }

        SyncCaptureTimer();
        UpdateRecordingUi();
    }

    private void SyncCaptureTimer()
    {
        if (_session.Stage != BoardRecordingStage.Recording)
        {
            _captureTimer.Stop();
            return;
        }

        _captureTimer.Interval = TimeSpan.FromMilliseconds(_session.IntervalMs);
        _captureTimer.Start();
    }

    private void StopClick(object? sender, RoutedEventArgs e) => StopRecording();

    private void StopRecording()
    {
        if (!_session.Stop())
        {
            SetStatus("Nothing has been recorded yet.");
            return;
        }

        SyncCaptureTimer();
        Close();
    }

    private async void DiscardClick(object? sender, RoutedEventArgs e)
    {
        string? outcome = null;
        try
        {
            var discarded = await _session.DiscardAsync(() => _destructiveActions.ConfirmAsync(
                LinuxSettings.Current.RecorderAskBeforeDiscarding,
                () => new ConfirmDialog(
                    "Discard recording",
                    "Delete the frames recorded so far and clear the board? This cannot be undone.",
                    "Discard recording").ShowForAsync(this)));

            if (discarded)
            {
                BoardSurface.Strokes.Clear();
                _failure = null;
            }
            else if (_session.HasFrames)
            {
                outcome = $"Discard canceled — {FramesKept()}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _failure = $"The recording could not be discarded: {FirstLine(ex)}";
        }

        SyncCaptureTimer();
        UpdateRecordingUi();
        if (outcome is not null)
            SetStatus(outcome);
    }

    private string FramesKept() => _session.FrameCount == 1 ? "1 frame kept." : $"{_session.FrameCount} frames kept.";

    /// <summary>
    /// Mirrors the session onto the controls. A failure or the frame limit outranks the state
    /// label until the user next starts recording or discards, so a tick that follows cannot
    /// replace the one message that says why recording stopped.
    /// </summary>
    private void UpdateRecordingUi()
    {
        var stage = _session.Stage;
        var frames = _session.FrameCount;
        var idle = stage == BoardRecordingStage.Stopped;

        FrequencyInput.IsEnabled = idle;
        BoardWidthInput.IsEnabled = idle;
        BoardHeightInput.IsEnabled = idle;
        CanResize = idle;
        OptionsButton.IsEnabled = stage != BoardRecordingStage.Recording;
        DiscardButton.IsEnabled = frames > 0;
        RecordButton.IsEnabled = !_session.AutoRecord && !_session.IsAtFrameLimit;
        RecordButton.IsChecked = stage == BoardRecordingStage.Recording;

        GuidanceBanner.IsVisible = _failure is not null;
        GuidanceText.Text = _failure;

        SetStatus(_failure ?? stage switch
        {
            BoardRecordingStage.Recording => $"Recording — {frames} frames",
            BoardRecordingStage.Paused => $"Paused — {frames} frames",
            _ => $"Ready — {frames} frames"
        });
    }

    #endregion

    #region Shortcuts and Options

    private void BoardKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F8)
        {
            StopRecording();
            e.Handled = true;
            return;
        }

        if (e.Key is Key.LeftCtrl or Key.RightCtrl && _session.CtrlPressed())
            MirrorAutoRecord();
    }

    private void BoardKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftCtrl or Key.RightCtrl && _session.CtrlReleased())
            MirrorAutoRecord();
    }

    private void BoardDeactivated(object? sender, EventArgs e)
    {
        if (_session.Deactivated())
            MirrorAutoRecord();
    }

    /// <summary>Shows the session's Auto Record on its button without the button feeding the change back.</summary>
    private void MirrorAutoRecord()
    {
        _mirroringAutoRecord = true;
        AutoRecordButton.IsChecked = _session.AutoRecord;
        _mirroringAutoRecord = false;
        UpdateRecordingUi();
    }

    private async void OptionsClick(object? sender, RoutedEventArgs e)
    {
        Topmost = false;
        try
        {
            await (App.CurrentApp?.ShowOptions(this) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            SetStatus($"Options could not be opened: {FirstLine(ex)}");
        }
        finally
        {
            Topmost = true;
        }
    }

    #endregion

    #region Closing

    private void BoardClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_destructiveActions.IsPromptOpen)
        {
            e.Cancel = true;
            return;
        }

        CloseDown();
    }

    /// <summary>
    /// A window manager's close request raises Closing first, but a window torn down by the
    /// platform raises only Closed, so both close down. The recording itself stays with the
    /// session until <see cref="TakeRecording"/> hands it over.
    /// </summary>
    private void BoardClosed(object? sender, EventArgs e) => CloseDown();

    private void CloseDown()
    {
        _captureTimer.Stop();
        _session.Pause();
        _frameSink.Dispose();
        RememberCanvasSize();
    }

    #endregion

    private void SetStatus(string message)
    {
        StatusText.Text = message;
        ToolTip.SetTip(StatusText, message);
    }

    /// <summary>A numeric field's value, or its minimum while it is empty mid-edit.</summary>
    private static int Field(NumericUpDown field) => (int)Math.Round(field.Value ?? field.Minimum);

    private static string FirstLine(Exception exception) =>
        exception.Message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
        ?? "the operation failed";

    /// <summary>
    /// Renders the live canvas at its logical size scaled by the window's DPI and writes it as
    /// a PNG. The render target is kept between frames: allocating a fresh surface every tick
    /// costs more than the render itself and shows up directly in the recorded frame delays.
    /// </summary>
    private sealed class BoardCanvasFrameSink(BoardCanvas canvas) : IBoardFrameSink, IDisposable
    {
        private static readonly PngBitmapEncoderOptions PngOptions = new();

        private RenderTargetBitmap? _target;
        private PixelSize _targetPixels;
        private double _targetScaling;

        /// <summary>The pixel size a frame taken now would have.</summary>
        public PixelSize FramePixels()
        {
            var scaling = Scaling;
            var size = canvas.Bounds.Size;
            return new PixelSize(
                Math.Max(1, (int)Math.Round(size.Width * scaling)),
                Math.Max(1, (int)Math.Round(size.Height * scaling)));
        }

        public void Write(string path)
        {
            var target = Target(FramePixels(), Scaling);
            target.Render(canvas);
            target.Save(path, PngOptions);
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
        }

        private double Scaling => TopLevel.GetTopLevel(canvas)?.RenderScaling ?? 1;

        private RenderTargetBitmap Target(PixelSize pixels, double scaling)
        {
            if (_target is not null && _targetPixels == pixels && Math.Abs(_targetScaling - scaling) < double.Epsilon)
                return _target;

            _target?.Dispose();
            _target = new RenderTargetBitmap(pixels, new Vector(96 * scaling, 96 * scaling));
            _targetPixels = pixels;
            _targetScaling = scaling;
            return _target;
        }
    }
}
