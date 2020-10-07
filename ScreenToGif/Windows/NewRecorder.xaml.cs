using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Native;
using ScreenToGif.Util;
using ScreenToGif.Util.InputHook;
using ScreenToGif.ViewModel;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace ScreenToGif.Windows
{
    public partial class NewRecorder : RecorderWindow
    {
        #region Variables

        private static readonly object Lock = new object();

        /// <summary>
        /// The view model of the recorder.
        /// </summary>
        private readonly ScreenRecorderViewModel _viewModel;

        /// <summary>
        /// Keyboard and mouse hooks helper.
        /// </summary>
        private readonly InputHook _actHook;

        /// <summary>
        /// This is the helper class which brings the screen area selection.
        /// </summary>
        private readonly RegionSelection _regionSelection = new RegionSelection();

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked;

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

        /// <summary>
        /// True when the user stop the recording. 
        /// </summary>
        private bool _stopRequested;

        /// <summary>
        /// Deals with all screen capture methods.
        /// </summary>
        private ICapture _capture;

        /// <summary>
        /// When the recording is async and was stopped, it needs to wait for the last task to finish.
        /// </summary>
        private Task<int> _captureTask;

        private readonly Timer _preStartTimer = new Timer();
        private Timer _captureTimer = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();
        private readonly Timer _followTimer = new Timer();
        private readonly Timer _showBorderTimer = new Timer();

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

        #region Dependency Properties

        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        public static readonly DependencyProperty IsFollowingProperty = DependencyProperty.Register(nameof(IsFollowing), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false, IsFollowing_PropertyChanged));

        #endregion

        #region Properties

        public bool IsRecording
        {
            get => (bool)GetValue(IsRecordingProperty);
            set => SetValue(IsRecordingProperty, value);
        }

        public bool IsFollowing
        {
            get => (bool)GetValue(IsFollowingProperty);
            set => SetValue(IsFollowingProperty, value);
        }

        /// <summary>
        /// Get the selected region in screen coordinates.
        /// Scales the region selection to the DPI/Scale of the screen where the capture selection is located.
        /// Also, takes into account the 1px border of the selection rectangle.
        /// </summary>
        public Rect CaptureRegion => _viewModel != null && _viewModel.Region.IsEmpty != true ? _viewModel.Region.Scale(_regionSelection.Scale).Offset(Util.Other.RoundUpValue(_regionSelection.Scale)) : Rect.Empty;
        //public Rect CaptureRegion => _viewModel != null && _viewModel.Region.IsEmpty != true ? _viewModel.Region.Offset(1).Scale(_regionSelection.Scale) : Rect.Empty;

        #endregion


        public NewRecorder()
        {
            InitializeComponent();

            _preStartTimer.Tick += PreStart_Elapsed;
            _preStartTimer.Interval = 1000;

            #region Global hook

            try
            {
                _actHook = new InputHook(true, true); //true for the mouse, true for the keyboard.
                _actHook.KeyDown += KeyHookTarget;
                _actHook.OnMouseActivity += MouseHookTarget;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to initialize the user activity hook.");
            }

            #endregion

            #region Model and commands

            DataContext = _viewModel = new ScreenRecorderViewModel();

            RegisterCommands();

            #endregion

            #region Focus scope explanation

            //Since I'm using Commands inside a ContextMenu, I need to set logical focus in order for it to work.
            //FocusManager.FocusedElement="{Binding RelativeSource={x:Static RelativeSource.Self}, Mode=OneTime}"
            //https://www.wpftutorial.net/RoutedCommandsInContextMenu.html

            #endregion

            _regionSelection.PositionChanged += RegionSelection_PositionChanged;
            _regionSelection.DragStarted += RegionSelection_DragStarted;
            _regionSelection.DragEnded += RegionSelection_DragEnded;

            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Timers

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            _showBorderTimer.Interval = 500;
            _showBorderTimer.Tick += ShowBorderTimer_Tick;

            _followTimer.Tick += FollowTimer_Tick;

            #endregion

            DetectCaptureFrequency();

            _viewModel.IsDirectMode = UserSettings.All.UseDesktopDuplication;
            _viewModel.Monitors = Monitor.AllMonitorsGranular();
            
            await UpdatePositioning(true);

            if (UserSettings.All.CursorFollowing)
                Follow();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            lock (Lock)
            {
                if (_regionSelection.WindowState == WindowState.Minimized)
                    _regionSelection.WindowState = WindowState.Normal;

                IsFollowing = UserSettings.All.CursorFollowing;
                
                if (!IsFollowing || UserSettings.All.FollowShortcut != Key.None)
                    return;

                UserSettings.All.CursorFollowing = IsFollowing = false;

                Dialog.Ok(LocalizationHelper.Get("S.StartUp.Recorder"), LocalizationHelper.Get("S.Options.Warning.Follow.Header"),
                    LocalizationHelper.Get("S.Options.Warning.Follow.Message"), Icons.Warning);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var step = (Keyboard.Modifiers & ModifierKeys.Alt) != 0 ? 5 : 1;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            //TODO: Remove.
            if (key == Key.Tab)
            {
                Console.WriteLine($"Current Element: {(Keyboard.FocusedElement as FrameworkElement)?.Name}, {Keyboard.FocusedElement}");
            }

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

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                return;

            if (Stage == Stage.Recording && IsRegionIntersected())
            {
                Pause();

                Topmost = true;
            }

            DisplaySelection();
            ForceUpdate();
        }

        private void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
        
        private void RegionSelection_PositionChanged(object sender, RoutedEventArgs e)
        {
            DetectMonitorChanges();

            WidthIntegerBox.IgnoreValueChanged = true;
            HeightIntegerBox.IgnoreValueChanged = true;

            UserSettings.All.SelectedRegionScale = _regionSelection.Scale;
            UserSettings.All.SelectedRegion = _viewModel.Region = _regionSelection.Rect;

            WidthIntegerBox.IgnoreValueChanged = false;
            HeightIntegerBox.IgnoreValueChanged = false;

            if (_capture != null)
            {
                _capture.Left = (int)CaptureRegion.Left;
                _capture.Top = (int)CaptureRegion.Top;
            }
        }

        private void RegionSelection_DragStarted(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void RegionSelection_DragEnded(object sender, RoutedEventArgs e)
        {
            DetectMonitorChanges();
            MoveCommandPanel();
            Show();
        }

        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    _viewModel.PauseCommand.Execute(sender, null);
                else if (Stage == Stage.PreStarting)
                    _viewModel.StopCommand.Execute(sender, null);

                GC.Collect();
            }
        }

        private async void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs)
        {
            if (_viewModel != null)
                _viewModel.Monitors = Monitor.AllMonitorsGranular();

            await UpdatePositioning();

            if (WindowState == WindowState.Minimized && _regionSelection != null)
                _regionSelection.WindowState = WindowState.Minimized;
        }

        private void SizeIntegerBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            MoveCommandPanel();
            DisplaySelection();
        }
        
        private void SizeIntegerBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var relativePoint = e.GetPosition(WidthIntegerBox);
            var screenPoint = WidthIntegerBox.PointToScreen(new Point(0, 0));
            var scale = this.Scale();

            Util.Native.SetCursorPos((int)(screenPoint.X + relativePoint.X * scale), (int)(screenPoint.Y + relativePoint.Y * scale));
        }

        private static void IsFollowing_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is NewRecorder rec))
                return;

            rec.Follow();
        }
        
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            _regionSelection.Hide();

            WindowState = WindowState.Minimized;
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Close the selecting rectangle.
            _regionSelection.Close();

            //Save Settings
            UserSettings.Save();

            #region Remove Hooks

            try
            {
                if (_actHook != null)
                {
                    _actHook.OnMouseActivity -= MouseHookTarget;
                    _actHook.KeyDown -= KeyHookTarget;
                    _actHook.Stop(); //Stop the user activity watcher.
                }
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
            _garbageTimer.Stop();
            _followTimer.Stop();

            #endregion

            //Clean all capture resources.
            if (_capture != null)
                await _capture.Dispose();

            GC.Collect();
        }


        private async void RegionButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(ModeType.Region);
        }

        private async void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(ModeType.Window);
        }

        private async void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(ModeType.Fullscreen);
        }


        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// When using commands when the current window has no focus, pass an IInputElement as the target to make it work.
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

                _posX = (int)Math.Round(args.PosX / _regionSelection.Scale, MidpointRounding.AwayFromZero);
                _posY = (int)Math.Round(args.PosY / _regionSelection.Scale, MidpointRounding.AwayFromZero);
                
                if (Stage == Stage.Recording && args.IsInteraction && UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction)
                {
                    var controlHit = VisualTreeHelper.HitTest(this, Mouse.GetPosition(this));
                    var selectionHit = _regionSelection.IsVisible && _regionSelection.Opacity > 0 ? VisualTreeHelper.HitTest(_regionSelection, Mouse.GetPosition(_regionSelection)) : null;
                    
                    if (controlHit == null && selectionHit == null)
                        await Snap();
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Error in mouse hook target.");
            }
        }
        

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
            
            if (IsRegionIntersected())
            {
                Splash.Dismiss();
                WindowState = WindowState.Minimized;
            }

            Title = "ScreenToGif";
            IsRecording = true;
            DisplayTimer.Start();
            FrameRate.Start(HasFixedDelay(), GetFixedDelay());

            if (UserSettings.All.ShowCursor)
            {
                #region Show the cursor

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

                #endregion

                return;
            }
                
            #region Don't show the cursor

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

                #endregion
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
            FrameCount = _capture.Capture(new FrameInfo(_recordClicked, _keyList));

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
            if (_viewModel.Region.IsEmpty || _prevPosX == _posX && _prevPosY == _posY || Stage == Stage.Paused || Stage == Stage.Stopped || Stage == Stage.Discarding ||
                (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers == UserSettings.All.DisableFollowModifiers))
                return;

            _prevPosX = _posX;
            _prevPosY = _posY;

            //Only move to the left if 'Mouse.X < Rect.L' and only move to the right if 'Mouse.X > Rect.R'
            _offsetX = _posX - UserSettings.All.FollowBuffer < _viewModel.Region.X ? _posX - _viewModel.Region.X - UserSettings.All.FollowBuffer :
                _posX + UserSettings.All.FollowBuffer > _viewModel.Region.Right ? _posX - _viewModel.Region.Right + UserSettings.All.FollowBuffer : 0;

            _offsetY = _posY - UserSettings.All.FollowBuffer < _viewModel.Region.Y ? _posY - _viewModel.Region.Y - UserSettings.All.FollowBuffer :
                _posY + UserSettings.All.FollowBuffer > _viewModel.Region.Bottom ? _posY - _viewModel.Region.Bottom + UserSettings.All.FollowBuffer : 0;

            //Hide the UI when moving.
            if (_posX - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < _viewModel.Region.X || _posX + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > _viewModel.Region.Right ||
                _posY - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < _viewModel.Region.Y || _posY + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > _viewModel.Region.Bottom)
            {
                _showBorderTimer.Stop();

                Visibility = Visibility.Hidden;
                _regionSelection.Hide();

                _showBorderTimer.Start();
            }

            //Limit to the current screen (only if in DirectX mode).
            //_viewModel.Region = new Rect(new Point((_viewModel.Region.X + _offsetX).Clamp(_viewModel.MaximumBounds.Left - 1, _viewModel.MaximumBounds.Width - _viewModel.Region.Width + 1), 
            //    (_viewModel.Region.Y + _offsetY).Clamp(_viewModel.MaximumBounds.Top - 1, _viewModel.MaximumBounds.Height - _viewModel.Region.Height + 1)), _viewModel.Region.Size);

            //Limit to the current screen.
            _viewModel.Region = new Rect(new Point((_viewModel.Region.X + _offsetX).Clamp(_viewModel.CurrentMonitor.Bounds.Left - 1, _viewModel.CurrentMonitor.Bounds.Width - _viewModel.Region.Width + 1), 
                (_viewModel.Region.Y + _offsetY).Clamp(_viewModel.CurrentMonitor.Bounds.Top - 1, _viewModel.CurrentMonitor.Bounds.Height - _viewModel.Region.Height + 1)), _viewModel.Region.Size);

            //Tell the capture helper that the position changed.
            if (_capture == null)
                return;
            
            _capture.Left = (int)CaptureRegion.Left;
            _capture.Top = (int)CaptureRegion.Top;
        }

        private void ShowBorderTimer_Tick(object sender, EventArgs e)
        {
            _showBorderTimer.Stop();

            DetectMonitorChanges();
            DisplaySelection();
            MoveCommandPanel();

            Visibility = Visibility.Visible;
        }

        #endregion

        #region Methods

        internal void MoveToMainScreen()
        {
            var main = _viewModel.Monitors.FirstOrDefault(f => f.IsPrimary) ?? _viewModel.Monitors.FirstOrDefault();

            if (main == null)
                return;

            //If there's no selection, simply move the command panel to the main screen.
            if (_viewModel.Region.IsEmpty)
            {
                MovePanelTo(main, main.WorkingArea.Left + main.WorkingArea.Width / 2 - RecorderWindow.ActualWidth / 2, main.WorkingArea.Top + main.WorkingArea.Height / 2 - RecorderWindow.ActualHeight / 2);
                return;
            }

            //This code it's kind of broken. It's not taking into consideration the relative position of the window on the secondary monitor.
            //It will move the window to the primary monitor, but it won't keep the same axis.  

            var diff = _regionSelection.Scale / main.Scale;
            var left = _viewModel.Region.Left / diff;
            var top = _viewModel.Region.Top / diff;
            
            if (main.Bounds.Top > top)
                top = main.Bounds.Top;

            if (main.Bounds.Left > left)
                left = main.Bounds.Left;

            if (main.Bounds.Bottom < top + _viewModel.Region.Height * diff)
                top = main.Bounds.Bottom - _viewModel.Region.Height * diff;

            if (main.Bounds.Right < left + _viewModel.Region.Width * diff)
                left = main.Bounds.Right - _viewModel.Region.Width * diff;

            UserSettings.All.SelectedRegion = _viewModel.Region = new Rect(new Point(left, top), _viewModel.Region.Size);
            UserSettings.All.SelectedRegionScale = main.Scale;

            DisplaySelection(_regionSelection.Mode, main);
            MoveCommandPanel();
        }

        private async Task UpdatePositioning(bool startup = false)
        {
            if (!startup)
            {
                #region When the recorder was already opened

                //When in selection mode, cancel selection.
                if (RegionSelectHelper.IsSelecting)
                    RegionSelectHelper.Abort();

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

                //Move region to the closest available screen.
                MoveToClosestScreen();

                #endregion
            }
            else
            {
                #region The user can opt out of the using the previous position and size

                if (!UserSettings.All.RecorderRememberPosition && !UserSettings.All.SelectedRegion.IsEmpty)
                {
                    if (!UserSettings.All.RecorderRememberSize)
                    {
                        UserSettings.All.SelectedRegion = Rect.Empty;
                    }
                    else
                    {
                        var main = _viewModel.Monitors.FirstOrDefault(f => f.IsPrimary) ?? _viewModel.Monitors.FirstOrDefault();

                        if (main != null)
                        {
                            //Center the selection on the main screen.
                            var left = main.Bounds.Left + main.Bounds.Width / 2d - UserSettings.All.SelectedRegion.Width / 2d;
                            var top = main.Bounds.Top + main.Bounds.Height / 2d - UserSettings.All.SelectedRegion.Height / 2d;

                            UserSettings.All.SelectedRegion = new Rect(new Point(left, top), UserSettings.All.SelectedRegion.Size);
                            UserSettings.All.SelectedRegionScale = main.Scale;
                        }
                        else
                        {
                            //If it was not possible to detect the primary screen, simply clear the selection.
                            UserSettings.All.SelectedRegion = Rect.Empty;
                            UserSettings.All.SelectedRegionScale = 1;
                        }
                    }
                }

                #endregion

                #region Previously selected region

                //If a region was previously selected.
                if (!UserSettings.All.SelectedRegion.IsEmpty)
                {
                    //Check if the previous selection can be positioned inside a screen.
                    var monitor = _viewModel.Monitors.FirstOrDefault(f => f.NativeBounds.Contains(UserSettings.All.SelectedRegion.Scale(UserSettings.All.SelectedRegionScale)));

                    if (monitor != null)
                    {
                        _viewModel.CurrentMonitor = monitor;
                        _viewModel.Region = UserSettings.All.SelectedRegion;
                        UserSettings.All.SelectedRegionScale = monitor.Scale;
                    }
                    else
                    {
                        //Fullscreen selection.
                        monitor = _viewModel.Monitors.FirstOrDefault(f => f.Bounds == UserSettings.All.SelectedRegion.Offset(1));

                        if (monitor != null)
                        {
                            _viewModel.CurrentMonitor = monitor;
                            _viewModel.Region = UserSettings.All.SelectedRegion;
                            UserSettings.All.SelectedRegionScale = monitor.Scale;
                        }
                    }
                }

                #endregion
            }

            #region Adjust the position of the main controls

            if (_viewModel.Region.IsEmpty)
            {
                #region Center on screen

                var screen = _viewModel.Monitors.FirstOrDefault(x => x.Bounds.Contains(Util.Native.GetMousePosition(1, Left, Top))) ?? _viewModel.Monitors.FirstOrDefault(x => x.IsPrimary) ?? _viewModel.Monitors.FirstOrDefault();

                if (screen == null)
                    throw new Exception("It was not possible to get a list of known screens.");

                MovePanelTo(screen, screen.WorkingArea.Left + screen.WorkingArea.Width / 2 - RecorderWindow.ActualWidth / 2, screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - RecorderWindow.ActualHeight / 2);

                #endregion
            }
            else
            {
                MoveCommandPanel();
                DisplaySelection((ModeType) UserSettings.All.RecorderModeIndex);
            }

            #endregion
        }

        private void ForceUpdate()
        {
            InvalidateMeasure();
            InvalidateArrange();
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Arrange(new Rect(DesiredSize));
        }

        private void RegisterCommands()
        {
            CommandBindings.Clear();
            CommandBindings.AddRange(new CommandBindingCollection
            {
                new CommandBinding(_viewModel.CloseCommand, (sender, args) => Close(),
                    (sender, args) => args.CanExecute = Stage == Stage.Stopped || ((UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && (Project == null || !Project.Any))),

                new CommandBinding(_viewModel.OptionsCommand, ShowOptions,
                    (sender, args) => args.CanExecute = (Stage != Stage.Recording || UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && Stage != Stage.PreStarting),

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
                        if (UserSettings.All.RecorderCompactMode)
                        {
                            args.CanExecute = Stage == Stage.Recording && ((UserSettings.All.CaptureFrequency != CaptureFrequency.Manual && UserSettings.All.CaptureFrequency != CaptureFrequency.Interaction && 
                                !UserSettings.All.RecorderDisplayDiscard) || FrameCount > 0) || Stage == Stage.Paused || Stage == Stage.PreStarting;
                            return;
                        }

                        args.CanExecute = (Stage == Stage.Recording && (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction || 
                            UserSettings.All.RecorderDisplayDiscard) && FrameCount > 0) || (Stage == Stage.Paused && FrameCount > 0);
                    }),

                new CommandBinding(_viewModel.DiscardCommand, async (sender, args) => await Discard(),
                    (sender, args) => args.CanExecute = (Stage == Stage.Paused && FrameCount > 0) || (Stage == Stage.Recording && (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || 
                        UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction || UserSettings.All.RecorderDisplayDiscard) && FrameCount > 0)),
            });

            _viewModel.RefreshKeyGestures();
        }

        private void UnregisterEvents()
        {
            _captureTimer.Tick -= Normal_Elapsed;
            _captureTimer.Tick -= NormalAsync_Elapsed;

            _captureTimer.Tick -= Cursor_Elapsed;
            _captureTimer.Tick -= CursorAsync_Elapsed;
        }

        private void ShowOptions(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;
            _regionSelection.Topmost = false;

            var options = new Options(Options.RecorderIndex);
            options.ShowDialog();

            DetectCaptureFrequency();
            RegisterCommands();
            DisplaySelection();
            MoveCommandPanel();

            //If not recording (or recording in manual/interactive mode, but with no frames captured yet), adjust the maximum bounds for the recorder.
            if (Stage == Stage.Stopped || ((UserSettings.All.CaptureFrequency == CaptureFrequency.Manual || UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction) && Stage == Stage.Recording && FrameCount == 0))
                _viewModel.IsDirectMode = UserSettings.All.UseDesktopDuplication;

            Topmost = true;
            _regionSelection.Topmost = true;
        }

        internal async Task Record()
        {
            try
            {
                switch (Stage)
                {
                    case Stage.Stopped:

                        #region If region not yet selected

                        if (_viewModel.Region.IsEmpty)
                        {
                            await PickRegion((ModeType) ReselectSplitButton.SelectedIndex, true);

                            if (_viewModel.Region.IsEmpty)
                                return;
                        }

                        #endregion

                        #region If interaction mode

                        if (UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction)
                        {
                            Stage = Stage.Recording;
                            SetTaskbarButtonOverlay();
                            return;
                        }

                        #endregion

                        #region To record

                        _captureTimer = new Timer { Interval = GetCaptureInterval() };

                        Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                        _keyList.Clear();
                        FrameCount = 0;

                        await PrepareNewCapture();

                        FrequencyIntegerUpDown.IsEnabled = false;

                        _regionSelection.HideGuidelines();
                        IsRecording = true;
                        Topmost = true;

                        UnregisterEvents();

                        //Detects a possible intersection of capture region and capture controls.
                        var isIntersecting = IsRegionIntersected();

                        if (isIntersecting)
                        {
                            Topmost = false;
                            Splash.Display(LocalizationHelper.GetWithFormat("S.Recorder.Splash.Title", "Press {0} to stop the recording", Util.Native.GetSelectKeyText(UserSettings.All.StopShortcut, UserSettings.All.StopModifiers)), 
                                LocalizationHelper.GetWithFormat("S.Recorder.Splash.Subtitle", "The recorder window will be minimized,&#10;restore it or press {0} to pause the capture", Util.Native.GetSelectKeyText(UserSettings.All.StartPauseShortcut, UserSettings.All.StartPauseModifiers)));
                            Splash.SetTime(-UserSettings.All.PreStartValue);
                        }

                        #region Start

                        if (isIntersecting || UserSettings.All.UsePreStart)
                        {
                            Stage = Stage.PreStarting;

                            Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.PreStarting");
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
                        if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute || UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                        {
                            if (UserSettings.All.AsyncRecording)
                                NormalAsync_Elapsed(null, null);
                            else
                                Normal_Elapsed(null, null);
                        }

                        Stage = Stage.Recording;
                        SetTaskbarButtonOverlay();

                        #endregion

                        break;

                    #endregion

                    #endregion

                    case Stage.Paused:

                        #region To record again

                        Stage = Stage.Recording;
                        Title = "ScreenToGif";
                        _regionSelection.HideGuidelines();
                        SetTaskbarButtonOverlay();

                        //If it's interaction mode, the capture is done via Snap().
                        if (UserSettings.All.CaptureFrequency == CaptureFrequency.Interaction)
                            return;

                        FrequencyIntegerUpDown.IsEnabled = false;

                        //Detects a possible intersection of capture region and capture controls.
                        if (IsRegionIntersected())
                            WindowState = WindowState.Minimized;
                        
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

        private async Task Snap()
        {
            #region If region not yet selected

            if (_viewModel.Region.IsEmpty)
            {
                await PickRegion((ModeType)ReselectSplitButton.SelectedIndex, true);

                if (_viewModel.Region.IsEmpty)
                    return;
            }

            #endregion

            _regionSelection.HideGuidelines();

            if (Project == null || Project.Frames.Count == 0)
            {
                try
                {
                    Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                    await PrepareNewCapture();

                    _keyList.Clear();
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

                //Displays that a frame was manually captured.
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

                _regionSelection.DisplayGuidelines();
                SetTaskbarButtonOverlay();
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to pause the recording.");
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.StartPauseNotPossible"), e.Message, e);
            }
        }

        private async Task Stop()
        {
            try
            {
                RecordControlsGrid.IsEnabled = false;
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

                Splash.Dismiss();
                Stage = Stage.Stopped;

                //Enables the controls that are disabled while recording;
                FrequencyIntegerUpDown.IsEnabled = true;
                IsRecording = false;
                Topmost = true;

                _regionSelection.DisplayGuidelines();
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
                    RecordControlsGrid.IsEnabled = true;
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
            RecordControlsGrid.IsEnabled = false;
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
            RecordControlsGrid.IsEnabled = true;

            Title = "ScreenToGif";
            Cursor = Cursors.Arrow;
            IsRecording = false;

            DetectCaptureFrequency();
            SetTaskbarButtonOverlay();
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

                _viewModel.IsDirectMode = false;
            }

            _capture.OnError += exception =>
            {
                //Pause the recording and show the error.  
                _viewModel.PauseCommand.Execute(null, null);

                ErrorDialog.Ok("ScreenToGif", LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), exception.Message, exception);
            };

            _capture.Start(GetCaptureInterval(), (int)CaptureRegion.X, (int)CaptureRegion.Y, (int)CaptureRegion.Width, (int)CaptureRegion.Height, _regionSelection.Scale, Project);
        }

        private ICapture GetDirectCapture()
        {
            if (UserSettings.All.OnlyCaptureChanges)
                return UserSettings.All.UseMemoryCache ? (ICapture) new DirectChangedCachedCapture() : new DirectChangedImageCapture();

            return UserSettings.All.UseMemoryCache ? new DirectCachedCapture() : new DirectImageCapture();
        }

        private async Task PickRegion(ModeType mode, bool quickSelection = false)
        {
            _regionSelection.Hide();
            Hide();

            var previousMode = UserSettings.All.RecorderModeIndex;

            var selection = await RegionSelectHelper.Select(mode, _viewModel.Region, _regionSelection.Monitor, quickSelection);

            ForceUpdate();

            if (selection.Region != Rect.Empty)
            {
                WidthIntegerBox.IgnoreValueChanged = true;
                HeightIntegerBox.IgnoreValueChanged = true;

                UserSettings.All.SelectedRegionScale = selection.Monitor.Scale;
                UserSettings.All.SelectedRegion = _viewModel.Region = selection.Region;

                WidthIntegerBox.IgnoreValueChanged = false;
                HeightIntegerBox.IgnoreValueChanged = false;

                DisplaySelection(mode, selection.Monitor);
                MoveCommandPanel();
            }
            else
            {
                UserSettings.All.RecorderModeIndex = previousMode;
                DisplaySelection();
            }

            Show();
        }

        private void DisplaySelection(ModeType? mode = null, Monitor display = null)
        {
            if (_viewModel.Region.IsEmpty)
            {
                if (_regionSelection.IsVisible)
                    _regionSelection.Hide();
            }
            else
            {
                if (display != null)
                    _viewModel.CurrentMonitor = display;

                _regionSelection.Select(mode, _viewModel.Region, display ?? _viewModel.CurrentMonitor);
            }

            DisplaySize();
            DetectMonitorChanges();
        }

        private void DisplaySize()
        {
            switch (UserSettings.All.RecorderModeIndex)
            {
                case (int)ModeType.Window:
                {
                    SizeTextBlock.ToolTip = null;

                    if (_viewModel.Region.IsEmpty)
                    {
                        SizeTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Window.Select");

                        SizeGrid.Visibility = Visibility.Collapsed;
                        SizeTextBlock.Visibility = Visibility.Visible;
                        return;
                    }

                    SizeGrid.Visibility = Visibility.Visible;
                    SizeTextBlock.Visibility = Visibility.Collapsed;
                    return;
                }
                case (int)ModeType.Fullscreen:
                {
                    SizeGrid.Visibility = Visibility.Collapsed;
                    SizeTextBlock.Visibility = Visibility.Visible;

                    if (_viewModel.CurrentMonitor == null)
                    {
                        SizeTextBlock.ToolTip = null;
                        SizeTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Screen.Select");
                        return;
                    }

                    SizeTextBlock.Text = _viewModel.CurrentMonitor.FriendlyName;
                    SizeTextBlock.ToolTip = 
                        LocalizationHelper.GetWithFormat("S.Recorder.Screen.Name.Info1", "Graphics adapter: {0}", _viewModel.CurrentMonitor.AdapterName) +
                        Environment.NewLine +
                        LocalizationHelper.GetWithFormat("S.Recorder.Screen.Name.Info2", "Resolution: {0} x {1}", _viewModel.CurrentMonitor.Bounds.Width, _viewModel.CurrentMonitor.Bounds.Height) +
                        (Math.Abs(_viewModel.CurrentMonitor.Scale - 1) > 0.001 ? Environment.NewLine +
                        LocalizationHelper.GetWithFormat("S.Recorder.Screen.Name.Info3", "Native resolution: {0} x {1}", _viewModel.CurrentMonitor.NativeBounds.Width, _viewModel.CurrentMonitor.NativeBounds.Height) : "")  +
                        Environment.NewLine +
                        LocalizationHelper.GetWithFormat("S.Recorder.Screen.Name.Info4", "DPI: {0} ({1:0.##}%)", _viewModel.CurrentMonitor.Dpi, _viewModel.CurrentMonitor.Scale * 100d);
                    
                    return;
                }
                default:
                {
                    SizeTextBlock.ToolTip = null;

                    if (_viewModel.Region.IsEmpty)
                    {
                        SizeTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Area.Select");

                        SizeGrid.Visibility = Visibility.Collapsed;
                        SizeTextBlock.Visibility = Visibility.Visible;
                        return;
                    }

                    SizeGrid.Visibility = Visibility.Visible;
                    SizeTextBlock.Visibility = Visibility.Collapsed;
                    return;
                }
            }
        }

        /// <summary>
        /// Repositions the capture controls near the selected region, in order to stay away from the capture. If no space available on the nearest screen, try others.
        /// </summary>
        private void MoveCommandPanel()
        {
            if (_viewModel.Region.Width < 25 || _viewModel.Region.Height < 25)
                return;

            #region Calculate the available spaces for all four sides 

            //If the selected region is passing the bottom edge of the display, it means that there are no space available on the bottom.
            //If the selected region is inside (bottom is below the top most part), it means that there are space available.
            //If none above, it means that the region is not located inside the screen.

            var bottomSpace = _viewModel.Region.Bottom > _viewModel.CurrentMonitor.Bounds.Bottom ? 0 :
                _viewModel.Region.Bottom > _viewModel.CurrentMonitor.Bounds.Top ? _viewModel.CurrentMonitor.Bounds.Bottom - _viewModel.Region.Bottom :
                    _viewModel.CurrentMonitor.Bounds.Height;

            var topSpace = _viewModel.Region.Top < _viewModel.CurrentMonitor.Bounds.Top ? 0 :
                _viewModel.Region.Top < _viewModel.CurrentMonitor.Bounds.Bottom ? _viewModel.Region.Top - _viewModel.CurrentMonitor.Bounds.Top :
                    _viewModel.CurrentMonitor.Bounds.Height;

            var leftSpace = _viewModel.Region.Left < _viewModel.CurrentMonitor.Bounds.Left ? 0 :
                _viewModel.Region.Left < _viewModel.CurrentMonitor.Bounds.Right ? _viewModel.Region.Left - _viewModel.CurrentMonitor.Bounds.Left :
                    _viewModel.CurrentMonitor.Bounds.Width;

            var rightSpace = _viewModel.Region.Right > _viewModel.CurrentMonitor.Bounds.Right ? 0 :
                _viewModel.Region.Right > _viewModel.CurrentMonitor.Bounds.Left ? _viewModel.CurrentMonitor.Bounds.Right - _viewModel.Region.Right :
                    _viewModel.CurrentMonitor.Bounds.Width;

            #endregion

            //Bottom.
            if (bottomSpace > (ActualHeight + 20))
            {
                MovePanelTo(_viewModel.CurrentMonitor, (_viewModel.Region.Left + _viewModel.Region.Width / 2 - (ActualWidth / 2))
                    .Clamp(_viewModel.CurrentMonitor.Bounds.Left, _viewModel.CurrentMonitor.Bounds.Right - ActualWidth), _viewModel.Region.Bottom + 10);
                return;
            }

            //Top.
            if (topSpace > ActualHeight + 20)
            {
                MovePanelTo(_viewModel.CurrentMonitor, (_viewModel.Region.Left + _viewModel.Region.Width / 2 - ActualWidth / 2)
                    .Clamp(_viewModel.CurrentMonitor.Bounds.Left, _viewModel.CurrentMonitor.Bounds.Right - ActualWidth), _viewModel.Region.Top - ActualHeight - 10);
                return;
            }

            //Left.
            if (leftSpace > ActualWidth + 20)
            {
                MovePanelTo(_viewModel.CurrentMonitor, _viewModel.Region.Left - ActualWidth - 10, _viewModel.Region.Top + _viewModel.Region.Height / 2 - ActualHeight / 2);
                return;
            }

            //Right.
            if (rightSpace > ActualWidth + 20)
            {
                MovePanelTo(_viewModel.CurrentMonitor, _viewModel.Region.Right + 10, _viewModel.Region.Top + _viewModel.Region.Height / 2 - ActualHeight / 2);
                return;
            }

            //No space available, simply center on the selected region.
            MovePanelTo(_viewModel.CurrentMonitor, _viewModel.Region.Left + _viewModel.Region.Width / 2 - ActualWidth / 2, _viewModel.Region.Top + _viewModel.Region.Height / 2 - ActualHeight / 2);
        }

        private void MovePanelTo(Monitor monitor, double left, double top)
        {
            if (_viewModel.CurrentControlMonitor?.Handle != monitor.Handle || _viewModel.CurrentControlMonitor?.Scale != monitor.Scale)
            {
                //First move the command window to the final monitor, so that the UI scale can be adjusted.
                this.MoveToScreen(monitor);
                
                _viewModel.CurrentControlMonitor = monitor;
            }

            //Move the command window to the final place.
            Left = left / (this.Scale() / monitor.Scale);
            Top = top / (this.Scale() / monitor.Scale);
        }

        /// <summary>
        /// Move the selection region to the closest screen when outside of any.
        /// </summary>
        private void MoveToClosestScreen()
        {
            //If the position was never set.
            if (_viewModel.Region.IsEmpty)
                return;

            var top = _viewModel.Region.Top;
            var left = _viewModel.Region.Left;

            var screen = Screen.FromRectangle(new Rectangle((int)_viewModel.Region.Left, (int)_viewModel.Region.Top, (int)_viewModel.Region.Width, (int)_viewModel.Region.Height));
            var closest = _viewModel.Monitors.FirstOrDefault(f => f.Name == screen.DeviceName) ?? _viewModel.Monitors.FirstOrDefault();
            //var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

            if (closest == null)
                throw new Exception("It was not possible to move the current selected region to the closest monitor.");

            //To much to the Left.
            if (closest.Bounds.Left > _viewModel.Region.Left - 1)
                left = closest.Bounds.Left;

            //Too much to the top.
            if (closest.Bounds.Top > _viewModel.Region.Top - 1)
                top = closest.Bounds.Top;

            //Too much to the right.
            if (closest.Bounds.Right < _viewModel.Region.Left + _viewModel.Region.Width)
                left = closest.Bounds.Right - _viewModel.Region.Width;

            //Too much to the bottom.
            if (closest.Bounds.Bottom < _viewModel.Region.Top + _viewModel.Region.Height)
                top = closest.Bounds.Bottom - _viewModel.Region.Height;

            UserSettings.All.SelectedRegionScale = closest.Scale;
            _viewModel.CurrentMonitor = closest;
            _viewModel.Region = UserSettings.All.SelectedRegion = new Rect(left, top, _viewModel.Region.Width, _viewModel.Region.Height);
        }

        /// <summary>
        /// True if the capture controls are intersecting with the capture region.
        /// </summary>
        /// <returns></returns>
        private bool IsRegionIntersected()
        {
            //TODO: It would be best to try to move the record controls away from the capture region before trying to hide it.
            return IsVisible && CaptureRegion.IntersectsWith(new Rect(Left, Top, Width, Height).Scale(_regionSelection.Scale));
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
        }

        private void DetectCaptureFrequency()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.Manual:
                    FrequencyTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Manual.Short");
                    FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                    FrequencyViewbox.Visibility = Visibility.Collapsed;
                    AdjustToManual();
                    break;
                case CaptureFrequency.Interaction:
                    FrequencyTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Interaction.Short");
                    FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                    FrequencyViewbox.Visibility = Visibility.Collapsed;
                    AdjustToInteraction();
                    break;
                case CaptureFrequency.PerSecond:
                    FrequencyTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Fps.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fps");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fps.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                case CaptureFrequency.PerMinute:
                    FrequencyTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Fpm.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                default: //PerHour.
                    FrequencyTextBlock.SetResourceReference(TextBlock.TextProperty, "S.Recorder.Fph.Short");
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

            _regionSelection.DisplayGuidelines();
        }

        private void AdjustToInteraction()
        {
            Stage = Project?.Frames?.Count > 0 ? Stage.Paused : Stage.Stopped;
            Title = "ScreenToGif";
            FrameRate.Start(HasFixedDelay(), GetFixedDelay());

            _regionSelection.DisplayGuidelines();
        }

        private void AdjustToAutomatic()
        {
            Stage = Project?.Frames?.Count > 0 ? Stage.Paused : Stage.Stopped;
            Title = "ScreenToGif";
            FrameRate.Stop();

            _regionSelection.DisplayGuidelines();

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

        private async void DetectMonitorChanges(bool detectCurrent = false)
        {
            if (detectCurrent)
            {
                var interop = new System.Windows.Interop.WindowInteropHelper(_regionSelection);
                var current = Screen.FromHandle(interop.Handle);

                _viewModel.CurrentMonitor = _viewModel.Monitors.FirstOrDefault(f => f.Name == current.DeviceName);
                //_viewModel.CurrentMonitor = Monitor.MostIntersected(_viewModel.Monitors, _viewModel.Region.Scale(_viewModel.CurrentMonitor.Scale)) ?? _viewModel.CurrentMonitor;
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

        private void MoveWindow(int left, int top, int right, int bottom)
        {
            //Limit to this screen in directX capture mode.
            var x = left > 0 ? Math.Max(_viewModel.Region.Left - left, _viewModel.MaximumBounds.Left - 1) : right > 0 ? Math.Min(_viewModel.Region.Left + right, _viewModel.MaximumBounds.Right - _viewModel.Region.Width + 1) : _viewModel.Region.Left;
            var y = top > 0 ? Math.Max(_viewModel.Region.Top - top, _viewModel.MaximumBounds.Top - 1) : bottom > 0 ? Math.Min(_viewModel.Region.Top + bottom, _viewModel.MaximumBounds.Bottom - _viewModel.Region.Height + 1) : _viewModel.Region.Top;

            _viewModel.Region = new Rect(x, y, _viewModel.RegionWidth, _viewModel.RegionHeight);

            DetectMonitorChanges();
            DisplaySelection();
            MoveCommandPanel();
        }

        private void ResizeWindow(int left, int top, int right, int bottom)
        {
            //Resize to top left increases heigth/width when reaching the limit.

            var newLeft = left < 0 ? Math.Max(_viewModel.Region.Left + left, _viewModel.MaximumBounds.Left - 1) : left > 0 ? _viewModel.Region.Left + left : _viewModel.Region.Left;
            var newTop = top < 0 ? Math.Max(_viewModel.Region.Top + top, _viewModel.MaximumBounds.Top - 1) : top > 0 ? _viewModel.Region.Top + top : _viewModel.Region.Top; 
            var width = (right > 0 ? Math.Min(_viewModel.Region.Width + right, _viewModel.MaximumBounds.Right - _viewModel.Region.Left + 1) - left : right < 0 ? _viewModel.Region.Width + right + (left > 0 ? -left : 0) : _viewModel.Region.Width - left);
            var height = (bottom > 0 ? Math.Min(_viewModel.Region.Height + bottom, _viewModel.MaximumBounds.Bottom - _viewModel.Region.Top + 1) - top : bottom < 0 ? _viewModel.Region.Height + bottom + (top > 0 ? -top : 0) : _viewModel.Region.Height - top);

            //Ignore input if the new size will be smaller than the minimum.
            if ((height < 25 && (top > 0 || bottom < 0)) || (width < 25 && (left > 0 || right < 0)))
                return;

            _viewModel.Region = new Rect(newLeft, newTop, width, height);

            DetectMonitorChanges();
            DisplaySelection();
            MoveCommandPanel();
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

        #endregion
    }
}