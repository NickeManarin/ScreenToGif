using System;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Machine to Human converter. Just kidding. ;)
    /// </summary>
    public class Humanizer
    {
        /// <summary>
        /// Converts a lenght value to a readable size.
        /// </summary>
        /// <param name="byteCount">The lenght of the file.</param>
        /// <returns>A string representation of a file size.</returns>
        public static string BytesToString(long byteCount)
        {
            string[] suf = { " B", " KB", " MB", " GB" }; //I hope no one make a gif with TB's of size. haha - Nicke

            if (byteCount == 0)
                return "0" + suf[0];

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);

            return (Math.Sign(byteCount) * num) + suf[place];
        }

        /// <summary>
        /// Converts a lenght value to a readable size.
        /// </summary>
        /// <param name="byteCount">The lenght of the file.</param>
        /// <returns>A string representation of a file size.</returns>
        public static string BytesToString(ulong byteCount)
        {
            string[] suf = { " B", " KB", " MB", " GB" }; //I hope no one make a gif with TB's of size. haha - Nicke

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

            var maxValue = Other.IsWin8OrHigher() ? faces.Length : 6; //Exclusive bound.

            return faces[random.Next(maxValue)];
        }

        /// <summary>
        /// Gets two sets of welcome messages.
        /// </summary>
        /// <returns>Two welcome messages.</returns>
        public static string WelcomeInfo()
        {
            var random = new Random();

            string[] texts = { "Welcome.New", "Welcome.Import", "Welcome.ThankYou", "Welcome.Size", "Welcome.Contact", "Welcome.Trouble", "Welcome.NewRecorder" };

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

            string[] texts = { "Welcome.New", "Welcome.Import", "Welcome.ThankYou", "Welcome.Size", "Welcome.Contact", "Welcome.Trouble", "Welcome.NewRecorder" };

            var pick1 = random.Next(texts.Length);
            var pick2 = random.Next(texts.Length);

            while (pick1 == pick2)
                pick2 = random.Next(texts.Length);

            return new [] {texts[pick1], texts[pick2] };
        }
    }
}
