using System;
using System.Threading.Tasks;
using ScreenToGif.Model;

namespace ScreenToGif.Capture;

internal interface ICapture : IAsyncDisposable, IDisposable
{
    bool WasStarted { get; set; }
    int FrameCount { get; set; }
    int MinimumDelay { get; set; }
    int Left { get; set; }
    int Top { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    string DeviceName { get; set; }
    ProjectInfo Project { get; set; }

    Action<Exception> OnError {get;set;}

    void Start(int delay, int left, int top, int width, int height, double dpi, ProjectInfo project);
    void ResetConfiguration();
    int Capture(FrameInfo frame);
    Task<int> CaptureAsync(FrameInfo frame);
    int CaptureWithCursor(FrameInfo frame);
    Task<int> CaptureWithCursorAsync(FrameInfo frame);
    int ManualCapture(FrameInfo frame, bool showCursor = false);
    Task<int> ManualCaptureAsync(FrameInfo frame, bool showCursor = false);
    void Save(FrameInfo info);
    Task Stop();
}