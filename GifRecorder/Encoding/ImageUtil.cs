using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ScreenToGif.Encoding
{
    public static class ImageUtil
    {
        public static List<Bitmap> ResizeAllBitmap(List<Bitmap> listFrames, int nWidth, int nHeight)
        {
            var listResize = new List<Bitmap>();

            foreach (Bitmap listFrame in listFrames)
            {
                Bitmap result = new Bitmap(nWidth, nHeight);
                using (Graphics g = Graphics.FromImage(result))
                    g.DrawImage(listFrame, 0, 0, nWidth, nHeight);
                listResize.Add(result);
            }
            listFrames.Clear();
            GC.Collect();
            return listResize;
        }

        public static Bitmap ResizeBitmap(Bitmap bitmap, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(bitmap, 0, 0, nWidth, nHeight);
            return result;
        }

        public static List<Bitmap> Crop(List<Bitmap> list, Rectangle cropArea)
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap img in list)
            {
                Bitmap bmpImage = new Bitmap(img);
                Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
                edit.Add(bmpCrop);
            }
            return edit;
        }

        public static List<Bitmap> GrayScale(List<Bitmap> list) //For all
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(MakeGrayscale(bitmap));
            }

            return edit;
        }

        public static List<Bitmap> Pixelate(List<Bitmap> list, Rectangle rectangle, Int32 pixelateSize) //For all
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Pixelate(bitmap, rectangle, pixelateSize));
            }

            return edit;
        }

        public static List<Bitmap> Blur(List<Bitmap> list, Rectangle rectangle, Int32 blurSize) //For all
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Blur(bitmap, rectangle, blurSize));
            }

            return edit;
        }

        public static List<Bitmap> Revert(List<Bitmap> list)
        {
            List<Bitmap> finalList = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                finalList.Insert(0, bitmap);
            }

            return finalList;
        }

        public static List<Bitmap> Yoyo(List<Bitmap> list)
        {
            list.AddRange(Revert(list));
            return list;
        }

        public static Bitmap MakeGrayscale(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(

                new float[][]
                {
                     new float[] {.3f, .3f, .3f, 0, 0},
                     new float[] {.59f, .59f, .59f, 0, 0},
                     new float[] {.11f, .11f, .11f, 0, 0},
                     new float[] {0, 0, 0, 1, 0},
                     new float[] {0, 0, 0, 0, 1}
                }
                  );

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        public static Bitmap Pixelate(Bitmap image, Rectangle rectangle, Int32 pixelateSize)
        {
            Bitmap pixelated = new System.Drawing.Bitmap(image.Width, image.Height);

            // make an exact copy of the bitmap provided
            using (Graphics graphics = System.Drawing.Graphics.FromImage(pixelated))
                graphics.DrawImage(image, new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            // look at every pixel in the rectangle while making sure we're within the image bounds
            for (Int32 xx = rectangle.X; xx < rectangle.X + rectangle.Width && xx < image.Width; xx += pixelateSize)
            {
                for (Int32 yy = rectangle.Y; yy < rectangle.Y + rectangle.Height && yy < image.Height; yy += pixelateSize)
                {
                    Int32 offsetX = pixelateSize / 2;
                    Int32 offsetY = pixelateSize / 2;

                    // make sure that the offset is within the boundry of the image
                    while (xx + offsetX >= image.Width) offsetX--;
                    while (yy + offsetY >= image.Height) offsetY--;

                    // get the pixel color in the center of the soon to be pixelated area
                    Color pixel = pixelated.GetPixel(xx + offsetX, yy + offsetY);

                    // for each pixel in the pixelate size, set it to the center color
                    for (Int32 x = xx; x < xx + pixelateSize && x < image.Width; x++)
                        for (Int32 y = yy; y < yy + pixelateSize && y < image.Height; y++)
                            pixelated.SetPixel(x, y, pixel);
                }
            }

            return pixelated;
        }

        public static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            Bitmap blurred = new Bitmap(image.Width, image.Height);

            // make an exact copy of the bitmap provided
            using (Graphics graphics = Graphics.FromImage(blurred))
                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            // look at every pixel in the blur rectangle
            for (Int32 xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (Int32 yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    Int32 avgR = 0, avgG = 0, avgB = 0;
                    Int32 blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (Int32 x = xx; (x < xx + blurSize && x < image.Width); x++)
                    {
                        for (Int32 y = yy; (y < yy + blurSize && y < image.Height); y++)
                        {
                            Color pixel = blurred.GetPixel(x, y);

                            avgR += pixel.R;
                            avgG += pixel.G;
                            avgB += pixel.B;

                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (Int32 x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++)
                        for (Int32 y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++)
                            blurred.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                }
            }

            return blurred;
        }

        public static Bitmap Colorize(Bitmap Image, Color[] Colors)
        {
            if (Colors.Length < 256)
                return null;
            Bitmap TempBitmap = new Bitmap(Image.Width, Image.Height);
            for (int x = 0; x < Image.Width; ++x)
            {
                for (int y = 0; y < Image.Height; ++y)
                {
                    int ColorUsing = Image.GetPixel(x, y).R;
                    TempBitmap.SetPixel(x, y, Colors[ColorUsing]);
                }
            }
            return TempBitmap;
        }
        
        /// <summary>
        /// Convert given image to negative bitmap
        /// </summary>
        /// <param name="OriginalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap Negative(Image OriginalImage)
        {
            return OriginalImage.DrawAsNegative();            
        }

        /// <summary>
        /// Convert each bitmap in the given list to negative filter
        /// </summary>
        /// <param name="list">System.Collections.Generic.List of System.Drawing.Bitmap to convert</param>
        /// <returns>Converted System.Collections.Generic.List of System.Drawing.Bitmap </returns>
        public static List<Bitmap> Negative(List<Bitmap> list) 
        {            
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Negative(bitmap));
            }
            
            return edit;
        }

        /// <summary>
        /// Convert given image to transparency bitmap
        /// </summary>
        /// <param name="OriginalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap Transparency(Image OriginalImage)
        {
            return OriginalImage.DrawWithTransparency();            
        }
        
        /// <summary>
        /// Convert each bitmap in the given list to transparency filter
        /// </summary>
        /// <param name="list">System.Collections.Generic.List 
        /// of System.Drawing.Bitmap to convert</param>
        /// <returns>Converted System.Collections.Generic.List 
        /// of System.Drawing.Bitmap</returns>
        public static List<Bitmap> Transparency(List<Bitmap> list)
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Transparency(bitmap));
            }

            return edit;
        }

        #region Filters

        /// <summary>
        /// Convert given image to negative filter
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap to converted</returns>
        public static Bitmap DrawAsNegative(this Image sourceImage)
        {
            ColorMatrix colorMatrix = new ColorMatrix(new float[][] 
                    {
                            new float[]{-1, 0, 0, 0, 0},
                            new float[]{0, -1, 0, 0, 0},
                            new float[]{0, 0, -1, 0, 0},
                            new float[]{0, 0, 0, 1, 0},
                            new float[]{1, 1, 1, 1, 1}
                    });

            return ApplyColorMatrix(sourceImage, colorMatrix);
        }

        /// <summary>
        /// Convert given image to transparency filter 
        /// and reduce the Alpha component by 50%
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap to converted</returns>
        public static Bitmap DrawWithTransparency(this Image sourceImage)
        {
            ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                        {
                            new float[]{1, 0, 0, 0, 0},
                            new float[]{0, 1, 0, 0, 0},
                            new float[]{0, 0, 1, 0, 0},
                            new float[]{0, 0, 0, 0.5f, 0},
                            new float[]{0, 0, 0, 0, 1}
                        });

            return ApplyColorMatrix(sourceImage, colorMatrix);
        }
        #endregion

        #region ApplyFilter
        /// <summary>
        /// Intend to apply the specified ColorMatrix upon the Image parameter specified
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image</param>
        /// <param name="colorMatrix">System.Drawing.Imaging.ColorMatrix</param>
        /// <returns>System.Drawing.Bitmap, the result of the process</returns>
        private static Bitmap ApplyColorMatrix(Image sourceImage, ColorMatrix colorMatrix)
        {
            Bitmap bmp32BppSource = GetArgbCopy(sourceImage);
            Bitmap bmp32BppDest = new Bitmap(bmp32BppSource.Width, bmp32BppSource.Height, PixelFormat.Format32bppArgb);
            
            using (Graphics graphics = Graphics.FromImage(bmp32BppDest))
            {
                ImageAttributes bmpAttributes = new ImageAttributes();
                bmpAttributes.SetColorMatrix(colorMatrix);

                graphics.DrawImage(bmp32BppSource, new Rectangle(0, 0, bmp32BppSource.Width, bmp32BppSource.Height),
                                    0, 0, bmp32BppSource.Width, bmp32BppSource.Height, GraphicsUnit.Pixel, bmpAttributes);
            }

            bmp32BppSource.Dispose();
            return bmp32BppDest;
        }

        /// <summary>
        /// Convert the given image to a 32Bit ARGB format, this format will be used 
        /// for converting image to different filters
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>converted System.Drawing.Bitmap</returns>
        private static Bitmap GetArgbCopy(Image sourceImage)
        {
            Bitmap bmpNew = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bmpNew))
            {
                graphics.DrawImage(sourceImage, new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), GraphicsUnit.Pixel);
                graphics.Flush();
            }

            return bmpNew;
        }
        #endregion

        #region CopyImage

        /// <summary>Creates a 24 bit-per-pixel copy of the source image.</summary>
        public static Image CopyImage(this Image image)
        {
            return CopyImage(image, PixelFormat.Format24bppRgb);
        }


        /// <summary>Creates a copy of the source image with the specified pixel format.</summary><remarks>
        /// This can also be achieved with the <see cref="System.Drawing.Bitmap.Clone(int, int, PixelFormat)"/>
        /// overload, but I have had issues with that method.</remarks>
        public static Image CopyImage(this Image image, PixelFormat format)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            // Don't try to draw a new Bitmap with an indexed pixel format.
            if (format == PixelFormat.Format1bppIndexed || format == PixelFormat.Format4bppIndexed || format == PixelFormat.Format8bppIndexed || format == PixelFormat.Indexed)
                return (image as Bitmap).Clone(new Rectangle(0, 0, image.Width, image.Height), format);

            Image result = null;
            try
            {
                result = new Bitmap(image.Width, image.Height, format);

                using (var graphics = Graphics.FromImage(result))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                    graphics.DrawImage(image, 0, 0, result.Width, result.Height);
                }
            }
            catch
            {
                if (result != null)
                    result.Dispose();

                throw;
            }
            return result;
        }

        #endregion
    }
}
