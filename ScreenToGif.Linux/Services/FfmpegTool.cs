using System.ComponentModel;
using System.Diagnostics;

namespace ScreenToGif.Linux.Services;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public sealed class FfmpegTool
{
    public Task<ProcessResult> RunFfmpegAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default) =>
        RunAsync(Resolve("SCREENTOGIF_FFMPEG", "ffmpeg"), arguments, cancellationToken);

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
            throw new InvalidOperationException($"'{executable}' was not found. Install FFmpeg or set the corresponding SCREENTOGIF_* environment variable.", ex);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

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
