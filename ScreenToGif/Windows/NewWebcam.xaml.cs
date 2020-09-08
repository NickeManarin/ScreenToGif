using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.Model;
using ScreenToGif.ViewModel;
using SharpDX;
//using SharpDX.MediaFoundation;

namespace ScreenToGif.Windows
{
    public partial class NewWebcam : Window
    {
        /// <summary>
        /// The view model of the recorder.
        /// </summary>
        private readonly WebcamViewModel _viewModel;

        public NewWebcam()
        {
            InitializeComponent();

            DataContext = _viewModel = new WebcamViewModel();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //SearchVideoDevices();
        }

        private void SourceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            MediaComboBox.SelectedIndex = 0;
        }

        private void MediaComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //StartPlayback();
        }


        //private void SearchVideoDevices()
        //{
        //    ControlGrid.IsEnabled = false;
        //    _viewModel.VideoSources.Clear();

        //    using (var attributes = new MediaAttributes())
        //    {
        //        attributes.Set(CaptureDeviceAttributeKeys.SourceType.Guid, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);

        //        foreach (var activate in MediaFactory.EnumDeviceSources(attributes))
        //        {
        //            var source = new VideoSource
        //            {
        //                Name = activate.Get<string>(CaptureDeviceAttributeKeys.FriendlyName.Guid),
        //                SymbolicLink = activate.Get<string>(CaptureDeviceAttributeKeys.SourceTypeVidcapSymbolicLink.Guid),
        //                IsFromHardware = activate.Get<int>(CaptureDeviceAttributeKeys.SourceTypeVidcapHwSource.Guid) > 0,
        //            };

        //            using (var mediaSource = activate.ActivateObject<MediaSource>())
        //            {
        //                using (var reader = new SourceReader(mediaSource))
        //                {
        //                    var sourceIndex = 0;
        //                    var mediaIndex = 0;

        //                    while (true)
        //                    {
        //                        var media = TryGetMediaSource(reader, ref sourceIndex, ref mediaIndex);

        //                        if (sourceIndex < 0)
        //                            break;
                                
        //                        //Filter out non-supported or repeated media formats.
        //                        //if (media == null || media.Format != "NV12" || source.MediaSources.Any(a => a.Width == media.Width && a.Height == media.Height && Math.Abs(a.Framerate - media.Framerate) < 0.01))
        //                        if (media == null || media.Format != "MJPG" || source.MediaSources.Any(a => a.Width == media.Width && a.Height == media.Height && Math.Abs(a.Framerate - media.Framerate) < 0.01))
        //                            continue;

        //                        source.MediaSources.Add(media);
        //                    }
        //                }
        //            }

        //            _viewModel.VideoSources.Add(source);
        //        }
        //    }

        //    SourceComboBox.SelectedIndex = 0;
        //    ControlGrid.IsEnabled = true;
        //}

        //private MediaSourceType TryGetMediaSource(SourceReader reader, ref int streamIndex, ref int mediaIndex)
        //{
        //    try
        //    {
        //        using (var mt = reader.GetNativeMediaType(streamIndex, mediaIndex))
        //        {
        //            if (mt.MajorType != MediaTypeGuids.Video)
        //            {
        //                streamIndex++;
        //                mediaIndex = 0;
        //                return null;
        //            }

        //            Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameSize), out var width, out var height);
        //            Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameRate), out var frameRateNumerator, out var frameRateDenominator);
                    
        //            //Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.PixelAspectRatio), out var aspectRatioNumerator, out var aspectRatioDenominator);
        //            //Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameRateRangeMax), out var rangeMaxNumerator, out var rangeMaxDenominator);
        //            //Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameRateRangeMin), out var rangeMinNumerator, out var rangeMinDenominator);
                    
        //            //var rotation = mt.Get<int>(MediaTypeAttributeKeys.VideoLighting);
        //            //Console.WriteLine($"MaxFramerate: {rangeMaxNumerator}/{rangeMaxDenominator}, MinFramerate: {rangeMinNumerator}/{rangeMinDenominator}");
                    
        //            mediaIndex++;

        //            return new MediaSourceType
        //            {
        //                StreamIndex = streamIndex,
        //                MediaIndex = mediaIndex,
        //                Width = width,
        //                Height = height,
        //                Framerate = (double) frameRateNumerator / frameRateDenominator,
        //                Format = IdentifySubtype(mt.Get(MediaTypeAttributeKeys.Subtype))
        //            };
        //        }
        //    }
        //    catch (SharpDXException s)
        //    {
        //        if ((uint)s.HResult == 0xC00D36B9)
        //        {
        //            //An object ran out of media types to suggest therefore the requested chain of streaming objects cannot be completed.
        //            streamIndex++;
        //            mediaIndex = 0;
        //        }
        //        else
        //        {
        //            streamIndex = -1;
        //            mediaIndex = -1;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        streamIndex = -1;
        //        mediaIndex = -1;
        //    }

        //    return null;
        //}

        //private string IdentifySubtype(Guid guid)
        //{
        //    if (guid == VideoFormatGuids.NV12)
        //        return "NV12";

        //    if (guid == VideoFormatGuids.Mjpg)
        //        return "MJPG";

        //    if (guid == VideoFormatGuids.YUY2)
        //        return "YUY2";

        //    if (guid == VideoFormatGuids.NV11)
        //        return "NV11";

        //    return guid.ToString();
        //}

        //private async void StartPlayback()
        //{
        //    if (_viewModel.SelectedVideoSource == null || _viewModel.SelectedMediaSource == null)
        //    {
        //        //?
        //        return;
        //    }
            
        //    //Clear resources of previously selected video source.

        //    Activate[] mediaSources;

        //    //Select the video source.
        //    using (var attributes = new MediaAttributes())
        //    {
        //        attributes.Set(CaptureDeviceAttributeKeys.SourceType.Guid, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);
        //        attributes.Set(CaptureDeviceAttributeKeys.SourceTypeVidcapSymbolicLink.Guid, _viewModel.SelectedVideoSource.SymbolicLink);

        //        mediaSources = MediaFactory.EnumDeviceSources(attributes);
        //    }

        //    var mediaSource = mediaSources[0].ActivateObject<MediaSource>();
        //    mediaSource.CreatePresentationDescriptor(out var descriptor);

        //    //Select the media source.
        //    using (var reader = new SourceReader(mediaSource))
        //    {
        //        using (var mt = reader.GetNativeMediaType(_viewModel.SelectedMediaSource.StreamIndex, _viewModel.SelectedMediaSource.MediaIndex))
        //        {
        //            //mt.Set(MediaTypeAttributeKeys.FrameSize, Util.Other.PackLong(_viewModel.SelectedMediaSource.Width, _viewModel.SelectedMediaSource.Height));

        //            reader.SetCurrentMediaType(_viewModel.SelectedMediaSource.StreamIndex, mt);
        //        }

        //        while (true)
        //        {
        //            var sample = reader.ReadSample(_viewModel.SelectedMediaSource.StreamIndex, SourceReaderControlFlags.None, out var readStreamIndex, out var readFlags, out var timestamp) ??
        //                reader.ReadSample(_viewModel.SelectedMediaSource.StreamIndex, SourceReaderControlFlags.None, out readStreamIndex, out readFlags, out timestamp);

        //            var width = _viewModel.SelectedMediaSource.Width;
        //            var height = _viewModel.SelectedMediaSource.Height;

        //            //using (var sourceBuffer = sample.GetBufferByIndex(0))
        //            using (var sourceBuffer = sample.ConvertToContiguousBuffer())
        //            {
        //                var sourcePointer = sourceBuffer.Lock(out var maxLength, out var currentLength);

        //                //var data = new byte[sample.TotalLength];
        //                //Marshal.Copy(sourcePointer, data, 0, sample.TotalLength);

        //                var data = new byte[maxLength];
        //                Marshal.Copy(sourcePointer, data, 0, maxLength);

        //                //if (maxLength > currentLength)
        //                //{
        //                //    await Task.Delay(TimeSpan.FromMilliseconds(500));
        //                //    continue;
        //                //}

        //                //var newData1 = new byte[width * 3 * height];

        //                //for (var i = 0; i < sample.TotalLength; i += 4)
        //                //{
        //                //    //X8R8B8G8 -> BGRA = 4
        //                //    newData1[i] = data[i + 3];
        //                //    newData1[i + 1] = data[i + 2];
        //                //    newData1[i + 2] = data[i + 1];
        //                //    newData1[i + 3] = 255; //data[i];
        //                //}

        //                //var source1 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, newData1, width * 4);

        //                var newData = new byte[width * 3 * height];

        //                //for (uint y = 0; y < height; y++)
        //                for (int y = height - 1; y >= 0; y--)
        //                {
        //                    for (uint x = 0; x < width; x++)
        //                    {
        //                        var xEven = x & 0xFFFFFFFE;
        //                        var yEven = y & 0xFFFFFFFE;
        //                        var yIndex = y * width + x;
        //                        var cIndex = width * height + yEven * (width / 2) + xEven;

        //                        var yy = data[yIndex];
        //                        var cr = data[cIndex + 0];
        //                        var cb = data[cIndex + 1];

        //                        var outputIndex = (newData.Length - (y * width + x) * 3) - 3;

        //                        //BGR.
        //                        newData[outputIndex + 0] = (byte)Math.Min(Math.Max((yy + 1.402 * (cr - 128)), 0), 255);
        //                        newData[outputIndex + 1] = (byte)Math.Min(Math.Max((yy - 0.344 * (cb - 128) - 0.714 * (cr - 128)), 0), 255);
        //                        newData[outputIndex + 2] = (byte)Math.Min(Math.Max((yy + 1.772 * (cb - 128)), 0), 255);
        //                    }
        //                }

        //                //var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, ((width * 24 + 31) / 32) * 4);
        //                //var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, newData, width * 4);
        //                var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, newData, width * 3);

        //                sourceBuffer.Unlock();
        //            }
        //        }
        //    }
        //}





        //private void TryLoad2()
        //{
        //    var attributes = new MediaAttributes();
        //    attributes.Set(CaptureDeviceAttributeKeys.SourceType.Guid, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);

        //    var mediaSources = MediaFactory.EnumDeviceSources(attributes);

        //    var mediaSource = mediaSources[0].ActivateObject<MediaSource>();
        //    mediaSource.CreatePresentationDescriptor(out var presentationDescriptor);

        //    var reader = new SourceReader(mediaSource);
        //    var mediaTypeIndex = 0;

        //    int width, height;

        //    using (var mt = reader.GetNativeMediaType(0, mediaTypeIndex))
        //    {
        //        Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameSize), out width, out height);
        //        Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.FrameRate), out var frameRateNumerator, out var frameRateDenominator);
        //        Util.Other.UnpackLong(mt.Get(MediaTypeAttributeKeys.PixelAspectRatio), out var aspectRatioNumerator, out var aspectRatioDenominator);

        //        //mt.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb24);
        //    }

        //    //Other way.
        //    //var VideoReady = false;
        //    //int streamIndex = 0;

        //    //while (!VideoReady)
        //    //{
        //    //    var nativeMediaType = reader.GetNativeMediaType(streamIndex, 0);
        //    //    var currentMediaType = reader.GetCurrentMediaType(streamIndex);
        //    //    var outputMediaType = new MediaType();

        //    //    reader.SetStreamSelection(streamIndex, true);

        //    //    if (nativeMediaType.MajorType == MediaTypeGuids.Video)
        //    //    {
        //    //        var VideoSubType = currentMediaType.Get<Guid>(MediaTypeAttributeKeys.Subtype);
        //    //        var VideoInterlaceMode = (VideoInterlaceMode)(currentMediaType.Get(MediaTypeAttributeKeys.InterlaceMode));

        //    //        Util.Other.UnpackLong(currentMediaType.Get(MediaTypeAttributeKeys.FrameSize), out var VideoWidth, out var VideoHeight);
        //    //        Util.Other.UnpackLong(currentMediaType.Get(MediaTypeAttributeKeys.FrameRate), out var VideoFrameRateNumerator, out var VideoFrameRateDenominator);
        //    //        Util.Other.UnpackLong(currentMediaType.Get(MediaTypeAttributeKeys.PixelAspectRatio), out var VideoAspectRatioNumerator, out var VideoAspectRatioDenominator);

        //    //        currentMediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.YUY2);
        //    //        reader.SetCurrentMediaType(streamIndex, currentMediaType);

        //    //        MediaFactory.CreateMediaType(outputMediaType);
        //    //        outputMediaType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
        //    //        outputMediaType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb32); //new Guid(22, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71)
        //    //        outputMediaType.Set(MediaTypeAttributeKeys.FrameSize.Guid, Util.Other.PackLong(VideoWidth, VideoHeight));
        //    //        outputMediaType.Set(MediaTypeAttributeKeys.FrameRate.Guid, Util.Other.PackLong(VideoFrameRateNumerator, VideoFrameRateDenominator));
        //    //        outputMediaType.Set(MediaTypeAttributeKeys.InterlaceMode, (int)VideoInterlaceMode.Progressive);
        //    //        outputMediaType.Set(MediaTypeAttributeKeys.PixelAspectRatio, Util.Other.PackLong(VideoAspectRatioNumerator, VideoAspectRatioDenominator));
        //    //        reader.SetCurrentMediaType(streamIndex, outputMediaType);

        //    //        VideoReady = true;
        //    //    }

        //    //    outputMediaType.Dispose();

        //    //    streamIndex++;
        //    //}

        //    var sample = reader.ReadSample(SourceReaderIndex.AnyStream, SourceReaderControlFlags.None, out var readStreamIndex, out var readFlags, out var timestamp) ??
        //                 reader.ReadSample(SourceReaderIndex.AnyStream, SourceReaderControlFlags.None, out readStreamIndex, out readFlags, out timestamp);

        //    var sourceBuffer = sample.GetBufferByIndex(0); // sample.ConvertToContiguousBuffer();
        //    var sourcePointer = sourceBuffer.Lock(out var maxLength, out var currentLength);

        //    var data = new byte[sample.TotalLength];
        //    Marshal.Copy(sourcePointer, data, 0, sample.TotalLength);

        //    //var source1 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, width * 4);

        //    var newData = new byte[width * 3 * height];

        //    for (uint y = 0; y < height; y++)
        //    {
        //        for (uint x = 0; x < width; x++)
        //        {
        //            var xEven = x & 0xFFFFFFFE;
        //            var yEven = y & 0xFFFFFFFE;
        //            var yIndex = y * width + x;
        //            var cIndex = width * height + yEven * (width / 2) + xEven;

        //            var yy = data[yIndex];
        //            var cr = data[cIndex + 0];
        //            var cb = data[cIndex + 1];

        //            var outputIndex = (newData.Length - (y * width + x) * 3) - 3;

        //            //BGR.
        //            newData[outputIndex + 0] = (byte)Math.Min(Math.Max((yy + 1.402 * (cr - 128)), 0), 255);
        //            newData[outputIndex + 1] = (byte)Math.Min(Math.Max((yy - 0.344 * (cb - 128) - 0.714 * (cr - 128)), 0), 255);
        //            newData[outputIndex + 2] = (byte)Math.Min(Math.Max((yy + 1.772 * (cb - 128)), 0), 255);
        //        }
        //    }

        //    //var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, ((width * 24 + 31) / 32) * 4);
        //    //var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, newData, width * 4);
        //    var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, newData, width * 3);

        //    sourceBuffer.Unlock();
        //    sourceBuffer.Dispose();


        //    //var device = new SharpDX.Direct3D11.Device(DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.VideoSupport);
        //    //var texture = new SharpDX.Direct3D11.Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription
        //    //{
        //    //    Usage = SharpDX.Direct3D11.ResourceUsage.Default,
        //    //    BindFlags = SharpDX.Direct3D11.BindFlags.None,
        //    //    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read,
        //    //    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
        //    //    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
        //    //    ArraySize = 1,
        //    //    Height = height,
        //    //    Width = width,
        //    //    MipLevels = 1,
        //    //    SampleDescription = new SampleDescription(1, 0),
        //    //});


        //    //data needs: 
        //    //Height * Stride * Format.BitsPerPixel/8

        //    //var device = new SharpDX.Direct3D9.Device(new Direct3D(), 0, DeviceType.Hardware, new WindowInteropHelper(this).Handle, CreateFlags.SoftwareVertexProcessing, new SharpDX.Direct3D9.PresentParameters(width, height));
        //    //var videoTexture = new SharpDX.Direct3D9.Texture(device, width, height, 1, SharpDX.Direct3D9.Usage.Dynamic, SharpDX.Direct3D9.Format.X8R8G8B8, SharpDX.Direct3D9.Pool.Default);
        //    //var lockData = videoTexture.LockRectangle(0, SharpDX.Direct3D9.LockFlags.None);
        //    //byte[] data = new byte[width * height * 4];
        //    //for (int k = 0; k < width * height * 4;)
        //    //{
        //    //    // Fill with black
        //    //    data[k++] = 0x00;
        //    //    data[k++] = 0x00;
        //    //    data[k++] = 0x00;
        //    //    data[k++] = 0xff;
        //    //}

        //    //Marshal.Copy(data, 0, lockData.DataPointer, width * height * 4);
        //    //videoTexture.UnlockRectangle(0);

        //    //var sourceBuffer = sample.ConvertToContiguousBuffer();
        //    //var sourcePointer = sourceBuffer.Lock(out var maxLength, out var currentLength);
        //    //var destRect = videoTexture.LockRectangle(0, SharpDX.Direct3D9.LockFlags.None);
        //    //var destPointer = destRect.DataPointer;

        //    //Native.MemoryCopy(destPointer, sourcePointer, (UIntPtr)sourceBuffer.MaxLength);

        //    //Marshal.Copy(lockData.DataPointer, data, 0, width * height * 4);
        //    //var source2 = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, width * 4);


        //    //videoTexture.UnlockRectangle(0);

        //    //sourceBuffer.Unlock();
        //    //sourceBuffer.Dispose();

        //    //for (int d = 0; d < presentationDescriptor.StreamDescriptorCount; d++)
        //    //{
        //    //    presentationDescriptor.GetStreamDescriptorByIndex(d, out var isSelected, out var streamDescriptor);

        //    //    for (int i = 0; i < streamDescriptor.MediaTypeHandler.MediaTypeCount; i++)
        //    //    {
        //    //        var type = streamDescriptor.MediaTypeHandler.GetMediaTypeByIndex(i);

        //    //        if (type.MajorType == MediaTypeGuids.Video)
        //    //        {
        //    //            var v = type.QueryInterface<VideoMediaType>();
        //    //            // contains always empty values
        //    //            var x = v.VideoFormat;
        //    //        }
        //    //    }
        //    //}
        //}


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
        //    public EventCallback(MediaSource mediasource, IntPtr pointer)
        //    {
        //        //this.NativePointer = Marshal.GetIUnknownForObject(iunknowObject);
        //        MediaSource = mediasource;
        //        NativePointer = pointer;
        //    }

        //    public MediaSource MediaSource { get; set; }

        //    public IDisposable Shadow { get; set; }

        //    public AsyncCallbackFlags Flags { get; }

        //    public WorkQueueId WorkQueueId { get; }


        //    public void Invoke(AsyncResult asyncResultRef)
        //    {
        //        var ev = MediaSource.EndGetEvent(asyncResultRef);

        //        switch (ev.TypeInfo)
        //        {
        //            case MediaEventTypes.SessionEnded:
        //                //_sessionState = SessionState.Ended;
        //                //OnSongFinishedPlaying(null, null);
        //                break;

        //            case MediaEventTypes.SessionTopologyStatus:
        //                //if (ev.Get(EventAttributeKeys.TopologyStatus) == TopologyStatus.Ready)
        //                //    OnTopologyReady();
        //                break;

        //            case MediaEventTypes.SessionStopped:
        //                //OnSessionStopped();
        //                break;
        //        }

        //        MediaSource.BeginGetEvent(this, null);
        //    }

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

    }
}