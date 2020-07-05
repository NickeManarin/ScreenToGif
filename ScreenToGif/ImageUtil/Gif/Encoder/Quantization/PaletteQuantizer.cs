using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ScreenToGif.ImageUtil.Gif.Encoder.Quantization
{
    public class PaletteQuantizer : Quantizer
    {
        ///<summary>
        ///List of all colors in the palette
        ///</summary>
        protected List<Color> Colors;


        ///<summary>
        ///Construct the palette quantizer.
        ///</summary>
        ///<param name="palette">The color palette to quantize to.</param>
        ///<remarks>
        ///This quantization method only requires a single quantization step when a palette is provided.
        ///</remarks>
        public PaletteQuantizer(ArrayList palette = null) : base(palette != null)
        {
            if (palette == null)
                return;

            Colors = new List<Color>(palette.Cast<Color>());
        }


        ///<summary>
        ///Override this to process the pixel in the second pass of the algorithm.
        ///</summary>
        ///<param name="pixel">The pixel to quantize</param>
        ///<returns>The quantized value</returns>
        protected override byte QuantizePixel(Color pixel)
        {
            byte colorIndex = 0;
            var leastDistance = int.MaxValue;

            //Loop through the entire palette, looking for the closest color match.
            for (var index = 0; index < ColorTable.Count; index++)
            {
                var paletteColor = ColorTable[index];

                var redDistance = paletteColor.R - pixel.R;
                var greenDistance = paletteColor.G - pixel.G;
                var blueDistance = paletteColor.B - pixel.B;

                var distance = (redDistance * redDistance) +
                               (greenDistance * greenDistance) +
                               (blueDistance * blueDistance);

                if (distance < leastDistance)
                {
                    colorIndex = (byte)index;
                    leastDistance = distance;

                    //And if it's an exact match, exit the loop.
                    if (0 == distance)
                        break;
                }
            }

            return colorIndex;
        }

        ///<summary>
        ///Retrieve the palette for the quantized image
        ///</summary>
        ///<returns>The new color palette</returns>
        internal override List<Color> BuildPalette()
        {
            return Colors;
        }
    }
}