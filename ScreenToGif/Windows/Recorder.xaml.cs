using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Events;
using ScreenToGif.Domain.Exceptions;
using ScreenToGif.Domain.Models;
using ScreenToGif.Model;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Native.Structs;
using ScreenToGif.Util;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;
using DpiChangedEventArgs = System.Windows.DpiChangedEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtons = ScreenToGif.Domain.Enums.MouseButtons;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows;

public partial class Recorder
{
    #region Variables

    /// <summary>
    /// The view model of the recorder.
    /// </summary>
    private readonly ScreenRecorderViewModel _viewModel;

    /// <summary>
    /// The region's left edge position.
    /// </summary>
    private int _left = 0;

    /// <summary>
    /// The region's top edge position.
    /// </summary>
    private int _top = 0;

    /// <summary>
    /// The region's width.
    /// </summary>
    private int _width = 0;

    /// <summary>
    /// The region's height.
    /// </summary>
    private int _height = 0;

    /// <summary>
    /// The object of the keyboard and mouse hooks.
    /// </summary>
    private readonly InputHook _actHook;

    /// <summary>
    /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
    /// </summary>
    private int _preStartCount = 1;

    /// <summary>
    /// The DPI of the current screen.
    /// </summary>
    private double _scale = 1;

    /// <summary>
    /// The last window handle saved.
    /// </summary>
    private IntPtr _lastHandle;

    #region Mouse cursor follow up

    /// <summary>
    /// The previous position of the cursor in the X axis.
    /// </summary>
    private int _prevPosX = 0;

    /// <summary>
    /// The previous position of the cursor in the Y axis.
    /// </summary>
    private int _prevPosY = 0;

    /// <summary>
    /// The latest position of the cursor in the X axis.
    /// </summary>
    private int _posX = 0;

    /// <summary>
    /// The latest position of the cursor in the Y axis.
    /// </summary>
    private int _posY = 0;

    /// <summary>
    /// The offset in pixels. Used for moving the recorder around the X axis.
    /// </summary>
    private double _offsetX = 0;

    /// <summary>
    /// The offset in pixels. Used for moving the recorder around the Y axis.
    /// </summary>
    private double _offsetY = 0;

    #endregion

    #endregion

    #region Timer

    private readonly Timer _preStartTimer = new Timer();

    private readonly Timer _followTimer = new Timer();
    private readonly Timer _showBorderTimer = new Timer();
    private readonly Timer _limitTimer = new Timer();

    #endregion


    public Recorder()
    {
        InitializeComponent();

        _preStartTimer.Tick += PreStart_Elapsed;
        _preStartTimer.Interval = 1000;

        #region Global Hook

        try
        {
            _actHook = new InputHook(true, true); //true for the mouse, true for the keyboard.
            _actHook.KeyDown += KeyHookTarget;
            _actHook.OnMouseActivity += MouseHookTarget;
        }
        catch (Exception) { }

        #endregion

        #region Model and commands

        DataContext = _viewModel = new ScreenRecorderViewModel();

        RegisterCommands();

        #endregion

        SystemEvents.PowerModeChanged += System_PowerModeChanged;
        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }


    #region Events

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        #region Adjust the position

        UpdateScreenDpi();

        _viewModel.IsDirectMode = UserSettings.All.UseDesktopDuplication;

        await UpdatePositioning(true);

        #endregion

        UpdateSize();
        UpdateLocation();

        DetectCaptureFrequency();
        DetectThinMode();
        AutoFitButtons(true);

        #region Timers

        _showBorderTimer.Interval = 500;
        _showBorderTimer.Tick += ShowBorderTimer_Tick;

        _followTimer.Tick += FollowTimer_Tick;

        #endregion

        CommandManager.InvalidateRequerySuggested();

        if (UserSettings.All.CursorFollowing)
            Follow();

        SizeChanged += Window_SizeChanged;
        DpiChanged += Window_DpiChanged;
        LocationChanged += Window_LocationChanged;

        //Automation arguments were passed by command line.
        if (Arguments.Open)
        {
            if (Arguments.FrequencyType.HasValue)
            {
                UserSettings.All.CaptureFrequency = Arguments.FrequencyType.Value;
                UserSettings.All.LatestFps = Arguments.Frequency;
                DetectCaptureFrequency();

                Arguments.FrequencyType = null;
            }

            if (Arguments.StartCapture && UserSettings.All.CaptureFrequency >= CaptureFrequencies.PerSecond)
            {
                if (Arguments.Limit > TimeSpan.Zero)
                {
                    _limitTimer.Tick += Limit_Elapsed;
                    _limitTimer.Interval = (int)Math.Min(int.MaxValue, Arguments.Limit.TotalMilliseconds);
                }

                await Record();
            }
            else
            {
                Arguments.ClearAutomationArgs();
            }
        }
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        if (IsFollowing && UserSettings.All.FollowShortcut == Key.None)
        {
            IsFollowing = false;

            Dialog.Ok(LocalizationHelper.Get("S.StartUp.Recorder"), LocalizationHelper.Get("S.Options.Warning.Follow.Header"),
                LocalizationHelper.Get("S.Options.Warning.Follow.Message"), Icons.Warning);
        }
    }

    private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
    {
        _scale = e.NewDpi.DpiScaleX;

        UpdateSize();
        UpdateLocation();

        WidthIntegerBox.Scale = _scale;
        HeightIntegerBox.Scale = _scale;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSize();
        AutoFitButtons();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var step = (Keyboard.Modifiers & ModifierKeys.Alt) != 0 ? 5 : 1;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (Stage == RecorderStages.Stopped)
        {
            //Control + Shift: Expand both ways.
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && (Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                switch (key)
                {
                    case Key.Up:
                        ResizeWindow(0, -step, 0, step);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        ResizeWindow(0, step, 0, -step);
                        e.Handled = true;
                        break;
                    case Key.Left:
                        ResizeWindow(step, 0, -step, 0);
                        e.Handled = true;
                        break;
                    case Key.Right:
                        ResizeWindow(-step, 0, step, 0);
                        e.Handled = true;
                        break;
                }

                return;
            }

            //If the Shift key is pressed, the sizing mode is enabled (bottom right).
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                switch (key)
                {
                    case Key.Left:
                        ResizeWindow(0, 0, -step, 0);
                        e.Handled = true;
                        break;
                    case Key.Up:
                        ResizeWindow(0, 0, 0, -step);
                        e.Handled = true;
                        break;
                    case Key.Right:
                        ResizeWindow(0, 0, step, 0);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        ResizeWindow(0, 0, 0, step);
                        e.Handled = true;
                        break;
                }

                return;
            }

            //If the Control key is pressed, the sizing mode is enabled (top left).
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                switch (key)
                {
                    case Key.Left:
                        ResizeWindow(-step, 0, 0, 0);
                        e.Handled = true;
                        break;
                    case Key.Up:
                        ResizeWindow(0, -step, 0, 0);
                        e.Handled = true;
                        break;
                    case Key.Right:
                        ResizeWindow(step, 0, 0, 0);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        ResizeWindow(0, step, 0, 0);
                        e.Handled = true;
                        break;
                }

                return;
            }
        }

        //If no other key is pressed, move the region.
        switch (key)
        {
            case Key.Left:
                MoveWindow(step, 0, 0, 0);
                e.Handled = true;
                break;
            case Key.Up:
                MoveWindow(0, step, 0, 0);
                e.Handled = true;
                break;
            case Key.Right:
                MoveWindow(0, 0, step, 0);
                e.Handled = true;
                break;
            case Key.Down:
                MoveWindow(0, 0, 0, step);
                e.Handled = true;
                break;
        }
    }

    private void CommandGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Mouse.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        UpdateLocation();
    }

    private void SizeIntegerBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!(sender is IntegerBox box))
            return;

        var screenPoint = box.PointToScreen(new Point(0, 0));
        var scale = this.Scale();

        User32.SetCursorPos((int)(screenPoint.X + (box.ActualWidth / 2) * scale), (int)(screenPoint.Y + (box.ActualHeight / 2) * scale));
    }

    private async void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            if (Stage == RecorderStages.Recording)
                Pause();
            else if (Stage == RecorderStages.PreStarting)
                await Stop();

            GC.Collect();
        }
    }

    private async void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs)
    {
        await UpdatePositioning();
    }

    private void SnapWindowButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        Mouse.Capture(this);

        Cursor = Cursors.Cross;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        //Save the settings
        UserSettings.All.RecorderTop = Top;
        UserSettings.All.RecorderLeft = Left;
        UserSettings.All.RecorderHeight = (int) Height;
        UserSettings.All.RecorderWidth = (int) Width;
        UserSettings.Save();

        #region Remove Hooks

        try
        {
            _actHook.OnMouseActivity -= MouseHookTarget;
            _actHook.KeyDown -= KeyHookTarget;
            _actHook.Stop(); //Stop the user activity watcher.
        }
        catch (Exception) { }

        #endregion

        SystemEvents.PowerModeChanged -= System_PowerModeChanged;
        SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

        #region Stops the timers

        if (Stage != RecorderStages.Stopped)
        {
            _preStartTimer.Stop();
            _preStartTimer.Dispose();

            await StopCapture();
        }

        //Garbage Collector Timer.
        GarbageTimer?.Stop();
        _followTimer?.Stop();

        #endregion

        //Clean all capture resources.
        if (Capture != null)
            await Capture.DisposeAsync();

        GC.Collect();
    }

    #endregion

    #region Hooks

    /// <summary>
    /// KeyHook event method. This fires when the user press a key.
    /// </summary>
    private async void KeyHookTarget(object sender, CustomKeyEventArgs e)
    {
        if (RegionSelectHelper.IsSelecting || Stage == RecorderStages.Discarding)
            return;

        //Capture when an user interactions happens.
        if (Stage == RecorderStages.Recording && UserSettings.All.CaptureFrequency == CaptureFrequencies.Interaction && !IsKeyboardFocusWithin)
            await Snap();

        //Record/snap or pause.
        if (Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
        {
            if (UserSettings.All.CaptureFrequency == CaptureFrequencies.Manual)
            {
                _viewModel.SnapCommand.Execute(null, this);
                return;
            }

            if (Stage == RecorderStages.Recording)
                _viewModel.PauseCommand.Execute(null, this);
            else
            {
                if (_viewModel.Region.IsEmpty && WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                _viewModel.RecordCommand.Execute(null, this);
            }

            return;
        }

        if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut && (Stage == RecorderStages.Recording || Stage == RecorderStages.Paused || Stage == RecorderStages.PreStarting))
            await Stop();
        else if (Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
            _viewModel.DiscardCommand.Execute(null, this);
        else if (Keyboard.Modifiers.HasFlag(UserSettings.All.FollowModifiers) && e.Key == UserSettings.All.FollowShortcut)
            UserSettings.All.CursorFollowing = IsFollowing = !IsFollowing;
        else
            KeyList.Add(new SimpleKeyGesture(e.Key, Keyboard.Modifiers, e.IsUppercase, e.IsInjected));
    }

    /// <summary>
    /// MouseHook event method, detects the mouse clicks.
    /// </summary>
    private async void MouseHookTarget(object sender, SimpleMouseGesture args)
    {
        try
        {
            if (RegionSelectHelper.IsSelecting || Stage == RecorderStages.Discarding)
                return;

            //In the future, store each mouse event, with a timestamp, independently of the capture.
            if (args.LeftButton == MouseButtonState.Pressed)
                RecordClicked = MouseButtons.Left;
            else if (args.RightButton == MouseButtonState.Pressed)
                RecordClicked = MouseButtons.Right;
            else if (args.MiddleButton == MouseButtonState.Pressed)
                RecordClicked = MouseButtons.Middle;
            else
                RecordClicked = MouseButtons.None;

            _posX = args.PosX;
            _posY = args.PosY;

            if (Stage == RecorderStages.Recording && args.IsInteraction && UserSettings.All.CaptureFrequency == CaptureFrequencies.Interaction)
            {
                var controlHit = VisualTreeHelper.HitTest(this, Mouse.GetPosition(this));

                if (controlHit == null)
                    await Snap();
            }
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Error in mouse hook target.");
        }

        if (WindowState == WindowState.Minimized || !IsMouseCaptured || Mouse.Captured == null)
            return;

        #region Get Handle and Window Rect

        var handle = User32.WindowFromPoint(new PointW { X = args.PosX, Y = args.PosY });
        var scale = this.Scale();

        if (_lastHandle != handle)
        {
            if (_lastHandle != IntPtr.Zero)
                Native.Helpers.Other.DrawFrame(_lastHandle, scale);

            _lastHandle = handle;
            Native.Helpers.Other.DrawFrame(handle, scale);
        }

        var rect = Native.Helpers.Windows.TrueWindowRectangle(handle);

        #endregion

        if (args.LeftButton == MouseButtonState.Pressed && Mouse.LeftButton == MouseButtonState.Pressed)
            return;

        #region Mouse Up

        Cursor = Cursors.Arrow;

        try
        {
            #region Try to get the process

            User32.GetWindowThreadProcessId(handle, out var id);
            var target = Process.GetProcesses().FirstOrDefault(p => p.Id == id);

            #endregion

            if (target is { ProcessName: "ScreenToGif" })
                return;

            //Clear up the selected window frame.
            Native.Helpers.Other.DrawFrame(handle, scale);
            _lastHandle = IntPtr.Zero;

            #region Values

            //TODO: Test values with other versions of windows.
            var top = (rect.Y / scale) - Constants.TopOffset + 0;
            var left = (rect.X / scale) - Constants.LeftOffset + 0;
            var height = ((rect.Height + 1) / scale) + Constants.TopOffset + Constants.BottomOffset - 1;
            var width = ((rect.Width + 1) / scale) + Constants.LeftOffset + Constants.RightOffset - 1;

            #endregion

            #region Validate

            if (top < SystemParameters.VirtualScreenTop)
                top = SystemParameters.VirtualScreenTop - 1;
            if (left < SystemParameters.VirtualScreenLeft)
                left = SystemParameters.VirtualScreenLeft - 1;
            if (SystemInformation.VirtualScreen.Height < (height + top) * scale) //TODO: Check if works with 2 screens.
                height = (SystemInformation.VirtualScreen.Height - top) / scale;
            if (SystemInformation.VirtualScreen.Width < (width + left) * scale)
                width = (SystemInformation.VirtualScreen.Width - left) / scale;

            #endregion

            Top = top;
            Left = left;
            Height = height;
            Width = width;
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error • Snap To Window");
        }
        finally
        {
            ReleaseMouseCapture();
        }

        #endregion
    }

    #endregion

    #region Timers

    private void PreStart_Elapsed(object sender, EventArgs e)
    {
        if (_preStartCount >= 1)
        {
            Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.PreStarting");
            DisplayTimer.SetElapsed(-_preStartCount);
            Splash.SetTime(-_preStartCount);
            _preStartCount--;
            return;
        }

        _preStartTimer.Stop();
        Title = "ScreenToGif";
        IsRecording = true;

        StartCapture();

        Stage = RecorderStages.Recording;
        AutoFitButtons();

        if (Arguments.StartCapture && Arguments.Limit > TimeSpan.Zero)
            _limitTimer.Start();
    }

    private void FollowTimer_Tick(object sender, EventArgs e)
    {
        if (WindowState != WindowState.Normal || _prevPosX == _posX && _prevPosY == _posY || Stage == RecorderStages.Paused || Stage == RecorderStages.Stopped || Stage == RecorderStages.Discarding ||
            (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers == UserSettings.All.DisableFollowModifiers))
            return;

        _prevPosX = _posX;
        _prevPosY = _posY;

        //TODO: Test with 2 monitors.
        //if (isCentered)
        //{
        //    //Hide the UI.
        //    _showBorderTimer.Stop();
        //    BeginStoryboard(this.FindStoryboard("HideRectangleStoryboard"), HandoffBehavior.SnapshotAndReplace);
        //    _showBorderTimer.Start();

        //    //_offsetX = _posX - Region.Width / 2d;
        //    //_offsetY = _posY - Region.Height / 2d;

        //    //Region = new Rect(new Point(_offsetX.Clamp(-1, Width - Region.Width + 1), _offsetY.Clamp(-1, Height - Region.Height + 1)), Region.Size);
        //    //DashedRectangle.Refresh();
        //}
        //else
        {
            if (_width < 1)
                UpdateSize();

            //Only move to the left if 'Mouse.X < Rect.L' and only move to the right if 'Mouse.X > Rect.R'
            _offsetX = _posX - UserSettings.All.FollowBuffer < _left ? _posX - _left - UserSettings.All.FollowBuffer : _posX + UserSettings.All.FollowBuffer > _left + _width ?
                _posX - (_left + _width) + UserSettings.All.FollowBuffer : 0;
            _offsetY = _posY - UserSettings.All.FollowBuffer < _top ? _posY - _top - UserSettings.All.FollowBuffer : _posY + UserSettings.All.FollowBuffer > _top + _height ?
                _posY - (_top + _height) + UserSettings.All.FollowBuffer : 0;

            //Hide the UI when moving.
            if (_posX - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < _left || _posX + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > _left + _width ||
                _posY - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < _top || _posY + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > _top + _height)
            {
                _showBorderTimer.Stop();
                HideInternals();
                BeginStoryboard(this.FindStoryboard("HideWindowStoryboard"), HandoffBehavior.Compose);
                _showBorderTimer.Start();
            }

            this.Refresh();
        }

        //Rearrange the window.
        Left = (Left + _offsetX / _scale).Clamp(0 - Constants.LeftOffset, SystemParameters.VirtualScreenWidth - Width + Constants.RightOffset);
        Top = (Top + _offsetY / _scale).Clamp(0 - Constants.TopOffset, SystemParameters.VirtualScreenHeight - Height + Constants.BottomOffset);
    }

    private void ShowBorderTimer_Tick(object sender, EventArgs e)
    {
        _showBorderTimer.Stop();

        this.Refresh();
        ShowInternals();

        BeginStoryboard(this.FindStoryboard("ShowWindowStoryboard"), HandoffBehavior.Compose);
    }

    private async void Limit_Elapsed(object sender, EventArgs e)
    {
        _limitTimer.Stop();

        if (!IsLoaded || (Stage != RecorderStages.Recording && Stage == RecorderStages.PreStarting))
            return;

        await Stop();
    }

    #endregion

    #region Methods

    private void RegisterCommands()
    {
        CommandBindings.Clear();
        CommandBindings.AddRange(new CommandBindingCollection
        {
            new CommandBinding(_viewModel.CloseCommand, (_, _) => Close(),
                (_, args) => args.CanExecute = Stage == RecorderStages.Stopped || (UserSettings.All.CaptureFrequency is CaptureFrequencies.Manual or CaptureFrequencies.Interaction && (Project == null || !Project.Any))),

            new CommandBinding(_viewModel.OptionsCommand, ShowOptions,
                (_, args) => args.CanExecute = (Stage != RecorderStages.Recording || UserSettings.All.CaptureFrequency is CaptureFrequencies.Manual or CaptureFrequencies.Interaction) && Stage != RecorderStages.PreStarting),

            new CommandBinding(_viewModel.SnapToWindowCommand, null,
                (_, args) => args.CanExecute = Stage == RecorderStages.Stopped || (Stage == RecorderStages.Recording && UserSettings.All.CaptureFrequency is CaptureFrequencies.Manual or CaptureFrequencies.Interaction && (Project == null || Project.Frames.Count == 0))),

            new CommandBinding(_viewModel.SwitchFrequencyCommand, SwitchFrequency,
                (_, args) =>
                {
                    if (args.Parameter != null && !args.Parameter.Equals("Switch"))
                    {
                        args.CanExecute = true;
                        return;
                    }

                    args.CanExecute = ((Stage != RecorderStages.Recording || Project == null) || UserSettings.All.CaptureFrequency is CaptureFrequencies.Manual or CaptureFrequencies.Interaction) && Stage != RecorderStages.PreStarting;
                }),

            new CommandBinding(_viewModel.RecordCommand, async (_, _) => await Record(),
                (_, args) => args.CanExecute = Stage is RecorderStages.Stopped or RecorderStages.Paused && UserSettings.All.CaptureFrequency != CaptureFrequencies.Manual),

            new CommandBinding(_viewModel.PauseCommand, (_, _) => Pause(),
                (_, args) => args.CanExecute = Stage == RecorderStages.Recording && UserSettings.All.CaptureFrequency != CaptureFrequencies.Manual),

            new CommandBinding(_viewModel.SnapCommand, async (_, _) => await Snap(),
                (_, args) => args.CanExecute = Stage == RecorderStages.Recording && UserSettings.All.CaptureFrequency == CaptureFrequencies.Manual),

            new CommandBinding(_viewModel.StopLargeCommand, async (_, _) => await Stop(),
                (_, args) => args.CanExecute = (Stage == RecorderStages.Recording && UserSettings.All.CaptureFrequency != CaptureFrequencies.Manual && UserSettings.All.CaptureFrequency != CaptureFrequencies.Interaction &&
                    !UserSettings.All.RecorderDisplayDiscard) || Stage == RecorderStages.PreStarting),

            new CommandBinding(_viewModel.StopCommand, async (_, _) => await Stop(),
                (_, args) =>
                {
                    args.CanExecute = Stage == RecorderStages.Recording &&
                        ((UserSettings.All.CaptureFrequency != CaptureFrequencies.Manual && UserSettings.All.CaptureFrequency != CaptureFrequencies.Interaction && !UserSettings.All.RecorderDisplayDiscard) || FrameCount > 0) || Stage == RecorderStages.Paused;
                }),

            new CommandBinding(_viewModel.DiscardCommand, async (_, _) => await Discard(),
                (_, args) => args.CanExecute = (Stage == RecorderStages.Paused && FrameCount > 0) || (Stage == RecorderStages.Recording && (UserSettings.All.CaptureFrequency is CaptureFrequencies.Manual or CaptureFrequencies.Interaction || UserSettings.All.RecorderDisplayDiscard) && FrameCount > 0)),
        });

        _viewModel.RefreshKeyGestures();
    }

    private void ShowOptions(object sender, ExecutedRoutedEventArgs e)
    {
        Topmost = false;

        var options = new Options(Options.RecorderIndex);
        options.ShowDialog();

        DetectCaptureFrequency();
        RegisterCommands();
        DetectThinMode();

        //If not recording (or recording in manual/interactive mode, but with no frames captured yet), adjust the maximum bounds for the recorder.
        if (Stage == RecorderStages.Stopped || ((UserSettings.All.CaptureFrequency == CaptureFrequencies.Manual || UserSettings.All.CaptureFrequency == CaptureFrequencies.Interaction) && Stage == RecorderStages.Recording && FrameCount == 0))
            _viewModel.IsDirectMode = UserSettings.All.UseDesktopDuplication;

        Topmost = true;
    }

    private void SwitchFrequency(object sender, ExecutedRoutedEventArgs e)
    {
        //When this event is fired from clicking on the switch button.
        if (e.Parameter?.Equals("Switch") == true)
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequencies.Manual:
                    UserSettings.All.CaptureFrequency = CaptureFrequencies.Interaction;
                    break;

                case CaptureFrequencies.Interaction:
                    UserSettings.All.CaptureFrequency = CaptureFrequencies.PerSecond;
                    break;

                case CaptureFrequencies.PerSecond:
                    UserSettings.All.CaptureFrequency = CaptureFrequencies.PerMinute;
                    break;

                case CaptureFrequencies.PerMinute:
                    UserSettings.All.CaptureFrequency = CaptureFrequencies.PerHour;
                    break;

                default: //PerHour.
                    UserSettings.All.CaptureFrequency = CaptureFrequencies.Manual;
                    break;
            }
        }

        //When event is fired when the frequency is picked from the context menu, just switch the labels.
        DetectCaptureFrequency();
        AutoFitButtons();
    }

    /// <summary>
    /// Method that starts or pauses the recording
    /// </summary>
    internal async Task Record()
    {
        try
        {
            switch (Stage)
            {
                case RecorderStages.Stopped:

                    #region If interaction mode

                    if (UserSettings.All.CaptureFrequency == CaptureFrequencies.Interaction)
                    {
                        Stage = RecorderStages.Recording;
                        SetTaskbarButtonOverlay();
                        return;
                    }

                    #endregion

                    #region To record

                    Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                    KeyList.Clear();
                    FrameCount = 0;

                    await Task.Factory.StartNew(UpdateScreenDpi);
                    await PrepareCapture();
                    HideGuidelines();

                    HeightIntegerBox.IsEnabled = false;
                    WidthIntegerBox.IsEnabled = false;
                    FrequencyIntegerUpDown.IsEnabled = false;

                    IsRecording = true;
                    Topmost = true;

                    #region Start

                    if (UserSettings.All.UsePreStart)
                    {
                        Stage = RecorderStages.PreStarting;

                        Title = $"ScreenToGif ({LocalizationHelper.Get("S.Recorder.PreStart")} {UserSettings.All.PreStartValue}s)";
                        DisplayTimer.SetElapsed(-UserSettings.All.PreStartValue);

                        _preStartCount = UserSettings.All.PreStartValue - 1;
                        _preStartTimer.Start();
                        return;
                    }

                    StartCapture();

                    Stage = RecorderStages.Recording;
                    AutoFitButtons();
                    SetTaskbarButtonOverlay();

                    if (Arguments.StartCapture && Arguments.Limit > TimeSpan.Zero)
                        _limitTimer.Start();

                    break;

                #endregion

                #endregion

                case RecorderStages.Paused:

                    #region To record again

                    Stage = RecorderStages.Recording;
                    Title = "ScreenToGif";

                    SetTaskbarButtonOverlay();
                    await PrepareCapture(false);
                    HideGuidelines();
                    AutoFitButtons();

                    //If it's interaction mode, the capture is done via Snap().
                    if (UserSettings.All.CaptureFrequency == CaptureFrequencies.Interaction)
                        return;

                    FrequencyIntegerUpDown.IsEnabled = false;

                    StartCapture();
                    break;

                #endregion
            }
        }
        catch (GraphicsConfigurationException g)
        {
            LogWriter.Log(g, "Impossible to start the recording due to wrong graphics adapter.");
            GraphicsConfigurationDialog.Ok(g, _viewModel.CurrentMonitor);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to start the recording.");
            ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.StartPauseNotPossible"), e.Message, e);
        }
        finally
        {
            Arguments.ClearAutomationArgs();

            //Wait a bit, then refresh the commands. Some of the commands are dependent of the FrameCount property.
            await Task.Delay(TimeSpan.FromMilliseconds(GetCaptureInterval() + 200));

            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Capture a single frame.
    /// </summary>
    private async Task Snap()
    {
        var snapTriggerDelay = GetTriggerDelay();

        if (snapTriggerDelay != 0)
            await Task.Delay(snapTriggerDelay);

        HideGuidelines();

        if (Project == null || Project.Frames.Count == 0)
        {
            try
            {
                Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                await PrepareCapture();

                IsRecording = true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to start the screencasting.");
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), ex.Message, ex);
                return;
            }
        }

        #region Take the screenshot

        try
        {
            var limit = 0;
            do
            {
                FrameCount = await Capture.ManualCaptureAsync(new FrameInfo(RecordClicked, KeyList), UserSettings.All.ShowCursor);

                if (limit > 5)
                    throw new Exception("Impossible to capture the manual screenshot.");

                limit++;
            }
            while (FrameCount == 0);

            KeyList.Clear();

            DisplayTimer.ManuallyCapturedCount++;
            CommandManager.InvalidateRequerySuggested();
        }
        catch (GraphicsConfigurationException g)
        {
            IsRecording = false;

            LogWriter.Log(g, "Impossible to start the recording due to wrong graphics adapter.");
            GraphicsConfigurationDialog.Ok(g, _viewModel.CurrentMonitor);
        }
        catch (Exception e)
        {
            IsRecording = false;

            LogWriter.Log(e, "Impossible to capture the manual screenshot.");
            ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible.Info"), e);
        }

        #endregion
    }

    internal void Pause()
    {
        try
        {
            if (Stage != RecorderStages.Recording || UserSettings.All.CaptureFrequency is CaptureFrequencies.Manual or CaptureFrequencies.Interaction)
                return;

            Stage = RecorderStages.Paused;
            Title = "ScreenToGif";

            PauseCapture();
            _limitTimer.Stop();

            FrequencyIntegerUpDown.IsEnabled = true;
            DisplayGuidelines();
            AutoFitButtons();
            SetTaskbarButtonOverlay();
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to pause the recording.");
            ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.StartPauseNotPossible"), e.Message, e);
        }
    }

    /// <summary>
    /// Stops the recording or the Pre-Start countdown.
    /// </summary>
    private async Task Stop()
    {
        try
        {
            ControlStackPanel.IsEnabled = false;
            Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.Stopping");
            Cursor = Cursors.AppStarting;

            _limitTimer.Stop();
            await StopCapture();

            if (Stage is RecorderStages.Recording or RecorderStages.Paused && Project?.Any == true)
            {
                #region Finishes if it's recording and it has any frames

                await Task.Delay(100);

                Close();
                return;

                #endregion
            }

            #region Stops if it is not recording, or has no frames

            //Stop the pre-start timer to kill pre-start warming up.
            if (Stage == RecorderStages.PreStarting)
                _preStartTimer.Stop();

            Stage = RecorderStages.Stopped;

            //Enables the controls that are disabled while recording;
            FrequencyIntegerUpDown.IsEnabled = true;
            ControlStackPanel.IsEnabled = true;
            HeightIntegerBox.IsEnabled = true;
            WidthIntegerBox.IsEnabled = true;

            IsRecording = false;
            Topmost = true;

            AutoFitButtons();
            DisplayGuidelines();
            SetTaskbarButtonOverlay();

            #endregion
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error on the Stop function");

            ErrorDialog.Ok("ScreenToGif", "Error while stopping", ex.Message, ex);
        }
        finally
        {
            if (IsLoaded)
            {
                Title = "ScreenToGif";
                Cursor = Cursors.Arrow;
                ControlStackPanel.IsEnabled = true;
            }
        }
    }

    private async Task Discard()
    {
        Pause();

        if (UserSettings.All.NotifyRecordingDiscard && !Dialog.Ask(LocalizationHelper.Get("S.Recorder.Discard.Title"),
                LocalizationHelper.Get("S.Recorder.Discard.Instruction"), LocalizationHelper.Get("S.Recorder.Discard.Message"), false))
            return;

        await StopCapture();

        FrameCount = 0;
        Stage = RecorderStages.Discarding;
        OutterGrid.IsEnabled = false;
        Cursor = Cursors.AppStarting;
        SetTaskbarButtonOverlay();

        await Task.Run(() =>
        {
            try
            {
                #region Remove all the files

                //Not sure if needed.
                foreach (var frame in Project.Frames)
                {
                    try
                    {
                        if (File.Exists(frame.Path))
                            File.Delete(frame.Path);
                    }
                    catch (Exception)
                    { }
                }

                try
                {
                    Directory.Delete(Project.FullPath, true);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Delete temp path");
                }

                #endregion

                Project.Frames.Clear();
            }
            catch (IOException io)
            {
                LogWriter.Log(io, "Error while trying to discard the recording");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
                LogWriter.Log(ex, "Error while trying to discard the recording");
            }
        });

        //Enables the controls that are disabled while recording;
        FrequencyIntegerUpDown.IsEnabled = true;
        HeightIntegerBox.IsEnabled = true;
        WidthIntegerBox.IsEnabled = true;
        OutterGrid.IsEnabled = true;

        Title = "ScreenToGif";
        Cursor = Cursors.Arrow;
        IsRecording = false;

        DetectCaptureFrequency();
        AutoFitButtons();
        SetTaskbarButtonOverlay();
    }

    /// <summary>
    /// Changes the way that the Record and Stop buttons are shown.
    /// </summary>
    private void AutoFitButtons(bool force = false)
    {
        if (LowerGrid.ActualWidth < 360)
        {
            if (MinimizeVisibility == Visibility.Collapsed && !force)
                return;

            _viewModel.ButtonStyle = (Style)FindResource("Style.Button.NoText");

            MinimizeVisibility = Visibility.Collapsed;

            if (IsThin)
                CaptionText.Visibility = Visibility.Collapsed;
        }
        else
        {
            if (MinimizeVisibility == Visibility.Visible && !force)
                return;

            _viewModel.ButtonStyle = (Style)FindResource("Style.Button.Horizontal");

            MinimizeVisibility = Visibility.Visible;

            if (IsThin)
                CaptionText.Visibility = Visibility.Visible;
        }
    }


    private void UpdateScreenDpi()
    {
        try
        {
            var source = Dispatcher.Invoke(() => PresentationSource.FromVisual(this));

            if (source?.CompositionTarget != null)
                _scale = Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);

            Dispatcher.Invoke(() =>
            {
                if (_viewModel.Monitors?.Any() == true)
                {
                    UpdateSize();
                    UpdateLocation();
                }

                WidthIntegerBox.Scale = _scale;
                HeightIntegerBox.Scale = _scale;
            });
        }
        finally
        {
            GC.Collect(1);
        }
    }

    private void UpdateSize()
    {
        //If minimized, assume that the position is the same.
        if (WindowState != WindowState.Minimized)
        {
            _width = (int)Math.Round((Width - Constants.HorizontalOffset) * _scale);
            _height = (int)Math.Round((Height - Constants.VerticalOffset) * _scale);
        }

        _viewModel.Region = new Rect(_viewModel.Region.TopLeft, new Size(_width, _height));

        if (Capture != null)
        {
            Capture.Width = _width;
            Capture.Height = _height;
        }

        DetectMonitorChanges(true);
    }

    private void UpdateLocation()
    {
        UserSettings.All.RecorderLeft = _left = (int)Math.Round((Math.Round(Left, MidpointRounding.AwayFromZero) + Constants.LeftOffset) * _scale);
        UserSettings.All.RecorderTop = _top = (int)Math.Round((Math.Round(Top, MidpointRounding.AwayFromZero) + Constants.TopOffset) * _scale);

        _viewModel.Region = new Rect(new Point(_left, _top), _viewModel.Region.BottomRight);

        if (Capture == null)
            return;

        Capture.Left = _left;
        Capture.Top = _top;

        DetectMonitorChanges(true);
    }

    private async void DetectMonitorChanges(bool detectCurrent = false)
    {
        if (detectCurrent)
        {
            var interop = new System.Windows.Interop.WindowInteropHelper(this);
            var current = Screen.FromHandle(interop.Handle);

            _viewModel.CurrentMonitor = _viewModel.Monitors.FirstOrDefault(f => f.Name == current.DeviceName);
        }

        if (_viewModel.CurrentMonitor != null && _viewModel.CurrentMonitor.Handle != _viewModel.PreviousMonitor?.Handle)
        {
            if (_viewModel.PreviousMonitor != null && Stage == RecorderStages.Recording && Project?.Any == true)
            {
                Pause();

                Capture.DeviceName = _viewModel.CurrentMonitor.Name;
                Capture?.ResetConfiguration();

                await Record();
            }

            _viewModel.PreviousMonitor = _viewModel.CurrentMonitor;
        }
    }

    private bool UpdatePositioning2(bool startup = false)
    {
        try
        {
            var top = UserSettings.All.RecorderTop;
            var left = UserSettings.All.RecorderLeft;

            //If the position was never set.
            if (double.IsNaN(top) || double.IsNaN(left))
            {
                //Let it center on screen when the window is loading.
                if (startup)
                    return false;

                //Let the code below decide where to position the screen.
                top = 0;
                left = 0;
            }

            //The catch here is to get the closest monitor from current Top/Left point.
            var monitors = MonitorHelper.AllMonitorsGranular();
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

            if (closest == null)
                return false;

            //To much to the Left.
            if (closest.WorkingArea.Left > UserSettings.All.RecorderLeft + UserSettings.All.RecorderWidth - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > UserSettings.All.RecorderTop + UserSettings.All.RecorderHeight - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < UserSettings.All.RecorderLeft + 100)
                left = closest.WorkingArea.Right - UserSettings.All.RecorderWidth;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < UserSettings.All.RecorderTop + 100)
                top = closest.WorkingArea.Bottom - UserSettings.All.RecorderHeight;

            Top = UserSettings.All.RecorderTop = top;
            Left = UserSettings.All.RecorderLeft = left;

            return true;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to position the recorder window.");
            return false;
        }
        finally
        {
            DetectMonitorChanges(true);
        }
    }

    private async Task UpdatePositioning(bool startup = false)
    {
        if (!startup)
        {
            switch (Stage)
            {
                case RecorderStages.PreStarting:
                {
                    await Stop();
                    break;
                }
                case RecorderStages.Recording:
                {
                    if (UserSettings.All.CaptureFrequency != CaptureFrequencies.Manual)
                        Pause();

                    break;
                }
            }
        }
        else
        {
            //The user can opt out of the using the previous position and size.
            if (!UserSettings.All.RecorderRememberPosition)
            {
                UserSettings.All.RecorderLeft = double.NaN;
                UserSettings.All.RecorderTop = double.NaN;

                if (!UserSettings.All.RecorderRememberSize)
                {
                    UserSettings.All.RecorderWidth = 518;
                    UserSettings.All.RecorderHeight = 269;
                }
            }

            //Command line arguments were sent.
            if (Arguments.Region != Rect.Empty)
            {
                UserSettings.All.RecorderLeft = Arguments.Region.Left - Constants.LeftOffset;
                UserSettings.All.RecorderTop = Arguments.Region.Top - Constants.TopOffset;
                UserSettings.All.RecorderWidth = (int)Arguments.Region.Width + Constants.HorizontalOffset;
                UserSettings.All.RecorderHeight = (int)Arguments.Region.Height + Constants.VerticalOffset;
                Arguments.Region = Rect.Empty;
            }
        }

        //Since the list of monitors could have been changed, it needs to be queried again.
        _viewModel.Monitors = MonitorHelper.AllMonitorsGranular();

        //Detect closest screen to the point (previously selected top/left point or current mouse coordinate).
        var point = startup ? (double.IsNaN(UserSettings.All.RecorderTop) || double.IsNaN(UserSettings.All.RecorderLeft) ?
            Native.Helpers.Other.GetMousePosition(_scale, Left, Top) : new Point((int)UserSettings.All.RecorderLeft, (int)UserSettings.All.RecorderTop)) : new Point((int) Left, (int) Top);
        var closest = _viewModel.Monitors.FirstOrDefault(x => x.Bounds.Contains(point)) ?? _viewModel.Monitors.FirstOrDefault(x => x.IsPrimary) ?? _viewModel.Monitors.FirstOrDefault();

        if (closest == null)
            throw new Exception("It was not possible to get a list of known screens.");

        //Move the window to the correct location.
        var left = UserSettings.All.RecorderLeft;
        var top = UserSettings.All.RecorderTop;

        if (double.IsNaN(UserSettings.All.RecorderTop) || double.IsNaN(UserSettings.All.RecorderLeft))
        {
            left = closest.WorkingArea.Left + closest.WorkingArea.Width / 2d - ActualWidth / 2d;
            top = closest.WorkingArea.Top + closest.WorkingArea.Height / 2d - ActualHeight / 2d;
        }
        else
        {
            //To much to the Left.
            if (closest.WorkingArea.Left > UserSettings.All.RecorderLeft + UserSettings.All.RecorderWidth - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > UserSettings.All.RecorderTop + UserSettings.All.RecorderHeight - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < UserSettings.All.RecorderLeft + 100)
                left = closest.WorkingArea.Right - UserSettings.All.RecorderWidth;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < UserSettings.All.RecorderTop + 100)
                top = closest.WorkingArea.Bottom - UserSettings.All.RecorderHeight;
        }

        Top = closest.NativeBounds.Top + 1;
        Left = closest.NativeBounds.Left + 1;

        var diff = this.Scale() / closest.Scale;
        Top = UserSettings.All.RecorderTop = top / diff;
        Left = UserSettings.All.RecorderLeft = left / diff;
        Height = UserSettings.All.RecorderHeight;
        Width = UserSettings.All.RecorderWidth;

        //After moving, detect the current monitor using a more reliable method.
        var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
        var current = Screen.FromHandle(windowInteropHelper.Handle);

        _viewModel.CurrentMonitor =  _viewModel.Monitors.FirstOrDefault(f => f.Name == current.DeviceName) ?? closest;

        var regionLeft = (int)Math.Round((Math.Round(Left, MidpointRounding.AwayFromZero) + Constants.LeftOffset) * _viewModel.CurrentMonitor.Scale);
        var regionTop = (int)Math.Round((Math.Round(Top, MidpointRounding.AwayFromZero) + Constants.TopOffset) * _viewModel.CurrentMonitor.Scale);
        var regionWidth = (int)Math.Round((UserSettings.All.RecorderWidth - Constants.HorizontalOffset) * _viewModel.CurrentMonitor.Scale);
        var regionHeight = (int)Math.Round((UserSettings.All.RecorderHeight - Constants.VerticalOffset) * _viewModel.CurrentMonitor.Scale);

        if (regionWidth < 0 || regionHeight < 0)
        {
            var desc = $"Scale: {this.Scale()}\n\nScreen: {closest.AdapterName}\nBounds: {closest.Bounds}\n\nTopLeft: {top}x{left}\nWidthHeight: {regionWidth}x{regionHeight}";

            LogWriter.Log("Wrong recorder window sizing", desc);

            Height = UserSettings.All.RecorderHeight = 500;
            Width = UserSettings.All.RecorderWidth = 250;
            return;
        }

        _viewModel.Region = new Rect(regionLeft, regionTop, regionWidth, regionHeight);
    }

    internal override void OnFollowingChanged()
    {
        Follow();

        base.OnFollowingChanged();
    }

    private void Follow()
    {
        if (IsFollowing && UserSettings.All.FollowShortcut != Key.None)
        {
            _followTimer.Interval = (1000 / UserSettings.All.LatestFps) / 2;
            _followTimer.Start();
            return;
        }

        _followTimer.Stop();
    }

    private async Task PrepareCapture(bool isNew = true)
    {
        if (isNew && Capture != null)
        {
            await Capture.DisposeAsync();
            Capture = null;
        }

        //If the capture helper was initialized already, ignore this.
        if (Capture != null)
            return;

        if (UserSettings.All.UseDesktopDuplication)
        {
            //Check if Windows 8 or newer.
            if (!OperationalSystemHelper.IsWin8OrHigher())
                throw new Exception(LocalizationHelper.Get("S.Recorder.Warning.Windows8"));

            Capture = GetDirectCapture();
            Capture.DeviceName = _viewModel.CurrentMonitor.Name;
            _viewModel.IsDirectMode = true;
        }
        else
        {
            //Capture with BitBlt.
            Capture = UserSettings.All.UseMemoryCache ? new CachedCapture() : new ImageCapture();
            _viewModel.IsDirectMode = true;
        }

        Capture.OnError += exception =>
        {
            Dispatcher?.Invoke(() =>
            {
                //Pause the recording and show the error.
                Pause();

                if (exception is GraphicsConfigurationException)
                    GraphicsConfigurationDialog.Ok(exception, _viewModel.CurrentMonitor);
                else
                    ErrorDialog.Ok("ScreenToGif", LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), exception.Message, exception);

                Capture.Dispose();
                Capture = null;
            });
        };

        var dpi = MonitorHelper.AllMonitors.FirstOrDefault(f => f.IsPrimary)?.Dpi ?? 96d;

        Capture.Start(GetCaptureInterval(), _left, _top, _width, _height, dpi / 96d, Project);
    }

    private void DetectCaptureFrequency()
    {
        switch (UserSettings.All.CaptureFrequency)
        {
            case CaptureFrequencies.Manual:
                FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Manual.Short");
                FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                FrequencyViewbox.Visibility = Visibility.Collapsed;
                AdjustToManual();
                break;
            case CaptureFrequencies.Interaction:
                FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Interaction.Short");
                FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                FrequencyViewbox.Visibility = Visibility.Collapsed;
                AdjustToInteraction();
                break;
            case CaptureFrequencies.PerSecond:
                FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Fps.Short");
                FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fps");
                FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fps.Range");
                FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                FrequencyViewbox.Visibility = Visibility.Visible;
                AdjustToAutomatic();
                break;

            case CaptureFrequencies.PerMinute:
                FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Fpm.Short");
                FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm");
                FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm.Range");
                FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                FrequencyViewbox.Visibility = Visibility.Visible;
                AdjustToAutomatic();
                break;

            default: //PerHour.
                FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Fph.Short");
                FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fph");
                FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fph.Range");
                FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                FrequencyViewbox.Visibility = Visibility.Visible;
                AdjustToAutomatic();
                break;
        }

        CommandManager.InvalidateRequerySuggested();
    }

    private void AdjustToManual()
    {
        Stage = RecorderStages.Recording;
        Title = "ScreenToGif";
        FrameRate.Start(HasFixedDelay(), GetFixedDelay());

        AutoFitButtons();
        DisplayGuidelines();
    }

    private void AdjustToInteraction()
    {
        Stage = Project?.Frames?.Count > 0 ? RecorderStages.Paused : RecorderStages.Stopped;
        Title = "ScreenToGif";
        FrameRate.Start(HasFixedDelay(), GetFixedDelay());

        AutoFitButtons();
        DisplayGuidelines();
    }

    private void AdjustToAutomatic()
    {
        Stage = Project?.Frames?.Count > 0 ? RecorderStages.Paused : RecorderStages.Stopped;
        Title = "ScreenToGif";
        FrameRate.Stop();

        AutoFitButtons();
        DisplayGuidelines();
    }

    internal override void StartCapture()
    {
        DisplayTimer.Start();

        base.StartCapture();
    }

    internal override void PauseCapture()
    {
        DisplayTimer.Pause();

        base.PauseCapture();
    }

    internal override async Task StopCapture()
    {
        DisplayTimer.Stop();

        await base.StopCapture();
    }

    private void DetectThinMode()
    {
        //Updates the Offsets of the two controls (because it's a static property, it will not update by itself).
        HeightIntegerBox.Offset = Constants.VerticalOffset;
        WidthIntegerBox.Offset = Constants.HorizontalOffset;
    }

    private void MoveWindow(int left, int top, int right, int bottom)
    {
        Left = left > 0 ? Math.Max(Left - left, _viewModel.MaximumBounds.Left - Constants.LeftOffset) : right > 0 ? Math.Min(Left + right, _viewModel.MaximumBounds.Right - Width + Constants.RightOffset) : Left;
        Top = top > 0 ? Math.Max(Top - top, _viewModel.MaximumBounds.Top - Constants.TopOffset) : bottom > 0 ? Math.Min(Top + bottom, _viewModel.MaximumBounds.Bottom - Height + Constants.BottomOffset) : Top;

        DetectMonitorChanges(true);
    }

    private void ResizeWindow(int left, int top, int right, int bottom)
    {
        var newLeft = left < 0 ? Math.Max(Left + left, _viewModel.MaximumBounds.Left - Constants.LeftOffset) : left > 0 ? Left + left : Left;
        var newTop = top < 0 ? Math.Max(Top + top, _viewModel.MaximumBounds.Top - Constants.TopOffset) : top > 0 ? Top + top : Top;
        var width = (right > 0 ? Math.Min(Width + right, _viewModel.MaximumBounds.Right - Left + Constants.RightOffset) - left : right < 0 ? Width + right + (left > 0 ? -left : 0) : Width - (newLeft - Left));
        var height = (bottom > 0 ? Math.Min(Height + bottom, _viewModel.MaximumBounds.Bottom - Top + Constants.BottomOffset) - top : bottom < 0 ? Height + bottom + (top > 0 ? -top : 0) : Height - (newTop - Top));

        //Ignore input if the new size will be smaller than the minimum.
        if ((height < 25 && (top > 0 || bottom < 0)) || (width < 25 && (left > 0 || right < 0)))
            return;

        Left = newLeft;
        Top = newTop;
        Width = width;
        Height = height;

        DetectMonitorChanges(true);
    }

    private void SetTaskbarButtonOverlay()
    {
        try
        {
            switch (Stage)
            {
                case RecorderStages.Stopped:
                    TaskbarItemInfo.Overlay = null;
                    return;
                case RecorderStages.Recording:
                    if (UserSettings.All.CaptureFrequency != CaptureFrequencies.Manual)
                        TaskbarItemInfo.Overlay = new DrawingImage((FindResource("Vector.Record") as DrawingBrush)?.Drawing);
                    else
                        TaskbarItemInfo.Overlay = null;
                    return;
                case RecorderStages.Paused:
                    TaskbarItemInfo.Overlay = new DrawingImage((FindResource("Vector.Pause") as DrawingBrush)?.Drawing);
                    return;
                case RecorderStages.Discarding:
                    TaskbarItemInfo.Overlay = new DrawingImage((FindResource("Vector.Remove") as DrawingBrush)?.Drawing);
                    return;
            }
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to set the taskbar button overlay");
        }
    }

    private void DisplayGuidelines()
    {
        GuidelinesGrid.Visibility = Visibility.Visible;
    }

    private void HideGuidelines()
    {
        GuidelinesGrid.Visibility = Visibility.Collapsed;
        Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
    }

    #endregion
}