using System.Collections.Generic;
using System.Diagnostics;

namespace ScreenToGif.Model
{
    [DebuggerDisplay("Name = {Name}, MediaSources: {MediaSources.Count}")]
    public class VideoSource
    {
        public string Name { get; set; }

        public string SymbolicLink { get; set; }

        public bool IsFromHardware { get; set; }

        public List<MediaSourceType> MediaSources { get; set; } = new List<MediaSourceType>();
    }
}