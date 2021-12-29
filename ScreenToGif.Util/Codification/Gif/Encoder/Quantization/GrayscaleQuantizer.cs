using System.Collections;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Codification.Gif.Encoder.Quantization;

public class GrayscaleQuantizer : PaletteQuantizer
{
    /// <summary>
    /// Grayscale palette quantizer.
    /// </summary>
    /// <remarks>
    /// Palette quantization only requires a single quantization step, because there's no need to build the palette.
    /// </remarks>
    public GrayscaleQuantizer(Color? transparent = null, int maxColors = 256) : base(new ArrayList())
    {
        Colors = new List<Color>(maxColors);

        MaxColorsWithTransparency = transparent.HasValue ? maxColors - 1 : maxColors;

        //Initialize a new color table with entries that are determined by some optimal palette-finding algorithm.
        for (var i = 0; i < MaxColorsWithTransparency; i++)
        {
            //Even distribution of grayscale colors. 
            var intensity = Convert.ToUInt32(i * 0xFF / (MaxColorsWithTransparency - 1));

            Colors.Add(Color.FromArgb(0xFF, (byte)intensity, (byte)intensity, (byte)intensity));
        }

        if (transparent.HasValue)
            Colors.Add(transparent.Value);
    }

    /// <summary>
    /// Override this to process the pixel in the second pass of the algorithm
    /// </summary>
    /// <param name="pixel">The pixel to quantize</param>
    /// <returns>The quantized value</returns>
    protected override byte QuantizePixel(Color pixel)
    {
        var luminance = pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114;

        //Gray scale is an intensity map from black to white.
        //Compute the index to the grayscale entry that approximates the luminance, and then round the index.
        //Also, constrain the index choices by the number of colors to do, and then set that pixel's index to the byte value.

        //return (byte)((int)((luminance + 0.5) * Colors.Length) >> 8); //Without transparency.
        //return (byte)(luminance + 0.5); //Without configurable color count.

        return (byte)((int)((luminance + 0.5) * MaxColorsWithTransparency) >> 8); //Returns the color index.
    }
}