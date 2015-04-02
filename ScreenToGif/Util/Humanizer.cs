using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);

            return (Math.Sign(byteCount) * num) + suf[place];
        }
    }
}
