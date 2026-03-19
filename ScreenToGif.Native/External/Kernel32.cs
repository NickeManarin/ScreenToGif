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

        [DllImport(Constants.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(Constants.Kernel32)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        internal const uint LoadLibrarySearchSystem32 = 0x00000800;
        internal const uint LoadLibrarySearchDefaultDirs = 0x00001000;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetDefaultDllDirectories(uint directoryFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
    }
}