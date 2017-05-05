using System.Windows.Media;

namespace ScreenToGif.ImageUtil.Quantization
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
        private readonly int _depth;

        public Quantizer(bool singlePass, int depth)
        {
            _singlePass = singlePass;
            _depth = depth;
        }

        public void Quantize(byte[] pixels, int width, int height)
        {
            if (!_singlePass)
                FirstPass(pixels, width, height);
        }

        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="pixels">The source data</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        protected virtual void FirstPass(byte[] pixels, int width, int height)
        {
            var pixelSize = _depth == 32 ? 4 : 3;

            for (var i = 0; i < pixels.Length; i =+ pixelSize)
            {
                InitialQuantizePixel(new Color {B = pixels[i], G = pixels[i + 1], R = pixels[i + 2]});
            }
        }




        /// <summary>
        /// Override this to process the pixel in the first pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <remarks>
        /// This function need only be overridden if your quantize algorithm needs two passes,
        /// such as an Octree quantizer.
        /// </remarks>
        protected virtual Color InitialQuantizePixel(Color pixel)
        {
            return Colors.Transparent;
        }

    }
}
