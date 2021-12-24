using ScreenToGif.Domain.Models;

namespace ScreenToGif.Util.Extensions;

internal static class PropertyExtensions
{
    internal static bool AsBoolean(this Property prop)
    {
        return prop == null || prop.Value.TryParseBoolean();
    }

    internal static DateTime? AsNullableDateTime(this Property prop)
    {
        return prop?.Value.TryParseDateTimeNullable();
    }

    internal static int AsInteger(this Property prop)
    {
        return prop?.Value.TryParseInteger() ?? 0;
    }
}