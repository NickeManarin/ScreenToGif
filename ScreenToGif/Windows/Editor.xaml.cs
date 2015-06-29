using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;
using ScreenToGif.Util.Writers;
using ScreenToGif.Windows.Other;
using ScreenToGif.Windows.Other.Page;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : Window
    {
        #region Variables

        public static readonly DependencyProperty FilledListProperty = DependencyProperty.Register("FilledList", typeof(bool), typeof(Editor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// True if there is a value inside the list of frames.
        /// </summary>
        public bool FilledList
        {
            get { return (bool)GetValue(FilledListProperty); }
            set { SetValue(FilledListProperty, value); }
        }

        /// <summary>
        /// The List of Frames.
        /// </summary>
        public List<FrameInfo> ListFrames { get; set; }

        private readonly System.Windows.Forms.Timer _timerPreview = new System.Windows.Forms.Timer();

        #endregion

        #region Initialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Editor()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ListFrames != null)
            {
                this.Cursor = Cursors.AppStarting;
                FrameListView.IsEnabled = false;
                RibbonTabControl.IsEnabled = false;

                ActionStack.Prepare(ListFrames[0].ImageLocation);

                _loadFramesDel = Load;
                _loadFramesDel.BeginInvoke(LoadCallback, null);
            }

            KeyUp += Editor_KeyUp;
        }

        #endregion

        #region Frame Selection

        private void FrameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[e.AddedItems.Count - 1] as FrameListBoxItem;

                if (item != null)
                {
                    ZoomBoxControl.ImageSource = ListFrames[item.FrameNumber].ImageLocation;
                    FrameListView.ScrollIntoView(item);
                }

                GC.Collect(1);
                return;
            }

            if (e.RemovedItems.Count > 0)
            {
                var item = e.RemovedItems[e.RemovedItems.Count - 1] as FrameListBoxItem;

                if (item != null)
                    ZoomBoxControl.ImageSource = ListFrames[(item.FrameNumber - 1) < 0 ? 0 : item.FrameNumber - 1].ImageLocation;

                GC.Collect(1);
            }
        }

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as FrameListBoxItem;

            if (item != null)
                ZoomBoxControl.ImageSource = ListFrames[item.FrameNumber].ImageLocation;

            GC.Collect(1);
        }

        #endregion

        #region File Tab

        #region New/Open

        private void NewAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            var newAnim = new Create();
            var result = newAnim.ShowDialog();

            if (!result.HasValue || result != true) return;

            #region FileName

            string pathTemp = Path.Combine(Path.GetTempPath(), @"ScreenToGif\Recording", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            if (!Directory.Exists(pathTemp))
                Directory.CreateDirectory(pathTemp);

            var fileName = Path.Combine(pathTemp, "0.bmp");

            #endregion

            ActionStack.Clear();
            ActionStack.Prepare(pathTemp);

            #region Create and Save Image

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                var bitmapSource = ImageMethods.CreateEmtpyBitmapSource(newAnim.Color, newAnim.WidthValue, newAnim.HeightValue, PixelFormats.Indexed1);
                var bitmapFrame = BitmapFrame.Create(bitmapSource);

                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(bitmapFrame);
                encoder.Save(stream);
                stream.Flush();
                stream.Close();
            }

            GC.Collect();

            #endregion

            #region Adds to the List

            var frame = new FrameInfo(fileName, 66);

            ListFrames = new List<FrameInfo> { frame };
            LoadNewFrames(ListFrames);

            #endregion
        }

        private void NewRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            if (result.HasValue && recorder.ExitArg == ExitAction.Recorded && recorder.ListFrames != null)
            {
                DiscardProjectButton_Click(null, null);

                ActionStack.Clear();
                ActionStack.Prepare(recorder.ListFrames[0].ImageLocation);

                LoadNewFrames(recorder.ListFrames);
            }

            Encoder.Restore();
            Show();
        }

        private void OpenMediaProjectButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = "Open a Media (Image or Video) or a Project File",
                Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi|" +
                         "ScreenToGif Project (*.stg) |*.stg",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                DiscardProjectButton_Click(null, null);

                _importFramesDel = ImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileName, CreateTempPath(), ImportFromCallback, null);
            }
        }

        private void NewWebcamRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            Hide();

            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            Show();

            if (result.HasValue && !result.Value)
            {
                if (webcam.ExitArg == ExitAction.Recorded)
                {
                    DiscardProjectButton_Click(null, null);

                    ActionStack.Clear();
                    ActionStack.Prepare(webcam.ListFrames[0].ImageLocation);

                    LoadNewFrames(webcam.ListFrames);
                }
            }
        }

        #endregion

        #region Insert

        private void InsertImageButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            var ofd = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                Title = "Open a Media (Image or Video)",
                Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif|" +
                         "Video (*.mp4, *.wmv, *.avi)|*.mp4;*.wmv;*.avi",
            };

            var result = ofd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);

                _importFramesDel = InsertImportFrom;
                _importFramesDel.BeginInvoke(ofd.FileName, CreateTempPath(), ImportFromCallback, null);
            }
        }

        private void InsertWebcamRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            Hide();
            Encoder.Minimize();

            var recorder = new Webcam();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();
                Encoder.Restore();
                Show();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex);
            insert.Owner = this;

            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);
                LoadNewFrames(insert.ActualList);
            }

            #endregion

            Encoder.Restore();
            Show();
        }

        private void InsertRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
            Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            #region If recording cancelled

            if (!result.HasValue || recorder.ExitArg != ExitAction.Recorded || recorder.ListFrames == null)
            {
                GC.Collect();
                Encoder.Restore();
                Show();

                return;
            }

            #endregion

            #region Insert

            var insert = new Insert(ListFrames.CopyList(), recorder.ListFrames, FrameListView.SelectedIndex) { Owner = this };
            result = insert.ShowDialog();

            if (result.HasValue && result.Value)
            {
                ActionStack.Did(ListFrames);
                LoadNewFrames(insert.ActualList);
            }

            #endregion

            Encoder.Restore();
            Show();
        }

        #endregion

        #region Project/Export/Discard

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Pause();

                var ofd = new SaveFileDialog();
                ofd.AddExtension = true;
                ofd.Filter = "Gif Animation (*.gif)|*.gif";
                ofd.Title = "Save Animation As Gif"; //TODO: Better description.

                var result = ofd.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                Encoder.AddItem(ListFrames.CopyToEncode(), ofd.FileName);
            }
            catch (Exception ex)
            {
                Dialog.Ok("Error While Saving", "Error while saving the animation", ex.Message);
                LogWriter.Log(ex, "Error while trying to save an animation.");
            }
        }

        private void SaveVideoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Pause();

                var ofd = new SaveFileDialog();
                ofd.AddExtension = true;
                ofd.Filter = "Avi Video (*.avi)|*.avi";
                ofd.Title = "Save Animation as Video";

                var result = ofd.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                Encoder.AddItem(ListFrames.CopyToEncode(), ofd.FileName);
            }
            catch (Exception ex)
            {
                Dialog.Ok("Error While Saving", "Error while saving the animation", ex.Message);
                LogWriter.Log(ex, "Error while trying to save an animation.");
            }
        }

        private void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            #region Export as Project

            try
            {
                Pause();

                if (ListFrames.Count == 0)
                    throw new UsageException("You don't have frames to be exported.", "You need to add at least a frame to be able to export the project.");

                #region Save Dialog

                var saveDialog = new SaveFileDialog();
                saveDialog.AddExtension = true;
                saveDialog.DefaultExt = ".stg";
                saveDialog.FileName = String.Format("Project - {0} Frames", ListFrames.Count);
                saveDialog.Filter = "*.stg|(ScreenToGif Project)|*.zip|(Zip Archive)";
                saveDialog.Title = "Select the File Location";

                var result = saveDialog.ShowDialog();

                if (!result.HasValue || !result.Value)
                    return;

                #endregion

                string serial = Serializer.SerializeToString(ListFrames);

                if (serial == null)
                    throw new Exception("Object serialization failed.");

                string tempDirectory = Path.GetDirectoryName(ListFrames.First().ImageLocation);

                var dir = Directory.CreateDirectory(Path.Combine(tempDirectory, "Export"));

                File.WriteAllText(Path.Combine(dir.FullName, "List.sb"), serial);

                foreach (FrameInfo frameInfo in ListFrames)
                {
                    File.Copy(frameInfo.ImageLocation, Path.Combine(dir.FullName, Path.GetFileName(frameInfo.ImageLocation)));
                }

                ZipFile.CreateFromDirectory(dir.FullName, saveDialog.FileName);
            }
            catch (UsageException us)
            {
                //TODO: Message.
            }
            catch (Exception ex)
            {
                //TODO: Message.

                LogWriter.Log(ex, "Exporting Recording as a Project");
            }

            #endregion
        }

        private void DiscardProjectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Pause();

                if (ListFrames == null || ListFrames.Count == 0)
                    return;

                FrameListView.SelectionChanged -= FrameListView_SelectionChanged;
                FrameListView.SelectedIndex = -1;

                FrameListView.Items.Clear();
                ZoomBoxControl.Clear();

                foreach (FrameInfo frame in ListFrames)
                {
                    File.Delete(frame.ImageLocation);
                }

                string path = Path.GetDirectoryName(ListFrames[0].ImageLocation);
                var folderList = Directory.EnumerateDirectories(path);

                foreach (string folder in folderList)
                {
                    if (!folder.StartsWith("Enc"))
                        Directory.Delete(folder, true);
                }

                FilledList = false;
                ListFrames.Clear();

                FrameListView.Visibility = Visibility.Collapsed;
                WelcomeBorder.Visibility = Visibility.Visible;
                WelcomeTextBlock.Text = "See? You just pushed a button. Yay!";

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
            }
            catch (Exception ex)
            {
                Dialog.Ok("Discard Error", "Error while trying to discard the project", ex.Message);
                LogWriter.Log(ex, "Error while trying to Discard the Project");
            }

            GC.Collect();
        }

        #endregion

        #endregion

        #region View Tab

        private void FirstButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectedIndex = 0;
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == 0)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            //Show previous frame.
            FrameListView.SelectedIndex--;
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            if (FrameListView.SelectedIndex == -1 || FrameListView.SelectedIndex == FrameListView.Items.Count - 1)
            {
                FrameListView.SelectedIndex = 0;
                return;
            }

            //Show next frame.
            FrameListView.SelectedIndex++;
        }

        private void LastButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
        }

        private void Zoom100Button_Click(object sender, RoutedEventArgs e)
        {
            ZoomBoxControl.Zoom = 1.0;
        }

        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            #region Get the sizes

            var height = ListFrames.First().ImageLocation.SourceFrom().Height;
            var width = ListFrames.First().ImageLocation.SourceFrom().Width;
            var viewHeight = ZoomBoxControl.ActualHeight;
            var viewWidth = ZoomBoxControl.ActualWidth;

            #endregion

            #region Calculate the Zoom

            var zoomHeight = 0D;
            var zoomWidth = 0D;

            if (width > viewWidth)
            {
                zoomHeight = viewHeight / height;
            }
            
            if (height > viewHeight)
            {
                zoomWidth = viewWidth /width;
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
        }

        private void ShowEncoderButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: If already started, restore the window
            Encoder.Start();
        }

        #endregion

        #region Options Tab

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Options();
            options.ShowDialog();
        }

        #endregion

        #region Edit Tab

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ActionStack.CanUndo())
                return;

            Pause();

            ListFrames = ActionStack.Undo(ListFrames.CopyList());
            LoadNewFrames(ListFrames);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ActionStack.CanUndo())
                return;

            Pause();

            ListFrames = ActionStack.Reset(ListFrames.CopyList());
            LoadNewFrames(ListFrames);
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ActionStack.CanRedo())
                return;

            Pause();

            ListFrames = ActionStack.Redo(ListFrames.CopyList());
            LoadNewFrames(ListFrames);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            //Add current selected frames to the "clipboard"
            //Not sure if the actual clipboard or just mine own 
            //(copy to another folder and save in an aux variable)

            //How can I make the slide in animation?
            //Also, I need to remove the page from the children.

            var cliboard = new ClipboardPage();
            MiddleGrid.Children.Add(cliboard);
            Grid.SetColumn(cliboard, 2);
        }

        private void MoveLeftButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            if (ListFrames.Count == 1)
                return;

            ActionStack.Did(ListFrames);

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>()).OrderBy(x => x.FrameNumber).ToList();
            FrameListView.SelectedItems.Clear();

            var listIndex = selected.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selected)
            {
                #region Index

                var oldindex = listIndex[selected.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index - 1;

                if (index == 0)
                    newIndex = FrameListView.Items.Count - 1;

                if (oldindex - 1 == index)
                {
                    FrameListView.SelectedItems.Add(item);
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

                FrameListView.SelectedItems.Add(item);
            }

            #region Count Frames

            foreach (var frame in FrameListView.Items.OfType<FrameListBoxItem>())
            {
                frame.FrameNumber = FrameListView.Items.IndexOf(frame);
            }

            #endregion

            #endregion
        }

        private void MoveRightButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            if (ListFrames.Count == 1)
                return;

            ActionStack.Did(ListFrames);

            #region Move Selected Frame to the Left

            var selected = new List<FrameListBoxItem>(FrameListView.SelectedItems.OfType<FrameListBoxItem>()).OrderByDescending(x => x.FrameNumber).ToList();
            FrameListView.SelectedItems.Clear();

            var listIndex = selected.Select(frame => FrameListView.Items.IndexOf(frame)).ToList();

            foreach (var item in selected)
            {
                #region Index

                var oldindex = listIndex[selected.IndexOf(item)];
                var index = FrameListView.Items.IndexOf(item);
                var newIndex = index + 1;

                if (index == FrameListView.Items.Count - 1)
                    newIndex = 0;

                if (oldindex + 1 == index)
                {
                    FrameListView.SelectedItems.Add(item);
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

                FrameListView.SelectedItems.Add(item);
            }

            #region Count Frames

            foreach (var frame in FrameListView.Items.OfType<FrameListBoxItem>())
            {
                frame.FrameNumber = FrameListView.Items.IndexOf(frame);
            }

            #endregion

            #endregion
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            try
            {
                if (ListFrames.Count == 1)
                {
                    if (Dialog.Ask("Last Frame", "Do you want to remove the last frame?",
                        "You are trying to remove the last frame. \n\rYou can't undo this operation.", Dialog.Icons.Question))
                    {
                        DiscardProjectButton_Click(null, null);
                    }

                    return;
                }

                int selectedIndex = FrameListView.SelectedIndex;

                ActionStack.Did(ListFrames);
                DeleteFrame(selectedIndex);

                AdjustFrameNumbers(selectedIndex);
                SelectNear(selectedIndex);
            }
            catch (Exception ex)
            {
                Dialog.Ok("Error", "Error while trying to remove frame", ex.Message); //TODO: make a link to the error dialog.
                LogWriter.Log(ex, "Error While Trying to Delete Frame");
            }
        }

        private void DeleteBeforeButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            ActionStack.Did(ListFrames);

            for (int index = FrameListView.SelectedIndex - 1; index >= 0; index--)
            {
                DeleteFrame(index);
            }

            AdjustFrameNumbers(0);
            SelectNear(0);
        }

        private void DeleteAfterButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            int countList = FrameListView.Items.Count - 1; //So we have a fixed value.

            ActionStack.Did(ListFrames);

            for (int i = countList; i > FrameListView.SelectedIndex; i--) //From the end to the middle.
            {
                DeleteFrame(i);
            }
        }

        private void ReverseOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            if (ListFrames.Count == 1)
                return;

            ActionStack.Did(ListFrames);

            ListFrames.Reverse();

            LoadNewFrames(ListFrames);
        }

        private void YoyoOrderButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();

            if (ListFrames.Count == 1)
                return;

            ActionStack.Did(ListFrames);

            LoadNewFrames(Util.Other.Yoyo(ListFrames));
        }

        #endregion

        #region Image Tab

        private void ResizeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Other Events

        private void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.SystemKey == Key.LeftAlt)
                e.Handled = true;
        }

        private void timerPreview_Tick(object sender, EventArgs e)
        {
            _timerPreview.Tick -= timerPreview_Tick;

            //Sets the interval for this frame. If this frame has 500ms, the next frame will take 500ms to show.
            _timerPreview.Interval = ListFrames[FrameListView.SelectedIndex].Delay;

            if (ListFrames.Count - 1 == FrameListView.SelectedIndex)
            {
                FrameListView.SelectedIndex = 0;
            }
            else
            {
                FrameListView.SelectedIndex++;
            }

            _timerPreview.Tick += timerPreview_Tick;

            GC.Collect(2);
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control || Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt)
            {
                e.Handled = false;
                return;
            }

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

        #region Drag and Drop

        private void ZoomBoxControl_Drop(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (fileNames != null)
                foreach (string name in fileNames)
                {
                    //TODO: Make the import into current recording.
                }
        }

        private void Grid_DragEnter(object sender, DragEventArgs e)
        {
            Pause();

            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        #endregion

        #endregion

        #region Private Methods

        #region Load

        #region Async Loading

        private delegate bool LoadFrames();

        private LoadFrames _loadFramesDel = null;

        private bool Load()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    FrameListView.Items.Clear();
                    FrameListView.UpdateLayout();
                    ZoomBoxControl.Visibility = Visibility.Visible;
                    WelcomeBorder.Visibility = Visibility.Collapsed;
                });

                ShowProgress("Loading Frames", ListFrames.Count);

                if (ListFrames != null)
                {
                    foreach (FrameInfo frame in ListFrames)
                    {
                        #region Cursor Merge

                        if (Settings.Default.ShowCursor && frame.CursorInfo != null)
                        {
                            try
                            {
                                using (var imageTemp = frame.ImageLocation.From())
                                {
                                    using (var graph = Graphics.FromImage(imageTemp))
                                    {
                                        #region Mouse Clicks

                                        if (frame.CursorInfo.Clicked && Settings.Default.MouseClicks)
                                        {
                                            //Draws the ellipse first, to get behind the cursor.
                                            var rectEllipse = new Rectangle(
                                                (int)frame.CursorInfo.Position.X - (frame.CursorInfo.Image.Width / 2),
                                                (int)frame.CursorInfo.Position.Y - (frame.CursorInfo.Image.Height / 2),
                                                    frame.CursorInfo.Image.Width - 10,
                                                    frame.CursorInfo.Image.Height - 10);

                                            graph.DrawEllipse(new System.Drawing.Pen(new SolidBrush(System.Drawing.Color.Yellow), 3), rectEllipse);
                                        }

                                        #endregion

                                        var rect = new Rectangle(
                                            (int)frame.CursorInfo.Position.X,
                                            (int)frame.CursorInfo.Position.Y,
                                            frame.CursorInfo.Image.Width,
                                            frame.CursorInfo.Image.Height);

                                        graph.DrawImage(frame.CursorInfo.Image, rect);
                                        graph.Flush();

                                        frame.CursorInfo.Image.Dispose();
                                    }

                                    imageTemp.Save(frame.ImageLocation);
                                }
                            }
                            catch (Exception) { }
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
                            itemInvoked.PreviewMouseLeftButtonDown += Item_PreviewMouseLeftButtonDown;

                            FrameListView.Items.Add(itemInvoked);

                            UpdateProgress(itemInvoked.FrameNumber);
                        });

                    }

                    if (ListFrames.Count > 0)
                        Dispatcher.Invoke(() => { FilledList = true; });
                }

                HideProgress();

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Frame Loading Error");
                return false;
            }
        }

        private void LoadCallback(IAsyncResult ar)
        {
            bool result = _loadFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                this.Cursor = Cursors.Arrow;

                //Re-enable the disable controls.
                RibbonTabControl.IsEnabled = true;
                FrameListView.IsEnabled = true;
                FrameListView.Visibility = Visibility.Visible;

                if (!result)
                {
                    //TODO: Expect errors
                    return;
                }

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
                FrameListView.SelectedIndex = 0;
            });
        }

        #endregion

        #region Async Import

        private delegate bool ImportFrames(string fileName, string pathTemp);

        private ImportFrames _importFramesDel = null;

        private List<FrameInfo> InsertInternal(string fileName, string pathTemp)
        {
            List<FrameInfo> listFrames;

            try
            {
                switch (fileName.Split('.').Last())
                {
                    case "stg":

                        listFrames = ImportFromProject(fileName, pathTemp);
                        break;

                    case "gif":

                        listFrames = ImportFromGif(fileName, pathTemp); //TODO: Remake.
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
                return null;
            }

            return listFrames;
        }

        private bool ImportFrom(string fileName, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;

                RibbonTabControl.IsEnabled = false;
                ZoomBoxControl.IsEnabled = false;
                FrameListView.IsEnabled = false;
                FrameListView.Visibility = Visibility.Collapsed;

                FrameListView.SelectionChanged -= FrameListView_SelectionChanged;
                FrameListView.SelectedItem = null;
            });

            #endregion

            ShowProgress("Preparing to Import", 100);

            ListFrames = InsertInternal(fileName, pathTemp);

            return Load();
        }

        private bool InsertImportFrom(string fileName, string pathTemp)
        {
            #region Disable UI

            Dispatcher.Invoke(() =>
            {
                Cursor = Cursors.AppStarting;

                RibbonTabControl.IsEnabled = false;
                ZoomBoxControl.IsEnabled = false;
                FrameListView.IsEnabled = false;
                FrameListView.Visibility = Visibility.Collapsed;
            });

            #endregion

            ShowProgress("Preparing to Import", 100);

            var auxList = InsertInternal(fileName, pathTemp);

            Dispatcher.Invoke(() =>
            {
                #region Insert

                var insert = new Insert(ListFrames, auxList, FrameListView.SelectedIndex) { Owner = this };
                var result = insert.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    LoadNewFrames(insert.ActualList);
                }

                #endregion
            });

            return true;
        }

        private void ImportFromCallback(IAsyncResult ar)
        {
            bool result = _importFramesDel.EndInvoke(ar);

            GC.Collect();

            Dispatcher.Invoke(delegate
            {
                Cursor = Cursors.Arrow;

                //Re-enable the disable controls.
                RibbonTabControl.IsEnabled = true;
                ZoomBoxControl.IsEnabled = true;
                FrameListView.IsEnabled = true;
                FrameListView.Visibility = Visibility.Visible;

                if (!result)
                {
                    //TODO: Expect errors
                    return;
                }

                FrameListView.SelectionChanged += FrameListView_SelectionChanged;
                FrameListView.SelectedIndex = 0;
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

                int count = 0;
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
                //TODO: Message.

                return new List<FrameInfo>();
            }
        }

        private List<FrameInfo> ImportFromGif(string sourceFileName, string pathTemp)
        {
            var gifDecoder = new GifDecoder();
            gifDecoder.Read(sourceFileName);

            var list = ImageMethods.GetFrames(sourceFileName);

            var listFrames = new List<FrameInfo>();

            int frameCount = gifDecoder.GetFrameCount();

            ShowProgress("Importing Frames", frameCount);

            for (int index = 0; index < frameCount; index++)
            {
                #region Each Frame

                var fileName = Path.Combine(pathTemp, index + ".bmp");

                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    //var frameAux = gifDecoder.GetFrame(index);
                    var frameAux = list[index];
                    frameAux.Save(stream, ImageFormat.Png);
                    frameAux.Dispose();

                    stream.Flush();
                    stream.Close();
                }

                var frame = new FrameInfo(fileName, gifDecoder.GetDelay(index));
                listFrames.Add(frame);

                UpdateProgress(index);

                GC.Collect(1);

                #endregion
            }

            #region Old Way to Save the Image to the Recording Folder

            //var decoder = new GifBitmapDecoder(new Uri(sourceFileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            //int count = 0;

            //foreach (BitmapFrame bitmapFrame in decoder.Frames)
            //{
            //    var fileName = String.Format("{0}{1}.bmp", pathTemp, count);

            //    using (var stream = new FileStream(fileName, FileMode.Create))
            //    {
            //        BitmapEncoder encoder = new BmpBitmapEncoder();
            //        encoder.Frames.Add(BitmapFrame.Create(bitmapFrame));
            //        encoder.Save(stream);
            //        stream.Close();
            //    }

            //    var frame = new FrameInfo(fileName, 66);
            //    listFrames.Add(frame);

            //    count++;
            //}

            #endregion

            return listFrames;
        }

        private List<FrameInfo> ImportFromImage(string sourceFileName, string pathTemp)
        {
            var fileName = Path.Combine(pathTemp, 0 + ".bmp");

            #region Save the Image to the Recording Folder

            var bitmap = new BitmapImage(new Uri(sourceFileName));

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
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
            int delay = 66;

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

            if (frameList == null) return null;

            ShowProgress("Importing Frames", frameList.Count);

            #region Saves the Frames to the Disk

            var frameInfoList = new List<FrameInfo>();
            int count = 0;

            foreach (BitmapFrame frame in frameList)
            {
                var frameName = Path.Combine(pathTemp, count + ".bmp");

                using (var stream = new FileStream(frameName, FileMode.Create))
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
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
                _timerPreview.Tick -= timerPreview_Tick;
                _timerPreview.Stop();

                PlayButton.Text = Properties.Resources.Con_PlayPreview;
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");
            }
            else
            {
                PlayButton.Text = Properties.Resources.Con_StopPreview;
                PlayButton.Content = FindResource("Vector.Pause");
                PlayPauseButton.Content = FindResource("Vector.Pause");

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

                _timerPreview.Tick += timerPreview_Tick;
                _timerPreview.Start();
            }
        }

        private void Pause()
        {
            if (_timerPreview.Enabled)
            {
                _timerPreview.Tick -= timerPreview_Tick;
                _timerPreview.Stop();

                PlayButton.Text = Properties.Resources.Con_PlayPreview;
                PlayButton.Content = FindResource("Vector.Play");
                PlayPauseButton.Content = FindResource("Vector.Play");
            }
        }

        #endregion

        #region UI Changes

        private void ShowProgress(string description, int maximum)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Content = description;
                StatusProgressBar.Maximum = maximum;
                StatusProgressBar.Value = 0;
                StatusGrid.Visibility = Visibility.Visible;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.Invoke(() =>
            {
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                StatusGrid.Visibility = Visibility.Hidden;
            });
        }

        private void SelectNear(int index)
        {
            if (FrameListView.Items.Count - 1 < index)
            {
                FrameListView.SelectedIndex = FrameListView.Items.Count - 1;
                return;
            }

            FrameListView.SelectedIndex = index;
        }

        private void AdjustFrameNumbers(int startIndex)
        {
            for (int index = startIndex; index < FrameListView.Items.Count; index++)
            {
                ((FrameListBoxItem)FrameListView.Items[index]).FrameNumber = index;
            }
        }

        #endregion

        #region Other

        /// <summary>
        /// Loads the new frames and clears the old ones.
        /// </summary>
        /// <param name="listFrames">The new list of frames.</param>
        private void LoadNewFrames(List<FrameInfo> listFrames)
        {
            Cursor = Cursors.AppStarting;
            RibbonTabControl.IsEnabled = false;
            FrameListView.IsEnabled = false;
            FrameListView.Visibility = Visibility.Collapsed;

            ListFrames = listFrames;
            FrameListView.SelectedItem = null;
            ZoomBoxControl.ImageSource = null;

            _loadFramesDel = Load;
            _loadFramesDel.BeginInvoke(LoadCallback, null);
        }

        private static string CreateTempPath()
        {
            #region Temp Path

            string pathTemp = Path.GetTempPath() + String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

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

        #endregion

        #endregion
    }
}
