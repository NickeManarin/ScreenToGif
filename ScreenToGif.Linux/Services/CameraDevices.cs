using System.Runtime.InteropServices;

namespace ScreenToGif.Linux.Services;

public sealed record CameraDevice(string DevicePath, string Name, string? StableId = null)
{
    public string PreferenceKey => StableId ?? DevicePath;
    public override string ToString() => Name;
}

public enum CameraProbeOutcome
{
    VideoCapture,
    NotVideoCapture,
    AccessDenied,
    Unavailable
}

public interface ICameraCapabilityProbe
{
    CameraProbeOutcome Probe(string devicePath);
}

/// <summary>Cameras that can be opened, and camera nodes the current user is not allowed to open.</summary>
public sealed record CameraDiscoveryResult(IReadOnlyList<CameraDevice> Devices, IReadOnlyList<CameraDevice> InaccessibleDevices)
{
    public static CameraDiscoveryResult Empty { get; } = new([], []);
}

/// <summary>Enumerates V4L2 nodes from sysfs and keeps only the ones that capture video.</summary>
public sealed class CameraDeviceCatalog(
    string sysfsRoot = CameraDeviceCatalog.DefaultSysfsRoot,
    string deviceRoot = "/dev",
    ICameraCapabilityProbe? probe = null)
{
    public const string DefaultSysfsRoot = "/sys/class/video4linux";
    private const string NodePrefix = "video";

    private readonly ICameraCapabilityProbe _probe = probe ?? new V4l2CapabilityProbe();

    public CameraDiscoveryResult Discover()
    {
        if (!Directory.Exists(sysfsRoot))
            return CameraDiscoveryResult.Empty;

        var devices = new List<CameraDevice>();
        var inaccessible = new List<CameraDevice>();
        foreach (var node in EnumerateNodes())
        {
            var devicePath = Path.Combine(deviceRoot, node);
            var device = new CameraDevice(devicePath, ReadName(node) ?? node, ReadStableId(node));
            switch (_probe.Probe(devicePath))
            {
                case CameraProbeOutcome.VideoCapture:
                    devices.Add(device);
                    break;
                case CameraProbeOutcome.AccessDenied:
                    inaccessible.Add(device);
                    break;
            }
        }

        return new CameraDiscoveryResult(Disambiguate(devices), inaccessible);
    }

    private IEnumerable<string> EnumerateNodes()
    {
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(sysfsRoot).Select(Path.GetFileName).OfType<string>().ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        return entries
            .Select(name => (Name: name, Index: ParseIndex(name)))
            .Where(node => node.Index is not null)
            .OrderBy(node => node.Index)
            .Select(node => node.Name);
    }

    private static int? ParseIndex(string nodeName) =>
        nodeName.StartsWith(NodePrefix, StringComparison.Ordinal)
        && int.TryParse(nodeName.AsSpan(NodePrefix.Length), out var index)
        && index >= 0
            ? index
            : null;

    private string? ReadName(string node)
    {
        try
        {
            var name = File.ReadAllText(Path.Combine(sysfsRoot, node, "name")).Trim();
            return name.Length == 0 ? null : CollapseRepeatedName(name);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string? ReadStableId(string node)
    {
        try
        {
            // The parent hardware path survives video-node renumbering. The V4L2 index keeps
            // multiple capture endpoints on one device from sharing a preference.
            var directory = Path.Combine(sysfsRoot, node);
            var target = new DirectoryInfo(Path.Combine(directory, "device")).ResolveLinkTarget(true);
            var index = File.ReadAllText(Path.Combine(directory, "index")).Trim();
            return target is not null && int.TryParse(index, out var number) && number >= 0
                ? $"sysfs:{target.FullName}#{number}"
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// UVC cameras report their V4L2 card string as "vendor: product", and many use the same word for
    /// both, so the reference camera calls itself "BP-6500: BP-6500". Saying it once reads better and
    /// loses nothing; a card string whose halves differ is left alone.
    /// </summary>
    private static string CollapseRepeatedName(string name)
    {
        var separator = name.IndexOf(": ", StringComparison.Ordinal);
        if (separator <= 0)
            return name;

        var vendor = name[..separator];
        var product = name[(separator + 2)..].Trim();
        return string.Equals(vendor, product, StringComparison.Ordinal) ? vendor : name;
    }

    private static IReadOnlyList<CameraDevice> Disambiguate(List<CameraDevice> devices)
    {
        var duplicated = devices.GroupBy(device => device.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        return devices
            .Select(device => duplicated.Contains(device.Name)
                ? device with { Name = $"{device.Name} ({Path.GetFileName(device.DevicePath)})" }
                : device)
            .ToArray();
    }
}

/// <summary>Asks the kernel whether a V4L2 node captures video, without touching its stream.</summary>
public sealed class V4l2CapabilityProbe : ICameraCapabilityProbe
{
    private const int OpenReadWrite = 0x0002;
    private const int OpenNonBlocking = 0x0800;
    private const nuint QueryCapabilityRequest = 0x80685600; // VIDIOC_QUERYCAP = _IOR('V', 0, struct v4l2_capability)
    private const uint VideoCaptureCapability = 0x00000001;
    private const uint VideoCaptureMultiPlanarCapability = 0x00001000;
    private const uint DeviceCapabilitiesValid = 0x80000000;
    private const int PermissionDenied = 13; // EACCES
    private const int OperationNotPermitted = 1; // EPERM

    public CameraProbeOutcome Probe(string devicePath)
    {
        var descriptor = open(devicePath, OpenReadWrite | OpenNonBlocking);
        if (descriptor < 0)
        {
            var error = Marshal.GetLastPInvokeError();
            return error is PermissionDenied or OperationNotPermitted
                ? CameraProbeOutcome.AccessDenied
                : CameraProbeOutcome.Unavailable;
        }

        try
        {
            var capability = new V4l2Capability();
            if (ioctl(descriptor, QueryCapabilityRequest, ref capability) != 0)
                return CameraProbeOutcome.Unavailable;

            var effective = (capability.Capabilities & DeviceCapabilitiesValid) != 0
                ? capability.DeviceCapabilities
                : capability.Capabilities;
            return (effective & (VideoCaptureCapability | VideoCaptureMultiPlanarCapability)) != 0
                ? CameraProbeOutcome.VideoCapture
                : CameraProbeOutcome.NotVideoCapture;
        }
        finally
        {
            close(descriptor);
        }
    }

    /// <summary>struct v4l2_capability: driver[16], card[32], bus_info[32], version, capabilities, device_caps, reserved[3].</summary>
    [StructLayout(LayoutKind.Explicit, Size = 104)]
    private struct V4l2Capability
    {
        [FieldOffset(80)] public uint Version;
        [FieldOffset(84)] public uint Capabilities;
        [FieldOffset(88)] public uint DeviceCapabilities;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int open([MarshalAs(UnmanagedType.LPStr)] string path, int flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int descriptor, nuint request, ref V4l2Capability argument);

    [DllImport("libc")]
    private static extern int close(int descriptor);
}
