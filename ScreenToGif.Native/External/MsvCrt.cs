using System.Runtime.InteropServices;

namespace ScreenToGif.Native.External
{
    internal static class MsvCrt
    {
        [DllImport(Constants.MsvCrt, EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        internal static extern IntPtr MemoryCopy(IntPtr dest, IntPtr src, UIntPtr count);
    }
}