using System.Diagnostics;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// A private X server for tests that paint the root window. Tests own the server rather than
/// borrowing the ambient one, because <c>xsetroot -solid</c> would otherwise repaint the desktop
/// of whoever ran the suite.
/// </summary>
internal sealed class XvfbSession : IDisposable
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(15);

    private readonly Process _server;

    private XvfbSession(Process server, string display)
    {
        _server = server;
        Display = display;
    }

    /// <summary>The display this server listens on, such as <c>:104</c>.</summary>
    public string Display { get; }

    /// <summary>
    /// Starts an X server of the given size. Xvfb picks its own free display number and reports it
    /// on the descriptor given to <c>-displayfd</c>, which both avoids racing another test for a
    /// number and tells us the moment the server is accepting clients.
    /// Throws when Xvfb is missing or will not come up: a display-dependent test fails loudly,
    /// never skips.
    /// </summary>
    public static XvfbSession Start(int width, int height)
    {
        var server = Process.Start(new ProcessStartInfo("/bin/sh",
            $"-c \"exec Xvfb -displayfd 1 -screen 0 {width}x{height}x24 -nolisten tcp\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("Xvfb is required for the X11 capture test but did not start.");

        try
        {
            var announcement = server.StandardOutput.ReadLineAsync().WaitAsync(StartTimeout).GetAwaiter().GetResult();
            if (!int.TryParse(announcement?.Trim(), out var displayNumber))
                throw new InvalidOperationException(
                    $"Xvfb did not announce a display number. It reported: {server.StandardError.ReadToEnd()}");

            return new XvfbSession(server, $":{displayNumber}");
        }
        catch
        {
            KillQuietly(server);
            server.Dispose();
            throw;
        }
    }

    /// <summary>Runs an X client against this display and throws when it fails.</summary>
    public void Run(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            Environment = { ["DISPLAY"] = Display }
        }) ?? throw new InvalidOperationException($"'{fileName}' did not start.");

        // Read before waiting: a client that fills its output pipe never exits.
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();

        if (!process.WaitForExit(ClientTimeout))
        {
            KillQuietly(process);
            throw new InvalidOperationException($"'{fileName} {arguments}' did not finish within {ClientTimeout}.");
        }

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"'{fileName} {arguments}' failed with exit code {process.ExitCode}: {error.Result}{output.Result}");
    }

    public void Dispose()
    {
        KillQuietly(_server);
        _server.Dispose();
    }

    private static void KillQuietly(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            process.WaitForExit(TimeSpan.FromSeconds(5));
        }
        catch (Exception exception) when (exception is InvalidOperationException or SystemException)
        {
            // The server is going away either way.
        }
    }
}
