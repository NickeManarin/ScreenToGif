using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models;
using ScreenToGif.Domain.ViewModels;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenToGif.ViewModel;

public class VideoSourceViewModel : BindableBase, IDisposable
{
    private readonly Lock _lock = new();
    private int _previewerReady = 0;
    private readonly MediaPlayer _lowerPlayer = new() { Volume = 0, ScrubbingEnabled = true };
    private readonly MediaPlayer _upperPlayer = new() { Volume = 0, ScrubbingEnabled = true };
    private readonly DebounceDispatcher _debounceDispatcher = new();
    private bool _wasPreviewChangeSubscribed;
    private bool _wasImportChangeSubscribed;
    private Process _process;
    private bool _cancelled;
    private readonly Queue<TimeSpan> _positions = new();

    private bool _isLoading;
    private bool _wasLoaded;
    private bool _isImporting;
    private double _importProgress;
    private double _maximumProgress;
    private double _scale = 100;
    private int _videoImporter;
    private int _originalWidth;
    private int _originalHeight;
    private TimeSpan _originalDuration;
    private int _framerate;
    private int _startMillisecond;
    private int _endMillisecond;
    private RenderTargetBitmap _lowerSelectionImage;
    private RenderTargetBitmap _upperSelectionImage;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetProperty(ref _isLoading, value);

            OnPropertyChanged(nameof(AreControlsEnabled));
        }
    }

    public bool WasLoaded
    {
        get => _wasLoaded;
        set => SetProperty(ref _wasLoaded, value);
    }

    public bool IsImporting
    {
        get => _isImporting;
        set
        {
            SetProperty(ref _isImporting, value);

            OnPropertyChanged(nameof(AreControlsEnabled));
            OnPropertyChanged(nameof(ImportingVisibility));
        }
    }

    public double ImportProgress
    {
        get => _importProgress;
        set => SetProperty(ref _importProgress, value);
    }

    public double MaximumProgress
    {
        get => _maximumProgress;
        set => SetProperty(ref _maximumProgress, value);
    }

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public bool AreControlsEnabled => !IsLoading && !IsImporting;

    public Visibility ImportingVisibility => IsImporting ? Visibility.Visible : Visibility.Collapsed;
    
    public string VideoPath { get; set; }

    public string RootFolder { get; set; }

    public List<IFrame> Frames { get; set; }

    public double Scale
    {
        get => _scale;
        set
        {
            SetProperty(ref _scale, value);

            OnPropertyChanged(nameof(TargetWidth));
            OnPropertyChanged(nameof(TargetHeight));
        }
    }

    public int VideoImporter
    {
        get => _videoImporter;
        set
        {
            if (SetProperty(ref _videoImporter, value))
                _ = LoadPreview();
        }
    }

    public int OriginalWidth
    {
        get => _originalWidth;
        set
        {
            SetProperty(ref _originalWidth, value);

            OnPropertyChanged(nameof(TargetWidth));
        }
    }

    public int OriginalHeight
    {
        get => _originalHeight;
        set
        {
            SetProperty(ref _originalHeight, value);

            OnPropertyChanged(nameof(TargetHeight));
        }
    }

    public TimeSpan OriginalDuration
    {
        get => _originalDuration;
        set
        {
            SetProperty(ref _originalDuration, value);

            OnPropertyChanged(nameof(MaximumMilliseconds));
        }
    }

    public int MaximumMilliseconds => (int)OriginalDuration.TotalMilliseconds;

    public int TargetWidth => (int)(OriginalWidth * Scale / 100D);

    public int TargetHeight => (int)(OriginalHeight * Scale / 100D);

    public int Framerate
    {
        get => _framerate;
        set
        {
            SetProperty(ref _framerate, value);

            OnPropertyChanged(nameof(FrameCount));
        }
    }

    public int Delay => 1000 / Framerate;

    public TimeSpan Duration => TimeSpan.FromMilliseconds(TotalMilliseconds);

    public int TotalMilliseconds => EndMillisecond - StartMillisecond;

    public int FrameCount
    {
        get
        {
            var delay = 1000 / Framerate;

            return Convert.ToInt32(TotalMilliseconds / delay);
        }
    }

    public int StartMillisecond
    {
        get => _startMillisecond;
        set
        {
            if (SetProperty(ref _startMillisecond, value))
            {
                if (!IsLoading)
                {
                    if (VideoImporter == 0)
                    {
                        _lowerPlayer.Position = TimeSpan.FromMilliseconds(value);
                    }
                    else
                    {
                        _debounceDispatcher.SyncTime = DateTime.Now;
                        _debounceDispatcher.Debounce(50, o => _ = RenderPreview(), DispatcherPriority.Input);
                    }
                }

                OnPropertyChanged(nameof(TotalMilliseconds));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(FrameCount));
            }
        }
    }

    public int EndMillisecond
    {
        get => _endMillisecond;
        set
        {
            if (SetProperty(ref _endMillisecond, value))
            {
                if (!IsLoading)
                {
                    if (VideoImporter == 0)
                    {
                        _upperPlayer.Position = TimeSpan.FromMilliseconds(value);
                    }
                    else
                    {
                        _debounceDispatcher.SyncTime = DateTime.Now;
                        _debounceDispatcher.Debounce(50, o => _ = RenderPreview(false), DispatcherPriority.Input);
                    }
                }
                
                OnPropertyChanged(nameof(TotalMilliseconds));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(FrameCount));
            }
        }
    }

    public RenderTargetBitmap LowerSelectionImage
    {
        get => _lowerSelectionImage;
        set => SetProperty(ref _lowerSelectionImage, value);
    }

    public RenderTargetBitmap UpperSelectionImage
    {
        get => _upperSelectionImage;
        set => SetProperty(ref _upperSelectionImage, value);
    }

    //Commands.
    public event EventHandler<object> ShowErrorRequested;
    public event EventHandler<string> ShowWarningRequested;
    public event EventHandler HideErrorRequested;
    public event EventHandler CloseRequested;

    //Methods.
    public void LoadSettings()
    {
        VideoImporter = UserSettings.All.VideoImporter;
        Framerate = UserSettings.All.LatestFpsImport;
    }

    public void SaveSettings()
    {
        UserSettings.All.VideoImporter = VideoImporter;
        UserSettings.All.LatestFpsImport = Framerate;
    }

    public async Task LoadPreview()
    {
        try
        {
            if (VideoImporter == 1 && !PathHelper.IsFfmpegPresent())
            {
                ShowWarningRequested?.Invoke(this, LocalizationHelper.Get("S.Editor.Warning.Ffmpeg"));
                return;
            }

            IsLoading = true;
            LowerSelectionImage = null;
            UpperSelectionImage = null;
            OriginalWidth = 0;
            OriginalHeight = 0;
            
            //MediaPlayer.
            if (VideoImporter == 0)
            {
                //Unregister all events.
                _upperPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                _lowerPlayer.MediaOpened -= MediaPlayer_MediaOpened;

                _upperPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                _lowerPlayer.MediaFailed -= MediaPlayer_MediaFailed;

                //The Change event throws error if not subscribed.
                if (_wasPreviewChangeSubscribed)
                {
                    _upperPlayer.Changed -= UpperPlayer_Changed;
                    _lowerPlayer.Changed -= LowerPlayer_Changed;
                    _wasPreviewChangeSubscribed = false;
                }

                if (_wasImportChangeSubscribed)
                {
                    _lowerPlayer.Changed -= CapturePlayer_Changed;
                    _wasImportChangeSubscribed = false;
                }
                
                //Register new handlers.
                _upperPlayer.MediaOpened += MediaPlayer_MediaOpened;
                _upperPlayer.MediaFailed += MediaPlayer_MediaFailed;
                _lowerPlayer.MediaOpened += MediaPlayer_MediaOpened;
                _lowerPlayer.MediaFailed += MediaPlayer_MediaFailed;

                if (!string.IsNullOrWhiteSpace(_upperPlayer.Source?.AbsoluteUri) && _upperPlayer.Source?.AbsoluteUri == new Uri(VideoPath).AbsoluteUri)
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

                return;
            }

            //FFmpeg.
            await GetVideoDetails();
            await SuccessLoading();
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to load the previewers", "Video source: " + VideoPath);
            ShowErrorRequested?.Invoke(this, LocalizationHelper.Get("S.ImportVideo.Error"));
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

        /*
            Arguments:  -i "C:\Users\user\Desktop\example.mp4" -hide_banner
            Response: Input #0, mov,mp4,m4a,3gp,3g2,mj2, from 'C:\Users\nicke\Desktop\RPReplay_Final1600052642.MP4':
              Metadata:
                major_brand     : mp42
                minor_version   : 1
                compatible_brands: isommp41mp42
                creation_time   : 2020-09-14T03:04:02.000000Z
              Duration: 00:00:09.30, start: 0.000000, bitrate: 18391 kb/s
                Stream #0:0(und): Audio: aac (LC) (mp4a / 0x6134706D), 44100 Hz, stereo, fltp, 2 kb/s (default)
                Metadata:
                  creation_time   : 2020-09-14T03:04:02.000000Z
                  handler_name    : Core Media Audio
                Stream #0:1(und): Video: h264 (High) (avc1 / 0x31637661), yuvj420p(pc, bt709/bt709/unknown), 1440x1920, 18383 kb/s, 59.14 fps, 60 tbr, 600 tbn, 1200 tbc (default)
                Metadata:
                  rotate          : 270
                  creation_time   : 2020-09-14T03:04:02.000000Z
                  handler_name    : Core Media Video
                Side data:
                  displaymatrix: rotation of 90.00 degrees
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
        var isRotated = linesFound2.Count > 0 && (linesFound2[0].Value.Contains("90") || linesFound2[0].Value.Contains("270"));

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

        OriginalWidth = Convert.ToInt32(size[isRotated ? 1 : 0]);
        OriginalHeight = Convert.ToInt32(size[isRotated ? 0 : 1]);
        OriginalDuration = TimeSpan.ParseExact(timingsFound[0].Value, "hh\\:mm\\:ss\\.ff", CultureInfo.InvariantCulture);
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
            OriginalWidth = _lowerPlayer.NaturalVideoWidth;
            OriginalHeight = _lowerPlayer.NaturalVideoHeight;
        }

        OriginalDuration = _lowerPlayer.NaturalDuration.HasTimeSpan ? _lowerPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero;

        //If it was not possible to load the video, warn the user.
        if (OriginalWidth <= 10 || OriginalHeight <= 10 || OriginalDuration.TotalMilliseconds <= 10)
        {
            FaultLoading();
            return;
        }

        //Events used to show the actual frames.
        _lowerPlayer.Changed += LowerPlayer_Changed;
        _upperPlayer.Changed += UpperPlayer_Changed;
        _wasPreviewChangeSubscribed = true;

        //If the video was loaded successfully.
        await SuccessLoading();
    }

    private async Task SuccessLoading()
    {
        LowerSelectionImage = new RenderTargetBitmap(OriginalWidth, OriginalHeight, 96, 96, PixelFormats.Pbgra32);
        UpperSelectionImage = new RenderTargetBitmap(OriginalWidth, OriginalHeight, 96, 96, PixelFormats.Pbgra32);

        StartMillisecond = 0;
        EndMillisecond = MaximumMilliseconds;
        WasLoaded = true;

        //MediaPlayer.
        if (VideoImporter == 0)
        {
            _lowerPlayer.Position = TimeSpan.FromMilliseconds(StartMillisecond);
            _upperPlayer.Position = TimeSpan.FromMilliseconds(EndMillisecond);
        }

        await RenderPreview();
        await RenderPreview(false);

        IsLoading = false;
    }

    private async Task RenderPreview(bool lower = true)
    {
        try
        {
            if (!WasLoaded)
                return;

            var drawingVisual = new DrawingVisual();

            if (lower)
            {
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, OriginalWidth, OriginalHeight));

                    if (VideoImporter == 0)
                        dc.DrawVideo(_lowerPlayer, new Rect(0, 0, _lowerPlayer.NaturalVideoWidth, _lowerPlayer.NaturalVideoHeight));
                    else
                    {
                        //Capture image from FFmpeg.
                        var image = await GetScreencap();

                        //Render image in target.
                        if (image != null)
                            dc.DrawImage(image, new Rect(0, 0, OriginalWidth, OriginalHeight));
                    }
                }

                lock (_lock)
                    LowerSelectionImage.Render(drawingVisual);

                OnPropertyChanged(nameof(LowerSelectionImage));

                return;
            }

            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Red, null, new Rect(0, 0, OriginalWidth, OriginalHeight));

                if (VideoImporter == 0)
                    dc.DrawVideo(_upperPlayer, new Rect(0, 0, _upperPlayer.NaturalVideoWidth, _upperPlayer.NaturalVideoHeight));
                else
                {
                    //Capture image from FFmpeg.
                    var image = await GetScreencap(false);

                    //Render image in target.
                    if (image != null)
                        dc.DrawImage(image, new Rect(0, 0, OriginalWidth, OriginalHeight));
                }
            }


            lock (_lock)
                UpperSelectionImage.Render(drawingVisual);

            OnPropertyChanged(nameof(UpperSelectionImage));
        }
        catch (TimeoutException t)
        {
            LogWriter.Log(t, "Impossible to get the preview of the video.");
            Dispatcher.CurrentDispatcher.Invoke(() => ShowErrorRequested?.Invoke(this, LocalizationHelper.Get("S.ImportVideo.Timeout")));
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to get the preview of the video.");
            Dispatcher.CurrentDispatcher.Invoke(() => ShowErrorRequested?.Invoke(this, LocalizationHelper.Get("S.ImportVideo.Error")));
        }
    }

    private async Task<BitmapSource> GetScreencap(bool lower = true, int trial = 0)
    {
        var endTime = VideoImporter == 1 && MaximumMilliseconds == EndMillisecond ? EndMillisecond - 100 : EndMillisecond;
        var time = TimeSpan.FromMilliseconds(lower ? StartMillisecond : endTime); //01:23:45

        var process = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
        {
            Arguments = $" -ss {time:hh\\:mm\\:ss\\.fff} -i \"{VideoPath}\" -vframes 1 -hide_banner -c:v png -f image2pipe -",
            CreateNoWindow = true,
            ErrorDialog = false,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var pro = Process.Start(process);

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
            var error = pro.StandardError.EndOfStream ? "" : await pro.StandardError.ReadToEndAsync();

            await Task.Delay(500);

            if (trial > 7)
                throw new TimeoutException("Too many attempts in getting the frame preview. " + error);

            return await GetScreencap(lower, trial + 1);
        }

        return bitmap;
    }

    private void FaultLoading(Exception ex = null)
    {
        IsLoading = false;
        WasLoaded = false;

        if (ex != null)
            ShowErrorRequested?.Invoke(this, ex);
    }

    public async Task Import()
    {
        HideErrorRequested?.Invoke(this, EventArgs.Empty);

        GC.Collect();

        //MediaPlayer.
        if (VideoImporter == 0)
        {
            //Calculate all positions.
            for (var span = StartMillisecond + Delay; span <= EndMillisecond; span += Delay)
                _positions.Enqueue(TimeSpan.FromMilliseconds(span));

            ImportProgress = 0;
            MaximumProgress = _positions.Count;
            Frames = [];
            IsImporting = true;

            _lowerPlayer.Changed -= LowerPlayer_Changed;
            _upperPlayer.Changed -= UpperPlayer_Changed;
            _wasPreviewChangeSubscribed = false;

            _lowerPlayer.Changed += CapturePlayer_Changed;
            _wasImportChangeSubscribed = true;

            //Resize the rendering to fit in the selected scale. With this code, the preview stops working.
            LowerSelectionImage = new RenderTargetBitmap(TargetWidth, TargetHeight, 96, 96, PixelFormats.Pbgra32);

            ImportAndSeek();
            return;
        }

        //Import via ffmpeg.
        await GetMultipleScreencaps();
    }

    private async Task GetMultipleScreencaps()
    {
        var start = TimeSpan.FromMilliseconds(StartMillisecond);
        var end = TimeSpan.FromMilliseconds(EndMillisecond);
        var folder = Path.Combine(RootFolder, "Import");
        var path = Path.Combine(folder, $"%0{FrameCount.ToString().Length + 1}d.png");

        try
        {
            //Create temporary folder.
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            Directory.CreateDirectory(folder);

            ImportProgress = 0;
            MaximumProgress = FrameCount;
            Frames = [];
            IsImporting = true;

            var info = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
            {
                Arguments = $" -i \"{VideoPath}\" -progress pipe:1 -vf scale={TargetWidth}:{TargetHeight} -ss {start:hh\\:mm\\:ss\\.fff} -to {end:hh\\:mm\\:ss\\.fff} -hide_banner -flush_packets 1 -stats_period 1 -c:v png -r {Framerate} -vframes {FrameCount} \"{path}\"",
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _process = new Process();
            _process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                var parsed = e.Data.Split('=');

                switch (parsed[0])
                {
                    case "frame":
                        ImportProgress = Convert.ToDouble(parsed[1]);
                        break;

                    case "progress":
                        if (parsed[1] == "end")
                            GetFiles(folder);
                        break;
                }
            };

            _process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                
                var match = Regex.Match(e.Data, @"frame=\s*(\d+)");

                if (match.Success)
                {
                    var current = Convert.ToDouble(match.Groups[1].Value);

                    ImportProgress = current;

                    if (Math.Abs(current - FrameCount) < double.Epsilon)
                        GetFiles(folder);
                }
            };
            
            _process.StartInfo = info;
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            
            await _process.WaitForExitAsync();

            if (_process == null)
                return;

            var error = await _process?.StandardError?.ReadToEndAsync();

            if (!string.IsNullOrWhiteSpace(error))
                throw new Exception("Error while capturing frames with FFmpeg.") { HelpLink = $"Command:\n\r{info.Arguments}\n\rResult:\n\r{error}" };
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Error importing frames with FFmpeg");

            IsImporting = false;

            Dispatcher.CurrentDispatcher.Invoke(() => ShowErrorRequested?.Invoke(this, LocalizationHelper.Get("S.ImportVideo.Error") + Environment.NewLine + e.Message + Environment.NewLine + e.HelpLink));
        }
    }

    private void ImportAndSeek()
    {
        if (_cancelled)
            return;

        lock (_lock)
        {
            var drawingVisual = new DrawingVisual();

            using (var dc = drawingVisual.RenderOpen())
                dc.DrawVideo(_lowerPlayer, new Rect(0, 0, TargetWidth, TargetHeight));

            _lowerSelectionImage.Render(drawingVisual);

            //Create a unique file name.
            string fileName;
            do
                fileName = $"{Frames.Count} {Guid.NewGuid():N}.png";
            while (File.Exists(Path.Combine(RootFolder, fileName)));

            //Save the file to disk.
            using (var fileStream = new FileStream(Path.Combine(RootFolder, fileName), FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(_lowerSelectionImage));
                encoder.Save(fileStream);
            }
            
            Frames.Add(new Frame
            {
                Delay = Delay,
                Path = Path.Combine(RootFolder, fileName)
            });
        }
        
        if (!_cancelled)
            SeekNextFrame();
    }

    private void SeekNextFrame()
    {
        //If more frames remain to capture...
        if (_positions.Count > 0)
        {
            //Seek to next position.
            _lowerPlayer.Position = _positions.Dequeue();

            ImportProgress = MaximumProgress - _positions.Count;
            return;
        }

        _lowerPlayer.Changed -= CapturePlayer_Changed;
        _wasImportChangeSubscribed = false;
        _lowerPlayer.Close();

        GC.Collect();

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void GetFiles(string folder)
    {
        if (!IsImporting)
            return;

        IsImporting = false;

        foreach (var file in Directory.GetFiles(folder, "*.png"))
        {
            //Create a unique file name.
            string newName;
            do
                newName = Path.Combine(RootFolder, $"{Frames.Count} {Guid.NewGuid():N}.png");
            while (File.Exists(newName));

            File.Copy(file, newName);

            Frames.Add(new Frame
            {
                Delay = Delay,
                Path = newName
            });
        }
        
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task Cancel()
    {
        _cancelled = true;

        //MediaPlayer.
        _process?.Kill();
        await Task.Delay(200);

        Frames?.Clear();
        GC.Collect();
    }

    public void RemoveImportFiles()
    {
        try
        {
            var path = Path.Combine(RootFolder, "Import");

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Impossible to delete imported frames.");
        }
    }

    //Events.
    private void MediaPlayer_MediaOpened(object sender, EventArgs e)
    {
        _ = WhenBothLoaded();
    }

    private void MediaPlayer_MediaFailed(object sender, ExceptionEventArgs e)
    {
        FaultLoading(e.ErrorException);
    }

    private void LowerPlayer_Changed(object sender, EventArgs e)
    {
        _ = RenderPreview();
    }

    private void UpperPlayer_Changed(object sender, EventArgs e)
    {
        _ = RenderPreview(false);
    }

    private void CapturePlayer_Changed(object sender, EventArgs e)
    {
        ImportAndSeek();
    }
    
    public void Dispose()
    {
        _lowerPlayer.Close();
        _upperPlayer.Close();

        _process?.Dispose();
    }
}
