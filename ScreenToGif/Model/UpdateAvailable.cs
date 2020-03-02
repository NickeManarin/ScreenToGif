using System;

namespace ScreenToGif.Model
{
    internal class UpdateAvailable
    {
        public bool IsFromGithub { get; set; } = true;

        public Version Version { get; set; }
        public long PortableSize { get; set; }
        public long InstallerSize { get; set; }
        public string InstallerDownloadUrl { get; set; }
        public string PortableDownloadUrl { get; set; }
        public string Description { get; set; }

        public bool IsDownloading { get; set; }
        public string InstallerName { get; set; }
        public string InstallerPath { get; set; }
    }
}