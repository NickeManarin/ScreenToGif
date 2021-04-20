using System;
using System.Collections.Generic;
using System.Windows.Media;
using ScreenToGif.Util;

namespace ScreenToGif.Model.ExportPresets.AnimatedImage.Gif
{
    /// <summary>
    /// Settings for the a Gif preset with the built-in (embedded) encoder.
    /// </summary>
    public class EmbeddedGifPreset : GifPreset
    {
        private ColorQuantizationType _quantizer = ColorQuantizationType.Neural;
        private int _samplingFactor = 1;
        private int _maximumColorCount = 256;
        private bool _enableTransparency;
        private bool _selectTransparencyColor;
        private Color _transparencyColor = Colors.Black;
        private bool _detectUnchanged = true;
        private bool _paintTransparent = true;
        private Color _chromaKey = Color.FromRgb(50, 205, 50);


        public ColorQuantizationType Quantizer
        {
            get => _quantizer;
            set => SetProperty(ref _quantizer, value);
        }

        public int SamplingFactor
        {
            get => _samplingFactor;
            set => SetProperty(ref _samplingFactor, value);
        }

        public int MaximumColorCount
        {
            get => _maximumColorCount;
            set => SetProperty(ref _maximumColorCount, value);
        }

        public bool EnableTransparency
        {
            get => _enableTransparency;
            set => SetProperty(ref _enableTransparency, value);
        }

        public bool SelectTransparencyColor
        {
            get => _selectTransparencyColor;
            set => SetProperty(ref _selectTransparencyColor, value);
        }

        public Color TransparencyColor
        {
            get => _transparencyColor;
            set => SetProperty(ref _transparencyColor, value);
        }

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

        public Color ChromaKey
        {
            get => _chromaKey;
            set => SetProperty(ref _chromaKey, value);
        }


        public EmbeddedGifPreset()
        {
            Encoder = EncoderType.ScreenToGif;
            ImageId = "Vector.Logo";
        }

        public static List<EmbeddedGifPreset> Defaults => new List<EmbeddedGifPreset>
        {
            new EmbeddedGifPreset
            {
                TitleKey = "S.Preset.Default.Title",
                DescriptionKey = "S.Preset.Default.Description",
                HasAutoSave = true,
                IsSelected = true,
                IsDefault = true,
                IsSelectedForEncoder = true,
                CreationDate = new DateTime(2021, 02, 20),

                Quantizer = ColorQuantizationType.Neural,
                SamplingFactor = 10
            },

            new EmbeddedGifPreset
            {
                TitleKey = "S.Preset.Gif.Embedded.High.Title",
                DescriptionKey = "S.Preset.Gif.Embedded.High.Description",
                HasAutoSave = true,
                IsDefault = true,
                IsSelectedForEncoder = true,
                CreationDate = new DateTime(2021, 02, 20),

                Quantizer = ColorQuantizationType.Neural
            },

            new EmbeddedGifPreset
            {
                TitleKey = "S.Preset.Gif.Embedded.Transparent.Title",
                DescriptionKey = "S.Preset.Gif.Embedded.Transparent.Description",
                HasAutoSave = true,
                IsDefault = true,
                CreationDate = new DateTime(2021, 02, 20),

                Quantizer = ColorQuantizationType.Neural,
                EnableTransparency = true,
                DetectUnchanged = false,
                PaintTransparent = false,
            },

            new EmbeddedGifPreset
            {
                TitleKey = "S.Preset.Gif.Embedded.Graphics.Title",
                DescriptionKey = "S.Preset.Gif.Embedded.Graphics.Description",
                HasAutoSave = true,
                IsDefault = true,
                CreationDate = new DateTime(2021, 02, 20),

                Quantizer = ColorQuantizationType.Octree
            }
        };
    }
}