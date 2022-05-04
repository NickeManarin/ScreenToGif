using System.Runtime.InteropServices;
using ScreenToGif.Native.Structs;

namespace ScreenToGif.Native.External
{
    public static class Kernel32
    {
        [DllImport(Constants.Kernel32)]
        public static extern int GetProcessId(IntPtr handle);

        [DllImport(Constants.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

        [DllImport(Constants.Kernel32)]
        public static extern IntPtr LocalAlloc(uint uFlags, UIntPtr uBytes);

        [DllImport(Constants.Kernel32, SetLastError = true)]
        public static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport(Constants.Kernel32)]
        internal static extern IntPtr LoadLibrary(string path);

        [DllImport(Constants.Kernel32)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
}