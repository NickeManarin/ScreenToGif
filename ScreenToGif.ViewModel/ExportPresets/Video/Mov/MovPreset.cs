using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Video.Mov;

public class MovPreset : VideoPreset
{
    public MovPreset()
    {
        Type = ExportFormats.Mov;
        DefaultExtension = ".mov";
        Extension = ".mov";
    }
}