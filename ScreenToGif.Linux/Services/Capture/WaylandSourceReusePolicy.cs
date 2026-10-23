using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScreenToGif.Linux.Services.Capture;

/// <summary>Administrator approval enables source reuse for the invoking desktop user.</summary>
internal static class WaylandSourceReusePolicy
{
    internal const string HelperPath = "/usr/lib/screentogif/allow-source-reuse";
    internal const string PolicyPath = "/usr/share/polkit-1/actions/io.screentogif.source-reuse.policy";
    private static string GrantPath => $"/var/lib/screentogif/source-reuse/{getuid()}";
    internal static bool IsAllowed => File.Exists(GrantPath);
    internal static bool CanConfigure => File.Exists(HelperPath) && File.Exists(PolicyPath) && File.Exists("/usr/bin/pkexec");

    internal static async Task<bool> SetAllowedAsync(bool allowed, CancellationToken cancellationToken)
    {
        if (!CanConfigure)
            throw new IOException("Install the packaged application and pkexec to enable administrator approval.");
        var start = new ProcessStartInfo("/usr/bin/pkexec")
        {
            UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true
        };
        start.ArgumentList.Add("--disable-internal-agent");
        start.ArgumentList.Add(HelperPath);
        start.ArgumentList.Add(allowed ? "enable" : "disable");
        using var process = Process.Start(start) ?? throw new IOException("Administrator authentication could not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(cancellationToken); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (System.ComponentModel.Win32Exception) { } // An already elevated helper exits after its atomic write.
            catch (InvalidOperationException) { } // The helper may have exited after HasExited was checked.
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            throw;
        }
        await Task.WhenAll(output, error);
        if (process.ExitCode is 126 or 127) return false;
        if (process.ExitCode != 0 || IsAllowed != allowed)
            throw new IOException("The administrator setting could not be changed. No new permission was granted.");
        if (!allowed) new ScreenCastRestoreTokenStore().Forget();
        return true;
    }

    [DllImport("libc")] private static extern uint getuid();
}
