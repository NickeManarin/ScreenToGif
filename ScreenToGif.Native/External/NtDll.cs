using System.Runtime.InteropServices;

namespace ScreenToGif.Native.External
{
    public static class NtDll
    {
        [DllImport(Constants.NtDll, EntryPoint = "NtQueryTimerResolution", SetLastError = true)]
        public static extern int QueryTimerResolution(out int maximumResolution, out int minimumResolution, out int currentResolution);
    }
}