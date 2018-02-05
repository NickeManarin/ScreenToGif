using System;
using System.Collections;
using System.Windows.Media;

namespace ScreenToGif.ImageUtil.Gif.Encoder.Quantization
{
    public class GrayscaleQuantizer : PaletteQuantizer
    {
        /// <summary>
        /// Construct the palette quantizer
        /// </summary>
        /// <remarks>
        /// Palette quantization only requires a single quantization step.
        /// </remarks>
        public GrayscaleQuantizer() : base(new ArrayList())
        {
            Colors = new Color[MaxColors];

            var nColors = MaxColors;

            // Initialize a new color table with entries that are determined
            // by some optimal palette-finding algorithm; for demonstration 
            // purposes, use a grayscale.
            for (uint i = 0; i < nColors; i++)
            {
                var intensity = Convert.ToUInt32(i * 0xFF / (nColors - 1));    // Even distribution. 

                // The GIF encoder makes the first entry in the palette
                // that has a ZERO alpha the transparent color in the GIF.
                // Pick the first one arbitrarily, for demonstration purposes.

                // Create a gray scale for demonstration purposes.
                // Otherwise, use your favorite color reduction algorithm
                // and an optimum palette for that algorithm generated here.
                // For example, a color histogram, or a median cut palette.
                Colors[i] = Color.FromArgb(0xFF, (byte)intensity, (byte)intensity, (byte)intensity);
            }
        }

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <returns>The quantized value</returns>
        protected override byte QuantizePixel(Color pixel)
        {
            var luminance = pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114;

            // Gray scale is an intensity map from black to white.
            // Compute the index to the grayscale entry that
            // approximates the luminance, and then round the index.
            // Also, constrain the index choices by the number of
            // colors to do, and then set that pixel's index to the 
            // byte value.
            
            return (byte)(luminance + 0.5); //Returns the color index.
        }
    }
}