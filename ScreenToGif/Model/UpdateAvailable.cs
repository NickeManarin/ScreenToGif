using System;
using System.Threading.Tasks;
using ScreenToGif.Util;

namespace ScreenToGif.Model
{
    internal class UpdateAvailable
    {
        public bool IsFromGithub { get; set; } = true;

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
        
        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
    }
}