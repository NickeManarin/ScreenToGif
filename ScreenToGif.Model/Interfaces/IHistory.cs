using System.Windows.Documents;
using ScreenToGif.Domain.Enums;

namespace ScreenToGif.Domain.Interfaces;

public interface IHistory
{
    public UploadDestinations Type { get; set; }

    public string PresetName { get; set; }

    public DateTime? DateInUtc { get; set; }

    public DateTime? DateInLocalTime { get; }

    public int Result { get; set; }

    public bool WasSuccessful { get; }

    public long Size { get; set; }

    public TimeSpan? Duration { get; set; }

    public string Link { get; set; }

    public string DeletionLink { get; set; }

    public string Message { get; set; }

    public FlowDocument Content { get; }

    public string GetLink(IPreset preset);
}