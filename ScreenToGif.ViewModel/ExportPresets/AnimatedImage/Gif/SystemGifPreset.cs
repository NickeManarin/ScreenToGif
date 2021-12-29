using ScreenToGif.Domain.Enums;

namespace ScreenToGif.ViewModel.ExportPresets.AnimatedImage.Gif;

public class SystemGifPreset : GifPreset
{
    public SystemGifPreset()
    {
        Encoder = EncoderTypes.System;
        ImageId = "Vector.Net";
    }


    public static SystemGifPreset Default => new()
    {
        TitleKey = "S.Preset.Gif.System.Low.Title",
        DescriptionKey = "S.Preset.Gif.System.Low.Description",
        HasAutoSave = true,
        IsDefault = true,
        IsSelectedForEncoder = true,
        CreationDate = new DateTime(2021, 02, 20)
    };
}