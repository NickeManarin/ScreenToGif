using System;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Gif
{
    public class SystemGifPreset : GifPreset
    {
        public SystemGifPreset()
        {
            Encoder = EncoderType.System;
            ImageId = "Vector.Net";
        }


        public static SystemGifPreset Default => new SystemGifPreset
        {
            TitleKey = "S.Preset.Gif.System.Low.Title",
            DescriptionKey = "S.Preset.Gif.System.Low.Description",
            HasAutoSave = true,
            IsDefault = true,
            IsSelectedForEncoder = true,
            CreationDate = new DateTime(2021, 02, 20)
        };
    }
}