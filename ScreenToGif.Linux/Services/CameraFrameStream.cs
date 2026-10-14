using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ScreenToGif.Linux.Services;

/// <summary>Why a frame stream stopped: the child's exit code and the tail of what it wrote to standard error.</summary>
public sealed record CameraStreamFailure(string Source, int ExitCode, string StandardErrorTail)
{
    public string Message => string.IsNullOrWhiteSpace(StandardErrorTail)
        ? $"FFmpeg stopped reading {Source} with exit code {ExitCode}."
        : $"FFmpeg stopped reading {Source} with exit code {ExitCode}: {StandardErrorTail}";
}

/// <summary>
/// One long-lived FFmpeg child decoding a video source into fixed-size BGRA frames on standard output.
/// Frames are read on a background task and handed to <see cref="FrameArrived"/> synchronously; the buffer
/// a handler receives belongs to the stream and is only valid until that handler returns, so a handler that
/// keeps the pixels must copy them.
/// </summary>
/// <remarks>
/// Pointing the same reader at FFmpeg's <c>lavfi</c> test source is what makes the frame path testable
/// without a capture device. See the ADR "Capture webcam frames through an FFmpeg V4L2 subprocess".
/// </remarks>
public sealed partial class CameraFrameStream : IAsyncDisposable
{
    private const int BytesPerPixel = 4;
    private const int StandardErrorTailLines = 12;

    private readonly IReadOnlyList<string> _arguments;
    private readonly CancellationTokenSource _stopping = new();
    private readonly Queue<string> _standardErrorTail = new();
    private readonly object _tailLock = new();

    private readonly byte[] _frame;
    private Process? _process;
    private Task? _readLoop;
    private Task? _standardErrorLoop;
    private bool _childExited;
    private bool _disposed;

    private CameraFrameStream(IReadOnlyList<string> arguments, int width, int height, string source)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "A camera stream needs positive frame dimensions.");

        _arguments = arguments;
        Width = width;
        Height = height;
        Source = source;
        _frame = new byte[FrameByteLength];
    }

    /// <summary>Raised on a background thread once per decoded frame, before the next frame is read.</summary>
    public event EventHandler<CameraFrame>? FrameArrived;

    /// <summary>Raised on a background thread when the child stops on its own rather than on disposal.</summary>
    public event EventHandler<CameraStreamFailure>? Failed;

    public int Width { get; }

    public int Height { get; }

    /// <summary>What the stream is reading, for messages: a device path or the test source's name.</summary>
    public string Source { get; }

    public int FrameByteLength => Width * Height * BytesPerPixel;

    public int? ProcessId { get; private set; }

    public bool ChildHasExited
    {
        get
        {
            if (_childExited)
                return true;
            try
            {
                return _process?.HasExited ?? false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }

    public string StandardErrorTail
    {
        get
        {
            lock (_tailLock)
                return string.Join(Environment.NewLine, _standardErrorTail);
        }
    }

    public static CameraFrameStream CreateForCamera(string devicePath, CameraCaptureFormat format, int frameRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(devicePath);
        ArgumentNullException.ThrowIfNull(format);
        return new CameraFrameStream(CameraArguments(devicePath, format, frameRate), format.Width, format.Height, devicePath);
    }

    public static CameraFrameStream CreateForTestSource(int width, int height, int frameRate) =>
        CreateForArguments(
            [
                "-nostdin", "-hide_banner", "-loglevel", "error",
                "-f", "lavfi", "-i", $"testsrc=size={width}x{height}:rate={frameRate}",
                "-f", "rawvideo", "-pix_fmt", "bgra", "-"
            ],
            width,
            height,
            "the FFmpeg test source");

    public static CameraFrameStream CreateForArguments(IReadOnlyList<string> arguments, int width, int height, string source)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return new CameraFrameStream(arguments, width, height, source);
    }

    /// <summary>The FFmpeg arguments that decode one camera into BGRA frames on standard output.</summary>
    public static IReadOnlyList<string> CameraArguments(string devicePath, CameraCaptureFormat format, int frameRate) =>
    [
        // -nostdin because this child outlives the call that started it: without it an application
        // launched from a terminal shares that terminal with FFmpeg, which reads keys from it.
        "-nostdin", "-hide_banner", "-loglevel", "error",
        "-f", "v4l2",
        "-input_format", format.InputFormat,
        "-video_size", format.SizeArgument,
        "-framerate", frameRate.ToString(),
        "-i", devicePath,
        "-f", "rawvideo", "-pix_fmt", "bgra", "-"
    ];

    /// <summary>Launches the child and begins reading. Throws when FFmpeg cannot be started at all.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process is not null)
            throw new InvalidOperationException("The camera stream has already been started.");

        var executable = FfmpegTool.ResolveFfmpegExecutable();
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in _arguments)
            startInfo.ArgumentList.Add(argument);

        var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Unable to start {executable}.");
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            process.Dispose();
            throw new FfmpegUnavailableException(executable, ex);
        }
        catch
        {
            process.Dispose();
            throw;
        }

        _process = process;
        ProcessId = process.Id;
        _standardErrorLoop = Task.Run(DrainStandardErrorAsync);
        _readLoop = Task.Run(ReadFramesAsync);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        await _stopping.CancelAsync();
        KillChild();

        foreach (var task in new[] { _readLoop, _standardErrorLoop })
        {
            if (task is null)
                continue;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (_process is { } process)
        {
            try
            {
                await process.WaitForExitAsync(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
            }

            _childExited = true;
            process.Dispose();
        }

        _stopping.Dispose();
    }

    private void KillChild()
    {
        if (_process is not { } process)
            return;
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or Win32Exception)
        {
            // The child already exited, or the platform refused the signal; the wait below settles it either way.
        }
    }

    private async Task ReadFramesAsync()
    {
        var process = _process!;
        var output = process.StandardOutput.BaseStream;

        // One buffer is enough because the handler runs synchronously here: the next read cannot start
        // until it returns, which is the same rule the handler is told about in this class's summary.
        while (!_stopping.IsCancellationRequested)
        {
            try
            {
                await output.ReadExactlyAsync(_frame.AsMemory(0, FrameByteLength), _stopping.Token);
            }
            catch (Exception ex) when (ex is EndOfStreamException or OperationCanceledException or IOException or ObjectDisposedException)
            {
                break;
            }

            FrameArrived?.Invoke(this, new CameraFrame(Width, Height, _frame));
        }

        await ReportUnexpectedExitAsync(process);
    }

    private async Task ReportUnexpectedExitAsync(Process process)
    {
        if (_stopping.IsCancellationRequested)
            return;

        try
        {
            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (_standardErrorLoop is { } standardError)
        {
            try
            {
                await standardError;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
            }
        }

        if (_stopping.IsCancellationRequested)
            return;

        int exitCode;
        try
        {
            exitCode = process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        Failed?.Invoke(this, new CameraStreamFailure(Source, exitCode, StandardErrorTail));
    }

    /// <summary>
    /// Strips the <c>[component @ 0xADDRESS]</c> prefix FFmpeg puts on its diagnostics. The address is
    /// an allocation pointer that means nothing to the person reading the message in the window.
    /// </summary>
    private static string Readable(string line) => FfmpegComponentPrefix().Replace(line, string.Empty).Trim();

    [GeneratedRegex(@"^\[[^\]]*@\s*0x[0-9a-fA-F]+\]\s*")]
    private static partial Regex FfmpegComponentPrefix();

    private async Task DrainStandardErrorAsync()
    {
        var reader = _process!.StandardError;
        try
        {
            while (await reader.ReadLineAsync(_stopping.Token) is { } line)
            {
                var message = Readable(line);
                if (message.Length == 0)
                    continue;
                lock (_tailLock)
                {
                    if (_standardErrorTail.Count > 0 && _standardErrorTail.Last() == message)
                        continue;
                    _standardErrorTail.Enqueue(message);
                    while (_standardErrorTail.Count > StandardErrorTailLines)
                        _standardErrorTail.Dequeue();
                }
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
        {
        }
    }
}
