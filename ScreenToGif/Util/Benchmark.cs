using System.Diagnostics;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Frame rate monitor. 
    /// </summary>
    public static class FrameRate
    {
        #region Private Variables

        private static Stopwatch _stopwatch = new Stopwatch();
        private static int _interval = 15;
        private static bool _started = true;
        private static bool _fixedFrameRate = false;

        #endregion

        /// <summary>
        /// Prepares the FrameRate monitor.
        /// </summary>
        /// <param name="interval">The selected interval of each snapshot.</param>
        public static void Start(int interval)
        {
            _stopwatch = new Stopwatch();

            _interval = interval;
            _fixedFrameRate = UserSettings.All.FixedFrameRate;
        }

        /// <summary>
        /// Gets the diff between the last call.
        /// </summary>
        /// <returns>The ammount of seconds.</returns>
        public static int GetMilliseconds(int? framerate = null)
        {
            //Specific delay, for the snapshot feature, for example.
            if (framerate.HasValue)
                return framerate.Value;

            if (_fixedFrameRate)
                return _interval;

            if (_started)
            {
                _started = false;
                _stopwatch.Start();
                return _interval;
            }

            var mili = (int)_stopwatch.ElapsedMilliseconds;
            _stopwatch.Restart();

            return mili;
        }

        /// <summary>
        /// Determine that a stop/pause of the recording.
        /// </summary>
        public static void Stop()
        {
            _stopwatch.Stop();
            _started = true;
        }
    }
}
