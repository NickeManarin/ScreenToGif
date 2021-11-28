namespace ScreenToGif.Util.Extensions;

internal static class ParseExtensions
{
    internal static bool TryParseBoolean(this string source)
    {
        bool.TryParse(source, out var result);

        return result;
    }

    internal static DateTime? TryParseDateTimeNullable(this string source)
    {
        if (!DateTime.TryParse(source, out var result))
            return null;

        return result;
    }

    internal static int TryParseInteger(this string source)
    {
        return !int.TryParse(source, out var result) ? 0 : result;
    }
}