using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.ImageUtil.Gif.Decoder;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Windows.Other;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using Encoder = ScreenToGif.Windows.Other.Encoder;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Size = System.Windows.Size;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media.Effects;
using ScreenToGif.ImageUtil.Apng;
using ScreenToGif.Model.ExportPresets;
using ScreenToGif.Model.ExportPresets.Image;
using ScreenToGif.Model.ExportPresets.Other;
using ScreenToGif.Native;
using ScreenToGif.Settings;
using ScreenToGif.UserControls;
using VideoSource = ScreenToGif.Windows.Other.VideoSource;

namespace ScreenToGif.Windows
{
    public partial class Editor : Window, INotification, IEncoding
    {
        #region Properties

        public static readonly DependencyProperty FilledListProperty = DependencyProperty.Register(nameof(FilledList), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty NotPreviewingProperty = DependencyProperty.Register(nameof(NotPreviewing), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty TotalDurationProperty = DependencyProperty.Register(nameof(TotalDuration), typeof(TimeSpan), typeof(Editor));
        public static readonly DependencyProperty CurrentTimeProperty = DependencyProperty.Register(nameof(CurrentTime), typeof(TimeSpan), typeof(Editor));
        public static readonly DependencyProperty FrameSizeProperty = DependencyProperty.Register(nameof(FrameSize), typeof(System.Windows.Size), typeof(Editor));
        public static readonly DependencyProperty FrameScaleProperty = DependencyProperty.Register(nameof(FrameScale), typeof(int), typeof(Editor));
        public static readonly DependencyProperty AverageDelayProperty = DependencyProperty.Register(nameof(AverageDelay), typeof(double), typeof(Editor));
        public static readonly DependencyProperty FrameDpiProperty = DependencyProperty.Register(nameof(FrameDpi), typeof(double), typeof(Editor));
        public static readonly DependencyProperty IsCancelableProperty = DependencyProperty.Register(nameof(IsCancelable), typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// True if there is a value inside the list of frames.
        /// </summary>
        public bool FilledList
        {
            get => (bool)GetValue(FilledListProperty);
            set => SetValue(FilledListProperty, value);
        }

        /// <summary>
        /// True if not in preview mode.
        /// </summary>
        public bool NotPreviewing
        {
            get => (bool)GetValue(NotPreviewingProperty);
            set => SetValue(NotPreviewingProperty, value);
        }

        /// <summary>
        /// True if loading frames.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// The total duration of the animation. Used by the statistics tab.
        /// </summary>
        private TimeSpan TotalDuration
        {
            get => (TimeSpan)GetValue(TotalDurationProperty);
            set => SetValue(TotalDurationProperty, value);
        }

        /// <summary>
        /// The cumulative duration of the animation. Used by the statistics tab.
        /// </summary>
        private TimeSpan CurrentTime
        {
            get => (TimeSpan)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        /// <summary>
        /// The size of the frames. Used by the statistics tab.
        /// </summary>
        private Size FrameSize
        {
            get => (System.Windows.Size)GetValue(FrameSizeProperty);
            set => SetValue(FrameSizeProperty, value);
        }

        /// <summary>
        /// The scale of the frames in %. Used by the statistics tab.
        /// </summary>
        private int FrameScale
        {
            get => (int)GetValue(FrameScaleProperty);
            set => SetValue(FrameScaleProperty, value);
        }

        /// <summary>
        /// The average delay of the animation. Used by the statistics tab.
        /// </summary>
        private double AverageDelay
        {
            get => (double)GetValue(AverageDelayProperty);
            set => SetValue(AverageDelayProperty, value);
        }

        /// <summary>
        /// The DPI of the frames. Used by the statistics tab.
        /// </summary>
        private double FrameDpi
        {
            get => (double)GetValue(FrameDpiProperty);
            set => SetValue(FrameDpiProperty, value);
        }

        /// <summary>
        /// True if the current recording being loaded can be cancelled.
        /// </summary>
        public bool IsCancelable
        {
            get => (bool)GetValue(IsCancelableProperty);
            set => SetValue(IsCancelableProperty, value);
        }

        #endregion

        #region Variables

        /// <summary>
        /// The current project.
        /// </summary>
        public ProjectInfo Project { get; set; }

        /// <summary>
        /// The clipboard.
        /// </summary>
        public List<FrameInfo> ClipboardFrames { get; set; }

        /// <summary>
        /// Last selected frame index. Used to track users last selection and decide which frame to show.
        /// </summary>
        private int LastSelected { get; set; } = -1;

        /// <summary>
        /// True if the user was selecting frames using the FirstFrame/Previous/Next/LastFrame commands or the scroll wheel.
        /// </summary>
        private bool WasChangingSelection { get; set; }

        /// <summary>
        /// True if the user was previewing the recording.
        /// </summary>
        private bool WasPreviewing { get; set; }

        /// <summary>
        /// True if the PC is sleeping.
        /// </summary>
        private bool Slept { get; set; }

        /// <summary>
        /// True if this is the encoder window.
        /// </summary>
        public bool IsEncoderWindow { get; } = false;

        private System.Threading.CancellationTokenSource _timerPreview;
        private readonly DispatcherTimer _searchTimer;

        private Action<object, RoutedEventArgs> _applyAction = null;

        private bool _abortLoading;

        /// <summary>
        /// True if the window chrome was exended into the application area.
        /// </summary>
        private bool _chromeWasExtended = false;

        /// <summary>
        /// Lock used to prevent firing multiple times (at the same time) both the Activated/Deactivated events.
        /// </summary>
        public static readonly object ActivateLock = new object();

        #endregion

        public Editor()
        {
            InitializeComponent();
            
            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning())
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion
        }

        #region Main Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

            #region Adjust the position

            //Tries to adjust the position/size of the window, centers on screen otherwise.
            if (!UpdatePositioning())
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

            #endregion

            ScrollSynchronizer.SetScrollGroup(ZoomBoxControl.GetScrollViewer(), "Canvas");
            ScrollSynchronizer.SetScrollGroup(MainScrollViewer, "Canvas");
            ScrollSynchronizer.SetScrollGroup(BehindScrollViewer, "Canvas");

            DisplayUpdatePromoter();

            #region Load

            if (Project != null)
            {
                ShowProgress(LocalizationHelper.Get("S.Editor.Preparing"), Project.Frames.Count, true);

                Cursor = Cursors.AppStarting;
                IsLoading = true;
                IsCancelable = true;

                ActionStack.Project = Project;

                _loadFramesDel = Load;
                _loadFramesDel.BeginInvoke(LoadCallback, null);
                return;
            }

            #endregion

            //Open with...
            LoadFromArguments();

            RibbonTabControl.SelectedIndex = 0;

            WelcomeTextBlock.Text = LocalizationHelper.Get(Humanizer.WelcomeInfo());
            SymbolTextBlock.Text = Humanizer.Welcome();
        }
        
        private void Window_Activated(object sender, EventArgs e)
        {
            lock (ActivateLock)
            {
                //Debug.WriteLine("Activated");

                if (UserSettings.All.EditorExtendChrome)
                {
                    //Only extends the title bar again when needed.
                    if (!_chromeWasExtended)
                    {
                        Glass.ExtendGlassFrame(this, new Thickness(0, 126, 0, 0));
                        _chromeWasExtended = true;
                    }
                }
                else
                {
                    Glass.RetractGlassFrame(this);
                    _chromeWasExtended = false;
                }

                RibbonTabControl.UpdateVisual();

                //Returns the preview if was playing before the deactivation of the window.
                if (WasPreviewing)
                {
                    WasPreviewing = false;
                    PlayPause();
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            lock (ActivateLock)
            {
                try
                {
                    //Debug.WriteLine("Deactivated");
                    RibbonTabControl.UpdateVisual(false);

                    //Pauses the recording preview.
                    if (_timerPreview != null)
                    {
                        WasPreviewing = true;
                        Pause();
                    }
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Exception when losing focus on window.");
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            if (Math.Abs(e.NewDpi.PixelsPerInchX - e.OldDpi.PixelsPerInchX) < 0.01)
                return;

            ZoomBoxControl.RefreshImage();

            Cancel_Executed(sender, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //TODO: What if there's any processing happening? I need to try to stop.

            Pause();
            ClosePanel();

            if (Project != null && Project.Any)
            {
                Project.Persist();

                if (UserSettings.All.NotifyWhileClosingEditor && !Dialog.Ask(LocalizationHelper.Get("S.Editor.Exiting.Title"), LocalizationHelper.Get("S.Editor.Exiting.Instruction"),
                        LocalizationHelper.Get(UserSettings.All.AutomaticCleanUp ? "S.Editor.Exiting.Message2" : "S.Editor.Exiting.Message")))
                {
                    e.Cancel = true;
                    return;
                }

                Project.Clear();

                //Remove the ActionStack.
                ActionStack.Clear();
            }
            
            //Manually get the position/size of the window, so it's possible opening multiple instances.
            UserSettings.All.EditorTop = Top;
            UserSettings.All.EditorLeft = Left;
            UserSettings.All.EditorWidth = Width;
            UserSettings.All.EditorHeight = Height;
            UserSettings.All.EditorWindowState = WindowState;
            UserSettings.Save();

            Encoder.TryClose();

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
        }


        private void ZoomBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt)
            {
                #region Translate the Element (Scroll)

                if (sender.GetType() == typeof(ScrollViewer))
                {
                    switch (Keyboard.Modifiers)
                    {
                        case ModifierKeys.Alt:

                            var verDelta = e.Delta > 0 ? -10.5 : 10.5;
                            MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + verDelta);

                            break;
                        case ModifierKeys.Shift:

                            var horDelta = e.Delta > 0 ? -10.5 : 10.5;
                            MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + horDelta);

                            break;
                    }

                    return;
                }

                #endregion

                e.Handled = false;
                return;
            }

            WasChangingSelection = true;

            if (e.Delta > 0)
            {
                if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == FrameListView.Items.Count - 1)
                {
                    FrameListView.SelectedIndex = 0;
                    return;
                }

                //Show next frame.
                FrameListView.SelectedIndex++;
            }
            else
            {
                if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == 0)
                {
                    FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                    return;
                }

                //Show previous frame.
                FrameListView.SelectedIndex--;
            }
        }

        private void ZoomBoxControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Perhaps ignore when the mouse up happened because of a drag?
            if (_timerPreview != null || !NotPreviewing)
                (FindResource("Command.Play") as RoutedUICommand)?.Execute(null, this);
        }
        
        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Slept = true;
                Pause();
                GC.Collect();
                return;
            }

            Slept = false;
        }

        private void System_DisplaySettingsChanged(object sender, EventArgs e)
        {
            UpdatePositioning(false);
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Slept)
                return;

            //If the window color changes, update the tabs style.
            if (e.PropertyName == "WindowGlassColor")
                RibbonTabControl.UpdateVisual(IsActive);
        }

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region If nothing selected

            if (FrameListView.SelectedIndex == -1)
            {
                UpdateOtherStatistics();
                ZoomBoxControl.ImageSource = null;
                return;
            }

            #endregion

            if (LastSelected == -1 || _timerPreview != null || WasChangingSelection || LastSelected >= FrameListView.Items.Count || (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0))
                LastSelected = FrameListView.SelectedIndex;

            FrameListBoxItem current;

            if (_timerPreview != null || WasChangingSelection)
            {
                current = FrameListView.Items[FrameListView.SelectedIndex] as FrameListBoxItem;
            }
            else
            {
                //TODO: Test with other key shortcuts, because Ctrl + Z/Y was breaking this code.
                var focused = Keyboard.FocusedElement as FrameListBoxItem;

                //current = FrameListView.Items.GetItemAt(LastSelected) as FrameListBoxItem;
                if (focused != null && focused.IsVisible && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                    current = focused;
                else
                    current = FrameListView.Items.OfType<FrameListBoxItem>().FirstOrDefault(x => x.IsFocused || x.IsSelected);
            }

            //If there's no focused item.
            if (current == null)
            {
                if (FrameListView.Items.Count - 1 > LastSelected)
                    FrameListView.SelectedIndex = LastSelected;
                else
                    FrameListView.SelectedIndex = LastSelected = FrameListView.Items.Count - 1;

                if (FrameListView.SelectedIndex > -1)
                    current = FrameListView.Items[FrameListView.SelectedIndex] as FrameListBoxItem;
            }

            if (current != null)
            {
                if (!current.IsFocused && _timerPreview == null)// && !WasChangingSelection)
                    current.Focus();

                var currentIndex = FrameListView.Items.IndexOf(current);

                if (currentIndex > -1 && Project.Frames.Count > currentIndex)
                {
                    ZoomBoxControl.ImageSource = Project.Frames[currentIndex].Path;
                    FrameListView.ScrollIntoView(current);
                }
            }

            if (_timerPreview == null)
                UpdateOtherStatistics();

            WasChangingSelection = false;
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameListBoxItem item)// && !WasChangingSelection)
            {
                LastSelected = item.FrameNumber;
                Keyboard.Focus(item);
            }
        }

        #endregion


        #region File Tab

        #region New/Open

        private void NewRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && !e.Handled && Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
        }

        private void NewProject_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && !e.Handled;
        }

        private void NewRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            Encoder.Minimize();
            ClosePanel(removeEvent: true);

            App.MainViewModel.OpenRecorder.Execute(this);
        }

        private void NewWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            ClosePanel(removeEvent: true);

            App.MainViewModel.OpenWebcamRecorder.Execute(this);
        }

        private void NewBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            ClosePanel(removeEvent: true);

            App.MainViewModel.OpenBoardRecorder.Execute(this);
        }

        private void NewProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.NewAnimation, LocalizationHelper.Get("S.Editor.File.Blank", true), "Vector.File.New", ApplyNewProjectButton_Click);
        }

        private void ApplyNewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            //Start new project.
            var project = new ProjectInfo().CreateProjectFolder(ProjectByType.Editor);

            var fileName = Path.Combine(project.FullPath, "0.png");

            #region Create and Save Image

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(UserSettings.All.NewAnimationColor, UserSettings.All.NewAnimationWidth, UserSettings.All.NewAnimationHeight, this.Dpi(), PixelFormats.Indexed1);

                if (bitmapSource.Format != PixelFormats.Bgra32)
                    bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0);

                var bitmapFrame = BitmapFrame.Create(bitmapSource);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            GC.Collect();

            #endregion

            ClosePanel();

            project.Frames = new List<FrameInfo> { new FrameInfo(fileName, 66) };

            LoadProject(project);
            ShowHint("S.Hint.NewAnimation");
        }

        public void RecorderCallback(ProjectInfo project)
        {
            if (project?.Any == true)
            {
                LoadProject(project);
                ShowHint("S.Hint.NewRecording");
            }

            Encoder.Restore();
            ShowInTaskbar = true;
            WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
        }

        #endregion

        #region Insert

        private void Insert_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 0 && FrameListView.SelectedIndex != -1 && !IsLoading && !e.Handled && Application.Current.Windows.OfType<Window>().All(a => !(a is RecorderWindow));
        }

        private void InsertFromMedia_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 0 && FrameListView.SelectedIndex != -1 && !IsLoading && !e.Handled;
        }

        private void InsertRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            WindowState = WindowState.Minimized;
            Encoder.Minimize();

            ProjectInfo project = null;

            if (UserSettings.All.NewRecorder)
            {
                var recorder = new NewRecorder();
                recorder.ShowDialog();
                project = recorder.Project;

                if (recorder.Project?.Frames == null || !recorder.Project.Any)
                {
                    GC.Collect();

                    Encoder.Restore();
                    WindowState = WindowState.Normal;
                    return;
                }
            }
            else
            {
                var recorder = new Recorder();
                recorder.ShowDialog();
                project = recorder.Project;

                if (recorder.Project?.Frames == null || !recorder.Project.Any)
                {
                    GC.Collect();

                    Encoder.Restore();
                    WindowState = WindowState.Normal;
                    return;
                }
            }

            #region Insert

            var insert = new Insert(Project.Frames.CopyList(), project.Frames, FrameListView.SelectedIndex) { Owner = this };
            var result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.Frames = insert.CurrentList;
                LoadSelectedStarter(0);
            }

            #endregion

            Encoder.Restore();
            WindowState = WindowState.Normal;
        }

        private void InsertWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var recorder = new Webcam();
            recorder.ShowDialog();

            #region If recording cancelled

            if (recorder.Project?.Frames == null || !recorder.Project.Any)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(Project.Frames.CopyList(), recorder.Project.Frames, FrameListView.SelectedIndex) { Owner = this };

            var result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.Frames = insert.CurrentList;
                LoadSelectedStarter(0);
            }

            #endregion
        }

        private void InsertBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var recorder = new Board();
            recorder.ShowDialog();

            #region If recording cancelled

            if (recorder.Project?.Frames == null || !recorder.Project.Any)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(Project.Frames.CopyList(), recorder.Project.Frames, FrameListView.SelectedIndex) { Owner = this };

            var result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Project.Frames = insert.CurrentList;
                LoadSelectedStarter(0);
            }

            #endregion
        }

        private void InsertFromMedia_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Editor.File.OpenMedia"),
                Filter = $"{LocalizationHelper.Get("S.Editor.File.All")} (*.apng, *.avi, *.bmp, *.gif, *.jpg, *.jpeg, *.mkv, *.mp4, *.png, *.webp, *.webm, *.wmv)|*.apng;*.avi;*.bmp;*.gif;*.jpg;*.jpeg;*.mkv;*.mp4;*.png;*.webp;*.webm;*.wmv|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Image")} (*.apng, *.bmp, *.gif, *.jpg, *.jpeg, *.png)|*.apng;*.bmp;*.gif;*.jpg;*.jpeg;*.png|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Video")} (*.avi, *.mkv, *.mp4, *.webp, *.webm, *.wmv)|*.avi;*.mkv;*.mp4;*.webp;*.webm;*.wmv",
            };

            var result = ofd.ShowDialog();

            #region Validation

            var extensionList = ofd.FileNames.Select(Path.GetExtension).ToList();

            var media = new[] { "apng", "avi", "bmp", "gif", "jpg", "jpeg", "mkv", "mp4", "png", "webp", "webm", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.InvalidLoadingFiles")));
                return;
            }

            #endregion

            if (result.HasValue && result.Value)
            {
                _importFramesDel = InsertImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileNames.ToList(), InsertImportFromCallback, null);
            }
        }

        #endregion

        #region File

        private void File_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Any && !IsLoading && !e.Handled;
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ShowPanel(PanelType.SaveAs, LocalizationHelper.Get("S.Editor.File.Save", true), "Vector.Save", SaveAsButton_Click);
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            StatusList.Remove(StatusType.Warning);

            try
            {
                if (!(CustomContentControl.Content is ExportPanel panel))
                    return;

                //Lock UI.
                IsLoading = true;

                if (!await panel.IsValid())
                    return;

                var preset = panel.GetPreset();
               
                //Set some transient properties.
                var size = Project.Frames[0].Path.SizeOf();
                preset.Width = size.Width;
                preset.Height = size.Height;
                preset.Scale = this.Scale();

                await Task.Run(() => SaveAsync(preset));

                ClosePanel();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while exporting");
                StatusList.Error(ex.Message, StatusReasons.InvalidState); //TODO: Put a proper message and localize it.
            }
            finally
            {
                //Workaround for not disdabling the CanExecute of the panel.
                _applyAction = SaveAsButton_Click;

                //Return state of UI.
                Cursor = Cursors.Arrow;
                IsLoading = false;

                HideProgress();

                CommandManager.InvalidateRequerySuggested();
            }
        }


        private void Load_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Editor.File.OpenMediaProject"),
                Filter = $"{LocalizationHelper.Get("S.Editor.File.All")} (*.apng, *.avi, *.bmp, *.gif, *.jpg, *.jpeg, *.mkv, *.mp4, *.png, *.stg, *.webp, *.webm, *.wmv, *.zip)|*.apng;*.avi;*.bmp;*.gif;*.jpg;*.jpeg;*.mkv;*.mp4;*.png;*.stg;*.webp;*.webm;*.wmv;*.zip|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Image")} (*.apng, *.bmp, *.gif, *.jpg, *.jpeg, *.png)|*.apng;*.bmp;*.gif;*.jpg;*.jpeg;*.png|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Video")} (*.avi, *.mkv, *.mp4, *.webp, *.webm, *.wmv)|*.avi;*.mkv;*.mp4;*.webp;*.webm;*.wmv|" +
                         $"{LocalizationHelper.Get("S.Editor.File.Project")} (*.stg, *.zip) |*.stg;*.zip",
            };

            var result = ofd.ShowDialog();

            #region Validation

            var extensionList = ofd.FileNames.Select(s => Path.GetExtension(s).ToLowerInvariant()).ToList();

            var media = new[] { "apng", "avi", "bmp", "gif", "jpg", "jpeg", "mkv", "mp4", "png", "webp", "webm", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals(".stg") || x.Equals(".zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.InvalidLoadingFiles")));
                return;
            }

            #endregion

            if (result.HasValue && result.Value)
            {
                ClosePanel();

                //DiscardProject_Executed(null, null);

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileNames.ToList(), ImportFromCallback, null);
            }
        }

        private void LoadRecent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            ShowPanel(PanelType.LoadRecent, LocalizationHelper.Get("S.Editor.File.LoadRecent", true), "Vector.Project", LoadRecentButton_Click);
        }

        private void RecentDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadRecentButton_Click(sender, e);
        }

        private void RecentDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                LoadRecentButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void LoadRecentButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecentDataGrid.SelectedIndex < 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Recent.Warning.NoSelection"));
                return;
            }

            try
            {
                if (!(RecentDataGrid.SelectedItem is ProjectInfo project))
                    throw new Exception("Nothing selected");

                if (Project != null && Project.RelativePath == project.RelativePath)
                {
                    StatusList.Warning(LocalizationHelper.Get("S.Recent.Warning.SameProject"));
                    return;
                }

                if (MutexList.IsInUse(project.RelativePath))
                {
                    StatusList.Warning(LocalizationHelper.Get("S.Recent.Warning.AnotherEditor"));
                    return;
                }

                LoadProject(project, true, false, true);
            }
            catch (Exception ex)
            {
                ErrorDialog.Ok("ScreenToGif", "Error while trying to load", ex.Message, ex);
                return;
            }

            _applyAction = null;

            ClosePanel();
        }

        private void DiscardProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Discard(UserSettings.All.NotifyProjectDiscard);
        }

        #endregion

        #endregion

        #region Home Tab

        #region Action Stack

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanUndo() && !IsLoading && !e.Handled;
        }

        private void Reset_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanReset() && !IsLoading && !e.Handled;
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ActionStack.CanRedo() && !IsLoading && !e.Handled;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            Project.Frames = ActionStack.Undo(Project.Frames.CopyList());
            LoadProject(Project, false, false);

            ShowHint("S.Hint.Undo");
        }

        private void Reset_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            Project.Frames = ActionStack.Reset(Project.Frames.CopyList());
            LoadProject(Project, false, false);

            ShowHint("S.Hint.Reset");
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            Project.Frames = ActionStack.Redo(Project.Frames.CopyList());
            LoadProject(Project, false, false);

            ShowHint("S.Hint.Redo");
        }

        #endregion

        #region ClipBoard

        private void ClipBoard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading && !e.Handled;
        }

        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            #region Validation

            if (FrameListView.SelectedItems.Count == FrameListView.Items.Count)
            {
                Dialog.Ok(FindResource("S.Editor.Clipboard.InvalidCut.Title").ToString(),
                    FindResource("S.Editor.Clipboard.InvalidCut.Instruction").ToString(),
                    FindResource("S.Editor.Clipboard.InvalidCut.Message").ToString(), Icons.Info);
                return;
            }

            #endregion

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().OrderBy(x => x.FrameNumber).First().FrameNumber;

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, SelectedFramesIndex());

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => Project.Frames[item.FrameNumber]).ToList();

            FrameListView.SelectedIndex = -1;

            if (!Util.Clipboard.Cut(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to cut selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");

                Undo_Executed(null, null);

                return;
            }

            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => Project.Frames.RemoveAt(x.FrameNumber));
            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => FrameListView.Items.Remove(x));

            AdjustFrameNumbers(index);
            SelectNear(index);

            #region Item

            var imageItem = new ExtendedListBoxItem
            {
                Author = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentUICulture)
            };

            if (selected.Count > 1)
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.ImportVideo.Frames")} {string.Join(", ", selected.Select(x => x.FrameNumber))}";
                imageItem.Icon = FindResource("Vector.ImageStack") as Brush;
                imageItem.Content = $"{list.Count} Images";
            }
            else
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.Editor.List.Frame")} {selected[0].FrameNumber}";
                imageItem.Icon = FindResource("Vector.Image") as Brush;
                imageItem.Content = $"{list.Count} Image";
            }

            #endregion

            ClipboardListBox.Items.Add(imageItem);
            ClipboardListBox.SelectedIndex = ClipboardListBox.Items.Count - 1;

            ShowHint("S.Hint.Cut", false, selected.Count);

            ShowPanel(PanelType.Clipboard, LocalizationHelper.Get("S.Editor.Home.Clipboard", true), "Vector.Paste");
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => Project.Frames[item.FrameNumber]).ToList();

            if (!Util.Clipboard.Copy(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to copy selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");
                return;
            }

            #region Item

            var imageItem = new ExtendedListBoxItem
            {
                Author = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentUICulture)
            };

            if (list.Count > 1)
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.ImportVideo.Frames")} {string.Join(", ", selected.Select(x => x.FrameNumber))}";
                imageItem.Icon = FindResource("Vector.ImageStack") as Brush;
                imageItem.Content = LocalizationHelper.GetWithFormat("S.Clipboard.Entry.Images", "{0} images", list.Count);
            }
            else
            {
                imageItem.Tag = $"{LocalizationHelper.Get("S.Editor.List.Frame")} {selected[0].FrameNumber}";
                imageItem.Icon = FindResource("Vector.Image") as Brush;
                imageItem.Content = LocalizationHelper.GetWithFormat("S.Clipboard.Entry.Image", "{0} image", list.Count);
            }

            #endregion

            ClipboardListBox.Items.Add(imageItem);
            ClipboardListBox.SelectedIndex = ClipboardListBox.Items.Count - 1;

            ShowHint("S.Hint.Copy", false, selected.Count);

            ShowPanel(PanelType.Clipboard, LocalizationHelper.Get("S.Editor.Home.Clipboard", true), "Vector.Paste");
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && Util.Clipboard.Items.Count > 0 && ClipboardListBox.SelectedItem != null;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().Last().FrameNumber;
            index = PasteBeforeRadioButton.IsChecked.HasValue && PasteBeforeRadioButton.IsChecked.Value ? index : index + 1;

            var clipData = Util.Clipboard.Paste(Project.FullPath, ClipboardListBox.SelectedIndex, ClipboardListBox.SelectedIndex);

            ActionStack.SaveState(ActionStack.EditAction.Add, index, clipData.Count);

            Project.Frames.InsertRange(index, clipData);

            ClosePanel();

            LoadSelectedStarter(index, Project.Frames.Count - 1);

            ShowHint("S.Hint.Paste", false, clipData.Count);
        }

        private void ShowClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelType.Clipboard, LocalizationHelper.Get("S.Editor.Home.Clipboard", true), "Vector.Paste");
        }


        private void ClipBoardSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ClipboardListBox.SelectedItem != null && !IsLoading;
        }

        private void ExploreClipBoard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var selected = Util.Clipboard.Items[ClipboardListBox.SelectedIndex];

                Process.Start(Path.GetDirectoryName(selected[0].Path));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to browse the clipboard folder");
                Dialog.Ok(Title, "Impossible to browse the clipboard folder", ex.Message);
            }
        }

        private void RemoveClipboard_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Util.Clipboard.Remove(ClipboardListBox.SelectedIndex);
            ClipboardListBox.Items.RemoveAt(ClipboardListBox.SelectedIndex);
        }

        #endregion

        #region Zoom

        private void Zoom_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Any && !IsLoading && !OverlayGrid.IsVisible && FrameListView.SelectedIndex != -1;
        }

        private void Zoom100_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomBoxControl.Zoom = 1.0;
            ZoomBoxControl.SaveCurrentZoom();

            ShowHint("S.Hint.Zoom", false, 100);
        }

        private void SizeToContent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            ZoomBoxControl.UpdateLayout();

            var size = ZoomBoxControl.GetElementSize(true);

            //Tried to get the size again.
            if (size.Width < 2)
                size = Project.Frames[0].Path.NonScaledSize();

            //If failed again, abort.
            if (size.Width < 2)
                return;

            var scale = this.Scale();

            var borderHeight = ActualHeight - MainGrid.ActualHeight;
            var borderWidth = ActualWidth - MainGrid.ActualWidth;

            //Bug: I need to take into consideration that the RibbonTabControl.ActualHeight can change, because the tab headers can occupy 2 rows.
            var width = (size.Width * ZoomBoxControl.Zoom / ZoomBoxControl.ScaleDiff + 60) + borderWidth;
            var height = (size.Height * ZoomBoxControl.Zoom / ZoomBoxControl.ScaleDiff + (RibbonTabControl.ActualHeight + FrameListView.ActualHeight + LowerGrid.ActualHeight)) + borderHeight;

            //If image is too small, size to the minimum size.
            if (width < 770)
                width = 770;

            if (height < 575)
                height = 575;

            var screen = Monitor.AllMonitorsScaled(scale).FirstOrDefault(x => x.Bounds.Contains(new System.Windows.Point(Left, Top))) ??
                         Monitor.AllMonitorsScaled(scale).FirstOrDefault(x => x.IsPrimary);

            if (screen != null)
            {
                //If the resulting size is too big, fit the window on the available working area.
                if (screen.WorkingArea.Width < width)
                    width = screen.WorkingArea.Width;

                if (screen.WorkingArea.Height < height)
                    height = screen.WorkingArea.Height;

                //If the window overflows, put back in place.
                if (Left + width > screen.WorkingArea.Right)
                    Left = screen.WorkingArea.Right - width;

                if (Top + height > screen.WorkingArea.Bottom)
                    Top = screen.WorkingArea.Bottom - height;
            }

            Width = width;
            Height = height;
        }

        private void FitImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //Reset the zoom, to get the actual image size.
            ZoomBoxControl.Zoom = 1;
            ZoomBoxControl.UpdateLayout();

            var size = ZoomBoxControl.GetElementSize();

            if (size.Width < 2)
                size = Project.Frames[0].Path.ScaledSize();

            #region Calculate the Zoom

            var zoomHeight = 1D;
            var zoomWidth = 1D;

            if (size.Width > ZoomBoxControl.ActualWidth)
                zoomWidth = ZoomBoxControl.ActualWidth / size.Width;// * this.Scale();

            if (size.Height > ZoomBoxControl.ActualHeight)
                zoomHeight = ZoomBoxControl.ActualHeight / size.Height;// * this.Scale();

            #endregion

            #region Apply the zoom

            if (zoomHeight > 0 && zoomHeight < zoomWidth)
                ZoomBoxControl.Zoom = zoomHeight;
            else if (zoomWidth > 0 && zoomWidth < zoomHeight)
                ZoomBoxControl.Zoom = zoomWidth;
            else
                ZoomBoxControl.Zoom = 1;

            ZoomBoxControl.SaveCurrentZoom();

            #endregion

            ShowHint("S.Hint.Zoom", false, Convert.ToInt32(ZoomBoxControl.Zoom * 100));

            GC.Collect(1);
        }

        #endregion

        #region Select

        private void Selection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && FrameListView != null && FrameListView.HasItems;
        }

        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectAll();

            ShowHint("S.Hint.SelectAll");
        }

        private void GoTo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var go = new GoTo(Project.Frames.Count - 1) { Owner = this };
            var result = go.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            var item = FrameListView.Items[go.Selected] as FrameListBoxItem;

            if (item == null)
                return;

            Keyboard.Focus(item);

            FrameListView.ScrollIntoView(item);
            FrameListView.SelectedIndex = go.Selected;

            ShowHint("S.Hint.SelectSingle", false, go.Selected);
        }

        private void InverseSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            foreach (ListViewItem item in FrameListView.Items)
            {
                item.IsSelected = !item.IsSelected;
            }

            ShowHint("S.Hint.SelectInverse");
        }

        private void DeselectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            FrameListView.SelectedIndex = -1;

            ShowHint("S.Hint.Deselect");
        }

        #endregion

        #endregion

        #region Playback Tab

        private void Playback_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 1 && !IsLoading && _applyAction == null;
        }

        private void FirstFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;
            FrameListView.SelectedIndex = 0;
        }

        private void PreviousFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == 0)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            //Show previous frame.
            FrameListView.SelectedIndex--;
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PlayPause();
        }

        private void NextFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == FrameListView.Items.Count - 1)
            {
                FrameListView.SelectedIndex = 0;
                return;
            }

            //Show next frame.
            FrameListView.SelectedIndex++;
        }

        private void LastFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            WasChangingSelection = true;
            FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
        }

        #endregion

        #region Edit Tab

        #region Frames

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading;
        }

        private void DeletePrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading &&
                FrameListView.SelectedIndex > 0;
        }

        private void DeleteNext_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading &&
                FrameListView.SelectedIndex < FrameListView.Items.Count - 1;
        }

        private void Reduce_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && !IsLoading && FrameListView.Items.Count > 5;
        }

        private void RemoveDuplicates_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && !IsLoading && FrameListView.Items.Count > 1;
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            try
            {
                #region Validation

                if (Project.Frames.Count == FrameListView.SelectedItems.Count)
                {
                    //If the user wants to delete all frames, discard the project.
                    if (!UserSettings.All.NotifyProjectDiscard ||
                        Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteAll.Title"), LocalizationHelper.Get("S.Editor.DeleteAll.Instruction"), LocalizationHelper.Get("S.Editor.DeleteAll.Message"), false))
                        Discard(false);

                    return;
                }

                if (UserSettings.All.NotifyFrameDeletion)
                {
                    if (!Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteFrames.Title"), LocalizationHelper.Get("S.Editor.DeleteFrames.Instruction"),
                        string.Format(LocalizationHelper.Get("S.Editor.DeleteFrames.Message"), FrameListView.SelectedItems.Count)))
                        return;
                }

                #endregion

                var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
                var selectedOrdered = selected.OrderByDescending(x => x.FrameNumber).ToList();
                var list = selectedOrdered.Select(item => Project.Frames[item.FrameNumber]).ToList();

                ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, SelectedFramesIndex());

                FrameListView.SelectedItem = null;

                list.ForEach(x => File.Delete(x.Path));
                selectedOrdered.ForEach(x => Project.Frames.RemoveAt(x.FrameNumber));
                selectedOrdered.ForEach(x => FrameListView.Items.Remove(x));

                AdjustFrameNumbers(selectedOrdered.Last().FrameNumber);

                SelectNear(selectedOrdered.Last().FrameNumber);

                Project.Persist();
                UpdateStatistics();
                ShowHint("S.Hint.DeleteFrames", false, selected.Count);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error While Trying to Delete Frames");

                ErrorDialog.Ok(LocalizationHelper.Get("S.Editor.Title"), "Error while trying to delete frames", ex.Message, ex);
            }
        }

        private void DeletePrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            if (UserSettings.All.NotifyFrameDeletion)
            {
                if (!Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteFrames.Title"), LocalizationHelper.Get("S.Editor.DeleteFrames.Instruction"),
                    string.Format(LocalizationHelper.Get("S.Editor.DeleteFrames.Message"), FrameListView.SelectedIndex)))
                    return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, Util.Other.ListOfIndexesOld(0, FrameListView.SelectedIndex - 1));

            var count = FrameListView.SelectedIndex;

            for (var index = FrameListView.SelectedIndex - 1; index >= 0; index--)
                DeleteFrame(index);

            AdjustFrameNumbers(0);
            SelectNear(0);

            Project.Persist();
            UpdateStatistics();
            ShowHint("S.Hint.DeleteFrames", false, count);
        }

        private void DeleteNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            if (UserSettings.All.NotifyFrameDeletion)
            {
                if (!Dialog.Ask(LocalizationHelper.Get("S.Editor.DeleteFrames.Title"), LocalizationHelper.Get("S.Editor.DeleteFrames.Instruction"),
                    string.Format(LocalizationHelper.Get("S.Editor.DeleteFrames.Message"), FrameListView.Items.Count - FrameListView.SelectedIndex - 1)))
                    return;
            }

            var countList = FrameListView.Items.Count - 1; //So we have a fixed value.

            ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, Util.Other.ListOfIndexes(FrameListView.SelectedIndex + 1, FrameListView.Items.Count - FrameListView.SelectedIndex - 1));

            var count = FrameListView.Items.Count - FrameListView.SelectedIndex - 1;

            for (var i = countList; i > FrameListView.SelectedIndex; i--) //From the end to the middle.
            {
                DeleteFrame(i);
            }

            SelectNear(FrameListView.Items.Count - 1);

            Project.Persist();
            UpdateStatistics();
            ShowHint("S.Hint.DeleteFrames", false, count);
        }

        private void RemoveDuplicates_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.RemoveDuplicates, LocalizationHelper.Get("S.Editor.Edit.Frames.Duplicates", true), "Vector.RemoveImage", ApplyRemoveDuplicatesCountButton_Click);
        }

        private void ApplyRemoveDuplicatesCountButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting;

            _removeDuplicatesDel = RemoveDuplicatesAsync;
            _removeDuplicatesDel.BeginInvoke(UserSettings.All.DuplicatesSimilarity, UserSettings.All.DuplicatesRemoval, UserSettings.All.DuplicatesDelay, RemoveDuplicatesCallback, null);

            ClosePanel();
        }

        private void Reduce_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.ReduceFrames, LocalizationHelper.Get("S.Editor.Edit.Frames.Reduce", true), "Vector.RemoveImage", ApplyReduceFrameCountButton_Click);
        }

        private void ApplyReduceFrameCountButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = UserSettings.All.ReduceApplyToAll ? Util.Other.ListOfIndexes(0, Project.Frames.Count - 1) : SelectedFramesIndex();

            if (selected.Count == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Reduce.Warning.NoSelection"));
                return;
            }

            //Detects if there's any non-consecutive frames selected.
            if (!selected.All(v => selected.Contains(v + 1) || v == selected.Last()))
            {
                StatusList.Warning(LocalizationHelper.Get("S.Reduce.Warning.NonConsecutive"));
                return;
            }

            if (selected.Count <= UserSettings.All.ReduceFactor)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Reduce.Warning.SmallerThanFactor"));
                return;
            }

            Cursor = Cursors.AppStarting;

            _reduceFrameDel = ReduceFrameCount;
            _reduceFrameDel.BeginInvoke(selected, UserSettings.All.ReduceFactor, UserSettings.All.ReduceCount, UserSettings.All.ReduceDelay, ReduceFrameCountCallback, null);

            ClosePanel();
        }

        #endregion

        #region Reordering

        private void Reordering_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading && FrameListView.Items.Count > 1;
        }

        private void Reverse_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, Project.Frames.CopyList());

            Project.Frames.Reverse();

            LoadSelectedStarter(0);

            ShowHint("S.Hint.Reverse");
        }

        private void Yoyo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Add, Project.Frames.Count, Project.Frames.Count);

            Project.Frames = Util.Other.Yoyo(Project.Frames);
            LoadSelectedStarter(0);

            ShowHint("S.Hint.Yoyo");
        }

        private void MoveLeft_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, Project.Frames.CopyList());

            //TODO: Review this code.

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>());
            var selectedOrdered = selected.OrderBy(x => x.FrameNumber).ToList();

            var listIndex = selectedOrdered.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selectedOrdered)
            {
                #region Index

                var oldindex = listIndex[selectedOrdered.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index - 1;

                if (index == 0)
                    newIndex = FrameListView.Items.Count - 1;

                if (oldindex - 1 == index)
                {
                    continue;
                }

                #endregion

                #region Move

                var auxItem = Project.Frames[index];

                FrameListView.Items.RemoveAt(index);
                Project.Frames.RemoveAt(index);

                FrameListView.Items.Insert(newIndex, item);
                Project.Frames.Insert(newIndex, auxItem);

                #endregion
            }

            #region Update Count

            var indexUpdate = listIndex.First() == 0 ? 0 : listIndex.First() - 1;

            AdjustFrameNumbers(indexUpdate);

            #endregion

            #region Select Frames

            foreach (var item in selected)
            {
                FrameListView.SelectedItems.Add(item);
            }

            #endregion

            #endregion

            ShowHint("S.Hint.MoveLeft");

            e.Handled = true;
        }

        private void MoveRight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, Project.Frames.CopyList());

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>());
            var selectedOrdered = selected.OrderByDescending(x => x.FrameNumber).ToList();

            var listIndex = selectedOrdered.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selectedOrdered)
            {
                #region Index

                var oldindex = listIndex[selectedOrdered.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index + 1;

                if (index == FrameListView.Items.Count - 1)
                    newIndex = 0;

                if (oldindex + 1 == index)
                {
                    continue;
                }

                #endregion

                #region Move

                var auxItem = Project.Frames[index];

                FrameListView.Items.RemoveAt(index);
                Project.Frames.RemoveAt(index);

                FrameListView.Items.Insert(newIndex, item);
                Project.Frames.Insert(newIndex, auxItem);

                #endregion
            }

            #region Update Count

            var indexUpdate = listIndex.Last();

            if (listIndex.First() == FrameListView.Items.Count - 1)
                indexUpdate = 0;

            AdjustFrameNumbers(indexUpdate);

            #endregion

            #region Select Frames

            foreach (var item in selected)
            {
                FrameListView.SelectedItems.Add(item);
            }

            #endregion

            #endregion

            ShowHint("S.Hint.MoveRight");

            e.Handled = true;
        }

        #endregion

        #region Delay (Duration)

        private void OverrideDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.OverrideDelay, LocalizationHelper.Get("S.Editor.Edit.Delay.Override", true), "Vector.OverrideDelay", ApplyOverrideDelayButton_Click);
        }

        private void ApplyOverrideDelayButton_Click(object sender, RoutedEventArgs e)
        {
            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _delayFramesDel = DelayAsync;
            _delayFramesDel.BeginInvoke(DelayModel.FromSettings(DelayUpdateType.Override), false, false, DelayCallback, null); //NewDelayIntegerUpDown.Value

            ClosePanel();
        }


        private void IncreaseDecreaseDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.IncreaseDecreaseDelay, LocalizationHelper.Get("S.Editor.Edit.Delay.IncreaseDecrease", true), "Vector.IncreaseDecreaseDelay", ApplyIncreaseDecreaseDelayButtonClick);
        }

        private void ApplyIncreaseDecreaseDelayButtonClick(object sender, RoutedEventArgs e)
        {
            if (IncreaseDecreaseDelayIntegerUpDown.Value == 0)
            {
                ClosePanel();
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _delayFramesDel = DelayAsync;
            _delayFramesDel.BeginInvoke(DelayModel.FromSettings(DelayUpdateType.IncreaseDecrease), false, false, DelayCallback, null); //IncreaseDecreaseDelayIntegerUpDown.Value

            ClosePanel();
        }

        private void ScaleDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.ScaleDelay, LocalizationHelper.Get("S.Editor.Edit.Delay.Scale", true), "Vector.ScaleDelay", ApplyScaleDelayButtonClick);
        }

        private void ApplyScaleDelayButtonClick(object sender, RoutedEventArgs e)
        {
            if (ScaleDelayIntegerUpDown.Value == 0 || ScaleDelayIntegerUpDown.Value == 100)
            {
                ClosePanel();
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _delayFramesDel = DelayAsync;
            _delayFramesDel.BeginInvoke(DelayModel.FromSettings(DelayUpdateType.Scale), false, false, DelayCallback, null); //ScaleDelayIntegerUpDown.Value

            ClosePanel();
        }

        #endregion

        #endregion

        #region Image Tab

        private void Image_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && !IsLoading;
        }

        #region Size and Position

        private void Resize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WidthResizeNumericUpDown.ValueChanged -= WidthResizeIntegerUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged -= HeightResizeIntegerUpDown_ValueChanged;

            #region Info

            var image = Project.Frames[0].Path.SourceFrom();
            CurrentDpiRun.Text = (DpiNumericUpDown.Value = (int)Math.Round(image.DpiX, MidpointRounding.AwayFromZero)).ToString();
            CurrentWidthRun.Text = (WidthResizeNumericUpDown.Value = image.PixelWidth).ToString();
            CurrentHeightRun.Text = (HeightResizeNumericUpDown.Value = image.PixelHeight).ToString();

            #endregion

            #region Resize Attributes

            var gcd = Util.Other.Gcd(image.PixelHeight, image.PixelWidth);

            _widthRatio = image.PixelWidth / gcd;
            _heightRatio = image.PixelHeight / gcd;

            #endregion

            WidthResizeNumericUpDown.ValueChanged += WidthResizeIntegerUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged += HeightResizeIntegerUpDown_ValueChanged;

            ShowPanel(PanelType.Resize, LocalizationHelper.Get("S.Editor.Image.Resize", true), "Vector.Resize", ApplyResizeButton_Click);
        }

        private double _imageWidth;
        private double _imageHeight;
        private double _widthRatio = -1;
        private double _heightRatio = -1;

        private void KeepAspectCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            #region Resize Attributes

            var gcd = Util.Other.Gcd(HeightResizeNumericUpDown.Value, WidthResizeNumericUpDown.Value);

            _widthRatio = WidthResizeNumericUpDown.Value / gcd;
            _heightRatio = HeightResizeNumericUpDown.Value / gcd;

            #endregion

            _imageHeight = HeightResizeNumericUpDown.Value;
            _imageWidth = WidthResizeNumericUpDown.Value;
        }

        private void WidthResizeIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (KeepAspectCheckBox.IsChecked.HasValue && !KeepAspectCheckBox.IsChecked.Value)
                return;

            _imageWidth = WidthResizeNumericUpDown.Value;

            HeightResizeNumericUpDown.ValueChanged -= HeightResizeIntegerUpDown_ValueChanged;
            _imageHeight = Math.Round(_heightRatio * _imageWidth / _widthRatio);
            HeightResizeNumericUpDown.Value = (int)_imageHeight;
            HeightResizeNumericUpDown.ValueChanged += HeightResizeIntegerUpDown_ValueChanged;
        }

        private void HeightResizeIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (KeepAspectCheckBox.IsChecked.HasValue && !KeepAspectCheckBox.IsChecked.Value)
                return;

            WidthResizeNumericUpDown.ValueChanged -= WidthResizeIntegerUpDown_ValueChanged;
            _imageWidth = Math.Round(_widthRatio * HeightResizeNumericUpDown.Value / _heightRatio);
            WidthResizeNumericUpDown.Value = (int)_imageWidth;
            WidthResizeNumericUpDown.ValueChanged += WidthResizeIntegerUpDown_ValueChanged;
        }

        private void ApplyResizeButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            //Checks with the non scaled size.
            var size = Project.Frames[0].Path.ScaledSize();

            if (Math.Abs(size.Width - WidthResizeNumericUpDown.Value) < 0.1 && Math.Abs(size.Height - HeightResizeNumericUpDown.Value) < 0.1 && (int)Math.Round(Project.Frames[0].Path.DpiOf()) == DpiNumericUpDown.Value)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Resize.Warning"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            Enum.TryParse<ScalingMethod>((ResizeScalingQuality.SelectedItem as ComboBoxItem).Tag.ToString(), out var scalingQuality);

            _resizeFramesDel = Resize;
            _resizeFramesDel.BeginInvoke(WidthResizeNumericUpDown.Value, HeightResizeNumericUpDown.Value, DpiNumericUpDown.Value, (BitmapScalingMode)scalingQuality, ResizeCallback, null);

            ClosePanel();

            ShowHint("S.Hint.Resize");
        }


        private void Crop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Crop, LocalizationHelper.Get("S.Editor.Image.Crop", true), "Vector.Crop", ApplyCropButton_Click);
        }

        private CroppingAdorner _cropAdorner;
        private FrameworkElement _currentElement = null;
        private bool _resizing = false;

        private void AddCropToElement(FrameworkElement fel)
        {
            if (_currentElement != null)
                RemoveCropElements();

            var rcInterior = new Rect((int)(fel.Width * 0.2), (int)(fel.Height * 0.2), (int)(fel.Width * 0.6), (int)(fel.Height * 0.6));

            var aly = AdornerLayer.GetAdornerLayer(fel);
            _cropAdorner = new CroppingAdorner(fel, rcInterior);
            aly.Add(_cropAdorner);

            _cropAdorner.CropChanged += CropChanged;
            _currentElement = fel;

            _cropAdorner.Fill = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0));
            RefreshCropImage();
        }

        private void RemoveCropElements()
        {
            var aly = AdornerLayer.GetAdornerLayer(_currentElement);
            aly.Remove(_cropAdorner);

            _currentElement = null;
            _cropAdorner.CropChanged -= CropChanged;
            _cropAdorner = null;
        }

        private void CropChanged(object sender, RoutedEventArgs rea)
        {
            RefreshCropImage();

            _resizing = true;

            TopCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Top;
            LeftCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Left;
            BottomCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Bottom;
            RightCropNumericUpDown.Value = (int)_cropAdorner.ClipRectangle.Right;

            var scale = this.Scale();
            CropSizeTextBlock.Text = $"{(int)Math.Round(_cropAdorner.ClipRectangle.Width * scale)} × {(int)Math.Round(_cropAdorner.ClipRectangle.Height * scale)}";

            _resizing = false;
        }

        private void RefreshCropImage()
        {
            if (_cropAdorner == null || Math.Abs(ZoomBoxControl.Zoom - 1) > 0) return;

            try
            {
                var rect = new Int32Rect((int)(_cropAdorner.ClipRectangle.X * ZoomBoxControl.ScaleDiff),
                    (int)(_cropAdorner.ClipRectangle.Y * ZoomBoxControl.ScaleDiff),
                    (int)(_cropAdorner.ClipRectangle.Width * ZoomBoxControl.ScaleDiff),
                    (int)(_cropAdorner.ClipRectangle.Height * ZoomBoxControl.ScaleDiff));

                if (rect.HasArea)
                    CropImage.Source = (ZoomBoxControl.ImageSource ?? Project.Frames[LastSelected].Path).CropFrom(rect);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Crop preview", _cropAdorner.ClipRectangle + " => " + FrameSize);
            }
        }

        private void CropIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (_cropAdorner == null)
                return;

            if (_resizing)
                return;

            var top = TopCropNumericUpDown.Value;
            var left = LeftCropNumericUpDown.Value;
            var bottom = BottomCropNumericUpDown.Value;
            var right = RightCropNumericUpDown.Value;

            _cropAdorner.ClipRectangle = new Rect(new System.Windows.Point(left, top), new System.Windows.Point(right, bottom));
        }

        private void ApplyCropButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            RefreshCropImage();

            var rect = new Int32Rect((int)Math.Round(_cropAdorner.ClipRectangle.X * ZoomBoxControl.ScaleDiff, MidpointRounding.AwayFromZero),
                (int)Math.Round(_cropAdorner.ClipRectangle.Y * ZoomBoxControl.ScaleDiff, MidpointRounding.AwayFromZero),
                (int)Math.Round(_cropAdorner.ClipRectangle.Width * ZoomBoxControl.ScaleDiff),
                (int)Math.Round(_cropAdorner.ClipRectangle.Height * ZoomBoxControl.ScaleDiff));

            if (!rect.HasArea)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Crop.Warning"));
                return;
            }

            if (rect.Width < 10 || rect.Height < 10)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Crop.Warning.Bigger"));
                return;
            }

            if (CropImage.Source == null)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Crop.Warning"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            _cropFramesDel = Crop;
            _cropFramesDel.BeginInvoke(rect, CropCallback, null);

            RemoveCropElements();
            ClosePanel();

            ShowHint("S.Hint.Crop");
        }


        private void FlipRotate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FlipRotate, LocalizationHelper.Get("S.Editor.Image.FlipRotate", true), "Vector.FlipHorizontal", ApplyFlipRotateButton_Click);
        }

        private void ApplyFlipRotateButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            Cursor = Cursors.AppStarting;

            var type = FlipHorizontalRadioButton.IsChecked == true
                ? FlipRotateType.FlipHorizontal : FlipVerticalRadioButton.IsChecked == true
                ? FlipRotateType.FlipVertical : RotateLeftRadioButton.IsChecked == true ?
                  FlipRotateType.RotateLeft90 : FlipRotateType.RotateRight90;

            //If it's a rotate operation, the entire list of frames will be altered.
            var selectedIndexes = type == FlipRotateType.RotateLeft90 || type == FlipRotateType.RotateRight90
                ? Util.Other.ListOfIndexes(0, Project.Frames.Count)
                : SelectedFramesIndex();

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, selectedIndexes);

            _flipRotateFramesDel = FlipRotate;
            _flipRotateFramesDel.BeginInvoke(type, FlipRotateCallback, null);

            ClosePanel();

            ShowHint("S.Hint.FlipRotate");
        }

        #endregion

        #region Text

        private void Caption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Caption, LocalizationHelper.Get("S.Editor.Image.Caption", true), "Vector.Caption", ApplyCaptionButton_Click);
        }

        private void ApplyCaptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CaptionTextBox.Text.Trim().Length == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Caption.WarningNoText"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Caption.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            var render = CaptionOverlayGrid.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = OverlayAsync;
            _overlayFramesDel.BeginInvoke(render, false, OverlayCallback, null);

            ClosePanel();
        }


        private void FreeText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeText, LocalizationHelper.Get("S.Editor.Image.FreeText", true), "Vector.FreeText", ApplyFreeTextButton_Click);
        }

        private void FreeTextTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            //TODO: This event is not fired when the entire text is deleted.

            FreeTextOverlayControl.AdjustContent();
        }

        private void ApplyFreeTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeTextTextBox.Text.Length == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Caption.WarningNoText"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.FreeText.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            FreeTextOverlayControl.CanMove = false;

            var render = FreeTextOverlayControl.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            FreeTextOverlayControl.CanMove = true;

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = OverlayAsync;
            _overlayFramesDel.BeginInvoke(render, false, OverlayCallback, null);

            ClosePanel();
        }


        private void TitleFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.TitleFrame, LocalizationHelper.Get("S.Editor.Image.TitleFrame", true), "Vector.TitleFrame", ApplyTitleFrameButton_Click);
        }

        private void ApplyTitleFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.TitleFrame.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex, 1);

            var render = TitleFrameOverlayGrid.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            _titleFrameDel = TitleFrame;
            _titleFrameDel.BeginInvoke(render, FrameListView.SelectedIndex, ZoomBoxControl.ImageDpi, TitleFrameCallback, null);

            ClosePanel();
        }


        private void KeyStrokes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.KeyStrokes, LocalizationHelper.Get("S.Editor.Image.KeyStrokes", true), "Vector.Keyboard", ApplyKeyStrokesButton_Click);
        }

        private void EditKeyStrokesButton_Click(object sender, RoutedEventArgs e)
        {
            var keyStrokes = new KeyStrokes
            {
                InternalList = new ObservableCollection<FrameInfo>(Project.Frames.CopyList())
            };

            var result = keyStrokes.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            ActionStack.SaveState(ActionStack.EditAction.Properties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            for (var i = 0; i < keyStrokes.InternalList.Count; i++)
                Project.Frames[i].KeyList = new List<SimpleKeyGesture>(keyStrokes.InternalList[i].KeyList);
        }

        private void ApplyKeyStrokesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Project.Frames.Any(x => x.KeyList != null && x.KeyList.Any()))
            {
                StatusList.Warning(LocalizationHelper.Get("S.KeyStrokes.Warning.None"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            _keyStrokesDelegate = KeyStrokesAsync;
            _keyStrokesDelegate.BeginInvoke(KeyStrokesModel.FromSettings(), KeyStrokesCallback, null);

            ClosePanel();
        }

        #endregion

        #region Overlay

        private void FreeDrawing_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeDrawing, LocalizationHelper.Get("S.Editor.Image.FreeDrawing", true), "Vector.FreeDrawing", ApplyFreeDrawingButton_Click);
        }

        private void ApplyFreeDrawingButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeDrawingInkCanvas.Strokes.Count == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.Warning.NoDrawing"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.WarningSelection"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            var render = FreeDrawingInkCanvas.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            FreeDrawingInkCanvas.Strokes.Clear();

            _overlayFramesDel = OverlayAsync;
            _overlayFramesDel.BeginInvoke(render, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Shapes_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Shapes, LocalizationHelper.Get("S.Editor.Image.Shape", true), "Vector.Ellipse", ApplyShapesButton_Click);
        }

        private void ShapeModes_Checked(object sender, RoutedEventArgs e)
        {
            ShapeDrawingCanvas.DrawingMode = AddModeRadioButton.IsChecked == true ? DrawingCanvas.DrawingModes.Shape : DrawingCanvas.DrawingModes.Select;
        }

        private void ShapeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListBox listBox))
                return;

            switch (listBox.SelectedIndex)
            {
                case 0:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Rectangle;
                    break;
                case 1:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Ellipse;
                    break;
                case 2:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Triangle;
                    break;
                case 3:
                    ShapeDrawingCanvas.CurrentShape = DrawingCanvas.Shapes.Arrow;
                    break;
            }
        }

        private void ShapeProperties_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ShapeDrawingCanvas.StrokeThickness = ShapeOutlineDoubleUpDown.Value;
            ShapeDrawingCanvas.Stroke = ShapeOutlineColorBox.SelectedBrush;
            ShapeDrawingCanvas.Radius = ShapeRadiusDoubleUpDown.Value;
            ShapeDrawingCanvas.Fill = ShapesFillColorBox.SelectedBrush;
        }

        private void ApplyShapesButton_Click(object sender, RoutedEventArgs e)
        {
            if (ShapeDrawingCanvas.ShapesCount == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.Warning.NoDrawing"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.FreeDrawing.WarningSelection"));
                return;
            }

            ShapeDrawingCanvas.DeselectAll();

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            var render = ShapeDrawingCanvas.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            Cursor = Cursors.AppStarting;

            ShapeDrawingCanvas.RemoveAllShapes();

            _overlayFramesDel = OverlayAsync;
            _overlayFramesDel.BeginInvoke(render, false, OverlayCallback, null);

            ClosePanel();
        }


        private void MouseClicks_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.MouseClicks, LocalizationHelper.Get("S.Editor.Image.Clicks", true), "Vector.Cursor", ApplyMouseClicksButton_Click);
        }

        private void ApplyMouseClicksButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Project.Frames.Any(x => x.WasClicked))
            {
                StatusList.Warning(LocalizationHelper.Get("S.MouseClicks.Warning.None"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            _mouseClicksDelegate = MouseClicksAsync;
            _mouseClicksDelegate.BeginInvoke(MouseClicksModel.FromSettings(), MouseClicksCallback, null);

            ClosePanel();
        }


        private void Watermark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Watermark, LocalizationHelper.Get("S.Editor.Image.Watermark", true), "Vector.Watermark", ApplyWatermarkButton_Click);

            TopWatermarkDoubleUpDown.Scale = LeftWatermarkDoubleUpDown.Scale = this.Scale();
            TopWatermarkDoubleUpDown.Value = UserSettings.All.WatermarkTop;
            LeftWatermarkDoubleUpDown.Value = UserSettings.All.WatermarkLeft;

            if (string.IsNullOrEmpty(UserSettings.All.WatermarkFilePath))
            {
                if (TopWatermarkDoubleUpDown.Value < 0)
                    TopWatermarkDoubleUpDown.Value = 0;

                if (LeftWatermarkDoubleUpDown.Value < 0)
                    LeftWatermarkDoubleUpDown.Value = 0;
            }
        }

        private void SelectWatermark_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = LocalizationHelper.Get("S.Watermark.Select", true),
                Filter = $"{LocalizationHelper.Get("S.Editor.File.Image")} (*.bmp, *.jpg, *.jpeg, *.png)|*.bmp;*.jpg;*.jpeg;*.png",
            };

            var result = ofd.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            UserSettings.All.WatermarkFilePath = ofd.FileName;
            UserSettings.All.WatermarkSize = 1;

            WatermarkOverlayCanvas.AdjustContent();
        }

        private void ApplyWatermarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserSettings.All.WatermarkFilePath) || !File.Exists(UserSettings.All.WatermarkFilePath))
            {
                StatusList.Warning(LocalizationHelper.Get("S.Watermark.WarningNoImage"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Watermark.WarningSelection"));
                return;
            }

            UserSettings.All.WatermarkTop = TopWatermarkDoubleUpDown.Value;
            UserSettings.All.WatermarkLeft = LeftWatermarkDoubleUpDown.Value;

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            WatermarkOverlayCanvas.CanMove = false;
            WatermarkOverlayCanvas.CanResize = false;

            var render = WatermarkOverlayCanvas.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            WatermarkOverlayCanvas.CanMove = true;
            WatermarkOverlayCanvas.CanResize = true;

            #region Remove adorners

            var adornerLayer = AdornerLayer.GetAdornerLayer(WatermarkImage);

            //Remove all the adorners.
            foreach (var adorner in (adornerLayer.GetAdorners(WatermarkImage) ?? new Adorner[0]).OfType<ResizingAdorner>())
            {
                adorner.Destroy();
                adornerLayer.Remove(adorner);
            }

            #endregion

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = OverlayAsync;
            _overlayFramesDel.BeginInvoke(render, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Border_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Border, LocalizationHelper.Get("S.Editor.Image.Border", true), "Vector.Border", ApplyBorderButton_Click);
        }

        private void BorderProperties_ValueChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CaptionOverlayGrid.Width < 0)
                    return;

                //Measure the border size.
                var left = Math.Min(0, UserSettings.All.BorderLeftThickness);
                var top = Math.Min(0, UserSettings.All.BorderTopThickness);
                var right = Math.Min(0, UserSettings.All.BorderRightThickness);
                var bottom = Math.Min(0, UserSettings.All.BorderBottomThickness);
                var width = CaptionOverlayGrid.Width + Math.Abs(left) + +Math.Abs(right);
                var height = CaptionOverlayGrid.Height + Math.Abs(top) + Math.Abs(bottom);

                BorderBehindOverlayBorder.Width = width;
                BorderBehindOverlayBorder.Height = height;
                BorderPreviewGrid.Margin = new Thickness(left, top, right, bottom);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to measure dropshadow size.");
            }
        }

        private void ApplyBorderButton_Click(object sender, RoutedEventArgs e)
        {
            var model = BorderModel.FromSettings(true);

            if (Math.Abs(model.LeftThickness) < 0.001 && Math.Abs(model.TopThickness) < 0.001 && Math.Abs(model.RightThickness) < 0.001 && Math.Abs(model.BottomThickness) < 0.001)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Border.WarningThickness"));
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Border.WarningSelection"));
                return;
            }

            if (model.LeftThickness < 0 || model.TopThickness < 0 || model.RightThickness < 0 || model.BottomThickness < 0)
                ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));
            else
                ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _borderDelegate = BorderAsync;
            _borderDelegate.BeginInvoke(model, BorderCallback, null);

            ClosePanel();
        }


        private void Shadow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Shadow, LocalizationHelper.Get("S.Editor.Image.Shadow", true), "Vector.Shadow", ApplyShadowButton_Click);
        }

        private void ShadowProperties_ValueChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CaptionOverlayGrid.Width < 0)
                    return;

                //Converts the direction in degrees to radians.
                var radians = Math.PI / 180.0 * UserSettings.All.ShadowDirection;
                var offsetX = UserSettings.All.ShadowDepth * Math.Cos(radians);
                var offsetY = UserSettings.All.ShadowDepth * Math.Sin(radians);

                //Each side can have a different offset based on the direction of the shadow.
                var offsetLeft = offsetX < 0 ? offsetX * -1 : 0;
                var offsetTop = offsetY > 0 ? offsetY : 0;
                var offsetRight = offsetX > 0 ? offsetX : 0;
                var offsetBottom = offsetY < 0 ? offsetY * -1 : 0;

                //Measure drop shadow space.
                var marginLeft = offsetLeft > 0 ? offsetLeft + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d - offsetLeft, 0); //- offsetX
                var marginTop = offsetTop > 0 ? offsetTop + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d - offsetTop, 0); //- offsetY
                var marginRight = offsetRight > 0 ? offsetRight + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d + offsetRight, 0); //+ offsetX
                var marginBottom = offsetBottom > 0 ? offsetBottom + UserSettings.All.ShadowBlurRadius / 2d : Math.Max(UserSettings.All.ShadowBlurRadius / 2d + offsetBottom, 0); //+ offsetY

                ShadowPreviewGrid.Width = marginLeft + CaptionOverlayGrid.Width + marginRight;
                ShadowPreviewGrid.Height = Math.Round(marginTop + CaptionOverlayGrid.Height + marginBottom, 0);

                ShadowPreviewGrid.Margin = new Thickness(marginRight - marginLeft, marginBottom - marginTop, 0, 0);
                ShadowInternalGrid.Margin = new Thickness(marginLeft, marginTop, marginRight, marginBottom);

                ShadowInternalGrid.InvalidateVisual();
                ShadowPreviewGrid.InvalidateVisual();
                ShadowInternalGrid.InvalidateProperty(Grid.EffectProperty);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while trying to measure dropshadow size for the previewer.");
            }
        }

        private void ApplyShadowButton_Click(object sender, RoutedEventArgs e)
        {
            var model = ShadowModel.FromSettings();

            if (Math.Abs(model.Depth) < 0.1 && Math.Abs(model.BlurRadius) < 0.1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Shadow.Warning.Behind"));
                return;
            }

            if (Math.Abs(model.Opacity) < 0.1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Shadow.Warning.Invisible"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            Cursor = Cursors.AppStarting;

            _shadowDelegate = ShadowAsync;
            _shadowDelegate.BeginInvoke(model, ShadowCallback, null);

            ClosePanel();
        }


        private void Obfuscate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Obfuscate, LocalizationHelper.Get("S.Editor.Image.Obfuscate", true), "Vector.Obfuscate", ApplyObfuscateButton_Click);
        }

        private void ApplyObfuscateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObfuscateOverlaySelectControl.Selected.IsEmpty)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Obfuscate.Warning"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _obfuscateFramesDel = ObfuscateAsync;
            _obfuscateFramesDel.BeginInvoke(ObfuscateOverlaySelectControl.Selected, this.Scale(), false, ObfuscateCallback, null);

            ClosePanel();
        }


        private void Cinemagraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Cinemagraph, LocalizationHelper.Get("S.Editor.Image.Cinemagraph", true), "Vector.Cinemagraph", ApplyCinemagraphButton_Click);
        }

        private void ApplyCinemagraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (CinemagraphInkCanvas.Strokes.Count == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Cinemagraph.WarningNoDrawing"));
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            #region Get the Strokes and Clip the Image

            var image = Project.Frames[0].Path.SourceFrom();
            var rectangle = new RectangleGeometry(new Rect(new Point(0, 0), new Size(image.PixelWidth, image.PixelHeight)));
            var geometry = Geometry.Empty;

            foreach (var stroke in CinemagraphInkCanvas.Strokes)
                geometry = Geometry.Combine(geometry, stroke.GetGeometry(), GeometryCombineMode.Union, null);

            geometry = Geometry.Combine(geometry, rectangle, GeometryCombineMode.Xor, null);

            //Sicne the geometry is bound to the screen, it needs to be scaled to follow the image scale.
            geometry.Transform = new ScaleTransform(this.Scale() / ZoomBoxControl.ImageScale, this.Scale() / ZoomBoxControl.ImageScale);

            var clippedImage = new Image
            {
                Source = image,
                Clip = geometry
            };

            clippedImage.Measure(new Size(image.Width, image.Height));
            clippedImage.Arrange(new Rect(clippedImage.DesiredSize));

            //The ScaleDiff (ScreenScale / ImageScale) must be calculated as if the screen has 96DPI, since the clippedImage is not being attached to any visual.
            //var imageRender = clippedImage.GetScaledRender(1 / ZoomBoxControl.ImageScale, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());
            var imageRender = clippedImage.GetScaledRender(1, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

            #endregion

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = OverlayAsync;
            _overlayFramesDel.BeginInvoke(imageRender, true, OverlayCallback, null);

            ClosePanel();
        }


        private void Progress_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var size = Project.Frames[0].Path.ScaledSize();
            ProgressHorizontalRectangle.Width = size.Width / 2;
            ProgressVerticalRectangle.Height = size.Height / 2;

            ShowPanel(PanelType.Progress, LocalizationHelper.Get("S.Editor.Image.Progress", true), "Vector.Progress", ApplyProgressButton_Click);
        }

        private void ProgressPrecisionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || !ProgressGrid.IsVisible || TextRadioButton.IsChecked == false)
                return;

            ChangeProgressTextToCurrent();
        }

        private void ExtendedCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || !ProgressGrid.IsVisible || TextRadioButton.IsChecked == false)
                return;

            ChangeProgressTextToCurrent();
        }

        private void CustomProgressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || !ProgressGrid.IsVisible || TextRadioButton.IsChecked == false)
                return;

            ChangeProgressTextToCurrent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(e.Uri.AbsoluteUri);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, $"Error while trying to navigate to a given URI: '{e?.Uri?.AbsoluteUri}'.");
            }
        }

        private void ApplyProgressButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, Project.Frames, Util.Other.ListOfIndexes(0, Project.Frames.Count));

            _progressDelegateDel = ProgressAsync;
            _progressDelegateDel.BeginInvoke(ProgressModel.FromSettings(), ProgressCallback, null);

            ClosePanel();
        }

        #endregion

        #endregion

        #region Transitions Tab

        private void Transition_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && FrameListView?.SelectedItems != null && !IsLoading && FrameListView.SelectedIndex != -1;
        }

        private void Fade_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Fade, LocalizationHelper.Get("S.Editor.Fade.Title", true), "Vector.Fade", ApplyFadeButtonButton_Click);
        }

        private void ApplyFadeButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Fade.WarningSelection"));
                return;
            }

            if (UserSettings.All.FadeToType == FadeToType.Color && UserSettings.All.FadeToColor.A == 0)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Fade.WarningColor"));
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex + 1, (int)FadeSlider.Value);

            _transitionDel = Fade;
            _transitionDel.BeginInvoke(FrameListView.SelectedIndex, (int)FadeSlider.Value, null, TransitionCallback, null);

            ClosePanel();
        }


        private void Slide_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Slide, LocalizationHelper.Get("S.Editor.Slide.Title", true), "Vector.Slide", ApplySlideButtonButton_Click);
        }

        private void ApplySlideButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                StatusList.Warning(LocalizationHelper.Get("S.Editor.Slide.WarningSelection"));
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex + 1, (int)SlideSlider.Value);

            _transitionDel = Slide;
            _transitionDel.BeginInvoke(FrameListView.SelectedIndex, (int)SlideSlider.Value, SlideFrom.Right, TransitionCallback, null);

            ClosePanel();
        }

        #endregion


        #region Other Events

        #region Panel

        private void PanelAction_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            e.CanExecute = _applyAction != null || ClipboardGrid.IsVisible || LoadRecentGrid.IsVisible;// && ActionGrid.Width > 50 && ActionLowerGrid.IsVisible;
        }

        private void Ok_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ApplyButton.Focus();

            _applyAction?.Invoke(sender, e);

            //If the StatusBand started displaying the message, it means that the action failed.
            if (!StatusList.Children.OfType<StatusBand>().Any(a => a.Starting))
                _applyAction = null;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _applyAction = null;

            ClosePanel(true);
        }

        #endregion

        #region Frame ListView

        private void ListFramesSelection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView.SelectedIndex != -1 && !IsLoading;
        }

        private void OpenImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Process.Start(Project.Frames[FrameListView.SelectedIndex].Path);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Image");
                Dialog.Ok("Open Image", "Impossible to open image.", ex.Message);
            }
        }

        private void ExploreFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", $"/select,\"{Project.Frames[FrameListView.SelectedIndex].Path}\"");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Image Folder");
                Dialog.Ok("Open Image", "Impossible to open the image folder.", ex.Message);
            }
        }
        
        private void FrameListView_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PlayPause();

                //Avoids the selection of the frame by using the Space key.
                e.Handled = true;
            }

            if (e.Key == Key.PageDown)
            {
                NextFrame_Executed(sender, null);
                e.Handled = true;
            }

            if (e.Key == Key.PageUp)
            {
                PreviousFrame_Executed(sender, null);
                e.Handled = true;
            }
        }

        #endregion

        private void TimerPreview_Tick(int selectedIndex)
        {
            var sw = new Stopwatch();

            while (_timerPreview != null && !_timerPreview.IsCancellationRequested)
            {
                sw.Restart();
                long frameDelay = Project.Frames[selectedIndex].Delay;

                // Change active frame
                Dispatcher.Invoke(() => FrameListView.SelectedIndex = selectedIndex);

                // Wait for application UI to render changes (there is no point in ordering change of next frame if the previous one is not displayed yet)
                // Loaded priority could be used but input can become laggy
                Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

                int pass = 0;
                do
                {
                    pass++;

                    if (Project.Frames.Count - 1 == selectedIndex)
                    {
                        //If the playback should not loop, it will stop at the latest frame.
                        if (!UserSettings.All.LoopedPlayback)
                        {
                            Dispatcher.Invoke(() => Pause());
                            return;
                        }

                        selectedIndex = 0;
                    }
                    else
                    {
                        selectedIndex++;
                    }

                    if (!UserSettings.All.DropFramesDuringPreviewIfBehind)
                    {
                        break;
                    }

                    if (pass >= 2)
                    {
                        frameDelay += Project.Frames[selectedIndex].Delay;
                    }
                }
                while (sw.ElapsedMilliseconds >= frameDelay);

                if (Project.Frames[selectedIndex].Delay == 0)
                    Project.Frames[selectedIndex].Delay = 10;

                if (sw.ElapsedMilliseconds < frameDelay)
                {
                    // Wait rest of actual frame delay time
                    System.Threading.SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= frameDelay);
                }
            }
        }

        private void Control_DragEnter(object sender, DragEventArgs e)
        {
            Pause();

            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void Control_Drop(object sender, DragEventArgs e)
        {
            Pause();

            if (!(e.Data.GetData(DataFormats.FileDrop) is string[] fileNames) || fileNames.Length == 0) 
                return;
            
            #region Validation

            var extensionList = fileNames.Select(s => Path.GetExtension(s).ToLowerInvariant()).ToList();

            var media = new[] { ".jpg", ".jpeg", ".gif", ".bmp", ".png", ".avi", ".mp4", ".wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals(".stg") || x.Equals(".zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(Path.GetExtension(x)));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dialog.Ok(LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Title"),
                    LocalizationHelper.Get("S.Editor.DragDrop.MultipleFiles.Instruction"),
                    LocalizationHelper.Get("S.Editor.DragDrop.MultipleFiles.Message"), Icons.Warning);
                return;
            }

            if (mediaCount == 0 && projectCount == 0)
            {
                Dialog.Ok(LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Title"),
                    LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Instruction"),
                    LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Message"), Icons.Warning);
                return;
            }

            //if (projectCount > 0)
            //{
            //    Dialog.Ok(LocalizationHelper.Get("S.Editor.DragDrop.Invalid.Title"),
            //        LocalizationHelper.Get("S.Editor.DragDrop.InvalidProject.Instruction"),
            //        LocalizationHelper.Get("S.Editor.DragDrop.InvalidProject.Message"), Dialog.Icons.Warning);
            //    return;
            //}

            #endregion

            #region Importing options

            //If inserted into new recording or forced into new one.
            if (Project == null || !Project.Any || e.KeyStates == DragDropKeyStates.ControlKey || projectCount > 0)
                _importFramesDel = ImportFrom;
            else
                _importFramesDel = InsertImportFrom;

            #endregion

            _importFramesDel.BeginInvoke(fileNames.ToList(), ImportFromCallback, null);
        }

        private void CancelLoadingButton_Click(object sender, RoutedEventArgs e)
        {
            _abortLoading = true;
            CancelLoadingButton.IsEnabled = false;
        }

        private void InkCanvas_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || !(sender is InkCanvas canvas))
                return;

            //This event only exists because the InkPanel eats the Esc key used in Commands.
            //if something is not selected, run the command to close the panel.
            if (canvas.ActiveEditingMode != InkCanvasEditingMode.Select && canvas.GetSelectedStrokes().Any())
                CancelCommandBinding.Command.Execute(null);
        }

        #endregion

        #region Methods

        #region Load

        internal void LoadFromArguments()
        {
            if (!Argument.FileNames.Any())
                return;

            #region Validation

            var extensionList = Argument.FileNames.Select(Path.GetExtension).ToList();

            var media = new[] { "jpg", "jpeg", "gif", "bmp", "png", "apng", "avi", "mkv", "mp4", "webp", "webm", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            //TODO: Later I need to implement another validation for multiple video files.

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(FindResource("S.Editor.InvalidLoadingFiles").ToString()));
                return;
            }

            if (projectCount > 0)
            {
                Dispatcher.Invoke(() => StatusList.Warning(FindResource("S.Editor.InvalidLoadingProjects").ToString()));
                return;
            }

            #endregion

            _importFramesDel = ImportFrom;
            _importFramesDel.BeginInvoke(Argument.FileNames, ImportFromCallback, null);
        }

        #region Async Loading

        private delegate bool LoadFrames();

        private LoadFrames _loadFramesDel;

        /// <summary>
        /// Loads the new frames and clears the old ones.
        /// </summary>
        /// <param name="newProject">The project to load.</param>
        /// <param name="isNew">True if this is a new project.</param>
        /// <param name="clear">True if should clear the current list of frames.</param>
        /// <param name="createFlag">True if it should create a flag for single use, a mutex.</param>
        internal void LoadProject(ProjectInfo newProject, bool isNew = true, bool clear = true, bool createFlag = false)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;

            FrameListView.Items.Clear();
            ZoomBoxControl.Zoom = 1;

            #region Discard

            if (clear || isNew)
            {
                //Clear clipboard data.
                ClipboardListBox.Items.Clear();
                Util.Clipboard.Items?.Clear();
            }

            //TODO: Settings to choose if the project will be discarded.
            if (clear && Project != null && Project.Any)
            {
                Project.Persist();

                if (!UserSettings.All.NotifyProjectDiscard || Dialog.Ask(LocalizationHelper.Get("S.Editor.DiscardProject.Title"), LocalizationHelper.Get("S.Editor.DiscardPreviousProject.Instruction"),
                        LocalizationHelper.Get("S.Editor.DiscardPreviousProject.Message"), false))
                {
                    _discardFramesDel = Discard;
                    _discardFramesDel.BeginInvoke(Project, DiscardAndLoadCallback, null);

                    Project = newProject;

                    ActionStack.Clear();
                    ActionStack.Project = Project;
                    return;
                }

                Project.Clear();
                ActionStack.Clear();
            }

            #endregion

            if (isNew)
            {
                if (!clear) //Already clears the project above if flag 'clear' is true.
                    Project?.Clear();

                Project = newProject;

                if (createFlag)
                    Project.CreateMutex();

                ActionStack.Clear();
                ActionStack.Project = Project;
            }

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);
        }

        private bool Load()
        {
            try
            {
                if (!Project.IsNew)
                    Project.Persist();

                var corruptedList = new List<FrameInfo>();
                var count = 0;

                #region Get images from cache

                if (File.Exists(Project.CachePath))
                {
                    ShowProgress(LocalizationHelper.Get("S.Editor.RetrievingFromCache"), Project.Frames.Count);

                    using (var fileStream = new FileStream(Project.CachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var deflateStream = new DeflateStream(fileStream, CompressionMode.Decompress))
                        {
                            var number = 0;

                            foreach (var frame in Project.Frames)
                            {
                                if (_abortLoading)
                                {
                                    _abortLoading = false;
                                    return false;
                                }

                                Dispatcher.Invoke(() => { UpdateProgress(number++); });
                                BitmapSource source;

                                try
                                {
                                    var array = deflateStream.ReadBytes((int)frame.DataLength);

                                    if (Project.BitDepth == 24)
                                    {
                                        //24 bits: ((Project.Width * 24 + 31) / 32) * 4
                                        source = BitmapSource.Create(Project.Width, Project.Height, Project.Dpi, Project.Dpi, PixelFormats.Bgr24, null, array, ((Project.Width * 24 + 31) / 32) * 4);
                                        source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                                    }
                                    else
                                    {
                                        //32 bits: 4 * Project.Width
                                        source = BitmapSource.Create(Project.Width, Project.Height, Project.Dpi, Project.Dpi, PixelFormats.Bgra32, null, array, 4 * Project.Width);
                                    }
                                }
                                catch (EndOfStreamException d)
                                {
                                    LogWriter.Log(d, "It was not possible to read more bytes from the frame cache, since it reached the end");
                                    break;
                                }
                                catch (Exception e)
                                {
                                    //Not possible to read!
                                    throw;
                                }

                                using (var stream = new FileStream(frame.Path, FileMode.Create))
                                {
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(source));
                                    encoder.Save(stream);
                                    stream.Close();
                                }
                            }

                            GC.Collect();
                        }
                    }

                    File.Delete(Project.CachePath);
                }

                #endregion

                ShowProgress(LocalizationHelper.Get("S.Editor.LoadingFrames"), Project.Frames.Count);

                #region Check if there's any missing frames (and remove them)

                foreach (var frame in Project.Frames)
                {
                    if (_abortLoading)
                        break;

                    if (!File.Exists(frame.Path))
                        corruptedList.Add(frame);
                }

                if (_abortLoading)
                {
                    _abortLoading = false;
                    return false;
                }

                //Remove the corrupted frames.
                foreach (var frame in corruptedList)
                    Project.Frames.Remove(frame);

                if (Project.Frames.Count == 0)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        Dialog.Ok(LocalizationHelper.Get("S.Editor.LoadingFrames"), LocalizationHelper.Get("S.Editor.LoadingFrames.ProjectCorrupted.Instruction"),
                            LocalizationHelper.Get("S.Editor.LoadingFrames.ProjectCorrupted.Message"));
                    });
                    return false;
                }

                #endregion

                //If the project was never loaded inside the editor. Projects created with any version older than 2.14 won't enter here.
                if (Project.IsNew)
                {
                    Project.IsNew = false;
                    Project.Persist();

                    //Get enabled tasks.
                    var tasks = UserSettings.All.AutomatedTasksList?.Cast<DefaultTaskModel>().Where(w => w.IsEnabled).ToList() ?? new List<DefaultTaskModel>();

                    if (tasks.Any())
                    {
                        #region Initialize the previewer

                        Dispatcher.Invoke(() =>
                        {
                            ZoomBoxControl.LoadFromPath(Project.Frames[0].Path);

                            OverlayGrid.Visibility = Visibility.Visible;
                            OverlayGrid.Opacity = 1;
                            OverlayGrid.UpdateLayout();

                            var size = ZoomBoxControl.GetElementSize();

                            CaptionOverlayGrid.Width = size.Width;
                            CaptionOverlayGrid.Height = size.Height;
                            CaptionOverlayGrid.UpdateLayout();
                        });

                        #endregion

                        foreach (var task in tasks)
                        {
                            if (_abortLoading)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ZoomBoxControl.ImageSource = null;

                                    OverlayGrid.Visibility = Visibility.Collapsed;
                                    OverlayGrid.Opacity = 0;
                                });

                                _abortLoading = false;
                                return false;
                            }

                            try
                            {
                                //TODO: Abort loading from inside the tasks too.
                                switch (task.TaskType)
                                {
                                    case DefaultTaskModel.TaskTypeEnum.MouseClicks:
                                    {
                                        if (Project.CreatedBy == ProjectByType.ScreenRecorder)
                                            MouseClicksAsync(task as MouseClicksModel ?? MouseClicksModel.FromSettings());

                                        break;
                                    }

                                    case DefaultTaskModel.TaskTypeEnum.KeyStrokes:
                                    {
                                        if (Project.CreatedBy == ProjectByType.ScreenRecorder)
                                            KeyStrokesAsync(task as KeyStrokesModel ?? KeyStrokesModel.FromSettings());

                                        break;
                                    }

                                    case DefaultTaskModel.TaskTypeEnum.Delay:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            DelayAsync(task as DelayModel ?? DelayModel.FromSettings(), true, true);

                                        break;
                                    }

                                    case DefaultTaskModel.TaskTypeEnum.Progress:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            ProgressAsync(task as ProgressModel ?? ProgressModel.FromSettings());

                                        break;
                                    }

                                    case DefaultTaskModel.TaskTypeEnum.Border:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            BorderAsync(task as BorderModel ?? BorderModel.FromSettings());

                                        break;
                                    }

                                    case DefaultTaskModel.TaskTypeEnum.Shadow:
                                    {
                                        if (Project.CreatedBy != ProjectByType.Editor && Project.CreatedBy != ProjectByType.Unknown)
                                            ShadowAsync(task as ShadowModel ?? ShadowModel.FromSettings());

                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                LogWriter.Log(e, "Error while applying automatic task");
                                Dispatcher.Invoke(() => { ExceptionDialog.Ok(e, "ScreenToGif", "Error while executing a task", e.Message); });
                            }
                        }

                        #region Reset the previewer state

                        Dispatcher.Invoke(() =>
                        {
                            ZoomBoxControl.ImageSource = null;

                            OverlayGrid.Visibility = Visibility.Collapsed;
                            OverlayGrid.Opacity = 0;
                        });

                        #endregion
                    }
                }

                #region Load frames into the ListView

                foreach (var frame in Project.Frames)
                {
                    if (_abortLoading)
                        break;

                    frame.Index = count++;

                    Dispatcher.Invoke(() =>
                    {
                        var item = new FrameListBoxItem
                        {
                            FrameNumber = frame.Index,
                            Image = frame.Path,
                            Delay = frame.Delay
                        };

                        FrameListView.Items.Add(item);

                        UpdateProgress(item.FrameNumber);
                    });
                }

                if (_abortLoading)
                {
                    _abortLoading = false;
                    return false;
                }

                if (corruptedList.Any())
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        Dialog.Ok(LocalizationHelper.Get("S.Editor.LoadingFrames"), LocalizationHelper.Get("S.Editor.LoadingFrames.FramesCorrupted.Instruction"),
                            LocalizationHelper.Get("S.Editor.LoadingFrames.FramesCorrupted.Message"));
                    });
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error in loading frames");
                Dispatcher.Invoke(() => ErrorDialog.Ok(Title, "Error loading frames", "It was not possible to load all the frames.", ex));

                return false;
            }
        }

        private void LoadCallback(IAsyncResult ar)
        {
            var result = _loadFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;
                IsCancelable = false;

                if (Project.Any)
                    FilledList = true;

                if (!result)
                {
                    CancelLoadingButton.IsEnabled = true; //TODO: Is this right?

                    _discardFramesDel = Discard;
                    _discardFramesDel.BeginInvoke(Project, DiscardCallback, null);
                    return;
                }

                FrameListView.SelectedIndex = -1;
                FrameListView.SelectedIndex = 0; //TODO: Get the latest selected frame if it's the same project.
                ZoomBoxControl.PixelSize = Project.Frames[0].Path.ScaledSize();
                ZoomBoxControl.ImageScale = Project.Frames[0].Path.ScaleOf();
                ZoomBoxControl.RefreshImage();

                //ListBoxSelector.SetIsEnabled(FrameListView, true);
                //new ListBoxSelector(FrameListView);

                HideProgress();
                UpdateStatistics();

                WelcomeGrid.BeginStoryboard(this.FindStoryboard("HideWelcomeBorderStoryboard"), HandoffBehavior.Compose);

                CommandManager.InvalidateRequerySuggested();

                SetFocusOnCurrentFrame();

                //Adjust the window size based on the frame size.
                if (UserSettings.All.AutomaticallySizeOnContent && SizeToContentCommand.Command != null && SizeToContentCommand.Command.CanExecute(null))
                    SizeToContentCommand.Command.Execute(null);

                //Adjust the frame zoom based on the window size.
                if (UserSettings.All.AutomaticallyFitImage && FitImageCommand.Command != null && FitImageCommand.Command.CanExecute(null))
                    FitImageCommand.Command.Execute(null);
            }));
        }

        #endregion

        #region Async Selective Loading

        private delegate bool LoadSelectedFrames(int start, int? end);

        private LoadSelectedFrames _loadSelectedFramesDel;

        private void LoadSelectedStarter(int start, int? end = null)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;
            ShowProgress(LocalizationHelper.Get("S.Editor.UpdatingFrames"), Project.Frames.Count, true);

            //Persists the project to the disk.
            Task.Factory.StartNew(() => Project.Persist(), TaskCreationOptions.PreferFairness);

            _loadSelectedFramesDel = LoadSelected;
            _loadSelectedFramesDel.BeginInvoke(start, end, LoadSelectedCallback, null);
        }

        private bool LoadSelected(int start, int? end)
        {
            end = end ?? Project.Frames.Count - 1;
            UpdateProgress(0);

            try
            {
                //For each changed frame.
                for (var index = start; index <= end; index++)
                {
                    //Check if within limits.
                    if (index <= FrameListView.Items.Count - 1)
                    {
                        #region Edit the existing frame

                        Dispatcher.Invoke(() =>
                        {
                            var frame = (FrameListBoxItem)FrameListView.Items[index];

                            //TODO: Check if ListFrames.Count == FrameListView.Items.Count
                            frame.FrameNumber = index;
                            frame.Delay = Project.Frames[index].Delay;
                            frame.Image = null; //To update the image.
                            frame.Image = Project.Frames[index].Path;
                            frame.UpdateLayout();
                            frame.InvalidateVisual();
                        });

                        Project.Frames[index].Index = index;

                        #endregion
                    }
                    else
                    {
                        #region Create another frame

                        Dispatcher.Invoke(() =>
                        {
                            var item = new FrameListBoxItem
                            {
                                FrameNumber = index,
                                Image = Project.Frames[index].Path,
                                Delay = Project.Frames[index].Delay
                            };

                            FrameListView.Items.Add(item);
                        });

                        Project.Frames[index].Index = index;

                        #endregion
                    }

                    UpdateProgress(index);
                }

                if (Project.Frames.Count > 0)
                    Dispatcher.Invoke(() => { FilledList = true; });

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading Error");
                return false;
            }
        }

        private void LoadSelectedCallback(IAsyncResult ar)
        {
            try
            {
                var result = _loadSelectedFramesDel.EndInvoke(ar);
            }
            catch (Exception)
            { }

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                HideProgress();

                if (LastSelected != -1)
                {
                    ZoomBoxControl.ImageSource = null;

                    var valid = Project.ValidIndex(LastSelected);

                    if (valid > -1)
                    {
                        ZoomBoxControl.ImageSource = Project.Frames[valid].Path;
                        ZoomBoxControl.PixelSize = Project.Frames[0].Path.ScaledSize();
                        ZoomBoxControl.ImageScale = Project.Frames[0].Path.ScaleOf();
                        ZoomBoxControl.RefreshImage();
                        FrameListView.ScrollIntoView(FrameListView.Items[valid]);
                    }
                }

                UpdateStatistics();

                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();

                SetFocusOnCurrentFrame();
            });
        }

        #endregion

        #region Async Import

        private delegate bool ImportFrames(List<string> fileList);

        private ImportFrames _importFramesDel;

        private List<FrameInfo> InsertInternal(string fileName, string pathTemp, ref double previousDpi, ref bool warn)
        {
            List<FrameInfo> listFrames;

            try
            {
                switch (fileName.Split('.').Last().ToLowerInvariant())
                {
                    case "stg":
                    case "zip":
                    {
                        listFrames = ImportFromProject(fileName, pathTemp);
                        break;
                    }

                    case "gif":
                    {
                        listFrames = ImportFromGif(fileName, pathTemp);
                        break;
                    }

                    case "avi":
                    case "mkv":
                    case "mp4":
                    case "wmv":
                    case "webp":
                    case "webm":
                    {
                        listFrames = ImportFromVideo(fileName, pathTemp);
                        break;
                    }

                    case "apng":
                    case "png":
                    {
                        listFrames = ImportFromPng(fileName, pathTemp, ref previousDpi, ref warn);
                        break;
                    }

                    default:
                    {
                        listFrames = ImportFromImage(fileName, pathTemp, ref previousDpi, ref warn);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import error");

                return new List<FrameInfo>();
            }

            return listFrames;
        }

        private bool ImportFrom(List<string> fileList)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(LocalizationHelper.Get("S.Editor.PreparingImport"), 100, false);

            var project = new ProjectInfo().CreateProjectFolder(ProjectByType.Editor);
            var currentDpi = 0D;
            var wasWarned = false;

            //Adds each image to a list.
            foreach (var file in fileList)
            {
                if (Dispatcher.HasShutdownStarted)
                    return false;

                var warn = false;
                var frame = InsertInternal(file, project.FullPath, ref currentDpi, ref warn);

                if (frame != null)
                    project.Frames.AddRange(frame);

                //Warn that it's not allowed to import images with multiple DPI's at the same time.
                if (currentDpi > 0 && warn && !wasWarned)
                {
                    wasWarned = true;
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.Warning.DifferentDpi")));
                }
            }

            if (project.Frames.Count == 0)
            {
                if (Dispatcher.HasShutdownStarted)
                    return false;

                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;

                    if (project.Any)
                        FilledList = true;

                    HideProgress();

                    CommandManager.InvalidateRequerySuggested();
                    SetFocusOnCurrentFrame();
                });

                return false;
            }

            Dispatcher.Invoke(() => LoadProject(project));

            return true;
        }

        private void ImportFromCallback(IAsyncResult ar)
        {
            _importFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                ClosePanel(removeEvent: true);

                CommandManager.InvalidateRequerySuggested();
            });

            GC.Collect();
        }

        private bool InsertImportFrom(List<string> fileList)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(LocalizationHelper.Get("S.Editor.PreparingImport"), 100);

            var project = new ProjectInfo().CreateProjectFolder(ProjectByType.Editor);
            var currentDpi = 0D;
            var wasWarned = false;

            //Adds each image to a list.
            foreach (var file in fileList)
            {
                if (Dispatcher.HasShutdownStarted)
                    return false;

                var warn = false;
                var frame = InsertInternal(file, project.FullPath, ref currentDpi, ref warn);

                if (frame != null)
                    project.Frames.AddRange(frame);

                //Warn that it's not allowed to import images with multiple DPI's at the same time.
                if (currentDpi > 0 && warn && !wasWarned)
                {
                    wasWarned = true;
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.Editor.Warning.DifferentDpi")));
                }
            }

            if (!project.Any)
            {
                project.ReleaseMutex();

                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;
                });

                return false;
            }

            return Dispatcher.Invoke(() =>
            {
                #region Insert

                var insert = new Insert(Project.Frames, project.Frames, FrameListView.SelectedIndex) { Owner = this };
                var result = insert.ShowDialog();

                project.ReleaseMutex();

                //Discard(project);

                if (result.HasValue && result.Value)
                {
                    ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex, project.Frames.Count);

                    Project.Frames = insert.CurrentList;
                    LoadSelectedStarter(FrameListView.SelectedIndex, Project.Frames.Count - 1); //Check

                    return true;
                }

                HideProgress();

                if (LastSelected != -1)
                {
                    ZoomBoxControl.ImageSource = null;
                    ZoomBoxControl.ImageSource = Project.Frames[LastSelected].Path;

                    FrameListView.ScrollIntoView(FrameListView.Items[LastSelected]);
                }

                #region Enabled the UI

                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;
                });

                #endregion

                return false;

                #endregion
            });
        }

        private void InsertImportFromCallback(IAsyncResult ar)
        {
            var result = _importFramesDel.EndInvoke(ar);

            GC.Collect();

            if (!result)
                Dispatcher.Invoke(delegate
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;
                    HideProgress();

                    ClosePanel(removeEvent: true);

                    FrameListView.Focus();
                    CommandManager.InvalidateRequerySuggested();
                });
        }

        #endregion

        private List<FrameInfo> ImportFromProject(string source, string pathTemp)
        {
            try
            {
                //Extract to the folder of the newly created project.
                ZipFile.ExtractToDirectory(source, pathTemp);

                List<FrameInfo> list;

                if (File.Exists(Path.Combine(pathTemp, "Project.json")))
                {
                    //Read as text.
                    var json = File.ReadAllText(Path.Combine(pathTemp, "Project.json"));

                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var ser = new DataContractJsonSerializer(typeof(ProjectInfo));
                        var project = ser.ReadObject(ms) as ProjectInfo;
                        list = project.Frames;
                    }
                }
                else
                {
                    if (File.Exists(Path.Combine(pathTemp, "List.sb")))
                        throw new Exception("Project not compatible with this version");

                    throw new FileNotFoundException("Impossible to open project.", "List.sb");
                }

                //Shows the ProgressBar
                ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), list.Count);

                var count = 0;
                foreach (var frame in list)
                {
                    //Change the file path to the current one.
                    frame.Path = Path.Combine(pathTemp, Path.GetFileName(frame.Path));

                    count++;
                    UpdateProgress(count);
                }

                return list;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Importing project");
                Dispatcher.Invoke(() => Dialog.Ok("ScreenToGif", "Impossible to load project", ex.Message));
                return new List<FrameInfo>();
            }
        }

        private List<FrameInfo> ImportFromGif(string source, string pathTemp)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), 50, true);

            var listFrames = new List<FrameInfo>();

            var decoder = ImageMethods.GetDecoder(source, out var gifMetadata) as GifBitmapDecoder;

            ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), decoder?.Frames?.Count ?? 0);

            if (decoder.Frames.Count <= 0)
                return listFrames;

            var fullSize = ImageMethods.GetFullSize(decoder, gifMetadata);
            var index = 0;

            BitmapSource baseFrame = null;
            foreach (var rawFrame in decoder.Frames)
            {
                var metadata = ImageMethods.GetFrameMetadata(decoder, gifMetadata, index);

                var bitmapSource = ImageMethods.MakeFrame(fullSize, rawFrame, metadata, baseFrame, 96D);

                #region Disposal Method

                switch (metadata.DisposalMethod)
                {
                    case FrameDisposalMethod.None:
                    case FrameDisposalMethod.DoNotDispose:
                        baseFrame = bitmapSource;
                        break;
                    case FrameDisposalMethod.RestoreBackground:
                        baseFrame = ImageMethods.IsFullFrame(metadata, fullSize) ? null : ImageMethods.ClearArea(bitmapSource, metadata, 96D);
                        break;
                    case FrameDisposalMethod.RestorePrevious:
                        //Reuse same base frame.
                        break;
                }

                #endregion

                #region Each Frame

                var fileName = Path.Combine(pathTemp, $"{index} {DateTime.Now:hh-mm-ss-ffff}.png");

                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);
                    stream.Close();
                }

                //It should not throw a overflow exception because of the maximum value for the milliseconds.
                var frame = new FrameInfo(fileName, (int)metadata.Delay.TotalMilliseconds);
                listFrames.Add(frame);

                UpdateProgress(index);

                GC.Collect(1);

                #endregion

                index++;
            }

            return listFrames;
        }

        private List<FrameInfo> ImportFromPng(string source, string pathTemp, ref double previousDpi, ref bool warn)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ImportingFrames"), 50, true);

            using (var stream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var apng = new Apng(stream);
                var success = apng.ReadFrames();

                if (!success)
                    return ImportFromImage(source, pathTemp, ref previousDpi, ref warn);

                var fullSize = new System.Drawing.Size((int)apng.Ihdr.Width, (int)apng.Ihdr.Height);
                var list = new List<FrameInfo>();

                BitmapSource baseFrame = null;
                for (var index = 0; index < apng.Actl.NumFrames; index++)
                {
                    var metadata = apng.GetFrame(index);
                    var rawFrame = metadata.ImageData.SourceFrom();

                    var bitmapSource = Apng.MakeFrame(fullSize, rawFrame, metadata, baseFrame);

                    #region Disposal Method

                    switch (metadata.DisposeOp)
                    {
                        case Apng.DisposeOps.None: //No disposal is done on this frame before rendering the next; the contents of the output buffer are left as is.
                            baseFrame = bitmapSource;
                            break;
                        case Apng.DisposeOps.Background: //The frame's region of the output buffer is to be cleared to fully transparent black before rendering the next frame.
                            baseFrame = baseFrame == null || Apng.IsFullFrame(metadata, fullSize) ? null : Apng.ClearArea(baseFrame, metadata);
                            break;
                        case Apng.DisposeOps.Previous: //The frame's region of the output buffer is to be reverted to the previous contents before rendering the next frame.
                            //Reuse same base frame.
                            break;
                    }

                    #endregion

                    #region Each Frame

                    var fileName = Path.Combine(pathTemp, $"{index} {DateTime.Now:hh-mm-ss-ffff}.png");

                    //TODO: Do I need to verify the DPI of the image?

                    using (var output = new FileStream(fileName, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(output);
                        stream.Close();
                    }

                    list.Add(new FrameInfo(fileName, metadata.Delay));

                    UpdateProgress(index);

                    GC.Collect(1);

                    #endregion
                }

                return list;
            }
        }

        private List<FrameInfo> ImportFromImage(string source, string pathTemp, ref double previousDpi, ref bool warn)
        {
            var fileName = Path.Combine(pathTemp, $"{0} {DateTime.Now:hh-mm-ss-ffff}.png");

            #region Save the Image to the Recording Folder

            BitmapSource bitmap = new BitmapImage(new Uri(source));

            //Don't let it import multiple images with different DPI's.
            if (previousDpi > 0 && Math.Abs(previousDpi - bitmap.DpiX) > 0.09)
            {
                warn = true;
                return null;
            }
            
            if (Math.Abs(previousDpi) < 0.01)
                previousDpi = bitmap.DpiX;

            if (bitmap.Format != PixelFormats.Bgra32)
                bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Close();
            }
            
            GC.Collect();

            #endregion

            return new List<FrameInfo> { new FrameInfo(fileName, 66) };
        }

        private List<FrameInfo> ImportFromVideo(string source, string pathTemp)
        {
            //Get frames from video.
            return Dispatcher?.Invoke(() =>
            {
                var vid = new VideoSource
                {
                    RootFolder = pathTemp,
                    VideoPath = source,
                    Owner = this
                };
                var result = vid.ShowDialog();

                return result.HasValue && result.Value ? vid.Frames : null;
            });
        }

        #endregion

        #region Playback

        private void PlayPause()
        {
            lock (UserSettings.Lock)
            {
                if (_timerPreview != null || !NotPreviewing)
                {
                    if (_timerPreview != null)
                    {
                        _timerPreview.Cancel();
                        _timerPreview.Dispose();
                        _timerPreview = null;
                    }

                    NotPreviewing = true;
                    PlayButton.Text = LocalizationHelper.Get("S.Editor.Playback.Play");
                    PlayButton.Icon = FindResource("Vector.Play") as Brush;
                    PlayPauseButton.Icon = FindResource("Vector.Play") as Brush;

                    PlayMenuItem.Header = LocalizationHelper.Get("S.Editor.Playback.Play");
                    PlayMenuItem.Icon = FindResource("Vector.Play") as Brush;

                    SetFocusOnCurrentFrame();
                    UpdateOtherStatistics();
                }
                else
                {
                    NotPreviewing = false;
                    PlayButton.Text = LocalizationHelper.Get("S.Editor.Playback.Pause");
                    PlayButton.Icon = FindResource("Vector.Pause") as Brush;
                    PlayPauseButton.Icon = FindResource("Vector.Pause") as Brush;

                    PlayMenuItem.Header = LocalizationHelper.Get("S.Editor.Playback.Pause");
                    PlayMenuItem.Icon = FindResource("Vector.Pause") as Brush;

                    #region Starts playing the next frame

                    if (Project.Frames.Count - 1 == FrameListView.SelectedIndex)
                        FrameListView.SelectedIndex = 0;
                    else
                        FrameListView.SelectedIndex++;

                    #endregion

                    if (Project.Frames[FrameListView.SelectedIndex].Delay == 0)
                        Project.Frames[FrameListView.SelectedIndex].Delay = 10;

                    _timerPreview = new System.Threading.CancellationTokenSource();
                    int selectedIndex = FrameListView.SelectedIndex;

                    Task.Run(() => TimerPreview_Tick(selectedIndex), _timerPreview.Token);
                }
            }
        }

        private void Pause()
        {
            if (_timerPreview == null && NotPreviewing)
                return;

            if (_timerPreview != null)
            {
                _timerPreview.Cancel();
                _timerPreview.Dispose();
                _timerPreview = null;
            }

            NotPreviewing = true;
            PlayButton.Text = LocalizationHelper.Get("S.Editor.Playback.Play");
            PlayButton.Icon = FindResource("Vector.Play") as Brush;
            PlayPauseButton.Icon = FindResource("Vector.Play") as Brush;

            PlayMenuItem.Header = LocalizationHelper.Get("S.Editor.Playback.Play");
            PlayMenuItem.Icon = FindResource("Vector.Play") as Brush;

            SetFocusOnCurrentFrame();
            UpdateOtherStatistics();
        }

        #endregion

        #region UI

        #region Progress

        private void ShowProgress(string description, int maximum, bool isIndeterminate = false)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Content = description;
                StatusProgressBar.Maximum = maximum;
                StatusProgressBar.Value = 0;
                StatusProgressBar.IsIndeterminate = isIndeterminate;
                StatusGrid.Visibility = Visibility.Visible;

                TaskbarItemInfo.ProgressState = isIndeterminate ? TaskbarItemProgressState.Indeterminate : TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 0;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = MathHelper.CrossMultiplication(StatusProgressBar.Maximum, value, null) / 100d;

                StatusProgressBar.IsIndeterminate = false;
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StatusGrid.Visibility = Visibility.Collapsed;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            });
        }

        #endregion

        private void SelectNear(int index)
        {
            FrameListView.Focus();

            if (FrameListView.Items.Count - 1 < index)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            FrameListView.SelectedIndex = index;
            FrameListView.ScrollIntoView(FrameListView.SelectedItem);
        }

        private void AdjustFrameNumbers(int startIndex)
        {
            for (var index = startIndex; index < FrameListView.Items.Count; index++)
            {
                Project.Frames[index].Index = index;
                ((FrameListBoxItem)FrameListView.Items[index]).FrameNumber = index;
            }
        }

        private void ShowPanel(PanelType type, string title, string vector, Action<object, RoutedEventArgs> apply = null)
        {
            var focusFirstVisibleChild = true;

            #region Hide all visible grids

            foreach (var child in ActionInternalGrid.Children.OfType<Grid>().Where(x => x.Visibility == Visibility.Visible))
                child.Visibility = Visibility.Collapsed;

            CustomContentControl.Content = null;
            CustomContentControl.Visibility = Visibility.Collapsed;

            ShapeDrawingCanvas.DeselectAll();

            #endregion

            #region Overlay

            ZoomBoxControl.SaveCurrentZoom();

            if (Project != null && Project.Any && type < 0)
            {
                ZoomBoxControl.Zoom = 1.0;
                var size = ZoomBoxControl.GetElementSize();

                CaptionOverlayGrid.Width = size.Width;
                CaptionOverlayGrid.Height = size.Height;
            }

            #endregion

            #region Commons

            ActionTitleTextBlock.Text = title;
            ActionIconBorder.Background = FindResource(vector) as Brush;

            Util.Other.RemoveRoutedEventHandlers(ApplyButton, ButtonBase.ClickEvent);

            if (apply != null)
            {
                ApplyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Action.Apply");
                ApplyButton.Icon = FindResource("Vector.Ok") as Brush;
                _applyAction = apply;

                ActionLowerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ActionLowerGrid.Visibility = Visibility.Collapsed;
            }

            #endregion

            #region Type

            switch (type)
            {
                case PanelType.NewAnimation:
                    NewGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.SaveAs:
                    ApplyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Action.Save");
                    ApplyButton.Icon = FindResource("Vector.Save") as Brush;
                    
                    //WARNING: Temporary!!!
                    var grid = new ExportPanel();
                    grid.Save += (sender, args) =>
                    {
                        SaveAs_Executed(null, null);
                    };
                    grid.Validated += (sender, args) =>
                    {
                        StatusList.Warning(LocalizationHelper.Get(args.MessageKey), args.Reason, args.Action);
                    };
                    grid.ValidationRemoved += (sender, args) =>
                    {
                        StatusList.Remove(StatusType.Warning, args.Reason);
                    };

                    var frameCountBinding = new Binding();
                    frameCountBinding.ElementName = "FrameListView";
                    frameCountBinding.Path = new PropertyPath("Items.Count");
                    frameCountBinding.Mode = BindingMode.OneWay;
                    frameCountBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(grid, ExportPanel.FrameCountProperty, frameCountBinding);

                    var durationBinding = new Binding();
                    durationBinding.ElementName = "EditorWindow";
                    durationBinding.Path = new PropertyPath("TotalDuration");
                    durationBinding.Mode = BindingMode.OneWay;
                    durationBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(grid, ExportPanel.TotalTimeProperty, durationBinding);

                    var selectionCountBinding = new Binding();
                    selectionCountBinding.ElementName = "FrameListView";
                    selectionCountBinding.Path = new PropertyPath("SelectedItems.Count");
                    selectionCountBinding.Mode = BindingMode.OneWay;
                    selectionCountBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    BindingOperations.SetBinding(grid, ExportPanel.SelectionCountProperty, selectionCountBinding);

                    CustomContentControl.Content = grid;
                    CustomContentControl.Visibility = Visibility.Visible;

                    //Focus the filename text box instead of automatically focusing the first child control in the panel.
                    grid.InitialFocus();
                    
                    focusFirstVisibleChild = false;
                    break;
                case PanelType.LoadRecent:
                    ApplyButton.SetResourceReference(ExtendedButton.TextProperty, "S.Action.Open");
                    ApplyButton.Icon = FindResource("Vector.Open") as Brush;
                    LoadRecentGrid.Visibility = Visibility.Visible;

                    //Load list.
                    _loadRecentDel = LoadRecentAsync;
                    _loadRecentDel.BeginInvoke(LoadRecentCallback, null);
                    break;
                case PanelType.Clipboard:
                    ClipboardGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Resize:
                    ResizeGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelType.FlipRotate:
                    FlipRotateGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.FlipRotate2", true);
                    break;
                case PanelType.Crop:

                    #region Crop

                    CropGrid.Visibility = Visibility.Visible;

                    AddCropToElement(CropAreaGrid);

                    BottomCropNumericUpDown.Scale = TopCropNumericUpDown.Scale = RightCropNumericUpDown.Scale = LeftCropNumericUpDown.Scale = this.Scale();

                    BottomCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height - (CaptionOverlayGrid.Height * .1));
                    TopCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height * .1);

                    RightCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width - (CaptionOverlayGrid.Width * .1));
                    LeftCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width * .1);

                    ShowHint("S.Hint.ApplyAll", true);

                    #endregion

                    break;
                case PanelType.Caption:
                    CaptionGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.FreeText:
                    FreeTextGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.TitleFrame:
                    TitleFrameGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.TitleFrame2", true);
                    break;
                case PanelType.KeyStrokes:
                    KeyStrokesLabel.Text = "Ctrl + C";
                    KeyStrokesGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelType.FreeDrawing:
                    FreeDrawingGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.Shapes:
                    ShapesGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);

                    ShapeProperties_Changed(this, null);
                    ShapeType_SelectionChanged(ShapesListBox, null);
                    break;
                case PanelType.Watermark:

                    #region Watermark

                    //#region Arrange

                    //if (UserSettings.All.WatermarkLeft < 0)
                    //    UserSettings.All.WatermarkLeft = 10;

                    //if (UserSettings.All.WatermarkLeft + 10 > CaptionOverlayGrid.Width)
                    //    UserSettings.All.WatermarkLeft = 10;

                    //if (UserSettings.All.WatermarkTop < 0)
                    //    UserSettings.All.WatermarkTop = 10;

                    //if (UserSettings.All.WatermarkTop + 10 > CaptionOverlayGrid.Height)
                    //    UserSettings.All.WatermarkTop = 10;

                    //#endregion

                    WatermarkGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);

                    #endregion

                    break;
                case PanelType.Border:
                    BorderProperties_ValueChanged(null, null);
                    BorderGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelectedOrAll", true);
                    break;
                case PanelType.Obfuscate:
                    ObfuscateOverlaySelectControl.Scale = this.Scale();
                    ObfuscateOverlaySelectControl.Retry();
                    ObfuscateGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.Progress:
                    ProgressGrid.Visibility = Visibility.Visible;
                    ChangeProgressTextToCurrent();
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelType.Shadow:
                    ShadowProperties_ValueChanged(null, null);
                    ShadowGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelType.OverrideDelay:
                    OverrideDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.IncreaseDecreaseDelay:
                    IncreaseDecreaseDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.ScaleDelay:
                    ScaleDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelected", true);
                    break;
                case PanelType.Cinemagraph:
                    CinemagraphGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.Cinemagraph", true);
                    break;
                case PanelType.Fade:
                    FadeGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Transitions.Info", true);
                    break;
                case PanelType.Slide:
                    SlideGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Transitions.Info", true);
                    break;
                case PanelType.ReduceFrames:
                    ReduceGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplySelectedOrAll", true);
                    break;
                case PanelType.RemoveDuplicates:
                    RemoveDuplicatesGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
                case PanelType.MouseClicks:
                    MouseClicksGrid.Visibility = Visibility.Visible;
                    ShowHint("S.Hint.ApplyAll", true);
                    break;
            }

            #endregion

            #region Focus

            if (focusFirstVisibleChild)
            {
                var visible = ActionInternalGrid.Children.OfType<Grid>().FirstOrDefault(x => x.Visibility == Visibility.Visible);

                if (visible != null)
                {
                    visible.Focus();
                    visible.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }

            #endregion

            #region Animate

            if ((type == PanelType.SaveAs || type == PanelType.LoadRecent) && ActionGrid.Width < 300)
                ActionGrid.BeginStoryboard(this.FindStoryboard("ShowExtendedPanelStoryboard"), HandoffBehavior.Compose);
            else if (type != PanelType.SaveAs && type != PanelType.LoadRecent && (ActionGrid.Width < 5 || ActionGrid.Width > 280))
                ActionGrid.BeginStoryboard(this.FindStoryboard("ShowPanelStoryboard"), HandoffBehavior.Compose);

            #endregion

            #region Overlay Grid

            if (OverlayGrid.Opacity < 1 && type < 0)
                OverlayGrid.BeginStoryboard(this.FindStoryboard("ShowOverlayGridStoryboard"), HandoffBehavior.Compose);
            else if (OverlayGrid.Opacity > 0 && type > 0)
                OverlayGrid.BeginStoryboard(this.FindStoryboard("HideOverlayGridStoryboard"), HandoffBehavior.Compose);

            #endregion

            CommandManager.InvalidateRequerySuggested();
        }
        
        private void ClosePanel(bool isCancel = false, bool removeEvent = false)
        {
            StatusList.Remove(StatusType.Warning);

            if (ActionGrid.ActualWidth > 0)
                ZoomBoxControl.RestoreSavedZoom();

            HideHint();

            if (isCancel)
                SetFocusOnCurrentFrame();

            if (removeEvent)
                _applyAction = null;

            BeginStoryboard(this.FindStoryboard("HidePanelStoryboard"), HandoffBehavior.Compose);
            BeginStoryboard(this.FindStoryboard("HideOverlayGridStoryboard"), HandoffBehavior.Compose);

            CustomContentControl.Content = null;
            CustomContentControl.Visibility = Visibility.Collapsed;
        }

        private List<int> SelectedFramesIndex()
        {
            return FrameListView.SelectedItems.OfType<FrameListBoxItem>().Select(x => FrameListView.Items.IndexOf(x)).OrderBy(y => y).ToList();
        }

        private bool UpdatePositioning(bool onLoad = true)
        {
            //TODO: When the DPI changes, these values are still from the latest dpi.
            var top = onLoad ? UserSettings.All.EditorTop : Top;
            var left = onLoad ? UserSettings.All.EditorLeft : Left;
            var width = onLoad ? UserSettings.All.EditorWidth : Width;
            var height = onLoad ? UserSettings.All.EditorHeight : Height;
            var state = onLoad ? UserSettings.All.EditorWindowState : WindowState;

            //If the position was never set, let it center on screen. 
            if (double.IsNaN(top) && double.IsNaN(left))
                return false;

            //The catch here is to get the closest monitor from current Top/Left point. 
            var monitors = Monitor.AllMonitorsScaled(this.Scale());
            var closest = monitors.FirstOrDefault(x => x.Bounds.Contains(new Point((int)left, (int)top))) ?? monitors.FirstOrDefault(x => x.IsPrimary);

            if (closest == null)
                return false;

            //To much to the Left.
            if (closest.WorkingArea.Left > left + width - 100)
                left = closest.WorkingArea.Left;

            //Too much to the top.
            if (closest.WorkingArea.Top > top + height - 100)
                top = closest.WorkingArea.Top;

            //Too much to the right.
            if (closest.WorkingArea.Right < left + 100)
                left = closest.WorkingArea.Right - width;

            //Too much to the bottom.
            if (closest.WorkingArea.Bottom < top + 100)
                top = closest.WorkingArea.Bottom - height;

            if (top > int.MaxValue || top < int.MinValue || left > int.MaxValue || left < int.MinValue ||
                width > int.MaxValue || width < int.MinValue || height > int.MaxValue || height < int.MinValue)
            {
                var desc = $"On load: {onLoad}\nScale: {this.Scale()}\n\n" +
                           $"Screen: {closest.AdapterName}\nBounds: {closest.Bounds}\n\nTopLeft: {top}x{left}\nWidthHeight: {width}x{height}\n\n" +
                           $"TopLeft Settings: {UserSettings.All.EditorTop}x{UserSettings.All.EditorLeft}\nWidthHeight Settings: {UserSettings.All.EditorWidth}x{UserSettings.All.EditorHeight}";
                LogWriter.Log("Wrong Editor window sizing", desc);
                return false;
            }

            Top = top;
            Left = left;
            Width = width;
            Height = height;
            WindowState = state;

            return true;
        }

        #endregion
        
        #region Other

        public void NotificationUpdated()
        {
            RibbonTabControl.UpdateNotifications();
        }

        public EncoderListViewItem EncodingAdded(int id)
        {
            return RibbonTabControl.AddEncoding(id, IsActive);
        }

        public void EncodingUpdated(int? id = null, bool onlyStatus = false)
        {
            RibbonTabControl.UpdateEncoding(id, onlyStatus);
        }

        public EncoderListViewItem EncodingRemoved(int id)
        {
            return RibbonTabControl.RemoveEncoding(id);
        }


        /// <summary>
        /// If there's an update available, it will adjust the UI to warn the user about it.
        /// It does not control the notification list, that's separated.
        /// </summary>
        private void DisplayUpdatePromoter()
        {
            if (Global.UpdateAvailable == null)
            {
                UpdateStackPanel.Visibility = Visibility.Collapsed;
                return;
            }

            UpdateVersionRun.Text = $"{Global.UpdateAvailable.Version}";
            UpdateSizeRun.Text = Global.UpdateAvailable.InstallerSize > 0 ? Humanizer.BytesToString(Global.UpdateAvailable.InstallerSize) : "";
            UpdateStackPanel.Visibility = Visibility.Visible;
        }

        private void Discard(bool notify = true)
        {
            Pause();

            if (notify && !Dialog.Ask(LocalizationHelper.Get("S.Editor.DiscardProject.Title"), LocalizationHelper.Get("S.Editor.DiscardProject.Instruction"), LocalizationHelper.Get("S.Editor.DiscardProject.Message"), false))
                return;

            #region Prepare UI

            ClosePanel();

            FrameListView.SelectedIndex = -1;
            FrameListView.SelectionChanged -= FrameListView_SelectionChanged;

            FrameListView.Items.Clear();
            ClipboardListBox.Items.Clear();
            Util.Clipboard.Items.Clear();
            ZoomBoxControl.Clear();

            #endregion

            if (Project == null || !Project.Any) return;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(Project, DiscardCallback, null);
        }

        private void DeleteFrame(int index)
        {
            //Delete the File from the disk.
            File.Delete(Project.Frames[index].Path);

            //Remove from the list.
            Project.Frames.RemoveAt(index);
            FrameListView.Items.RemoveAt(index);
        }

        private List<FrameInfo> SelectedFrames()
        {
            var selectedIndexList = Dispatcher.Invoke(SelectedFramesIndex);
            return Project.Frames.Where(x => selectedIndexList.Contains(Project.Frames.IndexOf(x))).ToList();
        }
        
        private void UpdateStatistics()
        {
            TotalDuration = TimeSpan.FromMilliseconds(Project.Frames.Sum(x => x.Delay));
            FrameSize = Project.Frames.Count > 0 ? Project.Frames[0].Path.ScaledSize() : new Size(0, 0);
            FrameScale = Project.Frames.Count > 0 ? Convert.ToInt32(Project.Frames[0].Path.DpiOf() / 96d * 100d) : 0;
            AverageDelay = Project.Frames.Count > 0 ? Project.Frames.Average(x => x.Delay) : 0;
            FrameDpi = Project.Frames.Count > 0 ? Math.Round(Project.Frames[0].Path.DpiOf(), 0) : 0d;
        }

        private void UpdateOtherStatistics()
        {
            if (FrameListView.SelectedIndex > -1 && FrameListView.SelectedIndex < Project.Frames.Count)
                CurrentTime = TimeSpan.FromMilliseconds(Project.Frames.Take(FrameListView.SelectedIndex + 1).Sum(x => x.Delay));
            else
                CurrentTime = TimeSpan.Zero;
        }

        private void ShowHint(string hint, bool isPermanent = false, params object[] values)
        {
            if (HintTextBlock.Visibility == Visibility.Visible)
                BeginStoryboard(this.FindStoryboard("HideHintStoryboard"), HandoffBehavior.Compose);

            if (values.Length == 0)
                HintTextBlock.Text = TryFindResource(hint) + "";
            else
                HintTextBlock.Text = string.Format(TryFindResource(hint) + "", values);

            BeginStoryboard(this.FindStoryboard(isPermanent ? "ShowPermanentHintStoryboard" : "ShowHintStoryboard"), HandoffBehavior.Compose);
        }

        private void HideHint()
        {
            if (HintTextBlock.Visibility == Visibility.Visible)
                BeginStoryboard(this.FindStoryboard("HideHintStoryboard"), HandoffBehavior.Compose);
        }

        private void SetFocusOnCurrentFrame()
        {
            FrameListView.Focus();

            if (!(FrameListView.SelectedItem is FrameListBoxItem current))
                return;

            Keyboard.Focus(current);
            current.Focus();
        }

        private string GetProgressText(int precision, bool showTotal, string format, string dateFormat, int startNumber, long cumulative, long total, int current)
        {
            try
            {
                switch (precision)
                {
                    case 0: //Minutes
                        return showTotal ? TimeSpan.FromMilliseconds(cumulative).ToString(@"m\:ss") + "/" + TimeSpan.FromMilliseconds(total).ToString(@"m\:ss") : TimeSpan.FromMilliseconds(cumulative).ToString(@"m\:ss");
                    case 1: //Seconds
                        return showTotal ? (int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds + "/" + TimeSpan.FromMilliseconds(total).TotalSeconds + " s" : (int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds + " s";
                    case 2: //Milliseconds
                        return showTotal ? cumulative + "/" + total + " ms" : cumulative + " ms";
                    case 3: //Percentage
                        var count = (double)Project.Frames.Count;
                        return (current / count * 100).ToString("##0.#", CultureInfo.CurrentUICulture) + (showTotal ? "/100%" : " %");
                    case 4: //Frame number
                        return showTotal ? (startNumber + current - 1) + "/" + (startNumber + Project.Frames.Count - 1) : current.ToString();
                    case 5: //Custom
                        return format
                            .Replace("$ms", cumulative.ToString())
                            .Replace("$s", ((int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds).ToString())
                            .Replace("$m", TimeSpan.FromMilliseconds(cumulative).ToString())
                            .Replace("$p", (current / (double)Project.Frames.Count * 100).ToString("##0.#", CultureInfo.CurrentUICulture))
                            .Replace("$f", (startNumber + current - 1).ToString())
                            .Replace("@ms", total.ToString())
                            .Replace("@s", ((int)TimeSpan.FromMilliseconds(total).TotalSeconds).ToString())
                            .Replace("@m", TimeSpan.FromMilliseconds(total).ToString(@"m\:ss"))
                            .Replace("@p", "100")
                            .Replace("@f", (startNumber + Project.Frames.Count - 1).ToString());
                    case 6: //Actual date/time
                        return showTotal ? $"{Project.CreationDate.AddMilliseconds(cumulative).ToString(dateFormat)} -> {Project.CreationDate.AddMilliseconds(total).ToString(dateFormat)}"
                            : Project.CreationDate.AddMilliseconds(cumulative).ToString(dateFormat);
                    default:
                        return "???";
                }
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Invalid progress format.");
                return "???";
            }
        }

        private void ChangeProgressTextToCurrent()
        {
            var total = Project.Frames.Sum(y => y.Delay);
            var cumulative = 0L;

            for (var j = 0; j < FrameListView.SelectedIndex; j++)
                cumulative += Project.Frames[j].Delay;

            ProgressHorizontalTextBlock.Text = GetProgressText(UserSettings.All.ProgressPrecision, UserSettings.All.ProgressShowTotal, UserSettings.All.ProgressFormat, UserSettings.All.ProgressDateFormat,
                UserSettings.All.ProgressStartNumber, cumulative, total, FrameListView.SelectedIndex + 1);
        }
        
        #endregion

        #endregion


        #region Async

        #region Async Load Recent

        private delegate void LoadRecentDelegate();

        private LoadRecentDelegate _loadRecentDel;

        private void LoadRecentAsync()
        {
            ShowProgress(LocalizationHelper.Get("S.Recent.Searching"), 100, true);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Enumerate recent projects

            var list = new List<ProjectInfo>();

            try
            {
                Dispatcher.Invoke(() => RecentDataGrid.ItemsSource = null);

                var path = Path.Combine(UserSettings.All.TemporaryFolderResolved, "ScreenToGif", "Recording");

                if (!Directory.Exists(path))
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => RecentDataGrid.ItemsSource = null));
                    return;
                }

                var folderList = Directory.GetDirectories(path).Select(x => new DirectoryInfo(x)).ToList();

                foreach (var folder in folderList)
                {
                    var file = Path.Combine(folder.FullName, "Project.json");

                    if (!File.Exists(file))
                        continue;

                    var json = File.ReadAllText(file);

                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var ser = new DataContractJsonSerializer(typeof(ProjectInfo));
                        var project = ser.ReadObject(ms) as ProjectInfo;

                        //Ignore empty projects.
                        if (project == null || project.Frames.Count == 0 || !project.Frames.Any(x => File.Exists(x.Path)))
                            continue;

                        list.Add(project);
                    }
                }

                //Waits the animation to complete before filling the grid.
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    RecentDataGrid.ItemsSource = list;

                    if (RecentDataGrid.Items.Count > 0)
                    {
                        RecentDataGrid.Sort(ListSortDirection.Descending, "CreationDate");
                        RecentDataGrid.FocusOnFirstCell();
                    }
                }));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Loading list of recent");

                Dispatcher.Invoke(() => ErrorDialog.Ok("ScreenToGif", "Error while enumerating recent projects", ex.Message, ex));
            }

            #endregion
        }

        private void LoadRecentCallback(IAsyncResult ar)
        {
            _loadRecentDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;

                HideProgress();

                CommandManager.InvalidateRequerySuggested();
            });

            GC.Collect();
        }

        #endregion

        #region Async Save

        private void SaveAsync(ExportPreset preset)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.PreparingSaving"), 1, true);

            #region Filter out frames

            var indexes = new List<int>();

            if (preset.ExportPartially)
            {
                switch (preset.PartialExport)
                {
                    case PartialExportType.FrameExpression:
                    {
                        var blocks = preset.PartialExportFrameExpression.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var block in blocks)
                        {
                            //Frame range.
                            if (block.Contains("-"))
                            {
                                var subs = block.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                                if (!int.TryParse(subs[0], out var start) || !int.TryParse(subs[1], out var end))
                                    continue;

                                if (start == end) 
                                {
                                    //Single frame.
                                    indexes.Add(start); //indexes.Add(frames[start]);
                                }
                                else if (start < end) 
                                {
                                    //Normal range.
                                    for (var i = start; i <= end; i++)
                                        indexes.Add(i); //indexes.Add(frames[i]);
                                }
                                else 
                                {
                                    //Reversed range.
                                    //The CopyToExport() method bellow has no support for this yet, so it just copies the frames in order anyway.
                                    for (var i = start; i >= end; i--)
                                        indexes.Add(i); //indexes.Add(frames[i]);
                                }
                            }
                            else
                            {
                                if (int.TryParse(block, out var number))
                                    indexes.Add(number);
                            }
                        }

                        break;
                    }

                    case PartialExportType.FrameRange:
                    {
                        indexes = Enumerable.Range(preset.PartialExportFrameStart, preset.PartialExportFrameEnd - preset.PartialExportFrameStart).ToList();
                        break;
                    }

                    case PartialExportType.TimeRange:
                    {
                        var span = TimeSpan.Zero;

                        //Playback mode.
                        if (preset.PartialExportTimeStart < preset.PartialExportTimeEnd)
                        {
                            //Normal playback.
                            for (var i = 0; i < Project.Frames.Count; i++)
                            {
                                if (span >= preset.PartialExportTimeStart && span <= preset.PartialExportTimeEnd)
                                    indexes.Add(i);

                                if (span > preset.PartialExportTimeEnd)
                                    break;

                                span = span.Add(TimeSpan.FromMilliseconds(Project.Frames[i].Delay));
                            }
                        }
                        else
                        {
                            //Reversed playback.
                            //The CopyToExport() method bellow has no support for this yet, so it just copies the frames in order anyway.
                            span = TimeSpan.FromMilliseconds(Project.Frames.Sum(s => s.Delay));
                            
                            for (var i = Project.Frames.Count - 1; i >= 0 ; i--)
                            {
                                if (span >= preset.PartialExportTimeEnd && span <= preset.PartialExportTimeStart)
                                    indexes.Add(i);

                                if (span > preset.PartialExportTimeStart)
                                    break;

                                span = span.Subtract(TimeSpan.FromMilliseconds(Project.Frames[i].Delay));
                            }
                        }

                        break;
                    }

                    case PartialExportType.Selection:
                    {
                        indexes = Dispatcher.Invoke(SelectedFramesIndex);
                        break;
                    }
                }
            }

            #endregion

            #region Last minute validation

            //Validates each file name when saving multiple images (if more than one image, that will not be zipped).
            if (preset is ImagePreset imagePreset && !imagePreset.ZipFiles)
            {
                var output = Path.Combine(preset.OutputFolder, preset.ResolvedFilename);
                var padSize = (Project.Frames.Count - 1).ToString().Length;

                if (indexes.Count > 1 ? indexes.Any(a => File.Exists($"{output} ({(a + "").PadLeft(padSize, '0')})" + preset.Extension)) : File.Exists(output + preset.Extension))
                {
                    Dispatcher.Invoke(() => StatusList.Warning("S.SaveAs.Warning.Overwrite"));
                    return;
                }

                if (indexes.Count > 1 && !Dispatcher.Invoke(() => Dialog.Ask(LocalizationHelper.Get("S.SaveAs.Dialogs.Multiple.Title"), 
                    LocalizationHelper.Get("S.SaveAs.Dialogs.Multiple.Instruction"), LocalizationHelper.GetWithFormat("S.SaveAs.Dialogs.Multiple.Message", indexes.Count))))
                {
                    Dispatcher.Invoke(() => StatusList.Warning(LocalizationHelper.Get("S.SaveAs.Warning.Canceled")));
                    return;
                }
            }

            #endregion

            //Copy the frames, so it can be manipulated without problem.
            var copied = Project.CopyToExport(indexes, preset.Type == Export.Stg, preset.Type == Export.Gif && preset.Encoder == EncoderType.ScreenToGif);

            EncodingManager.StartEncoding(copied, preset);

            if (preset.ExportAsProjectToo)
            {
                var copiedAux = Project.CopyToExport(indexes, true);

                //Get default project encoder settings.
                var projectPreset = UserSettings.All.ExportPresets.OfType<StgPreset>().FirstOrDefault(f => f.IsSelectedForEncoder) ?? UserSettings.All.ExportPresets.OfType<StgPreset>().FirstOrDefault() ?? StgPreset.Default;
                projectPreset.OutputFolder = preset.OutputFolder;
                projectPreset.OutputFilename = preset.ResolvedFilename;

                EncodingManager.StartEncoding(copiedAux, projectPreset); 
            }
        }

        #endregion

        #region Async Discard

        private delegate void DiscardFrames(ProjectInfo project);

        private DiscardFrames _discardFramesDel;

        private void Discard(ProjectInfo project)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingFrames"), project.Frames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            try
            {
                var count = 0;
                foreach (var frame in project.Frames)
                {
                    File.Delete(frame.Path);

                    UpdateProgress(count++);
                }

                var folderList = Directory.EnumerateDirectories(project.FullPath).ToList();

                ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingFolders"), folderList.Count);

                count = 0;
                foreach (var folder in folderList)
                {
                    if (!folder.Contains("Encode "))
                        Directory.Delete(folder, true);

                    UpdateProgress(count++);
                }

                //Deletes the JSON file.
                if (File.Exists(project.ProjectPath))
                    File.Delete(project.ProjectPath);
            }
            catch (IOException io)
            {
                LogWriter.Log(io, "Error while trying to Discard the Project");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Dialog.Ok("Discard Error", "Error while trying to discard the project", ex.Message));
                LogWriter.Log(ex, "Error while trying to Discard the Project");
            }

            ActionStack.Clear();
            project.Clear();

            HideProgress();
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                WelcomeGrid.BeginStoryboard(this.FindStoryboard("ShowWelcomeBorderStoryboard"), HandoffBehavior.Compose);

                FilledList = false;
                IsLoading = false;

                WelcomeTextBlock.Text = LocalizationHelper.Get(Humanizer.WelcomeInfo());
                SymbolTextBlock.Text = Humanizer.Welcome();

                UpdateStatistics();

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;

                CommandManager.InvalidateRequerySuggested();
            }));

            GC.Collect();
        }

        private void DiscardAndLoadCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                FilledList = false;

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
            }));

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);

            GC.Collect();
        }

        #endregion

        #region Async Resize

        private delegate void ResizeFrames(int width, int height, double dpi, BitmapScalingMode scalingQuality);

        private ResizeFrames _resizeFramesDel;

        private void Resize(int width, int height, double dpi, BitmapScalingMode scalingQuality)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ResizingFrames"), Project.Frames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in Project.Frames)
            {
                var png = new PngBitmapEncoder();
                png.Frames.Add(ImageMethods.ResizeImage((BitmapImage)frame.Path.SourceFrom(), width, height, 0, dpi, scalingQuality));

                using (Stream stm = File.OpenWrite(frame.Path))
                    png.Save(stm);

                UpdateProgress(count++);
            }
        }

        private void ResizeCallback(IAsyncResult ar)
        {
            _resizeFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Crop

        private delegate void CropFrames(Int32Rect rect);

        private CropFrames _cropFramesDel;

        private void Crop(Int32Rect rect)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.CroppingFrames"), Project.Frames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in Project.Frames)
            {
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(frame.Path.CropFrom(rect)));

                using (Stream stm = File.OpenWrite(frame.Path))
                    png.Save(stm);

                UpdateProgress(count++);
            }
        }

        private void CropCallback(IAsyncResult ar)
        {
            _cropFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Progress

        private delegate void ProgressDelegate(ProgressModel model);

        private ProgressDelegate _progressDelegateDel;

        private void ProgressAsync(ProgressModel model)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var total = Project.Frames.Sum(y => y.Delay);
            var thickness = model.Thickness * ZoomBoxControl.ScaleDiff;
            var fontSize = model.FontSize * ZoomBoxControl.ScaleDiff;

            var count = 1;
            var cumulative = 0L;

            foreach (var frame in Project.Frames)
            {
                var image = frame.Path.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));

                    //TODO: Test with high dpi.
                    if (model.Type == ProgressType.Bar)
                    {
                        #region Bar

                        if (model.Orientation == Orientation.Horizontal)
                        {
                            //Width changes (Current/Total * Available size), Height is thickness.
                            var width = count / (double)Project.Frames.Count * image.Width; //* image.Width instead?
                            var left = model.HorizontalAlignment == HorizontalAlignment.Left ? 0 :
                                model.HorizontalAlignment == HorizontalAlignment.Right ? image.Width - width :
                                (image.Width - width) / 2d;
                            var top = model.VerticalAlignment == VerticalAlignment.Top ? 0 :
                                model.VerticalAlignment == VerticalAlignment.Bottom ? image.Height - thickness :
                                (image.Height - thickness) / 2d;

                            drawingContext.DrawRectangle(new SolidColorBrush(model.Color), null, new Rect(Math.Round(left, 0), Math.Round(top, 0), Math.Round(width, 0), thickness));
                        }
                        else
                        {
                            //Height changes (Current/Total * Available size), Width is thickness.
                            var height = count / (double)Project.Frames.Count * image.Height;
                            var left = model.HorizontalAlignment == HorizontalAlignment.Left ? 0 :
                                model.HorizontalAlignment == HorizontalAlignment.Right ? image.Width - thickness :
                                (image.Width - thickness) / 2d;
                            var top = model.VerticalAlignment == VerticalAlignment.Top ? 0 :
                                model.VerticalAlignment == VerticalAlignment.Bottom ? image.Height - height :
                                (image.Height - height) / 2d;

                            drawingContext.DrawRectangle(new SolidColorBrush(model.Color), null, new Rect(Math.Round(left, 0), Math.Round(top, 0), thickness, Math.Round(height, 0)));
                        }

                        #endregion
                    }
                    else
                    {
                        #region Text

                        if (count > 0)
                            cumulative += Project.Frames[count - 1].Delay;

                        //Calculate size.
                        var text = GetProgressText(model.Precision, model.ShowTotal, model.Format, model.DateFormat, model.StartNumber, cumulative, total, count); //FrameListView.SelectedIndex
                        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                            new Typeface(model.FontFamily, model.FontStyle, model.FontWeight, default), fontSize, new SolidColorBrush(model.FontColor),
                            null, TextFormattingMode.Ideal);

                        var width = formatted.Width + 4; //2px padding for both sides. 
                        var height = formatted.Height;
                        var left = model.HorizontalAlignment == HorizontalAlignment.Left ? 0 :
                            model.HorizontalAlignment == HorizontalAlignment.Right ? image.Width - width :
                            (image.Width - width) / 2d;
                        var top = model.VerticalAlignment == VerticalAlignment.Top ? 0 :
                            model.VerticalAlignment == VerticalAlignment.Bottom ? image.Height - height :
                            (image.Height - height) / 2d;

                        //Draw background rectangle and the text.
                        drawingContext.DrawRectangle(new SolidColorBrush(model.Color), null, new Rect(Math.Round(left, 0), Math.Round(top, 0), Math.Round(width, 0), Math.Round(height, 0)));
                        drawingContext.DrawText(formatted, new Point(Math.Round(left + 2, 0), Math.Round(top, 0)));

                        #endregion
                    }
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress((count++) - 1);
            }
        }

        private void ProgressCallback(IAsyncResult ar)
        {
            _progressDelegateDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Merge Frames

        private delegate List<int> OverlayFrames(RenderTargetBitmap render, bool forAll = false);

        private OverlayFrames _overlayFramesDel;

        private List<int> OverlayAsync(RenderTargetBitmap render, bool forAll = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? Project.Frames.Select(x => Project.Frames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), frameList.Count);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawImage(render, new Rect(0, 0, render.Width, render.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private void OverlayCallback(IAsyncResult ar)
        {
            var selected = _overlayFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowHint("S.Hint.Overlay");

                LoadSelectedStarter(selected.Min(), selected.Max());
            }));
        }


        //Currently not being used.
        private delegate List<int> OverlayMultipleFrames(List<RenderTargetBitmap> render, double dpi, bool forAll = false);

        private OverlayMultipleFrames _overlayMultipleFramesDel;

        private List<int> OverlayMultiple(List<RenderTargetBitmap> renderList, double dpi, bool forAll = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? Project.Frames.Select(x => Project.Frames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), frameList.Count);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawImage(renderList[count], new Rect(0, 0, renderList[count].Width, renderList[count].Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private void OverlayMultipleCallback(IAsyncResult ar)
        {
            var selected = _overlayMultipleFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                ShowHint("S.Hint.Overlay");

                LoadSelectedStarter(selected.Min(), selected.Max());
            });
        }

        #endregion

        #region Async Title Frame

        private delegate int TitleFrameAction(RenderTargetBitmap render, int selected, double dpi);

        private TitleFrameAction _titleFrameDel;

        private int TitleFrame(RenderTargetBitmap render, int selected, double dpi)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.CreatingTitleFrame"), 1, true);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Save Image

            var name = Path.GetFileNameWithoutExtension(Project.Frames[selected].Path);
            var folder = Path.GetDirectoryName(Project.Frames[selected].Path);
            var fileName = Path.Combine(folder, $"{name} TF {DateTime.Now:hh-mm-ss}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));

            //Saves the image into a file using the encoder.
            using (Stream stream = File.Create(fileName))
                encoder.Save(stream);

            GC.Collect();

            #endregion

            //Adds to the List
            Project.Frames.Insert(selected, new FrameInfo(fileName, UserSettings.All.TitleFrameDelay));

            return selected;
        }

        private void TitleFrameCallback(IAsyncResult ar)
        {
            var selected = _titleFrameDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowHint("S.Hint.TitleFrame");

                LoadSelectedStarter(selected, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Keystrokes

        private delegate void KeyStrokesDelegate(KeyStrokesModel model);

        private KeyStrokesDelegate _keyStrokesDelegate;

        private void KeyStrokesAsync(KeyStrokesModel model)
        {
            Dispatcher?.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var auxList = Project.Frames.CopyList();

            #region Make the keystrokes start earlier

            if (model.KeyStrokesEarlier)
            {
                //Checks if there is a key stroke that needs to be shown earlier, into the previous frames.
                for (var outer = 0; outer < auxList.Count; outer++)
                {
                    //For each frame, check if any other key pressed later wants to be shown on this frame.

                    var amount = 0;

                    //If it's the last item of the list.
                    //if (outer == auxList.Count - 1 && amount <= model.KeyStrokesEarlierBy)
                    //    auxList[outer].KeyList.InsertRange(auxList[outer].KeyList.Count, auxList[0].KeyList);

                    //Check next itens.
                    for (var inner = outer + 1; inner < auxList.Count - 1; inner++)
                    {
                        //For each frame, check if a next frame needs to show their key strokes.
                        amount += auxList[inner].Delay;

                        //If next item bleeds into this frame, insert on the list.
                        if (inner < auxList.Count - 1 && auxList[inner + 1].Delay <= model.KeyStrokesEarlierBy)
                            auxList[outer].KeyList.InsertRange(auxList[outer].KeyList.Count, auxList[inner].KeyList);

                        //Stops veryfying the previous frames if the delay sum is greater than the maximum.
                        if (amount >= model.KeyStrokesEarlierBy)
                            break;
                    }
                }
            }

            #endregion

            #region Expand the keystrokes

            if (model.KeyStrokesExtended)
            {
                //Checks from the end to the start, if there is a key stroke that needs to 'bleed'/'delayed' into the next frames.
                //I need to check from the end to the start so the keystrokes stays in position. 
                for (var outer = auxList.Count - 1; outer > 0; outer--)
                {
                    var amount = 0;

                    if (outer == 1 && amount <= model.KeyStrokesDelay)
                    {
                        var listA = auxList[0].KeyList.TakeWhile((_, i) => !auxList[0].KeyList.Skip(i).SequenceEqual(auxList[1].KeyList.Take(auxList[0].KeyList.Count - i))).Concat(auxList[1].KeyList);

                        auxList[1].KeyList = new List<SimpleKeyGesture>(listA);
                    }

                    //Check previous itens.
                    for (var inner = outer - 1; inner >= 0; inner--)
                    {
                        //For each frame, check if a previous frame needs to show their key strokes.
                        amount += auxList[inner].Delay;

                        //If previous item bleeds into this frame, insert on the list.
                        if (inner > 0 && auxList[inner - 1].Delay <= model.KeyStrokesDelay)
                        {
                            var listA = auxList[inner].KeyList.TakeWhile((_, i) => !auxList[inner].KeyList.Skip(i).SequenceEqual(auxList[outer].KeyList.Take(auxList[inner].KeyList.Count - i))).Concat(auxList[outer].KeyList);

                            auxList[outer].KeyList = new List<SimpleKeyGesture>(listA);
                        }

                        //Stops veryfying the previous frames if the delay sum is greater than the maximum.
                        if (amount >= model.KeyStrokesDelay)
                            break;
                    }
                }
            }

            #endregion

            //Pen used for drawing the outline of the text shape.
            var pen = new Pen(new SolidColorBrush(model.KeyStrokesOutlineColor), model.KeyStrokesOutlineThickness)
            {
                DashCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round
            };

            var count = 0;
            foreach (var frame in auxList)
            {
                if (!frame.KeyList.Any())
                {
                    UpdateProgress(count++);
                    continue;
                }

                #region Removes any duplicated modifier key

                var keyList = new List<SimpleKeyGesture>();
                for (var i = 0; i < frame.KeyList.Count; i++)
                {
                    if (model.KeyStrokesIgnoreInjected && frame.KeyList[i].IsInjected)
                        continue;

                    //Ignore Control, Shift, Alt and Windows keys if not acting as modifiers.
                    if (model.KeyStrokesIgnoreNonModifiers && (frame.KeyList[i].Key >= Key.LeftShift && frame.KeyList[i].Key <= Key.RightAlt || frame.KeyList[i].Key == Key.LWin || frame.KeyList[i].Key == Key.RWin))
                        continue;

                    //If there's another key ahead on the same frame.
                    if (frame.KeyList.Count > i + 1)
                    {
                        //If this frame being added will be repeated next, ignore.
                        if (frame.KeyList[i + 1].Key == frame.KeyList[i].Key && frame.KeyList[i + 1].Modifiers == frame.KeyList[i].Modifiers)
                            continue;

                        //TODO: If there's a key between the current key and the one that is repeated, they are going to be shown.

                        //If this frame being added will be repeated within the next key presses as a modifier, ignore.
                        if ((frame.KeyList[i].Key == Key.LeftCtrl || frame.KeyList[i].Key == Key.RightCtrl) && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Control) != 0)
                            continue;

                        if ((frame.KeyList[i].Key == Key.LeftShift || frame.KeyList[i].Key == Key.RightShift) && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Shift) != 0)
                            continue;

                        if ((frame.KeyList[i].Key == Key.LeftAlt || frame.KeyList[i].Key == Key.RightAlt) && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Alt) != 0)
                            continue;

                        if ((frame.KeyList[i].Key == Key.LWin || frame.KeyList[i].Key == Key.RWin) && (frame.KeyList[i + 1].Modifiers & ModifierKeys.Windows) != 0)
                            continue;
                    }

                    //Removes the previous modifier key, if a combination is next to it: "LeftCtrl Control + A" will be "Control + A". (This checks if the next modifier is not present as a current key).
                    if (i + 1 > frame.KeyList.Count - 1 || !(frame.KeyList[i].Key != Key.Left && frame.KeyList[i].Key != Key.Right && frame.KeyList[i + 1].Modifiers.ToString().Contains(frame.KeyList[i].Key.ToString().Remove("Left", "Right").Replace("Ctrl", "Control").TrimStart('L').TrimStart('R'))))
                        keyList.Add(frame.KeyList[i]);
                }

                if (keyList.Count == 0)
                {
                    UpdateProgress(count++);
                    continue;
                }

                #endregion

                #region Prepare the text

                var text = keyList.Select(x => "" + Util.Native.GetSelectKeyText(x.Key, x.Modifiers, x.IsUppercase)).Aggregate((p, n) => p + model.KeyStrokesSeparator + n);

                if (string.IsNullOrEmpty(text))
                {
                    UpdateProgress(count++);
                    continue;
                }

                #endregion

                var image = frame.Path.SourceFrom();

                #region Check if margins and paddings are set properly

                if (image.Width - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2 <= 0 || image.Height - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2 <= 0)
                {
                    UpdateProgress(count++);
                    continue;
                }

                #endregion

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    //The FormattedText class helps in transforming the text to a shape.
                    var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(model.KeyStrokesFontFamily, model.KeyStrokesFontStyle, model.KeyStrokesFontWeight, default), model.KeyStrokesFontSize,
                        new SolidColorBrush(model.KeyStrokesFontColor), null, TextFormattingMode.Ideal)
                    {
                        MaxTextWidth = image.Width - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2,
                        MaxTextHeight = image.Height - (model.KeyStrokesPadding + model.KeyStrokesMargin) * 2,
                    };

                    //TODO: Test with high dpi.

                    var geometry = formatted.BuildGeometry(new Point(0.5, 0.5));
                    var bounds = geometry.GetRenderBounds(pen);

                    var widthText = bounds.Width + model.KeyStrokesPadding * 2;
                    var heightText = bounds.Height + model.KeyStrokesPadding * 2 + 1; //Why 1?

                    var leftText =
                        model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Left ? model.KeyStrokesMargin - bounds.X :
                        model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Right ? image.Width - widthText - bounds.X - model.KeyStrokesMargin :
                        (image.Width - widthText - bounds.X) / 2d;

                    var topText =
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Top ? model.KeyStrokesMargin - bounds.Y :
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Bottom ? image.Height - heightText - bounds.Y - model.KeyStrokesMargin :
                        (image.Height - heightText) / 2d - bounds.Y;

                    var widthRectangle = model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Stretch ? image.Width - model.KeyStrokesMargin * 2 : widthText;
                    var heightRectangle = model.KeyStrokesVerticalAlignment == VerticalAlignment.Stretch ? image.Height - model.KeyStrokesMargin * 2 : Math.Max(heightText, model.KeyStrokesMinHeight);

                    //Center text when setting minimum height.
                    var minHeightAdjustment = model.KeyStrokesMinHeight > heightText && model.KeyStrokesVerticalAlignment != VerticalAlignment.Center && model.KeyStrokesVerticalAlignment != VerticalAlignment.Stretch ?
                        ((model.KeyStrokesMinHeight - heightText) / 2d) * (model.KeyStrokesVerticalAlignment == VerticalAlignment.Bottom ? -1 : 1) : 0;

                    var minHeightAdjustmentRectangle = model.KeyStrokesMinHeight > heightText && model.KeyStrokesVerticalAlignment == VerticalAlignment.Center ? ((model.KeyStrokesMinHeight - heightText) / 2d) : 0;

                    var leftRectangle =
                        model.KeyStrokesHorizontalAlignment == HorizontalAlignment.Stretch ? model.KeyStrokesMargin :
                        leftText + bounds.X < model.KeyStrokesMargin ? model.KeyStrokesMargin : leftText + bounds.X;

                    var topRectangle =
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Stretch || model.KeyStrokesVerticalAlignment == VerticalAlignment.Top ? model.KeyStrokesMargin :
                        model.KeyStrokesVerticalAlignment == VerticalAlignment.Bottom ? image.Height - heightRectangle - model.KeyStrokesMargin :
                        topText + bounds.Y < model.KeyStrokesMargin ? model.KeyStrokesMargin : topText + bounds.Y - minHeightAdjustmentRectangle;

                    geometry.Transform = new TranslateTransform(Math.Round(leftText + model.KeyStrokesPadding, 0), Math.Round(topText + model.KeyStrokesPadding + minHeightAdjustment, 0));

                    //Draws everything in order, the image, the rectangle and the text.
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawRectangle(new SolidColorBrush(model.KeyStrokesBackgroundColor), null, new Rect(Math.Round(leftRectangle, 0), Math.Round(topRectangle, 0), Math.Round(widthRectangle, 0), Math.Round(heightRectangle, 0)));

                    //This code will draw the outline outside the text. 
                    if (UserSettings.All.DrawOutlineOutside)
                    {
                        drawingContext.DrawGeometry(null, pen, geometry);
                        drawingContext.DrawGeometry(new SolidColorBrush(model.KeyStrokesFontColor), null, geometry);
                    }
                    else
                        drawingContext.DrawGeometry(new SolidColorBrush(model.KeyStrokesFontColor), pen, geometry);
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                GC.WaitForPendingFinalizers();
                GC.Collect(1);

                UpdateProgress(count++);
            }
        }

        private void KeyStrokesCallback(IAsyncResult ar)
        {
            _keyStrokesDelegate.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Border

        private delegate void BorderDelegate(BorderModel model);

        private BorderDelegate _borderDelegate;

        private void BorderAsync(BorderModel model)
        {
            Dispatcher?.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            //If the borders are negative or this was called by an automatic task, select all frames.
            var frames = model.LeftThickness < 0 || model.TopThickness < 0 || model.RightThickness < 0 || model.BottomThickness < 0 || !model.IsManual ? Project.Frames : SelectedFrames();
            var scale = Math.Round(ZoomBoxControl.ImageDpi / 96d, 2); //ZoomBoxControl.ImageScale;

            //Since there could be a difference in the DPI of the UI vs the one from the image, I need to adjust the scale of the thickness.
            var leftThick = model.LeftThickness * ZoomBoxControl.ScaleDiff;
            var topThick = model.TopThickness * ZoomBoxControl.ScaleDiff;
            var rightThick = model.RightThickness * ZoomBoxControl.ScaleDiff;
            var bottomThick = model.BottomThickness * ZoomBoxControl.ScaleDiff;

            var count = 0;
            foreach (var frame in frames)
            {
                var image = frame.Path.SourceFrom();
                var drawingVisual = new DrawingVisual();

                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    #region Draws the white rectangle behind with full size

                    var marginHorizontal = Math.Abs((int)Math.Min(leftThick, 0) + Math.Min(rightThick, 0)); //Left and right negative margins as a positive number.
                    var marginVertical = Math.Abs((int)Math.Min(topThick, 0) + Math.Min(bottomThick, 0)); //Top and bottom negative margins as a positive number.

                    drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, image.Width + marginHorizontal, image.Height + marginVertical));

                    #endregion

                    #region Draws the image with the top-left margin

                    var marginLeft = (int)Math.Abs(Math.Min(leftThick, 0)); //Left negative margin as a positive number.
                    var marginTop = (int)Math.Abs(Math.Min(topThick, 0)); //Right negative margin as a positive number.

                    drawingContext.DrawImage(image, new Rect(marginLeft, marginTop, image.Width, image.Height));

                    #endregion

                    #region Draws the 4 lines

                    //The lines are centrally aligned, so they must be drawn at thickness / 2.
                    var brush = new SolidColorBrush(model.Color);

                    var height = image.Height + marginVertical;
                    var width = image.Width + marginLeft - (rightThick > 0 ? rightThick : 0); //image.Width + width / 2d - trueRight / 2d

                    //Left border.
                    var xLeft = Math.Abs(leftThick) / 2d;
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(leftThick)), new Point(xLeft, 0), new Point(xLeft, height));

                    //Right border.
                    var xRight = (leftThick < 0 ? leftThick * -1 : 0) + image.Width + (Math.Abs(rightThick) / 2d * (rightThick < 0 ? 1 : -1));
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(rightThick)), new Point(xRight, 0), new Point(xRight, height));

                    //Top border.
                    var xTop = Math.Abs(leftThick);
                    var yTop = Math.Abs(topThick) / 2d;
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(topThick)), new Point(xTop, yTop), new Point(width, yTop));

                    //Bottom border.
                    var yBottom = (topThick < 0 ? topThick * -1 : 0) + image.Height + (Math.Abs(bottomThick) / 2d * (bottomThick < 0 ? 1 : -1));
                    drawingContext.DrawLine(new Pen(brush, Math.Abs(bottomThick)), new Point(xTop, yBottom), new Point(width, yBottom));

                    #endregion
                }

                var frameHeight = image.PixelHeight + (int)(Math.Round((topThick < 0 ? Math.Abs(topThick) : 0) + (bottomThick < 0 ? Math.Abs(bottomThick) : 0), 0) * scale);
                var frameWidth = image.PixelWidth + (int)(Math.Round((leftThick < 0 ? Math.Abs(leftThick) : 0) + (rightThick < 0 ? Math.Abs(rightThick) : 0), 0) * scale);

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(frameWidth, frameHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void BorderCallback(IAsyncResult ar)
        {
            _borderDelegate.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Shadow

        private delegate void ShadowDelegate(ShadowModel model);

        private ShadowDelegate _shadowDelegate;

        private void ShadowAsync(ShadowModel model)
        {
            Dispatcher?.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var scale = Math.Round(ZoomBoxControl.ImageDpi / 96d, 2); //ZoomBoxControl.ImageScale;
            var blur = model.BlurRadius * ZoomBoxControl.ScaleDiff;
            var depth = model.Depth * ZoomBoxControl.ScaleDiff;

            var count = 0;
            foreach (var frame in Project.Frames)
            {
                var image = frame.Path.SourceFrom();
                var drawingVisual = new DrawingVisual();

                //Sizes:
                var frameHeight = 0;
                var frameWidth = 0;

                drawingVisual.Effect = new DropShadowEffect
                {
                    Color = model.Color,
                    BlurRadius = model.BlurRadius * ZoomBoxControl.ScaleDiff,
                    Opacity = model.Opacity,
                    Direction = model.Direction,
                    ShadowDepth = model.Depth * ZoomBoxControl.ScaleDiff,
                    RenderingBias = RenderingBias.Quality
                };

                //Draws image with shadow.
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    //https://upload.wikimedia.org/wikipedia/commons/thumb/f/fe/Sin_Cos_Tan_Cot_unit_circle.svg/1154px-Sin_Cos_Tan_Cot_unit_circle.svg.png
                    //The cosine of the direction gives the offset of the X axis based on the Width/2 of the point where the circle line crosses the line coming from the center of the circle.
                    //The sine of the direction gives the offset of the Y axis based on the Height/2 of the point where the circle line crosses the line coming from the center of the circle.

                    //• Cosine:
                    //Negative: to the left.
                    //Positive: to the right.
                    //• Sine:
                    //Positive: to the top.
                    //Negative: to the bottom.

                    //<- 180°, 3.14rad
                    //L: Blur + Depth
                    //T: Blur
                    //R: Math.Max(Blur - Depth, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.
                    //B: Blur

                    //-> 0°, 0rad
                    //L: Math.Max(Blur - Depth, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.
                    //T: Blur
                    //R: Blur + Depth
                    //B: Blur

                    //^> 45°, 0.78rad
                    //L: Math.Max(Blur - Depth * ratio, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.
                    //T: Blur + Depth * ratio
                    //R: Blur + Depth * ratio
                    //B: Math.Max(Blur - Depth * ratio, 0) //If the depth is lower than the blur radius, a bit of shadow will appear at the side.

                    //Converts the direction in degrees to radians.
                    var radians = Math.PI / 180.0 * model.Direction;
                    var offsetX = depth * Math.Cos(radians);
                    var offsetY = depth * Math.Sin(radians);

                    var offsetLeft = offsetX < 0 ? offsetX * -1 : 0;
                    var offsetTop = offsetY > 0 ? offsetY : 0;
                    var offsetRight = offsetX > 0 ? offsetX : 0;
                    var offsetBottom = offsetY < 0 ? offsetY * -1 : 0;

                    //Measure drop shadow space.
                    var marginLeft = offsetLeft > 0 ? offsetLeft + blur / 2d : Math.Max(blur / 2d - offsetLeft, 0); //- offsetX
                    var marginTop = offsetTop > 0 ? offsetTop + blur / 2d : Math.Max(blur / 2d - offsetTop, 0); //- offsetY
                    var marginRight = offsetRight > 0 ? offsetRight + blur / 2d : Math.Max(blur / 2d - offsetRight, 0); //+ offsetX
                    var marginBottom = offsetBottom > 0 ? offsetBottom + blur / 2d : Math.Max(blur / 2d - offsetBottom, 0); //+ offsetY

                    drawingContext.DrawImage(image, new Rect((int)marginLeft, (int)marginTop, image.Width, image.Height));

                    frameHeight = (int)((marginTop + image.Height + marginBottom) * scale);
                    frameWidth = (int)((marginLeft + image.Width + marginRight) * scale);
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var innerBmp = new RenderTargetBitmap(frameWidth, frameHeight, ZoomBoxControl.ImageDpi, ZoomBoxControl.ImageDpi, PixelFormats.Pbgra32);
                innerBmp.Render(drawingVisual);

                //Draws background and rendered image on top.
                drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    //Draws the background of the image.
                    drawingContext.DrawRectangle(new SolidColorBrush(model.BackgroundColor), null, new Rect(0, 0, innerBmp.Width, innerBmp.Height));

                    //Image, already with the shadow.
                    drawingContext.DrawImage(innerBmp, new Rect(0, 0, innerBmp.Width, innerBmp.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(frameWidth, frameHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void ShadowCallback(IAsyncResult ar)
        {
            _shadowDelegate.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Flip/Rotate

        private delegate void FlipRotateFrames(FlipRotateType type);

        private FlipRotateFrames _flipRotateFramesDel;

        private void FlipRotate(FlipRotateType type)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingFlipRotate"), Project.Frames.Count);

            var frameList = type == FlipRotateType.RotateLeft90 ||
                type == FlipRotateType.RotateRight90 ? Project.Frames : SelectedFrames();

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();

                Transform transform = null;

                switch (type)
                {
                    case FlipRotateType.FlipVertical:
                        transform = new ScaleTransform(1, -1, 0.5, 0.5);
                        break;
                    case FlipRotateType.FlipHorizontal:
                        transform = new ScaleTransform(-1, 1, 0.5, 0.5);
                        break;
                    case FlipRotateType.RotateLeft90:
                        transform = new RotateTransform(-90, 0.5, 0.5);
                        break;
                    case FlipRotateType.RotateRight90:
                        transform = new RotateTransform(90, 0.5, 0.5);
                        break;
                    default:
                        transform = new ScaleTransform(1, 1, 0.5, 0.5);
                        break;
                }

                var transBitmap = new TransformedBitmap(image, transform);

                // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(transBitmap));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void FlipRotateCallback(IAsyncResult ar)
        {
            _flipRotateFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #region Async Reduce Frames

        private delegate void ReduceFrame(List<int> selection, int factor, int removeCount, ReduceDelayType mode);

        private ReduceFrame _reduceFrameDel;

        private void ReduceFrameCount(List<int> selection, int factor, int removeCount, ReduceDelayType mode)
        {
            var removeList = new List<int>();

            //Gets the list of frames to be removed.
            for (var i = selection.Min() + factor - 1; i < selection.Min() + selection.Count - 1; i += factor + removeCount)
                removeList.AddRange(Util.Other.ListOfIndexes(i + 1, removeCount));

            //Only allow removing frames within the possible range.
            removeList = removeList.Where(x => x < Project.Frames.Count).ToList();

            var alterList = mode == ReduceDelayType.Evenly ? Util.Other.ListOfIndexes(0, Project.Frames.Count).Where(w => !removeList.Contains(w)).ToList() :
                mode == ReduceDelayType.Previous ? (from item in removeList where item - 1 >= 0 select item - 1).ToList() : //.Union(removeList)
                new List<int>(); //No other frame will be altered if the delay is not adjusted.

            if (alterList.Any())
                ActionStack.SaveState(ActionStack.EditAction.RemoveAndAlter, Project.Frames, removeList, alterList);
            else
                ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, removeList);

            var delayRemoved = 0;
            for (var i = removeList.Count - 1; i >= 0; i--)
            {
                var removeIndex = removeList[i];

                if (mode == ReduceDelayType.Previous || factor == 1)
                {
                    //Simply stacks the delay of the removed frames to the previous frame;
                    Project.Frames[removeIndex - 1].Delay += Project.Frames[removeIndex].Delay;
                }
                else if (mode == ReduceDelayType.Evenly)
                {
                    if (i == removeList.Count - 1 || removeList[i] + 1 == removeList[i + 1])
                    {
                        //Store the delay of the frames being removed.
                        delayRemoved += Project.Frames[removeIndex].Delay;
                    }
                    else
                    {
                        if (delayRemoved > 0)
                        {
                            //Calculate the size of the remaining section (this is the factor, the number of frames not being removed in each section).
                            var size = removeList[i + 1] - removeList[i] - 1;

                            //Spread evenly the accumulated delay among the remaining frames.
                            for (var r = removeList[i + 1] - 1; r > removeList[i]; r--)
                                Project.Frames[r].Delay += delayRemoved / size; //Some information may be lost due to rounding.
                        }

                        //Start again the accumulation for this block of frames being removed.
                        delayRemoved = Project.Frames[removeIndex].Delay;
                    }
                }

                File.Delete(Project.Frames[removeIndex].Path);
                Project.Frames.RemoveAt(removeIndex);
            }
        }

        private void ReduceFrameCountCallback(IAsyncResult ar)
        {
            _reduceFrameDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (var i = FrameListView.Items.Count - 1; i >= Project.Frames.Count; i--)
                    FrameListView.Items.RemoveAt(i);

                SelectNear(LastSelected);
                Project.Persist();

                //TODO: Load from the start.
                LoadSelectedStarter(ReduceFactorIntegerUpDown.Value - 1, Project.Frames.Count - 1);

                ShowHint("S.Hint.Reduce");
            }));
        }

        #endregion

        #region Async Remove Duplicates

        private delegate int RemoveDuplicates(double similarity, DuplicatesRemovalType removal, DuplicatesDelayType delay);

        private RemoveDuplicates _removeDuplicatesDel;

        private int RemoveDuplicatesAsync(double similarity, DuplicatesRemovalType removal, DuplicatesDelayType delay)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            var removeList = new List<int>();
            var alterList = new List<int>();

            ShowProgress(LocalizationHelper.Get("S.Editor.AnalyzingDuplicates"), Project.Frames.Count - 1);

            //Gets the list of similar frames.
            for (var i = 0; i < Project.Frames.Count - 2; i++)
            {
                var sim = ImageMethods.CalculateDifference(Project.Frames[i], Project.Frames[i + 1]);

                if (sim >= similarity)
                    removeList.Add(removal == DuplicatesRemovalType.First ? i : i + 1);

                UpdateProgress(i + 1);
            }

            if (removeList.Count == 0)
            {
                //TODO: Nothing being removed. I need to warn the user.
                return Project.Frames.Count;
            }

            var count = 0;
            if (delay != DuplicatesDelayType.DontAdjust)
            {
                ShowProgress(LocalizationHelper.Get("S.Editor.AdjustingDuplicatesDelay"), removeList.Count);

                //Gets the list of frames that will be altered (if the delay will be adjusted).
                var mode = removal == DuplicatesRemovalType.First ? 1 : -1;
                alterList = (from item in removeList where item + mode >= 0 select item + mode).ToList();

                ActionStack.SaveState(ActionStack.EditAction.RemoveAndAlter, Project.Frames, removeList, alterList);

                if (removal == DuplicatesRemovalType.Last)
                {
                    for (var i = alterList.Count - 1; i >= 0; i--)
                    {
                        var index = alterList[i];

                        //Sum or average of the delays.
                        if (delay == DuplicatesDelayType.Sum)
                            Project.Frames[index].Delay += Project.Frames[index - mode].Delay;
                        else
                            Project.Frames[index].Delay = (Project.Frames[index - mode].Delay + Project.Frames[index].Delay) / 2;

                        UpdateProgress(count++);
                    }
                }
                else
                {
                    foreach (var index in alterList)
                    {
                        //Sum or average of the delays.
                        if (delay == DuplicatesDelayType.Sum)
                            Project.Frames[index].Delay += Project.Frames[index - mode].Delay;
                        else
                            Project.Frames[index].Delay = (Project.Frames[index - mode].Delay + Project.Frames[index].Delay) / 2;

                        UpdateProgress(count++);
                    }
                }
            }
            else
            {
                ActionStack.SaveState(ActionStack.EditAction.Remove, Project.Frames, removeList);
            }

            ShowProgress(LocalizationHelper.Get("S.Editor.DiscardingDuplicates"), removeList.Count);

            for (var i = removeList.Count - 1; i >= 0; i--)
            {
                var removeIndex = removeList[i];

                File.Delete(Project.Frames[removeIndex].Path);
                Project.Frames.RemoveAt(removeIndex);

                UpdateProgress(count++);
            }

            //Gets the minimum index being altered.
            return alterList.Count == 0 && removeList.Count == 0 ? Project.Frames.Count : alterList.Count > 0 ? Math.Min(removeList.Min(), alterList.Min()) : removeList.Min();
        }

        private void RemoveDuplicatesCallback(IAsyncResult ar)
        {
            var index = _removeDuplicatesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (var i = FrameListView.Items.Count - 1; i >= Project.Frames.Count; i--)
                    FrameListView.Items.RemoveAt(i);

                SelectNear(LastSelected);
                Project.Persist();

                LoadSelectedStarter(index, Project.Frames.Count - 1);

                ShowHint("S.Hint.Duplicates");
            }));
        }

        #endregion

        #region Async Delay

        private delegate void DelayFrames(DelayModel model, bool forAll = false, bool ignoreUi = false);

        private DelayFrames _delayFramesDel;

        private void DelayAsync(DelayModel model, bool forAll = false, bool ignoreUi = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();

            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ChangingDelay"), frameList.Count);

            var count = 0;
            foreach (var frameInfo in frameList)
            {
                switch (model.Type)
                {
                    case DelayUpdateType.Override:
                    {
                        frameInfo.Delay = model.NewDelay;
                        break;
                    }
                    case DelayUpdateType.IncreaseDecrease:
                    {
                        frameInfo.Delay += model.IncreaseDecreaseDelay;

                        if (frameInfo.Delay < 10)
                            frameInfo.Delay = 10;
                        break;
                    }
                    default:
                    {
                        frameInfo.Delay = (int)Math.Round(frameInfo.Delay * model.Percent / 100m, 0);

                        if (frameInfo.Delay < 10)
                            frameInfo.Delay = 10;
                        break;
                    }
                }

                #region Update UI

                if (!ignoreUi)
                {
                    var index = Project.Frames.IndexOf(frameInfo);
                    Dispatcher.Invoke(() => ((FrameListBoxItem)FrameListView.Items[index]).Delay = frameInfo.Delay);
                }

                #endregion

                UpdateProgress(count++);
            }

            Project.Persist();
        }

        private void DelayCallback(IAsyncResult ar)
        {
            _delayFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Cursor = Cursors.Arrow;

                UpdateStatistics();
                HideProgress();
                IsLoading = false;

                ShowHint("S.Hint.Delay");

                CommandManager.InvalidateRequerySuggested();
                SetFocusOnCurrentFrame();
            }));
        }

        #endregion

        #region Async Transitions

        private delegate int Transition(int selected, int frameCount, object optional);

        private Transition _transitionDel;

        private int Fade(int selected, int frameCount, object optional)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingTransition"), Project.Frames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            //Calculate opacity increment. When fading to a color, it will add a frame with a 100% opacity at the end. 
            var increment = 1F / (frameCount + (UserSettings.All.FadeToType == FadeToType.NextFrame ? 1 : 0));
            var previousName = Path.GetFileNameWithoutExtension(Project.Frames[selected].Path);
            var previousFolder = Path.GetDirectoryName(Project.Frames[selected].Path);

            #region Images

            //var size = Dispatcher.Invoke(() => FrameSize);
            var dpi = Dispatcher.Invoke(this.Dpi);

            var previousImage = Project.Frames[selected].Path.SourceFrom();
            var nextImage = UserSettings.All.FadeToType == FadeToType.NextFrame ? Project.Frames[Project.Frames.Count - 1 == selected ? 0 : selected + 1].Path.SourceFrom() :
                ImageMethods.CreateEmtpyBitmapSource(UserSettings.All.FadeToColor, previousImage.PixelWidth, previousImage.PixelHeight, dpi, PixelFormats.Indexed1);

            var nextBrush = new ImageBrush
            {
                ImageSource = nextImage,
                Stretch = Stretch.Uniform,
                TileMode = TileMode.None,
                Opacity = increment,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            #endregion

            #region Creates and Save each Transition Frame

            for (var index = 0; index < frameCount; index++)
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(previousImage, new Rect(0, 0, previousImage.Width, previousImage.Height));
                    drawingContext.DrawRectangle(nextBrush, null, new Rect(0, 0, nextImage.Width, nextImage.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, previousImage.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Increase the opacity for the next frame.
                nextBrush.Opacity += increment;

                //TODO: Fix filenaming.
                //TODO: This transition doesn't preserve any other frame info, such as keys pressed and mouse clicks.
                var fileName = Path.Combine(previousFolder, $"{previousName} T {index} {DateTime.Now:hh-mm-ss fff}.png");
                Project.Frames.Insert(selected + index + 1, new FrameInfo(fileName, UserSettings.All.FadeTransitionDelay));

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(fileName))
                    encoder.Save(stream);

                UpdateProgress(index);
            }

            #endregion

            return selected;
        }

        private int Slide(int selected, int frameCount, object optional)
        {
            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingTransition"), Project.Frames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            var previousName = Path.GetFileNameWithoutExtension(Project.Frames[selected].Path);
            var previousFolder = Path.GetDirectoryName(Project.Frames[selected].Path);

            #region Images

            var previousImage = Project.Frames[selected].Path.SourceFrom();
            var nextImage = Project.Frames[(Project.Frames.Count - 1) == selected ? 0 : selected + 1].Path.SourceFrom();

            var nextBrush = new ImageBrush
            {
                ImageSource = nextImage,
                Stretch = Stretch.Uniform,
                TileMode = TileMode.None,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };

            #endregion

            //Calculate Translate Transform increment.
            var increment = previousImage.Width / (frameCount + 1);
            var transf = increment;

            //Calculate the Opacity increment.
            var alphaIncrement = 1F / (frameCount + 1);
            nextBrush.Opacity = alphaIncrement;

            #region Creates and Save each Transition Frame

            for (var index = 0; index < frameCount; index++)
            {
                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(previousImage, new Rect(0, 0, previousImage.Width, previousImage.Height));
                    drawingContext.DrawRectangle(nextBrush, null, new Rect(previousImage.Width - transf, 0, nextImage.Width, nextImage.Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, previousImage.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Increase the translation and opacity for the next frame.
                transf += increment;
                nextBrush.Opacity += alphaIncrement;

                //TODO: Fix filenaming.
                //TODO: This transition doesn't preserve any other frame info, such as keys pressed and mouse clicks.
                var fileName = Path.Combine(previousFolder, $"{previousName} T {index} {DateTime.Now:hh-mm-ss fff}.png");
                Project.Frames.Insert(selected + index + 1, new FrameInfo(fileName, UserSettings.All.FadeTransitionDelay));

                // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(fileName))
                    encoder.Save(stream);

                UpdateProgress(index);
            }

            #endregion

            return selected;
        }

        private void TransitionCallback(IAsyncResult ar)
        {
            var selected = _transitionDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(selected, Project.Frames.Count - 1);

                ShowHint("S.Hint.Transition");
            }));
        }

        #endregion

        #region Async Obfuscate

        private delegate List<int> ObfuscateFrames(Rect rect, double screenScale, bool forAll = false);

        private ObfuscateFrames _obfuscateFramesDel;

        private List<int> ObfuscateAsync(Rect rect, double screenScale, bool forAll = false)
        {
            var frameList = forAll ? Project.Frames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? Project.Frames.Select(x => Project.Frames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), frameList.Count);

            var size = frameList[0].Path.ScaledSize();

            rect = rect.Scale(screenScale).Limit(size.Width, size.Height);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.Path.SourceFrom();
                BitmapSource render;

                switch (UserSettings.All.ObfuscationMode)
                {
                    case ObfuscationMode.Blur:
                    {
                        render = ImageMethods.Blur(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
                            UserSettings.All.BlurLevel, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                    case ObfuscationMode.Darken:
                    {
                        render = ImageMethods.Lightness(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, true, 
                            UserSettings.All.DarkenLevel, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                    case ObfuscationMode.Lighten:
                    {
                        render = ImageMethods.Lightness(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, false, 
                            UserSettings.All.LightenLevel, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                    default:
                    {
                        render = ImageMethods.Pixelate(image, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
                            UserSettings.All.PixelSize, UserSettings.All.ObfuscationSmoothnessOpacity, UserSettings.All.ObfuscationSmoothnessRadius, UserSettings.All.UseMedian, UserSettings.All.ObfuscationInvertedSelection);
                        break;
                    }
                }

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(render));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private void ObfuscateCallback(IAsyncResult ar)
        {
            var selected = _obfuscateFramesDel.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ShowHint("S.Hint.Overlay");

                LoadSelectedStarter(selected.Min(), selected.Max());
            }));
        }

        #endregion

        #region Async Mouse Clicks

        private delegate void MouseClicksDelegate(MouseClicksModel model);

        private MouseClicksDelegate _mouseClicksDelegate;

        private void MouseClicksAsync(MouseClicksModel model)
        {
            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
            });

            ShowProgress(LocalizationHelper.Get("S.Editor.ApplyingOverlay"), Project.Frames.Count);

            var auxList = Project.Frames.CopyList();

            var count = 0;
            foreach (var frame in auxList)
            {
                if (!frame.WasClicked || frame.CursorX == int.MinValue)
                {
                    UpdateProgress(count++);
                    continue;
                }

                var image = frame.Path.SourceFrom();
                var scale = Math.Round(image.DpiX / 96d, 2);

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height)); // - UserSettings.All.MouseClicksWidth/2d   // - UserSettings.All.MouseClicksHeight/2d
                    drawingContext.DrawEllipse(new SolidColorBrush(model.ForegroundColor), null, new Point(frame.CursorX / scale, frame.CursorY / scale), model.Width, model.Height);
                }

                //KeyStrokesOverlayGrid.GetScaledRender(ZoomBoxControl.ScaleDiff, ZoomBoxControl.ImageDpi, ZoomBoxControl.GetImageSize());

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, image.DpiX, image.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.Path))
                    encoder.Save(stream);

                GC.WaitForPendingFinalizers();
                GC.Collect(1);

                UpdateProgress(count++);
            }
        }

        private void MouseClicksCallback(IAsyncResult ar)
        {
            _mouseClicksDelegate.EndInvoke(ar);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LoadSelectedStarter(0, Project.Frames.Count - 1);
            }));
        }

        #endregion

        #endregion
    }
}