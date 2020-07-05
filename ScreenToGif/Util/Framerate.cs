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
        private static bool _fixedRate = false;

        #endregion

        /// <summary>
        /// Prepares the FrameRate monitor.
        /// </summary>
        /// <param name="interval">The selected interval of each snapshot.</param>
        public static void Start(int interval)
        {
            _stopwatch = new Stopwatch();

            _interval = interval;
            _fixedRate = UserSettings.All.FixedFrameRate;
        }

        /// <summary>
        /// Prapares the framerate monitor
        /// </summary>
        /// <param name="useFixed">If true, uses the fixed internal provided.</param>
        /// <param name="interval">The fixed interval to be used.</param>
        public static void Start(bool useFixed, int interval)
        {
            _stopwatch = new Stopwatch();

            _interval = interval;
            _fixedRate = useFixed;
        }

        /// <summary>
        /// Gets the diff between the last call.
        /// </summary>
        /// <returns>The ammount of seconds.</returns>
        public static int GetMilliseconds()
        {
            if (_fixedRate)
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