using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// The ways a camera can be unusable read differently to the user and leave different controls
/// available. These are checked from a discovery result or a failure, so no capture device is involved.
/// </summary>
public sealed class WebcamStatusTests
{
    private static readonly CameraDevice FirstCamera = new("/dev/video0", "BP-6500");
    private static readonly CameraDevice SecondCamera = new("/dev/video2", "Integrated Camera");

    [Fact]
    public void NoDevicesAtAllDisablesRecordingAndSaysWhatToDo()
    {
        var status = WebcamStatus.FromDiscovery(CameraDiscoveryResult.Empty);

        Assert.Equal(WebcamAvailability.NoDevices, status.Availability);
        Assert.Equal("No camera found", status.Heading);
        Assert.Contains("Plug in a webcam", status.Message, StringComparison.Ordinal);
        Assert.Contains("Refresh", status.Message, StringComparison.Ordinal);
        Assert.False(status.IsFailure);
        Assert.False(status.CanRecord);
        Assert.False(status.CanChooseDevice);
        Assert.True(status.CanRefresh);
    }

    [Fact]
    public void TheEmptyStateStaysMatterOfFact()
    {
        var message = WebcamStatus.FromDiscovery(CameraDiscoveryResult.Empty).Message;

        Assert.DoesNotContain(":(", message, StringComparison.Ordinal);
        Assert.DoesNotContain(":-(", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACameraThisUserMayNotOpenNamesItsPathAndTheGroupThatGrantsAccess()
    {
        var status = WebcamStatus.FromDiscovery(new CameraDiscoveryResult([], [FirstCamera]));

        Assert.Equal(WebcamAvailability.AccessDenied, status.Availability);
        Assert.Equal("Permission needed", status.Heading);
        Assert.Contains("/dev/video0", status.Message, StringComparison.Ordinal);
        Assert.Contains("'video' group", status.Message, StringComparison.Ordinal);
        Assert.True(status.IsFailure);
        Assert.False(status.CanRecord);
        Assert.True(status.CanRefresh);
    }

    [Fact]
    public void EveryInaccessibleCameraIsNamedRatherThanOnlyTheFirst()
    {
        var status = WebcamStatus.FromDiscovery(new CameraDiscoveryResult([], [FirstCamera, SecondCamera]));

        Assert.Contains("/dev/video0", status.Message, StringComparison.Ordinal);
        Assert.Contains("/dev/video2", status.Message, StringComparison.Ordinal);
        Assert.Contains("Cameras are", status.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAccessibleCameraOutweighsAnInaccessibleOneAndLetsRecordingStart()
    {
        var status = WebcamStatus.FromDiscovery(new CameraDiscoveryResult([FirstCamera], [SecondCamera]));

        Assert.Equal(WebcamAvailability.Ready, status.Availability);
        Assert.True(status.CanRecord);
        Assert.True(status.CanChooseDevice);
        Assert.Equal(string.Empty, status.Message);
        Assert.Equal(string.Empty, status.Heading);
    }

    [Fact]
    public void ABusyCameraIsExplainedInPlainWordsWithFfmpegsOwnTextKeptAsDetail()
    {
        var status = WebcamStatus.FromStreamFailure(
            new CameraStreamFailure("/dev/video0", 240, "Error opening input: Device or resource busy"));

        Assert.Equal(WebcamAvailability.StreamFailed, status.Availability);
        Assert.Equal("Camera unavailable", status.Heading);
        Assert.Contains("/dev/video0 is in use by another application", status.Message, StringComparison.Ordinal);
        Assert.Contains("Refresh", status.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("exit code", status.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Device or resource busy", status.Detail, StringComparison.Ordinal);
        Assert.False(status.CanRecord);
        Assert.True(status.CanChooseDevice);
        Assert.True(status.CanRefresh);
    }

    [Fact]
    public void AMissingFfmpegIsReportedAsAnInstallationProblemRatherThanACameraOne()
    {
        var status = WebcamStatus.FromMissingFfmpeg("/dev/video0", "'ffmpeg' was not found.");

        Assert.Equal(WebcamAvailability.StreamFailed, status.Availability);
        Assert.Contains("FFmpeg could not be started", status.Message, StringComparison.Ordinal);
        Assert.Contains("Options", status.Message, StringComparison.Ordinal);
        Assert.True(status.CanRefresh);
    }

    [Theory]
    [InlineData("Permission denied", "'video' group")]
    [InlineData("Operation not permitted", "'video' group")]
    [InlineData("No such file or directory", "no longer attached")]
    [InlineData("No such device", "no longer attached")]
    public void EachCauseTheUserCanActOnGetsItsOwnExplanation(string ffmpegText, string expected) =>
        Assert.Contains(
            expected,
            WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 1, ffmpegText)).Message,
            StringComparison.Ordinal);

    [Fact]
    public void AnUnrecognisedFailureStillNamesTheDeviceAndAnAction()
    {
        var status = WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 69, "   "));

        Assert.Contains("/dev/video0", status.Message, StringComparison.Ordinal);
        Assert.Contains("Refresh", status.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACameraThatCouldNotBeOpenedReportsTheReasonWithoutInventingAnExitCode()
    {
        var status = WebcamStatus.FromOpenFailure("/dev/video0", "FFmpeg listed no usable capture format for it.");

        Assert.Equal(WebcamAvailability.StreamFailed, status.Availability);
        Assert.Contains("/dev/video0", status.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("exit code", status.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("-1", status.Message, StringComparison.Ordinal);
        Assert.Contains("no usable capture format", status.Detail, StringComparison.Ordinal);
        Assert.False(status.CanRecord);
        Assert.True(status.CanChooseDevice);
    }

    [Theory]
    [MemberData(nameof(EveryUnusableStatus))]
    public void EveryMessageNamesAControlTheWindowActuallyShows(WebcamStatus status) =>
        Assert.Contains("Refresh", status.Message, StringComparison.Ordinal);

    public static IEnumerable<object[]> EveryUnusableStatus() => UnusableStatuses().Select(status => new object[] { status });

    private static IEnumerable<WebcamStatus> UnusableStatuses() =>
    [
        WebcamStatus.FromDiscovery(CameraDiscoveryResult.Empty),
        WebcamStatus.FromDiscovery(new CameraDiscoveryResult([], [FirstCamera])),
        WebcamStatus.FromOpenFailure("/dev/video0", "no usable capture format"),
        WebcamStatus.FromMissingFfmpeg("/dev/video0", "'ffmpeg' was not found."),
        WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 240, "Device or resource busy")),
        WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 1, "Permission denied")),
        WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 1, "No such device")),
        WebcamStatus.FromStreamFailure(new CameraStreamFailure("/dev/video0", 69, "something unrecognised"))
    ];

    [Fact]
    public void TheCaptureDescriptionNamesTheCameraAndTheSizeItWasOpenedAt() =>
        Assert.Equal(
            "BP-6500 at 1280x720",
            WebcamStatus.DescribeCapture(FirstCamera, new CameraCaptureFormat("mjpeg", IsCompressed: true, 1280, 720)));
}
