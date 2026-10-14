namespace ScreenToGif.Linux.Services;

public sealed class LinuxAutostartService
{
    private readonly Func<string> _commandFactory;

    public LinuxAutostartService(string? configHome = null, Func<string>? commandFactory = null)
    {
        configHome ??= Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
            configHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        DesktopFilePath = Path.Combine(configHome, "autostart", "screentogif.desktop");
        _commandFactory = commandFactory ?? BuildCurrentCommand;
    }

    public string DesktopFilePath { get; }

    public bool IsEnabled()
    {
        try
        {
            return File.Exists(DesktopFilePath) &&
                   File.ReadAllText(DesktopFilePath).Contains("X-ScreenToGif-Managed=true", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            if (IsEnabled())
                File.Delete(DesktopFilePath);
            return;
        }

        if (File.Exists(DesktopFilePath) && !IsEnabled())
            throw new InvalidOperationException($"An unmanaged autostart entry already exists at '{DesktopFilePath}'.");

        var directory = Path.GetDirectoryName(DesktopFilePath)
                        ?? throw new InvalidOperationException("The autostart path has no parent directory.");
        Directory.CreateDirectory(directory);

        var contents = $"""
                       [Desktop Entry]
                       Type=Application
                       Version=1.0
                       Name=ScreenToGif
                       Comment=Start ScreenToGif when signing in
                       Exec={_commandFactory()}
                       Icon=screentogif
                       Terminal=false
                       X-GNOME-Autostart-enabled=true
                       X-ScreenToGif-Managed=true

                       """;
        var temporaryPath = DesktopFilePath + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, contents, cancellationToken);
            File.Move(temporaryPath, DesktopFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static string BuildCurrentCommand()
    {
        var processPath = Environment.ProcessPath
                          ?? throw new InvalidOperationException("The current executable path is unavailable.");
        var assemblyPath = Environment.GetCommandLineArgs().FirstOrDefault();

        if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(assemblyPath) &&
            assemblyPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return $"{QuoteDesktopArgument(processPath)} {QuoteDesktopArgument(Path.GetFullPath(assemblyPath))}";
        }

        return QuoteDesktopArgument(processPath);
    }

    public static string QuoteDesktopArgument(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("`", "\\`", StringComparison.Ordinal).Replace("$", "\\$", StringComparison.Ordinal)}\"";
}
