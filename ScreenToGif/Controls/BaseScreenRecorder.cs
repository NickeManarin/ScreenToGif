using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ScreenToGif.Capture;
using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Model;
using ScreenToGif.Native.Helpers;
using ScreenToGif.Util;
using ScreenToGif.Util.Settings;

namespace ScreenToGif.Controls;

public class BaseScreenRecorder : BaseRecorder
{
    #region Variables

    /// <summary>
    /// The token in use to control the execution of the capture.
    /// </summary>
    private CancellationTokenSource _captureToken;

    /// <summary>
    /// Indicates when the user is mouse-clicking.
    /// </summary>
    internal MouseButtons RecordClicked = MouseButtons.None;

    /// <summary>
    /// Deals with all screen capture methods.
    /// </summary>
    internal ICapture Capture;

    /// <summary>
    /// Lists of pressed keys.
    /// </summary>
    internal readonly List<IKeyGesture> KeyList = new();

    /// <summary>
    /// Timer responsible for the forced clean up of the objects in memory.
    /// </summary>
    internal readonly System.Timers.Timer GarbageTimer = new System.Timers.Timer();

    #endregion


    public BaseScreenRecorder()
    {
        GarbageTimer.Interval = 3000;
        GarbageTimer.Elapsed += GarbageTimer_Tick;
    }


    private void GarbageTimer_Tick(object sender, EventArgs e)
    {
        GC.Collect(2);
    }


    internal bool HasFixedDelay()
    {
        return UserSettings.All.CaptureFrequency != CaptureFrequencies.PerSecond || UserSettings.All.FixedFrameRate;
    }

    internal int GetFixedDelay()
    {
        switch (UserSettings.All.CaptureFrequency)
        {
            case CaptureFrequencies.Manual:
                return UserSettings.All.PlaybackDelayManual;
            case CaptureFrequencies.Interaction:
                return UserSettings.All.PlaybackDelayInteraction;
            case CaptureFrequencies.PerMinute:
                return UserSettings.All.PlaybackDelayMinute;
            case CaptureFrequencies.PerHour:
                return UserSettings.All.PlaybackDelayHour;
            default: //When the capture is 'PerSecond', the fixed delay is set to use the current framerate.
                return 1000 / UserSettings.All.LatestFps;
        }
    }

    internal int GetTriggerDelay()
    {
        switch (UserSettings.All.CaptureFrequency)
        {
            case CaptureFrequencies.Interaction:
                return UserSettings.All.TriggerDelayInteraction;
            case CaptureFrequencies.Manual:
                return UserSettings.All.TriggerDelayManual;
            default:
                return 0;
        }
    }

    internal int GetCaptureInterval()
    {
        switch (UserSettings.All.CaptureFrequency)
        {
            case CaptureFrequencies.PerHour: //15 frames per hour = 240,000 ms (240 sec, 4 min).
                return (1000 * 60 * 60) / UserSettings.All.LatestFps;

            case CaptureFrequencies.PerMinute: //15 frames per minute = 4,000 ms (4 sec).
                return (1000 * 60) / UserSettings.All.LatestFps;

            default: //PerSecond. 15 frames per second = 66 ms.
                return 1000 / UserSettings.All.LatestFps;
        }
    }

    internal ICapture GetDirectCapture()
    {
        if (UserSettings.All.OnlyCaptureChanges)
            return UserSettings.All.UseMemoryCache ? (ICapture)new DirectChangedCachedCapture() : new DirectChangedImageCapture();

        return UserSettings.All.UseMemoryCache ? new DirectCachedCapture() : new DirectImageCapture();
    }


    internal virtual void StartCapture()
    {
        FrameRate.Start(HasFixedDelay(), GetFixedDelay());
        HasImpreciseCapture = false;

        if (UserSettings.All.ForceGarbageCollection)
            GarbageTimer.Start();

        lock (UserSettings.Lock)
        {
            //Starts the capture.
            _captureToken = new CancellationTokenSource();

            Task.Run(() => PrepareCaptureLoop(GetCaptureInterval()), _captureToken.Token);
        }
    }

    internal virtual void PauseCapture()
    {
        FrameRate.Stop();

        StopInternalCapture();
    }

    internal virtual async Task StopCapture()
    {
        FrameRate.Stop();

        StopInternalCapture();

        if (Capture != null)
            await Capture.Stop();

        GarbageTimer.Stop();
    }

    private void StopInternalCapture()
    {
        if (_captureToken == null)
            return;

        _captureToken.Cancel();
        _captureToken.Dispose();
        _captureToken = null;
    }

    private void PrepareCaptureLoop(int interval)
    {
        using (var resolution = new TimerResolution(1))
        {
            if (!resolution.SuccessfullySetTargetResolution)
            {
                LogWriter.Log($"Imprecise timer resolution... Target: {resolution.TargetResolution}, Current: {resolution.CurrentResolution}");
                Dispatcher.Invoke(() => HasImpreciseCapture = true);
            }

            if (UserSettings.All.ShowCursor)
                CaptureWithCursor(interval);
            else
                CaptureWithoutCursor(interval);

            Dispatcher.Invoke(() => HasImpreciseCapture = false);
        }
    }

    private void CaptureWithCursor(int interval)
    {
        var sw = new Stopwatch();

        while (_captureToken != null && !_captureToken.IsCancellationRequested)
        {
            sw.Restart();

            //Capture frame.
            var frame = new FrameInfo(RecordClicked, KeyList);
            KeyList.Clear();

            var frameCount = Capture.CaptureWithCursor(frame);
            Dispatcher.Invoke(() => FrameCount = frameCount);

            //If behind wait time, wait before capturing new frame.
            if (sw.ElapsedMilliseconds >= interval)
                continue;

            while (sw.Elapsed.TotalMilliseconds < interval)
                Thread.Sleep(1);

            //SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= interval);
        }

        sw.Stop();
    }

    private void CaptureWithoutCursor(int interval)
    {
        var sw = new Stopwatch();

        while (_captureToken != null && !_captureToken.IsCancellationRequested)
        {
            sw.Restart();

            //Capture frame.
            var frame = new FrameInfo(RecordClicked, KeyList);
            KeyList.Clear();

            var frameCount = Capture.Capture(frame);
            Dispatcher.Invoke(() => FrameCount = frameCount);

            //If behind wait time, wait before capturing new frame.
            if (sw.ElapsedMilliseconds >= interval)
                continue;

            while (sw.Elapsed.TotalMilliseconds < interval)
                Thread.Sleep(1);

            //SpinWait.SpinUntil(() => sw.ElapsedMilliseconds >= interval);
        }

        sw.Stop();
    }
}