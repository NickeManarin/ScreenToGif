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
            var newAnim = new Create();
            var result = newAnim.ShowDialog();

            if (!result.HasValue || result != true) return;

            #region FileName

            string pathTemp = Path.GetTempPath() + String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

            if (!Directory.Exists(pathTemp))
                Directory.CreateDirectory(pathTemp);

            var fileName = String.Format("{0}{1}.bmp", pathTemp, 0);

            #endregion

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
            Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            if (result.HasValue && recorder.ExitArg == ExitAction.Recorded && recorder.ListFrames != null)
            {
                DiscardProjectButton_Click(null, null);

                LoadNewFrames(recorder.ListFrames);
            }

            Encoder.Restore();
            Show();
        }

        private void OpenMediaProjectButton_Click(object sender, RoutedEventArgs e)
        {
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
            Hide();

            var webcam = new Webcam();
            var result = webcam.ShowDialog();

            Show();

            if (result.HasValue && !result.Value)
            {
                if (webcam.ExitArg == ExitAction.Recorded)
                {
                    DiscardProjectButton_Click(null, null);

                    LoadNewFrames(webcam.ListFrames);
                }
            }
        }

        #endregion

        #region Insert

        private void InsertImageButton_Click(object sender, RoutedEventArgs e)
        {
            var insert = new Insert();
            insert.ShowDialog();
        }

        private void InsertWebcamRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO:
            //Open the recording window.
            //Get the list of frames.
            //Change size to match current one.
            //Delete the temp folder of the recording.
        }

        private void InsertRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            Encoder.Minimize();

            var recorder = new Recorder();
            var result = recorder.ShowDialog();

            if (result.HasValue && recorder.ExitArg == ExitAction.Recorded && recorder.ListFrames != null)
            {
                var insert = new Insert(ListFrames, recorder.ListFrames);
                insert.ShowDialog();
            }

            Encoder.Restore();
            Show();

            //TODO:
            //Open the recording window. OK
            //Get the list of frames. OK
            //Change size to match current one.
            //Delete the temp folder of the recording.
        }

        #endregion

        #region Project/Export/Discard

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                //TODO: Add to the Undo stack.
                //Also, create the Copy of the images, and put inside a folder.

                int selectedIndex = FrameListView.SelectedIndex;

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

            for (int i = countList; i > FrameListView.SelectedIndex; i--) //From the end to the middle.
            {
                DeleteFrame(i);
            }
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
                    ZoomBoxControl.Visibility = Visibility.Visible;
                    WelcomeTextBlock.Visibility = Visibility.Collapsed;
                });

                ShowProgress("Loading Frames", ListFrames.Count);

                if (ListFrames != null)
                {
                    foreach (FrameInfo frame in ListFrames)
                    {
                        #region Cursor Merge

                        if (Settings.Default.ShowCursor)
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

            try
            {
                switch (fileName.Split('.').Last())
                {
                    case "stg":

                        ListFrames = ImportFromProject(fileName, pathTemp);
                        break;

                    case "gif":

                        ListFrames = ImportFromGif(fileName, pathTemp); //TODO: Remake.
                        break;

                    case "mp4":
                    case "wmv":
                    case "avi":

                        ListFrames = ImportFromVideo(fileName, pathTemp); //TODO: Remake. Show status.
                        break;

                    default:

                        ListFrames = ImportFromImage(fileName, pathTemp);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Import Error");
                return false;
            }

            return Load();
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
                #region Each Frae

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
            }
            else
            {
                PlayButton.Text = Properties.Resources.Con_StopPreview;
                PlayButton.Content = FindResource("Vector.Pause");

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
            }
        }

        #endregion

        #region UI Changes

        private void ShowProgress(string description, int maximum)
        {
            Dispatcher.InvokeAsync(() =>
            {
                StatusLabel.Content = description;
                StatusProgressBar.Maximum = maximum;
                StatusProgressBar.Value = 0;
                StatusGrid.Visibility = Visibility.Visible;
            }, DispatcherPriority.Loaded);
        }

        private void UpdateProgress(int value)
        {
            Dispatcher.InvokeAsync(() =>
            {
                StatusProgressBar.Value = value;
            });
        }

        private void HideProgress()
        {
            Dispatcher.InvokeAsync(() =>
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

        //Test
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Dialog.Ask("Something Wrong", "You just did something wrong",
                "Here is the test that explains what is \b happening and why you should cry. " +
                "\n Also, this is supposed to be in a new line. \v And this is supposed to be a vertical tab." +
                "\n\r More text. " +
                "\n\r Even more text. " +
                "\n\r Nothing else than text. " +
                " Should I call the ghostbuster?");
        }

        private void ExceptionTestButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("Not yet implemented", new TimeZoneNotFoundException("Not found, hahaha", new ArithmeticException()));
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            //http://stackoverflow.com/questions/6503851/how-to-undo-the-paint-operation-using-c-sharp
            //http://www.codeproject.com/Articles/18025/Generic-Memento-Pattern-for-Undo-Redo-in-C
            //http://www.codeproject.com/Articles/456591/Simple-Undo-redo-library-for-Csharp-NET
        }
    }
}
