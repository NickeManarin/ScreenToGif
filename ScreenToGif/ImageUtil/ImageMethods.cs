using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
using ScreenToGif.ImageUtil.Gif.Decoder;
using ScreenToGif.Util;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using PixelFormat = System.Windows.Media.PixelFormat;
using Size = System.Drawing.Size;
using ScreenToGif.ImageUtil.Gif.LegacyEncoder;
using ScreenToGif.Util.Model;

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
            //First frame rect.
            var size = listToEncode[0].Path.ScaledSize();
            listToEncode[0].Rect = new Int32Rect(0, 0, (int)size.Width, (int)size.Height);

            //End to start FOR
            for (var index = listToEncode.Count - 1; index > 0; index--)
            {
                #region Cancellation

                if (tokenSource.Token.IsCancellationRequested)
                {
                    Windows.Other.Encoder.SetStatus(Status.Canceled, id);

                    break;
                }

                #endregion

                #region For each Frame, from the end to the start

                Windows.Other.Encoder.Update(id, index - 1);

                //First frame is ignored.
                if (index <= 0) continue;

                #region Get Image Info

                var imageAux1 = listToEncode[index - 1].Path.From();
                var imageAux2 = listToEncode[index].Path.From();

                var startY = new bool[imageAux1.Height];
                var startX = new bool[imageAux1.Width];

                var image1 = new PixelUtilOld(imageAux1); //Previous image
                var image2 = new PixelUtilOld(imageAux2); //Actual image

                image1.LockBits();
                image2.LockBits();

                var height = imageAux1.Height;
                var width = imageAux1.Width;

                #endregion

                //Only use Parallel if the image is big enough.
                if (width * height > 150000)
                {
                    #region Parallel Loop

                    //x - width - sides
                    Parallel.For(0, width, x =>
                    {
                        //y - height - up/down
                        for (var y = 0; y < height; y++)
                        {
                            var pixel2 = image2.GetPixel(x, y);

                            if (image1.GetPixel(x, y) == pixel2 || pixel2.A == 0)
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
                    for (var x = 0; x < width; x++)
                    {
                        //y - height - up/down
                        for (var y = 0; y < height; y++)
                        {
                            #region For each Pixel

                            var pixel2 = image2.GetPixel(x, y);

                            if (image1.GetPixel(x, y) == pixel2 || pixel2.A == 0)
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

                var firstX = startX.ToList().FindIndex(x => x);
                var lastX = startX.ToList().FindLastIndex(x => x);

                if (firstX == -1)
                    firstX = 0;
                if (lastX == -1)
                    lastX = imageAux1.Width;

                var firstY = startY.ToList().FindIndex(x => x);
                var lastY = startY.ToList().FindLastIndex(x => x);

                if (lastY == -1)
                    lastY = imageAux1.Height;
                if (firstY == -1)
                    firstY = 0;

                if (lastX < firstX)
                {
                    var aux = lastX;
                    lastX = firstX;
                    firstX = aux;
                }

                if (lastY < firstY)
                {
                    var aux = lastY;
                    lastY = firstY;
                    firstY = aux;
                }

                #endregion

                #region Get the Width and Height

                var heightCut = Math.Abs(lastY - firstY);
                var widthCut = Math.Abs(lastX - firstX);

                //If nothing changed, shift the delay.
                if (heightCut + widthCut == height + width)
                {
                    //TODO: Maximum of 2 bytes, 255 x 100: 25.500 ms
                    listToEncode[index - 1].Delay += listToEncode[index].Delay;
                    listToEncode[index].Rect = new Int32Rect(0, 0, 0, 0);

                    GC.Collect(1);
                    continue;
                }

                if (heightCut != height)
                    heightCut++;

                if (widthCut != width)
                    widthCut++;

                listToEncode[index].Rect = new Int32Rect(firstX, firstY, widthCut, heightCut);

                #endregion

                #region Update Image

                //Cut the images and get the new values.
                var imageSave2 = new Bitmap(imageAux2.Clone(new Rectangle(firstX, firstY, widthCut, heightCut), imageAux2.PixelFormat));

                imageAux2.Dispose();
                imageAux1.Dispose();

                imageSave2.Save(listToEncode[index].Path);

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
            //First frame rect.
            var size = listToEncode[0].Path.ScaledSize();
            listToEncode[0].Rect = new Int32Rect(0, 0, (int)size.Width, (int)size.Height);

            //End to start FOR
            for (var index = listToEncode.Count - 1; index > 0; index--)
            {
                #region Cancellation

                if (tokenSource.Token.IsCancellationRequested)
                {
                    Windows.Other.Encoder.SetStatus(Status.Canceled, id);

                    break;
                }

                #endregion

                #region For each Frame, from the end to the start

                Windows.Other.Encoder.Update(id, index - 1);

                //First frame is ignored.
                if (index <= 0) continue;

                #region Get Image Info

                var imageAux1 = listToEncode[index - 1].Path.From();
                var imageAux2 = listToEncode[index].Path.From();

                var startY = new bool[imageAux1.Height];
                var startX = new bool[imageAux1.Width];

                var image1 = new PixelUtilOld(imageAux1); //Previous image
                var image2 = new PixelUtilOld(imageAux2); //Actual image

                image1.LockBits();
                image2.LockBits();

                var height = imageAux1.Height;
                var width = imageAux1.Width;

                #endregion

                //Only use Parallel if the image is big enough.
                if (width * height > 150000)
                {
                    #region Parallel Loop

                    //x - width - sides
                    Parallel.For(0, width, x =>
                    {
                        //y - height - up/down
                        for (var y = 0; y < height; y++)
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
                    for (var x = 0; x < width; x++)
                    {
                        //y - height - up/down
                        for (var y = 0; y < height; y++)
                        {
                            #region For each Pixel

                            if (image1.GetPixel(x, y) == image2.GetPixel(x, y)) continue;

                            #region Get the Changed Pixels

                            startX[x] = true;
                            startY[y] = true;

                            #endregion

                            #endregion
                        }
                    }

                    #endregion
                }

                image1.UnlockBits();
                image2.UnlockBits();

                #region Verify positions

                var firstX = startX.ToList().FindIndex(x => x);
                var lastX = startX.ToList().FindLastIndex(x => x);

                if (firstX == -1)
                    firstX = 0;

                if (lastX == -1)
                    lastX = imageAux1.Width;

                var firstY = startY.ToList().FindIndex(x => x);
                var lastY = startY.ToList().FindLastIndex(x => x);

                if (lastY == -1)
                    lastY = imageAux1.Height;

                if (firstY == -1)
                    firstY = 0;

                if (lastX < firstX)
                {
                    var aux = lastX;
                    lastX = firstX;
                    firstX = aux;
                }

                if (lastY < firstY)
                {
                    var aux = lastY;
                    lastY = firstY;
                    firstY = aux;
                }

                #endregion

                #region Get the Width and Height

                var heightCut = Math.Abs(lastY - firstY);
                var widthCut = Math.Abs(lastX - firstX);

                //If nothing changed, shift the delay.
                if (heightCut + widthCut == height + width)
                {
                    listToEncode[index - 1].Delay += listToEncode[index].Delay;
                    listToEncode[index].Rect = new Int32Rect(0, 0, 0, 0);

                    GC.Collect(1);
                    continue;
                }

                if (heightCut != height)
                {
                    heightCut++;
                }

                if (widthCut != width)
                {
                    widthCut++;
                }

                listToEncode[index].Rect = new Int32Rect(firstX, firstY, widthCut, heightCut);

                #endregion

                #region Update Image Info and Save

                //Cut the images and get the new values.
                var imageSave2 = new Bitmap(imageAux2.Clone(new Rectangle(firstX, firstY, widthCut, heightCut), imageAux2.PixelFormat));

                imageAux2.Dispose();
                imageAux1.Dispose();

                imageSave2.Save(listToEncode[index].Path);

                #endregion

                GC.Collect(1);

                #endregion
            }

            return listToEncode;
        }

        #endregion

        #region Import From Gif

        public static BitmapDecoder GetDecoder(string fileName, out GifFile gifFile)
        {
            gifFile = null;
            BitmapDecoder decoder = null;

            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                stream.Position = 0;
                decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                if (decoder is GifBitmapDecoder)// && !CanReadNativeMetadata(decoder))
                {
                    stream.Position = 0;
                    gifFile = GifFile.ReadGifFile(stream, true);
                }

                //if (decoder == null)
                //    throw new InvalidOperationException("Can't get a decoder from the source.");
            }

            return decoder;
        }

        private static bool CanReadNativeMetadata(BitmapDecoder decoder)
        {
            try
            {
                var m = decoder.Metadata;
                return m != null;
            }
            catch
            {
                return false;
            }
        }

        public static System.Drawing.Size GetFullSize(BitmapDecoder decoder, GifFile gifMetadata)
        {
            if (gifMetadata != null)
            {
                var lsd = gifMetadata.Header.LogicalScreenDescriptor;
                return new System.Drawing.Size(lsd.Width, lsd.Height);
            }

            var width = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Width", 0);
            var height = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Height", 0);
            return new System.Drawing.Size(width, height);
        }

        private static T GetQueryOrDefault<T>(this BitmapMetadata metadata, string query, T defaultValue)
        {
            if (metadata.ContainsQuery(query))
                return (T)Convert.ChangeType(metadata.GetQuery(query), typeof(T));

            return defaultValue;
        }

        public static FrameMetadata GetFrameMetadata(BitmapDecoder decoder, GifFile gifMetadata, int frameIndex)
        {
            if (gifMetadata != null && gifMetadata.Frames.Count > frameIndex)
            {
                return GetFrameMetadata(gifMetadata.Frames[frameIndex]);
            }

            return GetFrameMetadata(decoder.Frames[frameIndex]);
        }

        private static FrameMetadata GetFrameMetadata(BitmapFrame frame)
        {
            var metadata = (BitmapMetadata)frame.Metadata;
            var delay = TimeSpan.FromMilliseconds(100);
            var metadataDelay = metadata.GetQueryOrDefault("/grctlext/Delay", 10);

            if (metadataDelay != 0)
                delay = TimeSpan.FromMilliseconds(metadataDelay * 10);

            var disposalMethod = (FrameDisposalMethod)metadata.GetQueryOrDefault("/grctlext/Disposal", 0);

            var frameMetadata = new FrameMetadata
            {
                Left = metadata.GetQueryOrDefault("/imgdesc/Left", 0),
                Top = metadata.GetQueryOrDefault("/imgdesc/Top", 0),
                Width = metadata.GetQueryOrDefault("/imgdesc/Width", frame.PixelWidth),
                Height = metadata.GetQueryOrDefault("/imgdesc/Height", frame.PixelHeight),
                Delay = delay,
                DisposalMethod = disposalMethod
            };

            return frameMetadata;
        }

        private static FrameMetadata GetFrameMetadata(GifFrame gifMetadata)
        {
            var d = gifMetadata.Descriptor;

            var frameMetadata = new FrameMetadata
            {
                Left = d.Left,
                Top = d.Top,
                Width = d.Width,
                Height = d.Height,
                Delay = TimeSpan.FromMilliseconds(100),
                DisposalMethod = FrameDisposalMethod.None
            };

            var gce = gifMetadata.Extensions.OfType<GifGraphicControlExtension>().FirstOrDefault();

            if (gce != null)
            {
                if (gce.Delay != 0)
                    frameMetadata.Delay = TimeSpan.FromMilliseconds(gce.Delay);

                frameMetadata.DisposalMethod = (FrameDisposalMethod)gce.DisposalMethod;
            }

            return frameMetadata;
        }

        public static BitmapSource MakeFrame(System.Drawing.Size fullSize, BitmapSource rawFrame, FrameMetadata metadata, BitmapSource baseFrame)
        {
            //I removed this, so I could save the same as 32bpp
            //if (baseFrame == null && IsFullFrame(metadata, fullSize))
            //{
            //    // No previous image to combine with, and same size as the full image
            //    // Just return the frame as is
            //    return rawFrame;
            //}

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                if (baseFrame != null)
                {
                    var fullRect = new Rect(0, 0, fullSize.Width, fullSize.Height);
                    context.DrawImage(baseFrame, fullRect);
                }

                var rect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                context.DrawImage(rawFrame, rect);
            }

            //TODO: Test, DPI was hardcoded to 96.
            var bitmap = new RenderTargetBitmap(fullSize.Width, fullSize.Height, rawFrame.DpiX, rawFrame.DpiY, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            if (bitmap.CanFreeze && !bitmap.IsFrozen)
                bitmap.Freeze();

            return bitmap;
        }

        public static bool IsFullFrame(FrameMetadata metadata, System.Drawing.Size fullSize)
        {
            return metadata.Left == 0
                   && metadata.Top == 0
                   && metadata.Width == fullSize.Width
                   && metadata.Height == fullSize.Height;
        }

        public static BitmapSource ClearArea(BitmapSource frame, FrameMetadata metadata)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var fullRect = new Rect(0, 0, frame.PixelWidth, frame.PixelHeight);
                var clearRect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                var clip = Geometry.Combine(new RectangleGeometry(fullRect), new RectangleGeometry(clearRect), GeometryCombineMode.Exclude, null);

                context.PushClip(clip);
                context.DrawImage(frame, fullRect);
            }

            var bitmap = new RenderTargetBitmap(frame.PixelWidth, frame.PixelHeight, frame.DpiX, frame.DpiY, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            if (bitmap.CanFreeze && !bitmap.IsFrozen)
                bitmap.Freeze();

            return bitmap;
        }

        /// <summary>
        /// Return frame(s) as list of binary from jpeg, png, bmp or gif image file
        /// </summary>
        /// <param name="fileName">image file name</param>
        /// <returns>System.Collections.Generic.List of byte</returns>
        [Obsolete]
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

            using (var gifImg = Image.FromFile(fileName, true))
            {
                var imageGuid = gifImg.RawFormat.Guid;

                var imageFormat = (from pair in guidToImageFormatMap where imageGuid == pair.Key select pair.Value).FirstOrDefault();

                if (imageFormat == null)
                    throw new NoNullAllowedException("Unable to determine image format");

                //Get the frame count
                var dimension = new FrameDimension(gifImg.FrameDimensionsList[0]);
                var frameCount = gifImg.GetFrameCount(dimension);

                //Step through each frame
                for (var i = 0; i < frameCount; i++)
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

                foreach (var item in tmpFrames)
                {
                    var tmpBitmap = ConvertBytesToImage(item);

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
        /// <param name="dpi">The dpi of the image.</param>
        /// <param name="pixelFormat">The PixelFormat.</param>
        /// <returns>A BitmapSource of the given parameters.</returns>
        public static BitmapSource CreateEmtpyBitmapSource(System.Windows.Media.Color color, int width, int height, double dpi, PixelFormat pixelFormat)
        {
            var rawStride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            var rawImage = new byte[rawStride * height];

            var colors = new List<System.Windows.Media.Color> { color };
            var myPalette = new BitmapPalette(colors);

            return BitmapSource.Create(width, height, dpi, dpi, pixelFormat, myPalette, rawImage, rawStride);
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

        public static BitmapSource FromArray(List<byte> data, int w, int h, int ch)
        {
            var format = PixelFormats.Default;

            if (ch == 1) format = PixelFormats.Gray8; //grey scale image 0-255
            if (ch == 3) format = PixelFormats.Bgr24; //RGB
            if (ch == 4) format = PixelFormats.Bgr32; //RGB + alpha

            for (int i = data.Count - 1; i < w * h * ch; i++)
                data.Add(0);

            var wbm = new WriteableBitmap(w, h, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, w, h), data.ToArray().ToArray(), ch * w, 0);

            return wbm;
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
        public static BitmapFrame ResizeImage(BitmapImage source, int width, int height, int margin = 0, double dpi = 96d)
        {
            var scale = dpi / 96d;

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, new Rect(0, 0, width, height));
            }

            var resizedImage = new RenderTargetBitmap(
                (int)Math.Round(width * scale),
                (int)Math.Round(height * scale),
                dpi, dpi,              // Default DPI values
                PixelFormats.Pbgra32); // Default pixel format
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
        public static BitmapSource SourceFrom(this string fileSource, int? size = null)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                if (size.HasValue)
                    bitmapImage.DecodePixelHeight = size.Value;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); //Just in case you want to load the image in another thread
                return bitmapImage;
            }
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="stream">The stream to open.</param>
        /// <param name="size">The maximum height of the image.</param>
        /// <returns>The open BitmapSource.</returns>
        public static BitmapSource SourceFrom(this Stream stream, int? size = null)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

            if (size.HasValue)
                bitmapImage.DecodePixelHeight = size.Value;

            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); //Just in case you want to load the image in another thread
            return bitmapImage;
        }

        /// <summary>
        /// Gets the BitmapSource from the source and closes the file usage.
        /// </summary>
        /// <param name="fileSource">The file to open.</param>
        /// <param name="rect">The desired crop area.</param>
        /// <returns>The open BitmapSource.</returns>
        public static BitmapSource CropFrom(this string fileSource, Int32Rect rect)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); //Just in case you want to load the image in another thread.

                var scale = Math.Round(bitmapImage.DpiX / 96d, 2);

                rect = new Int32Rect((int)Math.Round(rect.X * scale), (int)Math.Round(rect.Y * scale), (int)Math.Round(rect.Width * scale), (int)Math.Round(rect.Height * scale));

                if (!new Int32Rect(0, 0, bitmapImage.PixelWidth, bitmapImage.PixelHeight).Contains(rect))
                    return null;

                return new CroppedBitmap(bitmapImage, rect);
            }
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="dpi">The DPI of the source.</param>
        /// <returns>An ImageSource</returns>
        public static RenderTargetBitmap GetRender(this UIElement source, double dpi)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(source);

            //TODO: Fix bounds when values are not rounded.

            var scale = Math.Round(dpi / 96d, 2);
            var width = (bounds.Width + bounds.X) * scale;
            var height = (bounds.Height + bounds.Y) * scale;

            #region If no bounds

            if (bounds.IsEmpty)
            {
                var control = source as Control;

                if (control != null)
                {
                    width = control.ActualWidth * scale;
                    height = control.ActualHeight * scale;
                }

                bounds = new Rect(new System.Windows.Point(0d, 0d), new System.Windows.Point(width, height));
            }

            #endregion

            var rtb = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), dpi, dpi, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(source);

                var locationRect = new System.Windows.Point(bounds.X, bounds.Y);
                var sizeRect = new System.Windows.Size((int)Math.Round(bounds.Width), (int)Math.Round(bounds.Height));

                ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
            }

            rtb.Render(dv);
            return (RenderTargetBitmap)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="scale">The scale of the UI element.</param>
        /// <param name="dpi">The DPI of the source.</param>
        /// <param name="size">The size of the destination image.</param>
        /// <returns>An ImageSource</returns>
        public static RenderTargetBitmap GetScaledRender(this Grid source, double scale, double dpi, System.Windows.Size size)
        {
            var rtb = new RenderTargetBitmap((int)Math.Round(size.Width), (int)Math.Round(size.Height), dpi, dpi, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(source);

                //Gets the child bounds.
                var bounds = VisualTreeHelper.GetDescendantBounds(source);
                var locationRect = new System.Windows.Point(bounds.X * scale, bounds.Y * scale);
                var sizeRect = new System.Windows.Size(bounds.Width * scale, bounds.Height * scale);

                ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
            }

            rtb.Render(dv);
            return (RenderTargetBitmap)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets a render of the current UIElement
        /// </summary>
        /// <param name="source">UIElement to screenshot</param>
        /// <param name="scale">The scale of the screen.</param>
        /// <param name="dpi">The DPI of the output.</param>
        /// <param name="size">The size of the destination image.</param>
        /// <returns>An ImageSource</returns>
        public static RenderTargetBitmap GetScaledRender(this UIElement source, double scale, double dpi, System.Windows.Size size)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(source);

            //var width = (bounds.Width + bounds.X) * scale;
            //var height = (bounds.Height + bounds.Y) * scale;

            #region If no bounds

            if (bounds.IsEmpty)
            {
                var control = source as FrameworkElement;

                if (control != null)
                    bounds = new Rect(new System.Windows.Point(0d, 0d), new System.Windows.Point(control.ActualWidth * scale, control.ActualHeight * scale));
            }

            #endregion

            var rtb = new RenderTargetBitmap((int)Math.Round(size.Width), (int)Math.Round(size.Height), dpi, dpi, PixelFormats.Pbgra32);
            
            source.Clip = new RectangleGeometry(new Rect(0, 0, rtb.Width, rtb.Height));
            source.ClipToBounds = true;

            var dv = new DrawingVisual();

            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(source)
                {
                    AutoLayoutContent = false,
                    Stretch = Stretch.None
                };

                //I still need to fix this, when there's an element outside the bounds, it gets stretched.
                //var locationRect = new System.Windows.Point(0 * scale, 0 * scale);
                //var sizeRect = new System.Windows.Size(rtb.Width * scale, rtb.Height * scale);

                var locationRect = new System.Windows.Point(bounds.X * scale, bounds.Y * scale);
                var sizeRect = new System.Windows.Size(bounds.Width * scale, bounds.Height * scale);

                ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
            }

            rtb.Render(dv);
            return (RenderTargetBitmap)rtb.GetAsFrozen();
        }

        /// <summary>
        /// Gets the DPI of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The DPI.</returns>
        public static double DpiOf(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnDemand;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return bitmapImage.DpiX;
            }
        }

        /// <summary>
        /// Gets the scale (dpi/96) of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The DPI.</returns>
        public static double ScaleOf(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnDemand;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return Math.Round(bitmapImage.DpiX / 96d, 2);
            }
        }

        /// <summary>
        /// Gets the size * scale of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The size of the image.</returns>
        public static System.Windows.Size ScaledSize(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnDemand;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return new System.Windows.Size(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            }
        }

        /// <summary>
        /// Gets the size of given image.
        /// </summary>
        /// <param name="fileSource">The filename of the source.</param>
        /// <returns>The size of the image.</returns>
        public static System.Windows.Size NonScaledSize(this string fileSource)
        {
            using (var stream = new FileStream(fileSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.None;

                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return new System.Windows.Size(bitmapImage.Width, bitmapImage.Height);
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

        #endregion
    }
}
