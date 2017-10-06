using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenToGif.ImageUtil.Gif.Encoder
{
    /// <summary>
    /// Helper Class that gets and sets image pixels using Marshal calls.
    /// </summary>
    public class PixelUtil
    {
        #region Variables and Properties

        private readonly BitmapSource _source = null;
        private WriteableBitmap _data = null;
        private IntPtr _iptr = IntPtr.Zero;

        /// <summary>
        /// Byte Array containing all pixel information.
        /// </summary>
        public byte[] Pixels { get; set; }

        /// <summary>
        /// Color depth.
        /// </summary>
        public int Depth { get; private set; }

        /// <summary>
        /// Width of the image.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height of the image.
        /// </summary>
        public int Height { get; private set; }

        #endregion

        /// <summary>
        /// Pixel marshalling class, use this to access pixels rapidly.
        /// </summary>
        /// <param name="source">The Bitmap to work with.</param>
        public PixelUtil(BitmapSource source)
        {
            _source = source;
        }

        /// <summary>
        /// Lock bitmap data.
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = _source.PixelWidth;
                Height = _source.PixelHeight;

                // Get total locked pixels count
                var pixelCount = Width * Height;

                // get source bitmap pixel format size
                Depth = _source.Format.BitsPerPixel;

                if (Depth != 32 && Depth != 24)
                    throw new ArgumentException("Only 24 and 32 bpp images are supported.");

                _data = new WriteableBitmap(_source);

                // Lock bitmap and return bitmap data.
                _data.Lock();

                /*
                    https://doanvublog.wordpress.com/tag/32bpp/
                    1,4,8 and 16bpp uses a color table.

                    1bpp : 1 byte, 8 pixels, 2 colors
                    4bpp : 1 byte, 2 pixels, 16 colors
                    8bpp : 1 byte, 1 pixel, 256 colors
                    16bpp : 2 bytes,  1 pixel
                    24bpp : 3 bytes, 1 pixel
                    32bpp : 4 bytes, 1 pixel

                    So, bpp/8 = color chunk size.
                */

                // Create byte array to copy pixel values
                var step = Depth / 8;

                Pixels = new byte[pixelCount * step];
                _iptr = _data.BackBuffer;

                // Copy data from pointer to array
                Marshal.Copy(_iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, _iptr, Pixels.Length);

                // Unlock bitmap data
                _data.Unlock();

                GC.Collect(1);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            // Get color components count
            var count = Depth / 8;

            // Get start index of the specified pixel
            var i = (y * Width + x) * count;

            //It need to have the right amount of pixels left.
            if (i > Pixels.Length - count)
                return Colors.Transparent; //throw new IndexOutOfRangeException();

            var clr = Colors.Transparent;

            if (Depth == 32) //For 32 bpp get Red, Green, Blue and Alpha
            {
                var b = Pixels[i];
                var g = Pixels[i + 1];
                var r = Pixels[i + 2];
                var a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            else if (Depth == 24) //For 24 bpp get Red, Green and Blue
            {
                var b = Pixels[i];
                var g = Pixels[i + 1];
                var r = Pixels[i + 2];
                clr = Color.FromRgb(r, g, b);
            }
            else if (Depth == 8 || Depth == 4 || Depth == 2 || Depth == 1) //For smaller bpp values, access the Palette.
            {
                //TODO: To access color information for smaller bpp, I need to divide the byte into bits and access the Palette.

                var index = (int)Pixels[i];

                if (_source.Palette != null)
                    clr = _source.Palette.Colors[index];
            }

            return clr;
        }

        public List<Color> GetAllPixels()
        {
            var list = new List<Color>();

            //Old way, line by line. This order is very important!!!
            //for (var y = 0; y < image.PixelHeight; y++)
            //{
            //    for (var x = 0; x < image.PixelWidth; x++)
            //    {
            //        list.Add(pixelUtil.GetPixel(x, y));
            //    }
            //}

            if (Depth == 32) //For 32 bpp get Red, Green, Blue and Alpha
            {
                for (var i = 0; i + 3 < Pixels.Length; i += 4)
                    list.Add(new Color {B = Pixels[i], G = Pixels[i + 1], R = Pixels[i + 2], A = Pixels[i + 3]});

                //list = Pixels.Select((x, i) => new { x, i }).GroupBy(x => x.i / 4).Select(g => g.ToList()).Select(g => new Color { B = g[0].x, G = g[1].x, R = g[2].x, A = g[3].x }).ToList();
                //list = Enumerable.Range(0, Pixels.Length / 4).ToLookup(i => new Color{ B = Pixels[i * 3], G = Pixels[i * 3 + 1], R = Pixels[i * 3 + 2], A = Pixels[i * 3 + 3] }).Cast<Color>().ToList();
            }
            else if (Depth == 24) //For 24 bpp get Red, Green and Blue
            {
                for (var i = 0; i + 2 < Pixels.Length; i += 3)
                    list.Add(new Color {B = Pixels[i], G = Pixels[i + 1], R = Pixels[i + 2]});

                //list = Pixels.Select((x, i) => new { x, i }).GroupBy(x => x.i / 3).Select(g => g.ToList()).Select(g => new Color { R = g[0].x, G = g[1].x, B = g[2].x }).ToList();
                //list = Enumerable.Range(0, Pixels.Length / 3).ToLookup(i => new Color { B = Pixels[i * 3], G = Pixels[i * 3 + 1], R = Pixels[i * 3 + 2]}).Cast<Color>().ToList();
            }

            return list;
        }

        public List<Color> GetAllPixels(bool sim)
        {
            var list = new List<Color>();

            return list;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            //Get color components count
            var count = Depth / 8;

            //Get start index of the specified pixel
            var i = (y * Width + x) * count;

            //Ignore if out of bounds.
            if (i > Pixels.Length - count)
                return;

            if (Depth == 32) //For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }

            else if (Depth == 24) //For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
        }
    }
}
