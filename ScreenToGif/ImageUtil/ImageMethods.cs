using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScreenToGif.FileWriters.GifWriter;
using ScreenToGif.Util;
using Encoder = ScreenToGif.Windows.Encoder;

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
        /// <param name="cutFolder">The folder that will hold the frames.</param>
        /// <returns>A List contaning all frames and its cut points</returns>
        public static List<FrameInfo> PaintTransparentAndCut(List<FrameInfo> listToEncode, Color transparent, int id, string cutFolder)
        {
            //End to start FOR
            for (int index = listToEncode.Count - 1; index > 0; index--)
            {
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

                string newFileName = Path.Combine(cutFolder, Path.GetFileName(listToEncode[index].ImageLocation ?? index + ".bmp"));
                int count = 0;

                //If file already exists.
                while (File.Exists(newFileName))
                {
                    newFileName = Path.Combine(cutFolder, Path.GetFileName(listToEncode[index].ImageLocation ?? String.Concat(index, " ", count, ".bmp")));
                    count++;
                }

                imageSave2.Save(newFileName);

                //Add to listToEncode.
                listToEncode[index].ImageLocation = newFileName;
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
        /// <param name="cutFolder">The folder that will hold the frames.</param>
        public static List<FrameInfo> CutUnchanged(List<FrameInfo> listToEncode, int id, string cutFolder)
        {
            //End to start FOR
            for (int index = listToEncode.Count - 1; index > 0; index--)
            {
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

                string newFileName = Path.Combine(cutFolder, Path.GetFileName(listToEncode[index].ImageLocation ?? index + ".bmp"));
                int count = 0;

                //If file already exists.
                while (File.Exists(newFileName))
                {
                    newFileName = Path.Combine(cutFolder, Path.GetFileName(listToEncode[index].ImageLocation ?? String.Concat(index, " ", count, ".bmp")));
                    count++;
                }

                imageSave2.Save(newFileName);

                //Add to listToEncode.
                listToEncode[index].ImageLocation = newFileName;
                listToEncode[index].PositionTopLeft = new Point(firstX, firstY);

                #endregion

                GC.Collect(1);

                #endregion
            }

            return listToEncode;
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

        #endregion
    }
}
