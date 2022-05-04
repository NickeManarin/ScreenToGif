using System.Runtime.InteropServices;
using ScreenToGif.Native.External;

namespace ScreenToGif.Native.Helpers;

/// <summary>
/// Loads externals functions/methods based on a dynamic DLL path.
/// </summary>
public static class FunctionLoader
{
    public static T LoadFunction<T>(string dllPath, string functionName) where T : Delegate
    {
        var hModule = Kernel32.LoadLibrary(dllPath);
        var functionAddress = Kernel32.GetProcAddress(hModule, functionName);

        return (T)Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
    }
}