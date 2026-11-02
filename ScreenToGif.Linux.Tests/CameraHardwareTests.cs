using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Marks a test that needs a real V4L2 capture device. The pinned xunit 2.4 cannot skip a fact from a
/// runtime condition inside the test, but it does read <see cref="FactAttribute.Skip"/> at discovery,
/// so setting it here keeps these tests out of every run that did not opt in.
/// </summary>
public sealed class CameraFactAttribute : FactAttribute
{
    public const string EnableVariableName = "SCREENTOGIF_CAMERA_TESTS";

    public CameraFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnableVariableName), "1", StringComparison.Ordinal))
            Skip = $"Set {EnableVariableName}=1 to run the tests that need a capture device.";
    }
}

/// <summary>Opt-in tests that open the machine's real camera; see <see cref="CameraFactAttribute"/>.</summary>
public sealed class CameraHardwareTests
{
    private const string FirstDevicePath = "/dev/video0";
    private const string SecondDevicePath = "/dev/video1";
    private const int FrameRate = 30;
    private const int RequiredFrames = 5;

    [CameraFact]
    public async Task RealDiscoveryFindsAtLeastOneCaptureDeviceAndNoMetadataOnlyNode()
    {
        var catalog = new CameraDeviceCatalog(CameraDeviceCatalog.DefaultSysfsRoot, "/dev", new V4l2CapabilityProbe());
        var ffmpeg = new FfmpegTool();

        var result = catalog.Discover();

        Assert.NotEmpty(result.Devices);
        foreach (var device in result.Devices)
        {
            var listing = await ffmpeg.RunFfmpegAsync(CameraFormatCatalog.ListArguments(device.DevicePath));
            Assert.NotEmpty(CameraFormatCatalog.Parse(listing.StandardError));
        }
    }

    [CameraFact]
    public void TheRealProbeSeparatesTheCaptureNodeFromItsMetadataNode()
    {
        var probe = new V4l2CapabilityProbe();

        Assert.Equal(CameraProbeOutcome.VideoCapture, probe.Probe(FirstDevicePath));

        if (File.Exists(SecondDevicePath))
            Assert.Equal(CameraProbeOutcome.NotVideoCapture, probe.Probe(SecondDevicePath));
    }

    [CameraFact]
    public async Task TheRealCameraDeliversFullSizedFramesAndItsChildStopsOnDisposal()
    {
        var listing = await new FfmpegTool().RunFfmpegAsync(CameraFormatCatalog.ListArguments(FirstDevicePath));
        var format = CameraFormatCatalog.Choose(CameraFormatCatalog.Parse(listing.StandardError))
            ?? throw new Xunit.Sdk.XunitException(
                $"No usable capture format was listed for {FirstDevicePath}: {listing.StandardError}");

        var lengths = new List<int>();
        var arrived = new SemaphoreSlim(0);
        await using var stream = CameraFrameStream.CreateForCamera(FirstDevicePath, format, FrameRate);
        stream.FrameArrived += (_, frame) =>
        {
            lock (lengths)
                lengths.Add(frame.Bgra.Length);
            arrived.Release();
        };

        stream.Start();

        for (var index = 0; index < RequiredFrames; index++)
        {
            Assert.True(
                await arrived.WaitAsync(TimeSpan.FromSeconds(20)),
                $"Frame {index + 1} of {RequiredFrames} did not arrive from {FirstDevicePath}: {stream.StandardErrorTail}");
        }

        int[] observed;
        lock (lengths)
            observed = [.. lengths];

        Assert.True(observed.Length >= RequiredFrames, $"Expected at least {RequiredFrames} frames but captured {observed.Length}.");
        Assert.All(observed, length => Assert.Equal(format.Width * format.Height * 4, length));

        await stream.DisposeAsync();

        Assert.True(stream.ChildHasExited);
    }
}
