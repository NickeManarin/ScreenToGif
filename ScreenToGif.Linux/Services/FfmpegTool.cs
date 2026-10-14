using System.ComponentModel;
using System.Diagnostics;

namespace ScreenToGif.Linux.Services;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// FFmpeg itself could not be launched, as opposed to media it could not read. A distinct type so a
/// caller can say which of the two happened without matching on the message text, and so the phrase
/// is written once next to the launch that produces it.
/// </summary>
public sealed class FfmpegUnavailableException(string executable, Exception inner)
    : InvalidOperationException(
        $"'{executable}' was not found. Install FFmpeg or set its path in Options.", inner);

public interface IFfmpegTool
{
    Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default);
    Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default);
    Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default);
    Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default);
}

public sealed class FfmpegTool : IFfmpegTool
{
    /// <summary>The FFmpeg command every caller launches, whether through this tool or as a long-lived child.</summary>
    public static string ResolveFfmpegExecutable() => Resolve("SCREENTOGIF_FFMPEG", "ffmpeg");

    public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
        RunAsync(ResolveFfmpegExecutable(), arguments, cancellationToken);

    public Task<ProcessResult> RunFfprobeAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
        RunAsync(Resolve("SCREENTOGIF_FFPROBE", "ffprobe"), arguments, cancellationToken);

    public async Task RunFfmpegCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        var result = await RunFfmpegAsync(arguments, cancellationToken);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(FormatError("ffmpeg", result));
    }

    public async Task<ProcessResult> RunFfprobeCheckedAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        var result = await RunFfprobeAsync(arguments, cancellationToken);

        if (result.ExitCode != 0)
            throw new InvalidOperationException(FormatError("ffprobe", result));

        return result;
    }

    private static async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var argumentList = arguments.ToArray();
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in argumentList)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Unable to start {executable}.");
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            throw new FfmpegUnavailableException(executable, ex);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw;
        }

        return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
    }

    private static string Resolve(string environmentVariable, string fallback)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured;
    }

    private static string FormatError(string executable, ProcessResult result)
    {
        var message = result.StandardError.Trim();
        return string.IsNullOrWhiteSpace(message)
            ? $"{executable} failed with exit code {result.ExitCode}."
            : $"{executable} failed with exit code {result.ExitCode}: {message}";
    }
}
