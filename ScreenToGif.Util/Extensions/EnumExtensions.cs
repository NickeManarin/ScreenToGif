using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace ScreenToGif.Util.Extensions;

/// <summary>
/// From https://stackoverflow.com/a/60529952/1735672.
/// </summary>
public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        return GetCustomAttribute<DescriptionAttribute>(value)?.Description ?? value.ToString();
    }

    public static string GetLowerDescription(this Enum value)
    {
        return GetCustomAttribute<DescriptionAttribute>(value)?.Description ?? value.ToString().ToLower();
    }

    /// <summary>
    /// Gets the custom attribute <typeparamref name="T"/> for the enum constant, if such a constant is defined and has such an attribute; otherwise null.
    /// </summary>
    public static T GetCustomAttribute<T>(this Enum value) where T : Attribute
    {
        return GetField(value)?.GetCustomAttribute<T>(false);
    }

    /// <summary>
    /// Gets the FieldInfo for the enum constant, if such a constant is defined; otherwise null.
    /// </summary>
    public static FieldInfo GetField(this Enum value)
    {
        var u64 = ToUInt64(value);
        return value.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(f => ToUInt64(f.GetRawConstantValue()) == u64);
    }

    /// <summary>
    /// Checks if an enum constant is defined for this enum value
    /// </summary>
    public static bool IsDefined(this Enum value)
    {
        return GetField(value) != null;
    }

    /// <summary>
    /// Converts the enum value to UInt64
    /// </summary>
    public static ulong ToUInt64(this Enum value) => ToUInt64((object)value);

    private static ulong ToUInt64(object value)
    {
        switch (Convert.GetTypeCode(value))
        {
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
                return unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture));

            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Char:
            case TypeCode.Boolean:
                return Convert.ToUInt64(value, CultureInfo.InvariantCulture);

            default: throw new InvalidOperationException("UnknownEnumType");
        }
    }
}