using System.Runtime.InteropServices;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.External
{
    public static class WinMm
    {
        [DllImport(Constants.WinMm, EntryPoint = "timeGetDevCaps", SetLastError = true)]
        public static extern uint GetDevCaps(ref TimeCaps timeCaps, uint sizeTimeCaps);

        [DllImport(Constants.WinMm, EntryPoint = "timeBeginPeriod")]
        public static extern uint BeginPeriod(uint uMilliseconds);

        [DllImport(Constants.WinMm, EntryPoint = "timeGetTime")]
        public static extern uint GetTime();

        [DllImport(Constants.WinMm, EntryPoint = "timeEndPeriod")]
        public static extern uint EndPeriod(uint uMilliseconds);
    }
}