using System;

namespace ScreenToGif.Model
{
    internal class UpdateModel
    {
        public Version Version { get; set; }
        public long PortableSize { get; set; }
        public long InstallerSize { get; set; }
        public string InstallerDownloadUrl { get; set; }
        public string PortableDownloadUrl { get; set; }
        public string Description { get; set; }
    }
}