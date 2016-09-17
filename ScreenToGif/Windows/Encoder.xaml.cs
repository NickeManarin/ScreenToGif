using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenToGif.Controls;
using ScreenToGif.FileWriters;
using ScreenToGif.ImageUtil;
using ScreenToGif.ImageUtil.Encoder;
using ScreenToGif.ImageUtil.LegacyEncoder;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using ScreenToGif.Util.Parameters;
using ScreenToGif.Windows.Other;

namespace ScreenToGif.Windows
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

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Encoder()
        {
            InitializeComponent();
        }

        #region Events

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //TODO: Check for memmory leaks.

            foreach (CancellationTokenSource tokenSource in CancellationTokenList)
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

        private void EncoderItem_CloseButtonClickedEvent(object sender)
        {
            var item = sender as EncoderListViewItem;
            if (item == null) return;

            if (item.Status != Status.Encoding)
            {
                item.CloseButtonClickedEvent -= EncoderItem_CloseButtonClickedEvent;

                int index = EncodingListView.Items.IndexOf(item);

                TaskList.RemoveAt(index);
                CancellationTokenList.RemoveAt(index);
                EncodingListView.Items.RemoveAt(index);
            }
            else if (!item.TokenSource.IsCancellationRequested)
            {
                item.TokenSource.Cancel();
            }
        }

        private void EncoderItem_LabelLinkClickedEvent(object name)
        {
            var fileName = name as string;

            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                Process.Start(fileName);
        }

        private void EncoderItem_PathButtonClickedEvent(object name)
        {
            var path = name as string;

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                Process.Start(path);
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var finishedTasks = TaskList.Where(x => x.IsCompleted || x.IsCanceled || x.IsFaulted).ToList();

            foreach (Task task in finishedTasks)
            {
                int index = TaskList.IndexOf(task);
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

                SetStatus(Status.Error, a);

                LogWriter.Log(t.Exception, "Encoding Error");
            },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, context);

            #endregion

            #region Creates List Item

            var encoderItem = new EncoderListViewItem
            {
                Image = param.Type == Export.Gif ? (UIElement)FindResource("Vector.Image") : (UIElement)FindResource("Vector.Video"),
                Text = FindResource("Encoder.Starting").ToString(),
                FrameCount = listFrames.Count,
                Id = a,
                TokenSource = cancellationTokenSource,
            };

            encoderItem.CloseButtonClickedEvent += EncoderItem_CloseButtonClickedEvent;
            encoderItem.LabelLinkClickedEvent += EncoderItem_LabelLinkClickedEvent;
            encoderItem.PathClickedEvent += EncoderItem_PathButtonClickedEvent;

            EncodingListView.Items.Add(encoderItem);

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
                {
                    item.CurrentFrame = currentFrame;
                }
            });
        }

        private void InternalSetStatus(Status status, int id, string fileName, bool isIndeterminate, string reason)
        {
            Dispatcher.Invoke(() =>
            {
                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null) return;

                item.Status = status;
                item.IsIndeterminate = isIndeterminate;

                GC.Collect(2);

                switch (status)
                {
                    case Status.Completed:
                        item.Image = (UIElement)FindResource("Vector.Success");

                        if (File.Exists(fileName))
                        {
                            var fileInfo = new FileInfo(fileName);
                            fileInfo.Refresh();

                            item.SizeInBytes = fileInfo.Length;
                            item.OutputFilename = fileName;
                            item.Text = FindResource("Encoder.Completed").ToString();
                        }
                        break;
                    case Status.FileDeletedOrMoved:
                        item.Image = (UIElement)FindResource("Vector.FilePermission");
                        item.Text = FindResource("Encoder.FileDeletedMoved").ToString();
                        break;
                    case Status.Canceled:
                        item.Text = FindResource("Encoder.Canceled").ToString();
                        break;
                    case Status.Error:
                        item.Image = (UIElement)FindResource("Vector.Error");
                        item.Text = FindResource("Encoder.Error").ToString();
                        item.Reason = reason;
                        break;
                }
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

                        if (gifParam.DetectUnchangedPixels && (gifParam.EncoderType == GifEncoderType.Legacy || gifParam.EncoderType == GifEncoderType.ScreenToGif))
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
                                            if (!listFrames[i].HasArea)
                                                continue;

                                            if (listFrames[i].Delay == 0)
                                                listFrames[i].Delay = 10;

                                            encoder.AddFrame(listFrames[i].ImageLocation, listFrames[i].Rect, listFrames[i].Delay);

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

                                        var bitmapAux = new Bitmap(frame.ImageLocation);

                                        encoder.SetDelay(frame.Delay);
                                        encoder.AddFrame(bitmapAux, frame.Rect.X, frame.Rect.Y);

                                        bitmapAux.Dispose();

                                        Update(id, numImage, string.Format(processing, numImage));
                                        numImage++;
                                    }
                                }

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
                                            var bitmapAux = new Bitmap(listFrames[i].ImageLocation);
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

                                var image = listFrames[0].ImageLocation.SourceFrom();

                                using (var aviWriter = new AviWriter(videoParam.Filename, 1000 / listFrames[0].Delay, image.PixelWidth, image.PixelHeight, videoParam.Quality))
                                {
                                    var numImage = 0;
                                    foreach (var frame in listFrames)
                                    {
                                        using (var outStream = new MemoryStream())
                                        {
                                            var bitImage = frame.ImageLocation.SourceFrom();

                                            var enc = new BmpBitmapEncoder();
                                            enc.Frames.Add(BitmapFrame.Create(bitImage));
                                            enc.Save(outStream);

                                            outStream.Flush();

                                            using (var bitmap = new Bitmap(outStream))
                                            {
                                                aviWriter.AddFrame(bitmap);
                                            }
                                        }

                                        //aviWriter.AddFrame(new BitmapImage(new Uri(frame.ImageLocation)));

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
                                {
                                    throw new ApplicationException("FFmpeg not present.");
                                }

                                videoParam.Command = string.Format(videoParam.Command,
                                    Path.Combine(Path.GetDirectoryName(listFrames[0].ImageLocation), "%d.png"),
                                    videoParam.ExtraParameters, videoParam.Framerate,
                                    param.Filename);

                                var process = new ProcessStartInfo(Settings.Default.FfmpegLocation)
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
                                    throw new Exception("Error while encoding with FFmpeg.", new Exception(str));

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

                SetStatus(Status.Error, id, null, false, ex.Message);
            }
            finally
            {
                #region Delete Encoder Folder

                try
                {
                    var encoderFolder = Path.GetDirectoryName(listFrames[0].ImageLocation);

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

        #region Private Static

        private static WindowState _lastState = WindowState.Normal;

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
                {
                    Restore();
                }

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
            if (_encoder == null)
                Start(scale);

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
        /// <param name="reason">The reason of the error.</param>
        public static void SetStatus(Status status, int id, string fileName = null, bool isIndeterminate = false, string reason = null)
        {
            _encoder?.InternalSetStatus(status, id, fileName, isIndeterminate, reason);
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
