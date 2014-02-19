using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Encoding
{
    /// <summary>
    /// Holds several image algorithms used by the program.
    /// </summary>
    public static class ImageUtil
    {
        #region Resize

        /// <summary>
        /// Resizes a List of Bitmaps.
        /// </summary>
        /// <param name="listFrames">The List of Bitmap to be resized.</param>
        /// <param name="nWidth">The desired Width</param>
        /// <param name="nHeight">The desired Height</param>
        /// <returns>The resized List of Bitmap</returns>
        public static List<Bitmap> ResizeBitmap(List<Bitmap> listFrames, int nWidth, int nHeight)
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

        /// <summary>
        /// Resizes the given Bitmap. 
        /// </summary>
        /// <param name="bitmap">The Bitmap to be resized</param>
        /// <param name="nWidth">The desired Width</param>
        /// <param name="nHeight">The desired Height</param>
        /// <returns>The resized Bitmap</returns>
        public static Bitmap ResizeBitmap(Bitmap bitmap, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(bitmap, 0, 0, nWidth, nHeight);
            return result;
        }

        #endregion

        #region Crop

        /// <summary>
        /// Crops the all the Bitmaps of given List.
        /// </summary>
        /// <param name="list">The List to be croped.</param>
        /// <param name="cropArea">The Crop area</param>
        /// <returns>The croped Bitmaps</returns>
        public static List<Bitmap> Crop(IEnumerable<Bitmap> list, Rectangle cropArea)
        {
            var edit = new List<Bitmap>();
            foreach (Bitmap img in list)
            {
                var bmpImage = new Bitmap(img);
                Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
                edit.Add(bmpCrop);
            }
            return edit;
        }

        #endregion

        #region Obsolete

        [Obsolete]
        public static List<Bitmap> GrayScale(List<Bitmap> list) //For all
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(MakeGrayscale(bitmap));
            }

            return edit;
        }

        [Obsolete]
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

        #endregion

        #region Yo-yo

        public static List<Bitmap> Revert(List<Bitmap> list)
        {
            
            List<Bitmap> finalList = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                finalList.Insert(0, bitmap);
            }

            return finalList;
        }

        /// <summary>
        /// Makes a Yo-yo efect with the given List (List + Reverted List)
        /// </summary>
        /// <param name="list">The list to apply the efect</param>
        /// <returns>A List with the Yo-yo efect</returns>
        public static List<Bitmap> Yoyo(List<Bitmap> list)
        {
            list.AddRange(Revert(list));
            //should we remove the first frame of de reverted part, so it won't repeat?
            return list;
        }

        #endregion

        #region Pixelate

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

        public static List<Bitmap> Pixelate(List<Bitmap> list, Rectangle rectangle, Int32 pixelateSize)
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Pixelate(bitmap, rectangle, pixelateSize));
            }

            return edit;
        }

        #endregion

        #region Blur

        public static List<Bitmap> Blur(List<Bitmap> list, Rectangle rectangle, Int32 blurSize) //For all
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Blur(bitmap, rectangle, blurSize));
            }

            return edit;
        }

        /// <summary>
        /// Apply smooth efect on image
        /// </summary>
        /// <param name="image">System.Drawing.Bitmap that will receive blur efect</param>
        /// <param name="rectangle">System.Drawing.Rectangle, the area to apply the efect</param>
        /// <param name="blurSize">System.Int32, the intensity of the blur</param>
        /// <returns>System.Drawing.Bitmap with apllied colors</returns>
        public static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            //Bitmap blurred = new Bitmap(image.Width, image.Height);

            //// make an exact copy of the bitmap provided
            //using (Graphics graphics = Graphics.FromImage(blurred))
            //    graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
            //        new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            Bitmap blurred = new Bitmap(image);

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

        #endregion

        #region Grayscale

        /// <summary>
        /// Convert given image to grayscale bitmap.
        /// </summary>
        /// <param name="OriginalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap Grayscale(Image OriginalImage)
        {
            return OriginalImage.DrawAsGrayscale();
        }

        /// <summary>
        /// Convert each bitmap in the given list to grayscale filter
        /// </summary>
        /// <param name="list">System.Collections.Generic.List of System.Drawing.Bitmap to convert</param>
        /// <returns>Converted System.Collections.Generic.List of System.Drawing.Bitmap </returns>
        public static List<Bitmap> Grayscale(List<Bitmap> list)
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Grayscale(bitmap));
            }

            return edit;
        }

        #endregion

        #region Negative

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

        #endregion

        #region Transparency

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

        #endregion

        #region SepiaTone

        /// <summary>
        /// Convert given image to transparency bitmap
        /// </summary>
        /// <param name="OriginalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap SepiaTone(Image OriginalImage)
        {
            return OriginalImage.DrawAsSepiaTone();
        }

        /// <summary>
        /// Convert each bitmap in the given list to SepiaTone filter
        /// </summary>
        /// <param name="list">System.Collections.Generic.List 
        /// of System.Drawing.Bitmap to convert</param>
        /// <returns>Converted System.Collections.Generic.List 
        /// of System.Drawing.Bitmap</returns>
        public static List<Bitmap> SepiaTone(List<Bitmap> list)
        {
            List<Bitmap> edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(SepiaTone(bitmap));
            }

            return edit;
        }

        #endregion

        #region Filters

        /// <summary>
        /// Convert given image to grayscale filter.
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>Converted System.Drawing.Bitmap</returns>
        private static Bitmap DrawAsGrayscale(this Image sourceImage)
        {
            ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                {
                     new float[] {.3f, .3f, .3f, 0, 0},
                     new float[] {.59f, .59f, .59f, 0, 0},
                     new float[] {.11f, .11f, .11f, 0, 0},
                     new float[] {0, 0, 0, 1, 0},
                     new float[] {0, 0, 0, 0, 1}
                }
                  );

            return ApplyColorMatrix(sourceImage, colorMatrix);
        }

        /// <summary>
        /// Convert given image to negative filter
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>Converted System.Drawing.Bitmap</returns>
        private static Bitmap DrawAsNegative(this Image sourceImage)
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
        /// Convert given image to the selected matrix of colors.
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <param name="colorMatrix">The System.Drawing.Imaging.ColorMatrix to apply</param>
        /// <returns>Converted System.Drawing.Bitmap</returns>
        private static Bitmap DrawAsSelectedColor(this Image sourceImage, ColorMatrix colorMatrix)
        {
            return ApplyColorMatrix(sourceImage, colorMatrix);
        }

        /// <summary>
        /// Convert given image to transparency filter 
        /// and reduce the Alpha component by 50%
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>Converted System.Drawing.Bitmap</returns>
        private static Bitmap DrawWithTransparency(this Image sourceImage)
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

        /// <summary>
        /// Convert given image to SepiaTone filter         
        /// </summary>
        /// <param name="sourceImage">System.Drawing.Image to convert</param>
        /// <returns>Converted System.Drawing.Bitmap </returns>
        private static Bitmap DrawAsSepiaTone(this Image sourceImage)
        {
            ColorMatrix colorMatrix = new ColorMatrix(new float[][] 
                {
                        new float[]{.393f, .349f, .272f, 0, 0},
                        new float[]{.769f, .686f, .534f, 0, 0},
                        new float[]{.189f, .168f, .131f, 0, 0},
                        new float[]{0, 0, 0, 1, 0},
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

        //Still not in use:
        //I'm looking forward to learn how to use Marshal.Copy() int the Blur and Pixelate functions.

        //Look this
        public static Image ThresholdMA(float thresh, Bitmap image)
        {
            Bitmap b = new Bitmap(image);

            BitmapData bData = b.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, b.PixelFormat);

            /* GetBitsPerPixel just does a switch on the PixelFormat and returns the number */
            byte bitsPerPixel = GetBitsPerPixel(bData.PixelFormat);

            /*the size of the image in bytes */
            int size = bData.Stride * bData.Height;

            /*Allocate buffer for image*/
            byte[] data = new byte[size];

            /*This overload copies data of /size/ into /data/ from location specified (/Scan0/)*/
            System.Runtime.InteropServices.Marshal.Copy(bData.Scan0, data, 0, size);

            for (int i = 0; i < size; i += bitsPerPixel / 8)
            {
                double magnitude = 1 / 3d * (data[i] + data[i + 1] + data[i + 2]);

                //data[i] is the first of 3 bytes of color
                if (magnitude < thresh)
                {
                    data[i] = 0;
                    data[i + 1] = 0;
                    data[i + 2] = 0;
                }
                else
                {
                    data[i] = 255;
                    data[i + 1] = 255;
                    data[i + 2] = 255;
                }
            }

            /* This override copies the data back into the location specified */
            System.Runtime.InteropServices.Marshal.Copy(data, 0, bData.Scan0, data.Length);

            b.UnlockBits(bData);

            return b;
        }
        //and this
        private static byte GetBitsPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return 24;
                    break;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                case PixelFormat.Format32bppRgb:
                    return 32;
                    break;
                default:
                    throw new ArgumentException("Only 24 and 32 bit images are supported");


            }
        }
    }
}
