using System;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Apng
{
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
            Encoder = EncoderType.ScreenToGif;
            ImageId = "Vector.Logo";
        }

        public static EmbeddedApngPreset Default = new EmbeddedApngPreset
        {
            TitleKey = "S.Preset.Default.Title",
            DescriptionKey = "S.Preset.Default.Description",
            HasAutoSave = true,
            IsSelectedForEncoder = true,
            IsDefault = true,
            CreationDate = new DateTime(2021, 02, 20)
        };
    }
}