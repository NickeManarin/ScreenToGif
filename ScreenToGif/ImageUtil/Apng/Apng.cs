using System;
using System.IO;
using System.Windows;

namespace ScreenToGif.ImageUtil.Apng
{
    internal class Apng : IDisposable
    {
        #region Properties

        /// <summary>
        /// The stream which the apgn is writen on.
        /// </summary>
        private Stream InternalStream { get; set; }

        /// <summary>
        /// Repeat Count for the apng.
        /// </summary>
        public int RepeatCount { get; set; } = 0;

        /// <summary>
        /// True if it's the first frame of the apgn.
        /// </summary>
        private bool IsFirstFrame { get; set; } = true;

        #endregion

        internal Apng(Stream stream, int repeatCount = 0)
        {
            InternalStream = stream;
            RepeatCount = repeatCount;
        }

        internal void AddFrame(string path, Int32Rect rect, int delay = 66)
        {

        }



        public void Dispose()
        {
            InternalStream?.Dispose();
        }
    }
}