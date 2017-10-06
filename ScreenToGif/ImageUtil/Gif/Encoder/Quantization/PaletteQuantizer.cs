using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ScreenToGif.ImageUtil.Gif.Encoder.Quantization
{
    public class PaletteQuantizer : Quantizer
    {
        ///<summary>
        ///Construct the palette quantizer
        ///</summary>
        ///<param name="palette">The color palette to quantize to</param>
        ///<remarks>
        ///Palette quantization only requires a single quantization step
        ///</remarks>
        public PaletteQuantizer(ArrayList palette) : base(true)
        {
            _colorMap = new Hashtable();

            Colors = new Color[palette.Count];
            palette.CopyTo(Colors);
        }

        ///<summary>
        ///Override this to process the pixel in the second pass of the algorithm.
        ///</summary>
        ///<param name="pixel">The pixel to quantize</param>
        ///<returns>The quantized value</returns>
        protected override byte QuantizePixel(Color pixel)
        {
            byte colorIndex = 0;
            var colorHash = BitConverter.ToInt32(new[] { pixel.A, pixel.R, pixel.G, pixel.B }, 0);

            //Check if the color is in the lookup table.
            if (_colorMap.ContainsKey(colorHash))
                colorIndex = (byte)_colorMap[colorHash];
            else
            {
                //Not found - loop through the palette and find the nearest match.
                //Firstly check the alpha value - if 0, lookup the transparent color.
                if (0 == pixel.A)
                {
                    //Transparent. Lookup the first color with an alpha value of 0.
                    for (var index = 0; index < Colors.Length; index++)
                    {
                        if (0 != Colors[index].A) continue;

                        colorIndex = (byte)index;
                        break;
                    }
                }
                else
                {
                    //Not transparent...
                    var leastDistance = int.MaxValue;
                    int red = pixel.R;
                    int green = pixel.G;
                    int blue = pixel.B;

                    //Loop through the entire palette, looking for the closest color match
                    for (var index = 0; index < Colors.Length; index++)
                    {
                        var paletteColor = Colors[index];

                        var redDistance = paletteColor.R - red;
                        var greenDistance = paletteColor.G - green;
                        var blueDistance = paletteColor.B - blue;

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
                }

                //Now I have the color, pop it into the hashtable for next time.
                _colorMap.Add(colorHash, colorIndex);
            }

            return colorIndex;
        }

        ///<summary>
        ///Retrieve the palette for the quantized image
        ///</summary>
        ///<returns>The new color palette</returns>
        protected override List<Color> GetPalette()
        {
            return Colors.ToList();
        }

        ///<summary>
        ///Lookup table for colors
        ///</summary>
        private readonly Hashtable _colorMap;

        ///<summary>
        ///List of all colors in the palette
        ///</summary>
        protected Color[] Colors;
    }
}
