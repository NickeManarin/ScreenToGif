using ScreenToGif.Cloud;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.ImageUtil.Apng;
using ScreenToGif.ImageUtil.Gif.Encoder;
using ScreenToGif.ImageUtil.Gif.LegacyEncoder;
using ScreenToGif.ImageUtil.Video;
using ScreenToGif.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ScreenToGif.ImageUtil.Psd;
using ScreenToGif.Model;
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

            if (item.Status != Status.Processing)
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

            }, CancellationTokenList.Last().Token, TaskCreationOptions.LongRunning);

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
                OutputType = param.Type,
                Text = FindResource("Encoder.Starting").ToString(),
                FrameCount = listFrames.Count,
                Id = a,
                TokenSource = cancellationTokenSource
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

        private void InternalUpdate(int id, string text, bool isIndeterminate = false, bool findText = false)
        {
            Dispatcher.Invoke(() =>
            {
                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null)
                    return;

                item.Text = !findText ? text : FindResource(text).ToString();
                item.IsIndeterminate = isIndeterminate;
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

        private void InternalSetUpload(int id, bool uploaded, string link, string deleteLink = null, Exception exception = null)
        {
            Dispatcher.Invoke(() =>
            {
                CommandManager.InvalidateRequerySuggested();

                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null)
                    return;

                item.Uploaded = uploaded;
                item.UploadLink = link;
                item.UploadLinkDisplay = !string.IsNullOrWhiteSpace(link) ? link.Replace("https:/", "").Replace("http:/", "").Trim('/') : link;
                item.DeletionLink = deleteLink;
                item.UploadTaskException = exception;

                GC.Collect();
            });
        }

        private string InternalGetUpload(int id)
        {
            return Dispatcher.Invoke(() =>
            {
                CommandManager.InvalidateRequerySuggested();

                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null)
                    return "";

                return item.UploadLink;
            });
        }

        private void InternalSetCopy(int id, bool copied, Exception exception = null)
        {
            Dispatcher.Invoke(() =>
            {
                CommandManager.InvalidateRequerySuggested();

                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null)
                    return;

                item.CopiedToClipboard = copied;
                item.CopyTaskException = exception;

                GC.Collect();
            });
        }

        private void InternalSetCommand(int id, bool executed, string command, string output, Exception exception = null)
        {
            Dispatcher.Invoke(() =>
            {
                CommandManager.InvalidateRequerySuggested();

                var item = EncodingListView.Items.Cast<EncoderListViewItem>().FirstOrDefault(x => x.Id == id);

                if (item == null)
                    return;

                item.CommandExecuted = executed;
                item.Command = command;
                item.CommandOutput = output;
                item.CommandTaskException = exception;

                GC.Collect();
            });
        }


        private async void Encode(List<FrameInfo> listFrames, int id, Parameters param, CancellationTokenSource tokenSource)
        {
            var processing = this.DispatcherStringResource("Encoder.Processing");

            try
            {
                switch (param.Type)
                {
                    case Export.Gif:

                        #region Gif

                        #region Cut/Paint Unchanged Pixels

                        if (param.EncoderType == GifEncoderType.Legacy || param.EncoderType == GifEncoderType.ScreenToGif)
                        {
                            if (param.DetectUnchangedPixels)
                            {
                                Update(id, 0, FindResource("Encoder.Analyzing").ToString());

                                if (param.DummyColor.HasValue)
                                {
                                    var color = Color.FromArgb(param.DummyColor.Value.R, param.DummyColor.Value.G, param.DummyColor.Value.B);

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

                        switch (param.EncoderType)
                        {
                            case GifEncoderType.ScreenToGif:

                                #region Improved encoding

                                using (var stream = new MemoryStream())
                                {
                                    using (var encoder = new GifFile(stream, param.RepeatCount))
                                    {
                                        encoder.UseGlobalColorTable = param.UseGlobalColorTable;
                                        encoder.TransparentColor = param.DummyColor;
                                        encoder.MaximumNumberColor = param.MaximumNumberColors;

                                        for (var i = 0; i < listFrames.Count; i++)
                                        {
                                            if (!listFrames[i].HasArea && param.DetectUnchangedPixels)
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
                                        using (var fileStream = new FileStream(param.Filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                            stream.WriteTo(fileStream);
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
                                    if (param.DummyColor.HasValue)
                                    {
                                        var color = Color.FromArgb(param.DummyColor.Value.R,
                                            param.DummyColor.Value.G, param.DummyColor.Value.B);

                                        encoder.SetTransparent(color);
                                        encoder.SetDispose(1); //Undraw Method, "Leave".
                                    }

                                    encoder.Start(param.Filename);
                                    encoder.SetQuality(param.Quality);
                                    encoder.SetRepeat(param.RepeatCount);

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

                                        if (!frame.HasArea && param.DetectUnchangedPixels)
                                            continue;

                                        var bitmapAux = new Bitmap(frame.Path);

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
                                    using (var encoder = new GifEncoder(stream, null, null, param.RepeatCount))
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
                                        using (var fileStream = new FileStream(param.Filename, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize, false))
                                            stream.WriteTo(fileStream);
                                    }
                                    catch (Exception ex)
                                    {
                                        SetStatus(Status.Error, id);
                                        LogWriter.Log(ex, "Encoding with paint.Net.");
                                    }
                                }

                                #endregion

                                break;
                            case GifEncoderType.FFmpeg:

                                #region FFmpeg encoding

                                SetStatus(Status.Processing, id, null, true);

                                if (!Util.Other.IsFfmpegPresent())
                                    throw new ApplicationException("FFmpeg not present.");

                                if (File.Exists(param.Filename))
                                    File.Delete(param.Filename);

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

                                param.Command = string.Format(param.Command, concatFile, param.ExtraParameters.Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()), param.Filename);

                                var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
                                {
                                    Arguments = param.Command,
                                    CreateNoWindow = true,
                                    ErrorDialog = false,
                                    UseShellExecute = false,
                                    RedirectStandardError = true
                                };

                                var pro = Process.Start(process);

                                var str = pro.StandardError.ReadToEnd();

                                var fileInfo = new FileInfo(param.Filename);

                                if (!fileInfo.Exists || fileInfo.Length == 0)
                                    throw new Exception("Error while encoding the gif with FFmpeg.") { HelpLink = $"Command:\n\r{param.Command}\n\rResult:\n\r{str}" };

                                #endregion

                                break;
                            case GifEncoderType.Gifski:

                                #region Gifski encoding

                                SetStatus(Status.Processing, id, null, true);

                                if (!Util.Other.IsGifskiPresent())
                                    throw new ApplicationException("Gifski not present.");

                                if (File.Exists(param.Filename))
                                    File.Delete(param.Filename);
                                
                                var gifski = new GifskiInterop();
                                var handle = gifski.Start(UserSettings.All.GifskiQuality, UserSettings.All.Looped);

                                ThreadPool.QueueUserWorkItem(delegate 
                                {
                                    Thread.Sleep(500);
                                    SetStatus(Status.Processing, id, null, false);

                                    for (var i = 0; i < listFrames.Count; i++)
                                    {
                                        Update(id, i, string.Format(processing, i));
                                        gifski.AddFrame(handle, (uint)i, listFrames[i].Path, listFrames[i].Delay);
                                    }

                                    gifski.EndAdding(handle);
                                }, null);

                                gifski.End(handle, param.Filename);

                                var fileInfo2 = new FileInfo(param.Filename);

                                if (!fileInfo2.Exists || fileInfo2.Length == 0)
                                    throw new Exception("Error while encoding the gif with Gifski.", new Win32Exception()) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };

                                #endregion

                                break;
                            default:
                                throw new Exception("Undefined Gif encoder type");
                        }

                        #endregion

                        break;
                    case Export.Apng:

                        #region Apng

                        #region Cut/Paint Unchanged Pixels

                        if (param.DetectUnchangedPixels)
                        {
                            Update(id, 0, FindResource("Encoder.Analyzing").ToString());

                            if (param.DummyColor.HasValue)
                            {
                                var color = Color.FromArgb(param.DummyColor.Value.A, param.DummyColor.Value.R, param.DummyColor.Value.G, param.DummyColor.Value.B);
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

                        #endregion

                        #region Encoding

                        using (var stream = new MemoryStream())
                        {
                            var frameCount = listFrames.Count(x => x.HasArea);

                            using (var encoder = new Apng(stream, frameCount, param.RepeatCount))
                            {
                                for (var i = 0; i < listFrames.Count; i++)
                                {
                                    if (!listFrames[i].HasArea && param.DetectUnchangedPixels)
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
                                using (var fileStream = new FileStream(param.Filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                    stream.WriteTo(fileStream);
                            }
                            catch (Exception ex)
                            {
                                SetStatus(Status.Error, id);
                                LogWriter.Log(ex, "Apng Encoding");
                            }
                        }

                        #endregion

                        #endregion

                        break;
                    case Export.Photoshop:

                        #region Psd

                        using (var stream = new MemoryStream())
                        {
                            //var frameCount = listFrames.Count(x => x.HasArea);

                            using (var encoder = new Psd(stream, param.RepeatCount, param.Height, param.Width))
                            {
                                for (var i = 0; i < listFrames.Count; i++)
                                {
                                    if (listFrames[i].Delay == 0)
                                        listFrames[i].Delay = 10;

                                    encoder.AddFrame(listFrames[i].Path, listFrames[i].Delay);

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
                                using (var fileStream = new FileStream(param.Filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                    stream.WriteTo(fileStream);
                            }
                            catch (Exception ex)
                            {
                                SetStatus(Status.Error, id);
                                LogWriter.Log(ex, "Psd Encoding");
                            }
                        }

                        #endregion

                        break;
                    case Export.Video:

                        #region Video

                        switch (param.VideoEncoder)
                        {
                            case VideoEncoderType.AviStandalone:

                                #region Avi Standalone

                                var image = listFrames[0].Path.SourceFrom();

                                if (File.Exists(param.Filename))
                                    File.Delete(param.Filename);

                                //1000 / listFrames[0].Delay
                                using (var aviWriter = new AviWriter(param.Filename, param.Framerate, image.PixelWidth, image.PixelHeight, param.VideoQuality))
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
                                                aviWriter.AddFrame(bitmap, param.FlipVideo);
                                        }

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

                                SetStatus(Status.Processing, id, null, true);

                                if (!Util.Other.IsFfmpegPresent())
                                    throw new ApplicationException("FFmpeg not present.");

                                if (File.Exists(param.Filename))
                                    File.Delete(param.Filename);

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

                                param.Command = string.Format(param.Command, concatFile, param.ExtraParameters.Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()), param.Filename);

                                var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
                                {
                                    Arguments = param.Command,
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

                //If it was canceled, try deleting the file.
                if (tokenSource.Token.IsCancellationRequested)
                {
                    if (File.Exists(param.Filename))
                        File.Delete(param.Filename);

                    SetStatus(Status.Canceled, id);
                    return;
                }

                #region Upload

                if (param.Upload && File.Exists(param.Filename))
                {
                    InternalUpdate(id, "Encoder.Uploading", true, true);

                    try
                    {
                        var cloud = CloudFactory.CreateCloud(param.UploadDestination);

                        var uploadedFile = await cloud.UploadFileAsync(param.Filename, CancellationToken.None);

                        InternalSetUpload(id, true, uploadedFile.Link, uploadedFile.DeleteLink);
                    }
                    catch (Exception e)
                    {
                        LogWriter.Log(e, "It was not possible to upload.");
                        InternalSetUpload(id, false, null, null, e);
                    }
                }

                #endregion

                #region Copy to clipboard

                if (param.CopyToClipboard && File.Exists(param.Filename))
                {
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var data = new DataObject();

                            switch (param.CopyType)
                            {
                                case CopyType.File:
                                    if (param.Type != Export.Video)
                                        data.SetImage(param.Filename.SourceFrom());

                                    data.SetText(param.Filename, TextDataFormat.Text);
                                    data.SetFileDropList(new StringCollection { param.Filename });
                                    break;
                                case CopyType.FolderPath:
                                    data.SetText(Path.GetDirectoryName(param.Filename) ?? param.Filename, TextDataFormat.Text);
                                    break;
                                case CopyType.Link:
                                    var link = InternalGetUpload(id);

                                    data.SetText(string.IsNullOrEmpty(link) ? param.Filename : link, TextDataFormat.Text);
                                    break;
                                default:
                                    data.SetText(param.Filename, TextDataFormat.Text);
                                    break;
                            }

                            //It tries to set the data to the clipboard 10 times before failing it to do so.
                            //This issue may happen if the clipboard is opened by any clipboard manager.
                            for (var i = 0; i < 10; i++)
                            {
                                try
                                {
                                    Clipboard.SetDataObject(data, true);
                                    break;
                                }
                                catch (COMException ex)
                                {
                                    if ((uint)ex.ErrorCode != 0x800401D0) //CLIPBRD_E_CANT_OPEN
                                        throw;
                                }

                                Thread.Sleep(100);
                            }
                            
                            InternalSetCopy(id, true);
                        }
                        catch (Exception e)
                        {
                            LogWriter.Log(e, "It was not possible to copy the file.");
                            InternalSetCopy(id, false, e);
                        }
                    });
                }

                #endregion

                #region Execute commands

                if (param.ExecuteCommands && !string.IsNullOrWhiteSpace(param.PostCommands))
                {
                    InternalUpdate(id, "Encoder.Executing", true, true);

                    var command = param.PostCommands.Replace("{p}", "\"" + param.Filename + "\"").Replace("{f}", "\"" + Path.GetDirectoryName(param.Filename) + "\"");
                    var output = "";

                    try
                    {
                        foreach (var com in command.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var procStartInfo = new ProcessStartInfo("cmd", "/c " + com)
                            {
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (var process = new Process())
                            {
                                process.StartInfo = procStartInfo;
                                process.Start();

                                var message = process.StandardOutput.ReadToEnd();
                                var error = process.StandardError.ReadToEnd();

                                if (!string.IsNullOrWhiteSpace(message))
                                    output += message + Environment.NewLine;

                                if (!string.IsNullOrWhiteSpace(message))
                                    output += message + Environment.NewLine;

                                if (!string.IsNullOrWhiteSpace(error))
                                    throw new Exception(error);

                                process.WaitForExit(1000);
                            }
                        }

                        InternalSetCommand(id, true, command, output);
                    }
                    catch (Exception e)
                    {
                        LogWriter.Log(e, "It was not possible to run the post encoding command.");
                        InternalSetCommand(id, false, command, output, e);
                    }
                }

                #endregion

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

            if (_encoder.EncodingListView.Items.Cast<EncoderListViewItem>().All(x => x.Status != Status.Processing))
                _encoder.Close();
        }

        #endregion
    }
}