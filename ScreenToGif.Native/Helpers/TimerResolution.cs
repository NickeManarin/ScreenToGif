using System.Runtime.InteropServices;
using ScreenToGif.Domain.Enums.Native;
using ScreenToGif.Native.External;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.Helpers
{
    /// <summary>
    /// Windows API Extension methods that enables a timer resolution change for the calling thread.
    /// https://docs.microsoft.com/en-us/windows/win32/api/timeapi/
    /// https://randomascii.wordpress.com/2020/10/04/windows-timer-resolution-the-great-rule-change/
    /// </summary>
    public class TimerResolution : IDisposable
    {
        #region Properties

        /// <summary>
        /// The target resolution in milliseconds.
        /// </summary>
        public uint TargetResolution { get; private set; }

        /// <summary>
        /// The current resolution in milliseconds.
        /// May differ from target resolution based on system limitation.
        /// </summary>
        public uint CurrentResolution { get; private set; }

        /// <summary>
        /// True if a new resolution was set (target resolution or not).
        /// </summary>
        public bool SuccessfullySetResolution { get; private set; }

        /// <summary>
        /// True if a new target resolution was set.
        /// </summary>
        public bool SuccessfullySetTargetResolution { get; private set; }

        #endregion

        /// <summary>
        /// Tries setting a given target timer resolution to the current thread.
        /// If the selected resolution can be set, a nearby value will be set instead.
        /// This must be disposed afterwards (or call EndPeriod() passing the CurrentResolution)
        /// </summary>
        /// <param name="targetResolution">The target resolution in milliseconds.</param>
        public TimerResolution(int targetResolution)
        {
            TargetResolution = (uint)targetResolution;

            //Get system limits.
            var timeCaps = new TimeCaps();
            if (WinMm.GetDevCaps(ref timeCaps, (uint)Marshal.SizeOf(typeof(TimeCaps))) != (uint)TimerResults.NoError)
                return;

            //Calculates resolution based on system limits.
            CurrentResolution = Math.Min(Math.Max(timeCaps.MinimumResolution, TargetResolution), timeCaps.MaximumResolution);

            //Begins the period in which the thread will run on this new timer resolution.
            if (WinMm.BeginPeriod(CurrentResolution) != (uint)TimerResults.NoError)
                return;

            SuccessfullySetResolution = true;

            if (CurrentResolution == TargetResolution)
                SuccessfullySetTargetResolution = true;
        }

        public void Dispose()
        {
            if (SuccessfullySetResolution)
                WinMm.EndPeriod(CurrentResolution);
        }
    }
}