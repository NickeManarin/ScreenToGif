using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Native
{
    /// <summary>
    /// Windows API Extension methods that enables a timer resolution change for the calling thread.
    /// https://docs.microsoft.com/en-us/windows/win32/api/timeapi/
    /// https://randomascii.wordpress.com/2020/10/04/windows-timer-resolution-the-great-rule-change/
    /// </summary>
    internal class TimerResolution : IDisposable
    {
        #region Native

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct TimeCaps
        {
            internal readonly uint MinimumResolution;
            internal readonly uint MaximumResolution;
        };

        internal enum TimerResult : uint
        {
            NoError = 0,
            NoCanDo = 97
        }

        [DllImport("winmm.dll", EntryPoint = "timeGetDevCaps", SetLastError = true)]
        private static extern uint GetDevCaps(ref TimeCaps timeCaps, uint sizeTimeCaps);

        [DllImport("ntdll.dll", EntryPoint = "NtQueryTimerResolution", SetLastError = true)]
        private static extern int QueryTimerResolution(out int maximumResolution, out int minimumResolution, out int currentResolution);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        internal static extern uint BeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        internal static extern uint GetTime();

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        internal static extern uint EndPeriod(uint uMilliseconds);

        #endregion

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
            TargetResolution = (uint) targetResolution;

            //Get system limits.
            var timeCaps = new TimeCaps();
            if (GetDevCaps(ref timeCaps, (uint) Marshal.SizeOf(typeof(TimeCaps))) != (uint) TimerResult.NoError)
                return;

            //Calculates resolution based on system limits.
            CurrentResolution = Math.Min(Math.Max(timeCaps.MinimumResolution, TargetResolution), timeCaps.MaximumResolution);

            //Begins the period in which the thread will run on this new timer resolution.
            if (BeginPeriod(CurrentResolution) != (uint) TimerResult.NoError)
                return;

            SuccessfullySetResolution = true;

            if (CurrentResolution == TargetResolution)
                SuccessfullySetTargetResolution = true;
        }

        public void Dispose()
        {
            if (SuccessfullySetResolution)
                EndPeriod(CurrentResolution);
        }
    }
}