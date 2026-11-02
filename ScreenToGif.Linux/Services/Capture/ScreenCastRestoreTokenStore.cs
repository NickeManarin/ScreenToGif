namespace ScreenToGif.Linux.Services.Capture;

/// <summary>Consumes single-use portal tokens atomically; failures fall back to source selection.</summary>
internal sealed class ScreenCastRestoreTokenStore
{
    private readonly string _path;

    internal ScreenCastRestoreTokenStore(string? path = null) => _path = path ?? Path.Combine(
        Path.GetDirectoryName(new LinuxApplicationSettingsStore().Path)!, "wayland-source.token");

    internal bool HasToken => File.Exists(_path);

    internal string? Take()
    {
        var leased = _path + "." + Guid.NewGuid().ToString("N");
        try
        {
            File.Move(_path, leased);
            var file = new FileInfo(leased);
            if (file.Length is 0 or > 4096) return null;
            var token = File.ReadAllText(leased);
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return null; }
        finally { Delete(leased); }
    }

    internal void Save(string? token)
    {
        if (!OperatingSystem.IsLinux()) return;
        if (string.IsNullOrWhiteSpace(token) || System.Text.Encoding.UTF8.GetByteCount(token) > 4096) return;
        var temporary = _path + "." + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            using (var stream = new FileStream(temporary, new FileStreamOptions
                   {
                       Mode = FileMode.CreateNew, Access = FileAccess.Write,
                       UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
                   }))
            using (var writer = new StreamWriter(stream)) writer.Write(token);
            File.Move(temporary, _path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        finally { Delete(temporary); }
    }

    internal bool Forget() => Delete(_path);

    private static bool Delete(string path)
    {
        try { File.Delete(path); return true; }
        catch (DirectoryNotFoundException) { return true; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return false; }
    }
}
