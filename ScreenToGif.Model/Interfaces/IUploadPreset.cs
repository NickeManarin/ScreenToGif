using System.Collections;
using System.Windows;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Interfaces;

public interface IUploadPreset : IPreset
{
    public UploadDestinations Type { get; set; }

    public bool IsEnabled { get; set; }

    public string ImageId { get; set; }

    public bool IsAnonymous { get; set; }

    public ArrayList History { get; set; }

    public List<ExportFormats> AllowedTypes { get; set; }

    public string TypeName { get; }

    public bool HasLimit { get; }

    public bool HasSizeLimit { get; }

    public bool HasDurationLimit { get; }

    public bool HasResolutionLimit { get; }

    public long? SizeLimit { get; }

    public TimeSpan? DurationLimit { get; }

    public Size? ResolutionLimit { get; }

    public string Limit { get; }

    public string Mode { get; }
}