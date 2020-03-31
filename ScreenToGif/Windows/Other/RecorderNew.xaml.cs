using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Capture;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Monitor = ScreenToGif.Util.Monitor;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows.Other
{
    public partial class RecorderNew : RecorderWindow
    {
        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The rectangle to record.
        /// </summary>
        private Rect _rect = Rect.Empty;

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

        private Task<int> _captureTask;

        private Point _latestPosition;

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

        #region Timer

        private readonly Timer _preStartTimer = new Timer();
        private Timer _captureTimer = new Timer();

        private readonly System.Timers.Timer _garbageTimer = new System.Timers.Timer();
        private readonly Timer _followTimer = new Timer();
        private readonly Timer _showBorderTimer = new Timer();
        
        #endregion

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register(nameof(IsPickingRegion), typeof(bool), typeof(RecorderNew), new PropertyMetadata(false));
        public static readonly DependencyProperty WasRegionPickedProperty = DependencyProperty.Register(nameof(WasRegionPicked), typeof(bool), typeof(RecorderNew), new PropertyMetadata(false));
        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(RecorderNew), new PropertyMetadata(false));
        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(RecorderNew), new PropertyMetadata(false));
        public static readonly DependencyProperty IsFollowingProperty = DependencyProperty.Register(nameof(IsFollowing), typeof(bool), typeof(RecorderNew), new PropertyMetadata(false, IsFollowing_PropertyChanged));
        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register(nameof(Region), typeof(Rect), typeof(RecorderNew), new PropertyMetadata(Rect.Empty));
        
        #endregion

        #region Properties

        public bool IsPickingRegion
        {
            get => (bool)GetValue(IsPickingRegionProperty);
            set => SetValue(IsPickingRegionProperty, value);
        }

        public bool WasRegionPicked
        {
            get => (bool)GetValue(WasRegionPickedProperty);
            set => SetValue(WasRegionPickedProperty, value);
        }


        public bool IsRecording
        {
            get => (bool)GetValue(IsRecordingProperty);
            set => SetValue(IsRecordingProperty, value);
        }

        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            set => SetValue(IsDraggingProperty, value);
        }

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
        public Rect ScreenRegion
        {
            get
            {
                if (Region.IsEmpty)
                    return Region;

                var region = Region;
                region.Offset(Left, Top);
                return region;
            }
        }

        #endregion

        public RecorderNew(bool hideBackButton = true)
        {
            InitializeComponent();

            BackButton.Visibility = hideBackButton ? Visibility.Collapsed : Visibility.Visible;

            #region Fill entire working space

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            #endregion

            //Config Timers - Todo: organize
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

        #region Events

        private void Window_Activated(object sender, EventArgs e)
        {
            IsFollowing = UserSettings.All.CursorFollowing;

            if (IsFollowing && UserSettings.All.FollowShortcut == Key.None)
            {
                UserSettings.All.CursorFollowing = IsFollowing = false;

                Dialog.Ok(LocalizationHelper.Get("S.StartUp.Recorder"), LocalizationHelper.Get("S.Options.Warning.Follow.Header"),
                    LocalizationHelper.Get("S.Options.Warning.Follow.Message"), Icons.Warning);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Factory.StartNew(UpdateScreenDpi);

            await UpdatePositioning(true);
            
            #region Timers

            _garbageTimer.Interval = 3000;
            _garbageTimer.Elapsed += GarbageTimer_Tick;
            _garbageTimer.Start();

            _showBorderTimer.Interval = 500;
            _showBorderTimer.Tick += ShowBorderTimer_Tick;

            _followTimer.Tick += FollowTimer_Tick;

            #endregion

            #region If Snapshot

            if (UserSettings.All.SnapshotMode)
                EnableSnapshot_Executed(null, null);

            if (UserSettings.All.CursorFollowing)
                Follow();

            #endregion
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            //TODO: Detect that the window was minimized before. E01
            if (WindowState != WindowState.Minimized && Stage == Stage.Recording && SelectControl.Mode == SelectControl.ModeType.Fullscreen)
            {
                RecordPauseButton_Click(sender, null);
                Topmost = true;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            #region Validate

            if (Stage != Stage.Stopped && Stage != Stage.Snapping)
                return;

            #endregion

            DialogResult = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            #region Validate

            if (Stage != Stage.Stopped && Stage != Stage.Snapping)
                return;

            #endregion

            Close();
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var draggableControl = sender as Border;

            if (draggableControl == null)
                return;

            IsDragging = true;

            _latestPosition = e.GetPosition(MainBorder);
            draggableControl.CaptureMouse();

            e.Handled = true;
        }

        private void MainBorder_MouseMove(object sender, MouseEventArgs e)
        {
            var draggableControl = sender as Border;

            if (!IsDragging || draggableControl == null) return;

            var currentPosition = e.GetPosition(Parent as UIElement);

            //TODO: Validate position.
            //TODO: Detect video options change, reposition the MainBorder.

            Canvas.SetLeft(MainBorder, currentPosition.X - _latestPosition.X);
            Canvas.SetTop(MainBorder, currentPosition.Y - _latestPosition.Y);

            e.Handled = true;
        }

        private void MainBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsDragging = false;

            var draggable = sender as Border;
            draggable?.ReleaseMouseCapture();

            e.Handled = true;
        }


        private void RegionButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion(SelectControl.ModeType.Region);
        }

        private void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion(SelectControl.ModeType.Window);
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion(SelectControl.ModeType.Fullscreen);
        }


        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            UserSettings.All.SelectedRegion = Region = SelectControl.Selected;

            WasRegionPicked = true;

            AdjustControls();
        }

        private void SelectControl_SelectionCanceled(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            AdjustControls();
        }


        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (Stage == Stage.SelectingRegion || (WindowState == WindowState.Minimized && SelectControl.Mode != SelectControl.ModeType.Fullscreen) || Region.IsEmpty || !WasRegionPicked)
                return;

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
            if (WindowState == WindowState.Minimized)
                return;

            _recordClicked = args.LeftButton == MouseButtonState.Pressed || args.RightButton == MouseButtonState.Pressed || args.MiddleButton == MouseButtonState.Pressed;

            _posX = (int)Math.Round(args.PosX / _scale, MidpointRounding.AwayFromZero);
            _posY = (int)Math.Round(args.PosY / _scale, MidpointRounding.AwayFromZero);
        }

        private async void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSettings.All.SnapshotMode)
                await RecordPause();
            else
                await Snap();
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            await Stop();
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

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }

        private void EnableSnapshot_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (Stage == Stage.Stopped || Stage == Stage.Snapping || Stage == Stage.Paused) && RecordControlsGrid.IsVisible;
        }


        private static void IsFollowing_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is RecorderNew rec))
                return;

            rec.Follow();
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var wasSnapshot = UserSettings.All.SnapshotMode;

            var options = new Options();
            options.ShowDialog();

            //Enables or disables the snapshot mode.
            if (wasSnapshot != UserSettings.All.SnapshotMode)
                EnableSnapshot_Executed(sender, e);
        }

        private void EnableSnapshot_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UserSettings.All.SnapshotMode)
            {
                #region SnapShot Recording

                //Set to Snapshot Mode, change the text of the record button to "Snap" and every press of the button, takes a screenshot.
                Stage = Stage.Snapping;
                Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.Snapshot");

                #endregion
            }
            else
            {
                #region Normal Recording

                if (_capture != null)
                    _capture.SnapDelay = null;

                if (Project?.Frames?.Count > 0)
                {
                    Stage = Stage.Paused;
                    Title = LocalizationHelper.Get("S.Recorder.Paused");

                    DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);
                }
                else
                {
                    Stage = Stage.Stopped;
                    Title = "ScreenToGif";
                }

                FrameRate.Stop();

                #region Register the events

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

                #endregion

                #endregion
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
            await Task.Factory.StartNew(UpdateScreenDpi);

            await UpdatePositioning();
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
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

        #endregion

        #region Methods

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (!IsPickingRegion)
                    Close();

                IsPickingRegion = false;
            }
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                SelectControl.Accept();
            }

            base.OnKeyDown(e);
        }

        private void PickRegion(SelectControl.ModeType mode)
        {           
            SelectControl.Mode = mode;
            SelectControl.BackImage = CaptureBackground();

            //Reset the values.
            SelectControl.Scale = _scale;
            SelectControl.Retry();

            if (SelectControl.Mode == SelectControl.ModeType.Region)
                SelectControl.Selected = Region;

            IsPickingRegion = true;

            //Set the current size of the screen.
            SelectControl.Width = Width;
            SelectControl.Height = Height;
        }

        private void EndPickRegion()
        {
            IsPickingRegion = false;
        }

        private void AdjustControls()
        {
            if (Region.Width < 10 || Region.Height < 10)
                return;

            if (MainBorder.ActualHeight < 1)
            {
                MainBorder.Measure(new Size(Width, Height));
                MainBorder.Arrange(new Rect(MainBorder.DesiredSize));
            }

            //Get monitors, ordered by how much they intersect the selected region. Btw, should I scale the size?
            var monitors = Monitor.AllMonitorsScaled(_scale, true).OrderByDescending(f =>
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

                var bottom1 = Region.Bottom > monitor.Bounds.Bottom ? 0 : 
                    Region.Bottom > monitor.Bounds.Top ? monitor.Bounds.Bottom - Region.Bottom : 
                        monitor.Bounds.Height;
                var top1 = Region.Top < monitor.Bounds.Top ? 0 : 
                    Region.Top < monitor.Bounds.Bottom ? Region.Top - monitor.Bounds.Top : 
                        monitor.Bounds.Height;
                var left1 = Region.Left < monitor.Bounds.Left ? 0 :
                    Region.Left < monitor.Bounds.Right ? Region.Left - monitor.Bounds.Left :
                        monitor.Bounds.Width;
                var right1 = Region.Right > monitor.Bounds.Right ? 0 :
                    Region.Right > monitor.Bounds.Left ? monitor.Bounds.Right - Region.Right :
                        monitor.Bounds.Width;

                if (count != 0)
                {
                    var toTop = monitor.Bounds.Bottom <= monitors[0].Bounds.Top;
                    var toLeft = monitor.Bounds.Right <= monitors[0].Bounds.Left;
                    var toRight = monitor.Bounds.Left >= monitors[0].Bounds.Right;
                    var toBottom = monitor.Bounds.Top >= monitors[0].Bounds.Bottom;

                    if (bottom1 > 0 && toBottom)
                    {
                        if (left1 > 0)
                            Canvas.SetLeft(MainBorder, monitor.Bounds.Left + left1 - MainBorder.ActualWidth - 10);
                        else if (right1 > 0)
                            Canvas.SetLeft(MainBorder, monitor.Bounds.Right - right1 + 10);
                        else
                            Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                        
                        Canvas.SetTop(MainBorder, monitor.Bounds.Bottom - bottom1 + 10);
                        return;
                    }

                    if (top1 > 0 && toTop)
                    {
                        if (left1 > 0)
                            Canvas.SetLeft(MainBorder, monitor.Bounds.Left + left1 - MainBorder.ActualWidth - 10);
                        else if (right1 > 0)
                            Canvas.SetLeft(MainBorder, monitor.Bounds.Right - right1 + 10);
                        else
                            Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);

                        Canvas.SetTop(MainBorder, monitor.Bounds.Top + top1 - MainBorder.ActualHeight - 10);
                        return;
                    }

                    if (left1 > 0 && toLeft)
                    {
                        Canvas.SetLeft(MainBorder, monitor.Bounds.Left + left1 - MainBorder.ActualWidth - 10);

                        if (top1 > 0)
                            Canvas.SetTop(MainBorder, monitor.Bounds.Top + top1 - MainBorder.ActualHeight - 10);
                        else if (bottom1 > 0)
                            Canvas.SetTop(MainBorder, monitor.Bounds.Bottom - bottom1 + 10);
                        else
                            Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                        return;
                    }

                    if (right1 > 0 && toRight)
                    {
                        Canvas.SetLeft(MainBorder, monitor.Bounds.Right - right1 + 10);
                        
                        if (top1 > 0)
                            Canvas.SetTop(MainBorder, monitor.Bounds.Top + top1 - MainBorder.ActualHeight - 10);
                        else if (bottom1 > 0)
                            Canvas.SetTop(MainBorder, monitor.Bounds.Bottom - bottom1 + 10);
                        else
                            Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                        return;
                    }

                    continue;
                }
                
                //Bottom.
                if (bottom1 > MainBorder.ActualHeight + 20)
                {
                    Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                    Canvas.SetTop(MainBorder, Region.Bottom + 10);
                    return;
                }

                //Top.
                if (top1 > MainBorder.ActualHeight + 20)
                {
                    Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                    Canvas.SetTop(MainBorder, Region.Top - MainBorder.ActualHeight - 10);
                    return;
                }

                //Left.
                if (left1 > MainBorder.ActualWidth + 20)
                {
                    //Show to the left of the main rectangle.
                    Canvas.SetLeft(MainBorder, Region.Left - MainBorder.ActualWidth - 10);
                    Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                    return;
                }

                //Right.
                if (right1 > MainBorder.ActualWidth + 20)
                {
                    //Show to the right of the main rectangle.
                    Canvas.SetLeft(MainBorder, Region.Right + 10);
                    Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                    return;
                }

                count++;
            }

            //No space available, simply center on the selected region.
            Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
            Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
        }

        private BitmapSource CaptureBackground()
        {
            //A 7 pixel border is added to allow the crop by the magnifying glass.
            return Native.CaptureBitmapSource((int)Math.Round((Width + 14) * _scale), (int)Math.Round((Height + 14) * _scale),
                (int)Math.Round((Left - 7) * _scale), (int)Math.Round((Top - 7) * _scale));
        }

        private void UnregisterEvents()
        {
            _captureTimer.Tick -= Normal_Elapsed;
            _captureTimer.Tick -= NormalAsync_Elapsed;

            _captureTimer.Tick -= Cursor_Elapsed;
            _captureTimer.Tick -= CursorAsync_Elapsed;
        }

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        internal async Task RecordPause()
        {
            try
            {
                switch (Stage)
                {
                    case Stage.Stopped:

                        #region If region not yet selected

                        if (ScreenRegion.IsEmpty)
                        {
                            PickRegion(ReselectSplitButton.SelectedIndex == 1 ? SelectControl.ModeType.Window : ReselectSplitButton.SelectedIndex == 2 ? SelectControl.ModeType.Fullscreen : SelectControl.ModeType.Region);
                            return;
                        }

                        #endregion

                        #region To Record

                        _captureTimer = new Timer { Interval = 1000 / FpsIntegerUpDown.Value };

                        Project = new ProjectInfo().CreateProjectFolder(ProjectByType.ScreenRecorder);

                        _keyList.Clear();
                        FrameCount = 0;

                        await Task.Factory.StartNew(UpdateScreenDpi);

                        _rect = ScreenRegion.Scale(_scale).Offset(Util.Other.RoundUpValue(_scale));

                        await PrepareNewCapture();

                        FpsIntegerUpDown.IsEnabled = false;

                        IsRecording = true;
                        Topmost = true;

                        //TODO: Adjust fullscreen recording usability.
                        //TODO: Detect that the window needs to be minimized. E01
                        if (SelectControl.Mode == SelectControl.ModeType.Fullscreen)
                        {
                            WindowState = WindowState.Minimized;
                            Topmost = false;
                        }

                        FrameRate.Start(_captureTimer.Interval);
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
                                #region If Show Cursor

                                if (UserSettings.All.AsyncRecording)
                                    _captureTimer.Tick += CursorAsync_Elapsed;
                                else
                                    _captureTimer.Tick += Cursor_Elapsed;

                                _captureTimer.Start();


                                Stage = Stage.Recording;

                                #endregion
                            }
                            else
                            {
                                #region If Not

                                if (UserSettings.All.AsyncRecording)
                                    _captureTimer.Tick += NormalAsync_Elapsed;
                                else
                                    _captureTimer.Tick += Normal_Elapsed;

                                _captureTimer.Start();

                                Stage = Stage.Recording;

                                #endregion
                            }
                        }
                        break;

                    #endregion

                    #endregion

                    case Stage.Recording:

                        #region To Pause

                        Stage = Stage.Paused;
                        Title = LocalizationHelper.Get("S.Recorder.Paused");

                        DiscardButton.BeginStoryboard(this.FindStoryboard("ShowDiscardStoryboard"), HandoffBehavior.Compose);

                        _captureTimer.Stop();

                        FrameRate.Stop();
                        break;

                    #endregion

                    case Stage.Paused:

                        #region To Record Again

                        Stage = Stage.Recording;
                        Title = "ScreenToGif";

                        DiscardButton.BeginStoryboard(this.FindStoryboard("HideDiscardStoryboard"), HandoffBehavior.Compose);

                        FrameRate.Start(_captureTimer.Interval);

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

            if (ScreenRegion.IsEmpty)
            {
                PickRegion(ReselectSplitButton.SelectedIndex == 1 ? SelectControl.ModeType.Window : ReselectSplitButton.SelectedIndex == 2 ? SelectControl.ModeType.Fullscreen : SelectControl.ModeType.Region);
                return;
            }

            #endregion

            if (Project == null || Project.Frames.Count == 0)
            {
                try
                {
                    _rect = ScreenRegion.Scale(_scale).Offset(Util.Other.RoundUpValue(_scale));

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

            if (_capture != null)
                _capture.SnapDelay = UserSettings.All.SnapshotDefaultDelay;

            #region Take the screenshot

            var limit = 0;
            do
            {
                if (UserSettings.All.ShowCursor)
                    Cursor_Elapsed(null, null);
                else
                    Normal_Elapsed(null, null);

                if (limit > 5)
                {
                    ErrorDialog.Ok(Title, LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible"), LocalizationHelper.Get("S.Recorder.Warning.CaptureNotPossible.Info"), null);
                    return;
                }

                limit++;
            }
            while (FrameCount == 0);

            #endregion
        }

        /// <summary>
        /// Stops the recording or the Pre-Start countdown.
        /// </summary>
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

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && Project.Any)
                {
                    #region Stop

                    if (UserSettings.All.AsyncRecording)
                        _stopRequested = true;

                    await Task.Delay(100);

                    Close();

                    #endregion
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !Project.Any)
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
                    FpsIntegerUpDown.IsEnabled = true;
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
                    StopButton.IsEnabled = true;
                    Cursor = Cursors.Arrow;
                    StopButton.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    DiscardButton.IsEnabled = true;
                }
            }
        }


        private void UpdateScreenDpi()
        {
            try
            {
                var source = Dispatcher.Invoke(() => PresentationSource.FromVisual(this));

                if (source?.CompositionTarget != null)
                    _scale = Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);
            }
            finally
            {
                GC.Collect(1);
            }
        }

        private async Task UpdatePositioning(bool startup = false)
        {
            #region Fill entire working space

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            #endregion

            var monitors = Monitor.AllMonitorsScaled(_scale, true);

            //When loading the recorder, the status controls are not well positioned.
            //When loading, if the selection extends top to bottom or if it crosses between two monitors, the selection will be lost.

            if (!startup)
            {
                //When in selection mode, cancel selection.
                if (IsPickingRegion)
                    SelectControl.Cancel();
                
                if (Stage == Stage.PreStarting)
                    await Stop();
                else if (Stage == Stage.Recording)
                    await RecordPause();

                //TODO: When discarding?

                if (Stage == Stage.Paused || Stage == Stage.Stopped)
                {
                    //Move region to the closest screen.
                    Region = MoveToClosestScreen();

                    if (Region.IsEmpty)
                        WasRegionPicked = true;
                    
                    //TODO: What if the region was bigger than now?
                }
            }
            else
            {
                #region Previously selected region

                //If a region was previously selected.
                if (!UserSettings.All.SelectedRegion.IsEmpty)
                {
                    //Check if the monitor still exists.
                    if (monitors.Any(x => x.Bounds.Contains(UserSettings.All.SelectedRegion)))
                    {
                        Region = UserSettings.All.SelectedRegion;
                        WasRegionPicked = true;
                    }
                }

                #endregion
            }

            SelectControl.Scale = _scale;
            SelectControl.Width = Width;
            SelectControl.Height = Height;
            
            SelectControl.Measure(new Size(Width, Height));
            SelectControl.Arrange(new Rect(new Point(0,0), new Size(Width, Height)));
            SelectControl.UpdateLayout();

            #region Adjust the position of the main controls

            if (Region.IsEmpty)
            {
                var screen = monitors.FirstOrDefault(x => x.Bounds.Contains(Native.GetMousePosition(_scale, Left, Top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

                if (screen == null)
                    throw new Exception("It was not possible to get a list of known screens.");

                //Update the main UI size.
                MainBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                MainBorder.Arrange(new Rect(MainBorder.DesiredSize));

                Canvas.SetLeft(MainBorder, (screen.WorkingArea.Left + screen.WorkingArea.Width / 2) - (MainBorder.ActualWidth / 2));
                Canvas.SetTop(MainBorder, screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - MainBorder.ActualHeight / 2);
            }
            else
            {
                AdjustControls();
            }

            MainCanvas.Visibility = Visibility.Visible;

            #endregion
        }

        private Rect MoveToClosestScreen()
        {
            //If the position was never set.
            if (Region.IsEmpty)
                return Rect.Empty;

            var top = Region.Top;
            var left = Region.Left;

            //The catch here is to get the closest monitor from current Top/Left point. 
            var monitors = Monitor.AllMonitorsScaled(this.Scale());
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary) ?? monitors.FirstOrDefault();

            if (closest == null)
                throw new Exception("It was not possible to move the current selected region to the closest monitor.");

            //To much to the Left.
            if (closest.WorkingArea.Left > Region.Left + Region.Width - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > Region.Top + Region.Height - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < Region.Left + 100)
                left = closest.WorkingArea.Right - Region.Width;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < Region.Top + 100)
                top = closest.WorkingArea.Bottom - Region.Height;

            return new Rect(left, top, Region.Width, Region.Height);
        }

        private void StageUpdate()
        {
            switch (Stage)
            {
                case Stage.Stopped:

                    break;
                case Stage.SelectingRegion:

                    break;
                case Stage.PreStarting:

                    break;
                case Stage.Snapping:

                    break;
                case Stage.Recording:

                    break;
            }
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
            if (_capture!= null)
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

            _capture.Start(1000 / FpsIntegerUpDown.Value, (int)_rect.X, (int)_rect.Y, (int)_rect.Width, (int)_rect.Height, _scale, Project);
        }

        private ICapture GetDirectCapture()
        {
            return UserSettings.All.UseMemoryCache ? new DirectCachedCapture() : new DirectImageCapture();
        }

        #endregion

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
                    LogWriter.Log(ex, "Delete Temp Path");
                }

                #endregion

                Project.Frames.Clear();
            }
            catch (IOException io)
            {
                LogWriter.Log(io, "Error while trying to Discard the Recording");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the recording", ex.Message));
                LogWriter.Log(ex, "Error while trying to Discard the Recording");
            }
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                //Enables the controls that are disabled while recording;
                FpsIntegerUpDown.IsEnabled = true;
                RecordControlsGrid.IsEnabled = true;

                Cursor = Cursors.Arrow;
                IsRecording = false;

                DiscardButton.BeginStoryboard(this.FindStoryboard("HideDiscardStoryboard"), HandoffBehavior.Compose);
                ReselectSplitButton.BeginStoryboard(this.FindStoryboard("ShowReselectStoryboard"), HandoffBehavior.Compose);

                if (!UserSettings.All.SnapshotMode)
                {
                    //Only display the Record text when not in snapshot mode. 
                    Title = "ScreenToGif";
                    Stage = Stage.Stopped;
                }
                else
                {
                    Stage = Stage.Snapping;
                    EnableSnapshot_Executed(null, null);
                }
            });

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
            }
            else
            {
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

                    Stage = Stage.Recording;

                    #endregion
                }
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

            //TODO: Test with 2 monitors.
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
                    MainCanvas.Visibility = Visibility.Hidden;
                    BeginStoryboard(this.FindStoryboard("HideRectangleStoryboard"), HandoffBehavior.SnapshotAndReplace);
                    _showBorderTimer.Start();
                }

                Region = new Rect(new Point((Region.X + _offsetX).Clamp(-1, Width - Region.Width + 1), (Region.Y + _offsetY).Clamp(-1, Height - Region.Height + 1)), Region.Size);
                DashedRectangle.Refresh();
            }
            
            //Rearrange the rectangles.
            _rect = ScreenRegion.Scale(_scale).Offset(Util.Other.RoundUpValue(_scale));

            if (_capture != null)
            {
                _capture.Left = (int)_rect.Left;
                _capture.Top = (int)_rect.Top;
            }
        }

        private void ShowBorderTimer_Tick(object sender, EventArgs e)
        {
            _showBorderTimer.Stop();

            AdjustControls();

            MainCanvas.Refresh();
            MainCanvas.Visibility = Visibility.Visible;

            BeginStoryboard(this.FindStoryboard("ShowRectangleStoryboard"), HandoffBehavior.Compose);
        }

        #endregion
    }
}