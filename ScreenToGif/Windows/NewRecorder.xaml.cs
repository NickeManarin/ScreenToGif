using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Capture;
using ScreenToGif.Windows.Other;
using Cursors = System.Windows.Input.Cursors;

namespace ScreenToGif.Windows
{
    public partial class NewRecorder : RecorderWindow
    {
        //This window is just the main recorder controls:
        //  Maybe add option for compact mode?

        //When capturing:
        //  If there no space on screen to put the UI
        //      Minimize the UI
        //      Warn the user that the recording can be stoped by pressing the shortcut or by restoring the UI into view.
        //  Ideia: Let the user draw things while recording.

        #region Variables

        private static readonly object Lock = new object();

        /// <summary>
        /// Keyboard and mouse hooks helper.
        /// </summary>
        private readonly UserActivityHook _actHook;

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
        private bool _recordClicked = false;

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

        //public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register(nameof(IsPickingRegion), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        //public static readonly DependencyProperty WasRegionPickedProperty = DependencyProperty.Register(nameof(WasRegionPicked), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        //public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false));
        public static readonly DependencyProperty IsFollowingProperty = DependencyProperty.Register(nameof(IsFollowing), typeof(bool), typeof(NewRecorder), new PropertyMetadata(false, IsFollowing_PropertyChanged));
        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register(nameof(Region), typeof(Rect), typeof(NewRecorder), new PropertyMetadata(Rect.Empty));

        #endregion

        #region Properties

        //public bool IsPickingRegion
        //{
        //    get => (bool)GetValue(IsPickingRegionProperty);
        //    set => SetValue(IsPickingRegionProperty, value);
        //}

        //public bool WasRegionPicked
        //{
        //    get => (bool)GetValue(WasRegionPickedProperty);
        //    set => SetValue(WasRegionPickedProperty, value);
        //}


        public bool IsRecording
        {
            get => (bool)GetValue(IsRecordingProperty);
            set => SetValue(IsRecordingProperty, value);
        }

        //public bool IsDragging
        //{
        //    get => (bool)GetValue(IsDraggingProperty);
        //    set => SetValue(IsDraggingProperty, value);
        //}

        public bool IsFollowing
        {
            get => (bool)GetValue(IsFollowingProperty);
            set => SetValue(IsFollowingProperty, value);
        }

        /// <summary>
        /// Get or set the selected region in window coordinates.
        /// </summary>
        public Rect Region
        {
            get => (Rect)GetValue(RegionProperty);
            set => SetValue(RegionProperty, value);
        }

        /// <summary>
        /// Get the selected region in screen coordinates.
        /// </summary>
        public Rect CaptureRegion
        {
            get
            {
                if (Region.IsEmpty)
                    return Region;

                var offset = Util.Other.RoundUpValue(_regionSelection.Scale);

                //Scales the region selection to the DPI/Scale of the screen where the capture selection is located.
                //Also, takes into account the 1px border of the selection rectangle.
                return Region.Offset(1).Scale(_regionSelection.Scale);
            }
        }

        #endregion


        public NewRecorder()
        {
            InitializeComponent();

            _preStartTimer.Tick += PreStart_Elapsed;
            _preStartTimer.Interval = 1000;

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook(true, true); //true for the mouse, true for the keyboard.
                _actHook.KeyDown += KeyHookTarget;
                _actHook.OnMouseActivity += MouseHookTarget;
            }
            catch (Exception) { }

            #endregion

            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }


        private async void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await UpdatePositioning(true);

            #region Timers

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            _showBorderTimer.Interval = 500;
            _showBorderTimer.Tick += ShowBorderTimer_Tick;

            _followTimer.Tick += FollowTimer_Tick;

            #endregion

            DetectCaptureFrequency();

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

        private void SwitchCaptureFrequency_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Snapping || Stage == Stage.Paused) && RecordControlsGrid.IsEnabled;
        }

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }


        private void SwitchCaptureFrequency_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //When this event is fired from clicking on the switch button.
            if (e.Parameter != null)
            {
                switch (UserSettings.All.CaptureFrequency)
                {
                    case CaptureFrequency.Manual:
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

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;
            _regionSelection.Topmost = false;

            var options = new Options(Options.RecorderIndex);
            options.ShowDialog();

            DetectCaptureFrequency();

            Topmost = true;
            _regionSelection.Topmost = true;
        }

        private void HeaderGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            _regionSelection.WindowState = WindowState.Minimized;
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //Can't exit the recorder when there's an active recording.
            switch (Stage)
            {
                case Stage.Snapping:
                {
                    if (Project?.Any != true)
                        Close();

                    break;
                }
                case Stage.Stopped:
                {
                    Close();
                    break;
                }

                default:
                    return;
            }
        }

        private async void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    await RecordPause();
                else if (Stage == Stage.PreStarting)
                    await Stop();

                GC.Collect();
            }
        }

        private async void SystemEvents_DisplaySettingsChanged(object sender, EventArgs eventArgs)
        {
            //await Task.Factory.StartNew(UpdateScreenDpi);

            await UpdatePositioning();
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
                _actHook.OnMouseActivity -= MouseHookTarget;
                _actHook.KeyDown -= KeyHookTarget;
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            #endregion

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            #region Stops the timers

            if (Stage != (int)Stage.Stopped)
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


        private static void IsFollowing_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is NewRecorder rec))
                return;

            rec.Follow();
        }


        private async void RegionButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(SelectControl.ModeType.Region);
        }

        private async void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(SelectControl.ModeType.Window);
        }

        private async void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            await PickRegion(SelectControl.ModeType.Fullscreen);
        }


        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            //TODO: I can't fire this when:
            //No region selected.
            //Selecting region.
            if (Region.IsEmpty)
                return;

            //if (Stage == Stage.SelectingRegion || (WindowState == WindowState.Minimized && SelectControl.Mode != SelectControl.ModeType.Fullscreen) || Region.IsEmpty) // || !WasRegionPicked)
            //    return;

            if (Stage != Stage.Discarding && Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
                RecordPauseButton_Click(null, null);
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut)
                StopButton_Click(null, null);
            else if ((Stage == Stage.Paused || Stage == Stage.Snapping) && Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
                DiscardButton_Click(null, null);
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.FollowModifiers) && e.Key == UserSettings.All.FollowShortcut)
                UserSettings.All.CursorFollowing = IsFollowing = !IsFollowing;
            else
                _keyList.Add(new SimpleKeyGesture(e.Key, Keyboard.Modifiers, e.IsUppercase));
        }

        /// <summary>
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private void MouseHookTarget(object sender, CustomMouseEventArgs args)
        {
            try
            {
                if (WindowState == WindowState.Minimized)
                    return;

                _recordClicked = args.LeftButton == MouseButtonState.Pressed || args.RightButton == MouseButtonState.Pressed || args.MiddleButton == MouseButtonState.Pressed;

                var _scale = 1d; //TODO: Scale.
                _posX = (int)Math.Round(args.PosX / _scale, MidpointRounding.AwayFromZero);
                _posY = (int)Math.Round(args.PosY / _scale, MidpointRounding.AwayFromZero);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Error in mouse hoook target.");
            }
        }


        private async void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _captureTimer.Stop();
            FrameRate.Stop();
            FrameCount = 0;
            Stage = Stage.Discarding;
            await _capture.Stop();

            //OutterGrid.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private async void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.CaptureFrequency == CaptureFrequency.Manual)
            {
                await Snap();
                return;
            }

            await RecordPause();
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            await Stop();
        }


        #region Discard Async

        private delegate void DiscardFrames();

        private DiscardFrames _discardFramesDel;

        private void Discard()
        {
            try
            {
                #region Remove all the files

                foreach (var frame in Project.Frames)
                {
                    try
                    {
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
                LogWriter.Log(io, "Error while trying to discard the Recording");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
                LogWriter.Log(ex, "Error while trying to discard the recording");
            }
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                //Enables the controls that are disabled while recording;
                FrequencyIntegerUpDown.IsEnabled = true;
                RecordControlsGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording = false;

                DiscardButton.BeginStoryboard(this.FindStoryboard("HideDiscardStoryboard"), HandoffBehavior.Compose);
                ReselectSplitButton.BeginStoryboard(this.FindStoryboard("ShowReselectStoryboard"), HandoffBehavior.Compose);

                DetectCaptureFrequency();
            }));

            GC.Collect();
        }

        #endregion

        #region Timers

        private void PreStart_Elapsed(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                Title = $"ScreenToGif ({LocalizationHelper.Get("S.Recorder.PreStart")} {_preStartCount}s)";
                _preStartCount--;
                return;
            }

            _preStartTimer.Stop();
            RecordPauseButton.IsEnabled = true;
            Title = "ScreenToGif";
            IsRecording = true;

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

                //Manually capture the first frame on timelapse mode.
                if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute || UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                {
                    if (UserSettings.All.AsyncRecording)
                        CursorAsync_Elapsed(null, null);
                    else
                        Cursor_Elapsed(null, null);
                }

                Stage = Stage.Recording;

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

                //Manually capture the first frame on timelapse mode.
                if (UserSettings.All.CaptureFrequency == CaptureFrequency.PerMinute || UserSettings.All.CaptureFrequency == CaptureFrequency.PerHour)
                {
                    if (UserSettings.All.AsyncRecording)
                        NormalAsync_Elapsed(null, null);
                    else
                        Normal_Elapsed(null, null);
                }

                Stage = Stage.Recording;

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
            if (Region.IsEmpty || _prevPosX == _posX && _prevPosY == _posY || Stage == Stage.Paused || Stage == Stage.Stopped || Stage == Stage.Discarding || Stage == Stage.SelectingRegion ||
                (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers == UserSettings.All.DisableFollowModifiers))
                return;

            _prevPosX = _posX;
            _prevPosY = _posY;

            //TODO: Test with multiple monitors.
            //if (isCentered)
            //{
            //    //Hide the UI.
            //    _showBorderTimer.Stop();
            //    BeginStoryboard(this.FindStoryboard("HideRectangleStoryboard"), HandoffBehavior.SnapshotAndReplace);
            //    _showBorderTimer.Start();

            //    _offsetX = _posX - Region.Width / 2d;
            //    _offsetY = _posY - Region.Height / 2d;

            //    Region = new Rect(new Point(_offsetX.Clamp(-1, Width - Region.Width + 1), _offsetY.Clamp(-1, Height - Region.Height + 1)), Region.Size);
            //    DashedRectangle.Refresh();
            //}
            //else
            {
                //Only move to the left if 'Mouse.X < Rect.L' and only move to the right if 'Mouse.X > Rect.R'
                _offsetX = _posX - UserSettings.All.FollowBuffer < Region.X ? _posX - Region.X - UserSettings.All.FollowBuffer :
                    _posX + UserSettings.All.FollowBuffer > Region.Right ? _posX - Region.Right + UserSettings.All.FollowBuffer : 0;

                _offsetY = _posY - UserSettings.All.FollowBuffer < Region.Y ? _posY - Region.Y - UserSettings.All.FollowBuffer :
                    _posY + UserSettings.All.FollowBuffer > Region.Bottom ? _posY - Region.Bottom + UserSettings.All.FollowBuffer : 0;

                //Hide the UI when moving.
                if (_posX - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < Region.X || _posX + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > Region.Right ||
                    _posY - UserSettings.All.FollowBuffer - UserSettings.All.FollowBufferInvisible < Region.Y || _posY + UserSettings.All.FollowBuffer + UserSettings.All.FollowBufferInvisible > Region.Bottom)
                {
                    _showBorderTimer.Stop();
                    Visibility = Visibility.Hidden;
                    BeginStoryboard(this.FindStoryboard("HideRectangleStoryboard"), HandoffBehavior.SnapshotAndReplace);
                    _showBorderTimer.Start();
                }

                Region = new Rect(new Point((Region.X + _offsetX).Clamp(-1, Width - Region.Width + 1), (Region.Y + _offsetY).Clamp(-1, Height - Region.Height + 1)), Region.Size);
                //DashedRectangle.Refresh();
            }

            //Rearrange the rectangles.
            //_rect = ScreenRegion.Scale(_scale).Offset(Util.Other.RoundUpValue(_scale));

            //if (_capture != null)
            //{
            //    _capture.Left = (int)_rect.Left;
            //    _capture.Top = (int)_rect.Top;
            //}
        }

        private void ShowBorderTimer_Tick(object sender, EventArgs e)
        {
            _showBorderTimer.Stop();

            //AdjustControls();

            Visibility = Visibility.Visible;

            BeginStoryboard(this.FindStoryboard("ShowRectangleStoryboard"), HandoffBehavior.Compose);
        }
        
        #endregion


        #region Methods

        private async Task UpdatePositioning(bool startup = false)
        {
            var monitors = Monitor.AllMonitorsGranular();

            if (!startup)
            {
                #region When the recorder was already opened
                
                //When in selection mode, cancel selection.
                if (RegionSelectHelper.IsSelecting)
                    RegionSelectHelper.Abort();

                //TODO: When discarding or other stages?
                if (Stage == Stage.PreStarting)
                    await Stop();
                else if (Stage == Stage.Recording)
                    await RecordPause();

                if (Stage == Stage.Paused || Stage == Stage.Stopped)
                {
                    //Move region to the closest available screen.
                    //TODO: What if the region was bigger than now?
                    Region = MoveToClosestScreen();
                }

                #endregion
            }
            else
            {
                #region Previously selected region

                //If a region was previously selected.
                if (!UserSettings.All.SelectedRegion.IsEmpty)
                {
                    //Check if the previous selection can be positioned inside a screen.
                    if (monitors.Any(x => x.Bounds.Contains(UserSettings.All.SelectedRegion)))
                        Region = UserSettings.All.SelectedRegion;
                }

                #endregion
            }

            #region Adjust the position of the main controls

            if (Region.IsEmpty)
            {
                //TODO: Scale.
                var screen = monitors.FirstOrDefault(x => x.Bounds.Contains(Native.GetMousePosition(1, Left, Top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

                if (screen == null)
                    throw new Exception("It was not possible to get a list of known screens.");

                //Move to the top, so that the UI can adjust to the DPI.
                Left = screen.Bounds.Left;
                Top = screen.Bounds.Top;

                Left = screen.WorkingArea.Left + screen.WorkingArea.Width / 2 - RecorderWindow.ActualWidth / 2;
                Top = screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - RecorderWindow.ActualHeight / 2;
            }
            else
            {
                DisplaySelection(MoveCommandPanel());
            }

            #endregion
        }

        private void UnregisterEvents()
        {
            _captureTimer.Tick -= Normal_Elapsed;
            _captureTimer.Tick -= NormalAsync_Elapsed;

            _captureTimer.Tick -= Cursor_Elapsed;
            _captureTimer.Tick -= CursorAsync_Elapsed;
        }

        internal async Task RecordPause()
        {
            try
            {
                switch (Stage)
                {
                    case Stage.Stopped:

                        #region If region not yet selected

                        if (Region.IsEmpty)
                        {
                            await PickRegion(ReselectSplitButton.SelectedIndex == 1 ? SelectControl.ModeType.Window : ReselectSplitButton.SelectedIndex == 2 ? SelectControl.ModeType.Fullscreen : SelectControl.ModeType.Region);
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

                        IsRecording = true;
                        Topmost = true;

                        //TODO: Adjust fullscreen recording usability.
                        //TODO: Detect that the window needs to be minimized. E01
                        //if (SelectControl.Mode == SelectControl.ModeType.Fullscreen)
                        {
                            //TODO: Minimize when in fullscreen or when the controls are located in the same screen as the selection.
                            //WindowState = WindowState.Minimized;
                            //Topmost = false;

                            //Warn the user that the screen will be hidden (show a splash screen for 3 seconds).
                            //Make it configurable.
                        }

                        FrameRate.Start(HasFixedDelay(), GetFixedDelay());
                        UnregisterEvents();

                        ReselectSplitButton.BeginStoryboard(this.FindStoryboard("HideReselectStoryboard"), HandoffBehavior.Compose);

                        #region Start

                        if (UserSettings.All.UsePreStart)
                        {
                            Title = $"ScreenToGif ({LocalizationHelper.Get("S.Recorder.PreStart")} {UserSettings.All.PreStartValue}s)";
                            RecordPauseButton.IsEnabled = false;

                            Stage = Stage.PreStarting;
                            _preStartCount = UserSettings.All.PreStartValue - 1;

                            _preStartTimer.Start();
                        }
                        else
                        {
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

                                #endregion
                            }
                            else
                            {
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

                                #endregion
                            }
                        }

                        break;

                    #endregion

                    #endregion

                    case Stage.Recording:

                        #region To pause

                        Stage = Stage.Paused;
                        Title = LocalizationHelper.Get("S.Recorder.Paused");
                        FrequencyIntegerUpDown.IsEnabled = true;

                        DiscardButton.BeginStoryboard(this.FindStoryboard("ShowDiscardStoryboard"), HandoffBehavior.Compose);

                        _captureTimer.Stop();

                        FrameRate.Stop();
                        break;

                    #endregion

                    case Stage.Paused:

                        #region To record again

                        Stage = Stage.Recording;
                        Title = "ScreenToGif";
                        FrequencyIntegerUpDown.IsEnabled = false;

                        DiscardButton.BeginStoryboard(this.FindStoryboard("HideDiscardStoryboard"), HandoffBehavior.Compose);

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
        }

        private async Task Snap()
        {
            #region If region not yet selected

            if (Region.IsEmpty)
            {
                await PickRegion(ReselectSplitButton.SelectedIndex == 1 ? SelectControl.ModeType.Window : ReselectSplitButton.SelectedIndex == 2 ? SelectControl.ModeType.Fullscreen : SelectControl.ModeType.Region);
                return;
            }

            #endregion

            if (Project == null || Project.Frames.Count == 0)
            {
                try
                {
                    ReselectSplitButton.BeginStoryboard(this.FindStoryboard("HideReselectStoryboard"), HandoffBehavior.Compose);
                    DiscardButton.BeginStoryboard(this.FindStoryboard("ShowDiscardStoryboard"), HandoffBehavior.Compose);

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
            }
            catch (Exception e)
            {
                IsRecording = false;

                LogWriter.Log(e, "Impossible to capture the manual screenshot.");
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible.Info"), e);
            }

            #endregion
        }

        private async Task Stop()
        {
            try
            {
                StopButton.IsEnabled = false;
                RecordPauseButton.IsEnabled = false;
                DiscardButton.IsEnabled = false;
                Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.Stopping");
                Cursor = Cursors.AppStarting;

                _captureTimer.Stop();
                FrameRate.Stop();

                if (_capture != null)
                    await _capture.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && Project?.Any == true)
                {
                    #region Stop

                    if (UserSettings.All.AsyncRecording)
                        _stopRequested = true;

                    await Task.Delay(100);

                    Close();

                    #endregion
                }
                else if (Stage == Stage.PreStarting || Stage == Stage.Snapping || Project?.Any != true)
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    if (Stage == Stage.PreStarting)
                    {
                        //Stop the pre-start timer to kill pre-start warming up
                        _preStartTimer.Stop();
                    }

                    //Only returns to the stopped stage if it was recording.
                    Stage = Stage == Stage.Snapping ? Stage.Snapping : Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FrequencyIntegerUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;

                    IsRecording = false;
                    Topmost = true;

                    ReselectSplitButton.BeginStoryboard(this.FindStoryboard("ShowReselectStoryboard"), HandoffBehavior.Compose);

                    #endregion
                }
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
                    StopButton.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    DiscardButton.IsEnabled = true;
                }
            }
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
            }
            else
            {
                //Capture with BitBlt.
                _capture = UserSettings.All.UseMemoryCache ? new CachedCapture() : new ImageCapture();
            }

            _capture.OnError += async exception =>
            {
                //Pause the recording and show the error.  
                await RecordPause();

                ErrorDialog.Ok("ScreenToGif", LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), exception.Message, exception);
            };

            _capture.Start(GetCaptureInterval(), (int)CaptureRegion.X, (int)CaptureRegion.Y, (int)CaptureRegion.Width, (int)CaptureRegion.Height, _regionSelection.Scale, Project);
        }

        private ICapture GetDirectCapture()
        {
            return UserSettings.All.UseMemoryCache ? new DirectCachedCapture() : new DirectImageCapture();
        }

        private async Task PickRegion(SelectControl.ModeType mode)
        {
            _regionSelection.Hide();
            Hide();

            var region = await RegionSelectHelper.Select(mode, Region);

            if (region != Rect.Empty)
            {
                UserSettings.All.SelectedRegion = Region = region;

                DisplaySelection(MoveCommandPanel(), mode == SelectControl.ModeType.Fullscreen);
            }
            else
            {
                DisplaySelection(null, mode == SelectControl.ModeType.Fullscreen);
            }
            
            Show();
        }

        private void DisplaySelection(Monitor display = null, bool isFullscreen = false)
        {
            if (Region.IsEmpty)
            {
                if (_regionSelection.IsVisible)
                    _regionSelection.Hide();
            }
            else
            {
                _regionSelection.Select(Region, display, isFullscreen);
            }
        }

        private Monitor MoveCommandPanel()
        {
            if (Region.Width < 10 || Region.Height < 10)
                return null;

            //Get monitors, ordered by how much they intersect the selected region.
            var monitors = Monitor.AllMonitorsGranular().OrderByDescending(f =>
            {
                var x = Math.Max(Region.Left, f.Bounds.Left);
                var num1 = Math.Min(Region.Left + Region.Width, f.Bounds.Right);
                var y = Math.Max(Region.Top, f.Bounds.Top);
                var num2 = Math.Min(Region.Top + Region.Height, f.Bounds.Bottom);

                if (num1 >= x && num2 >= y)
                    return num1 - x + num2 - y;

                return 0;
            }).ToList();

            //Repositions the capture controls near the selected region, in order to stay away from the capture. If no space available on the nearest screen, try others.
            var count = 0;
            foreach (var monitor in monitors)
            {
                #region Calculate the available spaces for all four sides 

                //If the selected region is passing the bottom edge of the display, it means that there are no space available on the bottom.
                //If the selected region is inside (bottom is below the top most part), it means that there are space available.
                //If none above, it means that the region is not located inside the screen.
                
                var bottomSpace = Region.Bottom > monitor.Bounds.Bottom ? 0 :
                    Region.Bottom > monitor.Bounds.Top ? monitor.Bounds.Bottom - Region.Bottom :
                        monitor.Bounds.Height;

                var topSpace = Region.Top < monitor.Bounds.Top ? 0 :
                    Region.Top < monitor.Bounds.Bottom ? Region.Top - monitor.Bounds.Top :
                        monitor.Bounds.Height;

                var leftSpace = Region.Left < monitor.Bounds.Left ? 0 :
                    Region.Left < monitor.Bounds.Right ? Region.Left - monitor.Bounds.Left :
                        monitor.Bounds.Width;
                
                var rightSpace = Region.Right > monitor.Bounds.Right ? 0 :
                    Region.Right > monitor.Bounds.Left ? monitor.Bounds.Right - Region.Right :
                        monitor.Bounds.Width;

                #endregion

                //When there's no space left on the monitor where the selection is located, try the other monitors (if the settings allows that).
                if (count != 0)
                {
                    var toTop = monitor.Bounds.Bottom <= monitors[0].Bounds.Top;
                    var toLeft = monitor.Bounds.Right <= monitors[0].Bounds.Left;
                    var toRight = monitor.Bounds.Left >= monitors[0].Bounds.Right;
                    var toBottom = monitor.Bounds.Top >= monitors[0].Bounds.Bottom;

                    //Bottom.
                    if (bottomSpace > 0 && toBottom)
                    {
                        double left;

                        if (leftSpace > 0)
                            left = monitor.Bounds.Left + leftSpace - ActualWidth - 10;
                        else if (rightSpace > 0)
                            left = monitor.Bounds.Right - rightSpace + 10;
                        else
                            left = Region.Left + Region.Width / 2 - ActualWidth / 2;

                        return MoveRegionTo(monitors.FirstOrDefault(), left, monitor.Bounds.Bottom - bottomSpace + 10);
                    }

                    //Top.
                    if (topSpace > 0 && toTop)
                    {
                        double left;

                        if (leftSpace > 0)
                            left = monitor.Bounds.Left + leftSpace - ActualWidth - 10;
                        else if (rightSpace > 0)
                            left = monitor.Bounds.Right - rightSpace + 10;
                        else
                            left = Region.Left + Region.Width / 2 - ActualWidth / 2;

                        return MoveRegionTo(monitors.FirstOrDefault(), left, monitor.Bounds.Top + topSpace - ActualHeight - 10);
                    }

                    //Left.
                    if (leftSpace > 0 && toLeft)
                    {
                        double top;

                        if (topSpace > 0)
                            top = monitor.Bounds.Top + topSpace - ActualHeight - 10;
                        else if (bottomSpace > 0)
                            top = monitor.Bounds.Bottom - bottomSpace + 10;
                        else
                            top = Region.Top + Region.Height / 2 - ActualHeight / 2;

                        return MoveRegionTo(monitors.FirstOrDefault(), monitor.Bounds.Left + leftSpace - ActualWidth - 10, top);
                    }

                    //Right.
                    if (rightSpace > 0 && toRight)
                    {
                        double top;

                        if (topSpace > 0)
                            top = monitor.Bounds.Top + topSpace - ActualHeight - 10;
                        else if (bottomSpace > 0)
                            top = monitor.Bounds.Bottom - bottomSpace + 10;
                        else
                            top = Region.Top + Region.Height / 2 - ActualHeight / 2;

                        return MoveRegionTo(monitors.FirstOrDefault(), monitor.Bounds.Right - rightSpace + 10, top);
                    }

                    continue;
                }

                //Bottom.
                if (bottomSpace > ActualHeight + 20)
                    return MoveRegionTo(monitor, Region.Left + Region.Width / 2 - ActualWidth / 2, Region.Bottom + 10);

                //Top.
                if (topSpace > ActualHeight + 20)
                    return MoveRegionTo(monitor, Region.Left + Region.Width / 2 - ActualWidth / 2, Region.Top - ActualHeight - 10);

                //Left.
                if (leftSpace > ActualWidth + 20)
                    return MoveRegionTo(monitor, Region.Left - ActualWidth - 10, Region.Top + Region.Height / 2 - ActualHeight / 2);

                //Right.
                if (rightSpace > ActualWidth + 20)
                    return MoveRegionTo(monitor, Region.Right + 10, Region.Top + Region.Height / 2 - ActualHeight / 2);

                count++;

                //When there's no space left on the same monitor as the selected region, try or not to find space in others.
                if (!UserSettings.All.FallThroughOtherScreens)
                    break;
            }

            //No space available, simply center on the selected region.
            return MoveRegionTo(monitors.FirstOrDefault(), Region.Left + Region.Width / 2 - ActualWidth / 2, Region.Top + Region.Height / 2 - ActualHeight / 2);
        }

        private Monitor MoveRegionTo(Monitor monitor, double left, double top)
        {
            //First move the region to the final monitor, so that the UI scale can be adjusted.
            Left = monitor?.Bounds.Left ?? 0;
            Top = monitor?.Bounds.Top ?? 0;

            //Move the command window to the final place.
            Left = left;
            Top = top;

            //Return the final monitor, so that the region selection can be moved too.
            return monitor;
        }

        /// <summary>
        /// Move the selection region to the closest screen when outside of any.
        /// </summary>
        private Rect MoveToClosestScreen()
        {
            //If the position was never set.
            if (Region.IsEmpty)
                return Rect.Empty;

            var top = Region.Top;
            var left = Region.Left;

            //TODO: Take into consideration that each screen has it's own DPI.
            //Will the size change?

            ////The catch here is to get the closest monitor from current Top/Left point. 
            //var monitors = Monitor.AllMonitorsScaled(this.Scale());
            //var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

            //if (closest == null)
            //    throw new Exception("It was not possible to move the current selected region to the closest monitor.");

            ////To much to the Left.
            //if (closest.WorkingArea.Left > Region.Left + Region.Width - 100)
            //    left = closest.WorkingArea.Left;

            ////Too much to the top.
            //if (closest.WorkingArea.Top > Region.Top + Region.Height - 100)
            //    top = closest.WorkingArea.Top;

            ////Too much to the right.
            //if (closest.WorkingArea.Right < Region.Left + 100)
            //    left = closest.WorkingArea.Right - Region.Width;

            ////Too much to the bottom.
            //if (closest.WorkingArea.Bottom < Region.Top + 100)
            //    top = closest.WorkingArea.Bottom - Region.Height;

            return new Rect(left, top, Region.Width, Region.Height);
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

        #endregion


        private void DetectCaptureFrequency()
        {
            switch (UserSettings.All.CaptureFrequency)
            {
                case CaptureFrequency.Manual:
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Manual.Short");
                    FrequencyIntegerUpDown.Visibility = Visibility.Collapsed;
                    FrequencyViewbox.Visibility = Visibility.Collapsed;
                    AdjustToManual();
                    break;
                case CaptureFrequency.PerSecond:
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Fps.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fps");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fps.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                case CaptureFrequency.PerMinute:
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Fpm.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fpm.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;

                default: //PerHour.
                    FrequencyButton.SetResourceReference(ImageButton.TextProperty, "S.Recorder.Fph.Short");
                    FrequencyIntegerUpDown.SetResourceReference(ToolTipProperty, "S.Recorder.Fph");
                    FrequencyViewbox.SetResourceReference(ToolTipProperty, "S.Recorder.Fph.Range");
                    FrequencyIntegerUpDown.Visibility = Visibility.Visible;
                    FrequencyViewbox.Visibility = Visibility.Visible;
                    AdjustToAutomatic();
                    break;
            }
        }

        private void AdjustToManual()
        {
            Stage = Stage.Snapping;
            Title = "ScreenToGif";
        }

        private void AdjustToAutomatic()
        {
            if (Project?.Frames?.Count > 0)
            {
                Stage = Stage.Paused;
                Title = LocalizationHelper.Get("S.Recorder.Paused");
            }
            else
            {
                Stage = Stage.Stopped;
                Title = "ScreenToGif";
            }

            FrameRate.Stop();

            //Register the events
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
                case CaptureFrequency.PerMinute:
                    return UserSettings.All.PlaybackDelayMinute;
                case CaptureFrequency.PerHour:
                    return UserSettings.All.PlaybackDelayHour;
                default: //When the capture is 'PerSecond', the fixed delay is set to use the current framerate.
                    return 1000 / FrequencyIntegerUpDown.Value;
            }
        }
    }
}