using System.Diagnostics;

namespace ScreenToGif.Domain.Models;

[DebuggerDisplay("Name = {Name}, MediaSources: {MediaSources.Count}")]
public class VideoSource
{
    public string Name { get; set; }

    public string SymbolicLink { get; set; }

    public bool IsFromHardware { get; set; }

    public List<MediaSource> MediaSources { get; set; } = new();
}