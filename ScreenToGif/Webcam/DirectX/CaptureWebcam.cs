#region License

// ------------------------------------------------------------------
// Adapted work from DirectX.Capture
// https://www.codeproject.com/articles/3566/directx-capture-class-library
// http://creativecommons.org/licenses/publicdomain/
// -----------------------------------------------------------------

#endregion

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using ScreenToGif.Webcam.DirectShow;

namespace ScreenToGif.Webcam.DirectX;

/// <summary>
/// Gets the video output of a webcam or other video device.
/// </summary>
public class CaptureWebcam : EditStreaming.ISampleGrabberCB, IDisposable
{
    #region Properties

    /// <summary>
    ///  The video capture device filter. Read-only. To use a different
    ///  device, dispose of the current Capture instance and create a new
    ///  instance with the desired device.
    /// </summary>
    public Filter VideoDevice { get; private set; }

    /// <summary>
    ///  The control that will host the preview window.
    /// </summary>
    public ContentControl PreviewWindow { get; set; }

    /// <summary>
    /// The Height of the video feed.
    /// </summary>
    public int Height
    {
        get
        {
            if (_videoInfoHeader != null)
                return _videoInfoHeader.BmiHeader.Height;

            return -1;
        }
    }

    /// <summary>
    /// The Width of the video feed.
    /// </summary>
    public int Width
    {
        get
        {
            if (_videoInfoHeader != null)
                return _videoInfoHeader.BmiHeader.Width;

            return -1;
        }
    }

    /// <summary>
    /// The Scale of the video feed.
    /// </summary>
    public double Scale { get; set; }

    #endregion

    #region Enum

    /// <summary>
    /// Possible states of the internal filter graph.
    /// </summary>
    protected enum GraphState
    {
        /// <summary>
        /// No filter graph at all.
        /// </summary>
        Null,
        /// <summary>
        /// Filter graph created with device filters added.
        /// </summary>
        Created,

        /// <summary>
        /// Filter complete built, ready to run (possibly previewing).
        /// </summary>
        Rendered,

        /// <summary>
        /// Recording is live.
        /// </summary>
        Live
    }

    #endregion

    #region Variables

    /// <summary>
    /// When graphState==Rendered, have we rendered the preview stream?
    /// </summary>
    protected bool IsPreviewRendered = false;

    /// <summary>
    /// Do we need the preview stream rendered (VideoDevice and PreviewWindow != null)
    /// </summary>
    protected bool WantPreviewRendered = false;

    ///// <summary>
    ///// List of physical video sources
    ///// </summary>
    //protected SourceCollection videoSources = null;

    /// <summary>
    /// State of the internal filter graph.
    /// </summary>
    protected GraphState ActualGraphState = GraphState.Null;

    /// <summary>
    /// DShow Filter: Graph builder.
    /// </summary>
    protected ExtendStreaming.IGraphBuilder GraphBuilder;

    /// <summary>
    /// DShow Filter: building graphs for capturing video.
    /// </summary>
    protected ExtendStreaming.ICaptureGraphBuilder2 CaptureGraphBuilder = null;

    /// <summary>
    /// DShow Filter: selected video device.
    /// </summary>
    protected CoreStreaming.IBaseFilter VideoDeviceFilter = null;

    /// <summary>
    /// DShow Filter: configure frame rate, size.
    /// </summary>
    protected ExtendStreaming.IAMStreamConfig VideoStreamConfig = null;

    /// <summary>
    /// DShow Filter: Start/Stop the filter graph -> copy of graphBuilder.
    /// </summary>
    protected ControlStreaming.IMediaControl MediaControl;

    /// <summary>
    /// DShow Filter: Control preview window -> copy of graphBuilder.
    /// </summary>
    protected ControlStreaming.IVideoWindow VideoWindow;

    /// <summary>
    /// DShow Filter: selected video compressor.
    /// </summary>
    protected CoreStreaming.IBaseFilter VideoCompressorFilter = null;

    /// <summary>
    /// Property Backer: Video compression filter.
    /// </summary>
    protected Filter VideoCompressor = null;

    /// <summary>
    /// Grabber filter interface.
    /// </summary>
    private CoreStreaming.IBaseFilter _baseGrabFlt;

    private byte[] _savedArray;

    protected EditStreaming.ISampleGrabber SampGrabber = null;
    private EditStreaming.VideoInfoHeader _videoInfoHeader;

    #endregion

    /// <summary>
    /// Default constructor of the Capture class.
    /// </summary>
    /// <param name="videoDevice">The video device to be the source.</param>
    /// <exception cref="ArgumentException">If no video device is provided.</exception>
    public CaptureWebcam(Filter videoDevice)
    {
        VideoDevice = videoDevice ?? throw new ArgumentException("The videoDevice parameter must be set to a valid Filter.\n");

        CreateGraph();
    }

    #region Public Methods

    /// <summary>
    /// Starts the video preview from the video source.
    /// </summary>
    public void StartPreview()
    {
        DerenderGraph();

        WantPreviewRendered = (PreviewWindow != null) && (VideoDevice != null);

        RenderGraph();
        StartPreviewIfNeeded();
    }

    /// <summary>
    /// Stops the video previewing.
    /// </summary>
    public void StopPreview()
    {
        DerenderGraph();

        WantPreviewRendered = false;

        RenderGraph();
        StartPreviewIfNeeded();
    }

    /// <summary>
    /// Closes and cleans the video previewing.
    /// </summary>
    public void Dispose()
    {
        WantPreviewRendered = false;

        try { DestroyGraph(); }
        catch { }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    ///  Create a new filter graph and add filters (devices, compressors, misc),
    ///  but leave the filters unconnected. Call RenderGraph()
    ///  to connect the filters.
    /// </summary>
    protected void CreateGraph()
    {
        //Skip if already created
        if ((int)ActualGraphState < (int)GraphState.Created)
        {
            //Make a new filter graph.
            GraphBuilder = (ExtendStreaming.IGraphBuilder)Activator.CreateInstance(Type.GetTypeFromCLSID(Uuid.Clsid.FilterGraph, true));

            //Get the Capture Graph Builder.
            var clsid = Uuid.Clsid.CaptureGraphBuilder2;
            var riid = typeof(ExtendStreaming.ICaptureGraphBuilder2).GUID;
            CaptureGraphBuilder = (ExtendStreaming.ICaptureGraphBuilder2)Activator.CreateInstance(Type.GetTypeFromCLSID(clsid, true));

            //Link the CaptureGraphBuilder to the filter graph
            var hr = CaptureGraphBuilder.SetFiltergraph(GraphBuilder);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            var comType = Type.GetTypeFromCLSID(Uuid.Clsid.SampleGrabber);

            if (comType == null)
                throw new Exception("DirectShow SampleGrabber not installed/registered!");

            var comObj = Activator.CreateInstance(comType);
            SampGrabber = (EditStreaming.ISampleGrabber)comObj; comObj = null;

            _baseGrabFlt = (CoreStreaming.IBaseFilter) SampGrabber;

            var media = new CoreStreaming.AmMediaType();

            //Get the video device and add it to the filter graph
            if (VideoDevice != null)
            {
                VideoDeviceFilter = (CoreStreaming.IBaseFilter)Marshal.BindToMoniker(VideoDevice.MonikerString);

                hr = GraphBuilder.AddFilter(VideoDeviceFilter, "Video Capture Device");

                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                media.majorType = Uuid.MediaType.Video;
                media.subType = Uuid.MediaSubType.RGB32;//RGB24;
                media.formatType = Uuid.FormatType.VideoInfo;
                media.temporalCompression = true; //New

                hr = SampGrabber.SetMediaType(media);

                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = GraphBuilder.AddFilter(_baseGrabFlt, "Grabber");

                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
            }

            // Retrieve the stream control interface for the video device
            // FindInterface will also add any required filters
            // (WDM devices in particular may need additional
            // upstream filters to function).

            // Try looking for an interleaved media type
            object o;
            var cat = Uuid.PinCategory.Capture;
            var med = Uuid.MediaType.Interleaved;
            var iid = typeof(ExtendStreaming.IAMStreamConfig).GUID;
            hr = CaptureGraphBuilder.FindInterface(cat, med, VideoDeviceFilter, iid, out o);

            if (hr != 0)
            {
                // If not found, try looking for a video media type
                med = Uuid.MediaType.Video;
                hr = CaptureGraphBuilder.FindInterface(cat, med, VideoDeviceFilter, iid, out o);

                if (hr != 0)
                    o = null;
            }

            VideoStreamConfig = o as ExtendStreaming.IAMStreamConfig;

            // Retrieve the media control interface (for starting/stopping graph)
            MediaControl = (ControlStreaming.IMediaControl)GraphBuilder;

            // Reload any video crossbars
            //if (videoSources != null) videoSources.Dispose(); videoSources = null;

            _videoInfoHeader = (EditStreaming.VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(EditStreaming.VideoInfoHeader));
            Marshal.FreeCoTaskMem(media.formatPtr); media.formatPtr = IntPtr.Zero;

            hr = SampGrabber.SetBufferSamples(false);
            if (hr == 0)
                hr = SampGrabber.SetOneShot(false);
            if (hr == 0)
                hr = SampGrabber.SetCallback(null, 0);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        //Update the state now that we are done.
        ActualGraphState = GraphState.Created;
    }

    /// <summary>
    ///  Disconnect and remove all filters except the device
    ///  and compressor filters. This is the opposite of
    ///  renderGraph(). Some properties such as FrameRate
    ///  can only be set when the device output pins are not
    ///  connected.
    /// </summary>
    protected void DerenderGraph()
    {
        // Stop the graph if it is running (ignore errors)
        MediaControl?.Stop();

        // Free the preview window (ignore errors)
        if (VideoWindow != null)
        {
            VideoWindow.put_Visible(CoreStreaming.DsHlp.OAFALSE);
            VideoWindow.put_Owner(IntPtr.Zero);
            VideoWindow = null;
        }

        // Remove the Resize event handler
        if (PreviewWindow != null)
            PreviewWindow.SizeChanged -= OnPreviewWindowResize;

        if ((int)ActualGraphState >= (int)GraphState.Rendered)
        {
            // Update the state
            ActualGraphState = GraphState.Created;
            IsPreviewRendered = false;

            // Disconnect all filters downstream of the video and audio devices. If we have a compressor
            // then disconnect it, but don't remove it
            if (VideoDeviceFilter != null)
                RemoveDownstream(VideoDeviceFilter, VideoCompressor == null);
        }
    }

    /// <summary>
    ///  Removes all filters downstream from a filter from the graph.
    ///  This is called only by DerenderGraph() to remove everything
    ///  from the graph except the devices and compressors. The parameter
    ///  "removeFirstFilter" is used to keep a compressor (that should
    ///  be immediately downstream of the device) if one is begin used.
    /// </summary>
    protected void RemoveDownstream(CoreStreaming.IBaseFilter filter, bool removeFirstFilter)
    {
        // Get a pin enumerator off the filter
        var hr = filter.EnumPins(out var pinEnum);

        if (pinEnum == null)
            return;

        pinEnum.Reset();

        if (hr != 0)
            return;

        //Loop through each pin.
        var pins = new CoreStreaming.IPin[1];

        do
        {
            // Get the next pin
            hr = pinEnum.Next(1, pins, out _);

            if (hr != 0 || pins[0] == null)
                continue;

            //Get the pin it is connected to
            pins[0].ConnectedTo(out var pinTo);

            if (pinTo != null)
            {
                // Is this an input pin?
                hr = pinTo.QueryPinInfo(out var info);

                if (hr == 0 && (info.dir == CoreStreaming.PinDirection.Input))
                {
                    // Recurse down this branch
                    RemoveDownstream(info.filter, true);

                    // Disconnect
                    GraphBuilder.Disconnect(pinTo);
                    GraphBuilder.Disconnect(pins[0]);

                    // Remove this filter
                    // but don't remove the video or audio compressors
                    if (info.filter != VideoCompressorFilter)
                        GraphBuilder.RemoveFilter(info.filter);
                }

                Marshal.ReleaseComObject(info.filter);
                Marshal.ReleaseComObject(pinTo);
            }

            Marshal.ReleaseComObject(pins[0]);
        } while (hr == 0);

        Marshal.ReleaseComObject(pinEnum);
    }

    /// <summary>
    ///  Connects the filters of a previously created graph
    ///  (created by CreateGraph()). Once rendered the graph
    ///  is ready to be used. This method may also destroy
    ///  streams if we have streams we no longer want.
    /// </summary>
    protected void RenderGraph()
    {
        var didSomething = false;

        // Stop the graph
        MediaControl?.Stop();

        // Create the graph if needed (group should already be created)
        CreateGraph();

        // Derender the graph if we have a capture or preview stream
        // that we no longer want. We can't derender the capture and
        // preview streams separately.
        // Notice the second case will leave a capture stream intact
        // even if we no longer want it. This allows the user that is
        // not using the preview to Stop() and Start() without
        // rerendering the graph.
        if (!WantPreviewRendered && IsPreviewRendered)
            DerenderGraph();

        // Render preview stream (only if necessary)
        if (WantPreviewRendered && !IsPreviewRendered)
        {
            //Render preview (video -> renderer)
            var cat = Uuid.PinCategory.Preview;
            var med = Uuid.MediaType.Video;

            var hr = CaptureGraphBuilder.RenderStream(cat, med, VideoDeviceFilter, _baseGrabFlt, null);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            //Get the IVideoWindow interface
            VideoWindow = (ControlStreaming.IVideoWindow) GraphBuilder;

            // Set the video window to be a child of the main window
            var source = PresentationSource.FromVisual(PreviewWindow) as HwndSource;
            hr = VideoWindow.put_Owner(source.Handle);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            //Set video window style
            hr = VideoWindow.put_WindowStyle(ControlStreaming.WindowStyle.Child | ControlStreaming.WindowStyle.ClipChildren | ControlStreaming.WindowStyle.ClipSiblings);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            //Position video window in client rect of owner window
            PreviewWindow.SizeChanged += OnPreviewWindowResize;
            OnPreviewWindowResize(this, null);

            //Make the video window visible, now that it is properly positioned.
            hr = VideoWindow.put_Visible(ControlStreaming.OABool.True);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            IsPreviewRendered = true;
            didSomething = true;

            var media = new CoreStreaming.AmMediaType();
            hr = SampGrabber.GetConnectedMediaType(media);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            if (media.formatType != Uuid.FormatType.VideoInfo || media.formatPtr == IntPtr.Zero)
                throw new NotSupportedException("Unknown Grabber Media Format");

            _videoInfoHeader = (EditStreaming.VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(EditStreaming.VideoInfoHeader));

            Marshal.FreeCoTaskMem(media.formatPtr);
            media.formatPtr = IntPtr.Zero;
        }

        if (didSomething)
            ActualGraphState = GraphState.Rendered;
    }

    /// <summary>
    ///  Setup and start the preview window if the user has
    ///  requested it (by setting PreviewWindow).
    /// </summary>
    protected void StartPreviewIfNeeded()
    {
        // Render preview
        if (WantPreviewRendered && IsPreviewRendered)
        {
            // Run the graph (ignore errors)
            // We can run the entire graph because the capture
            // stream should not be rendered (and that is enforced
            // in the if statement above)
            MediaControl.Run();
        }
    }

    /// <summary> Resize the preview when the PreviewWindow is resized </summary>
    protected void OnPreviewWindowResize(object sender, EventArgs e)
    {
        // Position video window in client rect of owner window.
        VideoWindow?.SetWindowPosition(0, 0,
            (int)(PreviewWindow.ActualWidth * Scale),
            (int)(PreviewWindow.ActualHeight * Scale)); //-70
    }

    /// <summary>
    ///  Completely tear down a filter graph and
    ///  release all associated resources.
    /// </summary>
    protected void DestroyGraph()
    {
        // Derender the graph
        // This will stop the graph and release preview window.
        // It also destroys half of the graph which is unnecessary but harmless here (ignore errors).
        try { DerenderGraph(); }
        catch { }

        // Update the state after derender because it
        // depends on correct status. But we also want to
        // update the state as early as possible in case of error.
        ActualGraphState = GraphState.Null;
        IsPreviewRendered = false;

        // Remove filters from the graph
        // This should be unnecessary but the Nvidia WDM video driver cannot be used by this application
        // again unless we remove it. Ideally, we should simply enumerate all the filters in the graph and remove them (ignore errors).
        if (GraphBuilder != null)
        {
            if (VideoCompressorFilter != null)
                GraphBuilder.RemoveFilter(VideoCompressorFilter);
            if (VideoDeviceFilter != null)
                GraphBuilder.RemoveFilter(VideoDeviceFilter);

            // Cleanup
            Marshal.ReleaseComObject(GraphBuilder); GraphBuilder = null;
        }

        if (CaptureGraphBuilder != null)
            Marshal.ReleaseComObject(CaptureGraphBuilder); CaptureGraphBuilder = null;
        if (VideoDeviceFilter != null)
            Marshal.ReleaseComObject(VideoDeviceFilter); VideoDeviceFilter = null;
        if (VideoCompressorFilter != null)
            Marshal.ReleaseComObject(VideoCompressorFilter); VideoCompressorFilter = null;

        // These are copies of graphBuilder
        MediaControl = null;
        VideoWindow = null;

        // For unmanaged objects we haven't released explicitly
        GC.Collect();
    }

    #endregion

    #region SampleGrabber

    /// <summary>
    /// Capture frame event delegate.
    /// </summary>
    /// <param name="bitmap">Returns a Bitmap image from the webcam.</param>
    public delegate void CaptureFrame(Bitmap bitmap);

    /// <summary>
    /// Capture frame event.
    /// </summary>
    public event CaptureFrame CaptureFrameEvent;

    public int SampleCB(double sampleTime, CoreStreaming.IMediaSample pSample)
    {
        return 0;
    }

    public int BufferCB(double sampleTime, IntPtr pBuffer, int bufferLen)
    {
        if (CaptureFrameEvent == null)
            return 1;

        var width = _videoInfoHeader.BmiHeader.Width;
        var height = _videoInfoHeader.BmiHeader.Height;

        var stride = width * 3;

        Marshal.Copy(pBuffer, _savedArray, 0, bufferLen);

        var handle = GCHandle.Alloc(_savedArray, GCHandleType.Pinned);
        var scan0 = (int)handle.AddrOfPinnedObject();
        //scan0 += (height - 1) * stride;
        scan0 += height * stride;

        var b = new Bitmap(width, height, -stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, (IntPtr)scan0);
        handle.Free();

        CaptureFrameEvent?.Invoke(b);

        return 0;
    }

    /// <summary>
    /// Prepares the capture of frames.
    /// </summary>
    public void PrepareCapture()
    {
        var size = _videoInfoHeader.BmiHeader.ImageSize;

        if (_savedArray == null)
        {
            if (size < 1000 || size > 16000000)
                return;

            _savedArray = new byte[size + 64000];
        }

        SampGrabber.SetBufferSamples(false);
    }

    /// <summary>
    /// Gets the current frame from the buffer.
    /// </summary>
    /// <returns>The Bitmap of the frame.</returns>
    public Bitmap GetFrame()
    {
        //TODO: Verify any possible leaks.

        //Asks for the buffer size.
        var bufferSize = 0;
        SampGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);

        //Allocs the byte array.
        var handleObj = GCHandle.Alloc(_savedArray, GCHandleType.Pinned);

        //Gets the address of the pinned object.
        var address = handleObj.AddrOfPinnedObject();

        //Puts the buffer inside the byte array.
        SampGrabber.GetCurrentBuffer(ref bufferSize, address);

        //Image size.
        var width = _videoInfoHeader.BmiHeader.Width;
        var height = _videoInfoHeader.BmiHeader.Height;

        var stride = width * 3;
        //address += (height - 1) * stride;
        address += height * stride;

        var bitmap = new Bitmap(width, height, -stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, address);
        handleObj.Free();

        return bitmap;
    }

    #endregion
}