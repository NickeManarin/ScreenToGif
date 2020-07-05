using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// Lookup table that holds the already calculated indexes for the colors.
        /// </summary>
        private readonly Hashtable _colorMap = new Hashtable();


        /// <summary>
        /// The image depth.
        /// </summary>
        public int Depth { get; set; } = 4;

        /// <summary>
        /// The maximum color count.
        /// </summary>
        public int MaxColors { get; set; } = 256;

        /// <summary>
        /// The maximum color count, without counting with the transparent color.
        /// </summary>
        public int MaxColorsWithTransparency { get; set; }

        /// <summary>
        /// The calculated color table of the image.
        /// </summary>
        public List<Color> ColorTable { get; set; }

        /// <summary>
        /// The color marked as transparent.
        /// </summary>
        public Color? TransparentColor { get; set; }

        /// <summary>
        /// TODO: The index of the transparent color.
        /// Not always MaxColors - 1, since the color table size is ^2 (...64, 128, 256).
        /// When the user selects a value that doesn't fit nicely in one of those spots (like 200), we can avoid wasting one color position.
        /// </summary>
        public byte TransparentColorIndex 
        { 
            get 
            {
                var max = TransparentColor.HasValue ? MaxColors - 1 : MaxColors;

                //?
                return 0;
            }
        }


        protected Quantizer(bool singlePass)
        {
            _singlePass = singlePass;
        }

        public byte[] Quantize(byte[] pixels, bool secondPassOnly = false)
        {
            #region Validation

            if (MaxColors < 2 || MaxColors > 256)
                throw new ArgumentOutOfRangeException(nameof(MaxColors), MaxColors, "The number of colors should be between 2 and 256");

            #endregion

            //When using a global color table, the analysis should not be executed again. 
            if (!secondPassOnly)
            {
                if (!_singlePass)
                    FirstPass(pixels);

                ColorTable = BuildPalette();
            }

            return SecondPass(pixels);
        }

        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="pixels">The source data</param>
        internal virtual void FirstPass(byte[] pixels)
        {
            for (var i = 0; i < pixels.Length; i += Depth)
                InitialQuantizePixel(Color.FromArgb(pixels[i + 3], pixels[i + 2], pixels[i + 1], pixels[i])); //Pixels are in BGR.
        }

        internal List<Color> GetPalette()
        {
            return ColorTable = BuildPalette();
        }

        internal virtual byte[] ParallelSecondPass(byte[] pixels)
        {
            var output = new byte[pixels.Length / Depth];

            Parallel.For(0, pixels.Length / Depth, index =>
            {
                var trueIndex = index * Depth;

                //Transparent pixels translate to the end of the color table.
                if (pixels[trueIndex + 3] == 0)
                {
                    output[index] = (byte)(ColorTable.Count - 1);
                    return;
                }

                var pixel = new Color
                {
                    B = pixels[trueIndex],
                    G = pixels[trueIndex + 1],
                    R = pixels[trueIndex + 2],
                    A = pixels[trueIndex + 3]
                };

                //lock (output)
                {
                    var hash = BitConverter.ToInt32(new[] { byte.MaxValue, pixel.R, pixel.G, pixel.B }, 0);

                    if (_colorMap.ContainsKey(hash))
                    {
                        output[index] = (byte)_colorMap[hash];
                        return;
                    }

                    var position = QuantizePixel(pixel);

                    output[index] = position;
                    _colorMap.Add(hash, position);
                }
            });

            return output;

            //var output = new List<byte>();
            //
            //for (var index = 0; index < pixels.Length; index += Depth)
            //{
            //    //Transparent pixels translate to the end of the color table.
            //    if (pixels[index + 3] == 0)
            //    {
            //        output.Add((byte)(ColorTable.Count - 1));
            //        continue;
            //    }

            //    var pixel = new Color
            //    {
            //        B = pixels[index], 
            //        G = pixels[index + 1], 
            //        R = pixels[index + 2], 
            //        A = pixels[index + 3]
            //    };

            //    var hash = BitConverter.ToInt32(new[] { byte.MaxValue, pixel.R, pixel.G, pixel.B }, 0);

            //    if (_colorMap.ContainsKey(hash))
            //    {
            //        output.Add((byte) _colorMap[hash]);
            //        continue;
            //    }

            //    var position = QuantizePixel(pixel);

            //    output.Add(position);
            //    _colorMap.Add(hash, position);
            //}
            //
            //return output.ToArray();
        }

        internal virtual byte[] SecondPass(byte[] pixels)
        {
            var output = new List<byte>();

            for (var index = 0; index < pixels.Length; index += Depth)
            {
                //Transparent pixels translate to the end of the color table.
                if (pixels[index + 3] == 0)
                {
                    output.Add((byte)(ColorTable.Count - 1));
                    continue;
                }

                var pixel = new Color
                {
                    B = pixels[index],
                    G = pixels[index + 1],
                    R = pixels[index + 2],
                    A = pixels[index + 3]
                };

                var hash = BitConverter.ToInt32(new[] { byte.MaxValue, pixel.R, pixel.G, pixel.B }, 0);

                if (_colorMap.ContainsKey(hash))
                {
                    output.Add((byte)_colorMap[hash]);
                    continue;
                }

                var position = QuantizePixel(pixel);

                output.Add(position);
                _colorMap.Add(hash, position);
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
        internal abstract List<Color> BuildPalette();
    }
}