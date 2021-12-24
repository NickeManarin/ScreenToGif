using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.Other;

public class PsdPreset : ExportPreset
{
    private bool _compressImage = true;
    private bool _saveTimeline = true;
    private bool _maximizeCompatibility = true;


    public bool CompressImage
    {
        get => _compressImage;
        set => SetProperty(ref _compressImage, value);
    }

    public bool SaveTimeline
    {
        get => _saveTimeline;
        set => SetProperty(ref _saveTimeline, value);
    }

    public bool MaximizeCompatibility
    {
        get => _maximizeCompatibility;
        set => SetProperty(ref _maximizeCompatibility, value);
    }


    public PsdPreset()
    {
        Type = ExportFormats.Psd;
        ImageId = "Vector.Logo";
        OutputFilenameKey = "S.Preset.Filename.Image";
        DefaultExtension = ".psd";
        Extension = ".psd";
        IsEncoderExpanded = false;
    }


    public static PsdPreset Default => new()
    {
        TitleKey = "S.Preset.Default.Title",
        DescriptionKey = "S.Preset.Default.Description",
        HasAutoSave = true,
        IsSelectedForEncoder = true,
        IsDefault = true,
        CreationDate = new DateTime(2021, 02, 20),
    };
}