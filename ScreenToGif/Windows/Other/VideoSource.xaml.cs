using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other
{
    public partial class VideoSource : Window
    {
        #region Variables

        private bool _cancelled;
        private int _playerLoadedCount = 0;
        private MediaPlayer _lowerPlayer = new MediaPlayer { Volume = 0, ScrubbingEnabled = true };
        private MediaPlayer _upperPlayer = new MediaPlayer { Volume = 0, ScrubbingEnabled = true };
        private readonly Queue<TimeSpan> _positions = new Queue<TimeSpan>();

        private int _width;
        private int _height;

        /// <summary>
        /// The generated frame list.
        /// </summary>
        public List<String> FrameList { get; set; }

        /// <summary>
        /// The delay of each frame.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// The scale of the frame.
        /// </summary>
        private double Scale { get; set; }

        private string fileName { get; set; }
        private string pathTemp { get; set; }


        #endregion

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="fileName">The name of the video.</param>
        public VideoSource(string fileName, string pathTemp)
        {
            InitializeComponent();

            Cursor = Cursors.AppStarting;
            this.fileName = fileName;
            this.pathTemp = pathTemp;

            _loadVideoDel = LoadVideoAsync;
            _loadVideoDel.BeginInvoke(new Uri(fileName), LoadFramesCallback, null);
        }

        #region Load

        private delegate bool LoadVideo(Uri file);

        private readonly LoadVideo _loadVideoDel = null;

        private bool LoadVideoAsync(Uri file)
        {
            try
            {
                Dispatcher.InvokeAsync(() =>
                {
                    _upperPlayer.MediaOpened += UpperMediaPlayer_MediaOpened;
                    _upperPlayer.MediaFailed += MediaPlayer_MediaFailed;
                    _upperPlayer.Open(file);
                    _upperPlayer.Pause();

                    _lowerPlayer.MediaOpened += LowerMediaplayer_MediaOpened;
                    _lowerPlayer.MediaFailed += MediaPlayer_MediaFailed;
                    _lowerPlayer.Open(file);
                    _lowerPlayer.Pause();
                });

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Video loading error");
                StatusBand.Error(LocalizationHelper.Get("S.ImportVideo.Error") + Environment.NewLine + ex.Message);
                return false;
            }
        }

        private void LoadFramesCallback(IAsyncResult ar)
        {
            var result = _loadVideoDel.EndInvoke(ar);

            Dispatcher.Invoke(delegate
            {
                if (!result)
                {
                    LoadingLabel.Content = "Something wrong happened... :(";
                    LoadingLabel.Foreground = new SolidColorBrush(Color.FromRgb(120, 20, 20));
                }
            });
        }

        #endregion

        #region Events

        private void LowerMediaplayer_MediaOpened(object sender, EventArgs e)
        {
            Cursor = Cursors.Arrow;

            //Size Labels
            HeightLabel.Content = _lowerPlayer.NaturalVideoHeight;
            WidthLabel.Content = _lowerPlayer.NaturalVideoWidth;

            WhenBothLoaded();
        }

        private void UpperMediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            WhenBothLoaded();
        }

        private void MediaPlayer_MediaFailed(object sender, EventArgs e)
        {
            Cursor = Cursors.Arrow;
            LoadingLabel.Visibility = Visibility.Collapsed;

            StatusBand.Warning(LocalizationHelper.Get("S.ImportVideo.Error"));
        }

        private void SelectionSlider_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            StartNumericUpDown.Value = Convert.ToInt32(SelectionSlider.LowerValue);

            MeasureDuration();
            CountFrames();
        }

        private void SelectionSlider_UpperValueChanged(object sender, RoutedEventArgs e)
        {
            EndNumericUpDown.Value = Convert.ToInt32(SelectionSlider.UpperValue);

            MeasureDuration();
            CountFrames();
        }

        private void StartNumericUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            //_previewVideoDel.BeginInvoke(true, SelectionSlider.LowerValue, PreviewVideoCallback, null);
            _lowerPlayer.Position = TimeSpan.FromMilliseconds(SelectionSlider.LowerValue);

            MeasureDuration();
            CountFrames();
        }

        private void EndNumericUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            //_previewVideoDel.BeginInvoke(false, SelectionSlider.UpperValue, PreviewVideoCallback, null);
            _upperPlayer.Position = TimeSpan.FromMilliseconds(SelectionSlider.UpperValue);

            MeasureDuration();
            CountFrames();
        }

        private void ScaleNumericUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            var height = Convert.ToInt32(_lowerPlayer.NaturalVideoHeight * (ScaleNumericUpDown.Value / 100D));
            var width = Convert.ToInt32(_lowerPlayer.NaturalVideoWidth * (ScaleNumericUpDown.Value / 100D));

            HeightLabel.Content = height;
            WidthLabel.Content = width;
        }

        private void FpsNumericUpDown_ValueChanged(object sender, RoutedEventArgs e)
        {
            CountFrames();
        }

        private void LowerPlayer_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            LowerSelectionImage.Source = PreviewFrame(_lowerPlayer);
        }

        private void UpperPlayer_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            UpperSelectionImage.Source = PreviewFrame(_upperPlayer);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Scale = ScaleNumericUpDown.Value * 0.01f;
            Delay = 1000 / FpsNumericUpDown.Value;
            FrameList = new List<String>();

            _width = (int)Math.Round(_lowerPlayer.NaturalVideoWidth * Scale);
            _height = (int)Math.Round(_lowerPlayer.NaturalVideoHeight * Scale);

            if (CountFrames() == 0)
            {
                //TODO: No frame was selected.
                return;
            }

            #region UI Reaction

            Splitter.Visibility = Visibility.Collapsed;
            DetailsGrid.Visibility = Visibility.Collapsed;
            SelectionSlider.IsEnabled = false;
            OkButton.IsEnabled = false;

            StatusLabel.Visibility = Visibility.Visible;
            CaptureProgressBar.Visibility = Visibility.Visible;

            MinHeight = Height;
            SizeToContent = SizeToContent.Manual;

            #endregion

            CaptureFrames();

            GC.Collect();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelled = true;

            FrameList?.Clear();

            GC.Collect();

            DialogResult = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _lowerPlayer?.Close();
            _upperPlayer?.Close();

            _lowerPlayer = null;
            _upperPlayer = null;

            GC.Collect();
        }

        #endregion

        #region Methods

        private void WhenBothLoaded()
        {
            _playerLoadedCount++;

            if (_playerLoadedCount > 1)
            {
                #region Visibility / IsEnable

                SelectionSlider.IsEnabled = true;
                OkButton.IsEnabled = true;

                Splitter.Visibility = Visibility.Visible;
                DetailsGrid.Visibility = Visibility.Visible;
                LoadingLabel.Visibility = Visibility.Collapsed;

                if (_lowerPlayer.NaturalVideoWidth <= 0 || _upperPlayer.NaturalVideoWidth <= 0 || !_upperPlayer.NaturalDuration.HasTimeSpan)
                {
                    Dialog.Ok(FindResource("ImportVideo.Title") as string, "Impossible to load video", "Looks like a codec is missing, but it may be anything else."); //TODO: Localize.

                    OkButton.IsEnabled = false;
                    return;
                }

                #endregion

                //Events used to show the actual frames.
                _lowerPlayer.Changed += LowerPlayer_Changed;
                _upperPlayer.Changed += UpperPlayer_Changed;

                var param = new Parameters();
                param.Command = $"-i {fileName}";
                var startInfo = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
                {
                    Arguments = param.Command,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };
                Process process = Process.Start(startInfo);
                StreamReader DurationReader = process.StandardError;
                process.WaitForExit();
                var result = DurationReader.ReadToEnd();
                TimeSpan duration = TimeSpan.Parse(System.Text.RegularExpressions.Regex.Replace(result, @"(.|\n)*Duration: (.*?),(.|\n)*", "$2"));

                SelectionSlider.Maximum = duration.TotalMilliseconds;

                LowerSelectionImage.Source = PreviewFrame(_lowerPlayer);
                UpperSelectionImage.Source = PreviewFrame(_upperPlayer);

                SelectionSlider.LowerValue = 0;
                SelectionSlider.UpperValue = SelectionSlider.Maximum;

                StartNumericUpDown.Value = Convert.ToInt32(SelectionSlider.LowerValue);
                EndNumericUpDown.Value = Convert.ToInt32(SelectionSlider.UpperValue);

                MeasureDuration();
                CountFrames();

                Dispatcher.InvokeAsync(() => { LowerSelectionImage.Source = PreviewFrame(_lowerPlayer); });
                Dispatcher.InvokeAsync(() => { UpperSelectionImage.Source = PreviewFrame(_upperPlayer); });

                MinWidth = Width;
            }
        }

        private void MeasureDuration()
        {
            DurationLabel.Content = string.Format(CultureInfo.InvariantCulture, "{0:##0,00} s", SelectionSlider.UpperValue - SelectionSlider.LowerValue);
        }

        private int CountFrames()
        {
            var delay = 1000 / FpsNumericUpDown.Value;
            var timespan = SelectionSlider.UpperValue - SelectionSlider.LowerValue;

            FrameCountLabel.Content = Convert.ToInt32(timespan / delay);

            return Convert.ToInt32(timespan / delay);
        }

        private void UpdateProgressBar(int valueLeft)
        {
            CaptureProgressBar.Value = CaptureProgressBar.Maximum - valueLeft;
        }

        private BitmapFrame PreviewFrame(MediaPlayer player)
        {
            var frame = GenerateFrame(player);

            if (Scale <= 0)
                return frame as BitmapFrame;

            var thumbnailFrame = BitmapFrame.Create(new TransformedBitmap(frame as BitmapSource, new ScaleTransform(Scale, Scale))).GetCurrentValueAsFrozen();

            return thumbnailFrame as BitmapFrame;
        }

        #endregion

        #region Capture Frames Methods/Events


        private void CaptureFrames()
        {
            CaptureProgressBar.Maximum = CountFrames();
            CaptureProgressBar.Value = 0;

            var param = new Parameters();
            var fps = FpsNumericUpDown.Value;
            var startTime = TimeSpan.FromMilliseconds(SelectionSlider.LowerValue);
            var endTime = TimeSpan.FromMilliseconds(SelectionSlider.UpperValue);

            param.Command = "-vsync 2 -i \"{0}\" {1} -y \"{2}\"";
            param.ExtraParameters = $"-r {fps} -vf scale={_width}:{_height} -ss {startTime} -to {endTime} -progress pipe:1";
            param.Filename = Path.Combine(pathTemp, "frame%05d.png");
            param.Command = string.Format(param.Command, fileName, param.ExtraParameters.Replace("{H}", param.Height.ToString()).Replace("{W}", param.Width.ToString()), param.Filename);

            var process = new Process();
            var startInfo = new ProcessStartInfo(UserSettings.All.FfmpegLocation)
            {
                Arguments = param.Command,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    var parsed = e.Data.Split('=');
                    switch (parsed[0])
                    {
                        case "frame":
                            Dispatcher.InvokeAsync(() => { CaptureProgressBar.Value = Convert.ToDouble(parsed[1]); });
                            break;
                        case "progress":
                            if (parsed[1] == "end")
                            {
                                Dispatcher.InvokeAsync(() => {
                                    FrameList = new List<string>(Directory.GetFiles(pathTemp, "*.png"));
                                    DialogResult = true; });
                            }
                            break;
                        default:
                            break;
                    }
                }
            });

            process.StartInfo = startInfo;
            process.Start();
            process.BeginOutputReadLine();
            //process.WaitForExit();
        }


        private Freezable GenerateFrame(MediaPlayer player)
        {
            var target = new RenderTargetBitmap(player.NaturalVideoWidth, player.NaturalVideoHeight, 96, 96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();

            using (var dc = drawingVisual.RenderOpen())
                dc.DrawVideo(player, new Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight));

            target.Render(drawingVisual);

            GC.Collect();

            return BitmapFrame.Create(target).GetCurrentValueAsFrozen();
        }


        #endregion

        private void SelectionSlider_MouseUp(object sender, RoutedEventArgs e)
        {
            StartNumericUpDown.Value = Convert.ToInt32(SelectionSlider.LowerValue);
            EndNumericUpDown.Value = Convert.ToInt32(SelectionSlider.UpperValue);

            MeasureDuration();
            CountFrames();
        }
    }
}