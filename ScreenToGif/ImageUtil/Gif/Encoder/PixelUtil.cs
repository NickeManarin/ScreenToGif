using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
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

        public IntPtr BackBuffer { get; set; } = IntPtr.Zero;

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
            //Get width and height of bitmap.
            Width = _source.PixelWidth;
            Height = _source.PixelHeight;

            //Get total locked pixels count.
            var pixelCount = Width * Height;

            //Get source bitmap pixel format size.
            Depth = _source.Format.BitsPerPixel;

            if (Depth != 32 && Depth != 24)
                throw new ArgumentException("Only 24 and 32 bpp images are supported.");

            _data = new WriteableBitmap(_source);

            //Lock bitmap and return bitmap data.
            _data.Lock();

            /*
                https://doanvublog.wordpress.com/tag/32bpp/
                1,4,8 and 16bpp uses a color table.

                1bpp : 1 byte, 8 pixels, 2 colors
                4bpp : 1 byte, 2 pixels, 16 colors
                8bpp : 1 byte, 1 pixel, 256 colors
                16bpp : 2 bytes, 1 pixel
                24bpp : 3 bytes, 1 pixel
                32bpp : 4 bytes, 1 pixel

                So, bpp/8 = color chunk size.
            */

            //Create byte array to copy pixel values.
            var step = Depth / 8;

            Pixels = new byte[pixelCount * step];
            BackBuffer = _data.BackBuffer;

            //Copy data from pointer to array.
            Marshal.Copy(BackBuffer, Pixels, 0, Pixels.Length);
        }

        public void LockBitsAndUnpad()
        {
            //Get width and height of bitmap.
            Width = _source.PixelWidth;
            Height = _source.PixelHeight;

            //Get total locked pixels count.
            var pixelCount = Width * Height;

            //Get source bitmap pixel format size.
            Depth = _source.Format.BitsPerPixel;

            if (Depth != 32 && Depth != 24)
                throw new ArgumentException("Only 24 and 32 bpp images are supported.");

            _data = new WriteableBitmap(_source);

            //Lock bitmap and return bitmap data.
            _data.Lock();

            /*
                https://doanvublog.wordpress.com/tag/32bpp/
                1,4,8 and 16bpp uses a color table.

                1bpp : 1 byte, 8 pixels, 2 colors
                4bpp : 1 byte, 2 pixels, 16 colors
                8bpp : 1 byte, 1 pixel, 256 colors
                16bpp : 2 bytes, 1 pixel
                24bpp : 3 bytes, 1 pixel
                32bpp : 4 bytes, 1 pixel

                So, bpp/8 = color chunk size.
            */

            //Create byte array to copy pixel values.
            var step = Depth / 8;

            //Adjust to necessary padding.
            var bytesPerRow = Width * step; 
            var pad = bytesPerRow % 4 != 0 ? 4 - bytesPerRow % 4 : 0;

            Pixels = new byte[pixelCount * step];
            BackBuffer = _data.BackBuffer;

            //Copy data from pointer to array normally, if it has no padding.
            if (pad == 0)
            {
                Marshal.Copy(BackBuffer, Pixels, 0, Pixels.Length);
                return;
            }

            //Removes the pad from the pixel array.
            for (var row = 0; row < Height; row++)
                Marshal.Copy(new IntPtr(BackBuffer.ToInt64() + row * (bytesPerRow + pad)), Pixels, row * bytesPerRow, bytesPerRow);
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public WriteableBitmap UnlockBits()
        {
            //Copy data from byte array to pointer.
            Marshal.Copy(Pixels, 0, BackBuffer, Pixels.Length);

            //Unlock bitmap data.
            _data.Unlock();

            GC.Collect(1);

            return _data;
        }

        public WriteableBitmap UnlockBitsWithoutCommit()
        {
            //Unlock bitmap data.
            _data.Unlock();

            GC.Collect(1);

            return _data;
        }

        public WriteableBitmap UnlockBitsAndCrop(Int32Rect rect)
        {
            #region Crop

            var sourceWidth = _data.PixelWidth;
            var blockSize = _source.Format.BitsPerPixel / 8;
            var outputPixels = new byte[rect.Width * rect.Height * blockSize];

            //Create the array of bytes.
            for (var line = 0; line <= rect.Height - 1; line++)
            {
                var sourceIndex = ((rect.Y + line) * sourceWidth + rect.X) * blockSize;
                var destinationIndex = line * rect.Width * blockSize;

                Array.Copy(Pixels, sourceIndex, outputPixels, destinationIndex, rect.Width * blockSize);
            }

            #endregion

            //Get the resultant image as WriteableBitmap with specified size.
            var result = new WriteableBitmap(rect.Width, rect.Height, _source.DpiX, _source.DpiY, _source.Format, _source.Palette);
            result.Lock();

            //for (var line = 0; line <= rect.Height - 1; line++)
            //{
            //    var sourceIndex = ((rect.Y + line) * sourceWidth + rect.X) * blockSize;
            //    var destinationIndex = line * rect.Width * blockSize;

            //    //Native.MemoryCopy(Marshal.UnsafeAddrOfPinnedArrayElement(outputPixels, destinationIndex), IntPtr.Add(result.BackBuffer, sourceIndex), new UIntPtr((uint) rect.Width * (uint) blockSize));

            //    //Array.Copy(Pixels, sourceIndex, outputPixels, destinationIndex, rect.Width * blockSize);
            //    //Marshal.Copy(outputPixels, sourceIndex, result.BackBuffer, rect.Width * blockSize); //Errado.
            //}

            Marshal.Copy(outputPixels, 0, result.BackBuffer, outputPixels.Length);

            result.Unlock();
            _data.Unlock();

            GC.Collect(1);
            return result;
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            //Get color components count.
            var count = Depth / 8;

            //Get start index of the specified pixel.
            var i = (y * Width + x) * count;

            //It needs to have the right amount of pixels left.
            if (i > Pixels.Length - count)
                return Colors.Transparent; //throw new IndexOutOfRangeException();

            var clr = Colors.Transparent;

            if (Depth == 32) //For 32 bpp get Red, Green, Blue and Alpha.
            {
                var b = Pixels[i];
                var g = Pixels[i + 1];
                var r = Pixels[i + 2];
                var a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            else if (Depth == 24) //For 24 bpp get Red, Green and Blue.
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

        public Color GetMedianColor(int xx, int yy, int offsetX, int offsetY)
        {
            int r = 0, g = 0, b = 0, mult = 0;

            var count = Depth / 8;

            for (var x = xx; x < offsetX + xx; x++)
            {
                for (var y = yy; y < offsetY + yy; y++)
                {
                    var i = (y * Width + x) * count;

                    if (i > Pixels.Length - count)
                        continue;

                    b += Pixels[i];
                    g += Pixels[i + 1];
                    r += Pixels[i + 2];
                    mult++;
                }
            }

            return Color.FromArgb(255, (byte)(r / mult), (byte)(g / mult), (byte)(b / mult));
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
                    list.Add(new Color { B = Pixels[i], G = Pixels[i + 1], R = Pixels[i + 2], A = Pixels[i + 3] });

                //list = Pixels.Select((x, i) => new { x, i }).GroupBy(x => x.i / 4).Select(g => g.ToList()).Select(g => new Color { B = g[0].x, G = g[1].x, R = g[2].x, A = g[3].x }).ToList();
                //list = Enumerable.Range(0, Pixels.Length / 4).ToLookup(i => new Color{ B = Pixels[i * 3], G = Pixels[i * 3 + 1], R = Pixels[i * 3 + 2], A = Pixels[i * 3 + 3] }).Cast<Color>().ToList();
            }
            else if (Depth == 24) //For 24 bpp get Red, Green and Blue
            {
                for (var i = 0; i + 2 < Pixels.Length; i += 3)
                    list.Add(new Color { B = Pixels[i], G = Pixels[i + 1], R = Pixels[i + 2] });

                //list = Pixels.Select((x, i) => new { x, i }).GroupBy(x => x.i / 3).Select(g => g.ToList()).Select(g => new Color { R = g[0].x, G = g[1].x, B = g[2].x }).ToList();
                //list = Enumerable.Range(0, Pixels.Length / 3).ToLookup(i => new Color { B = Pixels[i * 3], G = Pixels[i * 3 + 1], R = Pixels[i * 3 + 2]}).Cast<Color>().ToList();
            }

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