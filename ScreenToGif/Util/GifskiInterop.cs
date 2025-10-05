using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Structs;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util.Codification;
using ScreenToGif.Util.Extensions;
using ScreenToGif.Util.Settings;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenToGif.Util;

/// <summary>
/// Interoperability with the Gifski library.
/// https://docs.rs/gifski/latest/gifski/
/// </summary>
internal class GifskiInterop : IDisposable
{
    private double _timeStamp = 0;

    private delegate IntPtr NewDelegate(GifskiSettings settings);
    private delegate GifskiErrorCodes AddPngFrameDelegate(IntPtr handle, uint index, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, double timestamp);
    private delegate GifskiErrorCodes AddRgbFrameDelegate(IntPtr handle, uint frameNumber, uint width, uint bytesPerRow, uint height, IntPtr pixels, double timestamp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ErrorMessageCallback(IntPtr message, IntPtr userData);
    private delegate GifskiErrorCodes SetErrorMessageCallbackDelegate(IntPtr handle, ErrorMessageCallback callback, IntPtr userData);
    private delegate GifskiErrorCodes SetFileOutputDelegate(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
    private delegate GifskiErrorCodes FinishDelegate(IntPtr handle);

    private delegate GifskiErrorCodes EndAddingFramesDelegate(IntPtr handle);
    private delegate GifskiErrorCodes WriteDelegate(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string destination);
    private delegate void DropDelegate(IntPtr handle);

    private readonly NewDelegate _new;
    private readonly AddPngFrameDelegate _addPngFrame;
    private readonly AddRgbFrameDelegate _addRgbFrame;
    private readonly SetErrorMessageCallbackDelegate _setErrorCallback;
    private readonly SetFileOutputDelegate _setFileOutput;
    private readonly FinishDelegate _finish;
    private readonly EndAddingFramesDelegate _endAddingFrames;
    private readonly WriteDelegate _write;
    private readonly DropDelegate _drop;

    public bool IsOlderThan0Dot9 => _endAddingFrames != null;

    public GifskiInterop()
    {
        var dllPath = UserSettings.All.GifskiLocation;

        _new = FunctionLoader.LoadFunction<NewDelegate>(dllPath, "gifski_new");
        _addPngFrame = FunctionLoader.LoadFunction<AddPngFrameDelegate>(dllPath, "gifski_add_frame_png_file");
        _addRgbFrame = FunctionLoader.LoadFunction<AddRgbFrameDelegate>(dllPath, "gifski_add_frame_rgb");

        //Older versions of the library. < 0.9
        _endAddingFrames = FunctionLoader.TryLoadFunction<EndAddingFramesDelegate>(dllPath, "gifski_end_adding_frames");
        _write = FunctionLoader.TryLoadFunction<WriteDelegate>(dllPath, "gifski_write");
        _drop = FunctionLoader.TryLoadFunction<DropDelegate>(dllPath, "gifski_drop");

        //Newer versions.
        _setErrorCallback = FunctionLoader.TryLoadFunction<SetErrorMessageCallbackDelegate>(dllPath, "gifski_set_error_message_callback");
        _setFileOutput = FunctionLoader.TryLoadFunction<SetFileOutputDelegate>(dllPath, "gifski_set_file_output");
        _finish = FunctionLoader.TryLoadFunction<FinishDelegate>(dllPath, "gifski_finish");
    }

    internal IntPtr Start(uint width, uint height, int quality, bool looped = true, bool fast = false)
    {
        return _new(new GifskiSettings(width, height, (byte)quality, looped, fast));
    }

    internal GifskiErrorCodes AddFrame(IntPtr handle, uint index, string path, int delay, double lastDelay = 0, bool isLast = false)
    {
        if (_addPngFrame != null)
        {
            var result2 = _addPngFrame(handle, index, path, index == 0 ? lastDelay : _timeStamp);

            if (index == 0)
                result2 = _addPngFrame(handle, index, path, 0);

            _timeStamp += delay / 1000D;
            return result2;
        }

        //var aa = new FormatConvertedBitmap(path.SourceFrom(), PixelFormats.Rgb24, null, 0);

        var util = new PixelUtil(new FormatConvertedBitmap(path.SourceFrom(), PixelFormats.Rgb24, null, 0));
        util.LockBitsAndUnpad();

        var bytesPerRow = util.Width * 3; //Was ((util.Width * 24 + 31) / 32) * 3

        //if (bytesPerRow % 4 != 0)
        //    bytesPerRow += (4 - (bytesPerRow % 4));

        //Pin the buffer in order to pass the address as parameter later.
        var pinnedBuffer = GCHandle.Alloc(util.Pixels, GCHandleType.Pinned);
        var address = pinnedBuffer.AddrOfPinnedObject();

        //First frame receives the delay set of the last frame.
        var result = AddFramePixels(handle, index, (uint)util.Width, (uint)bytesPerRow, (uint)util.Height, address, index == 0 ? lastDelay : _timeStamp);

        //Bug in gifski, the first frame delay has a weird value if we don't have the next frame as zero. 
        if (index == 0)
            result = AddFramePixels(handle, index, (uint)util.Width, (uint)bytesPerRow, (uint)util.Height, address, 0);

        _timeStamp += delay / 1000D;
        
        //The buffer must be unpinned, to free resources.
        pinnedBuffer.Free();
        util.UnlockBitsWithoutCommit();

        return result;
    }

    internal GifskiErrorCodes AddFramePixels(IntPtr handle, uint frameNumber, uint width, uint bytesPerRow, uint height, IntPtr pixels, double timestamp)
    {
        return _addRgbFrame(handle, frameNumber, width, bytesPerRow, height, pixels, timestamp);
    }

    internal GifskiErrorCodes EndAdding(IntPtr handle)
    {
        return _endAddingFrames?.Invoke(handle) ?? _finish(handle);
    }

    private static void OnGifskiError(IntPtr messagePtr, IntPtr userData)
    {
        var message = Marshal.PtrToStringUTF8(messagePtr);

        System.Diagnostics.Debug.WriteLine($"Gifski error: {message}");
    }

    internal GifskiErrorCodes SetOutput(IntPtr handle, string destination)
    {
        _setErrorCallback(handle, OnGifskiError, IntPtr.Zero);

        return _setFileOutput(handle, destination);
    }

    internal GifskiErrorCodes End(IntPtr handle, string destination)
    {
        var status = _write(handle, destination);

        if (status != GifskiErrorCodes.Ok)
        {
            _drop(handle);

            return status;
        }

        _drop(handle);

        return GifskiErrorCodes.Ok;
    }

    private void ReleaseUnmanagedResources()
    {
        FunctionLoader.UnloadLibrary(UserSettings.All.GifskiLocation!);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();

        GC.SuppressFinalize(this);
    }

    ~GifskiInterop() => ReleaseUnmanagedResources();
}