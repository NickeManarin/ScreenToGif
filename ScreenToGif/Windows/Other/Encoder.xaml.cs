using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.ImageUtil;
using ScreenToGif.ImageUtil.Gif.Encoder;
using ScreenToGif.ImageUtil.Gif.LegacyEncoder;
using ScreenToGif.ImageUtil.Video;
using ScreenToGif.Util;
using ScreenToGif.Util.Model;
using ScreenToGif.Util.Parameters;
using Clipboard = System.Windows.Clipboard;
using Point = System.Windows.Point;

namespace ScreenToGif.Windows.Other
{
    public partial class Encoder : Window
    {
        #region Variables

        /// <summary>
        /// The static Encoder window.
        /// </summary>
        private static Encoder _encoder = null;

        /// <summary>
        /// List of Tasks, each task executes the encoding process for one recording.
        /// </summary>
        private static readonly List<Task> TaskList = new List<Task>();

        /// <summary>
        /// List of CancellationTokenSource, used to cancel each task.
        /// </summary>
        private static readonly List<CancellationTokenSource> CancellationTokenList = new List<CancellationTokenSource>();

        /// <summary>
        /// The start point of the draging operation.
        /// </summary>
        private Point _dragStart = new Point(0, 0);

        /// <summary>
        /// The latest state of the window. Used when hiding the window to show the recorder.
        /// </summary>
        private static WindowState _lastState = WindowState.Normal;

        #endregion

        public Encoder()
        {
            InitializeComponent();
        }

        #region Events

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //TODO: Check for memmory leaks.

            foreach (var tokenSource in CancellationTokenList)
            {
                tokenSource.Cancel();
            }

            _encoder = null;
            GC.Collect();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            foreach (var item in EncodingListView.Items.Cast<EncoderListViewItem>().Where(item => item.Status == Status.Completed || item.Status == Status.FileDeletedOrMoved))
            {
                if (!File.Exists(item.OutputFilename))
                {
                    SetStatus(Status.FileDeletedOrMoved, item.Id);
                }
                else if (item.Status == Status.FileDeletedOrMoved)
                {
                    SetStatus(Status.Completed, item.Id, item.OutputPath);
                }
            }
        }

        private void ClearAll_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TaskList.Any(x => x.IsCompleted || x.IsCanceled || x.IsFaulted);
        }

        private void ClearAll_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var finishedTasks = TaskList.Where(x => x.IsCompleted || x.IsCanceled || x.IsFaulted).ToList();

            foreach (var task in finishedTasks)
            {
                var index = TaskList.IndexOf(task);
                TaskList.Remove(task);
                task.Dispose();

                CancellationTokenList[index].Dispose();
                CancellationTokenList.RemoveAt(index);

                var item = EncodingListView.Items.OfType<EncoderListViewItem>().FirstOrDefault(x => x.Id == task.Id);

                if (item != null)
                    EncodingListView.Items.Remove(item);
            }

            GC.Collect();
        }

        private void EncoderItem_CancelClicked(object sender, RoutedEventArgs args)
        {
            var item = sender as EncoderListViewItem;

            if (item == null) return;

            if (item.Status != Status.Encoding)
            {
                item.CancelClicked -= EncoderItem_CancelClicked;

                var index = EncodingListView.Items.IndexOf(item);

                TaskList.RemoveAt(index);
                CancellationTokenList.RemoveAt(index);
                EncodingListView.Items.RemoveAt(index);
            }
            else if (!item.TokenSource.IsCancellationRequested)
            {
                item.TokenSource.Cancel();
            }
        }

        private void EncodingListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
        }

        private void EncodingListView_MouseMove(object sender, MouseEventArgs e)
        {
            var diff = _dragStart - e.GetPosition(null);

            if (e.LeftButton != MouseButtonState.Pressed || !(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;

            if (EncodingListView.SelectedItems.Count == 0)
                return;

            var files = EncodingListView.SelectedItems.OfType<EncoderListViewItem>().Where(y => y.Status == Status.Completed && File.Exists(y.OutputFilename)).Select(x => x.OutputFilename).ToArray();

            if (!files.Any())
                return;

            DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, files), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? DragDropEffects.Copy : DragDropEffects.Move);
        }

        #endregion

        #region Private

        private void InternalAddItem(List<FrameInfo> listFrames, Parameters param)
        {
            //Creates the Cancellation Token
            var cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenList.Add(cancellationTokenSource);

            var context = TaskScheduler.FromCurrentSynchronizationContext();

            //Creates Task and send the Task Id.
            var a = -1;
            var task = new Task(() =>
            {
                Encode(listFrames, a, param, cancellationTokenSource);
            },
                CancellationTokenList.Last().Token, TaskCreationOptions.LongRunning);

            a = task.Id;

            #region Error Handling

            task.ContinueWith(t =>
            {
                var aggregateException = t.Exception;
                aggregateException?.Handle(exception => true);

                SetStatus(Status.Error, a, null, false, t.Exception);
                LogWriter.Log(t.Exception, "Encoding Error");
            },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, context);

            #endregion

            #region Creates List Item

            var encoderItem = new EncoderListViewItem
            {
                OutputType = param.Type == Export.Gif ? OutputType.Gif : OutputType.Video,
                Text = FindResource("Encoder.Starting").ToString(),
                FrameCount = listFrames.Count,
                Id = a,
                TokenSource = cancellationTokenSource,
                WillCopyToClipboard = param is GifParameters && ((GifParameters)param).SaveToClipboard,
            };

            encoderItem.CancelClicked += EncoderItem_CancelClicked;

            EncodingListView.Items.Add(encoderItem);
            EncodingListView.ScrollIntoView(encoderItem);

            #endregion

            try
            {
                TaskList.Add(task);
                TaskList.Last().Start();
            }
            catch (Exception ex)
            {
                Dialog.Ok("Task Error", "Unable to start the encoding task", "A generic error occured while trying to start the encoding task. " + ex.Message);
                LogWriter.Log(ex, "Errow while starting the task.");
            }
        }

        private void InternalUpdate(int id, int currentFrame, string status)
        {
            Dispatcher.Invoke(() =>
            {
                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item != null)
                {
                    item.CurrentFrame = currentFrame;
                    item.Text = status;
                }
            });
        }

        private void InternalUpdate(int id, int currentFrame)
        {
            Dispatcher.Invoke(() =>
            {
                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item != null)
                    item.CurrentFrame = currentFrame;
            });
        }

        private void InternalSetStatus(Status status, int id, string fileName, bool isIndeterminate = false, Exception exception = null)
        {
            Dispatcher.Invoke(() =>
            {
                CommandManager.InvalidateRequerySuggested();

                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null) return;

                item.Status = status;
                item.IsIndeterminate = isIndeterminate;

                GC.Collect(2);

                switch (status)
                {
                    case Status.Completed:
                        if (File.Exists(fileName))
                        {
                            var fileInfo = new FileInfo(fileName);
                            fileInfo.Refresh();

                            item.SizeInBytes = fileInfo.Length;
                            item.OutputFilename = fileName;
                        }
                        break;

                    case Status.Error:
                        item.Exception = exception;
                        break;
                }
            });
        }

        private void CopyToClipboard(string path)
        {
            if (!File.Exists(path))
                return;

            Dispatcher.Invoke(() =>
            {
                var data = new DataObject();
                data.SetImage(path.SourceFrom());
                data.SetText(path, TextDataFormat.Text);
                data.SetFileDropList(new StringCollection { path });

                Clipboard.SetDataObject(data, true);
            });
        }

        private void Encode(List<FrameInfo> listFrames, int id, Parameters param, CancellationTokenSource tokenSource)
        {
            var processing = FindResource("Encoder.Processing").ToString();

            try
            {
                switch (param.Type)
                {
                    case Export.Gif:

                        #region Gif

                        var gifParam = (GifParameters)param;

                        #region Cut/Paint Unchanged Pixels

                        if (gifParam.EncoderType == GifEncoderType.Legacy || gifParam.EncoderType == GifEncoderType.ScreenToGif)
                        {
                            if (gifParam.DetectUnchangedPixels)
                            {
                                Update(id, 0, FindResource("Encoder.Analyzing").ToString());

                                if (gifParam.DummyColor.HasValue)
                                {
                                    var color = Color.FromArgb(gifParam.DummyColor.Value.R, gifParam.DummyColor.Value.G, gifParam.DummyColor.Value.B);

                                    listFrames = ImageMethods.PaintTransparentAndCut(listFrames, color, id, tokenSource);
                                }
                                else
                                {
                                    listFrames = ImageMethods.CutUnchanged(listFrames, id, tokenSource);
                                }
                            }
                            else
                            {
                                var size = listFrames[0].Path.ScaledSize();
                                listFrames.ForEach(x => x.Rect = new Int32Rect(0, 0, (int)size.Width, (int)size.Height));
                            }
                        }

                        #endregion
                        
                        switch (gifParam.EncoderType)
                        {
                            case GifEncoderType.ScreenToGif:

                                #region Improved encoding

                                using (var stream = new MemoryStream())
                                {
                                    using (var encoder = new GifFile(stream, gifParam.RepeatCount))
                                    {
                                        encoder.UseGlobalColorTable = gifParam.UseGlobalColorTable;
                                        encoder.TransparentColor = gifParam.DummyColor;
                                        encoder.MaximumNumberColor = gifParam.MaximumNumberColors;

                                        for (var i = 0; i < listFrames.Count; i++)
                                        {
                                            if (!listFrames[i].HasArea && gifParam.DetectUnchangedPixels)
                                                continue;

                                            if (listFrames[i].Delay == 0)
                                                listFrames[i].Delay = 10;

                                            encoder.AddFrame(listFrames[i].Path, listFrames[i].Rect, listFrames[i].Delay);

                                            Update(id, i, string.Format(processing, i));

                                            #region Cancellation

                                            if (tokenSource.Token.IsCancellationRequested)
                                            {
                                                SetStatus(Status.Canceled, id);
                                                break;
                                            }

                                            #endregion
                                        }
                                    }

                                    try
                                    {
                                        using (var fileStream = new FileStream(gifParam.Filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                        {
                                            stream.WriteTo(fileStream);
                                        }

                                        if (gifParam.SaveToClipboard)
                                            CopyToClipboard(gifParam.Filename);
                                    }
                                    catch (Exception ex)
                                    {
                                        SetStatus(Status.Error, id);
                                        LogWriter.Log(ex, "Improved Encoding");
                                    }
                                }

                                #endregion

                                break;
                            case GifEncoderType.Legacy:

                                #region Legacy Encoding

                                using (var encoder = new AnimatedGifEncoder())
                                {
                                    if (gifParam.DummyColor.HasValue)
                                    {
                                        var color = Color.FromArgb(gifParam.DummyColor.Value.R,
                                            gifParam.DummyColor.Value.G, gifParam.DummyColor.Value.B);

                                        encoder.SetTransparent(color);
                                        encoder.SetDispose(1); //Undraw Method, "Leave".
                                    }

                                    encoder.Start(gifParam.Filename);
                                    encoder.SetQuality(gifParam.Quality);
                                    encoder.SetRepeat(gifParam.RepeatCount);

                                    var numImage = 0;
                                    foreach (var frame in listFrames)
                                    {
                                        #region Cancellation

                                        if (tokenSource.Token.IsCancellationRequested)
                                        {
                                            SetStatus(Status.Canceled, id);
                                            break;
                                        }

                                        #endregion

                                        if (!frame.HasArea && gifParam.DetectUnchangedPixels)
                                            continue;

                                        var bitmapAux = new Bitmap(frame.Path);

                                        encoder.SetDelay(frame.Delay);
                                        encoder.AddFrame(bitmapAux, frame.Rect.X, frame.Rect.Y);

                                        bitmapAux.Dispose();

                                        Update(id, numImage, string.Format(processing, numImage));
                                        numImage++;
                                    }
                                }

                                if (gifParam.SaveToClipboard)
                                    CopyToClipboard(gifParam.Filename);

                                #endregion

                                break;
                            case GifEncoderType.PaintNet:

                                #region paint.NET encoding

                                using (var stream = new MemoryStream())
                                {
                                    using (var encoder = new GifEncoder(stream, null, null, gifParam.RepeatCount))
                                    {
                                        for (var i = 0; i < listFrames.Count; i++)
                                        {
                                            var bitmapAux = new Bitmap(listFrames[i].Path);
                                            encoder.AddFrame(bitmapAux, 0, 0, TimeSpan.FromMilliseconds(listFrames[i].Delay));
                                            bitmapAux.Dispose();

                                            Update(id, i, string.Format(processing, i));

                                            #region Cancellation

                                            if (tokenSource.Token.IsCancellationRequested)
                                            {
                                                SetStatus(Status.Canceled, id);

                                                break;
                                            }

                                            #endregion
                                        }
                                    }

                                    stream.Position = 0;

                                    try
                                    {
                                        using (var fileStream = new FileStream(gifParam.Filename, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize, false))
                                        {
                                            stream.WriteTo(fileStream);
                                        }

                                        if (gifParam.SaveToClipboard)
                                            CopyToClipboard(gifParam.Filename);
                                    }
                                    catch (Exception ex)
                                    {
                                        SetStatus(Status.Error, id);
                                        LogWriter.Log(ex, "Encoding with paint.Net.");
                                    }
                                }

                                #endregion

                                break;
                            default:
                                throw new Exception("Undefined Gif encoder type");
                        }

                        #endregion

                        break;
                    case Export.Video:

                        #region Video

                        var videoParam = (VideoParameters)param;

                        switch (videoParam.VideoEncoder)
                        {
                            case VideoEncoderType.AviStandalone:

                                #region Avi Standalone

                                var image = listFrames[0].Path.SourceFrom();

                                if (File.Exists(videoParam.Filename))
                                    File.Delete(videoParam.Filename);

                                //1000 / listFrames[0].Delay
                                using (var aviWriter = new AviWriter(videoParam.Filename, videoParam.Framerate, image.PixelWidth, image.PixelHeight, videoParam.Quality))
                                {
                                    var numImage = 0;
                                    foreach (var frame in listFrames)
                                    {
                                        using (var outStream = new MemoryStream())
                                        {
                                            var bitImage = frame.Path.SourceFrom();

                                            var enc = new BmpBitmapEncoder();
                                            enc.Frames.Add(BitmapFrame.Create(bitImage));
                                            enc.Save(outStream);

                                            outStream.Flush();

                                            using (var bitmap = new Bitmap(outStream))
                                                aviWriter.AddFrame(bitmap, videoParam.FlipVideo);
                                        }

                                        //aviWriter.AddFrame(new BitmapImage(new Uri(frame.Path)));

                                        Update(id, numImage, string.Format(processing, numImage));
                                        numImage++;

                                        #region Cancellation

                                        if (tokenSource.Token.IsCancellationRequested)
                                        {
                                            SetStatus(Status.Canceled, id);
                                            break;
                                        }

                                        #endregion
                                    }
                                }

                                #endregion

                                break;
                            case VideoEncoderType.Ffmpg:

                                #region Video using FFmpeg

                                SetStatus(Status.Encoding, id, null, true);

                                if (!Util.Other.IsFfmpegPresent())
                                    throw new ApplicationException("FFmpeg not present.");

                                if (File.Exists(videoParam.Filename))
                                    File.Delete(videoParam.Filename);

                                #region Generate concat

                                var concat = new StringBuilder();
                                foreach (var frame in listFrames)
                                {
                                    concat.AppendLine("file '" + frame.Path + "'");
                                    concat.AppendLine("duration " + (frame.Delay / 1000d).ToString(CultureInfo.InvariantCulture));
                                }

                                var concatPath = Path.GetDirectoryName(listFrames[0].Path) ?? Path.GetTempPath();
                                var concatFile = Path.Combine(concatPath, "concat.txt");

                                if (!Directory.Exists(concatPath))
                                    Directory.CreateDirectory(concatPath);

                                if (File.Exists(concatFile))
                                    File.Delete(concatFile);

                                File.WriteAllText(concatFile, concat.ToString());

                                #endregion

                                videoParam.Command = string.Format(videoParam.Command, concatFile, videoParam.ExtraParameters.Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()), param.Filename);

                                var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
                                {
                                    Arguments = videoParam.Command,
                                    CreateNoWindow = true,
                                    ErrorDialog = false,
                                    UseShellExecute = false,
                                    RedirectStandardError = true
                                };

                                var pro = Process.Start(process);

                                var str = pro.StandardError.ReadToEnd();

                                var fileInfo = new FileInfo(param.Filename);

                                if (!fileInfo.Exists || fileInfo.Length == 0)
                                    throw new Exception("Error while encoding with FFmpeg.") { HelpLink = str };

                                #endregion

                                break;
                            default:
                                throw new Exception("Undefined video encoder");
                        }

                        #endregion

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(param));
                }

                if (!tokenSource.Token.IsCancellationRequested)
                    SetStatus(Status.Completed, id, param.Filename);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Encode");

                SetStatus(Status.Error, id, null, false, ex);
            }
            finally
            {
                #region Delete Encoder Folder

                try
                {
                    var encoderFolder = Path.GetDirectoryName(listFrames[0].Path);

                    if (!string.IsNullOrEmpty(encoderFolder))
                        if (Directory.Exists(encoderFolder))
                            Directory.Delete(encoderFolder, true);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Cleaning the Encode folder");
                }

                #endregion

                GC.Collect();
            }
        }

        #endregion

        #region Public Static

        /// <summary>
        /// Shows the Encoder window.
        /// </summary>
        /// <param name="scale">Screen scale.</param>
        public static void Start(double scale)
        {
            #region If already started

            if (_encoder != null)
            {
                if (_encoder.WindowState == WindowState.Minimized)
                    Restore();

                return;
            }

            #endregion

            _encoder = new Encoder();

            var screen = ScreenHelper.GetScreen(_encoder);

            //Lower Right corner.
            _encoder.Left = screen.WorkingArea.Width / scale - _encoder.Width;
            _encoder.Top = screen.WorkingArea.Height / scale - _encoder.Height;

            _encoder.Show();
        }

        /// <summary>
        /// Add one list of frames to the encoding batch.
        /// </summary>
        /// <param name="listFrames">The list of frames to be encoded.</param>
        /// <param name="param">Encoding parameters.</param>
        /// <param name="scale">Screen scale.</param>
        public static void AddItem(List<FrameInfo> listFrames, Parameters param, double scale)
        {
            //Show or restore the encoder window.
            if (_encoder == null)
                Start(scale);
            else if (_encoder.WindowState == WindowState.Minimized)
                _encoder.WindowState = WindowState.Normal;

            if (_encoder == null)
                throw new ApplicationException("Error while starting the Encoding window.");

            _encoder.Activate();
            _encoder.InternalAddItem(listFrames, param);
        }

        /// <summary>
        /// Minimizes the Encoder window.
        /// </summary>
        public static void Minimize()
        {
            if (_encoder == null)
                return;

            _lastState = _encoder.WindowState;

            _encoder.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Minimizes the Encoder window.
        /// </summary>
        public static void Restore()
        {
            if (_encoder == null)
                return;

            _encoder.WindowState = _lastState;
        }

        /// <summary>
        /// Updates a specific item of the list.
        /// </summary>
        /// <param name="id">The unique ID of the item.</param>
        /// <param name="currentFrame">The current frame being processed.</param>
        /// <param name="status">Status description.</param>
        public static void Update(int id, int currentFrame, string status)
        {
            _encoder?.InternalUpdate(id, currentFrame, status);
        }

        /// <summary>
        /// Updates a specific item of the list.
        /// </summary>
        /// <param name="id">The unique ID of the item.</param>
        /// <param name="currentFrame">The current frame being processed.</param>
        public static void Update(int id, int currentFrame)
        {
            _encoder?.InternalUpdate(id, currentFrame);
        }

        /// <summary>
        /// Sets the Status of the encoding of a current item.
        /// </summary>
        /// <param name="status">The current status.</param>
        /// <param name="id">The unique ID of the item.</param>
        /// <param name="fileName">The name of the output file.</param>
        /// <param name="isIndeterminate">The state of the progress bar.</param>
        /// <param name="exception">The exception details of the error.</param>
        public static void SetStatus(Status status, int id, string fileName = null, bool isIndeterminate = false, Exception exception = null)
        {
            _encoder?.InternalSetStatus(status, id, fileName, isIndeterminate, exception);
        }

        /// <summary>
        /// Tries to close the Window if there's no enconding active.
        /// </summary>
        public static void TryClose()
        {
            if (_encoder == null)
                return;

            if (_encoder.EncodingListView.Items.Cast<EncoderListViewItem>().All(x => x.Status != Status.Encoding))
                _encoder.Close();
        }

        #endregion
    }
}
