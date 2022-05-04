using ScreenToGif.Util.Settings;

namespace ScreenToGif.ViewModel;

public class UpdateAvailable
{
    public bool IsFromGithub { get; set; } = true;

    /// <summary>
    /// The update binary idenfitication failed.
    /// Update must be done manually.
    /// </summary>
    public bool MustDownloadManually { get; set; } = false;

    public bool HasDownloadLink => !string.IsNullOrWhiteSpace(InstallerDownloadUrl);

    public Version Version { get; set; }
    public string Description { get; set; }
    public bool IsDownloading { get; set; }

    public string InstallerPath { get; set; }
    public string InstallerName { get; set; }
    public string InstallerDownloadUrl { get; set; }
    public long InstallerSize { get; set; }

    public string PortablePath { get; set; }
    public string PortableName { get; set; }
    public string PortableDownloadUrl { get; set; }
    public long PortableSize { get; set; }
    
#if FULL_MULTI_MSIX
    public string ActivePath
    {
        get => InstallerPath;
        set => InstallerPath = value;
    }

    public string ActiveName => InstallerName;
    public string ActiveDownloadUrl => InstallerDownloadUrl;
    public long ActiveSize => InstallerSize;
#else
    public string ActivePath
    {
        get => UserSettings.All.PortableUpdate ? PortablePath : InstallerPath;
        set
        {
            if (UserSettings.All.PortableUpdate)
                PortablePath = value;
            else
                InstallerPath = value;
        }
    }

    public string ActiveName => UserSettings.All.PortableUpdate ? PortableName : InstallerName;
    public string ActiveDownloadUrl => UserSettings.All.PortableUpdate ? PortableDownloadUrl : InstallerDownloadUrl;
    public long ActiveSize => UserSettings.All.PortableUpdate ? PortableSize : InstallerSize;
#endif

    public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
}