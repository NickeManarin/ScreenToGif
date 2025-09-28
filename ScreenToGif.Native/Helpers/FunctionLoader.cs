using System.Runtime.InteropServices;
using ScreenToGif.Native.External;

namespace ScreenToGif.Native.Helpers;

/// <summary>
/// Loads externals functions/methods based on a dynamic DLL path.
/// </summary>
public static class FunctionLoader
{
    private static readonly Dictionary<string, IntPtr> LoadedLibraries = new();

    public static T LoadFunction<T>(string dllPath, string functionName) where T : Delegate
    {
        if (!LoadedLibraries.TryGetValue(dllPath, out var moduleHandle))
        {
            moduleHandle = Kernel32.LoadLibrary(dllPath);

            if (moduleHandle == IntPtr.Zero)
                throw new Exception("Failed to load the DLL: " + dllPath);

            LoadedLibraries[dllPath] = moduleHandle;
        }

        var functionAddress = Kernel32.GetProcAddress(moduleHandle, functionName);

        if (functionAddress == IntPtr.Zero)
            throw new Exception("Failed to get the function address: " + functionName);

        return (T)Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
    }

    public static T? TryLoadFunction<T>(string dllPath, string functionName) where T : Delegate
    {
        if (!LoadedLibraries.TryGetValue(dllPath, out var moduleHandle))
        {
            moduleHandle = Kernel32.LoadLibrary(dllPath);

            if (moduleHandle == IntPtr.Zero)
                return null;

            LoadedLibraries[dllPath] = moduleHandle;
        }

        var functionAddress = Kernel32.GetProcAddress(moduleHandle, functionName);

        if (functionAddress == IntPtr.Zero)
            return null;

        return (T)Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
    }

    public static void UnloadLibrary(string dllPath)
    {
        if (!LoadedLibraries.TryGetValue(dllPath, out var moduleHandle))
            return;

        Kernel32.FreeLibrary(moduleHandle);

        LoadedLibraries.Remove(dllPath);
    }
}