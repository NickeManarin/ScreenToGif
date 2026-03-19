using ScreenToGif.Native.External;

namespace ScreenToGif.Native.Helpers;

public static class DllSecurity
{
    public static void HardenDllSearchPath()
    {
        //Restrict DLL search to safe system locations.
        Kernel32.SetDefaultDllDirectories(Kernel32.LoadLibrarySearchSystem32);

        //Remove the current directory from the search path.
        Kernel32.SetDllDirectory(string.Empty);

        //Preload version.dll from System32 so delay-load can't be hijacked.
        Kernel32.LoadLibraryEx("version.dll", IntPtr.Zero, Kernel32.LoadLibrarySearchSystem32);
    }
}
