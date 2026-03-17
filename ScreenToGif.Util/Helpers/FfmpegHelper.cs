using System.Text.RegularExpressions;

namespace ScreenToGif.Util.Helpers;

public static partial class FfmpegHelper
{
    [GeneratedRegex(@"\b(\d+\.\d+(\.\d+)?)\b")]
    public static partial Regex SemVerRegex();

    [GeneratedRegex(@"\bffmpeg\s+version\s+([^\s-]+)", RegexOptions.IgnoreCase, "pt-BR")]
    public static partial Regex FfmpegVersionRegex();

    public static string IdentifyVersion(string output)
    {
        var firstLine = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];

        //Regex to capture the version token after "ffmpeg version"
        var m = FfmpegVersionRegex().Match(firstLine);

        if (m.Success)
            return m.Groups[1].Value;

        //Fallback: try to find the first token that looks like a semver
        m = SemVerRegex().Match(firstLine);

        return m.Success ? m.Groups[1].Value : null;
    }

    public static bool IsOlder(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        token = token.Trim();

        //Try direct System.Version parse (handles "6", "6.0", "6.0.1", "5.4.2")
        if (Version.TryParse(token, out var v))
            return v.Major < 6;

        //Could not parse into a meaningful System.Version
        return false;
    }
}