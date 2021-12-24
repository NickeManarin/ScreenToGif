using System;
using System.ComponentModel;
using System.Globalization;

namespace ScreenToGif.Util.Converters;

public class StringArrayTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string text)
            return base.ConvertFrom(context, culture, value);

        var str = text.Trim();

        return str.Length == 0 ? null : str.Split(culture.TextInfo.ListSeparator[0]);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (null == value)
            throw new ArgumentNullException(nameof(value));

        if (null == destinationType)
            throw new ArgumentNullException(nameof(destinationType));

        if (value is not string[] array || destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        var separator = culture?.TextInfo.ListSeparator[0] ?? ',';

        return string.Join(separator.ToString(), array);
    }
}