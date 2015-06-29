using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ScreenToGif.Util.Writers;

//When using the NumericUpAndDown first, the slider won't work.

namespace ScreenToGif.Windows.Other
{
    /// <summary>
    /// Interaction logic for VideoSource.xaml
    /// </summary>
    public partial class VideoSource : Window
    {
        #region Variables

        private bool _cancelled;
        private int _playerLoadedCount = 0;
        private readonly MediaPlayer _lowerPlayer = new MediaPlayer() { Volume = 0, ScrubbingEnabled = true };
        private readonly MediaPlayer _upperPlayer = new MediaPlayer() { Volume = 0, ScrubbingEnabled = true };
        private readonly Queue<TimeSpan> _positions = new Queue<TimeSpan>();

        /// <summary>
        /// The generated frame list.
        /// </summary>
        public List<BitmapFrame> FrameList { get; set; }

        /// <summary>
        /// The delay of each frame.
        /// </summary>
        public int Delay { get; set; }

        /// <summary>
        /// The scale of the frame.
        /// </summary>
        private Double Scale { get; set; }

        #endregion

        /// <summary>
        /// Default contructor.
        /// </summary>
        /// <param name="fileName">The name of the video.</param>
        public VideoSource(string fileName)
        {
            InitializeComponent();

            Cursor = Cursors.AppStarting;

            _loadVideoDel = LoadVideoAsync;
            _loadVideoDel.BeginInvoke(new Uri(fileName), LoadFramesCallback, null);
        }

        #region Async

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
                    _upperPlayer.Open(file);
                    _upperPlayer.Pause();

                    _lowerPlayer.MediaOpened += LowerMediaplayer_MediaOpened;
                    _lowerPlayer.Open(file);
                    _lowerPlayer.Pause();
                });

                return true;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Video Loading Error");
                return false;
            }
        }

        private void LoadFramesCallback(IAsyncResult ar)
        {
            bool result = _loadVideoDel.EndInvoke(ar);

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

        private void SelectionSlider_LowerValueChanged(object sender, EventArgs e)
        {
            StartNumericUpDown.Value = Convert.ToInt32(SelectionSlider.LowerValue);

            MeasureDuration();
            CountFrames();
        }

        private void SelectionSlider_UpperValueChanged(object sender, EventArgs e)
        {
            EndNumericUpDown.Value = Convert.ToInt32(SelectionSlider.UpperValue);

            MeasureDuration();
            CountFrames();
        }

        private void StartNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            //_previewVideoDel.BeginInvoke(true, SelectionSlider.LowerValue, PreviewVideoCallback, null);
            _lowerPlayer.Position = TimeSpan.FromMilliseconds(SelectionSlider.LowerValue);
        }

        private void EndNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            //_previewVideoDel.BeginInvoke(false, SelectionSlider.UpperValue, PreviewVideoCallback, null);
            _upperPlayer.Position = TimeSpan.FromMilliseconds(SelectionSlider.UpperValue);
        }

        private void ScaleNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            int height = Convert.ToInt32(_lowerPlayer.NaturalVideoHeight * (ScaleNumericUpDown.Value / 100D));
            int width = Convert.ToInt32(_lowerPlayer.NaturalVideoWidth * (ScaleNumericUpDown.Value / 100D));

            HeightLabel.Content = height;
            WidthLabel.Content = width;
        }

        private void FpsNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            CountFrames();
        }

        private void LowerPlayer_Changed(object sender, EventArgs e)
        {
            LowerSelectionImage.Source = PreviewFrame(_lowerPlayer);
        }

        private void UpperPlayer_Changed(object sender, EventArgs e)
        {
            UpperSelectionImage.Source = PreviewFrame(_upperPlayer);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Scale = 100F/ScaleNumericUpDown.Value;
            Delay = 1000 / FpsNumericUpDown.Value;
            FrameList = new List<BitmapFrame>();

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
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelled = true;

            DialogResult = false;
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

                #endregion

                //Events used to show the actual frames.
                _lowerPlayer.Changed += LowerPlayer_Changed;
                _upperPlayer.Changed += UpperPlayer_Changed;

                SelectionSlider.Maximum = _upperPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;

                LowerSelectionImage.Source = PreviewFrame(_lowerPlayer);
                UpperSelectionImage.Source = PreviewFrame(_upperPlayer);

                SelectionSlider.LowerValue = 0;
                SelectionSlider.UpperValue = SelectionSlider.Maximum;

                Dispatcher.InvokeAsync(() => { LowerSelectionImage.Source =  PreviewFrame(_lowerPlayer); });
                Dispatcher.InvokeAsync(() => { UpperSelectionImage.Source = PreviewFrame(_upperPlayer); });

                MinWidth = Width;
            }
        }

        private void MeasureDuration()
        {
            DurationLabel.Content = String.Format(CultureInfo.InvariantCulture, "{0:##0,00} s", SelectionSlider.UpperValue - SelectionSlider.LowerValue);
        }

        private int CountFrames()
        {
            var delay = 1000 / FpsNumericUpDown.Value;
            var timespan = SelectionSlider.UpperValue - SelectionSlider.LowerValue;

            FrameCountLabel.Content = Convert.ToInt32(timespan / delay);

            return Convert.ToInt32(timespan/delay);
        }

        private void UpdateProgressBar(int valueLeft)
        {
            CaptureProgressBar.Value = CaptureProgressBar.Maximum - valueLeft;
        }

        private BitmapFrame PreviewFrame(MediaPlayer player)
        {
            var frame = GenerateFrame(player);

            if (Scale <= 0)
            {
                return frame as BitmapFrame;
            }

            var thumbnailFrame = BitmapFrame.Create(new TransformedBitmap(frame as BitmapSource, new ScaleTransform(Scale, Scale))).GetCurrentValueAsFrozen();

            return thumbnailFrame as BitmapFrame;
        }

        #endregion

        #region Capture Frames Methods/Events

        private void CapturePlayer_Changed(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() => { CaptureCurrentFrame(_lowerPlayer); }); //TODO: bad async
        }

        private void CaptureFrames()
        {
            _lowerPlayer.Changed -= LowerPlayer_Changed;
            _lowerPlayer.Changed += CapturePlayer_Changed;

            //Calculate all positions.
            for (double span = SelectionSlider.LowerValue + Delay; span <= SelectionSlider.UpperValue; span += Delay)
            {
                _positions.Enqueue(TimeSpan.FromMilliseconds(span));
            }

            CaptureProgressBar.Maximum = _positions.Count;
            CaptureProgressBar.Value = 0;

            CaptureCurrentFrame(_lowerPlayer);
        }

        private void SeekNextFrame()
        {
            if (_cancelled)
                return;

            //If more frames remain to capture...
            if (0 < _positions.Count)
            {
                //Seek to next position and start watchdog timer
                _lowerPlayer.Position = _positions.Dequeue();

                Dispatcher.Invoke(() => { UpdateProgressBar(_positions.Count); });
            }
            else
            {
                _lowerPlayer.Changed -= CapturePlayer_Changed;
                _lowerPlayer.Close();

                //Close this window.
                DialogResult = true;
            }
        }

        private Freezable GenerateFrame(MediaPlayer player)
        {
            var target = new RenderTargetBitmap(player.NaturalVideoWidth, player.NaturalVideoHeight, 96, 96, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();
            
            using (DrawingContext dc = drawingVisual.RenderOpen())
                dc.DrawVideo(player, new Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight));

            target.Render(drawingVisual);

            GC.Collect();

            return BitmapFrame.Create(target).GetCurrentValueAsFrozen();
        }

        private void CaptureCurrentFrame(MediaPlayer player)
        {
            var frame = GenerateFrame(player);

            #region Scaling

            if (Scale <= 0)
            {
                var bit = frame as BitmapFrame;

                if (bit != null)
                    FrameList.Add(bit);
            }

            var thumbnailFrame = BitmapFrame.Create(new TransformedBitmap(frame as BitmapSource, new ScaleTransform(Scale, Scale))).GetCurrentValueAsFrozen();

            var thumbBit = thumbnailFrame as BitmapFrame;

            if (thumbBit != null)
                FrameList.Add(thumbBit);

            #endregion

            SeekNextFrame();

            GC.Collect();
        }

        #endregion
    }
}
