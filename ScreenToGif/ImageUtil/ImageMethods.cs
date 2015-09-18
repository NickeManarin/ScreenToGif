using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenToGif.FileWriters.GifWriter;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;
using Color = System.Drawing.Color;
using Encoder = ScreenToGif.Windows.Encoder;
using Image = System.Drawing.Image;
using PixelFormat = System.Windows.Media.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ScreenToGif.ImageUtil
{
    /// <summary>
    /// Image algorithms.
    /// </summary>
    public static class ImageMethods
    {
        #region Paint Transparent

        /// <summary>
        /// Analizes all frames (from the end to the start) and paints all unchanged pixels with a given color, 
        /// after, it cuts the image to reduce filesize.
        /// </summary>
        /// <param name="listToEncode">The list of frames to analize.</param>
        /// <param name="transparent">The color to paint the unchanged pixels.</param>
        /// <param name="id">The Id of the current Task.</param>
        /// <param name="tokenSource">The cancelation token source.</param>
        /// <returns>A List contaning all frames and its cut points</returns>
        public static List<FrameInfo> PaintTransparentAndCut(List<FrameInfo> listToEncode, Color transparent, int id, CancellationTokenSource tokenSource)
        {
            //End to start FOR
            for (int index = listToEncode.Count - 1; index > 0; index--)
            {
                #region Cancellation

                if (tokenSource.Token.IsCancellationRequested)
                {
                    Encoder.SetStatus(Status.Cancelled, id);

                    break;
                }

                #endregion

                #region For each Frame, from the end to the start

                Encoder.Update(id, index - 1);

                //First frame is ignored.
                if (index <= 0) continue;

                #region Get Image Info

                var imageAux1 = listToEncode[index - 1].ImageLocation.From();
                var imageAux2 = listToEncode[index].ImageLocation.From();

                var startY = new bool[imageAux1.Height];
                var startX = new bool[imageAux1.Width];

                var image1 = new PixelUtil(imageAux1); //Previous image
                var image2 = new PixelUtil(imageAux2); //Actual image

                image1.LockBits();
                image2.LockBits();

                int height = imageAux1.Height;
                int width = imageAux1.Width;

                #endregion

                //Only use Parallel if the image is big enough.
                if ((width * height) > 150000)
                {
                    #region Parallel Loop

                    //x - width - sides
                    Parallel.For(0, width, x =>
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
                    }); //SPEEEEEED, alot!

                    #endregion
                }
                else
                {
                    #region Sequential Loop

                    //x - width - sides
                    for (int x = 0; x < width; x++)
                    {
                        //y - height - up/down
                        for (int y = 0; y < height; y++)
                        {
                            #region For each Pixel

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

                            #endregion
                        }
                    }

                    #endregion
                }

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

                #region Get the Width and Height

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

                #region Update Image Info and Save

                imageSave2.Save(listToEncode[index].ImageLocation);

                listToEncode[index].PositionTopLeft = new Point(firstX, firstY);

                #endregion

                GC.Collect(1);

                #endregion
            }

            return listToEncode;
        }

        /// <summary>
        /// Analizes all frames (from the end to the start) and paints all unchanged pixels with a given color, 
        /// after, it cuts the image to reduce filesize.
        /// </summary>
        /// <param name="listToEncode">The list of frames to analize.</param>
        /// <param name="id">The Id of the Task.</param>
        /// <param name="tokenSource">The cancelation token source.</param>
        public static List<FrameInfo> CutUnchanged(List<FrameInfo> listToEncode, int id, CancellationTokenSource tokenSource)
        {
            //End to start FOR
            for (int index = listToEncode.Count - 1; index > 0; index--)
            {
                #region Cancellation

                if (tokenSource.Token.IsCancellationRequested)
                {
                    Encoder.SetStatus(Status.Cancelled, id);

                    break;
                }

                #endregion

                #region For each Frame, from the end to the start

                Encoder.Update(id, index - 1);

                //First frame is ignored.
                if (index <= 0) continue;

                #region Get Image Info

                var imageAux1 = listToEncode[index - 1].ImageLocation.From();
                var imageAux2 = listToEncode[index].ImageLocation.From();

                var startY = new bool[imageAux1.Height];
                var startX = new bool[imageAux1.Width];

                var image1 = new PixelUtil(imageAux1); //Previous image
                var image2 = new PixelUtil(imageAux2); //Actual image

                image1.LockBits();
                image2.LockBits();

                int height = imageAux1.Height;
                int width = imageAux1.Width;

                #endregion

                //Only use Parallel if the image is big enough.
                if ((width * height) > 150000)
                {
                    #region Parallel Loop

                    //x - width - sides
                    Parallel.For(0, width, x =>
                    {
                        //y - height - up/down
                        for (int y = 0; y < height; y++)
                        {
                            if (image1.GetPixel(x, y) != image2.GetPixel(x, y))
                            {
                                #region Get the Changed Pixels

                                startX[x] = true;
                                startY[y] = true;

                                #endregion
                            }
                        }
                    }); //SPEEEEEED, alot!

                    #endregion
                }
                else
                {
                    #region Sequential Loop

                    //x - width - sides
                    for (int x = 0; x < width; x++)
                    {
                        //y - height - up/down
                        for (int y = 0; y < height; y++)
                        {
                            #region For each Pixel

                            if (image1.GetPixel(x, y) != image2.GetPixel(x, y))
                            {
                                #region Get the Changed Pixels

                                startX[x] = true;
                                startY[y] = true;

                                #endregion
                            }

                            #endregion
                        }
                    }

                    #endregion
                }

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

                #region Get the Width and Height

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

                #region Update Image Info and Save

                imageSave2.Save(listToEncode[index].ImageLocation);

                //Add to listToEncode.
                listToEncode[index].PositionTopLeft = new Point(firstX, firstY);

                #endregion

                GC.Collect(1);

                #endregion
            }

            return listToEncode;
        }

        #endregion

        #region Import From Gif

        /// <summary>
        /// Return frame(s) as list of binary from jpeg, png, bmp or gif image file
        /// </summary>
        /// <param name="fileName">image file name</param>
        /// <returns>System.Collections.Generic.List of byte</returns>
        public static List<Bitmap> GetFrames(string fileName)
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
                Guid imageGuid = gifImg.RawFormat.Guid;

                ImageFormat imageFormat = (from pair in guidToImageFormatMap where imageGuid == pair.Key select pair.Value).FirstOrDefault();

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

                //Get list of frame(s) from image file.
                var myBitmaps = new List<Bitmap>();

                foreach (byte[] item in tmpFrames)
                {
                    Bitmap tmpBitmap = ConvertBytesToImage(item);

                    if (tmpBitmap != null)
                    {
                        myBitmaps.Add(tmpBitmap);
                    }
                }

                return myBitmaps;
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

        #endregion

        #region Create and Save Images

        /// <summary>
        /// Creates a solid color BitmapSource.
        /// </summary>
        /// <param name="color">The Background color.</param>
        /// <param name="width">The Width of the image.</param>
        /// <param name="height">The Height of the image.</param>
        /// <param name="pixelFormat">The PixelFormat.</param>
        /// <returns>A BitmapSource of the given parameters.</returns>
        public static BitmapSource CreateEmtpyBitmapSource(System.Windows.Media.Color color, int width, int height, PixelFormat pixelFormat)
        {
            int rawStride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            var rawImage = new byte[rawStride * height];

            var colors = new List<System.Windows.Media.Color> { color };
            var myPalette = new BitmapPalette(colors);

            return BitmapSource.Create(width, height, 96, 96, pixelFormat, myPalette, rawImage, rawStride);
        }

        /// <summary>
        /// Converts a BitmapSource to a BitmapImage.
        /// </summary>
        /// <typeparam name="T">A BitmapEncoder derived class.</typeparam>
        /// <param name="bitmapSource">The source to convert.</param>
        /// <returns>A converted BitmapImage.</returns>
        private static BitmapImage GetBitmapImage<T>(BitmapSource bitmapSource) where T : BitmapEncoder, new()
        {
            var frame = BitmapFrame.Create(bitmapSource);
            var encoder = new T();
            encoder.Frames.Add(frame);

            var bitmapImage = new BitmapImage();
            bool isCreated;

            try
            {
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    isCreated = true;
                }
            }
            catch
            {
                isCreated = false;
            }

            return isCreated ? bitmapImage : null;
        }

        #endregion

        #region Edit Images

        /// <summary>
        /// Resizes the given image.
        /// </summary>
        /// <param name="source">The image source.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="margin">Cut margin.</param>
        /// <param name="dpi">The DPI of the image.</param>
        /// <returns>A resized ImageSource</returns>
        public static BitmapFrame ResizeImage(ImageSource source, int width, int height, int margin = 0, int dpi = 96)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                dpi, dpi,              // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        /// <summary>
        /// Crops a given image.
        /// </summary>
        /// <param name="source">The BitmapSource.</param>
        /// <param name="rect">The crop rectangle.</param>
        /// <returns>The Cropped image.</returns>
        public static BitmapFrame CropImage(BitmapSource source, Int32Rect rect)
        {
            var croppedImage = new CroppedBitmap(source, rect);

            return BitmapFrame.Create(croppedImage);
        }

        #endregion

        #region Others

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

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <param name="size">The maximum height of the image.</param>
        /// <returns>The open BitmapSource.</returns>
        public static BitmapSource SourceFrom(this string fileSource, Int32? size = null)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                if (size.HasValue)
                    bitmapImage.DecodePixelHeight = size.Value;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // just in case you want to load the image in another thread
                return bitmapImage;
            }
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <returns>The open BitmapSource.</returns>
        public static Size SizeOf(this string fileSource)
        {
            var bitmapAux = new Bitmap(fileSource);
            var size = new Size(bitmapAux.Width, bitmapAux.Height);
            bitmapAux.Dispose();

            return size;
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="dpi">The DPI of the source.</param>
        /// <returns>An ImageSource</returns>
        public static BitmapSource GetRender(this UIElement source, double dpi)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(source);

            var scale = dpi / 96.0;
            var width = (bounds.Width + bounds.X) * scale;
            var height = (bounds.Height + bounds.Y) * scale;

            var rtb = new RenderTargetBitmap((int)Math.Round(width * scale, MidpointRounding.AwayFromZero),
                    (int)Math.Round(height * scale, MidpointRounding.AwayFromZero), dpi, dpi, PixelFormats.Pbgra32);

            //var rtb = new RenderTargetBitmap((int)Math.Round(source.DesiredSize.Width, MidpointRounding.AwayFromZero),
            //        (int)Math.Round(source.DesiredSize.Height, MidpointRounding.AwayFromZero), dpi, dpi, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext ctx = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(source);

                var location = new System.Windows.Point(bounds.X >= 0 ? bounds.X : 0, bounds.Y >= 0 ? bounds.Y : 0);
                var size = new System.Windows.Point(width <= source.RenderSize.Width ? width : source.RenderSize.Width, 
                    height <= source.RenderSize.Height ? height : source.RenderSize.Height);

                ctx.DrawRectangle(vb, null, new Rect(location, size));
            }

            //290w 196h
            rtb.Render(dv);
            return (BitmapSource)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets the DPI of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The DPI.</returns>
        public static double DpiOf(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.None;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return bitmapImage.DpiX;
            }
        }

        #endregion
    }
}
