using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Model;

namespace ScreenToGif.Windows.Other
{
    public partial class RecorderNew : Window
    {
        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;
        
        /// <summary>
        /// The project information about the current recording.
        /// </summary>
        internal ProjectInfo Project { get; set; }

        /// <summary>
        /// Lists of pressed keys.
        /// </summary>
        private readonly List<SimpleKeyGesture> _keyList = new List<SimpleKeyGesture>();

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The action to be executed after closing this Window.
        /// </summary>
        public ExitAction ExitArg = ExitAction.Return;

        private Point _latestPosition;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty IsPickingRegionProperty = DependencyProperty.Register("IsPickingRegion", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty WasRegionPickedProperty = DependencyProperty.Register("WasRegionPicked", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register("IsRecording", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register("IsDragging", typeof(bool), typeof(RecorderNew),
            new PropertyMetadata(false));

        public static readonly DependencyProperty RegionProperty = DependencyProperty.Register("Region", typeof(Rect), typeof(RecorderNew),
            new PropertyMetadata(Rect.Empty));

        public static readonly DependencyProperty StageProperty = DependencyProperty.Register("Stage", typeof(Stage), typeof(RecorderNew),
            new FrameworkPropertyMetadata(Stage.Stopped));

        #endregion

        #region Properties

        public bool IsPickingRegion
        {
            get { return (bool)GetValue(IsPickingRegionProperty); }
            set { SetValue(IsPickingRegionProperty, value); }
        }

        public bool WasRegionPicked
        {
            get { return (bool)GetValue(WasRegionPickedProperty); }
            set { SetValue(WasRegionPickedProperty, value); }
        }

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage
        {
            get { return (Stage)GetValue(StageProperty); }
            set { SetValue(StageProperty, value); }
        }

        public bool IsRecording
        {
            get { return (bool)GetValue(IsRecordingProperty); }
            set { SetValue(IsRecordingProperty, value); }
        }

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        public Rect Region
        {
            get { return (Rect)GetValue(RegionProperty); }
            set { SetValue(RegionProperty, value); }
        }

        #endregion

        public RecorderNew(bool hideBackButton = true)
        {
            InitializeComponent();

            BackButton.Visibility = hideBackButton ? Visibility.Collapsed : Visibility.Visible;

            #region Fill entire working space

            Left = 0;
            Top = 0;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            #endregion

            #region Global Hook

            try
            {
                //_actHook = new UserActivityHook(true, true); //true for the mouse, true for the keyboard.
                //_actHook.KeyDown += KeyHookTarget;
                //_actHook.OnMouseActivity += MouseHookTarget;
            }
            catch (Exception) { }

            #endregion

            #region Temporary folder

            //If never configurated.
            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
                UserSettings.All.TemporaryFolder = Path.GetTempPath();

            #endregion
        }

        #region Events

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
            PickRegion();

            SelectControl.Mode = SelectControl.ModeType.Region;
        }

        private void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion();

            SelectControl.Mode = SelectControl.ModeType.Window;
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            PickRegion();

            SelectControl.Mode = SelectControl.ModeType.Fullscreen;

            //TODO: Pick the screen where the main border is located.
        }


        private void SelectControl_SelectionAccepted(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            Region = SelectControl.Selected;

            AdjustControls();

            WasRegionPicked = true;
        }

        private void SelectControl_SelectionCanceled(object sender, RoutedEventArgs routedEventArgs)
        {
            EndPickRegion();

            //TODO: ?
        }


        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                return;

            if (Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
                RecordPauseButton_Click(null, null);
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut)
                StopButton_Click(null, null);
            else if ((Stage == Stage.Paused || Stage == Stage.Snapping) && Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
                DiscardButton_Click(null, null);
            else
                _keyList.Add(new SimpleKeyGesture(e.Key, Keyboard.Modifiers));
        }

        /// <summary>
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private void MouseHookTarget(object sender, CustomMouseEventArgs args)
        {
            if (WindowState == WindowState.Minimized)
                return;

            _recordClicked = (args.Button == MouseButton.Left || args.Button == MouseButton.Right) || (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed);
        }


        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            //if (!UserSettings.All.SnapshotMode)
            //    RecordPause();
            //else
            //    Snap();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            //Stop();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            //_capture.Stop();
            //FrameRate.Stop();
            //FrameCount = 0;
            //Stage = Stage.Stopped;

            //OutterGrid.IsEnabled = false;
            //Cursor = Cursors.AppStarting;

            //_discardFramesDel = Discard;
            //_discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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

            //SystemEvents.PowerModeChanged -= System_PowerModeChanged;

            //#region Stops the timers

            //if (Stage != (int)Stage.Stopped)
            //{
            //    _preStartTimer.Stop();
            //    _preStartTimer.Dispose();

            //    _capture.Stop();
            //    _capture.Dispose();
            //}

            ////Garbage Collector Timer.
            //_garbageTimer.Stop();

            //#endregion

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

            base.OnKeyDown(e);
        } 

        private void PickRegion()
        {
            //Reset the values.
            SelectControl.Retry();

            IsPickingRegion = true;
        }

        private void EndPickRegion()
        {
            IsPickingRegion = false;

        }

        private void AdjustControls()
        {
            DashedRectangle.Visibility = Visibility.Visible;

            //await Task.Delay(1000);
            //TODO:Check if out of bounds (because of multi monitors gaps).

            if (MainCanvas.ActualHeight - (Region.Top + Region.Height) > 100)
            {
                //Show at the bottom of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                Canvas.SetTop(MainBorder, Region.Bottom + 10);
                return;
            }

            if (Region.Top > 100)
            {
                //Show on top of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                Canvas.SetTop(MainBorder, Region.Top - MainBorder.ActualHeight - 10);
                return;
            }

            if (Region.Left > 100)
            {
                //Show to the left of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left - MainBorder.ActualWidth - 10);
                Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                return;
            }

            if (MainCanvas.ActualWidth - (Region.Left + Region.Width) > 100)
            {
                //Show to the right of the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Right + 10);
                Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
                return;
            }

            if (Region.Width > 100 && Region.Height > 100)
            {
                //Show inside the main rectangle.
                Canvas.SetLeft(MainBorder, Region.Left + Region.Width / 2 - MainBorder.ActualWidth / 2);
                Canvas.SetTop(MainBorder, Region.Top + Region.Height / 2 - MainBorder.ActualHeight / 2);
            }
        }

        #endregion

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Transform to a command. Validate closing.

            Close();
        }

        private void ReselectButton_Click(object sender, RoutedEventArgs e)
        {
            WasRegionPicked = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Center the main UI

            var screen = Monitor.AllMonitors.FirstOrDefault(x => x.Bounds.Contains(Mouse.GetPosition(this))) ??
                         Monitor.AllMonitors.FirstOrDefault(x => x.IsPrimary);

            if (screen != null)
            {
                MainBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                MainBorder.Arrange(new Rect(MainBorder.DesiredSize));

                Canvas.SetLeft(MainBorder, (screen.WorkingArea.Left + screen.WorkingArea.Width / 2) - (MainBorder.ActualWidth / 2));
                Canvas.SetTop(MainBorder, screen.WorkingArea.Top + screen.WorkingArea.Height / 2 - MainBorder.ActualHeight / 2);
            }

            #endregion


        }
    }
}