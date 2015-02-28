using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenToGif.Encoding
{
    /// <summary>
    /// Helper Class that gets and sets image pixels using Marshal calls.
    /// </summary>
    public class PixelUtil
    {
        readonly Bitmap _source = null;
        IntPtr _iptr = IntPtr.Zero;
        BitmapData _bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Pixel marshalling class, use this to access pixels rapidly.
        /// </summary>
        /// <param name="source">The Bitmap to work with</param>
        public PixelUtil(Bitmap source)
        {
            //this.source = new Bitmap(source); //Not working
            this._source = source;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = _source.Width;
                Height = _source.Height;

                // Get total locked pixels count
                int pixelCount = Width * Height;

                // Create rectangle to lock
                var rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = Image.GetPixelFormatSize(_source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                _bitmapData = _source.LockBits(rect, ImageLockMode.ReadWrite,
                                             _source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[pixelCount * step];
                _iptr = _bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(_iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception e)
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
                _source.UnlockBits(_bitmapData);
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
            Color clr = Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) //For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) //For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8) //For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
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
            int cCount = Depth / 8;

            //Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (Depth == 32) //For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) //For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8) //For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }
    }
}
