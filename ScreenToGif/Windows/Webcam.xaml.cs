using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
//using System.Windows.Interop;
//using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenToGif.Model;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Webcam.DirectX;
using ScreenToGif.Windows.Other;
//using SharpDX;
//using SharpDX.Direct3D;
//using SharpDX.Direct3D9;
//using SharpDX.DXGI;
//using SharpDX.MediaFoundation;
//using SharpDX.Win32;
using Timer = System.Windows.Forms.Timer;

namespace ScreenToGif.Windows
{
    public partial class Webcam
    {
        #region Variables

        private Filters _filters;

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Counters

        /// <summary>
        /// The numbers of frames, this is updated while recording.
        /// </summary>
        private int _frameCount = 0;

        #endregion

        private Timer _timer = new Timer();

        /// <summary>
        /// The DPI of the current screen.
        /// </summary>
        private double _scale = 1;

        /// <summary>
        /// The amount of pixels of the window border. Width.
        /// </summary>
        private int _offsetX;

        /// <summary>
        /// The amout of pixels of the window border. Height.
        /// </summary>
        private int _offsetY;

        /// <summary>
        /// True if the BackButton should be hidden.
        /// </summary>
        private readonly bool _hideBackButton;

        #endregion

        #region Async Load

        public delegate List<string> Load();

        private Load _loadDel;

        /// <summary>
        /// Loads the list of video devices.
        /// </summary>
        private List<string> LoadVideoDevices()
        {
            var devicesList = new List<string>();
            _filters = new Filters();

            for (var i = 0; i < _filters.VideoInputDevices.Count; i++)
            {
                devicesList.Add(_filters.VideoInputDevices[i].Name);
            }

            return devicesList;
        }

        private void LoadCallBack(IAsyncResult r)
        {
            var result = _loadDel.EndInvoke(r);

            #region If no devices detected

            if (result.Count == 0)
            {
                Dispatcher.Invoke(() =>
                {
                    RecordPauseButton.IsEnabled = false;
                    FpsNumericUpDown.IsEnabled = false;
                    VideoDevicesComboBox.IsEnabled = false;

                    WebcamControl.Visibility = Visibility.Collapsed;
                    NoVideoLabel.Visibility = Visibility.Visible;
                });

                return;
            }

            #endregion

            #region Detected at least one device

            Dispatcher.Invoke(() =>
            {
                VideoDevicesComboBox.ItemsSource = result;
                VideoDevicesComboBox.SelectedIndex = 0;

                RecordPauseButton.IsEnabled = true;
                FpsNumericUpDown.IsEnabled = true;
                VideoDevicesComboBox.IsEnabled = true;

                WebcamControl.Visibility = Visibility.Visible;
                NoVideoLabel.Visibility = Visibility.Collapsed;

                _actHook.Start(false, true); //false for the mouse, true for the keyboard.
            });

            #endregion
        }

        #endregion

        #region Inicialization

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Webcam(bool hideBackButton = true)
        {
            InitializeComponent();

            _hideBackButton = hideBackButton;

            //Load.
            _timer.Tick += Normal_Elapsed;

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
            }
            catch (Exception) { }

            #endregion
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hideBackButton)
                BackButton.Visibility = Visibility.Collapsed;

            SystemEvents.PowerModeChanged += System_PowerModeChanged;

            #region DPI

            var source = PresentationSource.FromVisual(this);

            if (source?.CompositionTarget != null)
                _scale = source.CompositionTarget.TransformToDevice.M11;
            
            #endregion

            #region Window Offset

            //Gets the window chrome offset
            _offsetX = (int)Math.Round((ActualWidth - ((Grid)Content).ActualWidth) / 2);
            _offsetY = (int)Math.Round((ActualHeight - ((Grid)Content).ActualHeight) - _offsetX);

            #endregion

            //TryLoad1();
            //TryLoad2();
            
            _loadDel = LoadVideoDevices;
            _loadDel.BeginInvoke(LoadCallBack, null);
        }

        private void TryLoad2()
        {
            //List all media sources.
            //Select one media source.
            //List all streams.
            //Select one stream.
            //Get stream properties.
            //Read sample.
            //Parse sample.
            //Save sample as frame.

            //var attributes = new MediaAttributes(1);
            //attributes.Set(CaptureDeviceAttributeKeys.SourceType.Guid, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);

            //var mediaSource = MediaFactory.EnumDeviceSources(attributes)[0].ActivateObject<MediaSource>();
            //mediaSource.CreatePresentationDescriptor(out var presentationDescriptor);

            //var reader = new SourceReader(mediaSource);
            //var mediaTypeIndex = 0;

            //int width, height;
            
            //using (var mt = reader.GetNativeMediaType(0, mediaTypeIndex))
            //{
            //    UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameSize), out  width, out  height);
            //    UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameRate), out var frameRateNumerator, out var frameRateDenominator);
            //    UnpackLong(mt.Get(MediaTypeAttributeKeys.PixelAspectRatio), out var aspectRatioNumerator, out var aspectRatioDenominator);
            //}

            //Other way.
            var VideoReady = true;
            int streamIndex = 0;
            while (!VideoReady)
            {
                //var nativeMediaType = reader.GetNativeMediaType(streamIndex, 0);
                //var currentMediaType = reader.GetCurrentMediaType(streamIndex);
                //var outputMediaType = new MediaType();

                //reader.SetStreamSelection(streamIndex, true);

                //if (nativeMediaType.MajorType == MediaTypeGuids.Video)
                //{
                //    var VideoStreamIndex = streamIndex;

                //    var VideoSubType = currentMediaType.Get<Guid>(MediaTypeAttributeKeys.Subtype);
                //    UnpackLong(currentMediaType.Get(MediaTypeAttributeKeys.FrameSize), out var VideoWidth, out var VideoHeight);
                //    UnpackLong(currentMediaType.Get(MediaTypeAttributeKeys.FrameRate), out var VideoFrameRateNumerator, out var VideoFrameRateDenominator);
                //    UnpackLong(currentMediaType.Get(MediaTypeAttributeKeys.PixelAspectRatio), out var VideoAspectRatioNumerator, out var VideoAspectRatioDenominator);
                //    var VideoInterlaceMode = (VideoInterlaceMode)(currentMediaType.Get(MediaTypeAttributeKeys.InterlaceMode));

                //    MediaFactory.CreateMediaType(outputMediaType);
                //    outputMediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                //    outputMediaType.Set(MediaTypeAttributeKeys.Subtype, new Guid(22, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71));
                //    outputMediaType.Set(MediaTypeAttributeKeys.FrameSize.Guid, PackLong(VideoWidth, VideoHeight));
                //    outputMediaType.Set(MediaTypeAttributeKeys.FrameRate.Guid, PackLong(VideoFrameRateNumerator, VideoFrameRateDenominator));
                //    outputMediaType.Set(MediaTypeAttributeKeys.InterlaceMode, (int)VideoInterlaceMode.Progressive);
                //    outputMediaType.Set(MediaTypeAttributeKeys.PixelAspectRatio, PackLong(1, 1));

                //    reader.SetCurrentMediaType(streamIndex, outputMediaType);

                    VideoReady = true;
                //}

                //outputMediaType.Dispose();

                streamIndex++;
            }


            //var sample = reader.ReadSample(SourceReaderIndex.AnyStream, SourceReaderControlFlags.None, out var readStreamIndex, out var readFlags, out var timestamp);
            
            //if (sample == null)
            //    sample = reader.ReadSample(SourceReaderIndex.AnyStream, SourceReaderControlFlags.None, out readStreamIndex, out readFlags, out timestamp);


            //var sourceBuffer = sample.GetBufferByIndex(0); // sample.ConvertToContiguousBuffer();
            //var sourcePointer = sourceBuffer.Lock(out var maxLength, out var currentLength);

            //var data = new byte[sample.TotalLength];
            //Marshal.Copy(sourcePointer, data, 0, sample.TotalLength);

            //var newData = new byte[width * 4 * height];

            //var partWidth = width / 4;
            //var partHeight = height / 3;

            //for (var i = 0; i < sample.TotalLength; i += 4)
            //{
            //    //X8R8B8G8 -> BGRA = 4
            //    newData[i] = data[i + 3];
            //    newData[i + 1] = data[i + 2];
            //    newData[i + 2] = data[i + 1];
            //    newData[i + 3] = 255; //data[i];
            //}

            ////var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, ((width * 24 + 31) / 32) * 4);
            //var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, newData, width * 4);

            //sourceBuffer.Unlock();
            //sourceBuffer.Dispose();

            //var device = new SharpDX.Direct3D11.Device(DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.VideoSupport);
            //var texture = new SharpDX.Direct3D11.Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription
            //{
            //    Usage = SharpDX.Direct3D11.ResourceUsage.Default,
            //    BindFlags = SharpDX.Direct3D11.BindFlags.None,
            //    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read,
            //    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            //    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
            //    ArraySize = 1,
            //    Height = height,
            //    Width = width,
            //    MipLevels = 1,
            //    SampleDescription = new SampleDescription(1, 0),
            //});




            //data needs: 
            //Height * Stride * Format.BitsPerPixel/8

            //var device = new SharpDX.Direct3D9.Device(new Direct3D(), 0, DeviceType.Hardware, new WindowInteropHelper(this).Handle, CreateFlags.SoftwareVertexProcessing, new SharpDX.Direct3D9.PresentParameters(width, height));
            //var videoTexture = new SharpDX.Direct3D9.Texture(device, width, height, 1, SharpDX.Direct3D9.Usage.Dynamic, SharpDX.Direct3D9.Format.X8R8G8B8, SharpDX.Direct3D9.Pool.Default);
            //var lockData = videoTexture.LockRectangle(0, SharpDX.Direct3D9.LockFlags.None);
            //byte[] data = new byte[width * height * 4];
            //for (int k = 0; k < width * height * 4;)
            //{
            //    // Fill with black
            //    data[k++] = 0x00;
            //    data[k++] = 0x00;
            //    data[k++] = 0x00;
            //    data[k++] = 0xff;
            //}

            //Marshal.Copy(data, 0, lockData.DataPointer, width * height * 4);
            //videoTexture.UnlockRectangle(0);

            //var sourceBuffer = sample.ConvertToContiguousBuffer();
            //var sourcePointer = sourceBuffer.Lock(out var maxLength, out var currentLength);
            //var destRect = videoTexture.LockRectangle(0, SharpDX.Direct3D9.LockFlags.None);
            //var destPointer = destRect.DataPointer;

            //Native.MemoryCopy(destPointer, sourcePointer, (UIntPtr)sourceBuffer.MaxLength);

            //Marshal.Copy(lockData.DataPointer, data, 0, width * height * 4);
            //var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, width * 4);


            //videoTexture.UnlockRectangle(0);

            //sourceBuffer.Unlock();
            //sourceBuffer.Dispose();



            //for (int d = 0; d < presentationDescriptor.StreamDescriptorCount; d++)
            //{
            //    presentationDescriptor.GetStreamDescriptorByIndex(d, out var isSelected, out var streamDescriptor);

            //    for (int i = 0; i < streamDescriptor.MediaTypeHandler.MediaTypeCount; i++)
            //    {
            //        var type = streamDescriptor.MediaTypeHandler.GetMediaTypeByIndex(i);

            //        if (type.MajorType == MediaTypeGuids.Video)
            //        {
            //            var v = type.QueryInterface<VideoMediaType>();
            //            // contains always empty values
            //            var x = v.VideoFormat;
            //        }
            //    }
            //}
        }

        private long PackLong(int left, int right)
        {
            return (long)left << 32 | (uint)right;
        }

        private void UnpackLong(long value, out int left, out int right)
        {
            left = (int)(value >> 32);
            right = (int)(value & 0xffffffffL);
        }

        //private static MediaSource _mediaSource = null;

        //private void TryLoad1()
        //{
        //    var attributes = new MediaAttributes(1);
        //    attributes.Set<Guid>(CaptureDeviceAttributeKeys.SourceType, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);
        //    var activates = MediaFactory.EnumDeviceSources(attributes);

        //    var dic = new Dictionary<string, Activate>();
        //    foreach (var activate in activates)
        //    {
        //        var uid = activate.Get(CaptureDeviceAttributeKeys.SourceTypeVidcapSymbolicLink);
        //        dic.Add(uid, activate);
        //    }

        //    var camera = dic.First().Value;

        //    //"279a808d-aec7-40c8-9c6b-a6b492c78a66" is MediaSource class GUID
        //    camera.ActivateObject(System.Guid.Parse("279a808d-aec7-40c8-9c6b-a6b492c78a66"), out var vOut);

        //    _mediaSource = new MediaSource(vOut);
        //    _mediaSource.CreatePresentationDescriptor(out var descriptor);

        //    var time = new Variant();
        //    time.ElementType = VariantElementType.UInt; //Is this VT_I8 ??
        //    time.Value = 0; //Value = 0

        //    //var callback = new MFCallback(this, mediaSource);
        //    var callback = new EventCallback(null, vOut);
        //    _mediaSource.BeginGetEvent(callback, null);
        //    _mediaSource.Start(descriptor, null, time);
        //}

        //public class EventCallback : CppObject, IAsyncCallback
        //{
        //    public EventCallback(object iunknowObject, IntPtr pointer)
        //    {
        //        //this.NativePointer = Marshal.GetIUnknownForObject(iunknowObject);
        //        this.NativePointer = pointer;
        //    }

        //    public IDisposable Shadow { get; set; }

        //    public void Invoke(AsyncResult asyncResultRef)
        //    {
        //        var ev = _mediaSource.EndGetEvent(asyncResultRef);

        //         switch (ev.TypeInfo)
        //         {
        //             case MediaEventTypes.SessionEnded:
        //                 //_sessionState = SessionState.Ended;
        //                //OnSongFinishedPlaying(null, null);
        //                break;
                    
        //            case MediaEventTypes.SessionTopologyStatus:
        //                 //if (ev.Get(EventAttributeKeys.TopologyStatus) == TopologyStatus.Ready)
        //                //    OnTopologyReady();
        //                break;
                    
        //            case MediaEventTypes.SessionStopped:
        //                 //OnSessionStopped();
        //                break;
        //         }

        //        _mediaSource.BeginGetEvent(this, null);
        //    }

        //    public AsyncCallbackFlags Flags { get; }

        //    public WorkQueueId WorkQueueId { get; }

        //    public Result QueryInterface(ref Guid guid, out IntPtr comObject)
        //    {
        //        return (Result)Marshal.QueryInterface(this.NativePointer, ref guid, out comObject);
        //    }

        //    public int AddReference()
        //    {
        //        if (this.NativePointer == IntPtr.Zero)
        //            throw new InvalidOperationException("COM Object pointer is null");

        //        return Marshal.AddRef(this.NativePointer);
        //    }

        //    public int Release()
        //    {
        //        if (this.NativePointer == IntPtr.Zero)
        //            throw new InvalidOperationException("COM Object pointer is null");

        //        return Marshal.Release(this.NativePointer);
        //    }
        //}

        //class MFCallback : ComObject, IAsyncCallback
        //{
        //    MediaSource _session;
        //    Webcam _player;

        //    public MFCallback(Webcam player, MediaSource _session)
        //    {
        //        this._session = _session;
        //        _player = player;
        //        Disposed += (sender, args) => disposed = true;
        //    }

        //    bool disposed = false;

        //    public IDisposable Shadow { get; set; }

        //    public void Invoke(AsyncResult asyncResultRef)
        //    {
        //        if (disposed)
        //            return;

        //        try
        //        {
        //            var ev = _session.EndGetEvent(asyncResultRef);

        //            if (disposed)
        //                return;

        //            if (ev.TypeInfo == MediaEventTypes.SessionTopologySet)
        //            {
        //               // _player.Begin();
        //            }

        //            //if (ev.TypeInfo == MediaEventTypes.SessionEnded)
        //            //    _player.Playing = false;

        //            _session.BeginGetEvent(this, null);
        //        }
        //        catch (Exception)
        //        {

        //        }
        //    }

        //    public AsyncCallbackFlags Flags { get; private set; }

        //    public WorkQueueId WorkQueueId { get; private set; }
        //}

        #endregion

        #region Hooks

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, CustomKeyEventArgs e)
        {
            if (!IsActive)
                return;

            if (Stage != Stage.Discarding && Keyboard.Modifiers.HasFlag(UserSettings.All.StartPauseModifiers) && e.Key == UserSettings.All.StartPauseShortcut)
                RecordPauseButton_Click(null, null);
            else if (Keyboard.Modifiers.HasFlag(UserSettings.All.StopModifiers) && e.Key == UserSettings.All.StopShortcut)
                Stop_Executed(null, null);
            else if ((Stage == Stage.Paused || Stage == Stage.Snapping) && Keyboard.Modifiers.HasFlag(UserSettings.All.DiscardModifiers) && e.Key == UserSettings.All.DiscardShortcut)
                DiscardButton_Click(null, null);
        }

        #endregion

        #region Other Events

        private void VideoDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (VideoDevicesComboBox.SelectedIndex == -1)
                {
                    WebcamControl.VideoDevice = null;
                    return;
                }

                WebcamControl.VideoDevice = _filters.VideoInputDevices[VideoDevicesComboBox.SelectedIndex];
                WebcamControl.Refresh();

                if (WebcamControl.VideoWidth > 0)
                {
                    Width = WebcamControl.VideoWidth * _scale / 2;
                    Height = (WebcamControl.VideoHeight + 31) * _scale / 2;
                }

                if (Top < 0)
                    Top = 0;

                if (Left < 0)
                    Left = 0;
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Video device not supported");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
                return;

            Width = WebcamControl.VideoWidth * _scale * ScaleSlider.Value;
            Height = (WebcamControl.VideoHeight + 31) * _scale * ScaleSlider.Value;

            if (Top < 0)
                Top = 0;

            if (Left < 0)
                Left = 0;
        }

        private void System_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                if (Stage == Stage.Recording)
                    RecordPauseButton_Click(null, null);
                else if (Stage == Stage.PreStarting)
                    Stop_Executed(null, null);

                GC.Collect();
            }
        }

        private async void Window_LocationChanged(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(UpdateScreenDpi);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _actHook.Stop(); //Stop the user activity watcher.
            }
            catch (Exception) { }

            SystemEvents.PowerModeChanged -= System_PowerModeChanged;
        }

        #endregion

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk and adds the filename in the list of frames.
        /// </summary>
        /// <param name="filename">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        public delegate void AddFrame(string filename, Bitmap bitmap);

        private AddFrame _addDel;

        private void AddFrames(string filename, Bitmap bitmap)
        {
            bitmap.Save(filename);
            bitmap.Dispose();
        }

        public delegate void AddRenderFrame(string filename, RenderTargetBitmap bitmap);

        private AddRenderFrame _addRenderDel;

        private void AddRenderFrames(string filename, RenderTargetBitmap bitmap)
        {
            var bitmapEncoder = new BmpBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var filestream = new FileStream(filename, FileMode.Create))
                bitmapEncoder.Save(filestream);
        }

        private void CallBack(IAsyncResult r)
        {
            //if (!this.IsLoaded) return;

            _addDel.EndInvoke(r);
        }

        #endregion

        #region Discard Async

        private delegate void DiscardFrames();

        private DiscardFrames _discardFramesDel;

        private void Discard()
        {
            #region Remove all the files

            foreach (var frame in Project.Frames)
            {
                try
                {
                    File.Delete(frame.Path);
                }
                catch (Exception)
                { }
            }

            try
            {
                Directory.Delete(Project.FullPath, true);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Delete Temp Path");
            }

            #endregion

            Project.Frames.Clear();
        }

        private void DiscardCallback(IAsyncResult ar)
        {
            _discardFramesDel.EndInvoke(ar);

            Dispatcher.Invoke(() =>
            {
                //Enables the controls that are disabled while recording;
                FpsNumericUpDown.IsEnabled = true;
                RefreshButton.IsEnabled = true;
                VideoDevicesComboBox.IsEnabled = true;
                LowerGrid.IsEnabled = true;

                DiscardButton.BeginStoryboard(FindResource("HideDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                Cursor = Cursors.Arrow;

                //if (!UserSettings.All.SnapshotMode)
                {
                    //Only display the Record text when not in snapshot mode. 
                    Title = "ScreenToGif";
                    Stage = Stage.Stopped;
                }
                //else
                {
                    //Stage = Stage.Snapping;
                    //EnableSnapshot_Executed(null, null);
                }

                GC.Collect();
            });

            GC.Collect();
        }

        #endregion

        #region Timer

        private void Normal_Elapsed(object sender, EventArgs e)
        {
            var fileName = $"{Project.FullPath}{_frameCount}.png";
            Project.Frames.Add(new FrameInfo(fileName, _timer.Interval));

            //Get the actual position of the form.
            var lefttop = Dispatcher.Invoke(() => new System.Drawing.Point((int)Math.Round((Left + _offsetX) * _scale, MidpointRounding.AwayFromZero),
                (int)Math.Round((Top + _offsetY) * _scale, MidpointRounding.AwayFromZero)));

            //Take a screenshot of the area.
            var bt = Native.Capture((int)Math.Round(WebcamControl.ActualWidth * _scale, MidpointRounding.AwayFromZero),
                (int)Math.Round(WebcamControl.ActualHeight * _scale, MidpointRounding.AwayFromZero), lefttop.X, lefttop.Y);

            _addDel.BeginInvoke(fileName, new Bitmap(bt), null, null); //CallBack
            //_addDel.BeginInvoke(fileName, new Bitmap(WebcamControl.Capture.GetFrame()), CallBack, null);
            //_addRenderDel.BeginInvoke(fileName, WebcamControl.GetRender(this.Dpi(), new System.Windows.Size()), CallBack, null);

            //ThreadPool.QueueUserWorkItem(delegate { AddFrames(fileName, new Bitmap(_capture.GetFrame())); });

            Dispatcher.Invoke(() => Title = $"ScreenToGif • {_frameCount}");

            _frameCount++;
            GC.Collect(1);
        }

        #endregion

        #region Click Events

        private void BackButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ScaleButton_Click(object sender, RoutedEventArgs e)
        {
            ScalePopup.IsOpen = true;
        }

        private void RecordPauseButton_Click(object sender, RoutedEventArgs e)
        {
            WebcamControl.Capture.PrepareCapture();

            if (Stage == Stage.Stopped)
            {
                #region To Record

                _timer = new Timer { Interval = 1000 / FpsNumericUpDown.Value };

                Project = new ProjectInfo().CreateProjectFolder(ProjectByType.WebcamRecorder);

                RefreshButton.IsEnabled = false;
                VideoDevicesComboBox.IsEnabled = false;
                FpsNumericUpDown.IsEnabled = false;
                Topmost = true;

                _addDel = AddFrames;
                _addRenderDel = AddRenderFrames;
                //WebcamControl.Capture.GetFrame();

                #region Start - Normal or Snap

                if (UserSettings.All.CaptureFrequency != CaptureFrequency.Manual)
                {
                    #region Normal Recording

                    _timer.Tick += Normal_Elapsed;
                    Normal_Elapsed(null, null);
                    _timer.Start();

                    Stage = Stage.Recording;

                    #endregion
                }
                else
                {
                    #region SnapShot Recording

                    Stage = Stage.Snapping;
                    Title = "ScreenToGif - " + LocalizationHelper.Get("S.Recorder.Snapshot");

                    Normal_Elapsed(null, null);

                    #endregion
                }

                #endregion

                #endregion
            }
            else if (Stage == Stage.Recording)
            {
                #region To Pause

                Stage = Stage.Paused;
                Title = LocalizationHelper.Get("S.Recorder.Paused");

                DiscardButton.BeginStoryboard(FindResource("ShowDiscardStoryboard") as Storyboard, HandoffBehavior.Compose);

                _timer.Stop();

                #endregion
            }
            else if (Stage == Stage.Paused)
            {
                #region To Record Again

                Stage = Stage.Recording;
                Title = "ScreenToGif";

                _timer.Start();

                #endregion
            }
            else if (Stage == Stage.Snapping)
            {
                #region Take Screenshot

                Normal_Elapsed(null, null);

                #endregion
            }
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _frameCount = 0;
            Stage = Stage.Stopped;

            Cursor = Cursors.AppStarting;
            LowerGrid.IsEnabled = false;

            _discardFramesDel = Discard;
            _discardFramesDel.BeginInvoke(DiscardCallback, null);
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Project != null && Project.Frames.Count > 0;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                _frameCount = 0;

                _timer.Stop();

                if (Stage != Stage.Stopped && Stage != Stage.PreStarting && Project.Any)
                {
                    //If not Already Stoped nor Pre Starting and FrameCount > 0, Stops
                    Close();
                }
                else if ((Stage == Stage.PreStarting || Stage == Stage.Snapping) && !Project.Any)
                {
                    #region if Pre-Starting or in Snapmode and no Frames, Stops

                    Stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    FpsNumericUpDown.IsEnabled = true;
                    RecordPauseButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    VideoDevicesComboBox.IsEnabled = true;
                    Topmost = true;

                    Title = "ScreenToGif";

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                LogWriter.Log(nll, "NullPointer in the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", nll.Message, nll);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error in the Stop function");

                ErrorDialog.Ok("ScreenToGif", "Error while stopping", ex.Message, ex);
            }
        }

        private void NotRecording_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting && LowerGrid.IsEnabled;
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var options = new Options();
            options.ShowDialog();

            Topmost = true;
        }

        private void CheckVideoDevices_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RecordPauseButton.IsEnabled = false;

            VideoDevicesComboBox.ItemsSource = null;

            //Check again for video devices.
            _loadDel = LoadVideoDevices;
            _loadDel.BeginInvoke(LoadCallBack, null);
        }

        #endregion

        private void UpdateScreenDpi()
        {
            try
            {
                var source = Dispatcher.Invoke(() => PresentationSource.FromVisual(this));

                if (source?.CompositionTarget != null)
                    _scale = Dispatcher.Invoke(() => source.CompositionTarget.TransformToDevice.M11);
            }
            finally
            {
                GC.Collect(1);
            }
        }
    }
}