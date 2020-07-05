using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenToGif.Cloud;
using ScreenToGif.Controls;
using ScreenToGif.ImageUtil;
using ScreenToGif.ImageUtil.Apng;
using ScreenToGif.ImageUtil.Gif.Encoder;
using ScreenToGif.ImageUtil.Gif.LegacyEncoder;
using ScreenToGif.ImageUtil.Psd;
using ScreenToGif.ImageUtil.Video;
using ScreenToGif.Model;
using ScreenToGif.Windows.Other;
using Encoder = ScreenToGif.Windows.Other.Encoder;

namespace ScreenToGif.Util
{
    internal class EncodingManager
    {
        #region Variables

        public static List<EncodingItem> Encodings { get; set; } = new List<EncodingItem>();
        
        /// <summary>
        /// List of CancellationTokenSource, used to cancel each task.
        /// </summary>
        private static readonly List<CancellationTokenSource> CancellationTokenList = new List<CancellationTokenSource>();

        /// <summary>
        /// List of Tasks, each task executes the encoding process for one recording.
        /// </summary>
        private static readonly List<Task> TaskList = new List<Task>();

        /// <summary>
        /// List of encoding views, used to update the data without lagging the UI.
        /// </summary>
        internal static readonly List<EncoderListViewItem> ViewList = new List<EncoderListViewItem>();

        #endregion

        internal static void StartEncoding(ExportProject project, Parameters param, double scale)
        {
            //If the user still wants an encoder window, here's when it should be opened.
            if (UserSettings.All.DisplayEncoder)
                Application.Current.Dispatcher?.BeginInvoke(new Action(() => Encoder.Start(scale)));

            //Creates the Cancellation Token
            var cancellationTokenSource = new CancellationTokenSource();
            CancellationTokenList.Add(cancellationTokenSource);

            var context = Application.Current.Dispatcher?.Invoke(TaskScheduler.FromCurrentSynchronizationContext);

            //Creates Task and send the Task Id.
            var taskId = -1;
            var task = new Task(async () =>
            {
                //ReSharper disable once AccessToModifiedClosure
                await Encode(project, taskId, param, cancellationTokenSource);

            }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            taskId = task.Id;

            #region Error handling

            task.ContinueWith(t =>
            {
                var aggregateException = t.Exception;
                aggregateException?.Handle(exception => true);

                Update(taskId, Status.Error, null, false, t.Exception);
                LogWriter.Log(t.Exception, "Encoding error.");

            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, context);

            #endregion

            //Adds the encoding to the list.
            Encodings.Add(new EncodingItem
            {
                Id = taskId,
                OutputType = param.Type,
                Text = LocalizationHelper.Get("S.Encoder.Starting"),
                FrameCount = project.FrameCount,
                TokenSource = cancellationTokenSource
            });

            try
            {
                TaskList.Add(task);
                TaskList.Last().Start();
            }
            catch (Exception ex)
            {
                ErrorDialog.Ok("Task Error", "Unable to start the encoding task", "A generic error occured while trying to start the encoding task.", ex);
                LogWriter.Log(ex, "Errow while starting the task.");
            }

            //Application.Current.Dispatcher.Invoke(() => Refresh(taskId));
            Application.Current.Dispatcher?.BeginInvoke(new Action(() => EncodingAdded(taskId)));
        }

        #region Encoding manipulation

        internal static void Update(int id, int currentFrame, string text)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            item.CurrentFrame = currentFrame;
            item.Text = text;
            item.IsIndeterminate = false;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }

        internal static void Update(int id, int currentFrame)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            item.CurrentFrame = currentFrame;
            
            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }

        internal static void Update(int id, int type, TimeSpan elapsed)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            switch (type)
            {
                case 0: //Analysis.
                    item.TimeToAnalyze = elapsed;
                    break;
                case 1: //Encoding.
                    item.TimeToEncode = elapsed;
                    break;
                case 2: //Upload.
                    item.TimeToUpload = elapsed;
                    break;
                case 3: //Copy.
                    item.TimeToCopy = elapsed;
                    break;
                case 4: //Execute commands.
                    item.TimeToExecute = elapsed;
                    break;
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }

        internal static void Update(int id, string text, bool isIndeterminate = false, bool findText = false)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            item.Text = !findText ? text : LocalizationHelper.Get(text);
            item.IsIndeterminate = isIndeterminate;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }

        /// <summary>
        /// Updates the status of the encoding of a current item.
        /// </summary>
        /// <param name="status">The current status.</param>
        /// <param name="id">The unique ID of the item.</param>
        /// <param name="fileName">The name of the output file.</param>
        /// <param name="isIndeterminate">The state of the progress bar.</param>
        /// <param name="exception">The exception details of the error.</param>
        internal static void Update(int id, Status status, string fileName = null, bool isIndeterminate = false, Exception exception = null)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            var wasStatusUpdated = item.Status != status;

            item.Status = status;
            item.IsIndeterminate = isIndeterminate;

            switch (status)
            {
                case Status.Completed:
                {
                    if (fileName != null)
                        if (File.Exists(fileName))
                        {
                            var fileInfo = new FileInfo(fileName);
                            fileInfo.Refresh();

                            item.SizeInBytes = fileInfo.Length;
                            item.OutputFilename = fileName;
                            item.SavedToDisk = true;
                        }
                    break;
                }
                case Status.Error:
                {    
                    item.Exception = exception;
                    break;
                }
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item, wasStatusUpdated)));
        }
        

        private static void SetUpload(int id, bool uploaded, string link, string deleteLink = null, Exception exception = null)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            item.Uploaded = uploaded;
            item.UploadLink = link;
            item.UploadLinkDisplay = !string.IsNullOrWhiteSpace(link) ? link.Replace("https:/", "").Replace("http:/", "").Trim('/') : link;
            item.DeletionLink = deleteLink;
            item.UploadTaskException = exception;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }

        private static void SetCopy(int id, bool copied, Exception exception = null)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            item.CopiedToClipboard = copied;
            item.CopyTaskException = exception;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }

        private static void SetCommand(int id, bool executed, string command, string output, Exception exception = null)
        {
            var item = Encodings.FirstOrDefault(x => x.Id == id);

            if (item == null)
                return;

            item.CommandExecuted = executed;
            item.Command = command;
            item.CommandOutput = output;
            item.CommandTaskException = exception;

            Application.Current.Dispatcher.BeginInvoke(new Action(() => EncodingUpdated(item)));
        }


        /// <summary>
        /// Gets the current status of the encoding of a current item.
        /// </summary>
        /// <param name="id">The unique ID of the item.</param>
        internal static Status? GetStatus(int id)
        {
            return Encodings.FirstOrDefault(f => f.Id == id)?.Status;
        }

        private static string GetUploadLink(int id)
        {
            return Encodings.FirstOrDefault(f => f.Id == id)?.UploadLink;
        }


        internal static void RemoveEncodings(int id)
        {
            RemoveEncodings(e => e.Id == id);
        }

        internal static void RemoveFinishedEncodings()
        {
            RemoveEncodings(p => p.Status != Status.Processing);
        }

        internal static void RemoveEncodings(Predicate<EncodingItem> match)
        {
            var list = Encodings.Where(match.Invoke).ToList();

            foreach (var encoding in list)
            {
                Encodings.Remove(encoding);

                TaskList.FirstOrDefault(f => f.Id == encoding.Id)?.Dispose();
                encoding.TokenSource.Dispose();

                TaskList.RemoveAll(r => r.Id == encoding.Id);
                CancellationTokenList.Remove(encoding.TokenSource);

                EncodingRemoved(encoding.Id);
            }
        }

        internal static void StopAllEncodings()
        {
            foreach (var tokenSource in CancellationTokenList)
                tokenSource.Cancel();
        }

        #endregion

        #region Progress report

        internal static void EncodingAdded(int id)
        {
            foreach (var window in Application.Current.Windows.OfType<IEncoding>())
            {
                //When set to display the encodings on a separated window, ignore the others (vice-versa).
                if (!UserSettings.All.DisplayEncoder == window.IsEncoderWindow && Encoder.IsAvailable)
                    continue;

                var view = window.EncodingAdded(id);
                
                if (view != null)
                    ViewList.Add(view);
            }
        }

        internal static void EncodingUpdated(EncodingItem current, bool statusUpdated = false)
        {
            foreach (var item in ViewList.Where(w => w.Id == current.Id))
            {
                item.Status = current.Status;
                item.CurrentFrame = current.CurrentFrame;
                item.Text = current.Text;
                item.IsIndeterminate = current.IsIndeterminate;
                item.SizeInBytes = current.SizeInBytes;
                item.OutputFilename = current.OutputFilename;
                item.SavedToDisk = current.SavedToDisk;
                item.Exception = current.Exception;

                item.Uploaded = current.Uploaded;
                item.UploadLink = current.UploadLink;
                item.UploadLinkDisplay = current.UploadLinkDisplay;
                item.DeletionLink = current.DeletionLink;
                item.UploadTaskException = current.UploadTaskException;

                item.CopiedToClipboard = current.CopiedToClipboard;
                item.CopyTaskException = current.CopyTaskException;

                item.CommandExecuted = current.CommandExecuted;
                item.Command = current.Command;
                item.CommandOutput = current.CommandOutput;
                item.CommandTaskException = current.CommandTaskException;

                item.TimeToAnalyze = current.TimeToAnalyze;
                item.TimeToEncode = current.TimeToEncode;
                item.TimeToUpload = current.TimeToUpload;
                item.TimeToCopy = current.TimeToCopy;
                item.TimeToExecute = current.TimeToExecute;
            }

            if (!statusUpdated)
                return;

            foreach (var window in Application.Current.Windows.OfType<IEncoding>())
            {
                //When set to display the encodings on a separated window, ignore the others (vice-versa).
                if (!UserSettings.All.DisplayEncoder == window.IsEncoderWindow && Encoder.IsAvailable)
                    continue;

                window.EncodingUpdated(current.Id, true);
            }
        }

        internal static void EncodingRemoved(int id)
        {
            foreach (var window in Application.Current.Windows.OfType<IEncoding>())
            {
                var view = window.EncodingRemoved(id);

                if (view != null)
                    ViewList.Remove(view);
            }
        }

        internal static void MoveEncodingsToPopups()
        {
            foreach (var window in Application.Current.Windows.OfType<IEncoding>())
            {
                //Only send this message to editors.
                if (window.IsEncoderWindow)
                    continue;

                window.EncodingUpdated(null, false);
            }
        }

        #endregion

        #region Encoding

        private static async Task Encode(ExportProject project, int id, Parameters param, CancellationTokenSource tokenSource)
        {
            var processing = LocalizationHelper.Get("S.Encoder.Processing");
            var watch = new Stopwatch();
            watch.Start();

            try
            {
                switch (param.Type)
                {
                    case Export.Gif:
                    {
                        #region Gif

                        switch (param.EncoderType)
                        {
                            case GifEncoderType.ScreenToGif:

                                #region Frame analysis

                                Update(id, 0, LocalizationHelper.Get("S.Encoder.Analyzing"));

                                if (param.EnableTransparency && param.ChromaKey.HasValue)
                                {
                                    ImageMethods.PaintAndCutForTransparency(project, param.TransparencyColor, param.ChromaKey.Value, id, tokenSource);
                                }
                                else if (param.DetectUnchangedPixels)
                                {
                                    if (param.ChromaKey.HasValue)
                                        ImageMethods.PaintTransparentAndCut(project, param.ChromaKey.Value, id, tokenSource);
                                    else
                                        ImageMethods.CutUnchanged(project, id, tokenSource);
                                }

                                Update(id, 0, watch.Elapsed);
                                watch.Restart();

                                if (tokenSource.Token.IsCancellationRequested)
                                {
                                    Update(id, Status.Canceled);
                                    return;
                                }

                                #endregion

                                #region ScreenToGif encoding

                                using (var stream = new MemoryStream())
                                {
                                    using (var encoder = new GifFile(stream))
                                    {
                                        encoder.RepeatCount = project.FrameCount > 1 ? param.RepeatCount : -1;
                                        encoder.UseGlobalColorTable = param.UseGlobalColorTable;
                                        encoder.TransparentColor = param.ChromaKey.HasValue ? System.Windows.Media.Color.FromArgb(0, param.ChromaKey.Value.R, param.ChromaKey.Value.G, param.ChromaKey.Value.B) : new System.Windows.Media.Color?();
                                        encoder.MaximumNumberColor = param.MaximumNumberColors;
                                        encoder.UseFullTransparency = param.EnableTransparency;
                                        encoder.QuantizationType = param.ColorQuantizationType;
                                        encoder.SamplingFactor = param.SamplingFactor;

                                        //Get the last index, in cases where the last frames have no changes.
                                        var last = project.FramesFiles.FindLastIndex(f => f.HasArea);

                                        //Read the frames pixels from the cache.
                                        using (var fileStream = new FileStream(project.ChunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                        {
                                            Update(id, 0, string.Format(processing, 0));

                                            for (var i = 0; i < project.Frames.Count; i++)
                                            {
                                                if (!project.Frames[i].HasArea && param.DetectUnchangedPixels)
                                                    continue;

                                                if (project.Frames[i].Delay == 0)
                                                    project.Frames[i].Delay = 10;

                                                fileStream.Position = project.Frames[i].DataPosition;
                                                encoder.AddFrame(fileStream.ReadBytes((int)project.Frames[i].DataLength), project.Frames[i].Rect, project.Frames[i].Delay, last == i);

                                                Update(id, i, string.Format(processing, i));

                                                #region Cancellation

                                                if (tokenSource.Token.IsCancellationRequested)
                                                {
                                                    Update(id, Status.Canceled);
                                                    break;
                                                }

                                                #endregion
                                            }
                                        }
                                    }

                                    try
                                    {
                                        using (var fileStream = new FileStream(param.Filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                            stream.WriteTo(fileStream);
                                    }
                                    catch (Exception ex)
                                    {
                                        Update(id, Status.Error);
                                        LogWriter.Log(ex, "Improved Encoding");
                                    }
                                }

                                #endregion

                                break;
                            case GifEncoderType.PaintNet:

                                #region System encoding

                                using (var stream = new MemoryStream())
                                {
                                    using (var encoder = new GifEncoder(stream, null, null, param.RepeatCount))
                                    {
                                        for (var i = 0; i < project.FramesFiles.Count; i++)
                                        {
                                            var bitmapAux = new Bitmap(project.FramesFiles[i].Path);
                                            encoder.AddFrame(bitmapAux, 0, 0, TimeSpan.FromMilliseconds(project.FramesFiles[i].Delay));
                                            bitmapAux.Dispose();

                                            Update(id, i, string.Format(processing, i));

                                            #region Cancellation

                                            if (tokenSource.Token.IsCancellationRequested)
                                            {
                                                Update(id, Status.Canceled);
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
                                        Update(id, Status.Error);
                                        LogWriter.Log(ex, "Encoding with paint.Net.");
                                    }
                                }

                                #endregion

                                break;
                            case GifEncoderType.FFmpeg:

                                EncodeWithFfmpeg("gif", project.FramesFiles, id, param, tokenSource, processing);

                                break;
                            case GifEncoderType.Gifski:

                                #region Gifski encoding

                                Update(id, Status.Processing, null, true);

                                if (!Util.Other.IsGifskiPresent())
                                    throw new ApplicationException("Gifski not present.");

                                if (File.Exists(param.Filename))
                                    File.Delete(param.Filename);

                                var gifski = new GifskiInterop();
                                var handle = gifski.Start(UserSettings.All.GifskiQuality, UserSettings.All.Looped);

                                if (gifski.Version.Major == 0 && gifski.Version.Minor < 9)
                                {
                                    #region Older

                                    ThreadPool.QueueUserWorkItem(delegate
                                    {
                                        Thread.Sleep(500);

                                        if (GetStatus(id) == Status.Error)
                                            return;

                                        Update(id, Status.Processing, null, false);

                                        GifskiInterop.GifskiError res;

                                        for (var i = 0; i < project.FramesFiles.Count; i++)
                                        {
                                            #region Cancellation

                                            if (tokenSource.Token.IsCancellationRequested)
                                            {
                                                Update(id, Status.Canceled);
                                                break;
                                            }

                                            #endregion

                                            Update(id, i, string.Format(processing, i));

                                            res = gifski.AddFrame(handle, (uint)i, project.FramesFiles[i].Path, project.FramesFiles[i].Delay);

                                            if (res != GifskiInterop.GifskiError.Ok)
                                                throw new Exception("Error while adding frames with Gifski. " + res, new Win32Exception(res.ToString())) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };
                                        }

                                        res = gifski.EndAdding(handle);

                                        if (res != GifskiInterop.GifskiError.Ok)
                                            throw new Exception("Error while finishing adding frames with Gifski. " + res, new Win32Exception(res.ToString())) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };
                                    }, null);

                                    gifski.End(handle, param.Filename);

                                    #endregion
                                }
                                else
                                {
                                    #region Version 0.9.3 and newer

                                    var res = gifski.SetOutput(handle, param.Filename);

                                    if (res != GifskiInterop.GifskiError.Ok)
                                        throw new Exception("Error while setting output with Gifski. " + res, new Win32Exception()) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };

                                    Thread.Sleep(500);

                                    if (GetStatus(id) == Status.Error)
                                        return;

                                    Update(id, Status.Processing, null, false);

                                    for (var i = 0; i < project.FramesFiles.Count; i++)
                                    {
                                        #region Cancellation

                                        if (tokenSource.Token.IsCancellationRequested)
                                        {
                                            Update(id, Status.Canceled);
                                            break;
                                        }

                                        #endregion

                                        Update(id, i, string.Format(processing, i));

                                        res = gifski.AddFrame(handle, (uint)i, project.FramesFiles[i].Path, project.FramesFiles[i].Delay, i + 1 == project.FramesFiles.Count);

                                        if (res != GifskiInterop.GifskiError.Ok)
                                            throw new Exception("Error while adding frames with Gifski. " + res, new Win32Exception(res.ToString())) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };
                                    }

                                    Update(id, Status.Processing, null, false);

                                    gifski.EndAdding(handle);

                                    #endregion
                                }

                                var fileInfo2 = new FileInfo(param.Filename);

                                if (!fileInfo2.Exists || fileInfo2.Length == 0)
                                    throw new Exception("Error while encoding the gif with Gifski. Empty output file.", new Win32Exception()) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };
                                
                                #endregion

                                break;
                            default:
                                throw new Exception("Undefined Gif encoder type");
                        }

                        Update(id, 1, watch.Elapsed);
                        watch.Restart();

                        break;

                        #endregion
                    }
                    case Export.Apng:
                    {
                        #region Apng

                        switch (param.ApngEncoder)
                        {
                            case ApngEncoderType.ScreenToGif:
                            {
                                #region Cut/Paint Unchanged Pixels

                                if (param.DetectUnchangedPixels)
                                {
                                    Update(id, 0, LocalizationHelper.Get("S.Encoder.Analyzing"));

                                    if (param.ChromaKey.HasValue)
                                        project.FramesFiles = ImageMethods.PaintTransparentAndCut(project.FramesFiles, param.ChromaKey.Value, id, tokenSource);
                                    else
                                        project.FramesFiles = ImageMethods.CutUnchanged(project.FramesFiles, id, tokenSource);

                                    Update(id, 0, watch.Elapsed);
                                    watch.Restart();
                                }
                                else
                                {
                                    var size = project.FramesFiles[0].Path.ScaledSize();
                                    project.FramesFiles.ForEach(x => x.Rect = new Int32Rect(0, 0, (int)size.Width, (int)size.Height));
                                }

                                #endregion

                                #region Encoding

                                using (var stream = new MemoryStream())
                                {
                                    var frameCount = project.FramesFiles.Count(x => x.HasArea);

                                    using (var encoder = new Apng(stream, frameCount, param.RepeatCount))
                                    {
                                        for (var i = 0; i < project.FramesFiles.Count; i++)
                                        {
                                            if (!project.FramesFiles[i].HasArea && param.DetectUnchangedPixels)
                                                continue;

                                            if (project.FramesFiles[i].Delay == 0)
                                                project.FramesFiles[i].Delay = 10;

                                            encoder.AddFrame(project.FramesFiles[i].Path, project.FramesFiles[i].Rect, project.FramesFiles[i].Delay);

                                            Update(id, i, string.Format(processing, i));

                                            #region Cancellation

                                            if (tokenSource.Token.IsCancellationRequested)
                                            {
                                                Update(id, Status.Canceled);
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
                                        Update(id, Status.Error);
                                        LogWriter.Log(ex, "Apng Encoding");
                                    }
                                }

                                #endregion

                                break;
                            }

                            case ApngEncoderType.FFmpeg:
                            {
                                EncodeWithFfmpeg("apng", project.FramesFiles, id, param, tokenSource, processing);
                                break;
                            }
                        }

                        Update(id, 1, watch.Elapsed);
                        watch.Restart();

                        break;

                        #endregion
                    }
                    case Export.Photoshop:
                    {
                        #region Psd

                        using (var stream = new MemoryStream())
                        {
                            using (var encoder = new Psd(stream, param.RepeatCount, param.Height, param.Width, param.Compress, param.SaveTimeline))
                            {
                                for (var i = 0; i < project.FramesFiles.Count; i++)
                                {
                                    if (project.FramesFiles[i].Delay == 0)
                                        project.FramesFiles[i].Delay = 10;

                                    encoder.AddFrame(i, project.FramesFiles[i].Path, project.FramesFiles[i].Delay);

                                    Update(id, i, string.Format(processing, i));

                                    #region Cancellation

                                    if (tokenSource.Token.IsCancellationRequested)
                                    {
                                        Update(id, Status.Canceled);
                                        break;
                                    }

                                    #endregion
                                }
                            }

                            using (var fileStream = new FileStream(param.Filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                stream.WriteTo(fileStream);
                        }

                        Update(id, 1, watch.Elapsed);
                        watch.Restart();

                        break;

                        #endregion
                    }
                    case Export.Video:
                    {
                        #region Video

                        switch (param.VideoEncoder)
                        {
                            case VideoEncoderType.AviStandalone:
                            {
                                #region Avi Standalone

                                var image = project.FramesFiles[0].Path.SourceFrom();

                                if (File.Exists(param.Filename))
                                    File.Delete(param.Filename);

                                //1000 / frames[0].Delay
                                using (var aviWriter = new AviWriter(param.Filename, param.Framerate, image.PixelWidth, image.PixelHeight, param.VideoQuality))
                                {
                                    var numImage = 0;
                                    foreach (var frame in project.FramesFiles)
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
                                            Update(id, Status.Canceled);
                                            break;
                                        }

                                        #endregion
                                    }
                                }

                                #endregion

                                break;
                            }
                            case VideoEncoderType.Ffmpg:
                            {
                                EncodeWithFfmpeg("video", project.FramesFiles, id, param, tokenSource, processing);
                                break;
                            }
                            default:
                                throw new Exception("Undefined video encoder");
                        }

                        Update(id, 1, watch.Elapsed);
                        watch.Restart();

                        break;

                        #endregion
                    }
                    case Export.Project:
                    {
                        #region Project

                        Update(id, Status.Processing, null, true);
                        Update(id, 0, LocalizationHelper.Get("S.Encoder.CreatingFile"));

                        if (File.Exists(param.Filename))
                            File.Delete(param.Filename);

                        ZipFile.CreateFromDirectory(Path.GetDirectoryName(project.FramesFiles[0].Path), param.Filename, param.CompressionLevel, false);

                        Update(id, 1, watch.Elapsed);
                        watch.Restart();

                        break;

                        #endregion
                    }

                    default:
                        throw new ArgumentOutOfRangeException(nameof(param));
                }

                //If it was canceled, try deleting the file.
                if (tokenSource.Token.IsCancellationRequested)
                {
                    if (File.Exists(param.Filename))
                        File.Delete(param.Filename);

                    Update(id, Status.Canceled);
                    return;
                }

                #region Upload

                if (param.Upload && File.Exists(param.Filename))
                {
                    Update(id, "S.Encoder.Uploading", true, true);

                    try
                    {
                        //TODO: Limit upload size and time.

                        var cloud = CloudFactory.CreateCloud(param.UploadDestination);

                        var uploadedFile = await cloud.UploadFileAsync(param.Filename, CancellationToken.None);

                        SetUpload(id, true, uploadedFile.Link, uploadedFile.DeleteLink);

                        Update(id, 2, watch.Elapsed);
                        watch.Stop();
                    }
                    catch (Exception e)
                    {
                        LogWriter.Log(e, "It was not possible to upload.");
                        SetUpload(id, false, null, null, e);
                    }
                }

                #endregion

                #region Copy to clipboard

                if (param.CopyToClipboard && File.Exists(param.Filename))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var data = new DataObject();

                            switch (param.CopyType)
                            {
                                case CopyType.File:
                                    data.SetFileDropList(new StringCollection { param.Filename });
                                    break;
                                case CopyType.FolderPath:
                                    data.SetText(Path.GetDirectoryName(param.Filename) ?? param.Filename, TextDataFormat.Text);
                                    break;
                                case CopyType.Link:
                                    var link = GetUploadLink(id);

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
                                    System.Windows.Clipboard.SetDataObject(data, true);
                                    break;
                                }
                                catch (COMException ex)
                                {
                                    if ((uint)ex.ErrorCode != 0x800401D0) //CLIPBRD_E_CANT_OPEN
                                        throw;
                                }

                                Thread.Sleep(100);
                            }

                            SetCopy(id, true);
                        }
                        catch (Exception e)
                        {
                            LogWriter.Log(e, "It was not possible to copy the file.");
                            SetCopy(id, false, e);
                        }
                    });

                    Update(id, 3, watch.Elapsed);
                    watch.Stop();
                }

                #endregion

                #region Execute commands

#if !UWP

                if (param.ExecuteCommands && !string.IsNullOrWhiteSpace(param.PostCommands))
                {
                    Update(id, "S.Encoder.Executing", true, true);

                    var command = param.PostCommands.Replace("{p}", "\"" + param.Filename + "\"").Replace("{f}", "\"" + Path.GetDirectoryName(param.Filename) + "\"").Replace("{u}", "\"" + GetUploadLink(id) + "\"");
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

                                var message = await process.StandardOutput.ReadToEndAsync();
                                var error = await process.StandardError.ReadToEndAsync();

                                if (!string.IsNullOrWhiteSpace(message))
                                    output += message + Environment.NewLine;

                                if (!string.IsNullOrWhiteSpace(message))
                                    output += message + Environment.NewLine;

                                if (!string.IsNullOrWhiteSpace(error))
                                    throw new Exception(error);

                                process.WaitForExit(1000);
                            }
                        }

                        SetCommand(id, true, command, output);

                        Update(id, 4, watch.Elapsed);
                        watch.Stop();
                    }
                    catch (Exception e)
                    {
                        LogWriter.Log(e, "It was not possible to run the post encoding command.");
                        SetCommand(id, false, command, output, e);
                    }
                }

#endif

                #endregion

                if (!tokenSource.Token.IsCancellationRequested)
                    Update(id, Status.Completed, param.Filename);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Encode");

                Update(id, Status.Error, null, false, ex);
            }
            finally
            {
                watch.Stop();

                #region Delete the encoder folder

                try
                {
                    var folder = Path.GetDirectoryName(project.UsesFiles ? project.FramesFiles[0].Path : project.ChunkPath);

                    if (!string.IsNullOrEmpty(folder))
                        if (Directory.Exists(folder))
                            Directory.Delete(folder, true);
                }
                catch (Exception ex)
                {
                    LogWriter.Log(ex, "Cleaning the encoder folder");
                }

                #endregion

                GC.Collect();
            }
        }

        private static void EncodeWithFfmpeg(string name, List<FrameInfo> listFrames, int id, Parameters param, CancellationTokenSource tokenSource, string processing)
        {
            #region FFmpeg encoding

            Update(id, Status.Processing, null, true);

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

            if (name.Equals("apng"))
                param.Command = string.Format(param.Command, "file:" + concatFile, (param.ExtraParameters ?? "").Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()), param.RepeatCount, param.Filename);
            else
            {
                if ((param.ExtraParameters ?? "").Contains("-pass 2"))
                {
                    //-vsync 2 -safe 0 -f concat -i ".\concat.txt" -c:v libvpx-vp9 -b:v 2M -pix_fmt yuv420p -tile-columns 6 -frame-parallel 1 -auto-alt-ref 1 -lag-in-frames 25 -vf "pad=width=1686:height=842:x=0:y=0:color=black" -pass 1 -y ".\Video.webm"
                    //-vsync 2 -safe 0 -f concat -i ".\concat.txt" -c:v libvpx-vp9 -b:v 2M -pix_fmt yuv420p -tile-columns 6 -frame-parallel 1 -auto-alt-ref 1 -lag-in-frames 25 -vf "pad=width=1686:height=842:x=0:y=0:color=black" -pass 2 -y ".\Video.webm"

                    param.Command = $"-vsync 2 -safe 0 -f concat -i \"{"file:" + concatFile}\" {(param.ExtraParameters ?? "").Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()).Replace("-pass 2", "-pass 1")} -passlogfile \"{param.Filename}\" -y \"{param.Filename}\"";
                    param.SecondPassCommand = $"-hide_banner -vsync 2 -safe 0 -f concat -i \"{"file:" + concatFile}\" {(param.ExtraParameters ?? "").Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString())} -passlogfile \"{param.Filename}\" -y \"{param.Filename}\"";
                }
                else
                    param.Command = string.Format(param.Command, "file:" + concatFile, (param.ExtraParameters ?? "").Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()), param.Filename);
            }

            var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
            {
                Arguments = param.Command,
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            var log = "";
            using (var pro = Process.Start(process))
            {
                var indeterminate = true;

                Update(id, 0, LocalizationHelper.Get("S.Encoder.Analyzing"));

                while (!pro.StandardError.EndOfStream)
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        pro.Kill();
                        return;
                    }

                    var line = pro.StandardError.ReadLine() ?? "";
                    log += Environment.NewLine + line;

                    var split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (var block = 0; block < split.Length; block++)
                    {
                        //frame=  321 fps=170 q=-0.0 Lsize=      57kB time=00:00:14.85 bitrate=  31.2kbits/s speed=7.87x    
                        if (!split[block].StartsWith("frame="))
                            continue;

                        if (int.TryParse(split[block + 1], out var frame))
                        {
                            if (frame > 0)
                            {
                                if (indeterminate)
                                {
                                    Update(id, Status.Processing, null, false);
                                    indeterminate = false;
                                }

                                Update(id, frame, string.Format(processing, frame));
                            }
                        }

                        break;
                    }
                }
            }

            var fileInfo = new FileInfo(param.Filename);

            if (!string.IsNullOrWhiteSpace(param.SecondPassCommand))
            {
                log += Environment.NewLine + SecondPassFfmpeg(param.SecondPassCommand, id, tokenSource, LocalizationHelper.Get("S.Encoder.Processing.Second"));

                EraseSecondPassLogs(param.Filename);
            }

            if (!fileInfo.Exists || fileInfo.Length == 0)
                throw new Exception($"Error while encoding the {name} with FFmpeg.") { HelpLink = $"Command:\n\r{param.Command + Environment.NewLine + param.SecondPassCommand}\n\rResult:\n\r{log}" };

            #endregion
        }

        private static string SecondPassFfmpeg(string command, int id, CancellationTokenSource tokenSource, string processing)
        {
            var log = "";

            var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
            {
                Arguments = command,
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using (var pro = Process.Start(process))
            {
                var indeterminate = true;

                Update(id, 0, LocalizationHelper.Get("S.Encoder.Analyzing.Second"));

                while (!pro.StandardError.EndOfStream)
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        pro.Kill();
                        return log;
                    }

                    var line = pro.StandardError.ReadLine() ?? "";
                    log += Environment.NewLine + line;

                    var split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (var block = 0; block < split.Length; block++)
                    {
                        //frame=  321 fps=170 q=-0.0 Lsize=      57kB time=00:00:14.85 bitrate=  31.2kbits/s speed=7.87x    
                        if (!split[block].StartsWith("frame="))
                            continue;

                        if (int.TryParse(split[block + 1], out var frame))
                        {
                            if (frame > 0)
                            {
                                if (indeterminate)
                                {
                                    Update(id, Status.Processing, null, false);
                                    indeterminate = false;
                                }

                                Update(id, frame, string.Format(processing, frame));
                            }
                        }

                        break;
                    }
                }
            }

            return log;
        }

        private static void EraseSecondPassLogs(string filename)
        {
            try
            {
                if (File.Exists(filename + "-0.log.mbtree"))
                    File.Delete(filename + "-0.log.mbtree");

                if (File.Exists(filename + "-0.log"))
                    File.Delete(filename + "-0.log");
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to delete the log files created by the second pass.");
            }
        }

        #endregion
    }


    interface IEncoding
    {
        bool IsEncoderWindow { get; }

        EncoderListViewItem EncodingAdded(int id);
        void EncodingUpdated(int? id = null, bool onlyStatus = false);
        EncoderListViewItem EncodingRemoved(int id);
    }

    internal class EncodingItem
    {
        public int Id { get; set; }

        public Export OutputType { get; set; }

        public Status Status { get; set;}

        public string Text { get; set; }

        public int FrameCount { get; set; }
        
        public int CurrentFrame { get; set; }

        public bool IsIndeterminate { get; set; }

        public long SizeInBytes { get; set; }
        
        public string OutputFilename { get; set; }
        
        public bool SavedToDisk { get; set; }


        public bool CopiedToClipboard { get; set; }
        
        public Exception CopyTaskException { get; set; }
        
        
        public bool CommandExecuted { get; set; }
        
        public string Command { get; set; }
        
        public string CommandOutput { get; set; }
        
        public Exception CommandTaskException { get; set; }


        public bool Uploaded { get; set; }
        
        public string UploadLink { get; set; }
        
        public string UploadLinkDisplay { get; set; }
        
        public string DeletionLink { get; set; }
        
        public Exception UploadTaskException { get; set; }

        public Exception Exception { get; set; }

        public CancellationTokenSource TokenSource { get; set; }


        public TimeSpan TimeToAnalyze { get; set; }

        public TimeSpan TimeToEncode { get; set; }

        public TimeSpan TimeToUpload { get; set; }

        public TimeSpan TimeToCopy { get; set; }

        public TimeSpan TimeToExecute { get; set; }
    }
}