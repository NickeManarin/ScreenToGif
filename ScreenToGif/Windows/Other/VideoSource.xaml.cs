using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Model;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class VideoSource : Window
    {
        #region Variables

        private readonly object _lock = new object();
        private bool _cancelled;
        private bool _isDisplayingError;
        private int _previewerReady = 0;
        private bool _wasPreviewChangedRegistered;
        private bool _wasCaptureChangedRegistered;
        private MediaPlayer _lowerPlayer = new MediaPlayer { Volume = 0, ScrubbingEnabled = true };
        private MediaPlayer _upperPlayer = new MediaPlayer { Volume = 0, ScrubbingEnabled = true };
        private RenderTargetBitmap _lowerRenderTargetBitmap;
        private RenderTargetBitmap _upperRenderTargetBitmap;
        private readonly Queue<TimeSpan> _positions = new Queue<TimeSpan>();
        private Process _process;

        /// <summary>
        /// The path of the video file to be imported.
        /// </summary>
        public string VideoPath { get; set; }

        /// <summary>
        /// The path of the project where the imported frames will be stored after importing.
        /// </summary>
        public string RootFolder { get; set; }

        /// <summary>
        /// The width of the video, detected by this video importer.
        /// </summary>
        public int VideoWidth { get; private set; }

        /// <summary>
        /// The height of the video, detected by this video importer.
        /// </summary>
        public int VideoHeight { get; private set; }

        /// <summary>
        /// The duration of the video, detected by this video importer.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The imported frame list.
        /// </summary>
        public List<FrameInfo> Frames { get; set; }

        /// <summary>
        /// The delay of each frame.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// The scale of the frame.
        /// </summary>
        private double Scale { get; set; }

        #endregion

        public VideoSource()
        {
            InitializeComponent();
        }

        #region Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPreview();
        }

        private async void ImporterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            //Load the previewers using the selected importer.
            await LoadPreview();
        }

        private async void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            await WhenBothLoaded();
        }

        private void MediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            FaultLoading(e.ErrorException);
        }

        private void SelectionSlider_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            StartIntegerUpDown.Value = Convert.ToInt32(SelectionSlider.LowerValue);

            MeasureDuration();
            FrameCountTextBlock.Text = CountFrames().ToString();
        }

        private void SelectionSlider_UpperValueChanged(object sender, RoutedEventArgs e)
        {
            EndIntegerUpDown.Value = Convert.ToInt32(SelectionSlider.UpperValue);

            MeasureDuration();
            FrameCountTextBlock.Text = CountFrames().ToString();
        }

        private async void StartIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.VideoImporter == 0)
            {
                _lowerPlayer.Position = TimeSpan.FromMilliseconds(SelectionSlider.LowerValue);
                return;
            }

            if (SelectionSlider.IsMouseCaptureWithin)
                return;

            Cursor = Cursors.AppStarting;
            await RenderPreview();
            Cursor = Cursors.Arrow;
        }

        private async void EndIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (UserSettings.All.VideoImporter == 0)
            {
                _upperPlayer.Position = TimeSpan.FromMilliseconds(SelectionSlider.UpperValue);
                return;
            }

            if (SelectionSlider.IsMouseCaptureWithin)
                return;

            Cursor = Cursors.AppStarting;
            await RenderPreview(false);
            Cursor = Cursors.Arrow;
        }

        private async void SelectionSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Cursor = Cursors.AppStarting;
            await RenderPreview();
            await RenderPreview(false);
            Cursor = Cursors.Arrow;
        }

        private void ScaleNumericUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            var height = Convert.ToInt32(VideoHeight * (ScaleIntegerUpDown.Value / 100D));
            var width = Convert.ToInt32(VideoWidth * (ScaleIntegerUpDown.Value / 100D));

            HeightTextBlock.Text = height.ToString("d", CultureInfo.CurrentUICulture);
            WidthTextBlock.Text = width.ToString("d", CultureInfo.CurrentUICulture);
        }

        private void FpsIntegerUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            FrameCountTextBlock.Text = CountFrames().ToString();
        }

        private async void LowerPlayer_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            Cursor = Cursors.AppStarting;
            await RenderPreview();
            Cursor = Cursors.Arrow;
        }

        private async void UpperPlayer_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            Cursor = Cursors.AppStarting;
            await RenderPreview(false);
            Cursor = Cursors.Arrow;
        }

        private void CapturePlayer_Changed(object sender, EventArgs e)
        {
            //ImportAndSeek();
            Dispatcher?.BeginInvoke(new Action(ImportAndSeek));
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (CountFrames() == 0)
            {
                StatusBand.Warning(LocalizationHelper.Get("S.ImportVideo.Nothing"));
                return;
            }

            Scale = ScaleIntegerUpDown.Value * 0.01f;
            Delay = 1000 / FpsIntegerUpDown.Value;
            VideoWidth = (int)Math.Round(VideoWidth * Scale);
            VideoHeight = (int)Math.Round(VideoHeight * Scale);
            Frames = new List<FrameInfo>();

            Splitter.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Collapsed;
            SelectionSlider.IsEnabled = false;
            OkButton.IsEnabled = false;
            StatusLabel.Visibility = Visibility.Visible;
            CaptureProgressBar.Visibility = Visibility.Visible;
            MinHeight = Height;
            SizeToContent = SizeToContent.Manual;

            GC.Collect();

            if (UserSettings.All.VideoImporter == 0)
            {
                //Calculate all positions.
                for (var span = SelectionSlider.LowerValue + Delay; span <= SelectionSlider.UpperValue; span += Delay)
                    _positions.Enqueue(TimeSpan.FromMilliseconds(span));

                CaptureProgressBar.Value = 0;
                CaptureProgressBar.Maximum = _positions.Count;

                if (_wasPreviewChangedRegistered)
                {
                    _lowerPlayer.Changed -= LowerPlayer_Changed;
                    _upperPlayer.Changed -= UpperPlayer_Changed;
                    _wasPreviewChangedRegistered = false;
                }

                if (!_wasCaptureChangedRegistered)
                {
                    _lowerPlayer.Changed += CapturePlayer_Changed;
                    _wasCaptureChangedRegistered = true;
                }

                //Resize the rendering to fit in the selected scale. With this code, the preview stops working.
                _lowerRenderTargetBitmap = new RenderTargetBitmap(VideoWidth, VideoHeight, 96, 96, PixelFormats.Pbgra32);

                ImportAndSeek();
            }
            else
            {
                //Import via ffmpeg.
                await GetMultipleScreencaps();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelled = true;

            #region Erase data

            try
            {
                if (UserSettings.All.VideoImporter == 0)
                {
                    if (Frames != null)
                        foreach (var frame in Frames.Where(frame => File.Exists(frame.Path)))
                            File.Delete(frame.Path);
                }
                else
                {
                    _process?.Kill();

                    ClearImportFolder(Path.Combine(RootFolder, "Import"));
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to delete imported frames when canceling the import.");
            }

            #endregion

            Frames?.Clear();
            GC.Collect();

            DialogResult = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _lowerPlayer?.Close();
            _upperPlayer?.Close();
            _process?.Dispose();

            _lowerPlayer = null;
            _upperPlayer = null;
            _process = null;

            GC.Collect();
        }

        #endregion

        #region Methods

        private async Task LoadPreview()
        {
            StatusBand.Hide();

            //Disable the UI when loading.
            Cursor = Cursors.AppStarting;
            ImporterComboBox.IsEnabled = false;
            DetailsGrid.IsEnabled = false;
            SelectionSlider.IsEnabled = false;
            OkButton.IsEnabled = false;
            PreviewerGrid.Opacity = 0;
            LoadingLabel.Visibility = Visibility.Visible;
            DetailsGrid.Visibility = Visibility.Collapsed;

            LowerSelectionImage.Source = null;
            UpperSelectionImage.Source = null;
            VideoWidth = 0;
            VideoHeight = 0;
            Duration = TimeSpan.Zero;
            _previewerReady = 0;

            //If trying to use FFmpeg, check if it's possible.
            if (UserSettings.All.VideoImporter == 1)
            {
                if (!Util.Other.IsFfmpegPresent())
                {
                    StatusBand.Warning(LocalizationHelper.Get("S.Editor.Warning.Ffmpeg"), null, () => App.MainViewModel.OpenOptions.Execute(Options.ExtrasIndex));
                    FaultLoading();
                    return;
                }
            }

            var log = "";

            try
            {
                if (UserSettings.All.VideoImporter == 0)
                {
                    log += "Video path: " + VideoPath;

                    //Unregister all events.
                    _upperPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                    _upperPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                    _lowerPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                    _lowerPlayer.MediaFailed -= MediaPlayer_MediaFailed;

                    if (_wasPreviewChangedRegistered)
                    {
                        _lowerPlayer.Changed -= LowerPlayer_Changed;
                        _upperPlayer.Changed -= UpperPlayer_Changed;
                        _wasPreviewChangedRegistered = false;
                    }

                    if (_wasCaptureChangedRegistered)
                    {
                        _lowerPlayer.Changed -= CapturePlayer_Changed;
                        _wasCaptureChangedRegistered = false;
                    }

                    var previous = _upperPlayer.Source?.AbsoluteUri;

                    _upperPlayer.MediaOpened += MediaPlayer_MediaOpened;
                    _upperPlayer.MediaFailed += MediaPlayer_MediaFailed;
                    _lowerPlayer.MediaOpened += MediaPlayer_MediaOpened;
                    _lowerPlayer.MediaFailed += MediaPlayer_MediaFailed;

                    if (!string.IsNullOrWhiteSpace(previous) && previous == new Uri(VideoPath).AbsoluteUri)
                    {
                        //Same video as before.
                        _previewerReady = 2;
                        await WhenBothLoaded();
                    }
                    else
                    {
                        //Open the same video file in both players.
                        _upperPlayer.Open(new Uri(VideoPath));
                        _upperPlayer.Pause();
                        _lowerPlayer.Open(new Uri(VideoPath));
                        _lowerPlayer.Pause();
                    }
                }
                else
                {
                    await GetVideoDetails();
                    await SucessLoading();
                }
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Impossible to load the previewers", log);

                FaultLoading(ex);
            }
        }

        private async Task GetVideoDetails()
        {
            var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
            {
                Arguments = $" -i \"{VideoPath}\" -hide_banner",
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var log = "Arguments: " + process.Arguments + Environment.NewLine;

            string response;
            using (var pro = await Task.Run(() => Process.Start(process)))
            {
                if (pro == null)
                    throw new Exception("It was not possible to start the FFmpeg process." + log);

                //Read all output data.
                response = await pro.StandardError.ReadToEndAsync();
            }

            /*
               Input #0, mov,mp4,m4a,3gp,3g2,mj2, from 'C:\Users\user\Desktop\example.mp4':
               Metadata:
               major_brand     : mp42
               minor_version   : 0
               compatible_brands: isomavc1mp42
               creation_time   : 2010-03-15T22:51:17.000000Z
               Duration: 00:01:02.58, start: 0.000000, bitrate: 589 kb/s
               Stream #0:0(und): Audio: aac (LC) (mp4a / 0x6134706D), 44100 Hz, stereo, fltp, 104 kb/s (default)
               Metadata:
               creation_time   : 2010-03-15T22:51:17.000000Z
               handler_name    : (C) 2007 Google Inc. v08.13.2007.
               Stream #0:1(und): Video: h264 (Constrained Baseline) (avc1 / 0x31637661), yuv420p, 480x360 [SAR 1:1 DAR 4:3], 487 kb/s, 15 fps, 15 tbr, 15002 tbn, 30 tbc (default)
               Metadata:
               creation_time   : 2010-03-15T22:51:17.000000Z
               handler_name    : (C) 2007 Google Inc. v08.13.2007.
               At least one output file must be specified
             */

            log += "Response: " + response + Environment.NewLine;

            //Tries to find the line which shows the video stream details. TODO: What happens if there's more than 1 video stream?
            var lineRegex = new Regex(".*(Stream.\\#).*(Video\\:).*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var linesFound = lineRegex.Matches(response);

            if (linesFound.Count == 0)
                throw new Exception("No video stream found." + log);

            //Tries to find the part which tells the resolution.
            var resolutionRegex = new Regex("[0-9]{2,4}x[0-9]{2,4}\\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var resolutionsFound = resolutionRegex.Matches(linesFound[0].Value);

            if (resolutionsFound.Count == 0)
                throw new Exception("No video resolution found." + log);

            //Tries to find the line which shows the video rotation.
            var rotationRegex = new Regex(".*(rotate          : ).*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var linesFound2 = rotationRegex.Matches(response);
            var isRotated = linesFound2.Count > 0 && linesFound2[0].Value.Contains("90");

            //Tries to find the line which shows the video stream details.
            var durationRegex = new Regex(".*(Duration: ).*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var linesFound3 = durationRegex.Matches(response);

            if (linesFound3.Count == 0)
                throw new Exception("No video duration found." + log);

            //Tried to find the total time of the video.
            var timingRegex = new Regex("[0-9]{1,2}:[0-9]{1,2}:[0-9]{1,2}(\\.[0-9]{1,2})?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var timingsFound = timingRegex.Matches(linesFound3[0].Value);

            if (timingsFound.Count == 0)
                throw new Exception("No video timing found." + log);

            var size = resolutionsFound[0].Value.Split('x');

            VideoWidth = Convert.ToInt32(size[isRotated ? 1 : 0]);
            VideoHeight = Convert.ToInt32(size[isRotated ? 0 : 1]);
            Duration = TimeSpan.ParseExact(timingsFound[0].Value, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);

            //Trim the video a bit, just to make sure that we'll get the frame at the end.
            if (Duration > TimeSpan.Zero && Duration.TotalMilliseconds > Duration.Milliseconds * 2)
                Duration = Duration.Subtract(TimeSpan.FromMilliseconds(Duration.Milliseconds * 2));
        }

        private async Task WhenBothLoaded()
        {
            lock (_lock)
            {
                //Wait for both media players to load, ensuring that only one at time can enter this code block.
                _previewerReady++;

                if (_previewerReady <= 1)
                    return;
            }

            //Get video details.
            if (_lowerPlayer.NaturalVideoWidth > 0 && _lowerPlayer.NaturalVideoHeight > 0)
            {
                VideoWidth = _lowerPlayer.NaturalVideoWidth;
                VideoHeight = _lowerPlayer.NaturalVideoHeight;
            }

            Duration = _lowerPlayer.NaturalDuration.HasTimeSpan ? _lowerPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero;

            //If it was not possible to load the video, warn the user.
            if (VideoWidth <= 10 || VideoHeight <= 10 || Duration.TotalMilliseconds <= 10)
            {
                FaultLoading();
                return;
            }

            //Events used to show the actual frames.
            _lowerPlayer.Changed += LowerPlayer_Changed;
            _upperPlayer.Changed += UpperPlayer_Changed;
            _wasPreviewChangedRegistered = true;

            //If the video was loaded successfully.
            await SucessLoading();
        }

        /// <summary>
        /// If the previewers loading went okay.
        /// </summary>
        private async Task SucessLoading()
        {
            //Size Labels
            WidthTextBlock.Text = VideoWidth.ToString("d", CultureInfo.CurrentUICulture);
            HeightTextBlock.Text = VideoHeight.ToString("d", CultureInfo.CurrentUICulture);

            LowerSelectionImage.Source = _lowerRenderTargetBitmap = new RenderTargetBitmap(VideoWidth, VideoHeight, 96, 96, PixelFormats.Pbgra32);
            UpperSelectionImage.Source = _upperRenderTargetBitmap = new RenderTargetBitmap(VideoWidth, VideoHeight, 96, 96, PixelFormats.Pbgra32);

            SelectionSlider.Maximum = Duration.TotalMilliseconds;
            SelectionSlider.LowerValue = 0;
            SelectionSlider.UpperValue = SelectionSlider.Maximum;

            //Refresh the first previewer manually, since it initially stays at the first frame.
            await RenderPreview();

            if (UserSettings.All.VideoImporter == 0)
                await RenderPreview(false);

            //Update the UI.
            Cursor = Cursors.Arrow;
            ImporterComboBox.IsEnabled = true;
            DetailsGrid.IsEnabled = true;
            SelectionSlider.IsEnabled = true;
            OkButton.IsEnabled = true;
            PreviewerGrid.Opacity = 1;
            LoadingLabel.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Visible;

            MinWidth = Width;
        }

        /// <summary>
        /// If the previewers loading went wrong.
        /// </summary>
        private void FaultLoading(Exception ex = null)
        {
            Cursor = Cursors.Arrow;
            ImporterComboBox.IsEnabled = true;
            LoadingLabel.Visibility = Visibility.Collapsed;

            if (ex != null && !_isDisplayingError)
            {
                _isDisplayingError = true;
                ErrorDialog.Ok(Title, LocalizationHelper.Get("S.ImportVideo.Error"), LocalizationHelper.Get("S.ImportVideo.Error.Detail"), ex);
                _isDisplayingError = false;
            }
        }

        private async Task RenderPreview(bool lower = true)
        {
            try
            {
                StatusBand.Hide();

                var drawingVisual = new DrawingVisual();

                if (lower)
                {
                    using (var dc = drawingVisual.RenderOpen())
                    {
                        if (UserSettings.All.VideoImporter == 0)
                            dc.DrawVideo(_lowerPlayer, new Rect(0, 0, _lowerPlayer.NaturalVideoWidth, _lowerPlayer.NaturalVideoHeight));
                        else
                        {
                            //Capture image from FFmpeg.
                            var image = await GetScreencap();

                            //Render image in target.
                            if (image != null)
                                dc.DrawImage(image, new Rect(0, 0, VideoWidth, VideoHeight));
                        }
                    }

                    lock (_lock)
                        _lowerRenderTargetBitmap.Render(drawingVisual);
                    return;
                }

                using (var dc = drawingVisual.RenderOpen())
                {
                    if (UserSettings.All.VideoImporter == 0)
                        dc.DrawVideo(_upperPlayer, new Rect(0, 0, _upperPlayer.NaturalVideoWidth, _upperPlayer.NaturalVideoHeight));
                    else
                    {
                        //Capture image from FFmpeg.
                        var image = await GetScreencap(false);

                        //Render image in target.
                        if (image != null)
                            dc.DrawImage(image, new Rect(0, 0, VideoWidth, VideoHeight));
                    }
                }

                lock (_lock)
                    _upperRenderTargetBitmap.Render(drawingVisual);
            }
            catch (TimeoutException t)
            {
                LogWriter.Log(t, "Impossible to get the preview of the video.");
                StatusBand.Warning(LocalizationHelper.Get("S.ImportVideo.Timeout"));
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Impossible to get the preview of the video.");
                StatusBand.Error(LocalizationHelper.Get("S.ImportVideo.Error"));
            }
        }

        private async Task<BitmapSource> GetScreencap(bool lower = true, int trial = 0)
        {
            var time = TimeSpan.FromMilliseconds(lower ? SelectionSlider.LowerValue : SelectionSlider.UpperValue); //01:23:45

            var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
            {
                Arguments = $" -ss {time:hh\\:mm\\:ss\\.fff} -i \"{VideoPath}\" -vframes 1 -hide_banner -c:v png -f image2pipe -",
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var pro = Process.Start(process))
            {
                if (pro == null)
                    throw new Exception("It was not possible to start the FFmpeg process.") { HelpLink = process.Arguments };

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = pro.StandardOutput.BaseStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                await Task.Factory.StartNew(() => pro.WaitForExit(5000));

                if (bitmap.Height < 2 || bitmap.Width < 2)
                {
                    var error = pro.StandardError.EndOfStream ? "" : pro.StandardError.ReadToEnd();

                    await Task.Delay(500);

                    if (trial > 7)
                        throw new TimeoutException("Too many attempts in getting the frame preview. " + error);

                    return await GetScreencap(lower, trial + 1);
                }

                return bitmap;
            }
        }

        private void MeasureDuration()
        {
            DurationTextBlock.Text = TimeSpan.FromMilliseconds(SelectionSlider.UpperValue - SelectionSlider.LowerValue).ToString("hh\\:mm\\:ss\\.fff", CultureInfo.CurrentUICulture);
        }

        private int CountFrames()
        {
            var delay = 1000 / FpsIntegerUpDown.Value;
            var timespan = SelectionSlider.UpperValue - SelectionSlider.LowerValue;

            return Convert.ToInt32(timespan / delay);
        }

        private void UpdateProgressBar(int valueLeft)
        {
            CaptureProgressBar.Value = CaptureProgressBar.Maximum - valueLeft;
        }

        private void ImportAndSeek()
        {
            if (_cancelled)
                return;

            lock (_lock)
            {
                var drawingVisual = new DrawingVisual();

                using (var dc = drawingVisual.RenderOpen())
                    dc.DrawVideo(_lowerPlayer, new Rect(0, 0, VideoWidth, VideoHeight));

                _lowerRenderTargetBitmap.Render(drawingVisual);

                //Create an unique file name.
                string fileName;
                do
                    fileName = $"{Frames.Count} {DateTime.Now:yyMMdd-hhmmssffff}.png";
                while (File.Exists(Path.Combine(RootFolder, fileName)));

                //Save the file to disk.
                using (var fileStream = new FileStream(Path.Combine(RootFolder, fileName), FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(_lowerRenderTargetBitmap));
                    encoder.Save(fileStream);
                }

                Frames.Add(new FrameInfo
                {
                    Delay = Delay,
                    Path = Path.Combine(RootFolder, fileName)
                });
            }

            GC.Collect();

            if (!_cancelled)
                SeekNextFrame();
        }

        private void SeekNextFrame()
        {
            //If more frames remain to capture...
            if (_positions.Count > 0)
            {
                //Seek to next position.
                //_lowerPlayer.Position = _positions.Dequeue();
                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    //_lowerPlayer.Changed -= CapturePlayer_Changed;
                    //_lowerPlayer.Position = TimeSpan.Zero;
                    //_lowerPlayer.Changed += CapturePlayer_Changed;
                    _lowerPlayer.Position = _positions.Dequeue();
                }));

                UpdateProgressBar(_positions.Count);
                return;
            }

            _lowerPlayer.Changed -= CapturePlayer_Changed;
            _lowerPlayer.Close();

            GC.Collect();

            DialogResult = true;
        }

        private async Task GetMultipleScreencaps()
        {
            var start = TimeSpan.FromMilliseconds(SelectionSlider.LowerValue);
            var end = TimeSpan.FromMilliseconds(SelectionSlider.UpperValue);
            var fps = FpsIntegerUpDown.Value;
            var count = CountFrames();
            var folder = Path.Combine(RootFolder, "Import");
            var path = Path.Combine(folder, $"%0{count.ToString().Length + 1}d.png");

            try
            {
                //Create temporary folder.
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);

                Directory.CreateDirectory(folder);

                CaptureProgressBar.Value = 0;
                CaptureProgressBar.Maximum = count;

                var info = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
                {
                    Arguments = $" -i \"{VideoPath}\" -vsync 2 -progress pipe:1 -vf scale={VideoWidth}:{VideoHeight} -ss {start:hh\\:mm\\:ss\\.fff} -to {end:hh\\:mm\\:ss\\.fff} -hide_banner -c:v png -r {fps} -vframes {count} \"{path}\"",
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                _process = new Process();
                _process.OutputDataReceived += (sender, e) =>
                {
                    Debug.WriteLine(e.Data);

                    if (string.IsNullOrEmpty(e.Data))
                        return;

                    var parsed = e.Data.Split('=');

                    switch (parsed[0])
                    {
                        case "frame":
                            Dispatcher?.InvokeAsync(() => { CaptureProgressBar.Value = Convert.ToDouble(parsed[1]); });
                            break;

                        case "progress":
                            if (parsed[1] == "end")
                                GetFiles(folder);

                            break;
                    }
                };

                _process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        throw new Exception("Error while capturing frames with FFmpeg.") { HelpLink = $"Command:\n\r{info.Arguments}\n\rResult:\n\r{e.Data}" };
                };

                _process.StartInfo = info;
                _process.Start();
                _process.BeginOutputReadLine();

                await Task.Factory.StartNew(() => _process.WaitForExit());
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "Error importing frames with FFmpeg");
                ClearImportFolder(folder);

                Splitter.Visibility = Visibility.Visible;
                DetailsGrid.Visibility = Visibility.Visible;
                SelectionSlider.IsEnabled = true;
                OkButton.IsEnabled = true;
                StatusLabel.Visibility = Visibility.Collapsed;
                CaptureProgressBar.Visibility = Visibility.Collapsed;

                StatusBand.Error(LocalizationHelper.Get("S.ImportVideo.Error") + Environment.NewLine + e.Message + Environment.NewLine + e.HelpLink);
            }
        }

        private void GetFiles(string folder)
        {
            if (Dispatcher?.Invoke(() => !IsLoaded) ?? false)
                return;

            foreach (var file in Directory.GetFiles(folder, "*.png"))
            {
                //Create an unique file name.
                string newName;
                do
                    newName = Path.Combine(RootFolder, $"{Frames.Count} {DateTime.Now:yyMMdd-hhmmssffff}.png");
                while (File.Exists(newName));

                File.Copy(file, newName);

                Frames.Add(new FrameInfo
                {
                    Delay = Delay,
                    Path = newName
                });
            }

            ClearImportFolder(folder);

            Dispatcher?.Invoke(() => { DialogResult = true; });
        }

        private void ClearImportFolder(string folder)
        {
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        #endregion
    }
}