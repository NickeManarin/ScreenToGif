using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Apng;

public class EmbeddedApngPreset : ApngPreset
{
    private bool _detectUnchanged = true;
    private bool _paintTransparent = true;


    public bool DetectUnchanged
    {
        get => _detectUnchanged;
        set => SetProperty(ref _detectUnchanged, value);
    }

    public bool PaintTransparent
    {
        get => _paintTransparent;
        set => SetProperty(ref _paintTransparent, value);
    }


    public EmbeddedApngPreset()
    {
        Encoder = EncoderTypes.ScreenToGif;
        ImageId = "Vector.Logo";
    }

    public static EmbeddedApngPreset Default = new()
    {
        TitleKey = "S.Preset.Default.Title",
        DescriptionKey = "S.Preset.Default.Description",
        HasAutoSave = true,
        IsSelectedForEncoder = true,
        IsDefault = true,
        CreationDate = new DateTime(2021, 02, 20)
    };
}