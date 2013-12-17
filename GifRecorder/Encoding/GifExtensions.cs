using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScreenToGif.Encoding
{
    public static class GifExtensions
    {
        #region Images to Animated Gif Image file

        /// <summary>Saves the images as frames to an animated Gif Image.</summary>
        /// <param name="images">The images to save.</param>
        /// <param name="path">The path of the Gif file to create.</param>
        /// <param name="delay">The delay between frames, in milliseconds.</param>
        /// <param name="repeat">The number of times the animation should repeat. Leave this zero 
        /// for it to loop forever, or specify a value to limit the number of repetitions.</param>
        public static void SaveAnimatedGifImage(this IEnumerable<Image> images, string path, int delay = 100, int repeat = 0) //delay as a IEnumerable too, later
        {
            var imageArray = images.ToArray();

            using (var stream = new MemoryStream())
            {
                using (var encoder = new GifEncoder(stream, null, null, repeat))
                {
                    for (int i = 0; i < imageArray.Length; i++)
                    {
                        encoder.AddFrame((imageArray[i] as Bitmap).CopyImage(), 0, 0, TimeSpan.FromMilliseconds(delay));
                    }
                }

                stream.Position = 0;

                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, Constants.BufferSize, false))
                {
                    stream.WriteTo(fileStream);
                }
            }
        }

        #endregion

        #region Animated Gif Image to Images

        /// <summary>Extracts the frames and the delay for each frame from an animated Gif Image.</summary>
        /// <returns>An enumerable of key-value pairs, where the key is the image, and the value is that 
        /// frame's delay, in milliseconds.</returns>
        public static IEnumerable<KeyValuePair<Image, int>> ExportFrames(this Image gifImage)
        {
            if (gifImage.RawFormat.Equals(ImageFormat.Gif) && ImageAnimator.CanAnimate(gifImage))
            {
                var frameDimension = new FrameDimension(gifImage.FrameDimensionsList[0]);
                var frameCount = gifImage.GetFrameCount(frameDimension);

                var delay = 0;
                var index = 0;

                for (int i = 0; i < frameCount; i++)
                {
                    delay = BitConverter.ToInt32(gifImage.GetPropertyItem(20736).Value, index) * 10;
                    index += Marshal.SizeOf(index);

                    gifImage.SelectActiveFrame(frameDimension, i);
                    yield return new KeyValuePair<Image, int>(gifImage.CopyImage(), delay);
                }
            }
        }

        #endregion
    }
}
