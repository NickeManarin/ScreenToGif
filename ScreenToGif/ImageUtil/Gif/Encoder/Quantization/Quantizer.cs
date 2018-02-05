using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ScreenToGif.ImageUtil.Gif.Encoder.Quantization
{
    public abstract class Quantizer
    {
        /// <summary>
        /// Flag used to indicate whether a single pass or two passes are needed for quantization.
        /// </summary>
        private readonly bool _singlePass;

        /// <summary>
        /// The image depth.
        /// </summary>
        public int Depth { get; set; } = 4;

        /// <summary>
        /// The maximum color count.
        /// </summary>
        public int MaxColors { get; set; } = 256;

        /// <summary>
        /// The calculated color table of the image.
        /// </summary>
        public List<Color> ColorTable { get; set; }

        /// <summary>
        /// The color marked as transparent.
        /// </summary>
        public Color? TransparentColor { get; set; }


        public Quantizer(bool singlePass)
        {
            _singlePass = singlePass;
        }

        public byte[] Quantize(byte[] pixels)
        {
            #region Validation

            if (MaxColors < 2 || MaxColors > 256)
                throw new ArgumentOutOfRangeException(nameof(MaxColors), MaxColors, "The number of colors should be between 2 and 255");

            #endregion

            if (!_singlePass)
                FirstPass(pixels);

            ColorTable = GetPalette();

            return SecondPass(pixels);
        }

        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="pixels">The source data</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        protected virtual void FirstPass(byte[] pixels)
        {
            //var pixelSize = Depth == 32 ? 4 : 3;

            for (var i = 0; i < pixels.Length; i += Depth)
                InitialQuantizePixel(new Color { A = 255, B = pixels[i], G = pixels[i + 1], R = pixels[i + 2] });
        }

        protected virtual byte[] SecondPass(byte[] pixels)
        {
            var output = new List<byte>();
            var previous = new Color { A = 255, B = pixels[0], G = pixels[1], R = pixels[2] };
            var previousByte = QuantizePixel(previous);

            output.Add(previousByte);

            for (var i = Depth; i < pixels.Length; i += Depth)
            {
                if (previous.B != pixels[i] || previous.G != pixels[i + 1] || previous.R != pixels[i + 2])
                {
                    previous = new Color { A = 255, B = pixels[i], G = pixels[i + 1], R = pixels[i + 2] };
                    previousByte = QuantizePixel(previous);
                    output.Add(previousByte);
                }
                else
                    output.Add(previousByte);
            }

            return output.ToArray();
        }


        /// <summary>
        /// Override this to process the pixel in the first pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <remarks>
        /// This function need only be overridden if your quantize algorithm needs two passes,
        /// such as an Octree quantizer.
        /// </remarks>
        protected virtual void InitialQuantizePixel(Color pixel) { }

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <returns>The quantized value</returns>
        protected abstract byte QuantizePixel(Color pixel);

        /// <summary>
        /// Retrieve the palette for the quantized image
        /// </summary>
        /// <returns>The new color palette</returns>
        protected abstract List<Color> GetPalette();
    }
}