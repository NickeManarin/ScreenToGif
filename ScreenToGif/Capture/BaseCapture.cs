using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Model;

namespace ScreenToGif.Capture;

public abstract class BaseCapture : ICapture
{
    private Task _task;

    #region Properties

    public bool WasStarted { get; set; }
    public bool IsAcceptingFrames { get; set; }
    public int FrameCount { get; set; }
    public int MinimumDelay { get; set; }
        
    public int Left { get; set; }
    public int Top { get; set; }

    /// <summary>
    /// The current width of the capture. It can fluctuate, based on the DPI of the current screen.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The current height of the capture. It can fluctuate, based on the DPI of the current screen.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// The starting width of the capture. 
    /// </summary>
    public int StartWidth { get; set; }

    /// <summary>
    /// The starting height of the capture.
    /// </summary>
    public int StartHeight { get; set; }

    /// <summary>
    /// The starting scale of the recording.
    /// </summary>
    public double StartScale { get; set; }

    /// <summary>
    /// The current scale of the recording.
    /// </summary>
    public double Scale { get; set; }

    /// <summary>
    /// The difference in scale from the start frame to the current frame.
    /// </summary>
    public double ScaleDiff => StartScale / Scale;

    /// <summary>
    /// The name of the monitor device where the recording is supposed to happen.
    /// </summary>
    public string DeviceName { get; set; }

    public ProjectInfo Project { get; set; }
    public Action<Exception> OnError { get; set; }

    protected BlockingCollection<FrameInfo> BlockingCollection { get; private set; } = new();

    #endregion

    ~BaseCapture()
    {
        Dispose();
    }

    public virtual void Start(int delay, int left, int top, int width, int height, double scale, ProjectInfo project)
    {
        if (WasStarted)
            throw new Exception("Screen capture was already started. Stop before trying again.");

        FrameCount = 0;
        MinimumDelay = delay;
        Left = left;
        Top = top;
        StartWidth = Width = width;
        StartHeight = Height = height;
        StartScale = scale;
        Scale = scale;

        Project = project;
        Project.Width = width;
        Project.Height = height;
        Project.Dpi = 96 * scale;

        BlockingCollection ??= new BlockingCollection<FrameInfo>();

        //Spin up a Task to consume the BlockingCollection.
        _task = Task.Factory.StartNew(() =>
        {
            try
            {
                while (true)
                    Save(BlockingCollection.Take());
            }
            catch (InvalidOperationException)
            {
                //It means that Take() was called on a completed collection.
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() => OnError?.Invoke(e));
            }
        });

        WasStarted = true;
        IsAcceptingFrames = true;
    }

    public virtual void ResetConfiguration()
    { }

    public virtual void Save(FrameInfo info)
    { }

    public virtual int Capture(FrameInfo frame)
    {
        return 0;
    }

    public virtual Task<int> CaptureAsync(FrameInfo frame)
    {
        return null;
    }

    public virtual int CaptureWithCursor(FrameInfo frame)
    {
        return 0;
    }

    public virtual Task<int> CaptureWithCursorAsync(FrameInfo frame)
    {
        return null;
    }

    public virtual int ManualCapture(FrameInfo frame, bool showCursor = false)
    {
        return showCursor ? CaptureWithCursor(frame) : Capture(frame);
    }

    public virtual Task<int> ManualCaptureAsync(FrameInfo frame, bool showCursor = false)
    {
        return showCursor ? CaptureWithCursorAsync(frame) : CaptureAsync(frame);
    }

    public virtual async Task Stop()
    {
        if (!WasStarted)
            return;

        IsAcceptingFrames = false;

        //Stop the consumer thread.
        BlockingCollection.CompleteAdding();

        await _task;

        WasStarted = false;
    }


    private async Task DisposeInternal()
    {
        if (WasStarted)
            await Stop();

        _task?.Dispose();
        _task = null;

        BlockingCollection?.Dispose();
        BlockingCollection = null;
    }

    public virtual async ValueTask DisposeAsync()
    {
        await DisposeInternal();
        GC.SuppressFinalize(this);
    }
        
    public void Dispose()
    {
        DisposeInternal().Wait();
        GC.SuppressFinalize(this);
    }
}