using System;
using System.Runtime.InteropServices;

namespace ScreenToGif.Util
{
    /// <summary>
    /// Loads externals functions/methods based on a dynamic DLL path.
    /// </summary>
    internal static class FunctionLoader
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        internal static T LoadFunction<T>(string dllPath, string functionName) where T : Delegate
        {
            {
                var hModule = LoadLibrary(dllPath);
                var functionAddress = GetProcAddress(hModule, functionName);

                return (T)Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
            }
        }
    }
}