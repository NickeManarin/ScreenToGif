using ScreenToGif.Linux.Services;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Drives camera discovery against a handmade sysfs tree and a stub capability probe, so node
/// filtering, ordering, naming and the inaccessible-node split are covered without a capture device.
/// </summary>
public sealed class CameraDeviceCatalogTests : IDisposable
{
    private readonly string _sysfsRoot = Path.Combine(Path.GetTempPath(), $"screentogif-sysfs-{Guid.NewGuid():N}");
    private const string DeviceRoot = "/fake-dev";

    public CameraDeviceCatalogTests() => Directory.CreateDirectory(_sysfsRoot);

    [Fact]
    public void CaptureNodesAreListedInAscendingNumericNodeOrder()
    {
        Node("video10", "Back Camera");
        Node("video2", "Front Camera");
        Node("video0", "Integrated Camera");
        Directory.CreateDirectory(Path.Combine(_sysfsRoot, "not-a-video-node"));

        var result = Catalog(CameraProbeOutcome.VideoCapture).Discover();

        Assert.Equal(
            [$"{DeviceRoot}/video0", $"{DeviceRoot}/video2", $"{DeviceRoot}/video10"],
            result.Devices.Select(device => device.DevicePath));
        Assert.Empty(result.InaccessibleDevices);
    }

    [Fact]
    public void AMetadataNodeIsExcludedFromBothLists()
    {
        Node("video0", "Integrated Camera");
        Node("video1", "Integrated Camera: Metadata");

        var result = Catalog(new Dictionary<string, CameraProbeOutcome>
        {
            [$"{DeviceRoot}/video0"] = CameraProbeOutcome.VideoCapture,
            [$"{DeviceRoot}/video1"] = CameraProbeOutcome.NotVideoCapture
        }).Discover();

        Assert.Equal("Integrated Camera", Assert.Single(result.Devices).Name);
        Assert.Empty(result.InaccessibleDevices);
    }

    [Fact]
    public void ANodeTheUserCannotOpenIsReportedAsInaccessibleRatherThanAvailable()
    {
        Node("video0", "Locked Camera");

        var result = Catalog(CameraProbeOutcome.AccessDenied).Discover();

        Assert.Empty(result.Devices);
        var inaccessible = Assert.Single(result.InaccessibleDevices);
        Assert.Equal($"{DeviceRoot}/video0", inaccessible.DevicePath);
        Assert.Equal("Locked Camera", inaccessible.Name);
    }

    [Fact]
    public void AnUnavailableNodeAppearsInNeitherList()
    {
        Node("video0", "Absent Camera");

        var result = Catalog(CameraProbeOutcome.Unavailable).Discover();

        Assert.Empty(result.Devices);
        Assert.Empty(result.InaccessibleDevices);
    }

    [Fact]
    public void CamerasSharingANameAreDisambiguatedByNodeWhileAUniqueNameIsLeftAlone()
    {
        Node("video0", "Integrated Camera");
        Node("video1", "Integrated Camera");
        Node("video2", "Capture Card");

        var result = Catalog(CameraProbeOutcome.VideoCapture).Discover();

        Assert.Equal(
            ["Integrated Camera (video0)", "Integrated Camera (video1)", "Capture Card"],
            result.Devices.Select(device => device.Name));
    }

    [Fact]
    public void AMissingSysfsRootDiscoversNothingWithoutThrowing()
    {
        var catalog = new CameraDeviceCatalog(
            Path.Combine(_sysfsRoot, "absent"),
            DeviceRoot,
            new StubProbe(_ => CameraProbeOutcome.VideoCapture));

        Assert.Same(CameraDiscoveryResult.Empty, catalog.Discover());
    }

    [Fact]
    public void ANodeWithoutAReadableNameFallsBackToTheNodeName()
    {
        Directory.CreateDirectory(Path.Combine(_sysfsRoot, "video0"));
        Node("video1", "   ");

        var result = Catalog(CameraProbeOutcome.VideoCapture).Discover();

        Assert.Equal(["video0", "video1"], result.Devices.Select(device => device.Name));
    }

    [Fact]
    public void ACardStringThatNamesTheSameWordTwiceIsShownOnce()
    {
        Node("video0", "BP-6500: BP-6500");

        Assert.Equal("BP-6500", Assert.Single(Catalog(CameraProbeOutcome.VideoCapture).Discover().Devices).Name);
    }

    [Fact]
    public void ACardStringWhoseHalvesDifferIsLeftAlone()
    {
        Node("video0", "Acme Optics: HD Webcam C1");

        Assert.Equal(
            "Acme Optics: HD Webcam C1",
            Assert.Single(Catalog(CameraProbeOutcome.VideoCapture).Discover().Devices).Name);
    }

    [Fact]
    public void HardwareIdentityFollowsRenumberingButSeparatesEndpointsAndEqualNames()
    {
        var hardware = Path.Combine(_sysfsRoot, "hardware");
        Directory.CreateDirectory(hardware);
        foreach (var (node, index) in new[] { ("video0", "0"), ("video1", "1"), ("video7", "0") })
        {
            Node(node, "Same Camera");
            var directory = Path.Combine(_sysfsRoot, node);
            Directory.CreateSymbolicLink(Path.Combine(directory, "device"), hardware);
            File.WriteAllText(Path.Combine(directory, "index"), index);
        }

        var devices = Catalog(CameraProbeOutcome.VideoCapture).Discover().Devices;

        Assert.Equal(devices[0].PreferenceKey, devices[2].PreferenceKey);
        Assert.NotEqual(devices[0].PreferenceKey, devices[1].PreferenceKey);
        Assert.NotEqual(devices[0].DevicePath, devices[2].DevicePath);
    }

    [Fact]
    public void WithoutStableHardwareIdentityTheNodePathIsThePreferenceKey()
    {
        Node("video0", "Same Camera");
        Node("video1", "Same Camera");

        var devices = Catalog(CameraProbeOutcome.VideoCapture).Discover().Devices;

        Assert.Equal(devices[0].DevicePath, devices[0].PreferenceKey);
        Assert.NotEqual(devices[0].PreferenceKey, devices[1].PreferenceKey);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sysfsRoot))
            Directory.Delete(_sysfsRoot, recursive: true);
    }

    private void Node(string nodeName, string name)
    {
        var directory = Path.Combine(_sysfsRoot, nodeName);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "name"), $"{name}\n");
    }

    private CameraDeviceCatalog Catalog(CameraProbeOutcome outcome) =>
        new(_sysfsRoot, DeviceRoot, new StubProbe(_ => outcome));

    private CameraDeviceCatalog Catalog(IReadOnlyDictionary<string, CameraProbeOutcome> outcomes) =>
        new(_sysfsRoot, DeviceRoot, new StubProbe(path => outcomes[path]));

    private sealed class StubProbe(Func<string, CameraProbeOutcome> outcome) : ICameraCapabilityProbe
    {
        public CameraProbeOutcome Probe(string devicePath) => outcome(devicePath);
    }
}
