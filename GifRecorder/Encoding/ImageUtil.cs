using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScreenToGif.Capture;
using ScreenToGif.Pages;

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
                var result = new Bitmap(nWidth, nHeight);
                using (Graphics g = Graphics.FromImage(result))
                    g.DrawImage(listFrame, 0, 0, nWidth, nHeight);
                listResize.Add(result);
            }
            listFrames.Clear();
            GC.Collect();
            return listResize;
        }

        /// <summary>
        /// Resizes a List of Bitmaps from the disk.
        /// </summary>
        /// <param name="listFrames">The List of Bitmap to be resized.</param>
        /// <param name="nWidth">The desired Width</param>
        /// <param name="nHeight">The desired Height</param>
        /// <returns>The resized List of Bitmap</returns>
        public static void ResizeBitmap(List<string> listFrames, int nWidth, int nHeight)
        {
            foreach (string frame in listFrames)
            {
                var bitmapAux = new Bitmap(frame);
                var result = new Bitmap(nWidth, nHeight);

                using (Graphics g = Graphics.FromImage(result))
                    g.DrawImage(bitmapAux, 0, 0, nWidth, nHeight);

                bitmapAux.Dispose();
                result.Save(frame);
                result.Dispose();
            }

            GC.Collect();
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
        public static void Crop(IEnumerable<string> list, Rectangle cropArea)
        {
            foreach (var path in list)
            {
                var bmpImage = path.From();
                bmpImage = bmpImage.Clone(cropArea, bmpImage.PixelFormat);

                bmpImage.Save(path);
                bmpImage.Dispose();
            }

            GC.Collect();
        }

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

        #region Yo-yo

        /// <summary>
        /// Reverts the list of frames.
        /// </summary>
        /// <param name="list">The list to be reverted.</param>
        /// <returns>The list reverted.</returns>
        public static List<Bitmap> Revert(List<Bitmap> list)
        {
            var finalList = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                finalList.Insert(0, new Bitmap(bitmap));
            }

            return finalList;
        }

        /// <summary>
        /// Internal Revert algorith, used to revert the position of the list.
        /// </summary>
        /// <typeparam name="T">The type of the lyt</typeparam>
        /// <param name="list">The list to be reverted.</param>
        /// <returns>The reverted list.</returns>
        private static List<T> Revert<T>(IEnumerable<T> list)
        {
            var finalList = new List<T>();
            foreach (T content in list)
            {
                finalList.Insert(0, content);
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

        /// <summary>
        /// Makes a Yo-yo efect with the given List (List + Reverted List)
        /// </summary>
        /// <param name="list">The list to apply the efect</param>
        /// <returns>A List with the Yo-yo efect</returns>
        public static List<string> Yoyo(List<string> list)
        {
            var listReverted = Revert(list);

            foreach (string fileName in listReverted)
            {
                File.Copy(fileName, fileName.Replace(".bmp", "R.bmp"));
                list.Add(fileName.Replace(".bmp", "R.bmp"));
            }

            return list;
        }

        /// <summary>
        /// Makes a Yo-yo efect with the given List (List + Reverted List)
        /// </summary>
        /// <param name="list">The list to apply the efect</param>
        /// <returns>A List with the Yo-yo efect</returns>
        public static List<T> Yoyo<T>(List<T> list)
        {
            list.AddRange(Revert(list));
            //should we remove the first frame of de reverted part, so it won't repeat?
            return list;
        }

        #endregion

        #region Border

        /// <summary>
        /// Adds a border in given Bitmap.
        /// </summary>
        /// <param name="image">The image to add a border.</param>
        /// <param name="thick">The size in pixels of the border.</param>
        /// <param name="color">The Color of the border.</param>
        /// <returns>The Bitmap with a black border.</returns>
        public static Bitmap Border(Bitmap image, float thick, Color color)
        {
            var borderImage = new Bitmap(image);
            Graphics g = Graphics.FromImage(borderImage);

            var borderPen = new Pen(new SolidBrush(color), thick);
            g.DrawRectangle(borderPen, thick / 2, thick / 2, borderImage.Width - thick, borderImage.Height - thick);

            return borderImage;
        }

        #endregion

        #region Pixelate

        /// <summary>
        /// Applies the pixelate effect in given frame.
        /// </summary>
        /// <param name="image">The image to pixelate.</param>
        /// <param name="rectangle">The area to pixelate.</param>
        /// <param name="pixelateSize">The size of the pixel.</param>
        /// <returns>A pixelated Bitmap.</returns>
        public static Bitmap Pixelate(Bitmap image, Rectangle rectangle, Int32 pixelateSize)
        {
            var pixelated = new Bitmap(image);

            var pixelUtil = new PixelUtil(pixelated);
            pixelUtil.LockBits();

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
                    Color pixel = pixelUtil.GetPixel(xx + offsetX, yy + offsetY);

                    // for each pixel in the pixelate size, set it to the center color
                    for (Int32 x = xx; x < xx + pixelateSize && x < image.Width; x++)
                        for (Int32 y = yy; y < yy + pixelateSize && y < image.Height; y++)
                            pixelUtil.SetPixel(x, y, pixel);
                }
            }

            pixelUtil.UnlockBits();

            return pixelated;
        }

        /// <summary>
        /// Applies the pixelate effect in each frame of the given list.
        /// </summary>
        /// <param name="list">The List of Bitmaps to pixelate.</param>
        /// <param name="rectangle">The area to pixelate.</param>
        /// <param name="pixelateSize">The size of the pixel.</param>
        /// <returns>A List with pixelated Bitmaps.</returns>
        public static List<Bitmap> Pixelate(List<Bitmap> list, Rectangle rectangle, Int32 pixelateSize)
        {
            var edit = new List<Bitmap>();

            foreach (Bitmap bitmap in list)
            {
                edit.Add(Pixelate(bitmap, rectangle, pixelateSize));
            }

            return edit;
        }

        #endregion

        #region Blur

        /// <summary>
        /// Apply smooth efect on image
        /// </summary>
        /// <param name="image">System.Drawing.Bitmap that will receive blur efect</param>
        /// <param name="rectangle">System.Drawing.Rectangle, the area to apply the efect</param>
        /// <param name="blurSize">System.Int32, the intensity of the blur</param>
        /// <returns>System.Drawing.Bitmap with apllied colors</returns>
        public static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            Bitmap blurred = new Bitmap(image);

            var pixelUtil = new PixelUtil(blurred);
            pixelUtil.LockBits();

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
                            Color pixel = pixelUtil.GetPixel(x, y);

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
                            pixelUtil.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                }
            }

            pixelUtil.UnlockBits();

            return blurred;
        }

        /// <summary>
        /// Apply the blure effect in a given list of frames.
        /// </summary>
        /// <param name="list">The list to apply the effect.</param>
        /// <param name="rectangle">The area to apply the blur</param>
        /// <param name="blurSize">The intensity of the blur.</param>
        /// <returns>A List of Bitmaps blured.</returns>
        public static List<Bitmap> Blur(List<Bitmap> list, Rectangle rectangle, Int32 blurSize) //For all
        {
            var edit = new List<Bitmap>();
            foreach (Bitmap bitmap in list)
            {
                edit.Add(Blur(bitmap, rectangle, blurSize));
            }

            return edit;
        }

        #endregion

        #region Grayscale

        /// <summary>
        /// Convert given image to grayscale bitmap.
        /// </summary>
        /// <param name="originalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap Grayscale(Image originalImage)
        {
            return originalImage.DrawAsGrayscale();
        }

        /// <summary>
        /// Convert each bitmap in the given list to grayscale filter
        /// </summary>
        /// <param name="list">System.Collections.Generic.List of System.Drawing.Bitmap to convert</param>
        /// <returns>Converted System.Collections.Generic.List of System.Drawing.Bitmap </returns>
        public static List<Bitmap> Grayscale(List<Bitmap> list)
        {
            var edit = new List<Bitmap>();

            foreach (Bitmap bitmap in list)
            {
                edit.Add(Grayscale(bitmap));
            }

            return edit;
        }

        #endregion

        #region Color

        /// <summary>
        /// Colorize the given image.
        /// </summary>
        /// <param name="originalImage">System.Drawing.Image to colorize</param>
        /// <param name="values">The Color to be applied</param>
        /// <returns>System.Drawing.Bitmap colorized</returns>
        public static Bitmap Colorize(Image originalImage, float[] values)
        {
            var matrix = new ColorMatrix(new[]
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new[] {values[0], values[1], values[2], values[3], 1} //RGBA and W
            });

            return originalImage.DrawAsSelectedColor(matrix);
        }

        /// <summary>
        /// Colorize the given image.
        /// </summary>
        /// <param name="originalImage">The image to colorize.</param>
        /// <param name="color">The Color to be applied</param>
        /// <returns>A bitmap with the color filter.</returns>
        public static Bitmap Colorize(Bitmap originalImage, Color color)
        {
            using (var graph = Graphics.FromImage(originalImage))
            {
                graph.FillRectangle(new SolidBrush(color), new Rectangle(0, 0, originalImage.Width, originalImage.Height));
                graph.Flush();
            }

            GC.Collect();

            return originalImage;
        }

        #endregion

        #region Negative

        /// <summary>
        /// Convert given image to negative bitmap
        /// </summary>
        /// <param name="originalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap Negative(Image originalImage)
        {
            return originalImage.DrawAsNegative();
        }

        /// <summary>
        /// Convert each bitmap in the given list to negative filter
        /// </summary>
        /// <param name="list">System.Collections.Generic.List of System.Drawing.Bitmap to convert</param>
        /// <returns>Converted System.Collections.Generic.List of System.Drawing.Bitmap </returns>
        public static List<Bitmap> Negative(List<Bitmap> list)
        {
            var edit = new List<Bitmap>();
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
        /// <param name="originalImage">System.Drawing.Image to convert</param>
        /// <param name="transparency">The ammount of transparency</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap Transparency(Image originalImage, float transparency = 0.5f)
        {
            return originalImage.DrawWithTransparency(transparency);
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
            var edit = new List<Bitmap>();

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
        /// <param name="originalImage">System.Drawing.Image to convert</param>
        /// <returns>System.Drawing.Bitmap converted</returns>
        public static Bitmap SepiaTone(Image originalImage)
        {
            return originalImage.DrawAsSepiaTone();
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
            var edit = new List<Bitmap>();

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
            var colorMatrix = new ColorMatrix(new[]
            {
                     new[] {.3f, .3f, .3f, 0, 0},
                     new[] {.59f, .59f, .59f, 0, 0},
                     new[] {.11f, .11f, .11f, 0, 0},
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
            var colorMatrix = new ColorMatrix(new[]
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
        /// <param name="transparency">The ammount of transparency</param>
        /// <returns>Converted System.Drawing.Bitmap</returns>
        private static Bitmap DrawWithTransparency(this Image sourceImage, float transparency)
        {
            var colorMatrix = new ColorMatrix(new[]
            {
                            new float[]{1, 0, 0, 0, 0},
                            new float[]{0, 1, 0, 0, 0},
                            new float[]{0, 0, 1, 0, 0},
                            new[]{0, 0, 0, transparency, 0},
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
            var colorMatrix = new ColorMatrix(new[]
            {
                        new[]{.393f, .349f, .272f, 0, 0},
                        new[]{.769f, .686f, .534f, 0, 0},
                        new[]{.189f, .168f, .131f, 0, 0},
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
            var bmp32BppDest = new Bitmap(bmp32BppSource.Width, bmp32BppSource.Height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bmp32BppDest))
            {
                var bmpAttributes = new ImageAttributes();
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
            var bmpNew = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bmpNew))
            {
                graphics.DrawImage(sourceImage, new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), GraphicsUnit.Pixel);
                graphics.Flush();
            }

            return bmpNew;
        }

        #endregion

        #region CopyImage

        /// <summary>Creates a copy of the source image with the specified pixel format.</summary><remarks>
        /// This can also be achieved with the <see cref="System.Drawing.Bitmap.Clone(int, int, PixelFormat)"/>
        /// overload, but I have had issues with that method.</remarks>
        public static Image CopyImage(this Image image, PixelFormat format = PixelFormat.Format24bppRgb)
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

        #region InsertImage

        private const string PngFormat = ".png";
        private const string JpegFormat = ".jpeg";
        private const string JpegFormat2 = ".jpg";
        private const string BmpFormat = ".bmp";
        private const string GifFormat = ".gif";

        /// <summary>
        /// Get frame(s) as list of bitmap(s) from jpeg, png, bmp or gif image file
        /// </summary>
        /// <param name="fileName">image file name</param>
        /// <param name="size">Used for resizing bitmap(s) </param>
        /// <param name="count">The amount of frames in the collection.</param>
        /// <exception cref="ArgumentException">[fileName] type
        /// isn't supported</exception>
        /// <exception cref="FileNotFoundException">[fileName] don't exist</exception>
        /// <returns>System.Collections.Generic.List of bitmap(s)</returns>
        public static List<Bitmap> GetBitmapsFromFile(string fileName, int count, Size size)
        {
            bool multipleImages = false;

            //Check the existance of the image.
            if (!File.Exists(fileName))
                throw new FileNotFoundException("Unable to locate " + fileName);

            string extension = Path.GetExtension(fileName).ToLower();

            switch (extension)
            {
                case GifFormat:
                    multipleImages = true;
                    break;
                case PngFormat:
                case JpegFormat:
                case JpegFormat2:
                case BmpFormat:
                    break;
                default:
                    throw new ArgumentException("The selected file name is not supported", "fileName");
            }

            //Get list of frame(s) from image file.
            var myBitmaps = new List<Bitmap>();

            if (!multipleImages)
            {
                //Normal image files: jpeg, png or bmp.
                using (Image img = fileName.From())
                {
                    //TODO: make a different kind of resize, without re-scalling
                    Bitmap bitmapResized = ImageUtil.ResizeBitmap
                        (
                            (Bitmap)img,
                            size.Width,
                            size.Height
                        );

                    myBitmaps.Add(bitmapResized);
                }
            }
            else
            {
                //Gif File
                IEnumerable<byte[]> binaryGif = GetFrames(fileName);

                foreach (byte[] item in binaryGif)
                {
                    Bitmap tmpBitmap = ConvertBytesToImage(item);

                    if (tmpBitmap != null)
                    {
                        if (count != 0)
                        {
                            myBitmaps.Add(ImageUtil.ResizeBitmap(tmpBitmap,
                                size.Width,
                                size.Height
                                ));
                        }
                        else
                        {
                            //If there is no bitmap in the list, the first will not be resized.
                            myBitmaps.Add(tmpBitmap);
                        }
                    }
                }
            }

            return myBitmaps;
        }

        /// <summary>
        /// Return frame(s) as list of binary from jpeg, png, bmp or gif image file
        /// </summary>
        /// <param name="fileName">image file name</param>
        /// <returns>System.Collections.Generic.List of byte</returns>
        private static IEnumerable<byte[]> GetFrames(string fileName)
        {
            var tmpFrames = new List<byte[]>();

            // Check the image format to determine what format
            // the image will be saved to the memory stream in
            var guidToImageFormatMap = new Dictionary<Guid, ImageFormat>()
            {
                {ImageFormat.Bmp.Guid,  ImageFormat.Bmp},
                {ImageFormat.Gif.Guid,  ImageFormat.Png},
                {ImageFormat.Icon.Guid, ImageFormat.Png},
                {ImageFormat.Jpeg.Guid, ImageFormat.Jpeg},
                {ImageFormat.Png.Guid,  ImageFormat.Png}
            };

            using (Image gifImg = Image.FromFile(fileName, true))
            {
                ImageFormat imageFormat = null;
                Guid imageGuid = gifImg.RawFormat.Guid;

                foreach (KeyValuePair<Guid, ImageFormat> pair in guidToImageFormatMap)
                {
                    if (imageGuid == pair.Key)
                    {
                        imageFormat = pair.Value;
                        break;
                    }
                }

                if (imageFormat == null)
                    throw new NoNullAllowedException("Unable to determine image format");

                //Get the frame count
                var dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
                int frameCount = gifImg.GetFrameCount(dimension);

                //Step through each frame
                for (int i = 0; i < frameCount; i++)
                {
                    //Set the active frame of the image and then
                    gifImg.SelectActiveFrame(dimension, i);

                    //write the bytes to the tmpFrames array
                    using (var ms = new MemoryStream())
                    {
                        gifImg.Save(ms, imageFormat);
                        tmpFrames.Add(ms.ToArray());
                    }
                }

                return tmpFrames;
            }
        }

        /// <summary>
        /// Convert bytes to Bitamp
        /// </summary>
        /// <param name="imageBytes">Image in a byte type</param>
        /// <returns>System.Drawing.Bitmap</returns>
        private static Bitmap ConvertBytesToImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            //Read bytes into a MemoryStream
            using (var ms = new MemoryStream(imageBytes))
            {
                //Recreate the frame from the MemoryStream
                using (var bmp = new Bitmap(ms))
                {
                    return (Bitmap)bmp.Clone();
                }
            }
        }

        #endregion InsertImage

        #region Paint Transparent

        /// <summary>
        /// Analizes all frames (from the end to the start) and paints all unchanged pixels with a given color, 
        /// after, it cuts the image to reduce filesize.
        /// </summary>
        /// <param name="listBit">The list of frames to analize. This is a parameter by reference.</param>
        /// <param name="transparent">The color to paint the unchanged pixels.</param>
        /// <returns></returns>
        public static List<FrameInfo> PaintTransparentAndCut(List<Bitmap> listBit, Color transparent)
        {
            var listToEncode = new List<FrameInfo>();

            //end to start FOR
            for (int index = listBit.Count - 1; index > 0; index--)
            {
                var startY = new bool[listBit[index - 1].Height];
                var startX = new bool[listBit[index - 1].Width];

                if (index > 0)
                {
                    var image1 = new PixelUtil(listBit[index - 1]); //previous image
                    var image2 = new PixelUtil(listBit[index]); //actual image

                    image1.LockBits();
                    image2.LockBits();

                    int height = listBit[index - 1].Height;
                    int width = listBit[index - 1].Width;

                    #region Loop

                    //x - width - sides
                    for (int x = 0; x < width; x++)
                    {
                        //y - height - up/down
                        for (int y = 0; y < height; y++)
                        {
                            if (image1.GetPixel(x, y) == image2.GetPixel(x, y))
                            {
                                image2.SetPixel(x, y, transparent);
                            }
                            else
                            {
                                #region Get the Changed Pixels

                                startX[x] = true;
                                startY[y] = true;

                                #endregion
                            }
                        }
                    }

                    #endregion

                    image1.UnlockBits();
                    image2.UnlockBits();

                    #region Verify positions

                    int firstX = startX.ToList().FindIndex(x => x);
                    int lastX = startX.ToList().FindLastIndex(x => x);

                    if (firstX == -1)
                    {
                        firstX = 0;
                    }
                    if (lastX == -1)
                    {
                        lastX = listBit[index - 1].Width;
                    }

                    int firstY = startY.ToList().FindIndex(x => x);
                    int lastY = startY.ToList().FindLastIndex(x => x);

                    if (lastY == -1)
                    {
                        lastY = listBit[index - 1].Height;
                    }
                    if (firstY == -1)
                    {
                        firstY = 0;
                    }

                    if (lastX < firstX)
                    {
                        int aux = lastX;
                        lastX = firstX;
                        firstX = aux;
                    }

                    if (lastY < firstY)
                    {
                        int aux = lastY;
                        lastY = firstY;
                        firstY = aux;
                    }

                    #endregion

                    #region Get the Widht and Height

                    int heigthCut = Math.Abs(lastY - firstY);
                    int widthCut = Math.Abs(lastX - firstX);

                    if (heigthCut != height)
                    {
                        heigthCut++;
                    }
                    else
                    {
                        //It means that no pixel got changed.
                        heigthCut = 1;
                        //So i cut to 1 pixel to save the most, 0 can't be.
                    }

                    if (widthCut != width)
                    {
                        widthCut++;
                    }
                    else
                    {
                        widthCut = 1;
                    }

                    #endregion

                    //Cut the images and get the new values.
                    listBit[index] = new Bitmap(listBit[index].Clone(new Rectangle(firstX, firstY, widthCut, heigthCut), listBit[index].PixelFormat));

                    //Add to listToEncode.
                    //listToEncode.Insert(0, new FrameInfo(listBit[index], new Point(firstX, firstY)));
                }
            }

            //Inserts the first not modified frame.
            //listToEncode.Insert(0, new FrameInfo(listBit[0], new Point(0, 0)));

            return listToEncode;
        }

        /// <summary>
        /// Analizes all frames (from the end to the start) and paints all unchanged pixels with a given color, 
        /// after, it cuts the image to reduce filesize.
        /// </summary>
        /// <param name="listBit">The list of frames to analize. This is a parameter by reference.</param>
        /// <param name="transparent">The color to paint the unchanged pixels.</param>
        /// <returns></returns>
        public static List<FrameInfo> PaintTransparentAndCut(List<string> listBit, Color transparent)
        {
            var listToEncode = new List<FrameInfo>();

            //End to start FOR
            for (int index = listBit.Count - 1; index > 0; index--)
            {
                Processing.Status(index - 1);

                var imageAux1 = listBit[index - 1].From();
                var imageAux2 = listBit[index].From();

                var startY = new bool[imageAux1.Height];
                var startX = new bool[imageAux1.Width];

                if (index > 0)
                {
                    var image1 = new PixelUtil(imageAux1); //previous image
                    var image2 = new PixelUtil(imageAux2); //actual image

                    image1.LockBits();
                    image2.LockBits();

                    int height = imageAux1.Height;
                    int width = imageAux1.Width;

                    #region Loop

                    //x - width - sides
                    for (int x = 0; x < width; x++)
                    {
                        //y - height - up/down
                        for (int y = 0; y < height; y++)
                        {
                            if (image1.GetPixel(x, y) == image2.GetPixel(x, y))
                            {
                                image2.SetPixel(x, y, transparent);
                            }
                            else
                            {
                                #region Get the Changed Pixels

                                startX[x] = true;
                                startY[y] = true;

                                #endregion
                            }
                        }
                    }

                    #endregion

                    image1.UnlockBits();
                    image2.UnlockBits();

                    #region Verify positions

                    int firstX = startX.ToList().FindIndex(x => x);
                    int lastX = startX.ToList().FindLastIndex(x => x);

                    if (firstX == -1)
                    {
                        firstX = 0;
                    }
                    if (lastX == -1)
                    {
                        lastX = imageAux1.Width;
                    }

                    int firstY = startY.ToList().FindIndex(x => x);
                    int lastY = startY.ToList().FindLastIndex(x => x);

                    if (lastY == -1)
                    {
                        lastY = imageAux1.Height;
                    }
                    if (firstY == -1)
                    {
                        firstY = 0;
                    }

                    if (lastX < firstX)
                    {
                        int aux = lastX;
                        lastX = firstX;
                        firstX = aux;
                    }

                    if (lastY < firstY)
                    {
                        int aux = lastY;
                        lastY = firstY;
                        firstY = aux;
                    }

                    #endregion

                    #region Get the Widht and Height

                    int heigthCut = Math.Abs(lastY - firstY);
                    int widthCut = Math.Abs(lastX - firstX);

                    if (heigthCut != height)
                    {
                        heigthCut++;
                    }
                    else
                    {
                        //It means that no pixel got changed.
                        heigthCut = 1;
                        //So i cut to 1 pixel to save the most, 0 can't be.
                    }

                    if (widthCut != width)
                    {
                        widthCut++;
                    }
                    else
                    {
                        widthCut = 1;
                    }

                    #endregion

                    //Cut the images and get the new values.
                    var imageSave2 = new Bitmap(imageAux2.Clone(new Rectangle(firstX, firstY, widthCut, heigthCut), imageAux2.PixelFormat));
                    
                    imageAux2.Dispose();
                    imageAux1.Dispose();

                    File.Delete(listBit[index]);

                    imageSave2.Save(listBit[index]);

                    //Add to listToEncode.
                    listToEncode.Insert(0, new FrameInfo(listBit[index], new Point(firstX, firstY)));

                    GC.Collect(1);
                }
            }

            //Inserts the first not modified frame.
            listToEncode.Insert(0, new FrameInfo(listBit[0], new Point(0, 0)));

            return listToEncode;
        }

        /// <summary>
        /// Analizes all frames (from the end to the start) and paints all unchanged pixels with a given color.
        /// </summary>
        /// <param name="listBit">The list of frames to analize. This is a parameter by reference.</param>
        /// <param name="transparent">The color to paint the unchanged pixels.</param>
        public static void PaintTransparent(List<Bitmap> listBit, Color transparent)
        {
            for (int index = listBit.Count - 1; index > 0; index--)
            {
                //end to start FOR
                if (index > 0)
                {
                    var image1 = new PixelUtil(listBit[index - 1]); //last
                    var image2 = new PixelUtil(listBit[index]); //actual

                    image1.LockBits();
                    image2.LockBits();

                    int height = listBit[index - 1].Height;
                    int width = listBit[index - 1].Width - 1;

                    //Parallelize each column.
                    Parallel.For(0, width, x =>
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (image1.GetPixel(x, y) == image2.GetPixel(x, y))
                            {
                                image2.SetPixel(x, y, transparent);
                            }
                        }
                    }); //SPEEEEEED, this has alot!

                    #region Old Sequential Code

                    //Benchmark.Start();

                    //for (int x = 0; x < listBit[index - 1].Width; x++)
                    //{
                    //    for (int y = 0; y < listBit[index - 1].Height; y++)
                    //    {
                    //        if (image1.GetPixel(x, y) == image2.GetPixel(x, y))
                    //        {
                    //            image2.SetPixel(x, y, transparent);
                    //        }
                    //    }
                    //}

                    //Benchmark.End();
                    //Console.WriteLine(Benchmark.Span);

                    #endregion

                    image1.UnlockBits();
                    image2.UnlockBits();
                }
            }//); //delete ); if for
        }

        #region Old Code

        #region Parallel each column

        //x - width - sides
        //Parallel.For(0, width, x =>
        //{
        //    //y - height - up/down
        //    for (int y = 0; y < height; y++)
        //    {
        //        if (image1.GetPixel(x, y) == image2.GetPixel(x, y))
        //        {
        //            image2.SetPixel(x, y, transparent);
        //        }
        //        else
        //        {
        //            #region Get the Changed Pixels

        //            //TopLeft:
        //            //Greater X, less image
        //            //Smaller Y, less image

        //            //BottomRight:
        //            //Smaller X, less image
        //            //Greater Y, less image

        //            startX[x] = true;
        //            startY[y] = true;

        //            #region Old code (Don't work with a Parallel)

        //            //if (x < firstX)
        //            //{
        //            //    firstX = x;
        //            //}
        //            //if (x > lastX)
        //            //{
        //            //    lastX = x;
        //            //}

        //            //if (y > firstY)
        //            //{
        //            //    firstY = y;
        //            //}
        //            //if (y < lastY)
        //            //{
        //            //    lastY = y;
        //            //}

        //            #endregion

        //            #endregion
        //        }
        //    }
        //}); //SPEEEEEED, alot!

        #endregion

        #endregion

        #endregion

        #region Others

        private static Image CreateGridBackground(Image img)
        {
            Image gridImage = new Bitmap(img.Width, img.Height);
            int cellSize = 10; //Convert.ToInt32(Resources.Grid_Cell_Size);
            int numOfCells = 150; //Convert.ToInt32(Resources.Grid_Cells_Number);
            var p = new Pen(Color.Gray);

            using (Graphics g = Graphics.FromImage(gridImage))
            {
                for (int y = 0; y < numOfCells; ++y)
                {
                    g.DrawLine(p, 0, y * cellSize, numOfCells * cellSize, y * cellSize);
                }

                for (int x = 0; x < numOfCells; ++x)
                {
                    g.DrawLine(p, x * cellSize, 0, x * cellSize, numOfCells * cellSize);
                }
            }

            return gridImage;
        }

        /// <summary>
        /// Merge 2 images togheter.
        /// </summary>
        ///<param name="imageBack"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Bitmap Merge(this Bitmap imageBack, Bitmap image)
        {
            using (var graph = Graphics.FromImage(imageBack))
            {
                graph.DrawImage(image, 0, 0);
                graph.Flush();
            }

            return new Bitmap(imageBack);
        }

        /// <summary>
        /// Gets the Bitmap from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <returns>The open Bitmap.</returns>
        public static Bitmap From(this string fileSource)
        {
            var bitmapAux = new Bitmap(fileSource);
            var bitmapReturn = new Bitmap(bitmapAux);
            bitmapAux.Dispose();

            return bitmapReturn;
        }

        #endregion
    }
}
