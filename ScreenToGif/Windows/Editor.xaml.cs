using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
using ScreenToGif.FileWriters;
using ScreenToGif.ImageUtil;
using ScreenToGif.Util;
using ScreenToGif.Windows.Other;
using ScreenToGif.ImageUtil.Decoder;
using ScreenToGif.Util.Parameters;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Windows.Size;

namespace ScreenToGif.Windows
{
    public partial class Editor : Window
    {
        #region Properties

        public static readonly DependencyProperty FilledListProperty = DependencyProperty.Register("FilledList", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty NotPreviewingProperty = DependencyProperty.Register("NotPreviewing", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register("IsLoading", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty TotalDurationProperty = DependencyProperty.Register("TotalDuration", typeof(TimeSpan), typeof(Editor));
        public static readonly DependencyProperty FrameSizeProperty = DependencyProperty.Register("FrameSize", typeof(System.Windows.Size), typeof(Editor));
        public static readonly DependencyProperty FrameScaleProperty = DependencyProperty.Register("FrameScale", typeof(int), typeof(Editor));
        public static readonly DependencyProperty AverageDelayProperty = DependencyProperty.Register("AverageDelay", typeof(double), typeof(Editor));

        /// <summary>
        /// True if there is a value inside the list of frames.
        /// </summary>
        public bool FilledList
        {
            get { return (bool)GetValue(FilledListProperty); }
            set { SetValue(FilledListProperty, value); }
        }

        /// <summary>
        /// True if not in preview mode.
        /// </summary>
        public bool NotPreviewing
        {
            get { return (bool)GetValue(NotPreviewingProperty); }
            set { SetValue(NotPreviewingProperty, value); }
        }

        /// <summary>
        /// True if loading frames.
        /// </summary>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        /// <summary>
        /// The total duration of the animation. Used by the statistisc tab.
        /// </summary>
        private TimeSpan TotalDuration
        {
            get { return (TimeSpan)GetValue(TotalDurationProperty); }
            set { SetValue(TotalDurationProperty, value); }
        }

        /// <summary>
        /// The size of the frames. Used by the statistisc tab.
        /// </summary>
        private Size FrameSize
        {
            get { return (System.Windows.Size)GetValue(FrameSizeProperty); }
            set { SetValue(FrameSizeProperty, value); }
        }

        /// <summary>
        /// The scale of the frames in %. Used by the statistisc tab.
        /// </summary>
        private int FrameScale
        {
            get { return (int)GetValue(FrameScaleProperty); }
            set { SetValue(FrameScaleProperty, value); }
        }

        /// <summary>
        /// The average delay of the animation. Used by the statistisc tab.
        /// </summary>
        private double AverageDelay
        {
            get { return (double)GetValue(AverageDelayProperty); }
            set { SetValue(AverageDelayProperty, value); }
        }

        #endregion

        #region Variables

        /// <summary>
        /// The List of Frames.
        /// </summary>
        public List<FrameInfo> ListFrames { get; set; }

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

        private readonly System.Windows.Forms.Timer _timerPreview = new System.Windows.Forms.Timer();

        private Action<object, RoutedEventArgs> _applyAction = null;

        #endregion

        public Editor()
        {
            InitializeComponent();
        }

        #region Main Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.PowerModeChanged += System_PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += System_DisplaySettingsChanged;
            SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

            ScrollSynchronizer.SetScrollGroup(ZoomBoxControl.GetScrollViewer(), "Canvas");
            ScrollSynchronizer.SetScrollGroup(MainScrollViewer, "Canvas");

            #region Window Positioning

            if (Math.Abs(UserSettings.All.EditorLeft - -1) < 0.5)
                UserSettings.All.EditorLeft = (SystemParameters.WorkArea.Width - SystemParameters.WorkArea.Left - Width) / 2;
            if (Math.Abs(UserSettings.All.EditorTop - -1) < 0.5)
                UserSettings.All.EditorTop = (SystemParameters.WorkArea.Height - SystemParameters.WorkArea.Top - Height) / 2;

            if (UserSettings.All.EditorLeft > SystemParameters.WorkArea.Width)
                UserSettings.All.EditorLeft = SystemParameters.WorkArea.Width - 100;
            if (UserSettings.All.EditorTop > SystemParameters.WorkArea.Height)
                UserSettings.All.EditorTop = SystemParameters.WorkArea.Height - 100;

            #endregion

            #region Load

            if (ListFrames != null)
            {
                ShowProgress(FindResource("Editor.Preparing").ToString(), ListFrames.Count, true);

                Cursor = Cursors.AppStarting;
                IsLoading = true;

                ActionStack.Prepare(ListFrames[0].ImageLocation);

                _loadFramesDel = Load;
                _loadFramesDel.BeginInvoke(LoadCallback, null);
                return;
            }

            #endregion

            #region Open With...

            if (Argument.FileNames.Any())
            {
                #region Validation

                var extensionList = Argument.FileNames.Select(Path.GetExtension).ToList();

                var media = new[] { "jpg", "gif", "bmp", "png", "avi", "mp4", "wmv" };

                var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
                var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

                //TODO: Later I need to implement another validation for multiple video files.

                if (projectCount != 0 && mediaCount != 0)
                {
                    Dispatcher.Invoke(() => EditorStatusBand.Warning(FindResource("Editor.InvalidLoadingFiles").ToString()));
                    return;
                }

                if (projectCount > 0)
                {
                    Dispatcher.Invoke(() => EditorStatusBand.Warning(FindResource("Editor.InvalidLoadingProjects").ToString()));
                    return;
                }

                #endregion

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(Argument.FileNames, CreateTempPath(), ImportFromCallback, null);
                return;
            }

            #endregion

            RibbonTabControl.SelectedIndex = 0;
            WelcomeTextBlock.Text = Humanizer.Welcome();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            //TODO: Check with High dpi.
            if (UserSettings.All.EditorExtendChrome)
                Glass.ExtendGlassFrame(this, new Thickness(0, 100, 0, 0)); //26
            else
                Glass.RetractGlassFrame(this);

            RibbonTabControl.UpdateVisual();

            //Returns the preview if was playing before the deactivation of the window.
            if (WasPreviewing)
            {
                WasPreviewing = false;
                PlayPause();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            RibbonTabControl.UpdateVisual(false);

            //Pauses the recording preview.
            if (_timerPreview.Enabled)
            {
                WasPreviewing = true;
                Pause();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //What if there's any processing happening?

            Pause();

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


        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Pause();
                GC.Collect();
            }
        }

        private void System_DisplaySettingsChanged(object sender, EventArgs e)
        {
            //TODO: If a monitor is removed, or resolution changes, update the position of the window.
        }

        private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //If the window color changes, update the tabs style.
            if (e.PropertyName == "WindowGlassColor")
            {
                RibbonTabControl.UpdateVisual(IsActive);
            }
        }

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region If nothing selected

            if (FrameListView.SelectedIndex == -1)
            {
                ZoomBoxControl.ImageSource = null;
                return;
            }

            #endregion

            if (LastSelected == -1 || _timerPreview.Enabled || WasChangingSelection || LastSelected >= FrameListView.Items.Count || (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0))
                LastSelected = FrameListView.SelectedIndex;

            FrameListBoxItem current;

            if (_timerPreview.Enabled || WasChangingSelection)
            {
                current = FrameListView.Items[FrameListView.SelectedIndex] as FrameListBoxItem;
            }
            else
            {
                current = FrameListView.Items.OfType<FrameListBoxItem>().FirstOrDefault(x => x.IsFocused || x.IsSelected);
            }

            //If there's no focused item.
            if (current == null)
            {
                if (FrameListView.Items.Count - 1 > LastSelected)
                    FrameListView.SelectedIndex = LastSelected;
                else
                    FrameListView.SelectedIndex = LastSelected = FrameListView.Items.Count - 1;

                current = FrameListView.Items[FrameListView.SelectedIndex] as FrameListBoxItem;
            }

            if (current != null)
            {
                if (!current.IsFocused && !_timerPreview.Enabled)// && !WasChangingSelection)
                    current.Focus();

                var currentIndex = FrameListView.Items.IndexOf(current);

                ZoomBoxControl.ImageSource = ListFrames[currentIndex].ImageLocation;
                FrameListView.ScrollIntoView(current);
            }

            WasChangingSelection = false;
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as FrameListBoxItem;

            if (item != null)// && !WasChangingSelection)
            {
                LastSelected = item.FrameNumber;
                Keyboard.Focus(item);
            }

            //GC.Collect(1);
        }

        #endregion


        #region File Tab

        #region New/Open

        private void NewRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
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

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            if (result.HasValue && !result.Value && recorder.ExitArg == ExitAction.Recorded && recorder.ListFrames != null)
            {
                ActionStack.Clear();
                ActionStack.Prepare(recorder.ListFrames[0].ImageLocation);

                LoadNewFrames(recorder.ListFrames);

                ShowHint("Hint.NewRecording");
            }

            Encoder.Restore();
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
        }

        private void NewWebcamRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            if (result.HasValue && !result.Value && webcam.ExitArg == ExitAction.Recorded && webcam.ListFrames != null)
            {
                ActionStack.Clear();
                ActionStack.Prepare(webcam.ListFrames[0].ImageLocation);

                LoadNewFrames(webcam.ListFrames);

                ShowHint("Hint.NewWebcamRecording");
            }
        }

        private void NewBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var board = new Board();
            var result = board.ShowDialog();

            if (result.HasValue && !result.Value && board.ExitArg == ExitAction.Recorded && board.ListFrames != null)
            {
                ActionStack.Clear();
                ActionStack.Prepare(board.ListFrames[0].ImageLocation);

                LoadNewFrames(board.ListFrames);

                ShowHint("Hint.NewBoardRecording");
            }
        }

        private void NewAnimation_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.NewAnimation, StringResource("Editor.File.Blank"), "Vector.File.New", ApplyNewImageButton_Click);
        }

        private void NewAnimationBackgroundColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.NewAnimationColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.NewAnimationColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyNewImageButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            #region Temporary folder

            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
            {
                UserSettings.All.TemporaryFolder = Path.GetTempPath();
            }

            var pathTemp = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")) + "\\";

            #endregion

            var fileName = Path.Combine(pathTemp, "0.png");

            ActionStack.Clear();
            ActionStack.Prepare(pathTemp);

            #region Create and Save Image

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var scale = this.Scale();

                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(UserSettings.All.NewAnimationColor, UserSettings.All.NewAnimationWidth, UserSettings.All.NewAnimationHeight, this.Dpi(), PixelFormats.Indexed1);
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

            #region Adds to the List

            var frame = new FrameInfo(fileName, 66);

            LoadNewFrames(new List<FrameInfo> { frame });

            #endregion

            ShowHint("Hint.NewAnimation");
        }

        private void NewFromMediaProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                CheckFileExists = true,
                Title = FindResource("Editor.OpenMediaProject").ToString(),
                Filter = "All supported files (*.bmp, *.jpg, *.png, *.gif, *.mp4, *.wmv, *.avi, *.stg, *.zip)|*.bmp;*.jpg;*.png;*.gif;*.mp4;*.wmv;*.avi;*.stg;*.zip|" +
                         "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi|" +
                         "ScreenToGif Project (*.stg, *.zip) |*.stg;*.zip",
            };

            var result = ofd.ShowDialog();

            #region Validation

            var extensionList = ofd.FileNames.Select(Path.GetExtension).ToList();

            var media = new[] { "jpg", "gif", "bmp", "png", "avi", "mp4", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => EditorStatusBand.Warning(FindResource("Editor.InvalidLoadingFiles").ToString()));
                return;
            }

            #endregion

            if (result.HasValue && result.Value)
            {
                //DiscardProject_Executed(null, null);

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileNames.ToList(), CreateTempPath(), ImportFromCallback, null);
            }
        }

        #endregion

        #region Insert

        private void Insert_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 0 && FrameListView.SelectedIndex != -1 && !IsLoading && !e.Handled;
        }

        private void InsertRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();
            WindowState = WindowState.Minimized;
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();

                Encoder.Restore();
                WindowState = WindowState.Normal;
                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex) { Owner = this };
            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ListFrames = insert.ActualList;
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
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex);
            insert.Owner = this;

            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ListFrames = insert.ActualList;
                LoadSelectedStarter(0);
            }

            #endregion
        }

        private void InsertBoardRecording_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var recorder = new Board();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex);
            insert.Owner = this;

            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ListFrames = insert.ActualList;
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
                Title = FindResource("Editor.OpenMedia").ToString(),
                Filter = "All supported files (*.bmp, *.jpg, *.png, *.gif, *.mp4, *.wmv, *.avi)|*.bmp;*.jpg;*.png;*.gif;*.mp4;*.wmv;*.avi|" +
                         "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi",
            };

            var result = ofd.ShowDialog();

            #region Validation

            var extensionList = ofd.FileNames.Select(Path.GetExtension).ToList();

            var media = new[] { "jpg", "gif", "bmp", "png", "avi", "mp4", "wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals("stg") || x.Equals("zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(x));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dispatcher.Invoke(() => EditorStatusBand.Warning(FindResource("Editor.InvalidLoadingFiles").ToString()));
                return;
            }

            #endregion

            if (result.HasValue && result.Value)
            {
                _importFramesDel = InsertImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileNames.ToList(), CreateTempPath(), InsertImportFromCallback, null);
            }
        }

        #endregion

        #region File

        private void File_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Any() && !IsLoading && !e.Handled;
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            if (!Util.Other.IsFfmpegPresent())
                SystemEncoderRadioButton.IsChecked = true;

            FilenameTextBox_TextChanged(null, null);

            ShowPanel(PanelType.SaveAs, StringResource("Editor.File.Save"), "Vector.Save", SaveAsButton_Click);
        }

        private void SaveType_Checked(object sender, RoutedEventArgs e)
        {
            FilenameTextBox_TextChanged(null, null);
        }

        private void FfmpegEncoderRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            if (Util.Other.IsFfmpegPresent())
            {
                EncoderStatusBand.Hide();
                return;
            }

            EncoderStatusBand.Warning(StringResource("Editor.Warning.Ffmpeg"));
            SystemEncoderRadioButton.IsChecked = true;
        }

        private void NewEncoderRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
                return;

            EncoderStatusBand.Info("Experimental encoder. Only works with images with less than 256 colors.");
        }

        private void TransparentColorButton_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new ColorSelector(UserSettings.All.ChromaKey, false) { Owner = this };
            var result = colorDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.ChromaKey = colorDialog.SelectedColor;
            }
        }

        private void ChooseLocation_Click(object sender, RoutedEventArgs e)
        {
            //It's only a relative path if not null/empty and there's no root folder declared.
            var isRelative = !string.IsNullOrWhiteSpace(UserSettings.All.LatestOutputFolder) && !Path.IsPathRooted(UserSettings.All.LatestOutputFolder);
            var notAlt = !string.IsNullOrWhiteSpace(UserSettings.All.LatestOutputFolder) && UserSettings.All.LatestOutputFolder.Contains(Path.DirectorySeparatorChar);

            var initial = Directory.Exists(UserSettings.All.LatestOutputFolder) ? UserSettings.All.LatestOutputFolder : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); 

            var sfd = new SaveFileDialog
            {
                FileName = UserSettings.All.LatestFilename,
                InitialDirectory = isRelative ? Path.GetFullPath(initial) : initial,
                DefaultExt = GifRadioButton.IsChecked == true ? ".gif" : ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag.ToString(),
                Filter = GifRadioButton.IsChecked == true ? "Gif animation (.gif)|*.gif" : "Avi video (.avi)|*.avi|Mp4 video (.mp4)|*.mp4|WebM video|*.webm|Wmv video|*.wmv",
            };

            var result = sfd.ShowDialog();

            if (!result.HasValue || !result.Value) return;

            UserSettings.All.LatestOutputFolder = Path.GetDirectoryName(sfd.FileName);
            UserSettings.All.LatestFilename = Path.GetFileNameWithoutExtension(sfd.FileName);
            UserSettings.All.OverwriteOnSave = FileExistsGrid.Visibility == Visibility.Visible;

            //Converts to a relative path again.
            if (isRelative && !string.IsNullOrWhiteSpace(UserSettings.All.LatestOutputFolder))
            {
                var selected = new Uri(UserSettings.All.LatestOutputFolder);
                var baseFolder = new Uri(AppDomain.CurrentDomain.BaseDirectory);
                var relativeFolder = Uri.UnescapeDataString(baseFolder.MakeRelativeUri(selected).ToString());

                //This app even returns you the correct slashes/backslashes.
                UserSettings.All.LatestOutputFolder = notAlt ? relativeFolder : relativeFolder.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private void IncreaseNumber_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileNumber(1);
        }

        private void DecreaseNumber_Click(object sender, RoutedEventArgs e)
        {
            ChangeFileNumber(-1);
        }

        private void FilenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            var extension = GifRadioButton.IsChecked == true ? ".gif" : (FfmpegEncoderRadioButton.IsChecked == true
                ? ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag
                : ".avi");

            var exists = File.Exists(Path.Combine(OutputFolderTextBox.Text, OutputFilenameTextBox.Text + extension));

            FileExistsGrid.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FileHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var extension = GifRadioButton.IsChecked == true ? ".gif" : (FfmpegEncoderRadioButton.IsChecked == true
                    ? ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag
                    : ".avi");

                Process.Start(Path.Combine(OutputFolderTextBox.Text, OutputFilenameTextBox.Text + extension));
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open file");
            }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            EditorStatusBand.Hide();

            #region Validation

            if (string.IsNullOrWhiteSpace(UserSettings.All.LatestOutputFolder))
            {
                EditorStatusBand.Warning(StringResource("SaveAs.Warning.Folder"));
                return;
            }

            if (string.IsNullOrWhiteSpace(UserSettings.All.LatestFilename))
            {
                EditorStatusBand.Warning(StringResource("SaveAs.Warning.Filename"));
                return;
            }

            var extension = GifRadioButton.IsChecked == true ? ".gif" : (FfmpegEncoderRadioButton.IsChecked == true
                ? ((ComboBoxItem)VideoTypeComboBox.SelectedItem).Tag
                : ".avi");

            var fileName = Path.Combine(UserSettings.All.LatestOutputFolder, UserSettings.All.LatestFilename + extension);

            if (!UserSettings.All.OverwriteOnSave)
            {
                if (File.Exists(fileName))
                {
                    FileExistsGrid.Visibility = Visibility.Visible;
                    EditorStatusBand.Warning(StringResource("SaveAs.Warning.Overwrite"));
                    return;
                }
            }

            #endregion

            #region Parameters

            Parameters param;

            if (GifRadioButton.IsChecked.HasValue && GifRadioButton.IsChecked.Value)
            {
                param = new GifParameters
                {
                    Type = Export.Gif,
                    EncoderType = NewEncoderRadioButton.IsChecked == true ? GifEncoderType.ScreenToGif :
                        LegacyEncoderRadioButton.IsChecked == true ? GifEncoderType.Legacy : GifEncoderType.PaintNet,

                    DetectUnchangedPixels = UserSettings.All.DetectUnchanged,
                    DummyColor = UserSettings.All.DetectUnchanged && UserSettings.All.PaintTransparent ? UserSettings.All.ChromaKey : new Color?(),

                    Quality = UserSettings.All.Quality,

                    UseGlobalColorTable = false,
                    MaximumNumberColors = UserSettings.All.MaximumColors,
                    ColorQuantizationType = ColorQuantizationType.Ordered,

                    RepeatCount = UserSettings.All.Looped ? (UserSettings.All.RepeatForever ? 0 : UserSettings.All.RepeatCount) : -1,
                    Filename = fileName
                };
            }
            else
            {
                //framerate = -vf ""zoompan = d = 25 + '50*eq(in,3)' + '100*eq(in,5)'""
                var command = "-i \"{0}\" {1} -r {2} -y \"{3}\"";

                param = new VideoParameters
                {
                    Type = Export.Video,
                    VideoEncoder = FfmpegEncoderRadioButton.IsChecked == true ? VideoEncoderType.Ffmpg : VideoEncoderType.AviStandalone,
                    Quality = (uint)AviQualitySlider.Value,
                    Command = command,
                    ExtraParameters = UserSettings.All.ExtraParameters,
                    Framerate = UserSettings.All.LatestFps,
                    Filename = fileName
                };
            }

            #endregion

            ClosePanel();

            Encoder.AddItem(ListFrames.CopyToEncode(), param, this.Scale());
        }


        private void SaveAsProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Pause();

            var fileName = Util.Other.FileName("stg", ListFrames.Count);

            if (string.IsNullOrEmpty(fileName)) return;

            _saveProjectDel = SaveProject;
            _saveProjectDel.BeginInvoke(fileName, SaveProjectCallback, null);
        }

        private void DiscardProject_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            #region Prepare UI

            ClosePanel();

            FrameListView.SelectedIndex = -1;
            FrameListView.SelectionChanged -= FrameListView_SelectionChanged;

            FrameListView.Items.Clear();
            ClipboardListBox.Items.Clear();
            ZoomBoxControl.Clear();

            #endregion

            if (ListFrames == null || ListFrames.Count == 0) return;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(ListFrames, DiscardCallback, null);
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

            ListFrames = ActionStack.Undo(ListFrames.CopyList());
            LoadNewFrames(ListFrames, false);

            ShowHint("Hint.Undo");
        }

        private void Reset_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            ListFrames = ActionStack.Reset(ListFrames.CopyList());
            LoadNewFrames(ListFrames, false);

            ShowHint("Hint.Reset");
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            ListFrames = ActionStack.Redo(ListFrames.CopyList());
            LoadNewFrames(ListFrames, false);

            ShowHint("Hint.Redo");
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
                Dialog.Ok(FindResource("Editor.Clipboard.InvalidCut.Title").ToString(),
                    FindResource("Editor.Clipboard.InvalidCut.Instruction").ToString(),
                    FindResource("Editor.Clipboard.InvalidCut.Message").ToString(), Dialog.Icons.Info);
                return;
            }

            #endregion

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().OrderBy(x => x.FrameNumber).First().FrameNumber;

            ActionStack.SaveState(ActionStack.EditAction.Remove, ListFrames, SelectedFramesIndex());

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

            FrameListView.SelectedIndex = -1;

            if (!Util.Clipboard.Cut(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to cut selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");

                Undo_Executed(null, null);

                return;
            }

            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => ListFrames.RemoveAt(x.FrameNumber));
            selected.OrderByDescending(x => x.FrameNumber).ToList().ForEach(x => FrameListView.Items.Remove(x));

            AdjustFrameNumbers(index);
            SelectNear(index);

            #region Item

            var imageItem = new ImageListBoxItem
            {
                Author = DateTime.Now.ToString("hh:mm:ss")
            };

            if (selected.Count > 1)
            {
                imageItem.Tag = $"{StringResource("ImportVideo.Frames")} {string.Join(", ", selected.Select(x => x.FrameNumber))}";
                imageItem.Image = FindResource("Vector.ImageStack") as Canvas;
                imageItem.Content = $"{list.Count} Images";
            }
            else
            {
                imageItem.Tag = $"{StringResource("Editor.List.Frame")} {selected[0].FrameNumber}";
                imageItem.Image = FindResource("Vector.Image") as Canvas;
                imageItem.Content = $"{list.Count} Image";
            }

            #endregion

            ClipboardListBox.Items.Add(imageItem);
            ClipboardListBox.SelectedIndex = ClipboardListBox.Items.Count - 1;

            ShowHint("Hint.Cut", selected.Count);

            ShowPanel(PanelType.Clipboard, FindResource("Editor.Home.Clipboard").ToString(), "Vector.Paste");
        }

        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
            var list = selected.Select(item => ListFrames[item.FrameNumber]).ToList();

            if (!Util.Clipboard.Copy(list))
            {
                Dialog.Ok("Clipboard Exception", "Impossible to copy selected frames.",
                    "Something wrong happened, please report this issue (by sending the exception log).");
                return;
            }

            #region Item

            var imageItem = new ImageListBoxItem();
            imageItem.Tag = $"Frames: {string.Join(", ", selected.Select(x => x.FrameNumber))}";
            imageItem.Author = DateTime.Now.ToString("hh:mm:ss");

            if (list.Count > 1)
            {
                imageItem.Image = FindResource("Vector.ImageStack") as Canvas;
                imageItem.Content = $"{list.Count} Images";
            }
            else
            {
                imageItem.Image = FindResource("Vector.Image") as Canvas;
                imageItem.Content = $"{list.Count} Image";
            }

            #endregion

            ClipboardListBox.Items.Add(imageItem);
            ClipboardListBox.SelectedIndex = ClipboardListBox.Items.Count - 1;

            ShowHint("Hint.Copy", selected.Count);

            ShowPanel(PanelType.Clipboard, FindResource("Editor.Home.Clipboard").ToString(), "Vector.Paste");
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView?.SelectedItem != null && Util.Clipboard.Items.Count > 0 &&
                           ClipboardListBox.SelectedItem != null;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var index = FrameListView.SelectedItems.OfType<FrameListBoxItem>().Last().FrameNumber;
            index = PasteBeforeRadioButton.IsChecked.HasValue && PasteBeforeRadioButton.IsChecked.Value
                    ? index
                    : index + 1;

            var clipData = Util.Clipboard.Paste(ListFrames[0].ImageLocation, ClipboardListBox.SelectedIndex, ClipboardListBox.SelectedIndex);

            ActionStack.SaveState(ActionStack.EditAction.Add, index, clipData.Count);

            ListFrames.InsertRange(index, clipData);

            ClosePanel();

            LoadSelectedStarter(index, ListFrames.Count - 1);

            ShowHint("Hint.Paste", clipData.Count);
        }

        private void ShowClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(PanelType.Clipboard, FindResource("Editor.Home.Clipboard").ToString(), "Vector.Paste");
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

                Process.Start(Path.GetDirectoryName(selected[0].ImageLocation));
            }
            catch (Exception ex)
            {
                Dialog.Ok("Browse Folder Error", "Impossible to browse clipboard folder.", ex.Message);
                LogWriter.Log(ex, "Browse Clipboard Folder");
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
            e.CanExecute = ListFrames != null && ListFrames.Count > 0 && !IsLoading && !OverlayGrid.IsVisible && FrameListView.SelectedIndex != -1;
        }

        private void Zoom100_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomBoxControl.Zoom = 1.0;

            ShowHint("Hint.Zoom", 100);
        }

        private void FitImage_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var image = ListFrames.First().ImageLocation.SourceFrom();

            #region Calculate the Zoom

            var zoomHeight = 1D;
            var zoomWidth = 1D;

            if (image.Width > ZoomBoxControl.ActualWidth)
            {
                zoomWidth = ZoomBoxControl.ActualWidth / image.Width;
            }

            if (image.Height > ZoomBoxControl.ActualHeight)
            {
                zoomHeight = ZoomBoxControl.ActualHeight / image.Height;
            }

            #endregion

            #region Apply the zoom

            if (zoomHeight > 0 && zoomHeight < zoomWidth)
                ZoomBoxControl.Zoom = zoomHeight;
            else if (zoomWidth > 0 && zoomWidth < zoomHeight)
                ZoomBoxControl.Zoom = zoomWidth;
            else
                ZoomBoxControl.Zoom = 1;

            #endregion

            ShowHint("Hint.Zoom", Convert.ToInt32(ZoomBoxControl.Zoom * 100));

            GC.Collect(1);
        }

        #endregion

        #region Select

        private void Selection_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !IsLoading && FrameListView != null && FrameListView.HasItems && !IsLoading;
        }

        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectAll();

            ShowHint("Hint.SelectAll");
        }

        private void GoTo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var go = new GoTo(ListFrames.Count - 1) { Owner = this };
            var result = go.ShowDialog();

            if (result.HasValue && result.Value)
            {
                FrameListView.SelectedIndex = go.Selected;

                ShowHint("Hint.SelectSingle", go.Selected);
            }
        }

        private void InverseSelection_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            foreach (ListViewItem item in FrameListView.Items)
            {
                item.IsSelected = !item.IsSelected;
            }

            ShowHint("Hint.SelectInverse");
        }

        private void DeselectAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ClosePanel();

            FrameListView.SelectedIndex = -1;

            ShowHint("Hint.Deselect");
        }

        #endregion

        #endregion

        #region Playback Tab

        private void Playback_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && ListFrames.Count > 1 && !IsLoading && _applyAction == null;
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

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            try
            {
                #region Validation

                if (ListFrames.Count == FrameListView.SelectedItems.Count)
                {
                    if (Dialog.Ask(FindResource("Editor.Remove.Title").ToString(),
                        FindResource("Editor.Remove.Instruction").ToString(),
                        FindResource("Editor.Remove.Message").ToString()))
                    {
                        DiscardProject_Executed(null, null);
                    }

                    return;
                }

                #endregion

                //TODO: Error when resetting after deleting frames quickly. Not sure why.

                var selected = FrameListView.SelectedItems.OfType<FrameListBoxItem>().ToList();
                var selectedOrdered = selected.OrderByDescending(x => x.FrameNumber).ToList();
                var list = selectedOrdered.Select(item => ListFrames[item.FrameNumber]).ToList();

                ActionStack.SaveState(ActionStack.EditAction.Remove, ListFrames, SelectedFramesIndex());

                FrameListView.SelectedItem = null;

                list.ForEach(x => File.Delete(x.ImageLocation));
                selectedOrdered.ForEach(x => ListFrames.RemoveAt(x.FrameNumber));
                selectedOrdered.ForEach(x => FrameListView.Items.Remove(x));

                AdjustFrameNumbers(selectedOrdered.Last().FrameNumber);

                SelectNear(selectedOrdered.Last().FrameNumber);

                UpdateStatistics();
                ShowHint("Hint.DeleteFrames", selected.Count);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error While Trying to Delete Frames");

                ErrorDialog.Ok(FindResource("Editor.Title") as string, "Error while trying to delete frames", ex.Message, ex);
            }
        }

        private void DeletePrevious_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Remove, ListFrames, Util.Other.CreateIndexList(0, FrameListView.SelectedIndex));

            var count = FrameListView.SelectedIndex;

            for (var index = FrameListView.SelectedIndex - 1; index >= 0; index--)
            {
                DeleteFrame(index);
            }

            AdjustFrameNumbers(0);
            SelectNear(0);

            UpdateStatistics();
            ShowHint("Hint.DeleteFrames", count);
        }

        private void DeleteNext_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var countList = FrameListView.Items.Count - 1; //So we have a fixed value.

            ActionStack.SaveState(ActionStack.EditAction.Remove, ListFrames, Util.Other.CreateIndexList2(FrameListView.SelectedIndex + 1, FrameListView.Items.Count - FrameListView.SelectedIndex - 1));

            var count = FrameListView.Items.Count - FrameListView.SelectedIndex - 1;

            for (var i = countList; i > FrameListView.SelectedIndex; i--) //From the end to the middle.
            {
                DeleteFrame(i);
            }

            SelectNear(FrameListView.Items.Count - 1);

            UpdateStatistics();
            ShowHint("Hint.DeleteFrames", count);
        }

        #endregion

        #region Reordering

        private void Reordering_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading &&
            FrameListView.Items.Count > 1;
        }

        private void Reverse_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, ListFrames.CopyList());

            ListFrames.Reverse();

            LoadSelectedStarter(0);

            ShowHint("Hint.Reverse");
        }

        private void Yoyo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Add, ListFrames.Count, ListFrames.Count);

            ListFrames = Util.Other.Yoyo(ListFrames);
            LoadSelectedStarter(0);

            ShowHint("Hint.Yoyo");
        }

        private void MoveLeft_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, ListFrames.CopyList());

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

                var auxItem = ListFrames[index];

                FrameListView.Items.RemoveAt(index);
                ListFrames.RemoveAt(index);

                FrameListView.Items.Insert(newIndex, item);
                ListFrames.Insert(newIndex, auxItem);

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

            ShowHint("Hint.MoveLeft");

            e.Handled = true;
        }

        private void MoveRight_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            ActionStack.SaveState(ActionStack.EditAction.Reorder, ListFrames.CopyList());

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

                var auxItem = ListFrames[index];

                FrameListView.Items.RemoveAt(index);
                ListFrames.RemoveAt(index);

                FrameListView.Items.Insert(newIndex, item);
                ListFrames.Insert(newIndex, auxItem);

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

            ShowHint("Hint.MoveRight");

            e.Handled = true;
        }

        #endregion

        #region Delay (Duration)

        private void OverrideDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.OverrideDelay, StringResource("Editor.Edit.Delay.Override"), "Vector.OverrideDelay", ApplyOverrideDelayButton_Click);
        }

        private void ApplyOverrideDelayButton_Click(object sender, RoutedEventArgs e)
        {
            ActionStack.SaveState(ActionStack.EditAction.Properties, ListFrames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _delayFramesDel = Delay;
            _delayFramesDel.BeginInvoke(DelayChangeType.Override, NewDelayIntegerUpDown.Value, DelayCallback, null);

            ClosePanel();
        }


        private void IncreaseDecreaseDelay_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.IncreaseDecreaseDelay, StringResource("Editor.Edit.Delay.IncreaseDecrease"), "Vector.IncreaseDecreaseDelay", ApplyIncreaseDecreaseDelayButtonClick);
        }

        private void ApplyIncreaseDecreaseDelayButtonClick(object sender, RoutedEventArgs e)
        {
            if (IncreaseDecreaseDelayIntegerUpDown.Value == 0)
            {
                ClosePanel();
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Properties, ListFrames, SelectedFramesIndex());

            Cursor = Cursors.AppStarting;

            _delayFramesDel = Delay;
            _delayFramesDel.BeginInvoke(DelayChangeType.IncreaseDecrease, IncreaseDecreaseDelayIntegerUpDown.Value, DelayCallback, null);

            ClosePanel();
        }

        #endregion

        #endregion

        #region Image Tab

        private void Image_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = FrameListView != null && FrameListView.SelectedItem != null && !IsLoading;
        }

        #region Size and Position

        private void Resize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            WidthResizeNumericUpDown.ValueChanged -= WidthResizeIntegerUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged -= HeightResizeIntegerUpDown_ValueChanged;

            #region Info

            var image = ListFrames[0].ImageLocation.SourceFrom();
            CurrentDpiLabel.Content = DpiNumericUpDown.Value = (int)Math.Round(image.DpiX, MidpointRounding.AwayFromZero);
            CurrentWidthLabel.Content = WidthResizeNumericUpDown.Value = (int)Math.Round(image.Width, MidpointRounding.AwayFromZero);
            CurrentHeightLabel.Content = HeightResizeNumericUpDown.Value = (int)Math.Round(image.Height, MidpointRounding.AwayFromZero);

            #endregion

            #region Resize Attributes

            var gcd = Util.Other.Gcd(image.Height, image.Width);

            _widthRatio = image.Width / gcd;
            _heightRatio = image.Height / gcd;

            #endregion

            WidthResizeNumericUpDown.ValueChanged += WidthResizeIntegerUpDown_ValueChanged;
            HeightResizeNumericUpDown.ValueChanged += HeightResizeIntegerUpDown_ValueChanged;

            ShowPanel(PanelType.Resize, FindResource("Editor.Image.Resize").ToString(), "Vector.Resize", ApplyResizeButton_Click);
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
            var size = ListFrames[0].ImageLocation.NonScaledSize();

            if (Math.Abs(size.Width - WidthResizeNumericUpDown.Value) < 0.1 && Math.Abs(size.Height - HeightResizeNumericUpDown.Value) < 0.1 &&
                (int)Math.Round(ListFrames[0].ImageLocation.DpiOf()) == DpiNumericUpDown.Value)
            {
                EditorStatusBand.Warning(FindResource("Editor.Resize.Warning").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, Util.Other.CreateIndexList2(0, ListFrames.Count));

            Cursor = Cursors.AppStarting;

            _resizeFramesDel = Resize;
            _resizeFramesDel.BeginInvoke(WidthResizeNumericUpDown.Value, HeightResizeNumericUpDown.Value,
                DpiNumericUpDown.Value, ResizeCallback, null);

            ClosePanel();

            ShowHint("Hint.Resize");
        }


        private void Crop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Crop, StringResource("Editor.Image.Crop"), "Vector.Crop", ApplyCropButton_Click);
        }

        private CroppingAdorner _cropAdorner;
        private FrameworkElement _currentElement = null;
        private bool _resizing = false;

        private void AddCropToElement(FrameworkElement fel)
        {
            if (_currentElement != null)
            {
                RemoveCropElements();
            }

            var rcInterior = new Rect(fel.Width * 0.2, fel.Height * 0.2, fel.Width * 0.6, fel.Height * 0.6);

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
            CropSizeLabel.Content = $"{(int)Math.Round(_cropAdorner.ClipRectangle.Width * scale)} × {(int)Math.Round(_cropAdorner.ClipRectangle.Height * scale)}";

            _resizing = false;
        }

        private void RefreshCropImage()
        {
            if (_cropAdorner == null) return;

            var rect = new Int32Rect((int)_cropAdorner.ClipRectangle.X, (int)_cropAdorner.ClipRectangle.Y, (int)_cropAdorner.ClipRectangle.Width, (int)_cropAdorner.ClipRectangle.Height);

            if (rect.HasArea)
                CropImage.Source = ListFrames[LastSelected].ImageLocation.CropFrom(rect);
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

            var rect = new Int32Rect((int)Math.Round(_cropAdorner.ClipRectangle.X, MidpointRounding.AwayFromZero), (int)Math.Round(_cropAdorner.ClipRectangle.Y, MidpointRounding.AwayFromZero),
                (int)Math.Round(_cropAdorner.ClipRectangle.Width), (int)Math.Round(_cropAdorner.ClipRectangle.Height));

            if (!rect.HasArea)
            {
                EditorStatusBand.Warning(FindResource("Editor.Crop.Warning").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, Util.Other.CreateIndexList2(0, ListFrames.Count));

            Cursor = Cursors.AppStarting;

            _cropFramesDel = Crop;
            _cropFramesDel.BeginInvoke(rect, CropCallback, null);

            RemoveCropElements();
            ClosePanel();

            ShowHint("Hint.Crop");
        }


        private void FlipRotate_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FlipRotate, StringResource("Editor.Image.FlipRotate"), "Vector.FlipHorizontal", ApplyFlipRotateButton_Click);
        }

        private void ApplyFlipRotateButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            Cursor = Cursors.AppStarting;

            var type = FlipHorizontalRadioButton.IsChecked.Value
                ? FlipRotateType.FlipHorizontal : FlipVerticalRadioButton.IsChecked.Value
                ? FlipRotateType.FlipVertical : RotateLeftRadioButton.IsChecked.Value ?
                  FlipRotateType.RotateLeft90 : FlipRotateType.RotateRight90;

            //If it's a rotate operation, the entire list of frames will be altered.
            var selectedIndexes = type == FlipRotateType.RotateLeft90 || type == FlipRotateType.RotateRight90
                ? Util.Other.CreateIndexList2(0, ListFrames.Count)
                : SelectedFramesIndex();

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, selectedIndexes);

            _flipRotateFramesDel = FlipRotate;
            _flipRotateFramesDel.BeginInvoke(type, FlipRotateCallback, null);

            ClosePanel();

            ShowHint("Hint.FlipRotate");
        }

        #endregion

        #region Text

        private void Caption_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Caption, StringResource("Editor.Image.Caption"), "Vector.Caption", ApplyCaptionButton_Click);
        }

        private void CaptionFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.CaptionFontColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.CaptionFontColor = colorPicker.SelectedColor;
            }
        }

        private void CaptionOutlineColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.CaptionOutlineColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.CaptionOutlineColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyCaptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CaptionTextBox.Text.Length == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.Caption.WarningNoText").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Caption.WarningSelection").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, SelectedFramesIndex());

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = CaptionOverlayGrid.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void FreeText_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeText, StringResource("Editor.Image.FreeText"), "Vector.FreeText", ApplyFreeTextButton_Click);
        }

        private void FreeTextTextBlock_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (element == null) return;

            element.CaptureMouse();
            e.Handled = true;
        }

        private void FreeTextTextBlock_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (element == null) return;

            element.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void FreeTextTextBlock_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.Captured == null) return;
            if (!Mouse.Captured.Equals(sender)) return;

            var element = sender as FrameworkElement;

            if (element == null) return;

            var newPoint = e.GetPosition(FreeTextOverlayCanvas);

            //Maximum axis -2000 to 2000.
            if (newPoint.X > 2000)
                newPoint.X = 2000;
            if (newPoint.X < -2000)
                newPoint.X = -2000;

            if (newPoint.Y > 2000)
                newPoint.Y = 2000;
            if (newPoint.Y < -2000)
                newPoint.Y = -2000;

            Canvas.SetTop(element, newPoint.Y);
            Canvas.SetLeft(element, newPoint.X);
        }

        private void FreeTextFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.FreeTextFontColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.FreeTextFontColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyFreeTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeTextTextBox.Text.Length == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.Caption.WarningNoText").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.FreeText.WarningSelection").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, SelectedFramesIndex());

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = FreeTextOverlayCanvas.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void TitleFrame_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.TitleFrame, StringResource("Editor.Image.TitleFrame"), "Vector.TitleFrame", ApplyTitleFrameButton_Click);
        }

        private void TitleFrameFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.TitleFrameFontColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.TitleFrameFontColor = colorPicker.SelectedColor;
            }
        }

        private void TitleFrameBackgroundColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.TitleFrameBackgroundColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.TitleFrameBackgroundColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyTitleFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.TitleFrame.WarningSelection").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex, 1);

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = TitleFrameOverlayGrid.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _titleFrameDel = TitleFrame;
            _titleFrameDel.BeginInvoke(render, FrameListView.SelectedIndex, dpi, TitleFrameCallback, null);

            ClosePanel();
        }

        #endregion

        #region Overlay

        private void FreeDrawing_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.FreeDrawing, StringResource("Editor.Image.FreeDrawing"), "Vector.FreeDrawing", ApplyFreeDrawingButton_Click);
        }

        private void FreeDrawingColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.FreeDrawingColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.FreeDrawingColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyFreeDrawingButton_Click(object sender, RoutedEventArgs e)
        {
            if (FreeDrawingInkCanvas.Strokes.Count == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.FreeDrawing.WarningNoDrawing").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.FreeDrawing.WarningSelection").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, SelectedFramesIndex());

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = FreeDrawingInkCanvas.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            FreeDrawingInkCanvas.Strokes.Clear();

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Watermark_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Watermark, StringResource("Editor.Image.Watermark"), "Vector.Watermark", ApplyWatermarkButton_Click);
        }

        private void SelectWatermark_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = StringResource("Editor.Watermark.Select"),
                Filter = "Image (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.WatermarkFilePath = ofd.FileName;
            }
        }

        private void WatermarkImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            #region Arrange

            if (UserSettings.All.WatermarkLeft + WatermarkImage.ActualWidth < 0)
                UserSettings.All.WatermarkLeft = 0; //6 - (int)WatermarkImage.ActualWidth;

            if (UserSettings.All.WatermarkLeft + 10 > CaptionOverlayGrid.Width)
                UserSettings.All.WatermarkLeft = (int)CaptionOverlayGrid.Width - (int)WatermarkImage.ActualWidth;

            if (UserSettings.All.WatermarkTop + WatermarkImage.ActualHeight < 0)
                UserSettings.All.WatermarkTop = 0; //6 - (int)WatermarkImage.ActualHeight;

            if (UserSettings.All.WatermarkTop + 10 > CaptionOverlayGrid.Height)
                UserSettings.All.WatermarkTop = (int)CaptionOverlayGrid.Height - (int)WatermarkImage.ActualHeight;

            #endregion
        }

        private void ApplyWatermarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserSettings.All.WatermarkFilePath) || !File.Exists(UserSettings.All.WatermarkFilePath))
            {
                EditorStatusBand.Warning(FindResource("Editor.Watermark.WarningNoImage").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Watermark.WarningSelection").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, SelectedFramesIndex());

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = WatermarkOverlayCanvas.GetRender(dpi, scaledSize);

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

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Border_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Border, StringResource("Editor.Image.Border"), "Vector.Border", ApplyBorderButton_Click);
        }

        private void BorderColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.BorderColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.BorderColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyBorderButton_Click(object sender, RoutedEventArgs e)
        {
            if (BorderOverlayBorder.BorderThickness == new Thickness(0, 0, 0, 0))
            {
                EditorStatusBand.Warning(FindResource("Editor.Border.WarningThickness").ToString());
                return;
            }

            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Border.WarningSelection").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, SelectedFramesIndex());

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();
            var render = BorderOverlayBorder.GetRender(dpi, scaledSize);

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(render, dpi, false, OverlayCallback, null);

            ClosePanel();
        }


        private void Cinemagraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Cinemagraph, StringResource("Editor.Image.Cinemagraph"), "Vector.Cinemagraph", ApplyCinemagraphButton_Click);
        }

        private void ApplyCinemagraphButton_Click(object sender, RoutedEventArgs e)
        {
            if (CinemagraphInkCanvas.Strokes.Count == 0)
            {
                EditorStatusBand.Warning(FindResource("Editor.Cinemagraph.WarningNoDrawing").ToString());
                return;
            }

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, Util.Other.CreateIndexList2(0, ListFrames.Count));

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();

            #region Get the Strokes and Clip the Image

            var image = ListFrames[0].ImageLocation.SourceFrom();
            var rectangle = new RectangleGeometry(new Rect(new System.Windows.Point(0, 0), new Size(image.PixelWidth, image.PixelHeight)));
            var geometry = Geometry.Empty;

            foreach (var stroke in CinemagraphInkCanvas.Strokes)
            {
                geometry = Geometry.Combine(geometry, stroke.GetGeometry(), GeometryCombineMode.Union, null);
            }

            geometry = Geometry.Combine(geometry, rectangle, GeometryCombineMode.Xor, null);

            var clippedImage = new System.Windows.Controls.Image
            {
                Height = image.Height,
                Width = image.Width,
                Source = image,
                Clip = geometry
            };

            clippedImage.Measure(new Size(image.Width, image.Height)); //scaledSize
            clippedImage.Arrange(new Rect(new Size(image.Width, image.Height))); //scaledSize

            var imageRender = clippedImage.GetRender(dpi, scaledSize);

            #endregion

            Cursor = Cursors.AppStarting;

            _overlayFramesDel = Overlay;
            _overlayFramesDel.BeginInvoke(imageRender, dpi, true, OverlayCallback, null);

            ClosePanel();
        }


        private void Progress_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();

            var size = ListFrames[0].ImageLocation.ScaledSize();
            ProgressHorizontalRectangle.Width = size.Width / 2;
            ProgressVerticalRectangle.Height = size.Height / 2;

            ShowPanel(PanelType.Progress, StringResource("Editor.Image.Progress"), "Vector.Progress", ApplyProgressButton_Click);
        }

        private void ProgressFontColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.ProgressFontColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.ProgressFontColor = colorPicker.SelectedColor;
            }
        }

        private void ProgressPrecisionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void ProgressColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.ProgressColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.ProgressColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyProgressButton_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.ImageAndProperties, ListFrames, Util.Other.CreateIndexList2(0, ListFrames.Count));

            var dpi = ListFrames[0].ImageLocation.DpiOf();
            var scaledSize = ListFrames[0].ImageLocation.ScaledSize();

            var frameList = new List<RenderTargetBitmap>();

            if (UserSettings.All.ProgressType == ProgressType.Bar)
            {
                #region Bar

                //For all frames.
                for (var i = 1; i <= ListFrames.Count; i++)
                {
                    //Se the size of the bar as the percentage of the total size: Current/Total * Available size
                    ProgressHorizontalRectangle.Width = i / (double)ListFrames.Count * ProgressOverlayGrid.RenderSize.Width;
                    ProgressVerticalRectangle.Height = i / (double)ListFrames.Count * ProgressOverlayGrid.RenderSize.Height;

                    //Assures that the UIElement is up to the changes.
                    ProgressHorizontalRectangle.Arrange(new Rect(ProgressOverlayGrid.RenderSize));
                    ProgressVerticalRectangle.Arrange(new Rect(ProgressOverlayGrid.RenderSize));

                    //Renders the current Visual.
                    frameList.Add(ProgressOverlayGrid.GetRender(dpi, scaledSize));
                }

                #endregion
            }
            else
            {
                #region Text

                var total = ListFrames.Sum(y => y.Delay);

                //For all frames.
                for (var i = 1; i <= ListFrames.Count; i++)
                {
                    //Calculates the cumulative total miliseconds.
                    var cumulative = 0L;

                    for (var j = 0; j < i; j++)
                        cumulative += ListFrames[j].Delay;

                    //Type of the representation.
                    ChangeProgressText(cumulative, total, i);

                    //Assures that the UIElement is up to the changes.
                    ProgressHorizontalTextBlock.Arrange(new Rect(ProgressOverlayGrid.RenderSize));

                    //Renders the current Visual.
                    frameList.Add(ProgressOverlayGrid.GetRender(dpi, scaledSize));
                }

                #endregion
            }

            _overlayMultipleFramesDel = OverlayMultiple;
            _overlayMultipleFramesDel.BeginInvoke(frameList, dpi, true, OverlayMultipleCallback, null);

            ClosePanel();
        }

        #endregion

        #endregion

        #region Transitions Tab

        private void Transition_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ListFrames != null && FrameListView?.SelectedItems != null && !IsLoading && FrameListView.SelectedIndex != -1;
        }

        private void Fade_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Pause();
            ShowPanel(PanelType.Fade, StringResource("Editor.Fade.Title"), "Vector.Fade", ApplyFadeButtonButton_Click);
        }

        private void FadeToColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var colorPicker = new ColorSelector(UserSettings.All.FadeToColor) { Owner = this };
            var result = colorPicker.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UserSettings.All.FadeToColor = colorPicker.SelectedColor;
            }
        }

        private void ApplyFadeButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Fade.WarningSelection").ToString());
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
            ShowPanel(PanelType.Slide, StringResource("Editor.Slide.Title"), "Vector.Slide", ApplySlideButtonButton_Click);
        }

        private void ApplySlideButtonButton_Click(object sender, RoutedEventArgs e)
        {
            if (FrameListView.SelectedIndex == -1)
            {
                EditorStatusBand.Warning(FindResource("Editor.Slide.WarningSelection").ToString());
                return;
            }

            Cursor = Cursors.AppStarting;

            ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex + 1, (int)FadeSlider.Value);

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

            e.CanExecute = _applyAction != null;// && ActionGrid.Width > 50 && ActionLowerGrid.IsVisible;
        }

        private void Ok_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _applyAction?.Invoke(sender, e);

            //If the StatusBand started displaying the message, it means that the action failed.
            if (!EditorStatusBand.Starting)
                _applyAction = null;
        }

        private void Cancel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _applyAction = null;

            ClosePanel();
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
                Process.Start(ListFrames[FrameListView.SelectedIndex].ImageLocation);
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
                Process.Start("explorer.exe", $"/select,\"{ListFrames[FrameListView.SelectedIndex].ImageLocation}\"");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Open Image Folder");
                Dialog.Ok("Open Image", "Impossible to open the image folder.", ex.Message);
            }
        }

        private void ExportImages_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //TODO: Integrate with a panel and give options of how to export.

            if (FrameListView.SelectedItems.Count == 1)
            {
                try
                {
                    #region Single frame

                    var sfd = new SaveFileDialog
                    {
                        FileName = $"{StringResource("Frame")} {FrameListView.SelectedIndex}",
                        DefaultExt = ".png",
                        Filter = "Png image (.png)|*.png"
                    };

                    var result = sfd.ShowDialog(this);

                    if (!result.Value)
                        return;

                    var selected = FrameListView.SelectedItem as FrameListBoxItem;

                    if (selected != null)
                        File.Copy(selected.Image, sfd.FileName);

                    return;

                    #endregion
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Exporting a single frames");

                    Dispatcher.Invoke(() => Dialog.Ok("Error While Exporting", "Error while exporting the frame", ex.Message));
                    return;
                }
            }

            try
            {
                #region Multiple

                var sfd2 = new SaveFileDialog
                {
                    FileName = $"{StringResource("Editor.Edit.Frames")} {FrameListView.SelectedItems.Count}",
                    DefaultExt = ".zip",
                    Filter = "Zip files (.zip)|*.zip"
                };

                if (!sfd2.ShowDialog() ?? false)
                    return;

                if (File.Exists(sfd2.FileName))
                    File.Delete(sfd2.FileName);

                var exportDirectory = Path.Combine(Path.GetDirectoryName(ListFrames.First().ImageLocation), "Export");

                if (Directory.Exists(exportDirectory))
                    Directory.Delete(exportDirectory, true);

                var dir = Directory.CreateDirectory(exportDirectory);

                foreach (var frame in FrameListView.SelectedItems.OfType<FrameListBoxItem>())
                {
                    File.Copy(frame.Image, Path.Combine(dir.FullName, Path.GetFileName(frame.Image)), true);
                }

                ZipFile.CreateFromDirectory(dir.FullName, sfd2.FileName);

                Directory.Delete(dir.FullName, true);

                #endregion
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Exporting multiple frames");

                Dispatcher.Invoke(() => Dialog.Ok("Error While Exporting", "Error while exporting the frames", ex.Message));
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

        private void TimerPreview_Tick(object sender, EventArgs e)
        {
            _timerPreview.Tick -= TimerPreview_Tick;

            if (ListFrames.Count - 1 == FrameListView.SelectedIndex)
            {
                FrameListView.SelectedIndex = 0;
            }
            else
            {
                FrameListView.SelectedIndex++;
            }

            if (ListFrames[FrameListView.SelectedIndex].Delay == 0)
                ListFrames[FrameListView.SelectedIndex].Delay = 10;

            //Sets the interval for this frame. If this frame has 500ms, the next frame will take 500ms to show.
            _timerPreview.Interval = ListFrames[FrameListView.SelectedIndex].Delay;
            _timerPreview.Tick += TimerPreview_Tick;

            GC.Collect(2);
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

            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (fileNames == null) return;
            if (fileNames.Length == 0) return;

            #region Validation

            var extensionList = fileNames.Select(Path.GetExtension).ToList();

            var media = new[] { ".jpg", ".gif", ".bmp", ".png", ".avi", ".mp4", ".wmv" };

            var projectCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && (x.Equals(".stg") || x.Equals(".zip")));
            var mediaCount = extensionList.Count(x => !string.IsNullOrEmpty(x) && media.Contains(Path.GetExtension(x)));

            if (projectCount != 0 && mediaCount != 0)
            {
                Dialog.Ok(StringResource("Editor.DragDrop.InvalidFiles.Title"),
                    StringResource("Editor.DragDrop.InvalidFiles.Instruction"),
                    StringResource("Editor.DragDrop.InvalidFiles.Message"), Dialog.Icons.Warning);
                return;
            }

            if (mediaCount == 0 && projectCount == 0)
            {
                Dialog.Ok(StringResource("Editor.DragDrop.InvalidFiles.Title"),
                    StringResource("Editor.DragDrop.InvalidFiles.Instruction"),
                    StringResource("Editor.DragDrop.InvalidFiles.Message"), Dialog.Icons.Warning);
                return;
            }

            //if (projectCount > 0)
            //{
            //    Dialog.Ok(StringResource("Editor.DragDrop.Invalid.Title"),
            //        StringResource("Editor.DragDrop.InvalidProject.Instruction"),
            //        StringResource("Editor.DragDrop.InvalidProject.Message"), Dialog.Icons.Warning);
            //    return;
            //}

            #endregion

            #region Importing options

            //If inserted into new recording or forced into new one.
            if (ListFrames == null || ListFrames.Count == 0 || e.KeyStates == DragDropKeyStates.ControlKey || projectCount > 0)
                _importFramesDel = ImportFrom;
            else
                _importFramesDel = InsertImportFrom;

            #endregion

            _importFramesDel.BeginInvoke(fileNames.ToList(), CreateTempPath(), ImportFromCallback, null);
        }

        #endregion

        #region Private Methods

        #region Load

        #region Async Loading

        private delegate bool LoadFrames();

        private LoadFrames _loadFramesDel;

        /// <summary>
        /// Loads the new frames and clears the old ones.
        /// </summary>
        /// <param name="listFrames">The new list of frames.</param>
        /// <param name="clear">True if should clear the current list of frames.</param>
        private void LoadNewFrames(List<FrameInfo> listFrames, bool clear = true)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;

            FrameListView.Items.Clear();
            ZoomBoxControl.Zoom = 1;

            #region Discard

            if (ListFrames != null && ListFrames.Any() && clear)
            {
                _discardFramesDel = Discard;
                _discardFramesDel.BeginInvoke(ListFrames, DiscardAndLoadCallback, null);

                ListFrames = listFrames;

                return;
            }

            #endregion

            ListFrames = listFrames;

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);
        }

        private bool Load()
        {
            try
            {
                ShowProgress(DispatcherStringResource("Editor.LoadingFrames"), ListFrames.Count);

                var color = System.Drawing.Color.FromArgb(UserSettings.All.ClickColor.A, UserSettings.All.ClickColor.R,
                    UserSettings.All.ClickColor.G, UserSettings.All.ClickColor.B);

                foreach (var frame in ListFrames)
                {
                    #region Mouse Click

                    if (frame.WasClicked && UserSettings.All.DetectMouseClicks)
                    {
                        try
                        {
                            using (var imageTemp = frame.ImageLocation.From())
                            {
                                using (var graph = Graphics.FromImage(imageTemp))
                                {
                                    var rectEllipse = new Rectangle(frame.CursorX - 6, frame.CursorY - 6, 12, 12);

                                    graph.DrawEllipse(new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.FromArgb(120, color)), 12), rectEllipse);
                                    graph.Flush();
                                }

                                imageTemp.Save(frame.ImageLocation);
                            }
                        }
                        catch (Exception) { }

                        frame.WasClicked = false;
                    }

                    #endregion

                    var itemInvoked = Dispatcher.Invoke(() =>
                    {
                        var item = new FrameListBoxItem
                        {
                            FrameNumber = ListFrames.IndexOf(frame),
                            Image = frame.ImageLocation,
                            Delay = frame.Delay
                        };

                        return item;
                    });

                    Dispatcher.InvokeAsync(() =>
                    {
                        FrameListView.Items.Add(itemInvoked);

                        UpdateProgress(itemInvoked.FrameNumber);
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading");
                return false;
            }
        }

        private void LoadCallback(IAsyncResult ar)
        {
            var result = _loadFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;

                if (ListFrames.Count > 0)
                    FilledList = true;

                FrameListView.SelectedIndex = 0;
                FrameListView.Focus();

                //ListBoxSelector.SetIsEnabled(FrameListView, true);
                //new ListBoxSelector(FrameListView);

                HideProgress();
                UpdateStatistics();

                WelcomeGrid.BeginStoryboard(FindResource("HideWelcomeBorderStoryboard") as Storyboard, HandoffBehavior.Compose);

                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        #region Async Selective Loading

        private delegate bool LoadSelectedFrames(int start, int? end);

        private LoadSelectedFrames _loadSelectedFramesDel;

        private void LoadSelectedStarter(int start, int? end = null)
        {
            Cursor = Cursors.AppStarting;
            IsLoading = true;
            ShowProgress(StringResource("Editor.UpdatingFrames"), ListFrames.Count, true);

            _loadSelectedFramesDel = LoadSelected;
            _loadSelectedFramesDel.BeginInvoke(start, end, LoadSelectedCallback, null);
        }

        private bool LoadSelected(int start, int? end)
        {
            end = end ?? ListFrames.Count - 1;
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
                            frame.Delay = ListFrames[index].Delay;
                            frame.Image = null; //To update the image.
                            frame.Image = ListFrames[index].ImageLocation;
                            frame.UpdateLayout();
                            frame.InvalidateVisual();
                        });

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
                                Image = ListFrames[index].ImageLocation,
                                Delay = ListFrames[index].Delay
                            };

                            FrameListView.Items.Add(item);
                        });

                        #endregion
                    }

                    UpdateProgress(index);
                }

                if (ListFrames.Count > 0)
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
            {}

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                HideProgress();

                if (LastSelected != -1)
                {
                    ZoomBoxControl.ImageSource = null;
                    ZoomBoxControl.ImageSource = ListFrames[LastSelected].ImageLocation;

                    FrameListView.ScrollIntoView(FrameListView.Items[LastSelected]);
                }

                FrameListView.Focus();
                UpdateStatistics();

                IsLoading = false;
                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        #region Async Import

        private delegate void ImportFrames(List<string> fileList, string pathTemp);

        private ImportFrames _importFramesDel;

        private List<FrameInfo> InsertInternal(string fileName, string pathTemp)
        {
            List<FrameInfo> listFrames;

            try
            {
                switch (fileName.Split('.').Last())
                {
                    case "stg":
                    case "zip":

                        listFrames = ImportFromProject(fileName, pathTemp);
                        break;

                    case "gif":

                        listFrames = ImportFromGif(fileName, pathTemp);
                        break;

                    case "mp4":
                    case "wmv":
                    case "avi":

                        listFrames = ImportFromVideo(fileName, pathTemp); //TODO: Remake. Show status.
                        break;

                    default:

                        listFrames = ImportFromImage(fileName, pathTemp);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import Error");

                return new List<FrameInfo>();
            }

            return listFrames;
        }

        private void ImportFrom(List<string> fileList, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(DispatcherStringResource("Editor.PreparingImport"), 100);

            //Adds each image to a list.
            var listFrames = new List<FrameInfo>();
            foreach (var file in fileList)
            {
                if (Dispatcher.HasShutdownStarted)
                    return;

                listFrames.AddRange(InsertInternal(file, pathTemp) ?? new List<FrameInfo>());
            }

            if (listFrames.Count == 0)
            {
                if (Dispatcher.HasShutdownStarted)
                    return;

                Dispatcher.Invoke(delegate
                {
                    Cursor = Cursors.Arrow;
                    IsLoading = false;

                    if (ListFrames?.Count > 0)
                        FilledList = false;

                    HideProgress();

                    CommandManager.InvalidateRequerySuggested();
                });

                return;
            }

            ActionStack.Clear();
            ActionStack.Prepare(listFrames[0].ImageLocation);

            Dispatcher.Invoke(() => LoadNewFrames(listFrames));
        }

        private void ImportFromCallback(IAsyncResult ar)
        {
            _importFramesDel.EndInvoke(ar);

            GC.Collect();
        }

        private void InsertImportFrom(List<string> fileList, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;
                IsLoading = true;
            });

            #endregion

            ShowProgress(DispatcherStringResource("Editor.PreparingImport"), 100);

            //Adds each image to a list.
            var listFrames = new List<FrameInfo>();
            foreach (var file in fileList)
            {
                listFrames.AddRange(InsertInternal(file, pathTemp) ?? new List<FrameInfo>());
            }

            Dispatcher.Invoke(() =>
            {
                ActionStack.SaveState(ActionStack.EditAction.Add, FrameListView.SelectedIndex, listFrames.Count);

                #region Insert

                //TODO: Treat multi-sized set of images...
                var insert = new Insert(ListFrames, listFrames, FrameListView.SelectedIndex) { Owner = this };
                var result = insert.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    ListFrames = insert.ActualList;
                    LoadSelectedStarter(FrameListView.SelectedIndex, ListFrames.Count - 1); //Check
                }
                else
                {
                    HideProgress();

                    if (LastSelected != -1)
                    {
                        ZoomBoxControl.ImageSource = null;
                        ZoomBoxControl.ImageSource = ListFrames[LastSelected].ImageLocation;

                        FrameListView.ScrollIntoView(FrameListView.Items[LastSelected]);
                    }
                }
                #endregion
            });
        }

        private void InsertImportFromCallback(IAsyncResult ar)
        {
            _importFramesDel.EndInvoke(ar);

            GC.Collect();

            //TODO: Check if this thing is needed.
            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;
                IsLoading = false;

                FrameListView.Focus();
                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        private List<FrameInfo> ImportFromProject(string sourceFileName, string pathTemp)
        {
            try
            {
                //Extract to the folder.
                ZipFile.ExtractToDirectory(sourceFileName, pathTemp);

                if (!File.Exists(Path.Combine(pathTemp, "List.sb")))
                    throw new FileNotFoundException("Impossible to open project.", "List.sb");

                //Read as text.
                var serial = File.ReadAllText(Path.Combine(pathTemp, "List.sb"));

                //Deserialize to a List.
                var list = Serializer.DeserializeFromString<List<FrameInfo>>(serial);

                //Shows the ProgressBar
                ShowProgress("Importing Frames", list.Count);

                var count = 0;
                foreach (var frame in list)
                {
                    //Change the file path to the current one.
                    frame.ImageLocation = Path.Combine(pathTemp, Path.GetFileName(frame.ImageLocation));

                    count++;
                    UpdateProgress(count);
                }

                return list;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error • Import Project");
                return new List<FrameInfo>();
            }
        }

        private List<FrameInfo> ImportFromGif(string sourceFileName, string pathTemp)
        {
            ShowProgress(DispatcherStringResource("Editor.ImportingFrames"), 50, true);

            GifFile gifMetadata;
            var listFrames = new List<FrameInfo>();

            var decoder = ImageMethods.GetDecoder(sourceFileName, out gifMetadata) as GifBitmapDecoder;

            ShowProgress(DispatcherStringResource("Editor.ImportingFrames"), decoder.Frames.Count);

            if (decoder.Frames.Count > 0)
            {
                var fullSize = ImageMethods.GetFullSize(decoder, gifMetadata);
                var index = 0;

                BitmapSource baseFrame = null;
                foreach (var rawFrame in decoder.Frames)
                {
                    var metadata = ImageMethods.GetFrameMetadata(decoder, gifMetadata, index);

                    var bitmapSource = ImageMethods.MakeFrame(fullSize, rawFrame, metadata, baseFrame);

                    #region Disposal Method

                    switch (metadata.DisposalMethod)
                    {
                        case FrameDisposalMethod.None:
                        case FrameDisposalMethod.DoNotDispose:
                            baseFrame = bitmapSource;
                            break;
                        case FrameDisposalMethod.RestoreBackground:
                            baseFrame = ImageMethods.IsFullFrame(metadata, fullSize) ? null : ImageMethods.ClearArea(bitmapSource, metadata);
                            break;
                        case FrameDisposalMethod.RestorePrevious:
                            //Reuse same base frame.
                            break;
                    }

                    #endregion

                    #region Each Frame

                    var fileName = Path.Combine(pathTemp, $"{index} {DateTime.Now.ToString("hh-mm-ss-FFFF")}.png");

                    using (var stream = new FileStream(fileName, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(stream);
                        stream.Close();
                    }

                    //It should not throw a overflow exception because of the maximum value for the miliseconds.
                    var frame = new FrameInfo(fileName, (int)metadata.Delay.TotalMilliseconds);
                    listFrames.Add(frame);

                    UpdateProgress(index);

                    GC.Collect(1);

                    #endregion

                    index++;
                }
            }

            return listFrames;
        }

        private List<FrameInfo> ImportFromImage(string sourceFileName, string pathTemp)
        {
            var fileName = Path.Combine(pathTemp, $"{0} {DateTime.Now.ToString("hh-mm-ss-FFFF")}.png");

            #region Save the Image to the Recording Folder

            var bitmap = new BitmapImage(new Uri(sourceFileName));

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

        private List<FrameInfo> ImportFromVideo(string fileName, string pathTemp)
        {
            var delay = 66;

            var frameList = Dispatcher.Invoke(() =>
            {
                var videoSource = new VideoSource(fileName) { Owner = this };
                var result = videoSource.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    delay = videoSource.Delay;
                    return videoSource.FrameList;
                }

                return null;
            });

            if (frameList == null)
                return new List<FrameInfo>();

            ShowProgress(DispatcherStringResource("Editor.ImportingFrames"), frameList.Count);

            #region Saves the Frames to the Disk

            var frameInfoList = new List<FrameInfo>();
            var count = 0;

            foreach (var frame in frameList)
            {
                var frameName = Path.Combine(pathTemp, $"{count} {DateTime.Now.ToString("hh-mm-ss-FFFF")}.png");

                using (var stream = new FileStream(frameName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(frame);
                    encoder.Save(stream);
                    stream.Close();
                }

                var frameInfo = new FrameInfo(frameName, delay);
                frameInfoList.Add(frameInfo);

                GC.Collect();
                count++;

                UpdateProgress(count);
            }

            #endregion

            return frameInfoList;
        }

        #endregion

        #region Playback

        private void PlayPause()
        {
            if (_timerPreview.Enabled)
            {
                _timerPreview.Tick -= TimerPreview_Tick;
                _timerPreview.Stop();

                NotPreviewing = true;
                PlayButton.Text = StringResource("Editor.Playback.Play");
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");

                PlayMenuItem.Header = StringResource("Editor.Playback.Play");
                PlayMenuItem.Image = (Canvas)FindResource("Vector.Play");
            }
            else
            {
                NotPreviewing = false;
                PlayButton.Text = StringResource("Editor.Playback.Pause");
                PlayButton.Content = FindResource("Vector.Pause");
                PlayPauseButton.Content = FindResource("Vector.Pause");

                PlayMenuItem.Header = StringResource("Editor.Playback.Pause");
                PlayMenuItem.Image = (Canvas)FindResource("Vector.Pause");

                #region Starts playing the next frame

                if (ListFrames.Count - 1 == FrameListView.SelectedIndex)
                {
                    FrameListView.SelectedIndex = 0;
                }
                else
                {
                    FrameListView.SelectedIndex++;
                }

                #endregion

                if (ListFrames[FrameListView.SelectedIndex].Delay == 0)
                    ListFrames[FrameListView.SelectedIndex].Delay = 10;

                _timerPreview.Interval = ListFrames[FrameListView.SelectedIndex].Delay;
                _timerPreview.Tick += TimerPreview_Tick;
                _timerPreview.Start();
            }
        }

        private void Pause()
        {
            if (_timerPreview.Enabled)
            {
                _timerPreview.Tick -= TimerPreview_Tick;
                _timerPreview.Stop();

                NotPreviewing = true;
                PlayButton.Text = StringResource("Editor.Playback.Play");
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");

                PlayMenuItem.Header = StringResource("Editor.Playback.Play");
                PlayMenuItem.Image = (Canvas)FindResource("Vector.Play");
            }
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

                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                StatusProgressBar.IsIndeterminate = false;
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StatusGrid.Visibility = Visibility.Hidden;
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
                ((FrameListBoxItem)FrameListView.Items[index]).FrameNumber = index;
            }
        }

        private void ShowPanel(PanelType type, string title, string vector, Action<object, RoutedEventArgs> apply = null)
        {
            #region Hide all visible grids

            foreach (var child in ActionInternalGrid.Children.OfType<Grid>().Where(x => x.Visibility == Visibility.Visible))
            {
                child.Visibility = Visibility.Collapsed;
            }

            #endregion

            #region Overlay

            //TODO: Better
            if (ListFrames != null && ListFrames.Count > 0)
            {
                var image = ListFrames[0].ImageLocation.SourceFrom();
                CaptionOverlayGrid.Width = image.Width;
                CaptionOverlayGrid.Height = image.Height;
            }

            #endregion

            #region Commons

            ActionTitleLabel.Content = title;
            ActionViewBox.Child = FindResource(vector) as Canvas;

            Util.Other.RemoveRoutedEventHandlers(ApplyButton, ButtonBase.ClickEvent);

            if (apply != null)
            {
                ApplyButton.Text = StringResource("Action.Apply");
                ApplyButton.Content = FindResource("Vector.Ok") as Canvas;
                _applyAction = apply;

                ActionLowerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ActionLowerGrid.Visibility = Visibility.Collapsed;
            }

            RemoveAdorners();

            #endregion

            #region Type

            switch (type)
            {
                case PanelType.SaveAs:
                    ApplyButton.Text = StringResource("Action.Save");
                    ApplyButton.Content = FindResource("Vector.Save") as Canvas;
                    SaveGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.NewAnimation:
                    NewGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Clipboard:
                    ClipboardGrid.Visibility = Visibility.Visible;
                    break;
                case PanelType.Resize:
                    ResizeGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplyAll");
                    break;
                case PanelType.FlipRotate:
                    FlipRotateGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.FlipRotate2");
                    break;
                case PanelType.Crop:

                    #region Crop

                    CropGrid.Visibility = Visibility.Visible;

                    AddCropToElement(CropAreaGrid);
                    RefreshCropImage();

                    BottomCropNumericUpDown.Scale = TopCropNumericUpDown.Scale = RightCropNumericUpDown.Scale = LeftCropNumericUpDown.Scale = this.Scale();

                    BottomCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height - (CaptionOverlayGrid.Height * .1));
                    TopCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Height * .1);

                    RightCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width - (CaptionOverlayGrid.Width * .1));
                    LeftCropNumericUpDown.Value = (int)(CaptionOverlayGrid.Width * .1);

                    ShowHint("Hint.ApplyAll");

                    #endregion

                    break;
                case PanelType.Caption:
                    CaptionGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");
                    break;
                case PanelType.FreeText:
                    FreeTextGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");
                    break;
                case PanelType.TitleFrame:
                    TitleFrameGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.TitleFrame2");
                    break;
                case PanelType.FreeDrawing:
                    FreeDrawingGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");
                    break;
                case PanelType.Watermark:

                    #region Watermark

                    var adornerLayer = AdornerLayer.GetAdornerLayer(WatermarkImage);

                    adornerLayer.Add(new ResizingAdorner(WatermarkImage));

                    TopWatermarkIntegerUpDown.Scale = LeftWatermarkIntegerUpDown.Scale =  this.Scale();

                    #region Arrange

                    if (UserSettings.All.WatermarkLeft < 0)
                        UserSettings.All.WatermarkLeft = 10;

                    if (UserSettings.All.WatermarkLeft + 10 > CaptionOverlayGrid.Width)
                        UserSettings.All.WatermarkLeft = 10;

                    if (UserSettings.All.WatermarkTop < 0)
                        UserSettings.All.WatermarkTop = 10;

                    if (UserSettings.All.WatermarkTop + 10 > CaptionOverlayGrid.Height)
                        UserSettings.All.WatermarkTop = 10;

                    #endregion

                    WatermarkGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");

                    #endregion

                    break;
                case PanelType.Border:
                    BorderGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");
                    break;
                case PanelType.Progress:
                    ProgressGrid.Visibility = Visibility.Visible;

                    ChangeProgressTextToCurrent();
                    ShowHint("Hint.ApplyAll");
                    break;
                case PanelType.OverrideDelay:
                    OverrideDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");
                    break;
                case PanelType.IncreaseDecreaseDelay:
                    IncreaseDecreaseDelayGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.ApplySelected");
                    break;
                case PanelType.Cinemagraph:
                    CinemagraphGrid.Visibility = Visibility.Visible;
                    ShowHint("Hint.Cinemagraph");
                    break;
                case PanelType.Fade:
                    FadeGrid.Visibility = Visibility.Visible;
                    ShowHint("Transitions.Info");
                    break;
                case PanelType.Slide:
                    SlideGrid.Visibility = Visibility.Visible;
                    ShowHint("Transitions.Info");
                    break;
            }

            #endregion

            #region Focus

            var visible = ActionInternalGrid.Children.OfType<Grid>().FirstOrDefault(x => x.Visibility == Visibility.Visible);

            if (visible != null)
            {
                visible.Focus();
                visible.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }

            #endregion

            #region Animate

            if (type == PanelType.SaveAs && ActionGrid.Width < 280)
            {
                ActionGrid.BeginStoryboard(FindResource("ShowExtendedPanelStoryboard") as Storyboard, HandoffBehavior.Compose);
            }
            else if (type != PanelType.SaveAs && (ActionGrid.Width < 5 || ActionGrid.Width > 240))
            {
                ActionGrid.BeginStoryboard(FindResource("ShowPanelStoryboard") as Storyboard, HandoffBehavior.Compose);
            }

            #endregion

            #region Overlay Grid

            if (OverlayGrid.Opacity < 1 && type < 0)
            {
                OverlayGrid.BeginStoryboard(FindResource("ShowOverlayGridStoryboard") as Storyboard, HandoffBehavior.Compose);
                ZoomBoxControl.Zoom = 1.0;
            }
            else if (OverlayGrid.Opacity > 0 && type > 0)
                OverlayGrid.BeginStoryboard(FindResource("HideOverlayGridStoryboard") as Storyboard, HandoffBehavior.Compose);

            #endregion

            CommandManager.InvalidateRequerySuggested();
        }

        private void ClosePanel()
        {
            EditorStatusBand.Hide();

            RemoveAdorners();
            SetFocusOnCurrentFrame();

            BeginStoryboard(FindStoryboard("HidePanelStoryboard"), HandoffBehavior.Compose);
            BeginStoryboard(FindStoryboard("HideOverlayGridStoryboard"), HandoffBehavior.Compose);
        }

        private List<int> SelectedFramesIndex()
        {
            return FrameListView.SelectedItems.OfType<FrameListBoxItem>().Select(x => FrameListView.Items.IndexOf(x)).OrderBy(y => y).ToList();
        }

        #endregion

        #region Async Project

        private delegate void SaveProjectDelegate(string fileName);

        private SaveProjectDelegate _saveProjectDel;

        private void SaveProject(string fileName)
        {
            ShowProgress(DispatcherStringResource("Editor.ExportingRecording"), ListFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Export as Project

            try
            {
                //Serialize the current list of frames.
                var serial = Serializer.SerializeToString(ListFrames);

                if (serial == null)
                    throw new Exception("Object serialization failed.");

                if (File.Exists(fileName))
                    File.Delete(fileName);

                var exportDirectory = Path.Combine(Path.GetDirectoryName(ListFrames.First().ImageLocation), "Export");

                if (Directory.Exists(exportDirectory))
                    Directory.Delete(exportDirectory, true);

                var dir = Directory.CreateDirectory(exportDirectory);

                //Write the list of frames.
                File.WriteAllText(Path.Combine(dir.FullName, "List.sb"), serial);

                var count = 0;
                foreach (var frameInfo in ListFrames)
                {
                    File.Copy(frameInfo.ImageLocation, Path.Combine(dir.FullName, Path.GetFileName(frameInfo.ImageLocation)), true);
                    UpdateProgress(count++);
                }

                ZipFile.CreateFromDirectory(dir.FullName, fileName);

                Directory.Delete(dir.FullName, true);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Exporting Recording as a Project");

                Dispatcher.Invoke(() => Dialog.Ok("Error While Saving", "Error while Saving as Project", ex.Message));
            }

            #endregion
        }

        private void SaveProjectCallback(IAsyncResult ar)
        {
            _saveProjectDel.EndInvoke(ar);

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

        #region Async Discard

        private delegate void DiscardFrames(List<FrameInfo> removeFrames);

        private DiscardFrames _discardFramesDel;

        private void Discard(List<FrameInfo> removeFrames)
        {
            ShowProgress(DispatcherStringResource("Editor.DiscardingFrames"), removeFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            try
            {
                var count = 0;
                foreach (var frame in removeFrames)
                {
                    File.Delete(frame.ImageLocation);

                    UpdateProgress(count++);
                }

                var path = Path.GetDirectoryName(removeFrames[0].ImageLocation);
                var folderList = Directory.EnumerateDirectories(path).ToList();

                ShowProgress(DispatcherStringResource("Editor.DiscardingFolders"), folderList.Count);

                count = 0;
                foreach (var folder in folderList)
                {
                    if (!folder.Contains("Encode "))
                        Directory.Delete(folder, true);

                    UpdateProgress(count++);
                }

                removeFrames.Clear();
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
            removeFrames.Clear();

            HideProgress();
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                WelcomeGrid.BeginStoryboard(FindResource("ShowWelcomeBorderStoryboard") as Storyboard, HandoffBehavior.Compose);

                FilledList = false;
                IsLoading = false;
                WelcomeTextBlock.Text = Humanizer.Welcome();

                UpdateStatistics();

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;

                CommandManager.InvalidateRequerySuggested();
            });

            GC.Collect();
        }

        private void DiscardAndLoadCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                FilledList = false;

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
            });

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);

            GC.Collect();
        }

        #endregion

        #region Async Resize

        private delegate void ResizeFrames(int width, int height, double dpi);

        private ResizeFrames _resizeFramesDel;

        private void Resize(int width, int height, double dpi)
        {
            ShowProgress(DispatcherStringResource("Editor.ResizingFrames"), ListFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in ListFrames)
            {
                var png = new PngBitmapEncoder();
                png.Frames.Add(ImageMethods.ResizeImage((BitmapImage)frame.ImageLocation.SourceFrom(), width, height, 0, dpi));

                using (Stream stm = File.OpenWrite(frame.ImageLocation))
                {
                    png.Save(stm);
                }

                UpdateProgress(count++);
            }
        }

        private void ResizeCallback(IAsyncResult ar)
        {
            _resizeFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(0, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Crop

        private delegate void CropFrames(Int32Rect rect);

        private CropFrames _cropFramesDel;

        private void Crop(Int32Rect rect)
        {
            ShowProgress(DispatcherStringResource("Editor.CroppingFrames"), ListFrames.Count);

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in ListFrames)
            {
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(frame.ImageLocation.CropFrom(rect)));

                using (Stream stm = File.OpenWrite(frame.ImageLocation))
                {
                    png.Save(stm);
                }

                UpdateProgress(count++);
            }
        }

        private void CropCallback(IAsyncResult ar)
        {
            _cropFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(0, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Merge Frames

        private delegate List<int> OverlayFrames(RenderTargetBitmap render, double dpi, bool forAll = false);

        private OverlayFrames _overlayFramesDel;

        private List<int> Overlay(RenderTargetBitmap render, double dpi, bool forAll = false)
        {
            var frameList = forAll ? ListFrames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? ListFrames.Select(x => ListFrames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(DispatcherStringResource("Editor.ApplyingOverlay"), frameList.Count);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.ImageLocation.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawImage(render, new Rect(0, 0, render.Width, render.Height));
                }

                //Converts the Visual (DrawingVisual) into a BitmapSource.
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder.
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                //Saves the image into a file using the encoder.
                using (Stream stream = File.Create(frame.ImageLocation))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }

            return selectedList;
        }

        private void OverlayCallback(IAsyncResult ar)
        {
            var selected = _overlayFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                ShowHint("Hint.Overlay");

                LoadSelectedStarter(selected.Min(), selected.Max());
            });
        }


        private delegate List<int> OverlayMultipleFrames(List<RenderTargetBitmap> render, double dpi, bool forAll = false);

        private OverlayMultipleFrames _overlayMultipleFramesDel;

        private List<int> OverlayMultiple(List<RenderTargetBitmap> renderList, double dpi, bool forAll = false)
        {
            var frameList = forAll ? ListFrames : SelectedFrames();
            var selectedList = Dispatcher.Invoke(() =>
            {
                IsLoading = true;

                return forAll ? ListFrames.Select(x => ListFrames.IndexOf(x)).ToList() : SelectedFramesIndex();
            });

            ShowProgress(DispatcherStringResource("Editor.ApplyingOverlay"), frameList.Count);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.ImageLocation.SourceFrom();

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawImage(image, new Rect(0, 0, image.Width, image.Height));
                    drawingContext.DrawImage(renderList[count], new Rect(0, 0, renderList[count].Width, renderList[count].Height));
                }

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                // Creates a PngBitmapEncoder and adds the BitmapSource to the frames of the encoder
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));

                // Saves the image into a file using the encoder
                using (Stream stream = File.Create(frame.ImageLocation))
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
                ShowHint("Hint.Overlay");

                LoadSelectedStarter(selected.Min(), selected.Max());
            });
        }

        #endregion

        #region Async Title Frame

        private delegate int TitleFrameAction(RenderTargetBitmap render, int selected, double dpi);

        private TitleFrameAction _titleFrameDel;

        private int TitleFrame(RenderTargetBitmap render, int selected, double dpi)
        {
            ShowProgress(DispatcherStringResource("Editor.CreatingTitleFrame"), 1, true);

            Dispatcher.Invoke(() => IsLoading = true);

            #region Save Image

            var name = Path.GetFileNameWithoutExtension(ListFrames[selected].ImageLocation);
            var folder = Path.GetDirectoryName(ListFrames[selected].ImageLocation);
            var fileName = Path.Combine(folder, $"{name} TF {DateTime.Now.ToString("hh-mm-ss")}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));

            //Saves the image into a file using the encoder.
            using (Stream stream = File.Create(fileName))
                encoder.Save(stream);

            GC.Collect();

            #endregion

            //Adds to the List
            ListFrames.Insert(selected, new FrameInfo(fileName, UserSettings.All.TitleFrameDelay));

            return selected;
        }

        private void TitleFrameCallback(IAsyncResult ar)
        {
            var selected = _titleFrameDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                ShowHint("Hint.TitleFrame");

                LoadSelectedStarter(selected, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Flip/Rotate

        private delegate void FlipRotateFrames(FlipRotateType type);

        private FlipRotateFrames _flipRotateFramesDel;

        private void FlipRotate(FlipRotateType type)
        {
            ShowProgress(DispatcherStringResource("Editor.ApplyingFlipRotate"), ListFrames.Count);

            var frameList = type == FlipRotateType.RotateLeft90 ||
                type == FlipRotateType.RotateRight90 ? ListFrames : SelectedFrames();

            Dispatcher.Invoke(() => IsLoading = true);

            var count = 0;
            foreach (var frame in frameList)
            {
                var image = frame.ImageLocation.SourceFrom();

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
                using (Stream stream = File.Create(frame.ImageLocation))
                    encoder.Save(stream);

                UpdateProgress(count++);
            }
        }

        private void FlipRotateCallback(IAsyncResult ar)
        {
            _flipRotateFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(0, ListFrames.Count - 1);
            });
        }

        #endregion

        #region Async Delay

        private delegate void DelayFrames(DelayChangeType type, int delay);

        private DelayFrames _delayFramesDel;

        private void Delay(DelayChangeType type, int delay)
        {
            var frameList = SelectedFrames();

            Dispatcher.Invoke(() =>
            {
                IsLoading = true;
                Cursor = Cursors.AppStarting;
            });

            ShowProgress(DispatcherStringResource("Editor.ChangingDelay"), frameList.Count);

            var count = 0;
            foreach (var frameInfo in frameList)
            {
                if (type == DelayChangeType.Override)
                {
                    frameInfo.Delay = delay;
                }
                else
                {
                    frameInfo.Delay += delay;

                    if (frameInfo.Delay < 10)
                        frameInfo.Delay = 10;
                }

                #region Update UI

                var index = ListFrames.IndexOf(frameInfo);
                Dispatcher.Invoke(() => ((FrameListBoxItem)FrameListView.Items[index]).Delay = frameInfo.Delay);

                #endregion

                UpdateProgress(count++);
            }
        }

        private void DelayCallback(IAsyncResult ar)
        {
            _delayFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.Arrow;

                HideProgress();
                IsLoading = false;

                ShowHint("Hint.Delay");

                CommandManager.InvalidateRequerySuggested();
            });
        }

        #endregion

        #region Async Transitions

        private delegate int Transition(int selected, int frameCount, object optional);

        private Transition _transitionDel;

        private int Fade(int selected, int frameCount, object optional)
        {
            ShowProgress(DispatcherStringResource("Editor.ApplyingTransition"), ListFrames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            //Calculate opacity increment.
            var increment = 1F / (frameCount + 1);
            var previousName = Path.GetFileNameWithoutExtension(ListFrames[selected].ImageLocation);
            var previousFolder = Path.GetDirectoryName(ListFrames[selected].ImageLocation);

            #region Images

            //var size = Dispatcher.Invoke(() => FrameSize);
            var dpi = Dispatcher.Invoke(this.Dpi);

            //TODO: Check with high dpi. Also with image dpi that is different from the screen
            var previousImage = ListFrames[selected].ImageLocation.SourceFrom();
            var nextImage = UserSettings.All.FadeToType == FadeToType.NextFrame ? ListFrames[ListFrames.Count - 1 == selected ? 0 : selected + 1].ImageLocation.SourceFrom() :
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

                // Converts the Visual (DrawingVisual) into a BitmapSource
                var bmp = new RenderTargetBitmap(previousImage.PixelWidth, previousImage.PixelHeight, previousImage.DpiX, previousImage.DpiY, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                //Increase the opacity for the next frame.
                nextBrush.Opacity += increment;

                //TODO: Fix filenaming.
                var fileName = Path.Combine(previousFolder, $"{previousName} T {index} {DateTime.Now.ToString("hh-mm-ss fff")}.png");
                ListFrames.Insert(selected + index + 1, new FrameInfo(fileName, 66));

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

        private int Slide(int selected, int frameCount, object optional)
        {
            ShowProgress(DispatcherStringResource("Editor.ApplyingTransition"), ListFrames.Count - selected + frameCount);

            Dispatcher.Invoke(() => IsLoading = true);

            var previousName = Path.GetFileNameWithoutExtension(ListFrames[selected].ImageLocation);
            var previousFolder = Path.GetDirectoryName(ListFrames[selected].ImageLocation);

            #region Images

            var previousImage = ListFrames[selected].ImageLocation.SourceFrom();
            var nextImage = ListFrames[(ListFrames.Count - 1) == selected ? 0 : selected + 1].ImageLocation.SourceFrom();

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
                var fileName = Path.Combine(previousFolder, $"{previousName} T {index} {DateTime.Now.ToString("hh-mm-ss")}.png");
                ListFrames.Insert(selected + index + 1, new FrameInfo(fileName, 66));

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

            Dispatcher.Invoke(() =>
            {
                LoadSelectedStarter(selected, ListFrames.Count - 1);

                ShowHint("Hint.Transition");
            });
        }

        #endregion

        #region Other

        private static string CreateTempPath()
        {
            #region Temporary folder

            if (string.IsNullOrWhiteSpace(UserSettings.All.TemporaryFolder))
            {
                UserSettings.All.TemporaryFolder = Path.GetTempPath();
            }

            var pathTemp = Path.Combine(UserSettings.All.TemporaryFolder, "ScreenToGif", "Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            if (!Directory.Exists(pathTemp))
                Directory.CreateDirectory(pathTemp);

            #endregion

            return pathTemp;
        }

        private void DeleteFrame(int index)
        {
            //Delete the File from the disk.
            File.Delete(ListFrames[index].ImageLocation);

            //Remove from the list.
            ListFrames.RemoveAt(index);
            FrameListView.Items.RemoveAt(index);
        }

        private List<FrameInfo> SelectedFrames()
        {
            var selectedIndexList = Dispatcher.Invoke(SelectedFramesIndex);
            return ListFrames.Where(x => selectedIndexList.Contains(ListFrames.IndexOf(x))).ToList();
        }

        private string StringResource(string key)
        {
            return FindResource(key).ToString().Replace("\n", " ").Replace("\\n", " ").Replace("\r", " ").Replace("&#10;", " ").Replace("&#x0d;", " ");
        }

        private string DispatcherStringResource(string key)
        {
            return Dispatcher.Invoke(() => FindResource(key).ToString().Replace("\n", " ").Replace("\\n", " ").Replace("\r", " ").Replace("&#10;", " ").Replace("&#x0d;", " "));
        }

        private void ChangeFileNumber(int change)
        {
            //If there's no filename declared, show the default one.
            if (string.IsNullOrWhiteSpace(UserSettings.All.LatestFilename))
            {
                UserSettings.All.LatestFilename = FindResource("SaveAs.File.Animation") as string;
                return;
            }

            var index = UserSettings.All.LatestFilename.Length;
            int start = -1, end = -1;

            //Detects the last number in a string.
            foreach (var c in UserSettings.All.LatestFilename.Reverse())
            {
                if (char.IsNumber(c))
                {
                    if (end == -1)
                        end = index;

                    start = index - 1;
                }
                else if (start == index)
                    break;

                index--;
            }

            //If there's no number.
            if (end == -1)
            {
                UserSettings.All.LatestFilename += $" ({change})";
                return;
            }

            //If iy's a negative number, include the signal.
            if (start > 0 && UserSettings.All.LatestFilename.Substring(start - 1, 1).Equals("-"))
                start--;

            //Cut, convert, merge.
            int number;
            if (int.TryParse(UserSettings.All.LatestFilename.Substring(start, end - start), out number))
            {
                var offset = start + number.ToString().Length;

                UserSettings.All.LatestFilename = UserSettings.All.LatestFilename.Substring(0, start) + (number + change) +
                    UserSettings.All.LatestFilename.Substring(offset, UserSettings.All.LatestFilename.Length - end);
            }
        }

        private void UpdateStatistics()
        {
            TotalDuration = TimeSpan.FromMilliseconds(ListFrames.Sum(x => x.Delay));
            FrameSize = ListFrames.Count > 0 ? ListFrames[0].ImageLocation.ScaledSize() : new Size(0, 0);
            FrameScale = ListFrames.Count > 0 ? Convert.ToInt32(ListFrames[0].ImageLocation.DpiOf() / 96d * 100d) : 0;
            AverageDelay = ListFrames.Count > 0 ? ListFrames.Average(x => x.Delay) : 0;
        }

        private void RemoveAdorners()
        {
            #region Remove adorners

            var adornerLayer = AdornerLayer.GetAdornerLayer(WatermarkImage);

            //Remove all the adorners.
            foreach (var adorner in (adornerLayer.GetAdorners(WatermarkImage) ?? new Adorner[0]).OfType<ResizingAdorner>())
            {
                adorner.Destroy();
                adornerLayer.Remove(adorner);
            }

            #endregion
        }

        private void ShowHint(string hint, params object[] values)
        {
            if (HintTextBlock.Visibility == Visibility.Visible)
            {
                BeginStoryboard(FindStoryboard("HideHintStoryboard"), HandoffBehavior.Compose);
            }

            if (values.Length == 0)
                HintTextBlock.Text = TryFindResource(hint) + "";
            else
                HintTextBlock.Text = string.Format(TryFindResource(hint) + "", values);

            BeginStoryboard(FindStoryboard("ShowHintStoryboard"), HandoffBehavior.Compose);
        }

        private Storyboard FindStoryboard(string key)
        {
            return (Storyboard)TryFindResource(key);
        }

        private void SetFocusOnCurrentFrame()
        {
            var current = FrameListView.SelectedItem as FrameListBoxItem;
            current?.Focus();
        }
        
        private void ChangeProgressText(long cumulative, long total, int current)
        {
            switch (ProgressPrecisionComboBox.SelectedIndex)
            {
                case 0: //Minutes
                    ProgressHorizontalTextBlock.Text = UserSettings.All.ProgressShowTotal ? TimeSpan.FromMilliseconds(cumulative).ToString(@"m\:ss") + "/" + TimeSpan.FromMilliseconds(total).ToString(@"m\:ss")
                        : TimeSpan.FromMilliseconds(cumulative).ToString(@"m\:ss");
                    break;
                case 1: //Seconds
                    ProgressHorizontalTextBlock.Text = UserSettings.All.ProgressShowTotal ? (int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds + "/" + TimeSpan.FromMilliseconds(total).TotalSeconds + " s"
                        : (int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds + " s";
                    break;
                case 2: //Miliseconds
                    ProgressHorizontalTextBlock.Text = UserSettings.All.ProgressShowTotal ? cumulative + "/" + total + " ms" : cumulative + " ms";
                    break;
                case 3: //Percentage
                    var count = (double)ListFrames.Count;
                    ProgressHorizontalTextBlock.Text = UserSettings.All.ProgressShowTotal ? (current / count * 100).ToString("##0.#") + "/100%"
                        : (current / count * 100).ToString("##0.# %");
                    break;
                case 4: //Frame number
                    ProgressHorizontalTextBlock.Text = UserSettings.All.ProgressShowTotal ? current + "/" + ListFrames.Count
                        : current.ToString();
                    break;
                case 5: //Custom
                    ProgressHorizontalTextBlock.Text = CustomProgressTextBox.Text
                        .Replace("$ms", cumulative.ToString())
                        .Replace("$s", ((int)TimeSpan.FromMilliseconds(cumulative).TotalSeconds).ToString())
                        .Replace("$m", TimeSpan.FromMilliseconds(cumulative).ToString())
                        .Replace("$p", (current / (double)ListFrames.Count * 100).ToString("##0.#"))
                        .Replace("$f", current.ToString())
                        .Replace("@ms", total.ToString())
                        .Replace("@s", ((int)TimeSpan.FromMilliseconds(total).TotalSeconds).ToString())
                        .Replace("@m", TimeSpan.FromMilliseconds(total).ToString(@"m\:ss"))
                        .Replace("@p", "100")
                        .Replace("@f", ListFrames.Count.ToString());
                    break;
            }
        }

        private void ChangeProgressTextToCurrent()
        {
            var total = ListFrames.Sum(y => y.Delay);
            var cumulative = 0L;

            for (var j = 0; j < FrameListView.SelectedIndex; j++)
                cumulative += ListFrames[j].Delay;

            ChangeProgressText(cumulative, total, FrameListView.SelectedIndex);
        }

        #endregion

        #endregion
    }
}
