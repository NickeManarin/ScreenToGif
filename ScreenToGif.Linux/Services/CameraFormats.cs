using System.Text.RegularExpressions;

namespace ScreenToGif.Linux.Services;

/// <summary>One capture mode a camera offers: an FFmpeg <c>-input_format</c> name at one frame size.</summary>
public sealed record CameraCaptureFormat(string InputFormat, bool IsCompressed, int Width, int Height)
{
    public string SizeArgument => $"{Width}x{Height}";

    public long Area => (long)Width * Height;
}

public sealed record CameraResolution(int Width, int Height)
{
    public override string ToString() => $"{Width} × {Height}";
}

/// <summary>Parses FFmpeg's V4L2 format listing and chooses the capture mode to open.</summary>
public static partial class CameraFormatCatalog
{
    public const int DefaultMaximumWidth = 1280;
    public const int DefaultMaximumHeight = 720;

    private static readonly (int Width, int Height)[] StepwiseCandidates = [(1920, 1080), (1280, 720), (640, 480), (320, 240)];
    private static readonly HashSet<string> VideoCodecFormats = new(StringComparer.OrdinalIgnoreCase) { "h264", "hevc", "vp8", "vp9", "av1" };

    public static IReadOnlyList<string> ListArguments(string devicePath) =>
        ["-hide_banner", "-f", "v4l2", "-list_formats", "all", "-i", devicePath];

    public static IReadOnlyList<CameraCaptureFormat> Parse(string ffmpegOutput)
    {
        var formats = new List<CameraCaptureFormat>();
        var seen = new HashSet<CameraCaptureFormat>();
        foreach (Match line in FormatLine().Matches(ffmpegOutput))
        {
            var compressed = line.Groups["kind"].Value.Equals("Compressed", StringComparison.OrdinalIgnoreCase);
            var name = line.Groups["name"].Value;
            foreach (var (width, height) in ParseSizes(line.Groups["sizes"].Value))
            {
                var format = new CameraCaptureFormat(name, compressed, width, height);
                if (seen.Add(format))
                    formats.Add(format);
            }
        }

        return formats;
    }

    /// <summary>
    /// Largest mode that fits the bounds, preferring compressed formats at equal size because USB cameras
    /// usually sustain higher frame rates in them; the smallest mode when nothing fits. Video codec modes are skipped.
    /// </summary>
    public static CameraCaptureFormat? Choose(
        IEnumerable<CameraCaptureFormat> formats,
        int maximumWidth = DefaultMaximumWidth,
        int maximumHeight = DefaultMaximumHeight)
    {
        var candidates = formats
            .Where(IsUsable)
            .Select((format, order) => (Format: format, Order: order))
            .ToArray();
        if (candidates.Length == 0)
            return null;

        var fitting = candidates.Where(c => c.Format.Width <= maximumWidth && c.Format.Height <= maximumHeight).ToArray();
        var ranked = fitting.Length > 0
            ? fitting.OrderByDescending(c => c.Format.Area)
            : candidates.OrderBy(c => c.Format.Area);
        return ranked
            .ThenByDescending(c => c.Format.IsCompressed)
            .ThenBy(c => c.Order)
            .First().Format;
    }

    public static IReadOnlyList<CameraResolution> Sizes(IEnumerable<CameraCaptureFormat> formats) => formats
        .Where(IsUsable)
        .Select(format => new CameraResolution(format.Width, format.Height))
        .Distinct()
        .OrderByDescending(size => (long)size.Width * size.Height)
        .ThenByDescending(size => size.Width)
        .ToArray();

    public static CameraCaptureFormat? ChooseAtSize(IEnumerable<CameraCaptureFormat> formats, CameraResolution size) =>
        Choose(formats.Where(format => format.Width == size.Width && format.Height == size.Height), int.MaxValue, int.MaxValue);

    public static CameraResolution? SelectSize(IReadOnlyList<CameraCaptureFormat> formats, CameraResolution? saved)
    {
        if (saved is not null && Sizes(formats).Contains(saved))
            return saved;

        return Choose(formats) is { } initial ? new CameraResolution(initial.Width, initial.Height) : null;
    }

    private static bool IsUsable(CameraCaptureFormat format) =>
        !VideoCodecFormats.Contains(format.InputFormat)
        && format.Width is > 0 and <= EditorResourceLimits.MaximumMediaDimension
        && format.Height is > 0 and <= EditorResourceLimits.MaximumMediaDimension;

    private static IEnumerable<(int Width, int Height)> ParseSizes(string text)
    {
        var discrete = DiscreteSize().Matches(text);
        if (discrete.Count > 0)
        {
            foreach (Match size in discrete)
                yield return (int.Parse(size.Groups["w"].Value), int.Parse(size.Groups["h"].Value));
            yield break;
        }

        var range = StepwiseSize().Match(text);
        if (!range.Success)
            yield break;

        var minWidth = int.Parse(range.Groups["minw"].Value);
        var maxWidth = int.Parse(range.Groups["maxw"].Value);
        var minHeight = int.Parse(range.Groups["minh"].Value);
        var maxHeight = int.Parse(range.Groups["maxh"].Value);
        var produced = new HashSet<(int, int)>();
        foreach (var (width, height) in StepwiseCandidates)
        {
            var clamped = (Math.Clamp(width, minWidth, maxWidth), Math.Clamp(height, minHeight, maxHeight));
            if (produced.Add(clamped))
                yield return clamped;
        }
    }

    [GeneratedRegex(@"^\[video4linux2,v4l2 @ [^\]]*\]\s*(?<kind>Compressed|Raw)\s*:\s*(?<name>\S+)\s*:(?<sizes>.*)$", RegexOptions.Multiline)]
    private static partial Regex FormatLine();

    [GeneratedRegex(@"(?<!\d)(?<w>\d{2,5})x(?<h>\d{2,5})(?!\d)")]
    private static partial Regex DiscreteSize();

    [GeneratedRegex(@"\{(?<minw>\d+)-(?<maxw>\d+)(?:,\s*\d+)?\}x\{(?<minh>\d+)-(?<maxh>\d+)(?:,\s*\d+)?\}")]
    private static partial Regex StepwiseSize();
}
