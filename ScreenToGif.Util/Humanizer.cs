using System;

namespace ScreenToGif.Util;

/// <summary>
/// Machine to Human converter. Just kidding. ;)
/// </summary>
public class Humanizer
{
    /// <summary>
    /// Converts a length value to a readable size.
    /// </summary>
    /// <param name="byteCount">The length of the file.</param>
    /// <param name="format">The format of the number.</param>
    /// <returns>A string representation of a file size.</returns>
    public static string BytesToString(long byteCount, string format = null)
    {
        string[] suf = { " B", " KB", " MB", " GB", " TB", " PB" }; 

        if (byteCount == 0)
            return "0" + suf[0];

        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 1);

        return (Math.Sign(byteCount) * num).ToString(format) + suf[place];
    }

    /// <summary>
    /// Converts a length value to a readable size.
    /// </summary>
    /// <param name="byteCount">The length of the file.</param>
    /// <returns>A string representation of a file size.</returns>
    public static string BytesToString(ulong byteCount)
    {
        string[] suf = { " B", " KB", " MB", " GB", " TB", " PB" }; 

        if (byteCount == 0)
            return "0" + suf[0];

        var place = Convert.ToInt32(Math.Floor(Math.Log(byteCount, 1024)));
        var num = Math.Round(byteCount / Math.Pow(1024, place), 1);

        return num + suf[place];
    }

    /// <summary>
    /// Random welcome symbol.
    /// </summary>
    /// <returns>Returns a welcome text/emoji.</returns>
    public static string Welcome()
    {
        var random = new Random();

        string[] faces = { "^.^", ":D", ";D", "^_^", "\\ (•◡•) /", "☺", "✌", "😉", "😊", "😆", "🎈",
            "💡", "🎬", "😎", "🎞", "🎨", "🎥", "📽", "📷", "📸", "📹", "🌏", "🌍", "🌎", "🗺", "🌠" };

        var maxValue = OperationalSystemHelper.IsWin8OrHigher() ? faces.Length : 6; //Exclusive bound.

        return faces[random.Next(maxValue)];
    }

    /// <summary>
    /// Gets two sets of welcome messages.
    /// </summary>
    /// <returns>Two welcome messages.</returns>
    public static string WelcomeInfo()
    {
        var random = new Random();

        string[] texts = { "S.Editor.Welcome.New", "S.Editor.Welcome.Import", "S.Editor.Welcome.ThankYou", "S.Editor.Welcome.Size", "S.Editor.Welcome.Contact", "S.Editor.Welcome.Trouble", "S.Editor.Welcome.NewRecorder" };

        var pick1 = random.Next(texts.Length);

        return texts[pick1];
    }

    /// <summary>
    /// Gets two sets of welcome messages.
    /// </summary>
    /// <returns>Two welcome messages.</returns>
    public static string[] WelcomeInfos()
    {
        var random = new Random();

        string[] texts = { "S.Editor.Welcome.New", "S.Editor.Welcome.Import", "S.Editor.Welcome.ThankYou", "S.Editor.Welcome.Size", "S.Editor.Welcome.Contact", "S.Editor.Welcome.Trouble", "S.Editor.Welcome.NewRecorder" };

        var pick1 = random.Next(texts.Length);
        var pick2 = random.Next(texts.Length);

        while (pick1 == pick2)
            pick2 = random.Next(texts.Length);

        return new [] {texts[pick1], texts[pick2] };
    }
}