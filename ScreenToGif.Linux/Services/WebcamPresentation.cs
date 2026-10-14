namespace ScreenToGif.Linux.Services;

/// <summary>What the Webcam window can currently do, which is decided by what discovery found.</summary>
public enum WebcamAvailability
{
    /// <summary>No V4L2 capture device is attached.</summary>
    NoDevices,

    /// <summary>Capture devices exist but this user may not open any of them.</summary>
    AccessDenied,

    /// <summary>At least one camera can be opened.</summary>
    Ready,

    /// <summary>A camera was chosen but FFmpeg could not read it.</summary>
    StreamFailed
}

/// <summary>
/// The Webcam window's idle-state presentation, derived from a discovery result or a stream failure
/// rather than from the window, so the ways a camera can be unusable are told apart and tested without
/// a capture device.
/// </summary>
/// <param name="Heading">The short line naming the state, which differs per availability.</param>
/// <param name="Message">One or two sentences: what happened, and what the user can do about it.</param>
/// <param name="Detail">The underlying technical text, for a disclosure; empty when there is none.</param>
public sealed record WebcamStatus(
    WebcamAvailability Availability,
    string Heading,
    string Message,
    string Detail,
    bool CanRecord,
    bool CanChooseDevice,
    bool CanRefresh)
{
    /// <summary>The group that owns <c>/dev/video*</c> on the distributions this application targets.</summary>
    public const string DeviceGroupName = "video";

    /// <summary>True where the state is worth the danger colour rather than the informational one.</summary>
    public bool IsFailure => Availability is WebcamAvailability.AccessDenied or WebcamAvailability.StreamFailed;

    public static WebcamStatus Ready { get; } =
        new(WebcamAvailability.Ready, string.Empty, string.Empty, string.Empty, true, true, true);

    public static WebcamStatus FromDiscovery(CameraDiscoveryResult discovery)
    {
        ArgumentNullException.ThrowIfNull(discovery);

        if (discovery.Devices.Count > 0)
            return Ready;

        if (discovery.InaccessibleDevices.Count > 0)
        {
            var paths = string.Join(", ", discovery.InaccessibleDevices.Select(device => device.DevicePath));
            var subject = discovery.InaccessibleDevices.Count == 1 ? "A camera is" : "Cameras are";
            return new WebcamStatus(
                WebcamAvailability.AccessDenied,
                "Permission needed",
                $"{subject} attached, but this account may not open {paths}. "
                + $"Add your account to the '{DeviceGroupName}' group, sign in again, then press Refresh.",
                string.Empty,
                CanRecord: false,
                CanChooseDevice: false,
                CanRefresh: true);
        }

        return new WebcamStatus(
            WebcamAvailability.NoDevices,
            "No camera found",
            "Plug in a webcam, then press Refresh.",
            string.Empty,
            CanRecord: false,
            CanChooseDevice: false,
            CanRefresh: true);
    }

    /// <summary>
    /// A camera that was listed but could not be read. The device selector and Refresh stay usable, so
    /// the user can pick another camera or retry after freeing the one that failed.
    /// </summary>
    public static WebcamStatus FromStreamFailure(CameraStreamFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Unusable(failure.Source, Explain(failure.Source, failure.StandardErrorTail), failure.Message);
    }

    /// <summary>
    /// A camera that could not be opened at all, as opposed to one whose stream died. No child exited,
    /// so there is no exit code to report and the detail stands on its own.
    /// </summary>
    public static WebcamStatus FromOpenFailure(string devicePath, string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(devicePath);
        return Unusable(devicePath, Explain(devicePath, detail), detail);
    }

    /// <summary>FFmpeg itself is missing, which is about the installation rather than about the camera.</summary>
    public static WebcamStatus FromMissingFfmpeg(string devicePath, string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(devicePath);
        return Unusable(
            devicePath,
            "FFmpeg could not be started. Install it, or set its path in Options, then press Refresh.",
            detail);
    }

    private static WebcamStatus Unusable(string devicePath, string message, string detail) =>
        new(WebcamAvailability.StreamFailed,
            "Camera unavailable",
            message,
            (detail ?? string.Empty).Trim(),
            CanRecord: false,
            CanChooseDevice: true,
            CanRefresh: true);

    /// <summary>
    /// Turns FFmpeg's own words into one sentence and an action. The causes below are the ones a user
    /// can actually do something about; anything else keeps a generic line and leans on the detail.
    /// </summary>
    private static string Explain(string devicePath, string? detail)
    {
        var text = detail ?? string.Empty;

        if (Mentions(text, "Device or resource busy"))
            return $"{devicePath} is in use by another application. Close it, then press Refresh.";
        if (Mentions(text, "Permission denied") || Mentions(text, "Operation not permitted"))
            return $"This account may not open {devicePath}. Add it to the '{DeviceGroupName}' group, sign in again, then press Refresh.";
        if (Mentions(text, "No such file or directory") || Mentions(text, "No such device"))
            return $"{devicePath} is no longer attached. Reconnect the camera, then press Refresh.";
        return $"FFmpeg could not read {devicePath}. Press Refresh to try again, or choose another camera or capture size.";
    }

    private static bool Mentions(string text, string phrase) =>
        text.Contains(phrase, StringComparison.OrdinalIgnoreCase);

    /// <summary>How the chosen camera and capture size are named in the window title.</summary>
    public static string DescribeCapture(CameraDevice device, CameraCaptureFormat format)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(format);
        return $"{device.Name} at {format.SizeArgument}";
    }
}

/// <summary>
/// Which of the Webcam window's controls are usable, derived in one place from everything that bears
/// on the answer. The window had three methods writing these flags and reading each other's state,
/// which let Record go live with no stream behind it; one derivation keeps them consistent and lets
/// the rules be tested without an Avalonia window.
/// </summary>
/// <param name="ShowRetry">Whether the status panel offers its own way to look for cameras again.</param>
public sealed record WebcamControls(
    bool CanRecord,
    bool CanStop,
    bool ShowDiscard,
    bool CanChooseDevice,
    bool CanRefresh,
    bool CanChangeFrameRate,
    bool CanScale,
    bool CanOpenOptions,
    bool ShowRetry)
{
    /// <param name="status">What discovery or the last failure says about the camera.</param>
    /// <param name="stage">Whether a recording is running, paused, or not started.</param>
    /// <param name="openInFlight">True while a camera is being opened, which no control may interrupt.</param>
    /// <param name="hasStream">True once a live stream is installed, which recording needs.</param>
    /// <param name="hasFrames">True while the running recording holds at least one frame.</param>
    /// <param name="hasPendingRecording">
    /// True while a finished recording is waiting to be handed to the editor, which happens when one
    /// stops at the frame limit. Starting another would throw it away, so Record gives way to Stop.
    /// </param>
    public static WebcamControls For(
        WebcamStatus status,
        WebcamRecordingStage stage,
        bool openInFlight,
        bool hasStream,
        bool hasFrames,
        bool hasPendingRecording)
    {
        ArgumentNullException.ThrowIfNull(status);

        var recording = stage == WebcamRecordingStage.Recording;
        var paused = stage == WebcamRecordingStage.Paused;
        var busy = recording || paused;
        var idleAndReady = !busy && !openInFlight && hasStream && status.CanRecord;

        return new WebcamControls(
            CanRecord: busy || (idleAndReady && !hasPendingRecording),
            CanStop: hasFrames || hasPendingRecording,
            ShowDiscard: hasFrames || hasPendingRecording,
            CanChooseDevice: !busy && !openInFlight && status.CanChooseDevice,
            CanRefresh: !busy && !openInFlight && status.CanRefresh,
            CanChangeFrameRate: idleAndReady,
            CanScale: idleAndReady,
            CanOpenOptions: !busy,
            ShowRetry: !busy && !openInFlight && status.Availability != WebcamAvailability.Ready && status.CanRefresh);
    }
}
