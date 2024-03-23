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
using System.Windows.Media;
using System.Windows.Media.Imaging;

using KGySoft.CoreLibraries;
using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;
using KGySoft.Threading;

using ScreenToGif.Cloud;
using ScreenToGif.Controls;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models;
using ScreenToGif.ImageUtil;
using ScreenToGif.Util.Codification.Apng;
using ScreenToGif.Util.Codification.Gif.Encoder;
using ScreenToGif.Util.Codification.Psd;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;
using ScreenToGif.ViewModel;
using ScreenToGif.ViewModel.ExportPresets;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Apng;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;
using ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Webp;
using ScreenToGif.ViewModel.ExportPresets.Image;
using ScreenToGif.ViewModel.ExportPresets.Other;
using ScreenToGif.ViewModel.ExportPresets.Video;
using ScreenToGif.ViewModel.UploadPresets;
using ScreenToGif.Windows.Other;

using Color = System.Windows.Media.Color;
using Encoder = ScreenToGif.Windows.Other.Encoder;
using LegacyGifEncoder = ScreenToGif.Util.Codification.Gif.LegacyEncoder.GifEncoder;
using KGySoftGifEncoder = KGySoft.Drawing.Imaging.GifEncoder;

namespace ScreenToGif.Util;

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

    internal static void StartEncoding(ExportProject project, ExportPreset preset)
    {
        //If the user still wants an encoder window, here's when it should be opened.
        if (UserSettings.All.DisplayEncoder)
            Application.Current.Dispatcher?.Invoke(() => Encoder.Start(preset.Scale));

        //Creates the Cancellation Token
        var cancellationTokenSource = new CancellationTokenSource();
        CancellationTokenList.Add(cancellationTokenSource);

        var context = Application.Current.Dispatcher?.Invoke(TaskScheduler.FromCurrentSynchronizationContext);

        //Creates Task and send the Task Id.
        var taskId = -1;
        var task = new Task(async () =>
        {
            //ReSharper disable once AccessToModifiedClosure
            await Encode(project, preset, taskId, cancellationTokenSource);

        }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
        taskId = task.Id;

        #region Error handling

        task.ContinueWith(t =>
        {
            var aggregateException = t.Exception;
            aggregateException?.Handle(exception => true);

            Update(taskId, EncodingStatus.Error, null, false, t.Exception);
            LogWriter.Log(t.Exception, "Encoding error.");

        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, context);

        #endregion

        //Adds the encoding to the list.
        Encodings.Add(new EncodingItem
        {
            Id = taskId,
            OutputType = preset.Type,
            Text = LocalizationHelper.Get("S.Encoder.Starting"),
            FrameCount = project.FrameCount,
            TokenSource = cancellationTokenSource
        });

        try
        {
            TaskList.Add(task);
            task.Start();
        }
        catch (Exception ex)
        {
            ErrorDialog.Ok("Task Error", "Unable to start the encoding task", "A generic error occurred while trying to start the encoding task.", ex);
            LogWriter.Log(ex, "Errow while starting the task.");
        }

        //Application.Current.Dispatcher.Invoke(() => Refresh(taskId));
        Application.Current.Dispatcher?.Invoke(() => EncodingAdded(taskId));
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

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
    }

    internal static void Update(int id, int currentFrame)
    {
        var item = Encodings.FirstOrDefault(x => x.Id == id);

        if (item == null)
            return;

        item.CurrentFrame = currentFrame;

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
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

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
    }

    internal static void Update(int id, string text, bool isIndeterminate = false, bool findText = false)
    {
        var item = Encodings.FirstOrDefault(x => x.Id == id);

        if (item == null)
            return;

        item.Text = !findText ? text : LocalizationHelper.Get(text);
        item.IsIndeterminate = isIndeterminate;

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
    }

    /// <summary>
    /// Updates the status of the encoding of a current item.
    /// </summary>
    /// <param name="status">The current status.</param>
    /// <param name="id">The unique ID of the item.</param>
    /// <param name="fileName">The name of the output file.</param>
    /// <param name="isIndeterminate">The state of the progress bar.</param>
    /// <param name="exception">The exception details of the error.</param>
    /// <param name="maxSteps">Optionally sets the maximum steps of the processing if (<paramref name="status"/> is <see cref="EncodingStatus.Processing"/>).</param>
    internal static void Update(int id, EncodingStatus status, string fileName = null, bool isIndeterminate = false, Exception exception = null, int? maxSteps = null)
    {
        var item = Encodings.FirstOrDefault(x => x.Id == id);

        if (item == null)
            return;

        var wasStatusUpdated = item.Status != status;

        item.Status = status;
        item.IsIndeterminate = isIndeterminate;

        switch (status)
        {
            case EncodingStatus.Processing:
                item.Text = String.Format(LocalizationHelper.Get("S.Encoder.Processing"), fileName == null ? String.Empty : Path.GetFileName(fileName));
                if (maxSteps.HasValue)
                    item.FrameCount = maxSteps.Value;
                break;
            case EncodingStatus.Completed:
            {
                if (fileName == null)
                    break;

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
            case EncodingStatus.Error:
            {
                item.Exception = exception;
                break;
            }
        }

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item, wasStatusUpdated));
    }

    internal static void UpdateMultiple(int id, EncodingStatus status, List<string> fileNames, bool isIndeterminate = false)
    {
        var item = Encodings.FirstOrDefault(x => x.Id == id);

        if (item == null)
            return;

        var wasStatusUpdated = item.Status != status;

        item.Status = status;
        item.IsIndeterminate = isIndeterminate;
        item.AreMultipleFiles = true;

        if (status == EncodingStatus.Completed && fileNames?.Any() == true)
        {
            foreach (var fileInfo in fileNames.Where(File.Exists).Select(fileName => new FileInfo(fileName)))
            {
                fileInfo.Refresh();

                item.SizeInBytes += fileInfo.Length;
            }

            if (item.SizeInBytes > 0)
            {
                item.OutputFilename = fileNames.FirstOrDefault();
                item.SavedToDisk = true;
            }
        }

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item, wasStatusUpdated));
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

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
    }

    private static void SetCopy(int id, bool copied, Exception exception = null)
    {
        var item = Encodings.FirstOrDefault(x => x.Id == id);

        if (item == null)
            return;

        item.CopiedToClipboard = copied;
        item.CopyTaskException = exception;

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
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

        Application.Current.Dispatcher.Invoke(() => EncodingUpdated(item));
    }


    /// <summary>
    /// Gets the current status of the encoding of a current item.
    /// </summary>
    /// <param name="id">The unique ID of the item.</param>
    internal static EncodingStatus? GetStatus(int id)
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
        RemoveEncodings(p => p.Status != EncodingStatus.Processing);
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
            item.FrameCount = current.FrameCount;
            item.CurrentFrame = current.CurrentFrame;
            item.Text = current.Text;
            item.IsIndeterminate = current.IsIndeterminate;
            item.SizeInBytes = current.SizeInBytes;
            item.OutputFilename = current.OutputFilename;
            item.SavedToDisk = current.SavedToDisk;
            item.AreMultipleFiles = current.AreMultipleFiles;
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

    private static async Task Encode(ExportProject project, ExportPreset preset, int id, CancellationTokenSource tokenSource)
    {
        var processing = LocalizationHelper.Get("S.Encoder.Processing");
        var watch = new Stopwatch();
        watch.Start();

        try
        {
            #region File naming

            if (preset.PickLocation)
            {
                preset.FullPath = Path.Combine(preset.OutputFolder, preset.ResolvedFilename + (preset.Extension ?? preset.DefaultExtension));
            }
            else
            {
                preset.OutputFolder = Path.GetTempPath(); //Get path where the cache is stored instead.
                preset.OutputFilename = Guid.NewGuid() + preset.Extension;
                preset.FullPath = Path.Combine(preset.OutputFolder, preset.OutputFilename);

                //If somehow this happens, try again. TODO: File should be created on the spot, to properly prevent this issue.
                if (File.Exists(Path.Combine(preset.OutputFilename, preset.OutputFilename)))
                {
                    preset.OutputFilename = Guid.NewGuid() + preset.Extension;
                    preset.FullPath = Path.Combine(preset.OutputFolder, preset.OutputFilename);
                }
            }

            #endregion

            switch (preset.Type)
            {
                case ExportFormats.Apng:
                {
                    #region Apng

                    switch (preset.Encoder)
                    {
                        case EncoderTypes.ScreenToGif:
                        {
                            if (preset is not EmbeddedApngPreset embApngPreset)
                                return;

                            #region Cut/Paint Unchanged Pixels

                            if (embApngPreset.DetectUnchanged)
                            {
                                Update(id, 0, LocalizationHelper.Get("S.Encoder.Analyzing"));

                                if (embApngPreset.PaintTransparent)
                                    project.FramesFiles = ImageMethods.PaintTransparentAndCut(project.FramesFiles, Colors.Transparent, id, tokenSource);
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

                                using (var encoder = new Apng(stream, frameCount, embApngPreset.Looped && project.FrameCount > 1 ? (embApngPreset.RepeatForever ? 0 : embApngPreset.RepeatCount) : 1))
                                {
                                    for (var i = 0; i < project.FramesFiles.Count; i++)
                                    {
                                        if (!project.FramesFiles[i].HasArea && embApngPreset.DetectUnchanged)
                                            continue;

                                        if (project.FramesFiles[i].Delay == 0)
                                            project.FramesFiles[i].Delay = 10;

                                        encoder.AddFrame(project.FramesFiles[i].Path, project.FramesFiles[i].Rect, project.FramesFiles[i].Delay);

                                        Update(id, i, string.Format(processing, i));

                                        #region Cancellation

                                        if (tokenSource.Token.IsCancellationRequested)
                                        {
                                            Update(id, EncodingStatus.Canceled);
                                            break;
                                        }

                                        #endregion
                                    }
                                }

                                try
                                {
                                    using (var fileStream = new FileStream(embApngPreset.FullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                        stream.WriteTo(fileStream);
                                }
                                catch (Exception ex)
                                {
                                    Update(id, EncodingStatus.Error);
                                    LogWriter.Log(ex, "Apng Encoding");
                                }
                            }

                            #endregion

                            break;
                        }

                        case EncoderTypes.FFmpeg:
                        {
                            EncodeWithFfmpeg(preset, project.FramesFiles, id, tokenSource, processing);
                            break;
                        }
                    }

                    Update(id, 1, watch.Elapsed);
                    watch.Restart();

                    break;

                    #endregion
                }
                case ExportFormats.Gif:
                {
                    #region Gif

                    switch (preset.Encoder)
                    {
                        case EncoderTypes.ScreenToGif:

                            #region Frame analysis

                            Update(id, 0, LocalizationHelper.Get("S.Encoder.Analyzing"));

                            if (preset is not EmbeddedGifPreset embGifPreset)
                                return;

                            if (embGifPreset.EnableTransparency)
                            {
                                ImageMethods.PaintAndCutForTransparency(project, embGifPreset.SelectTransparencyColor ? embGifPreset.TransparencyColor : new Color?(), embGifPreset.ChromaKey, id, tokenSource);
                            }
                            else if (embGifPreset.DetectUnchanged)
                            {
                                if (embGifPreset.PaintTransparent)
                                    ImageMethods.PaintTransparentAndCut(project, embGifPreset.ChromaKey, id, tokenSource);
                                else
                                    ImageMethods.CutUnchanged(project, id, tokenSource);
                            }

                            Update(id, 0, watch.Elapsed);
                            watch.Restart();

                            if (tokenSource.Token.IsCancellationRequested)
                            {
                                Update(id, EncodingStatus.Canceled);
                                return;
                            }

                            #endregion

                            #region ScreenToGif encoding

                            using (var stream = new MemoryStream())
                            {
                                using (var encoder = new GifFile(stream))
                                {
                                    encoder.RepeatCount = embGifPreset.Looped && project.FrameCount > 1 ? (embGifPreset.RepeatForever ? 0 : embGifPreset.RepeatCount) : -1;
                                    encoder.UseGlobalColorTable = embGifPreset.UseGlobalColorTable;
                                    encoder.TransparentColor = embGifPreset.EnableTransparency && embGifPreset.SelectTransparencyColor ? Color.FromArgb(0, embGifPreset.TransparencyColor.R, embGifPreset.TransparencyColor.G, embGifPreset.TransparencyColor.B) :
                                        (embGifPreset.DetectUnchanged && embGifPreset.PaintTransparent) || embGifPreset.EnableTransparency ? Color.FromArgb(0, embGifPreset.ChromaKey.R, embGifPreset.ChromaKey.G, embGifPreset.ChromaKey.B) :
                                        new Color?();
                                    encoder.MaximumNumberColor = embGifPreset.MaximumColorCount;
                                    encoder.UseFullTransparency = embGifPreset.EnableTransparency;
                                    encoder.QuantizationType = embGifPreset.Quantizer;
                                    encoder.SamplingFactor = embGifPreset.SamplingFactor;

                                    //Get the last index, in cases where the last frames have no changes.
                                    var last = project.FramesFiles.FindLastIndex(f => f.HasArea);

                                    //Read the frames pixels from the cache.
                                    using (var fileStream = new FileStream(project.ChunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        Update(id, 0, string.Format(processing, 0));

                                        for (var i = 0; i < project.Frames.Count; i++)
                                        {
                                            if (!project.Frames[i].HasArea && embGifPreset.DetectUnchanged)
                                                continue;

                                            if (project.Frames[i].Delay == 0)
                                                project.Frames[i].Delay = 10;

                                            fileStream.Position = project.Frames[i].DataPosition;
                                            encoder.AddFrame(fileStream.ReadBytes((int)project.Frames[i].DataLength), project.Frames[i].Rect, project.Frames[i].Delay, last == i);

                                            Update(id, i, string.Format(processing, i));

                                            #region Cancellation

                                            if (tokenSource.Token.IsCancellationRequested)
                                            {
                                                Update(id, EncodingStatus.Canceled);
                                                break;
                                            }

                                            #endregion
                                        }
                                    }
                                }

                                try
                                {
                                    using (var fileStream = new FileStream(preset.FullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                                        stream.WriteTo(fileStream);
                                }
                                catch (Exception ex)
                                {
                                    Update(id, EncodingStatus.Error);
                                    LogWriter.Log(ex, "Improved Encoding");
                                }
                            }

                            #endregion

                            break;

                        case EncoderTypes.KGySoft:
                            if (preset is not KGySoftGifPreset kgySoftGifPreset)
                                return;

                            await EncodeKGySoftGif(kgySoftGifPreset, project.FramesFiles, id, tokenSource.Token);
                            break;

                        case EncoderTypes.System:

                            #region System encoding

                            if (preset is not SystemGifPreset systemGifPreset)
                                return;

                            using (var stream = new MemoryStream())
                            {
                                using (var encoder = new LegacyGifEncoder(stream, null, null, systemGifPreset.Looped && project.FrameCount > 1 ? (systemGifPreset.RepeatForever ? 0 : systemGifPreset.RepeatCount) : -1))
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
                                            Update(id, EncodingStatus.Canceled);
                                            break;
                                        }

                                        #endregion
                                    }
                                }

                                stream.Position = 0;

                                try
                                {
                                    using (var fileStream = new FileStream(systemGifPreset.FullPath, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize, false))
                                        stream.WriteTo(fileStream);
                                }
                                catch (Exception ex)
                                {
                                    Update(id, EncodingStatus.Error);
                                    LogWriter.Log(ex, "Encoding with paint.Net.");
                                }
                            }

                            #endregion

                            break;
                        case EncoderTypes.FFmpeg:

                            EncodeWithFfmpeg(preset, project.FramesFiles, id, tokenSource, processing);

                            break;
                        case EncoderTypes.Gifski:

                            #region Gifski encoding

                            Update(id, EncodingStatus.Processing, null, true);

                            if (preset is not GifskiGifPreset gifskiGifPreset)
                                return;

                            if (!Other.IsGifskiPresent())
                                throw new ApplicationException("Gifski not present.");

                            if (File.Exists(preset.FullPath))
                                File.Delete(preset.FullPath);

                            var size = project.FramesFiles[0].Path.ScaledSize();

                            var gifski = new GifskiInterop();
                            var handle = gifski.Start((uint)size.Width, (uint)size.Height, gifskiGifPreset.Quality, gifskiGifPreset.Looped, gifskiGifPreset.Fast);

                            if (gifski.Version.Major == 0 && gifski.Version.Minor < 9)
                            {
                                #region Older

                                ThreadPool.QueueUserWorkItem(delegate
                                {
                                    Thread.Sleep(500);

                                    if (GetStatus(id) == EncodingStatus.Error)
                                        return;

                                    Update(id, EncodingStatus.Processing, null, false);

                                    GifskiInterop.GifskiError res;

                                    for (var i = 0; i < project.FramesFiles.Count; i++)
                                    {
                                        #region Cancellation

                                        if (tokenSource.Token.IsCancellationRequested)
                                        {
                                            Update(id, EncodingStatus.Canceled);
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

                                gifski.End(handle, gifskiGifPreset.FullPath);

                                #endregion
                            }
                            else
                            {
                                #region Version 0.9.3 and newer

                                var res = gifski.SetOutput(handle, gifskiGifPreset.FullPath);

                                if (res != GifskiInterop.GifskiError.Ok)
                                    throw new Exception("Error while setting output with Gifski. " + res, new Win32Exception()) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };

                                Thread.Sleep(500);

                                if (GetStatus(id) == EncodingStatus.Error)
                                    return;

                                Update(id, EncodingStatus.Processing, null, false);

                                var lastTimestamp = project.FramesFiles.Sum(s => s.Delay / 1000D); //project.FramesFiles[project.FramesFiles.Count - 1].Delay / 1000d;

                                for (var i = 0; i < project.FramesFiles.Count; i++)
                                {
                                    #region Cancellation

                                    if (tokenSource.Token.IsCancellationRequested)
                                    {
                                        Update(id, EncodingStatus.Canceled);
                                        break;
                                    }

                                    #endregion

                                    Update(id, i, string.Format(processing, i));

                                    res = gifski.AddFrame(handle, (uint)i, project.FramesFiles[i].Path, project.FramesFiles[i].Delay, lastTimestamp, i + 1 == project.FramesFiles.Count);

                                    if (res != GifskiInterop.GifskiError.Ok)
                                        throw new Exception("Error while adding frames with Gifski. " + res, new Win32Exception(res.ToString())) { HelpLink = $"Result:\n\r{Marshal.GetLastWin32Error()}" };
                                }

                                Update(id, EncodingStatus.Processing, null, false);

                                gifski.EndAdding(handle);

                                #endregion
                            }

                            var fileInfo2 = new FileInfo(gifskiGifPreset.FullPath);

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

                case ExportFormats.Bmp:
                case ExportFormats.Jpeg:
                case ExportFormats.Png:
                {
                    if (preset is not ImagePreset imagePreset)
                        return;

                    var padLength = project.FramesFiles.Select(s => s.Index).Max().ToString().Length;

                    if (!imagePreset.ZipFiles)
                    {
                        var frameList = new List<string>();

                        foreach (var frame in project.FramesFiles)
                        {
                            var path = Path.Combine(preset.OutputFolder, $"{preset.ResolvedFilename}-{frame.Index.ToString().PadLeft(padLength, '0')}{preset.Extension ?? preset.DefaultExtension}");

                            if (File.Exists(path))
                                File.Delete(path);

                            Update(id, frame.Index, string.Format(processing, frame.Index));

                            switch (preset.Type)
                            {
                                case ExportFormats.Bmp:
                                {
                                    using (var fileStream = new FileStream(path, FileMode.Create))
                                    {
                                        var bmpEncoder = new BmpBitmapEncoder();
                                        bmpEncoder.Frames.Add(BitmapFrame.Create(frame.Path.SourceFrom()));
                                        bmpEncoder.Save(fileStream);
                                    }

                                    break;
                                }
                                case ExportFormats.Jpeg:
                                {
                                    using (var fileStream = new FileStream(path, FileMode.Create))
                                    {
                                        var jpgEncoder = new JpegBitmapEncoder { QualityLevel = 100 };
                                        jpgEncoder.Frames.Add(BitmapFrame.Create(frame.Path.SourceFrom()));
                                        jpgEncoder.Save(fileStream);
                                    }

                                    break;
                                }
                                case ExportFormats.Png:
                                {
                                    File.Copy(frame.Path, path, true);
                                    break;
                                }
                            }

                            frameList.Add(path);
                        }

                        UpdateMultiple(id, EncodingStatus.Completed, frameList);
                        return;
                    }
                    else
                    {
                        var fileName = Path.Combine(preset.OutputFolder, preset.ResolvedFilename + ".zip");

                        if (File.Exists(fileName))
                            File.Delete(fileName);

                        //Temporary folder.
                        var outPath = Path.Combine(project.Path, "Export");

                        if (Directory.Exists(outPath))
                            Directory.Delete(outPath, true);

                        var dir = Directory.CreateDirectory(outPath);

                        //Get files.
                        foreach (var frame in project.FramesFiles)
                        {
                            switch (preset.Type)
                            {
                                case ExportFormats.Bmp:
                                {
                                    var path = Path.Combine(dir.FullName, $"{frame.Index.ToString().PadLeft(padLength, '0')}.bmp");

                                    using (var fileStream = new FileStream(path, FileMode.Create))
                                    {
                                        var bmpEncoder = new BmpBitmapEncoder();
                                        bmpEncoder.Frames.Add(BitmapFrame.Create(frame.Path.SourceFrom()));
                                        bmpEncoder.Save(fileStream);
                                    }

                                    break;
                                }
                                case ExportFormats.Jpeg:
                                {
                                    var path = Path.Combine(dir.FullName, $"{frame.Index.ToString().PadLeft(padLength, '0')}.jpg");

                                    using (var fileStream = new FileStream(path, FileMode.Create))
                                    {
                                        var jpgEncoder = new JpegBitmapEncoder { QualityLevel = 100 };
                                        jpgEncoder.Frames.Add(BitmapFrame.Create(frame.Path.SourceFrom()));
                                        jpgEncoder.Save(fileStream);
                                    }

                                    break;
                                }
                                case ExportFormats.Png:
                                {
                                    var path = Path.Combine(dir.FullName, $"{frame.Index.ToString().PadLeft(padLength, '0')}.png");

                                    File.Copy(frame.Path, path, true);
                                    break;
                                }
                            }
                        }

                        //Create Zip and clear temporary folder.
                        ZipFile.CreateFromDirectory(dir.FullName, fileName);
                        Directory.Delete(dir.FullName, true);
                    }

                    break;
                }

                case ExportFormats.Webp:
                case ExportFormats.Avi:
                case ExportFormats.Mov:
                case ExportFormats.Mp4:
                case ExportFormats.Mkv:
                case ExportFormats.Webm:
                {
                    #region FFmpeg

                    EncodeWithFfmpeg(preset, project.FramesFiles, id, tokenSource, processing);

                    Update(id, 1, watch.Elapsed);
                    watch.Restart();

                    break;

                    #endregion
                }

                case ExportFormats.Psd:
                {
                    #region Psd

                    if (preset is not PsdPreset psdPreset)
                        return;

                    using (var stream = new MemoryStream())
                    {
                        using (var encoder = new Psd(stream, psdPreset.Height, psdPreset.Width, psdPreset.CompressImage, psdPreset.SaveTimeline))
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
                                    Update(id, EncodingStatus.Canceled);
                                    break;
                                }

                                #endregion
                            }
                        }

                        using (var fileStream = new FileStream(psdPreset.FullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                            stream.WriteTo(fileStream);
                    }

                    Update(id, 1, watch.Elapsed);
                    watch.Restart();

                    break;

                    #endregion
                }
                case ExportFormats.Stg:
                {
                    #region Project

                    if (preset is not StgPreset stgPreset)
                        return;

                    Update(id, EncodingStatus.Processing, null, true);
                    Update(id, 0, LocalizationHelper.Get("S.Encoder.CreatingFile"));

                    if (File.Exists(stgPreset.FullPath))
                        File.Delete(stgPreset.FullPath);

                    ZipFile.CreateFromDirectory(Path.GetDirectoryName(project.FramesFiles[0].Path), stgPreset.FullPath, stgPreset.CompressionLevel, false);

                    Update(id, 1, watch.Elapsed);
                    watch.Restart();

                    break;

                    #endregion
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(preset));
            }

            //If it was canceled, try deleting the file.
            if (tokenSource.Token.IsCancellationRequested)
            {
                if (File.Exists(preset.FullPath))
                    File.Delete(preset.FullPath);

                Update(id, EncodingStatus.Canceled);
                return;
            }

            #region Upload

            if (preset.UploadFile && File.Exists(preset.FullPath))
            {
                Update(id, "S.Encoder.Uploading", true, true);

                try
                {
                    //Get selected preset.
                    var presetType = preset.Extension == ".zip" ? ExportFormats.Zip : preset.Type;
                    var uploadPreset = UserSettings.All.UploadPresets.OfType<UploadPreset>().FirstOrDefault(f => (f.AllowedTypes.Count == 0 || f.AllowedTypes.Contains(presetType)) && f.Title == preset.UploadService);

                    if (uploadPreset == null)
                        throw new Exception($"Missing upload preset called {preset.UploadService}");

                    //TODO: Limit upload by imposed service limits.

                    //Try uploading to the selected service.
                    var cloud = CloudFactory.CreateCloud(uploadPreset.Type);
                    var history = await cloud.UploadFileAsync(uploadPreset, preset.FullPath, CancellationToken.None);

                    uploadPreset.History.Add(history);
                    UserSettings.Save();

                    if (history.Result != 200)
                        throw new Exception(history.Message);

                    SetUpload(id, true, history.GetLink(uploadPreset), history.DeletionLink);
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "It was not possible to upload.");
                    SetUpload(id, false, null, null, e);
                }
                finally
                {
                    Update(id, 2, watch.Elapsed);
                    watch.Restart();
                }
            }

            #endregion

            #region Copy to clipboard

            if (preset.SaveToClipboard && File.Exists(preset.FullPath))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var data = new DataObject();

                        switch (preset.CopyType)
                        {
                            case CopyModes.File:
                                //data.SetData(DataFormats.GetDataFormat("GIF").Name, System.Drawing.Image.FromFile(param.Filename));
                                data.SetFileDropList(new StringCollection { preset.FullPath });
                                break;
                            case CopyModes.FolderPath:
                                data.SetText(Path.GetDirectoryName(preset.FullPath) ?? preset.FullPath, TextDataFormat.UnicodeText);
                                break;
                            case CopyModes.Link:
                                var link = GetUploadLink(id);

                                data.SetText(string.IsNullOrEmpty(link) ? preset.FullPath : link, TextDataFormat.UnicodeText);
                                break;
                            default:
                                data.SetText(preset.FullPath, TextDataFormat.UnicodeText);
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
                watch.Restart();
            }

            #endregion

            #region Execute commands

#if !FULL_MULTI_MSIX_STORE

            if (preset.ExecuteCustomCommands && !string.IsNullOrWhiteSpace(preset.CustomCommands))
            {
                Update(id, "S.Encoder.Executing", true, true);

                var command = preset.CustomCommands.Replace("{p}", "\"" + preset.FullPath + "\"").Replace("{f}", "\"" + Path.GetDirectoryName(preset.OutputFolder) + "\"").Replace("{u}", "\"" + GetUploadLink(id) + "\"");
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
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "It was not possible to run the post encoding command.");
                    SetCommand(id, false, command, output, e);
                }
                finally
                {
                    Update(id, 4, watch.Elapsed);
                    watch.Restart();
                }
            }

#endif

            #endregion

            if (!tokenSource.Token.IsCancellationRequested)
                Update(id, EncodingStatus.Completed, preset.FullPath);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Encode");

            Update(id, EncodingStatus.Error, null, false, ex);
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

    private static async Task EncodeKGySoftGif(KGySoftGifPreset preset, IList<IFrame> frames, int id, CancellationToken cancellationToken)
    {
        #region Local Methods

        IEnumerable<IReadableBitmapData> FramesIterator()
        {
            for (int i = 0; i < frames.Count; i++)
            {
                Update(id, i);
                using var bitmap = new Bitmap(frames[i].Path);
                using var bitmapData = bitmap.GetReadableBitmapData();
                yield return bitmapData;
            }
        }

        #endregion

        // A little cheating for PingPong mode: though the encoder could handle it, we convert it explicitly to a simple forward loop so
        // we can update the progress in FramesIterator and don't need to pass a DrawingProgress implementation to TaskConfig.Progress
        var animationMode = (AnimationMode)preset.RepeatCount;
        if (animationMode == AnimationMode.PingPong)
        {
            animationMode = AnimationMode.Repeat;
            for (int i = frames.Count - 2; i > 0; i--)
                frames.Add(frames[i]);
        }

        Update(id, EncodingStatus.Processing, maxSteps: frames.Count);
        var config = new AnimatedGifConfiguration(FramesIterator(), frames.Select(f => TimeSpan.FromMilliseconds(f.Delay)))
        {
            AllowDeltaFrames = preset.AllowDeltaFrames,
            AllowClippedFrames = preset.AllowClippedFrames,
            AnimationMode = animationMode,
            DeltaTolerance = preset.DeltaTolerance,
            SizeHandling = AnimationFramesSizeHandling.Center,
            Quantizer = QuantizerDescriptor.Create(preset.QuantizerId, preset),
            Ditherer = DithererDescriptor.Create(preset.DithererId, preset),
        };

        await using var stream = File.Create(preset.FullPath, Constants.BufferSize);
        await KGySoftGifEncoder.EncodeAnimationAsync(config, stream, new TaskConfig
        {
            CancellationToken = cancellationToken,
            ThrowIfCanceled = false
        });

        if (!cancellationToken.IsCancellationRequested)
            await stream.FlushAsync(cancellationToken);
    }

    private static void EncodeWithFfmpeg(ExportPreset preset, List<IFrame> listFrames, int id, CancellationTokenSource tokenSource, string processing)
    {
        Update(id, EncodingStatus.Processing, null, true);

        if (!Other.IsFfmpegPresent())
            throw new ApplicationException("FFmpeg not present.");

        if (File.Exists(preset.FullPath))
            File.Delete(preset.FullPath);

        #region Generate concat

        var concat = new StringBuilder();
        foreach (var frame in listFrames)
        {
            concat.AppendLine("file '" + frame.Path + "'");
            concat.AppendLine("duration " + (frame.Delay / 1000d).ToString(CultureInfo.InvariantCulture));
        }

        if (preset.Type != ExportFormats.Gif && preset.Type != ExportFormats.Apng && preset.Type != ExportFormats.Webp)
        {
            //Fix for last frame.
            concat.AppendLine("file '" + listFrames.LastOrDefault()?.Path + "'");
            concat.AppendLine("duration 0");
        }

        var concatPath = Path.GetDirectoryName(listFrames[0].Path) ?? Path.GetTempPath();
        var concatFile = Path.Combine(concatPath, "concat.txt");

        if (!Directory.Exists(concatPath))
            Directory.CreateDirectory(concatPath);

        if (File.Exists(concatFile))
            File.Delete(concatFile);

        File.WriteAllText(concatFile, concat.ToString());

        #endregion

        var firstPass = "";
        var secondPass = "";
        //TODO: Adapt the code to support v4 or v6
        switch (preset.Type)
        {
            case ExportFormats.Gif:
            {
                #region Gif

                if (preset is not FfmpegGifPreset gifPreset)
                    return;

                //ffmpeg -vsync 0 {I} -loop 0 -lavfi palettegen=stats_mode=single[pal],[0:v][pal]paletteuse=new=1:dither=sierra2_4a:diff_mode=rectangle -f gif {O}
                if (gifPreset.SettingsMode == VideoSettingsModes.Advanced)
                    firstPass = gifPreset.Parameters.Replace("\n", " ").Replace("\r", "");
                else
                {
                    //Input and loop.
                    firstPass += "{I} ";
                    firstPass += $"-loop {(gifPreset.Looped ? gifPreset.RepeatForever ? 0 : gifPreset.RepeatCount : -1)} ";

                    //Palette and dither. Does not work properly: (gifPreset.UseGlobalColorTable ? "diff" : "single")
                    firstPass += $"-lavfi palettegen=stats_mode=diff[pal],[0:v][pal]paletteuse={(gifPreset.UseGlobalColorTable ? "" : "new=1:")}";
                    firstPass += $"dither={gifPreset.Dither.GetDescription()}";
                    firstPass += (gifPreset.Dither == DitherMethods.Bayer ? $":bayer_scale={gifPreset.BayerScale}" : "");
                    firstPass += ":diff_mode=rectangle ";

                    //Pixel format.
                    if (gifPreset.PixelFormat != VideoPixelFormats.Auto)
                        firstPass += $"-pix_fmt {gifPreset.PixelFormat.GetLowerDescription()} ";

                    //Framerate.
                    if (gifPreset.Framerate != Framerates.Auto)
                        firstPass += $"-r {(gifPreset.Framerate == Framerates.Custom ? gifPreset.CustomFramerate.ToString(CultureInfo.InvariantCulture) : gifPreset.Framerate.GetLowerDescription())} ";

                    //Format and output.
                    firstPass += "-f gif ";

                    //Vsync
                    if (gifPreset.Vsync != Vsyncs.Off)
                    {
                        if (UserSettings.All.FfmpegVersion == SupportedFFmpegVersions.Version6)
                            firstPass += "-fps_mode " + gifPreset.Vsync.GetLowerDescription();
                        else
                            firstPass += "-vsync " + gifPreset.Vsync.GetLowerDescription();
                    }

                    firstPass += " {O}";
                }

                break;

                #endregion
            }
            case ExportFormats.Apng:
            {
                #region Apng

                if (preset is not FfmpegApngPreset apngPreset)
                    return;

                //ffmpeg -vsync 0 {I} -pred mixed -plays 0 -f apng {O}
                if (apngPreset.SettingsMode == VideoSettingsModes.Advanced)
                    firstPass = apngPreset.Parameters.Replace("\n", " ").Replace("\r", "");
                else
                {
                    //Input and loop.
                    firstPass += "{I} ";
                    firstPass += $"-plays {(apngPreset.Looped ? apngPreset.RepeatForever ? 0 : apngPreset.RepeatCount : -1)} ";

                    //Prediction method.
                    if (apngPreset.PredictionMethod != PredictionMethods.None)
                        firstPass += $"-pred {apngPreset.PredictionMethod.ToString().ToLower()} ";

                    //Pixel format.
                    if (apngPreset.PixelFormat != VideoPixelFormats.Auto)
                        firstPass += $"-pix_fmt {apngPreset.PixelFormat.GetLowerDescription()} ";

                    //Framerate.
                    if (apngPreset.Framerate != Framerates.Auto)
                        firstPass += $"-r {(apngPreset.Framerate == Framerates.Custom ? apngPreset.CustomFramerate.ToString(CultureInfo.InvariantCulture) : apngPreset.Framerate.GetLowerDescription())} ";

                    //Format and output.
                    firstPass += "-f apng ";

                    //Vsync
                    if (apngPreset.Vsync != Vsyncs.Off)
                    {
                        if (UserSettings.All.FfmpegVersion == SupportedFFmpegVersions.Version6)
                            firstPass += "-fps_mode " + apngPreset.Vsync.GetLowerDescription();
                        else
                            firstPass += "-vsync " + apngPreset.Vsync.GetLowerDescription();
                    }

                    firstPass += " {O}";
                }

                break;

                #endregion
            }
            case ExportFormats.Webp:
            {
                #region Webp

                if (preset is not FfmpegWebpPreset webpPreset)
                    return;

                //ffmpeg -vsync 0 {I} -c:v libwebp_anim -lossless 0 -quality 75 -loop 0 -f webp {O}
                if (webpPreset.SettingsMode == VideoSettingsModes.Advanced)
                    firstPass = webpPreset.Parameters.Replace("\n", " ").Replace("\r", "");
                else
                {
                    //Vsync
                    if (webpPreset.Vsync != Vsyncs.Off)
                        firstPass += $"-vsync {webpPreset.Vsync.ToString().ToLower()} ";

                    //Input, encoder and loop.
                    firstPass += "{I} -c:v libwebp_anim ";
                    firstPass += $"-loop {(webpPreset.Looped ? webpPreset.RepeatForever ? 0 : webpPreset.RepeatCount : -1)} ";

                    //Codec preset.
                    if (webpPreset.CodecPreset != VideoCodecPresets.Default)
                        firstPass += $"-preset {webpPreset.CodecPreset.GetLowerDescription()} ";

                    //Lossless.
                    firstPass += $"-lossless {(webpPreset.Lossless ? "1" : "0")} ";

                    //Quality.
                    firstPass += $"-quality {webpPreset.Quality} ";

                    //Pixel format.
                    if (webpPreset.PixelFormat != VideoPixelFormats.Auto)
                        firstPass += $"-pix_fmt {webpPreset.PixelFormat.GetLowerDescription()} ";

                    //Framerate.
                    if (webpPreset.Framerate != Framerates.Auto)
                        firstPass += $"-r {(webpPreset.Framerate == Framerates.Custom ? webpPreset.CustomFramerate.ToString(CultureInfo.InvariantCulture) : webpPreset.Framerate.GetLowerDescription())} ";

                    //Format and output.
                    firstPass += "-f webp ";

                    //Vsync
                    if (webpPreset.Vsync != Vsyncs.Off)
                    {
                        if (UserSettings.All.FfmpegVersion == SupportedFFmpegVersions.Version6)
                            firstPass += "-fps_mode " + webpPreset.Vsync.GetLowerDescription();
                        else
                            firstPass += "-vsync " + webpPreset.Vsync.GetLowerDescription();
                    }

                    firstPass += " {O}";
                }

                break;

                #endregion
            }

            case ExportFormats.Avi:
            case ExportFormats.Mkv:
            case ExportFormats.Mov:
            case ExportFormats.Mp4:
            case ExportFormats.Webm:
            {
                #region Video

                if (preset is not VideoPreset videoPreset)
                    return;

                if (videoPreset.SettingsMode == VideoSettingsModes.Advanced)
                {
                    firstPass = videoPreset.Parameters.Replace("\n", " ").Replace("\r", "");

                    if (firstPass.Contains("-pass 2"))
                    {
                        firstPass = firstPass.Replace("-pass 2", $"-pass 1 -passlogfile \"{preset.FullPath}\" ");
                        secondPass = "-hide_banner " + firstPass.Replace("-pass 1", $"-pass 2 -passlogfile \"{preset.FullPath}\" ");
                    }
                }
                else
                {
                    //Hardware acceleration.
                    if (videoPreset.HardwareAcceleration != HardwareAccelerationModes.Off)
                        firstPass += "-hwaccel auto ";

                    //Input and encoder.
                    firstPass += "{I} ";
                    firstPass += $"-c:v {videoPreset.VideoCodec.GetLowerDescription()} ";

                    //Some codecs require special treatments.
                    if (videoPreset.VideoCodec == VideoCodecs.Mpeg4)
                        firstPass += "-vtag xvid ";
                    else if (videoPreset.VideoCodec == VideoCodecs.Vp9)
                        firstPass += "-tile-columns 6 -frame-parallel 1 -auto-alt-ref 1 -lag-in-frames 25 ";

                    //Codec preset.
                    if (videoPreset.CodecPreset != VideoCodecPresets.Default && videoPreset.CodecPreset != VideoCodecPresets.None && videoPreset.CodecPreset != VideoCodecPresets.NotSelected)
                    {
                        if (videoPreset.VideoCodec is VideoCodecs.SvtAv1 or VideoCodecs.Rav1E)
                            firstPass += $"-preset {(int) videoPreset.CodecPreset} ";
                        else
                            firstPass += $"-preset {videoPreset.CodecPreset.GetLowerDescription()} ";
                    }

                    //Pixel format.
                    if (videoPreset.PixelFormat != VideoPixelFormats.Auto)
                        firstPass += $"-pix_fmt {videoPreset.PixelFormat.GetLowerDescription()} ";

                    //Workaround, makes the size to be divisible by two.
                    firstPass += "-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" "; //"scale=iw+mod(iw,2):ih+mod(ih,2):flags=neighbor" OR "pad=width={W}:height={H}:x=0:y=0:color=black"

                    //CRF.
                    if (videoPreset.ConstantRateFactor > 0)
                        firstPass += $"-crf {videoPreset.ConstantRateFactor.Value.ToString(CultureInfo.InvariantCulture)} ";

                    //Bitrate.
                    if (videoPreset.IsVariableBitRate)
                        firstPass += $"-q:v {videoPreset.QualityLevel.ToString(CultureInfo.InvariantCulture)} ";
                    else
                    {
                        if (videoPreset.BitRate > 0)
                            firstPass += $"-b:v {videoPreset.BitRate.ToString(CultureInfo.InvariantCulture)}{videoPreset.BitRateUnit.GetDescription()} ";
                        else if (videoPreset.BitRate == 0 && (videoPreset.VideoCodec == VideoCodecs.Vp8 || videoPreset.VideoCodec == VideoCodecs.Vp9))
                            firstPass += "-b:v 0 ";
                    }

                    //Minimum bitrate.
                    if (videoPreset.MinimumBitRate > 0)
                        firstPass += $"-minrate {videoPreset.MinimumBitRate.ToString(CultureInfo.InvariantCulture)}{videoPreset.MinimumBitRateUnit.GetDescription()} ";

                    //Maximum bitrate.
                    if (videoPreset.MaximumBitRate > 0)
                        firstPass += $"-maxrate {videoPreset.MaximumBitRate.ToString(CultureInfo.InvariantCulture)}{videoPreset.MaximumBitRateUnit.GetDescription()} ";

                    //Buffer size.
                    if (videoPreset.RateControlBuffer > 0)
                        firstPass += $"-bufsize {videoPreset.RateControlBuffer.ToString(CultureInfo.InvariantCulture)}{videoPreset.RateControlBufferUnit.GetDescription()} ";

                    //First pass adjustments.
                    if (videoPreset.Pass > 1)
                    {
                        if (videoPreset.VideoCodec == VideoCodecs.X265)
                            firstPass += "-x265-params pass=1 ";
                        else
                            firstPass += "-pass 1 ";

                        firstPass += $"-passlogfile \"{preset.FullPath}\" ";
                    }

                    //Framerate.
                    if (videoPreset.Framerate != Framerates.Auto)
                        firstPass += $"-r {(videoPreset.Framerate == Framerates.Custom ? videoPreset.CustomFramerate.ToString(CultureInfo.InvariantCulture) : videoPreset.Framerate.GetLowerDescription())} ";

                    //Format and output.
                    firstPass += $"-f {preset.Type.ToString().ToLower().Replace("mkv", "matroska")} ";

                    //Vsync
                    if (videoPreset.Vsync != Vsyncs.Off)
                    {
                        if (UserSettings.All.FfmpegVersion == SupportedFFmpegVersions.Version6)
                            firstPass += "-fps_mode " + videoPreset.Vsync.GetLowerDescription();
                        else
                            firstPass += "-vsync " + videoPreset.Vsync.GetLowerDescription();
                    }

                    firstPass += " {O}";

                    //Second pass, using a similar command with some adjustments.
                    if (videoPreset.Pass > 1)
                        secondPass = "-hide_banner " + firstPass.Replace("-pass 1", "-pass 2").Replace("pass=1", "pass=2");
                }

                break;

                #endregion
            }
        }

        //Replace special params.
        firstPass = firstPass.Replace("{I}", $"-safe 0 -f concat -i \"file:{concatFile}\"").Replace("{O}", $"-y \"{preset.FullPath}\"")
            .Replace("{H}", preset.Height.DivisibleByTwo().ToString(CultureInfo.InvariantCulture)).Replace("{W}", preset.Width.DivisibleByTwo().ToString(CultureInfo.InvariantCulture));

        secondPass = secondPass.Replace("{I}", $"-safe 0 -f concat -i \"file:{concatFile}\"").Replace("{O}", $"-y \"{preset.FullPath}\"")
            .Replace("{H}", preset.Height.DivisibleByTwo().ToString(CultureInfo.InvariantCulture)).Replace("{W}", preset.Width.DivisibleByTwo().ToString(CultureInfo.InvariantCulture));

        var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
        {
            Arguments = firstPass,
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
                var containsDuplicatedFrames = line.Contains("dup=");

                for (var block = 0; block < split.Length; block++)
                {
                    //frame=  321 fps=170 q=-0.0 Lsize=      57kB time=00:00:14.85 bitrate=  31.2kbits/s speed=7.87x
                    if (!split[block].StartsWith("frame="))
                        continue;

                    if (int.TryParse(split[block + 1], out var frame))
                    {
                        if (containsDuplicatedFrames)
                        {
                            var dupFramesPosition = split.IndexOf(x => x.Contains("dup="));
                            var dupFramesValue = split[dupFramesPosition][4..]; //skip dup=
                            if (int.TryParse(dupFramesValue, out var duplicatedFrames))
                            {
                                // if we've managed to read number of duplicated frames - adjust the frame number
                                frame -= duplicatedFrames;
                            }
                        }
                        if (frame > 0)
                        {
                            if (indeterminate)
                            {
                                Update(id, EncodingStatus.Processing, null, false);
                                indeterminate = false;
                            }

                            Update(id, frame, string.Format(processing, frame));
                        }
                    }

                    break;
                }
            }
        }

        var fileInfo = new FileInfo(preset.FullPath);

        //Execute the second pass, cleaning up the logs.
        if (!string.IsNullOrWhiteSpace(secondPass))
        {
            //I could try using as a single command.
            //ffmpeg -y -hwaccel auto {I} -c:v h264_nvenc -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" -pass 1 -f avi NUL
            //&&
            //ffmpeg -y -hwaccel auto {I} -c:v h264_nvenc -pix_fmt yuv420p -vf "scale=trunc(iw/2)*2:trunc(ih/2)*2" -pass 2 -f avi {O}

            log += Environment.NewLine + SecondPassFfmpeg(secondPass, id, tokenSource, LocalizationHelper.Get("S.Encoder.Processing.Second"));

            EraseSecondPassLogs(preset.FullPath);
        }

        if (!fileInfo.Exists || fileInfo.Length == 0)
            throw new Exception($"Error while encoding the {preset.Type} with FFmpeg.") { HelpLink = $"Command:\n\r{firstPass + Environment.NewLine + secondPass}\n\rResult:\n\r{log}" };
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
                                Update(id, EncodingStatus.Processing, null, false);
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

    public ExportFormats OutputType { get; set; }

    public EncodingStatus Status { get; set;}

    public string Text { get; set; }

    public int FrameCount { get; set; }

    public int CurrentFrame { get; set; }

    public bool IsIndeterminate { get; set; }

    public long SizeInBytes { get; set; }

    public string OutputFilename { get; set; }

    public bool SavedToDisk { get; set; }

    public bool AreMultipleFiles { get; set; }


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