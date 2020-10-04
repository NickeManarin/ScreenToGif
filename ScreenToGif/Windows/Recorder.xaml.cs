using System;
using System.Collections.Generic;
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
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.InputHook;
using ScreenToGif.ViewModel;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;
using DpiChangedEventArgs = System.Windows.DpiChangedEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Monitor = ScreenToGif.Native.Monitor;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
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
        /// Deals with all screen capture methods.
        /// </summary>
        private ICapture _capture;


        private Task<int> _captureTask;

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

        /// <summary>
        /// True when the user stop the recording. 
        /// </summary>
        private bool _stopRequested;

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The last window handle saved.
        /// </summary>
        private IntPtr _lastHandle;

        #region Flags

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        #endregion

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

        private Timer _captureTimer = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();

        private readonly Timer _preStartTimer = new Timer();

        private readonly Timer _followTimer = new Timer();
        private readonly Timer _showBorderTimer = new Timer();

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

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

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

            if (Stage == Stage.Stopped)
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

            Util.Native.SetCursorPos((int)(screenPoint.X + (box.ActualWidth / 2) * scale), (int)(screenPoint.Y + (box.ActualHeight / 2) * scale));
        }

        private async void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    Pause();
                else if (Stage == Stage.PreStarting)
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

            if (Stage != Stage.Stopped)
            {
                _preStartTimer.Stop();
                _preStartTimer.Dispose();

                _captureTimer.Stop();
                _captureTimer.Dispose();
            }

            //Garbage Collector Timer.
            _garbageTimer?.Stop();
            _followTimer?.Stop();

            #endregion

            //Clean all capture resources.
            if (_capture != null)
                await _capture.Dispose();

            GC.Collect();
        }

        #endregion
        
        #region Hooks

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private async void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (RegionSelectHelper.IsSelecting || Stage == Stage.Discarding)
                return;

            //Capture when an user interactions happens.
            if (Stage == Stage.Recording && UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction && !IsKeyboardFocusWithin)
                await Snap();

            //Record/snap or pause.
            if (Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
            {
                if (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual)
                {
                    _viewModel.SnapCommand.Execute(null, this);
                    return;
                }

                if (Stage == Stage.Recording)
                    _viewModel.PauseCommand.Execute(null, this);
                else
                {
                    if (_viewModel.Region.IsEmpty && WindowState == WindowState.Minimized)
                        WindowState = WindowState.Normal;

                    _viewModel.RecordCommand.Execute(null, this);
                }

                return;
            }

            if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut && (Stage == Stage.Recording || Stage == Stage.Paused || Stage == Stage.PreStarting))
                await Stop();
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
                _viewModel.DiscardCommand.Execute(null, this);
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.FollowModifiers) && e.Key == UserSettings.All.FollowShortcut)
                UserSettings.All.CursorFollowing = IsFollowing = !IsFollowing;
            else
                _keyList.Add(new SimpleKeyGesture(e.Key, Keyboard.Modifiers, e.IsUppercase, e.IsInjected));
        }

        /// <summary>
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private async void MouseHookTarget(object sender, SimpleMouseGesture args)
        {
            try
            {
                if (RegionSelectHelper.IsSelecting || Stage == Stage.Discarding)
                    return;

                //In the future, store each mouse event, with a timestamp, independently of the capture.
                _recordClicked = args.LeftButton == MouseButtonState.Pressed || args.RightButton == MouseButtonState.Pressed || args.MiddleButton == MouseButtonState.Pressed;

                _posX = args.PosX;
                _posY = args.PosY;

                if (Stage == Stage.Recording && args.IsInteraction && UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction)
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

            var handle = Util.Native.WindowFromPoint(new Util.Native.PointW { X = args.PosX, Y = args.PosY });
            var scale = this.Scale();

            if (_lastHandle != handle)
            {
                if (_lastHandle != IntPtr.Zero)
                    Util.Native.DrawFrame(_lastHandle, scale);

                _lastHandle = handle;
                Util.Native.DrawFrame(handle, scale);
            }

            var rect = Util.Native.TrueWindowRectangle(handle);

            #endregion

            if (args.LeftButton == MouseButtonState.Pressed && Mouse.LeftButton == MouseButtonState.Pressed)
                return;

            #region Mouse Up

            Cursor = Cursors.Arrow;

            try
            {
                #region Try to get the process

                Util.Native.GetWindowThreadProcessId(handle, out var id);
                var target = Process.GetProcesses().FirstOrDefault(p => p.Id == id);

                #endregion

                if (target != null && target.ProcessName == "ScreenToGif") 
                    return;

                //Clear up the selected window frame.
                Util.Native.DrawFrame(handle, scale);
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
            DisplayTimer.Start();
            FrameRate.Start(HasFixedDelay(), GetFixedDelay());

            if (UserSettings.All.ShowCursor)
            {
                #region If Show Cursor

                if (UserSettings.All.AsyncRecording)
                {
                    _captureTimer.Tick += CursorAsync_Elapsed;
                    CursorAsync_Elapsed(null, null);
                }
                else
                {
                    _captureTimer.Tick += Cursor_Elapsed;
                    Cursor_Elapsed(null, null);
                }

                _captureTimer.Start();

                Stage = Stage.Recording;
                AutoFitButtons();

                #endregion
            }
            else
            {
                #region If Not

                if (UserSettings.All.AsyncRecording)
                {
                    _captureTimer.Tick += NormalAsync_Elapsed;
                    NormalAsync_Elapsed(null, null);
                }
                else
                {
                    _captureTimer.Tick += Normal_Elapsed;
                    Normal_Elapsed(null, null);
                }

                _captureTimer.Start();

                Stage = Stage.Recording;
                AutoFitButtons();

                #endregion
            }
        }


        private async void NormalAsync_Elapsed(object sender, EventArgs e)
        {
            if (_stopRequested)
                return;

            _captureTask = _capture.CaptureAsync(new FrameInfo(_recordClicked, _keyList));
            FrameCount = await _captureTask;

            _keyList.Clear();
        }

        private async void CursorAsync_Elapsed(object sender, EventArgs e)
        {
            if (_stopRequested)
                return;

            _captureTask = _capture.CaptureWithCursorAsync(new FrameInfo(_recordClicked, _keyList));
            FrameCount = await _captureTask;

            _keyList.Clear();
        }

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            FrameCount = _capture.Capture(new FrameInfo(false, _keyList));

            _keyList.Clear();
        }

        private void Cursor_Elapsed(object sender, EventArgs e)
        {
            FrameCount = _capture.CaptureWithCursor(new FrameInfo(_recordClicked, _keyList));

            _keyList.Clear();
        }


        private void GarbageTimer_Tick(object sender, EventArgs e)
        {
            GC.Collect(UserSettings.All.LatestFps > 30 ? 6 : 2);
        }


        private void FollowTimer_Tick(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal || _prevPosX == _posX && _prevPosY == _posY || Stage == Stage.Paused || Stage == Stage.Stopped || Stage == Stage.Discarding ||
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

        #endregion

        #region Methods

        private void RegisterCommands()
        {
            CommandBindings.Clear();
            CommandBindings.AddRange(new CommandBindingCollection
            {
                new CommandBinding(_viewModel.CloseCommand, (sender, args) => Close(),
                    (sender, args) => args.CanExecute = Stage == Stage.Stopped || ((UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && (Project == null || !Project.Any))),

                new CommandBinding(_viewModel.OptionsCommand, ShowOptions,
                    (sender, args) => args.CanExecute = (Stage != Stage.Recording || UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && Stage != Stage.PreStarting),

                new CommandBinding(_viewModel.SnapToWindowCommand, null,
                    (sender, args) => args.CanExecute = Stage == Stage.Stopped || (Stage == Stage.Recording && (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && (Project == null || Project.Frames.Count == 0))),

                new CommandBinding(_viewModel.SwitchFrequencyCommand, SwitchFrequency,
                    (sender, args) =>
                    {
                        if (args.Parameter != null && !args.Parameter.Equals("Switch"))
                        {
                            args.CanExecute = true;
                            return;
                        }

                        args.CanExecute = ((Stage != Stage.Recording || Project == null) || UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && Stage != Stage.PreStarting;
                    }),

                new CommandBinding(_viewModel.RecordCommand, async (sender, args) => await Record(),
                    (sender, args) => args.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Paused) && UserSettings.All.CaptureFrequency != CaptureFrequency.Manual),

                new CommandBinding(_viewModel.PauseCommand, (sender, args) => Pause(),
                    (sender, args) => args.CanExecute = Stage == Stage.Recording && UserSettings.All.CaptureFrequency != CaptureFrequency.Manual),

                new CommandBinding(_viewModel.SnapCommand, async (sender, args) => await Snap(),
                    (sender, args) => args.CanExecute = Stage == Stage.Recording && UserSettings.All.CaptureFrequency == CaptureFrequency.Manual),

                new CommandBinding(_viewModel.StopLargeCommand, async (sender, args) => await Stop(),
                    (sender, args) => args.CanExecute = (Stage == Stage.Recording && UserSettings.All.CaptureFrequency != CaptureFrequency.Manual && UserSettings.All.CaptureFrequency != CaptureFrequency.Interaction &&
                        !UserSettings.All.RecorderDisplayDiscard) || Stage == Stage.PreStarting),

                new CommandBinding(_viewModel.StopCommand, async (sender, args) => await Stop(),
                    (sender, args) =>
                    {
                        args.CanExecute = Stage == Stage.Recording &&
                            ((UserSettings.All.CaptureFrequency != CaptureFrequency.Manual && UserSettings.All.CaptureFrequency != CaptureFrequency.Interaction && !UserSettings.All.RecorderDisplayDiscard) || FrameCount > 0) || Stage == Stage.Paused;
                    }),

                new CommandBinding(_viewModel.DiscardCommand, async (sender, args) => await Discard(),
                    (sender, args) => args.CanExecute = (Stage == Stage.Paused && FrameCount > 0) || (Stage == Stage.Recording && (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual ||
                        UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction || UserSettings.All.RecorderDisplayDiscard) && FrameCount > 0)),
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
            if (Stage == Stage.Stopped || ((UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && Stage == Stage.Recording && FrameCount == 0))
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
                    case CaptureFrequency.Manual:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.Interaction;
                        break;

                    case CaptureFrequency.Interaction:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.PerSecond;
                        break;

                    case CaptureFrequency.PerSecond:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.PerMinute;
                        break;

                    case CaptureFrequency.PerMinute:
                        UserSettings.All.CaptureFrequency = CaptureFrequency.PerHour;
                        break;

                    default: //PerHour.
                        UserSettings.All.CaptureFrequency = CaptureFrequency.Manual;
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
                    case Stage.Stopped:

                        #region If interaction mode

                        if (UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction)
                        {
                            Stage = Stage.Recording;
                            SetTaskbarButtonOverlay();
                            return;
                        }

                        #endregion

                        #region To record

                        //Take into account the frequency mode.
                        _captureTimer = new Timer { Interval = GetCaptureInterval() };

                        Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                        _keyList.Clear();
                        FrameCount = 0;

                        await Task.Factory.StartNew(UpdateScreenDpi);
                        await PrepareNewCapture();
                        HideGuidelines();

                        HeightIntegerBox.IsEnabled = false;
                        WidthIntegerBox.IsEnabled = false;
                        FrequencyIntegerUpDown.IsEnabled = false;

                        IsRecording = true;
                        Topmost = true;

                        UnregisterEvents();

                        #region Start

                        if (UserSettings.All.UsePreStart)
                        {
                            Stage = Stage.PreStarting;

                            Title = $"ScreenToGif ({LocalizationHelper.Get("S.Recorder.PreStart")} {UserSettings.All.PreStartValue}s)";
                            DisplayTimer.SetElapsed(-UserSettings.All.PreStartValue);

                            _preStartCount = UserSettings.All.PreStartValue - 1;
                            _preStartTimer.Start();
                            return;
                        }

                        DisplayTimer.Start();
                        FrameRate.Start(HasFixedDelay(), GetFixedDelay());

                        if (UserSettings.All.ShowCursor)
                        {
                            #region Show the cursor

                            if (UserSettings.All.AsyncRecording)
                                _captureTimer.Tick += CursorAsync_Elapsed;
                            else
                                _captureTimer.Tick += Cursor_Elapsed;

                            _captureTimer.Start();

                            //Manually capture the first frame on timelapse mode.
                            if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute || UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                            {
                                if (UserSettings.All.AsyncRecording)
                                    CursorAsync_Elapsed(null, null);
                                else
                                    Cursor_Elapsed(null, null);
                            }

                            Stage = Stage.Recording;
                            AutoFitButtons();
                            SetTaskbarButtonOverlay();

                            #endregion

                            return;
                        }

                        #region Don't show the cursor

                        if (UserSettings.All.AsyncRecording)
                            _captureTimer.Tick += NormalAsync_Elapsed;
                        else
                            _captureTimer.Tick += Normal_Elapsed;

                        _captureTimer.Start();

                        //Manually capture the first frame on timelapse mode.
                        if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute ||
                            UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                        {
                            if (UserSettings.All.AsyncRecording)
                                NormalAsync_Elapsed(null, null);
                            else
                                Normal_Elapsed(null, null);
                        }

                        Stage = Stage.Recording;
                        AutoFitButtons();
                        SetTaskbarButtonOverlay();

                        #endregion

                        break;

                    #endregion

                    #endregion
                    
                    case Stage.Paused:

                        #region To record again

                        Stage = Stage.Recording;
                        Title = "ScreenToGif";
                        
                        SetTaskbarButtonOverlay();
                        HideGuidelines();
                        AutoFitButtons();
                        
                        //If it's interaction mode, the capture is done via Snap().
                        if (UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction)
                            return;

                        FrequencyIntegerUpDown.IsEnabled = false;
                        
                        DisplayTimer.Start();
                        FrameRate.Start(HasFixedDelay(), GetFixedDelay());
                        
                        _captureTimer.Interval = GetCaptureInterval();
                        _captureTimer.Start();
                        break;

                        #endregion
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to start the recording.");
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.StartPauseNotPossible"), e.Message, e);
            }
            finally
            {
                //Wait a bit, then refresh the commands. Some of the commands are dependant of the FrameCount property.
                await Task.Delay(TimeSpan.FromMilliseconds(GetCaptureInterval() + 200));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Capture a single frame.
        /// </summary>
        private async Task Snap()
        {
            HideGuidelines();

            if (Project == null || Project.Frames.Count == 0)
            {
                try
                {
                    Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                    await PrepareNewCapture();

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
                    FrameCount = await _capture.ManualCaptureAsync(new FrameInfo(_recordClicked, _keyList), UserSettings.All.ShowCursor);

                    if (limit > 5)
                        throw new Exception("Impossible to capture the manual screenshot.");

                    limit++;
                }
                while (FrameCount == 0);

                _keyList.Clear();

                DisplayTimer.ManuallyCapturedCount++;
                CommandManager.InvalidateRequerySuggested();
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
                if (Stage != Stage.Recording)
                    return;

                Stage = Stage.Paused;
                Title = "ScreenToGif";
                FrequencyIntegerUpDown.IsEnabled = true;

                _captureTimer.Stop();
                DisplayTimer.Pause();
                FrameRate.Stop();

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

                _captureTimer.Stop();
                DisplayTimer.Stop();
                FrameRate.Stop();
                
                if (_capture != null)
                    await _capture.Stop();

                if ((Stage == Stage.Recording || Stage == Stage.Paused) && Project?.Any == true)
                {
                    #region Finishes if it's recording and it has any frames

                    if (UserSettings.All.AsyncRecording)
                        _stopRequested = true;

                    await Task.Delay(100);

                    Close();
                    return;

                    #endregion
                }

                #region Stops if it is not recording, or has no frames

                //Stop the pre-start timer to kill pre-start warming up.
                if (Stage == Stage.PreStarting)
                    _preStartTimer.Stop();

                Stage = Stage.Stopped;

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
            catch (NullReferenceException nll)
            {
                LogWriter.Log(nll, "NullPointer on the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", nll.Message, nll);
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
            if (_capture == null)
                return;

            Pause();

            if (UserSettings.All.NotifyRecordingDiscard && !Dialog.Ask(LocalizationHelper.Get("S.Recorder.Discard.Title"),
                LocalizationHelper.Get("S.Recorder.Discard.Instruction"), LocalizationHelper.Get("S.Recorder.Discard.Message"), false))
                return;

            _captureTimer.Stop();
            DisplayTimer.Stop();
            FrameRate.Stop();
            FrameCount = 0;
            Stage = Stage.Discarding;
            OutterGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;
            SetTaskbarButtonOverlay();

            //Frame capture (and disk write) must be stopped before trying to discard.
            await _capture.Stop();

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

        private void UnregisterEvents()
        {
            _captureTimer.Tick -= Normal_Elapsed;
            _captureTimer.Tick -= NormalAsync_Elapsed;

            _captureTimer.Tick -= Cursor_Elapsed;
            _captureTimer.Tick -= CursorAsync_Elapsed;
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
            _width = (int)Math.Round((Width - Constants.HorizontalOffset) * _scale);
            _height = (int)Math.Round((Height - Constants.VerticalOffset) * _scale);

            _viewModel.Region = new Rect(_viewModel.Region.TopLeft, new Size(_width, _height));

            if (_capture != null)
            {
                _capture.Width = _width;
                _capture.Height = _height;
            }

            DetectMonitorChanges(true);
        }

        private void UpdateLocation()
        {
            UserSettings.All.RecorderLeft = _left = (int)Math.Round((Math.Round(Left, MidpointRounding.AwayFromZero) + Constants.LeftOffset) * _scale);
            UserSettings.All.RecorderTop = _top = (int)Math.Round((Math.Round(Top, MidpointRounding.AwayFromZero) + Constants.TopOffset) * _scale);

            _viewModel.Region = new Rect(new Point(_left, _top), _viewModel.Region.BottomRight);

            if (_capture == null)
                return;

            _capture.Left = _left;
            _capture.Top = _top;

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
                if (_viewModel.PreviousMonitor != null && Stage == Stage.Recording && Project?.Any == true)
                {
                    Pause();

                    _capture.DeviceName = _viewModel.CurrentMonitor.Name;
                    _capture?.ResetConfiguration();

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
                var monitors = Monitor.AllMonitorsGranular();
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
                    case Stage.PreStarting:
                    {
                        await Stop();
                        break;
                    }
                    case Stage.Recording:
                    {
                        if (UserSettings.All.CaptureFrequency != CaptureFrequency.Manual)
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
            }

            //Since the list of monitors could have been changed, it needs to be queried again.
            _viewModel.Monitors = Monitor.AllMonitorsGranular();

            //Detect closest screen to the point (previously selected top/left point or current mouse coordinate).
            var point = startup ? (double.IsNaN(UserSettings.All.RecorderTop) || double.IsNaN(UserSettings.All.RecorderLeft) ? 
                Util.Native.GetMousePosition(_scale, Left, Top) : new Point((int)UserSettings.All.RecorderLeft, (int)UserSettings.All.RecorderTop)) : new Point((int) Left, (int) Top);
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
            var regionWidth = (int)Math.Round((UserSettings.All.RecorderWidth- Constants.HorizontalOffset) * _viewModel.CurrentMonitor.Scale);
            var regionHeight = (int)Math.Round((UserSettings.All.RecorderHeight - Constants.VerticalOffset) * _viewModel.CurrentMonitor.Scale);

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

        private async Task PrepareNewCapture()
        {
            if (_capture != null)
                await _capture.Dispose();

            if (UserSettings.All.UseDesktopDuplication)
            {
                //Check if Windows 8 or newer.
                if (!Util.Other.IsWin8OrHigher())
                    throw new Exception(LocalizationHelper.Get("S.Recorder.Warning.Windows8"));

                //Check if SharpDx is available.
                if (!Util.Other.IsSharpDxPresent())
                    throw new Exception(LocalizationHelper.Get("S.Recorder.Warning.MissingSharpDx"));

                _capture = GetDirectCapture();
                _capture.DeviceName = _viewModel.CurrentMonitor.Name;
                _viewModel.IsDirectMode = true;
            }
            else
            {
                //Capture with BitBlt.
                _capture = UserSettings.All.UseMemoryCache ? new CachedCapture() : new ImageCapture();
                _viewModel.IsDirectMode = true;
            }

            _capture.OnError += exception =>
            {
                Dispatcher?.Invoke(() =>
                {
                    //Pause the recording and show the error.  
                    Pause();

                    ErrorDialog.Ok("ScreenToGif", LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), exception.Message, exception);
                });
            };

            var dpi = Monitor.AllMonitors.FirstOrDefault(f => f.IsPrimary)?.Dpi ?? 96d;

            _capture.Start(GetCaptureInterval(), _left, _top, _width, _height, dpi / 96d, Project);
        }

        private ICapture GetDirectCapture()
        {
            return UserSettings.All.UseMemoryCache ? new DirectCachedCapture() : new DirectImageCapture();
        }

        private void DetectCaptureFrequency()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.Manual:
                    FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Manual.Short");
                    FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                    FrequencyViewbox.Visibility = Visibility.Collapsed;
                    AdjustToManual();
                    break;
                case CaptureFrequency.Interaction:
                    FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Interaction.Short");
                    FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                    FrequencyViewbox.Visibility = Visibility.Collapsed;
                    AdjustToInteraction();
                    break;
                case CaptureFrequency.PerSecond:
                    FrequencyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Recorder.Fps.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fps");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fps.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                case CaptureFrequency.PerMinute:
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
            Stage = Stage.Recording;
            Title = "ScreenToGif";
            FrameRate.Start(HasFixedDelay(), GetFixedDelay());

            AutoFitButtons();
            DisplayGuidelines();
        }

        private void AdjustToInteraction()
        {
            Stage = Project?.Frames?.Count > 0 ? Stage.Paused : Stage.Stopped;
            Title = "ScreenToGif";
            FrameRate.Start(HasFixedDelay(), GetFixedDelay());

            AutoFitButtons();
            DisplayGuidelines();
        }

        private void AdjustToAutomatic()
        {
            Stage = Project?.Frames?.Count > 0 ? Stage.Paused : Stage.Stopped;
            Title = "ScreenToGif";
            FrameRate.Stop();

            AutoFitButtons();
            DisplayGuidelines();

            UnregisterEvents();

            if (UserSettings.All.ShowCursor)
            {
                if (UserSettings.All.AsyncRecording)
                    _captureTimer.Tick += CursorAsync_Elapsed;
                else
                    _captureTimer.Tick += Cursor_Elapsed;
            }
            else
            {
                if (UserSettings.All.AsyncRecording)
                    _captureTimer.Tick += NormalAsync_Elapsed;
                else
                    _captureTimer.Tick += Normal_Elapsed;
            }
        }

        private int GetCaptureInterval()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.PerHour: //15 frames per hour = 240,000 ms (240 sec, 4 min).
                    return (1000 * 60 * 60) / FrequencyIntegerUpDown.Value;

                case CaptureFrequency.PerMinute: //15 frames per minute = 4,000 ms (4 sec).
                    return (1000 * 60) / FrequencyIntegerUpDown.Value;

                default: //PerSecond. 15 frames per second = 66 ms.
                    return 1000 / FrequencyIntegerUpDown.Value;
            }
        }

        private bool HasFixedDelay()
        {
            return UserSettings.All.CaptureFrequency != CaptureFrequency.PerSecond || UserSettings.All.FixedFrameRate;
        }

        private int GetFixedDelay()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.Manual:
                    return UserSettings.All.PlaybackDelayManual;
                case CaptureFrequency.Interaction:
                    return UserSettings.All.PlaybackDelayInteraction;
                case CaptureFrequency.PerMinute:
                    return UserSettings.All.PlaybackDelayMinute;
                case CaptureFrequency.PerHour:
                    return UserSettings.All.PlaybackDelayHour;
                default: //When the capture is 'PerSecond', the fixed delay is set to use the current framerate.
                    return 1000 / FrequencyIntegerUpDown.Value;
            }
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
                    case Stage.Stopped:
                        TaskbarItemInfo.Overlay = null;
                        return;
                    case Stage.Recording:
                        if (UserSettings.All.CaptureFrequency != CaptureFrequency.Manual)
                            TaskbarItemInfo.Overlay = new DrawingImage((FindResource("Vector.Record") as DrawingBrush)?.Drawing);
                        else
                            TaskbarItemInfo.Overlay = null;
                        return;
                    case Stage.Paused:
                        TaskbarItemInfo.Overlay = new DrawingImage((FindResource("Vector.Pause") as DrawingBrush)?.Drawing);
                        return;
                    case Stage.Discarding:
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
}