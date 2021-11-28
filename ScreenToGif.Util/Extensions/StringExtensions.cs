namespace ScreenToGif.Util.Extensions;

public static class StringExtensions
{
    public static string Remove(this string text, params string[] keys)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text), "The text should not be null.");

        foreach (var key in keys)
            text = text.Replace(key, string.Empty);

        return text;
    }

    public static string Truncate(this string text, int size)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= size ? text : text.Substring(0, size);
    }
}