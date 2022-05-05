using ScreenToGif.Domain.Enums;
using ScreenToGif.Domain.Interfaces;
using ScreenToGif.Domain.Models;
using ScreenToGif.Util;
using ScreenToGif.Util.Codification;
using ScreenToGif.Util.Codification.Gif.Decoder;
using ScreenToGif.Util.Codification.Gif.LegacyEncoder;
using ScreenToGif.Util.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Resources;
using Image = System.Drawing.Image;
using PixelFormat = System.Windows.Media.PixelFormat;
using Size = System.Drawing.Size;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.ImageUtil;

/// <summary>
/// Image algorithms.
/// </summary>
public static class ImageMethods
{
    #region Gif transparency

    /// <summary>
    /// Gets the project, scans the each image in the list, replacing the color with a color that will be treated as transparent by the encoder.
    /// </summary>
    /// <param name="project">The exported project.</param>
    /// <param name="source">The color that will be converted to the chroma key, which in turn will be treated as transparent. If null, takes all colors with transparency and convert to the chroma.</param>
    /// <param name="chroma">The color that will be treated as transparent.</param>
    /// <param name="taskId">The id of the encoding task.</param>
    /// <param name="tokenSource">The cancellation token source.</param>
    /// <returns>The export project, with the images already scanned and altered.</returns>
    public static ExportProject PaintAndCutForTransparency(ExportProject project, System.Windows.Media.Color? source, System.Windows.Media.Color chroma, int taskId, CancellationTokenSource tokenSource)
    {
        using (var oldStream = new FileStream(project.ChunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (var newStream = new FileStream(project.NewChunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                for (var index = 0; index < project.Frames.Count; index++)
                {
                    #region Cancellation

                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        EncodingManager.Update(taskId, EncodingStatus.Canceled);
                        break;
                    }

                    #endregion

                    #region For each frame

                    EncodingManager.Update(taskId, index);

                    //var watch = Stopwatch.StartNew();

                    #region Get image info

                    oldStream.Position = project.Frames[index].DataPosition;
                    var pixels = oldStream.ReadBytes((int)project.Frames[index].DataLength);

                    var startY = new bool[project.Frames[index].Rect.Height];
                    var startX = new bool[project.Frames[index].Rect.Width];

                    var height = project.Frames[index].Rect.Height;
                    var width = project.Frames[index].Rect.Width;
                    var blockCount = project.Frames[index].ImageDepth / 8;

                    #endregion

                    //Console.WriteLine("Info: " + watch.Elapsed);

                    //Only use Parallel if the image is big enough.
                    if (width * height > 150000)
                    {
                        #region Parallel loop

                        //x - width - sides
                        Parallel.For(0, pixels.Length / blockCount, i =>
                        {
                            i *= blockCount;

                            //Replace all transparent color to a transparent version of the chroma key.
                            //Replace all colors that match the source color with a transparent version of the chroma key.
                            if ((!source.HasValue && pixels[i + 3] == 0) || (source.HasValue && pixels[i] == source.Value.B && pixels[i + 1] == source.Value.G && pixels[i + 2] == source.Value.R))
                            {
                                pixels[i] = chroma.B;
                                pixels[i + 1] = chroma.G;
                                pixels[i + 2] = chroma.R;
                                pixels[i + 3] = 0;
                            }
                            else
                            {
                                var y = i / blockCount / width;
                                var x = i / blockCount - (y * width);

                                //var current = (y * image1.Width + x) * blockCount == i;

                                startX[x] = true;
                                startY[y] = true;
                            }
                        });

                        #endregion
                    }
                    else
                    {
                        #region Sequential loop

                        for (var i = 0; i < pixels.Length; i += blockCount)
                        {
                            //Replace all transparent color to a transparent version of the chroma key.
                            //Replace all colors that match the source color with a transparent version of the chroma key.

                            if ((!source.HasValue && pixels[i + 3] == 0) || (source.HasValue && pixels[i] == source.Value.B && pixels[i + 1] == source.Value.G && pixels[i + 2] == source.Value.R))
                            {
                                pixels[i] = chroma.B;
                                pixels[i + 1] = chroma.G;
                                pixels[i + 2] = chroma.R;
                                pixels[i + 3] = 0;
                            }
                            else
                            {
                                //Actual content, that should be ignored.
                                var y = i / blockCount / width;
                                var x = i / blockCount - (y * width);

                                //var current = (y * image1.Width + x) * blockCount == i;

                                startX[x] = true;
                                startY[y] = true;
                            }
                        }

                        #endregion
                    }

                    //Console.WriteLine("Change: " + watch.Elapsed);

                    //First frame gets ignored.
                    if (index == 0)
                    {
                        project.Frames[index].DataPosition = newStream.Position;
                        project.Frames[index].DataLength = pixels.LongLength;

                        newStream.WriteBytes(pixels);
                        continue;
                    }

                    #region Verify positions

                    var firstX = startX.ToList().FindIndex(x => x);
                    var lastX = startX.ToList().FindLastIndex(x => x);

                    if (firstX == -1)
                        firstX = 0;
                    if (lastX == -1)
                        lastX = width;

                    var firstY = startY.ToList().FindIndex(x => x);
                    var lastY = startY.ToList().FindLastIndex(x => x);

                    if (lastY == -1)
                        lastY = height;
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
                        project.Frames[index].Rect = new Int32Rect(0, 0, 0, 0);
                        project.Frames[index].DataPosition = newStream.Position;
                        project.Frames[index].DataLength = 0;

                        GC.Collect(1);
                        continue;
                    }

                    if (heightCut != height)
                        heightCut++;

                    if (widthCut != width)
                        widthCut++;

                    project.Frames[index].Rect = new Int32Rect(firstX, firstY, widthCut, heightCut);

                    #endregion

                    #region Crop and save

                    var newPixels = CropImageArray(pixels, width, 32, project.Frames[index].Rect);

                    project.Frames[index].DataPosition = newStream.Position;
                    project.Frames[index].DataLength = newPixels.LongLength;

                    newStream.WriteBytes(newPixels);

                    #endregion

                    //Console.WriteLine("Save: " + watch.Elapsed);
                    //Console.WriteLine();

                    GC.Collect(1);

                    #endregion
                }
            }
        }

        EncodingManager.Update(taskId, LocalizationHelper.Get("S.Encoder.SavingAnalysis"), true);

        //Detect any empty frame.
        for (var index = project.Frames.Count - 1; index >= 0; index--)
        {
            if (project.Frames[index].DataLength == 0)
                project.Frames[index - 1].Delay += project.Frames[index].Delay;
        }

        //Replaces the chunk file.
        File.Delete(project.ChunkPath);
        File.Move(project.NewChunkPath, project.ChunkPath);

        return project;
    }

    /// <summary>
    /// Analyzes all frames (from the end to the start) and paints all unchanged pixels with a given color,
    /// after, it cuts the image to reduce filesize.
    /// </summary>
    /// <param name="project">The project with frames to analyze.</param>
    /// <param name="chroma">The color to paint the unchanged pixels.</param>
    /// <param name="taskId">The Id of the current Task.</param>
    /// <param name="tokenSource">The cancellation token source.</param>
    /// <returns>The project containing all frames and its cut points.</returns>
    public static ExportProject PaintTransparentAndCut(ExportProject project, System.Windows.Media.Color chroma, int taskId, CancellationTokenSource tokenSource)
    {
        using (var oldStream = new FileStream(project.ChunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (var newFileStream = new FileStream(project.NewChunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var newStream = new BufferedStream(newFileStream, 100 * 1048576)) //Each 1 MB has 1_048_576 bytes.
                {
                    for (var index = project.Frames.Count - 1; index > 0; index--)
                    {
                        #region Cancellation

                        if (tokenSource.Token.IsCancellationRequested)
                        {
                            EncodingManager.Update(taskId, EncodingStatus.Canceled);
                            break;
                        }

                        #endregion

                        #region For each frame, from the end to the start

                        EncodingManager.Update(taskId, project.Frames.Count - index - 1);

                        //var watch = Stopwatch.StartNew();

                        #region Get image info

                        oldStream.Position = project.Frames[index - 1].DataPosition;
                        var image1 = oldStream.ReadBytes((int)project.Frames[index - 1].DataLength); //Previous image.
                        oldStream.Position = project.Frames[index].DataPosition;
                        var image2 = oldStream.ReadBytes((int)project.Frames[index].DataLength); //Current image.

                        var startY = new bool[project.Frames[index - 1].Rect.Height];
                        var startX = new bool[project.Frames[index - 1].Rect.Width];

                        var height = project.Frames[index - 1].Rect.Height;
                        var width = project.Frames[index - 1].Rect.Width;
                        var blockCount = project.Frames[index - 1].ImageDepth / 8;

                        #endregion

                        //Console.WriteLine("Info: " + watch.Elapsed);

                        //Only use Parallel if the image is big enough.
                        if (width * height > 150000)
                        {
                            #region Parallel Loop

                            //x - width - sides
                            Parallel.For(0, image1.Length / blockCount, i =>
                            {
                                i *= blockCount;

                                if (image1[i] != image2[i] || image1[i + 1] != image2[i + 1] || image1[i + 2] != image2[i + 2])
                                {
                                    //Different pixels should remain.
                                    var y = i / blockCount / width;
                                    var x = i / blockCount - (y * width);

                                    //image2[i + 3] = 255; When saving frames with transparency without the 'Enable transparency' ticked, the pixels that changed should be set to opaque.

                                    startX[x] = true;
                                    startY[y] = true;
                                }
                                else
                                {
                                    image2[i] = chroma.B;
                                    image2[i + 1] = chroma.G;
                                    image2[i + 2] = chroma.R;
                                    image2[i + 3] = 0;
                                }
                            });

                            #endregion
                        }
                        else
                        {
                            #region Sequential loop

                            for (var i = 0; i < image1.Length; i += blockCount)
                            {
                                if (image1[i] != image2[i] || image1[i + 1] != image2[i + 1] || image1[i + 2] != image2[i + 2])
                                {
                                    //Different pixels should remain.
                                    var y = i / blockCount / width;
                                    var x = i / blockCount - (y * width);

                                    //image2[i + 3] = 255; When saving frames with transparency without the 'Enable transparency' ticked, the pixels that changed should be set to opaque.

                                    startX[x] = true;
                                    startY[y] = true;
                                }
                                else
                                {
                                    image2[i] = chroma.B;
                                    image2[i + 1] = chroma.G;
                                    image2[i + 2] = chroma.R;
                                    image2[i + 3] = 0;
                                }
                            }

                            #endregion
                        }

                        //Console.WriteLine("Change: " + watch.Elapsed);

                        #region Verify positions

                        var firstX = startX.ToList().FindIndex(x => x);
                        var lastX = startX.ToList().FindLastIndex(x => x);

                        if (firstX == -1)
                            firstX = 0;
                        if (lastX == -1)
                            lastX = width;

                        var firstY = startY.ToList().FindIndex(x => x);
                        var lastY = startY.ToList().FindLastIndex(x => x);

                        if (lastY == -1)
                            lastY = height;
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
                            project.Frames[index - 1].Delay += project.Frames[index].Delay;
                            project.Frames[index].Rect = new Int32Rect(0, 0, 0, 0);
                            project.Frames[index].DataPosition = newStream.Position;
                            project.Frames[index].DataLength = 0;

                            GC.Collect(1);
                            continue;
                        }

                        if (heightCut != height)
                            heightCut++;

                        if (widthCut != width)
                            widthCut++;

                        project.Frames[index].Rect = new Int32Rect(firstX, firstY, widthCut, heightCut);

                        #endregion

                        #region Crop and save

                        var newPixels = CropImageArray(image2, width, 32, project.Frames[index].Rect);

                        //Writes to the buffer from end to start. Since I have the position, it does not matter.
                        project.Frames[index].DataPosition = newStream.Position;
                        project.Frames[index].DataLength = newPixels.LongLength;

                        newStream.WriteBytes(newPixels);

                        #endregion

                        //SavePixelArrayToFile(newPixels, project.Frames[index].Rect.Width, project.Frames[index].Rect.Height, 4, project.ChunkPath + index + ".png");

                        //Console.WriteLine("Save: " + watch.Elapsed);
                        //Console.WriteLine();

                        GC.Collect(1);

                        #endregion
                    }

                    EncodingManager.Update(taskId, LocalizationHelper.Get("S.Encoder.SavingAnalysis"), true);

                    #region Write the first frame

                    oldStream.Position = project.Frames[0].DataPosition;
                    var firstFrame = oldStream.ReadBytes((int)project.Frames[0].DataLength);

                    project.Frames[0].DataPosition = newStream.Position;
                    project.Frames[0].DataLength = firstFrame.LongLength;

                    //SavePixelArrayToFile(firstFrame, project.Frames[0].Rect.Width, project.Frames[0].Rect.Height, 4, project.ChunkPath + 0 + ".png");

                    newStream.WriteBytes(firstFrame);

                    #endregion
                }
            }
        }

        //Detect the data position of each frame.
        //for (var index = 1; index < project.Frames.Count - 1; index++)
        //    project.Frames[index].DataPosition = project.Frames[index - 1].DataLength + project.Frames[index - 1].DataPosition;

        //Replaces the chunk file.
        File.Delete(project.ChunkPath);
        File.Move(project.NewChunkPath, project.ChunkPath);

        return project;
    }

    /// <summary>
    /// Analyzes all frames (from the end to the start) and paints all unchanged pixels with a given color,
    /// after, it cuts the image to reduce filesize.
    /// </summary>
    /// <param name="project">The project with frames to analyze.</param>
    /// <param name="taskId">The Id of the Task.</param>
    /// <param name="tokenSource">The cancellation token source.</param>
    /// <returns>The project containing all frames and its cut points.</returns>
    public static ExportProject CutUnchanged(ExportProject project, int taskId, CancellationTokenSource tokenSource)
    {
        using (var oldStream = new FileStream(project.ChunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (var newStream = new FileStream(project.NewChunkPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                for (var index = project.Frames.Count - 1; index > 0; index--)
                {
                    #region Cancellation

                    if (tokenSource.Token.IsCancellationRequested)
                    {
                        EncodingManager.Update(taskId, EncodingStatus.Canceled);
                        break;
                    }

                    #endregion

                    #region For each frame, from the end to the start

                    EncodingManager.Update(taskId, project.Frames.Count - index - 1);

                    //var watch = Stopwatch.StartNew();

                    #region Get image info

                    oldStream.Position = project.Frames[index - 1].DataPosition;
                    var image1 = oldStream.ReadBytes((int)project.Frames[index - 1].DataLength); //Previous image.
                    oldStream.Position = project.Frames[index].DataPosition;
                    var image2 = oldStream.ReadBytes((int)project.Frames[index].DataLength); //Current image.

                    var startY = new bool[project.Frames[index - 1].Rect.Height];
                    var startX = new bool[project.Frames[index - 1].Rect.Width];

                    var height = project.Frames[index - 1].Rect.Height;
                    var width = project.Frames[index - 1].Rect.Width;
                    var blockCount = project.Frames[index - 1].ImageDepth / 8;

                    #endregion

                    //Console.WriteLine("Info: " + watch.Elapsed);

                    //Only use Parallel if the image is big enough.
                    if (width * height > 150000)
                    {
                        #region Parallel Loop

                        //x - width - sides
                        Parallel.For(0, image1.Length / blockCount, i =>
                        {
                            i *= blockCount;

                            if (image1[i] != image2[i] || image1[i + 1] != image2[i + 1] || image1[i + 2] != image2[i + 2])
                            {
                                //Different pixels should remain.
                                var y = i / blockCount / width;
                                var x = i / blockCount - (y * width);

                                //var current = (y * image1.Width + x) * blockCount == i;

                                startX[x] = true;
                                startY[y] = true;
                            }
                        });

                        #endregion
                    }
                    else
                    {
                        #region Sequential loop

                        for (var i = 0; i < image1.Length; i += blockCount)
                        {
                            if (image1[i] != image2[i] || image1[i + 1] != image2[i + 1] || image1[i + 2] != image2[i + 2])
                            {
                                //Different pixels should remain.
                                var y = i / blockCount / width;
                                var x = i / blockCount - (y * width);

                                //var current = (y * image1.Width + x) * blockCount == i;

                                startX[x] = true;
                                startY[y] = true;
                            }
                        }

                        #endregion
                    }

                    //Console.WriteLine("Change: " + watch.Elapsed);

                    #region Verify positions

                    var firstX = startX.ToList().FindIndex(x => x);
                    var lastX = startX.ToList().FindLastIndex(x => x);

                    if (firstX == -1)
                        firstX = 0;
                    if (lastX == -1)
                        lastX = width;

                    var firstY = startY.ToList().FindIndex(x => x);
                    var lastY = startY.ToList().FindLastIndex(x => x);

                    if (lastY == -1)
                        lastY = height;
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
                        project.Frames[index - 1].Delay += project.Frames[index].Delay;
                        project.Frames[index].Rect = new Int32Rect(0, 0, 0, 0);
                        project.Frames[index].DataPosition = newStream.Position;
                        project.Frames[index].DataLength = 0;

                        GC.Collect(1);
                        continue;
                    }

                    if (heightCut != height)
                        heightCut++;

                    if (widthCut != width)
                        widthCut++;

                    project.Frames[index].Rect = new Int32Rect(firstX, firstY, widthCut, heightCut);

                    #endregion

                    #region Crop and save

                    var newPixels = CropImageArray(image2, width, 32, project.Frames[index].Rect);

                    //Writes to the buffer from end to start. Since I have the position, it does not matter.
                    project.Frames[index].DataPosition = newStream.Position;
                    project.Frames[index].DataLength = newPixels.LongLength;

                    newStream.WriteBytes(newPixels);

                    #endregion

                    //Console.WriteLine("Save: " + watch.Elapsed);
                    //Console.WriteLine();

                    GC.Collect(1);

                    #endregion
                }

                EncodingManager.Update(taskId, LocalizationHelper.Get("S.Encoder.SavingAnalysis"), true);

                #region Write the first frame

                oldStream.Position = project.Frames[0].DataPosition;
                var firstFrame = oldStream.ReadBytes((int)project.Frames[0].DataLength);

                project.Frames[0].DataPosition = newStream.Position;
                project.Frames[0].DataLength = firstFrame.LongLength;

                newStream.WriteBytes(firstFrame);

                #endregion
            }
        }

        //Detect the data position of each frame.
        //for (var index = 1; index < project.Frames.Count - 1; index++)
        //    project.Frames[index].DataPosition = project.Frames[index - 1].DataLength + project.Frames[index - 1].DataPosition;

        //Replaces the chunk file.
        File.Delete(project.ChunkPath);
        File.Move(project.NewChunkPath, project.ChunkPath);

        return project;
    }


    public static List<IFrame> PaintTransparentAndCut(List<IFrame> listToEncode, System.Windows.Media.Color transparent, int taskId, CancellationTokenSource tokenSource)
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
                EncodingManager.Update(taskId, EncodingStatus.Canceled);
                break;
            }

            #endregion

            #region For each Frame, from the end to the start

            EncodingManager.Update(taskId, listToEncode.Count - index - 1);

            //var watch = Stopwatch.StartNew();

            #region Get Image Info

            var imageAux1 = listToEncode[index - 1].Path.SourceFrom();
            var imageAux2 = listToEncode[index].Path.SourceFrom();

            var startY = new bool[imageAux1.PixelHeight];
            var startX = new bool[imageAux1.PixelWidth];

            var image1 = new PixelUtil(imageAux1); //Previous image
            var image2 = new PixelUtil(imageAux2); //Actual image

            image1.LockBits();
            image2.LockBits();

            var height = imageAux1.PixelHeight;
            var width = imageAux1.PixelWidth;
            var blockCount = image1.Depth / 8;

            #endregion

            //Console.WriteLine("Lock: " + watch.Elapsed);

            //Only use Parallel if the image is big enough.
            if (width * height > 150000)
            {
                #region Parallel Loop

                //x - width - sides
                Parallel.For(0, image1.Pixels.Length / blockCount, i =>
                {
                    i = i * blockCount;

                    if (image1.Pixels[i] != image2.Pixels[i] || image1.Pixels[i + 1] != image2.Pixels[i + 1] || image1.Pixels[i + 2] != image2.Pixels[i + 2])
                    {
                        var y = i / blockCount / image1.Width;
                        var x = i / blockCount - (y * image1.Width);

                        //var current = (y * image1.Width + x) * blockCount == i;

                        startX[x] = true;
                        startY[y] = true;
                    }
                    else
                    {
                        image2.Pixels[i] = transparent.B;
                        image2.Pixels[i + 1] = transparent.G;
                        image2.Pixels[i + 2] = transparent.R;

                        if (blockCount == 4)
                            image2.Pixels[i + 3] = transparent.A; //255;
                    }
                });

                #endregion
            }
            else
            {
                #region Sequential loop

                for (var i = 0; i < image1.Pixels.Length; i += blockCount)
                {
                    if (image1.Pixels[i] != image2.Pixels[i] || image1.Pixels[i + 1] != image2.Pixels[i + 1] || image1.Pixels[i + 2] != image2.Pixels[i + 2])
                    {
                        var y = i / blockCount / image1.Width;
                        var x = i / blockCount - (y * image1.Width);

                        //var current = (y * image1.Width + x) * blockCount == i;

                        startX[x] = true;
                        startY[y] = true;
                    }
                    else
                    {
                        image2.Pixels[i] = transparent.B;
                        image2.Pixels[i + 1] = transparent.G;
                        image2.Pixels[i + 2] = transparent.R;

                        if (blockCount == 4)
                            image2.Pixels[i + 3] = transparent.A; //255;
                    }
                }

                #endregion
            }

            //Console.WriteLine("Change: " + watch.Elapsed);

            image1.UnlockBitsWithoutCommit();

            //Console.WriteLine("Unlock: " + watch.Elapsed);

            #region Verify positions

            var firstX = startX.ToList().FindIndex(x => x);
            var lastX = startX.ToList().FindLastIndex(x => x);

            if (firstX == -1)
                firstX = 0;
            if (lastX == -1)
                lastX = imageAux1.PixelWidth;

            var firstY = startY.ToList().FindIndex(x => x);
            var lastY = startY.ToList().FindLastIndex(x => x);

            if (lastY == -1)
                lastY = imageAux1.PixelHeight;
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

            using (var fileStream = new FileStream(listToEncode[index].Path, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image2.UnlockBitsAndCrop(new Int32Rect(firstX, firstY, widthCut, heightCut))));
                encoder.Save(fileStream);
            }

            imageAux1 = null;
            imageAux2 = null;

            #endregion

            //Console.WriteLine("Save: " + watch.Elapsed);
            //Console.WriteLine();

            GC.Collect(1);

            #endregion
        }

        return listToEncode;
    }

    public static List<IFrame> CutUnchanged(List<IFrame> listToEncode, int id, CancellationTokenSource tokenSource)
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
                EncodingManager.Update(id, EncodingStatus.Canceled);
                break;
            }

            #endregion

            #region For each Frame, from the end to the start

            EncodingManager.Update(id, listToEncode.Count - index - 1);

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
                }); //SPEEEEEED, a lot!

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


    /// <summary>
    /// Calculates the difference between one given frame and another.
    /// </summary>
    /// <param name="first">The first frame to compare.</param>
    /// <param name="second">The second frame to compare.</param>
    /// <returns>The similarity between the two frames in percentage.</returns>
    public static decimal CalculateDifference(IFrame first, IFrame second)
    {
        using var imageAux1 = first.Path.From();
        using var imageAux2 = second.Path.From();

        #region Get Image Info

        var height = imageAux1.Height;
        var width = imageAux1.Width;
        var pixelCount = height * width;

        #endregion

        var changedPixelCount = EnumerateIndexOfNonEqualPixels(imageAux1, imageAux2).AsParallel().Count();

        return MathExtensions.CrossMultiplication((decimal)pixelCount, pixelCount - changedPixelCount, null);
    }

    private static IEnumerable<int> EnumerateIndexOfNonEqualPixels(Bitmap imageAux1, Bitmap imageAux2)
    {
        if (imageAux1.PixelFormat != imageAux2.PixelFormat)
        {
            throw new ArgumentException("Images need to have same PixelFormat.");
        }

        if(Image.GetPixelFormatSize(imageAux1.PixelFormat) > 32)
        {
            throw new ArgumentException("PixelFormat not supported.");
        }

        var height = imageAux1.Height;
        var width = imageAux1.Width;

        var lineBuffer1 = new int[width];
        var lineBuffer2 = new int[width];
        var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

        for (int y = 0; y < height; y++)
        {
            PixelUtilOld.ReadLine(imageAux1, format, y, lineBuffer1);
            PixelUtilOld.ReadLine(imageAux2, format, y, lineBuffer2);

            for (int x = 0; x < width; x++)
            {
                if (lineBuffer1[x] != lineBuffer2[x])
                    yield return x + y * width;
            }
        }
    }

    /// <summary>
    /// Color distance calculation.
    /// https://www.compuphase.com/cmetric.htm
    /// </summary>
    public static double ColorDistance(Color e1, Color e2)
    {
        var rmean = (e1.R + (long)e2.R) / 2;
        var r = e1.R - (long)e2.R;
        var g = e1.G - (long)e2.G;
        var b = e1.B - (long)e2.B;

        return Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));
    }

    public static double ColorDistance(byte b1, byte g1, byte r1, byte b2, byte g2, byte r2)
    {
        var rMean = (r1 + (long)r2) / 2;
        var r = r1 - (long)r2;
        var g = g1 - (long)g2;
        var b = b1 - (long)b2;

        return Math.Sqrt((((512 + rMean) * r * r) >> 8) + 4 * g * g + (((767 - rMean) * b * b) >> 8));
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
            return GetFrameMetadata(gifMetadata.Frames[frameIndex]);

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

        var control = gifMetadata.Extensions.OfType<GifGraphicControlExtension>().FirstOrDefault();

        if (control == null)
            return frameMetadata;

        if (control.Delay != 0)
            frameMetadata.Delay = TimeSpan.FromMilliseconds(control.Delay);

        frameMetadata.DisposalMethod = (FrameDisposalMethod) control.DisposalMethod;

        return frameMetadata;
    }

    public static BitmapSource MakeFrame(System.Drawing.Size fullSize, BitmapSource rawFrame, FrameMetadata metadata, BitmapSource baseFrame, double? forceDpi = null)
    {
        //I removed this so I could save as Pbgra32.
        //if (baseFrame == null && IsFullFrame(metadata, fullSize))
        //{
        //    //No previous image to combine with, and same size as the full image, so just return as is.
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

        var bitmap = new RenderTargetBitmap(fullSize.Width, fullSize.Height, forceDpi ?? rawFrame.DpiX, forceDpi ?? rawFrame.DpiY, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        if (bitmap.CanFreeze && !bitmap.IsFrozen)
            bitmap.Freeze();

        return bitmap;
    }

    public static bool IsFullFrame(FrameMetadata metadata, System.Drawing.Size fullSize)
    {
        return metadata.Left == 0 && metadata.Top == 0 && metadata.Width == fullSize.Width && metadata.Height == fullSize.Height;
    }

    public static BitmapSource ClearArea(BitmapSource frame, FrameMetadata metadata, double? forcedDpi = null)
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

        var bitmap = new RenderTargetBitmap(frame.PixelWidth, frame.PixelHeight, forcedDpi ?? frame.DpiX, forcedDpi ?? frame.DpiY, PixelFormats.Pbgra32);
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
                return (Bitmap)bmp.Clone();
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

        if (ch == 1)
            format = PixelFormats.Gray8; //Grey scale image 0-255.
        else if (ch == 3)
            format = PixelFormats.Bgr24; //RGB.
        else if (ch == 4)
            format = PixelFormats.Bgr32; //RGB + alpha.

        for (var i = data.Count - 1; i < w * h * ch; i++)
            data.Add(0);

        var wbm = new WriteableBitmap(w, h, 96, 96, format, null);
        wbm.WritePixels(new Int32Rect(0, 0, w, h), data.ToArray(), ch * w, 0);

        return wbm;
    }

    public static void SavePixelArrayToFile(byte[] pixels, int width, int height, int channels, string filePath)
    {
        //var img = BitmapSource.Create(project.Frames[index].Rect.Width, project.Frames[index].Rect.Height, 96, 96, PixelFormats.Bgra32, null, newPixels, 4 * project.Frames[index].Rect.Width);

        //using (var stream = new FileStream(project.ChunkPath + index + ".png", FileMode.Create))
        //{
        //    var encoder = new PngBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(img));
        //    encoder.Save(stream);
        //    stream.Close();
        //}

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(FromArray(pixels.ToList(), width, height, channels)));
            encoder.Save(fileStream);
        }
    }

    #endregion

    #region Edit Images

    public static byte[] CropImageArray(byte[] pixels, int sourceWidth, int bitsPerPixel, Int32Rect rect)
    {
        var blockSize = bitsPerPixel / 8;
        var outputPixels = new byte[rect.Width * rect.Height * blockSize];

        //Create the array of bytes.
        for (var line = 0; line <= rect.Height - 1; line++)
        {
            var sourceIndex = ((rect.Y + line) * sourceWidth + rect.X) * blockSize;
            var destinationIndex = line * rect.Width * blockSize;

            Array.Copy(pixels, sourceIndex, outputPixels, destinationIndex, rect.Width * blockSize);
        }

        return outputPixels;
    }

    /// <summary>
    /// Resizes the given image.
    /// </summary>
    /// <param name="source">The image source.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="margin">Cut margin.</param>
    /// <param name="dpi">The DPI of the image.</param>
    /// <param name="scalingQuality">Scaling Quality to use when resizing. <seealso cref="System.Windows.Media.BitmapScalingMode"/></param>
    /// <returns>A resized ImageSource</returns>
    public static BitmapFrame ResizeImage(BitmapImage source, int width, int height, int margin = 0, double dpi = 96d, BitmapScalingMode scalingQuality = BitmapScalingMode.Unspecified)
    {
        var scale = dpi / 96d;

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            if (scalingQuality != BitmapScalingMode.Unspecified)
            {
                var dg = new DrawingGroup();
                using (var context = dg.Open())
                {
                    RenderOptions.SetBitmapScalingMode(dg, scalingQuality);
                    context.DrawImage(source, new Rect(0, 0, width / scale, height / scale));
                }

                drawingContext.DrawDrawing(dg);
            }
            else
            {
                drawingContext.DrawImage(source, new Rect(0, 0, width / scale, height / scale));
            }
        }

        //(int)Math.Round(width * scale)

        var resizedImage = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
        resizedImage.Render(drawingVisual);

        return BitmapFrame.Create(resizedImage);
    }

    /// <summary>
    /// Applies the pixelate effect in given frame.
    /// </summary>
    /// <param name="image">The image to pixelate.</param>
    /// <param name="rectX">The X-axis threshold of the area to pixelate.</param>
    /// <param name="rectY">The Y-axis threshold of the area to pixelate.</param>
    /// <param name="width">The X-axis threshold of the area to pixelate.</param>
    /// <param name="height">The Y-axis threshold of the area to pixelate.</param>
    /// <param name="pixelateSize">The size of the pixel.</param>
    /// <param name="opacityPower">Determinies how strong the opacity multiplier should be.</param>
    /// <param name="opacityDistance">Determines how far the opacity should start/end being smaller than 1 (based on the opacity power too).</param>
    /// <param name="useMedian">Calculate the median color of the pixel block.</param>
    /// <param name="inverted">Apply the effect to the inverted region of the selection.</param>
    /// <returns>A pixelated BitmapSource.</returns>
    internal static BitmapSource Pixelate(BitmapSource image, int rectX, int rectY, int width, int height, int pixelateSize, double opacityPower, int opacityDistance, bool useMedian, bool inverted)
    {
        var pixelUtil = new PixelUtil(image);
        pixelUtil.LockBits();

        var startX = inverted ? 0 : rectX;
        var startY = inverted ? 0 : rectY;
        var endX = inverted ? image.PixelWidth : rectX + width;
        var endY = inverted ? image.PixelHeight : rectY + height;

        //Loop through all the blocks that should be pixelated.
        for (var xx = startX; xx < endX; xx += pixelateSize)
        {
            for (var yy = startY; yy < endY; yy += pixelateSize)
            {
                //Ignore non selected parts of the image.
                if (inverted && xx > rectX && xx < rectX + width && yy > rectY && yy < rectY + height)
                    continue;

                var offsetX = pixelateSize / 2;
                var offsetY = pixelateSize / 2;

                if (xx + offsetX > image.PixelWidth)
                    offsetX = image.PixelWidth;

                if (yy + offsetY > image.PixelHeight)
                    offsetY = image.PixelHeight;

                //Get the pixel color in the center of the soon to be pixelated area.
                var changed = useMedian ? pixelUtil.GetMedianColor(xx, yy, offsetX, offsetY) : pixelUtil.GetPixel(xx + offsetX, yy + offsetY);

                //For each pixel in the pixelate size, set it to the center color.
                for (var x = xx; x < xx + pixelateSize && x < image.PixelWidth; x++)
                {
                    for (var y = yy; y < yy + pixelateSize && y < image.PixelHeight; y++)
                    {
                        //Ignore non selected parts of the image.
                        if (!inverted && (x < rectX || x > rectX + width || y < rectY || y > rectY + height))
                            continue;

                        if (inverted && x > rectX && x < rectX + width && y > rectY && y < rectY + height)
                            continue;

                        if (opacityDistance > 0)
                        {
                            var opacity = CalculateOpacity(inverted, x, y, rectX, rectY, width, height, opacityDistance, opacityPower);

                            pixelUtil.SetAndBlendPixel(x, y, changed, opacity);
                            continue;
                        }

                        pixelUtil.SetPixel(x, y, changed);
                    }
                }
            }
        }

        return pixelUtil.UnlockBits();
    }

    /// <summary>
    /// Applies the pixelate effect in given frame.
    /// </summary>
    /// <param name="image">The image to apply blur.</param>
    /// <param name="rectX">The X-axis threshold of the area to apply blur.</param>
    /// <param name="rectY">The Y-axis threshold of the area to apply blur.</param>
    /// <param name="width">The X-axis threshold of the area to apply blur.</param>
    /// <param name="height">The Y-axis threshold of the area to apply blur.</param>
    /// <param name="blurLevel">The level of blur.</param>
    /// <param name="opacityPower">Determinies how strong the opacity multiplier should be.</param>
    /// <param name="opacityDistance">Determines how far the opacity should start/end being smaller than 1 (based on the opacity power too).</param>
    /// <param name="inverted">Apply the effect to the inverted region of the selection.</param>
    /// <returns>A pixelated BitmapSource.</returns>
    internal static BitmapSource Blur(BitmapSource image, int rectX, int rectY, int width, int height, int blurLevel, double opacityPower, int opacityDistance, bool inverted)
    {
        var pixelUtil = new PixelUtil(image);
        pixelUtil.LockBits();

        var startX = inverted ? 0 : rectX;
        var startY = inverted ? 0 : rectY;
        var endX = inverted ? image.PixelWidth : rectX + width;
        var endY = inverted ? image.PixelHeight : rectY + height;

        Parallel.For(startX, pixelUtil.Pixels.Length / pixelUtil.ChannelsPerPixel, i =>
        {
            i *= pixelUtil.ChannelsPerPixel;

            var y = i / pixelUtil.ChannelsPerPixel / image.PixelWidth;
            var x = i / pixelUtil.ChannelsPerPixel - (y * image.PixelWidth);

            //Ignore non selected parts of the image.
            if (!inverted && (x < startX || x > endX || y < startY || y > endY))
                return;

            if (inverted && x > rectX && x < rectX + width && y > rectY && y < rectY + height)
                return;

            //Apply the blur
            int avgR = 0, avgG = 0, avgB = 0, avgA = 0;
            var blurPixelCount = 0;

            //Get the average of the colors in the block.
            for (var xx = x; xx < x + blurLevel && xx < image.PixelWidth; xx++)
            {
                for (var yy = y; yy < y + blurLevel && yy < image.PixelHeight; yy++)
                {
                    var pixel = pixelUtil.GetPixel(xx, yy);

                    avgB += pixel.B;
                    avgG += pixel.G;
                    avgR += pixel.R;
                    avgA += pixel.A;

                    blurPixelCount++;
                }
            }

            if (blurPixelCount > 0)
            {
                avgR /= blurPixelCount;
                avgG /= blurPixelCount;
                avgB /= blurPixelCount;
                avgA /= blurPixelCount;
            }

            //Apply the average to the block.
            for (var xx = x; xx < x + blurLevel && xx < image.PixelWidth; xx++)
            {
                for (var yy = y; yy < y + blurLevel && yy < image.PixelHeight; yy++)
                {
                    //Ignore non selected parts of the image.
                    if (!inverted && (xx < rectX || xx > rectX + width || yy < rectY || yy > rectY + height))
                        continue;

                    if (inverted && xx > rectX && xx < rectX + width && yy > rectY && yy < rectY + height)
                        continue;

                    if (opacityDistance > 0)
                    {
                        var opacity = CalculateOpacity(inverted, xx, yy, rectX, rectY, width, height, opacityDistance, opacityPower);

                        pixelUtil.SetAndBlendPixel(xx, yy, (byte)avgB, (byte)avgG, (byte)avgR, (byte)avgA, opacity);
                        continue;
                    }

                    pixelUtil.SetPixel(xx, yy, (byte)avgB, (byte)avgG, (byte)avgR, (byte)avgA);
                }
            }
        });

        #region Non parallel

        //for (var xx = startX; xx < endX; xx++)
        //{
        //    for (var yy = startY; yy < endY; yy++)
        //    {
        //        //Ignore non selected parts of the image.
        //        if (inverted && xx > rectX && xx < rectX + width && yy > rectY && yy < rectY + height)
        //            continue;

        //        int avgR = 0, avgG = 0, avgB = 0, avgA = 0;
        //        var blurPixelCount = 0;

        //        //Get the average of the colors in the block.
        //        for (var x = xx; x < xx + blurLevel && x < image.PixelWidth; x++)
        //        {
        //            for (var y = yy; y < yy + blurLevel && y < image.PixelHeight; y++)
        //            {
        //                var pixel = pixelUtil.GetPixel(x, y);

        //                avgB += pixel.B;
        //                avgG += pixel.G;
        //                avgR += pixel.R;
        //                avgA += pixel.A;

        //                blurPixelCount++;
        //            }
        //        }

        //        avgR /= blurPixelCount;
        //        avgG /= blurPixelCount;
        //        avgB /= blurPixelCount;
        //        avgA /= blurPixelCount;

        //        //Apply the average to the block.
        //        for (var x = xx; x < xx + blurLevel && x < image.PixelWidth; x++)
        //        {
        //            for (var y = yy; y < yy + blurLevel && y < image.PixelHeight; y++)
        //            {
        //                //Ignore non selected parts of the image.
        //                if (!inverted && (x < rectX || x > rectX + width || y < rectY || y > rectY + height))
        //                    continue;

        //                if (inverted && x > rectX && x < rectX + width && y > rectY && y < rectY + height)
        //                    continue;

        //                if (opacityDistance > 0)
        //                {
        //                    var opacity = CalculateOpacity(inverted, x, y, rectX, rectY, width, height, opacityDistance, opacityPower);

        //                    pixelUtil.SetAndBlendPixel(x, y, (byte)avgB, (byte)avgG, (byte)avgR, (byte)avgA, opacity);
        //                    continue;
        //                }

        //                pixelUtil.SetPixel(x, y, (byte)avgB, (byte)avgG, (byte)avgR, (byte)avgA);
        //            }
        //        }
        //    }
        //}

        #endregion

        return pixelUtil.UnlockBits();
    }

    /// <summary>
    /// Darkens or lightens the given frame.
    /// </summary>
    /// <param name="image">The image to have the lightness altered.</param>
    /// <param name="rectX">The X-axis threshold of the area to be altered.</param>
    /// <param name="rectY">The Y-axis threshold of the area to be altered.</param>
    /// <param name="width">The X-axis threshold of the area to be altered.</param>
    /// <param name="height">The Y-axis threshold of the area to alter lightness.</param>
    /// <param name="isDarken">True if the images should be darkened, else lightened.</param>
    /// <param name="lightnessLevel">The level of lightness.</param>
    /// <param name="opacityPower">Determinies how strong the opacity multiplier should be.</param>
    /// <param name="opacityDistance">Determines how far the opacity should start/end being smaller than 1 (based on the opacity power too).</param>
    /// <param name="inverted">Apply the effect to the inverted region of the selection.</param>
    /// <returns>A lightness altered BitmapSource.</returns>
    internal static BitmapSource Lightness(BitmapSource image, int rectX, int rectY, int width, int height, bool isDarken, double lightnessLevel, double opacityPower, int opacityDistance, bool inverted)
    {
        var pixelUtil = new PixelUtil(image);
        pixelUtil.LockBits();

        var startX = inverted ? 0 : rectX;
        var startY = inverted ? 0 : rectY;
        var endX = inverted ? image.PixelWidth : rectX + width;
        var endY = inverted ? image.PixelHeight : rectY + height;

        Parallel.For(startX, pixelUtil.Pixels.Length / pixelUtil.ChannelsPerPixel, i =>
        {
            i *= pixelUtil.ChannelsPerPixel;

            var y = i / pixelUtil.ChannelsPerPixel / image.PixelWidth;
            var x = i / pixelUtil.ChannelsPerPixel - (y * image.PixelWidth);

            //Ignore non selected parts of the image.
            if (!inverted && (x < startX || x > endX || y < startY || y > endY))
                return;

            if (inverted && x > rectX && x < rectX + width && y > rectY && y < rectY + height)
                return;

            //Apply smoothness.
            var original = pixelUtil.GetPixel(x, y);
            var changed = ChangeColorBrightness(original, lightnessLevel / 100d * (isDarken ? -1 : 1));

            if (opacityDistance > 0)
            {
                var opacity = CalculateOpacity(inverted, x, y, rectX, rectY, width, height, opacityDistance, opacityPower);

                pixelUtil.SetAndBlendPixel(x, y, changed, opacity);
                return;
            }

            pixelUtil.SetPixel(x, y, changed);
        });

        #region Non parallel

        //for (var xx = startX; xx < endX; xx++)
        //{
        //    for (var yy = startY; yy < endY; yy++)
        //    {
        //        //Ignore non selected parts of the image.
        //        if (inverted && xx > rectX && xx < rectX + width && yy > rectY && yy < rectY + height)
        //            continue;

        //        //Apply smoothness.
        //        var original = pixelUtil.GetPixel(xx, yy);
        //        var changed = ChangeColorBrightness(original, lightnessLevel / 100d * (isDarken ? -1 : 1));

        //        if (opacityDistance > 0)
        //        {
        //            var opacity = CalculateOpacity(inverted, xx, yy, rectX, rectY, width, height, opacityDistance, opacityPower);

        //            pixelUtil.SetAndBlendPixel(xx, yy, changed, opacity);
        //            continue;
        //        }

        //        pixelUtil.SetPixel(xx, yy, changed);
        //    }
        //}

        #endregion

        return pixelUtil.UnlockBits();
    }

    /// <summary>
    /// Creates color with corrected brightness.
    /// https://stackoverflow.com/a/12598573/1735672
    /// </summary>
    /// <param name="color">Color to correct.</param>
    /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. Negative values produce darker colors.</param>
    public static Color ChangeColorBrightness(Color color, double correctionFactor)
    {
        var red = (double)color.R;
        var green = (double)color.G;
        var blue = (double)color.B;

        if (correctionFactor < 0)
        {
            correctionFactor = 1 + correctionFactor;
            red *= correctionFactor;
            green *= correctionFactor;
            blue *= correctionFactor;
        }
        else
        {
            red = (255 - red) * correctionFactor + red;
            green = (255 - green) * correctionFactor + green;
            blue = (255 - blue) * correctionFactor + blue;
        }

        return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
    }

    /// <summary>
    /// Blends two colors based on a given opacity percentage.
    /// </summary>
    /// <param name="bottom">The current base color.</param>
    /// <param name="top">The new color that will be put on top of the base one.</param>
    /// <param name="opacity">The percentage of how much top color to put on top of the base color.</param>
    public static Color AlphaBlend(Color bottom, Color top, double opacity)
    {
        //var alpha = (byte)(255 * 1 - (1 - bottom.A) * (1 - top.A));
        var alpha = (byte)((top.A * opacity) + bottom.A * (1 - opacity));
        var red = (byte)((top.R * opacity) + bottom.R * (1 - opacity));
        var green = (byte)((top.G * opacity) + bottom.G * (1 - opacity));
        var blue = (byte)((top.B * opacity) + bottom.B * (1 - opacity));

        return Color.FromArgb(alpha, red, green, blue);
    }

    private static double CalculateOpacity(bool inverted, int xx, int yy, int rectX, int rectY, int width, int height, int opacityDistance, double opacityPower)
    {
        if (inverted)
        {
            var left = xx <= rectX && //Left.
                       yy - opacityDistance <= rectY + height && //Bottom corner.
                       yy + opacityDistance >= rectY ? //Top corner.
                rectX - xx : int.MaxValue;

            var top = yy <= rectY && //Top
                      xx - opacityDistance <= rectX + width && //Right corner.
                      xx + opacityDistance >= rectX ? //Left corner.
                rectY - yy : int.MaxValue;

            var right = xx >= rectX + width && //Right.
                        yy - opacityDistance <= rectY + height && //Bottom corner.
                        yy + opacityDistance >= rectY ? //Top corner.
                xx - (rectX + width) : int.MaxValue;

            var bottom = yy >= rectY + height && //Bottom.
                         xx - opacityDistance <= rectX + width && //Right corner.
                         xx + opacityDistance >= rectX ? //Left corner.
                yy - (rectY + height) : int.MaxValue;

            if (xx <= rectX && yy <= rectY) //Top left corner.
                top = left = Math.Max(top, left);

            if (xx >= rectX + width && yy <= rectY) //Top right corner.
                top = right = Math.Max(right, top);

            if (xx >= rectX + width && yy >= rectY + height) //Bottom right corner.
                bottom = right = Math.Max(bottom, right);

            if (xx <= rectX && yy >= rectY + height) //Bottom left corner.
                bottom = left = Math.Max(bottom, left);

            var distance = new[] { left, top, right, bottom }.OrderBy(o => o).First();
            return distance <= opacityDistance ? opacityPower / 100d * ((distance * 100d) / opacityDistance) / 100d : 1d;
        }

        //TODO: Option to avoid smoothing near the edges.
        //var distance = new[] { xx - rectX, yy - rectY, rectX + width - xx, rectY + height - yy }.OrderBy(o => o).First();

        var left2 = xx - rectX;
        var top2 = yy - rectY;
        var right2 = rectX + width - xx;
        var bottom2 = rectY + height - yy;

        var distance2 = new[] { left2, top2, right2, bottom2 }.OrderBy(o => o).First();
        return distance2 <= opacityDistance ? opacityPower / 100d * ((distance2 * 100d) / opacityDistance) / 100d : 1d;
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
            if (source is Control control)
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
            if (source is FrameworkElement control)
                bounds = new Rect(new System.Windows.Point(0d, 0d), new System.Windows.Point(control.ActualWidth * scale, control.ActualHeight * scale));
        }

        #endregion

        var rtb = new RenderTargetBitmap((int)Math.Round(size.Width), (int)Math.Round(size.Height), dpi, dpi, PixelFormats.Pbgra32);

        //source.Clip = new RectangleGeometry(new Rect(0, 0, rtb.Width, rtb.Height));
        //source.ClipToBounds = true;

        var dv = new DrawingVisual();

        using (var ctx = dv.RenderOpen())
        {
            var vb = new VisualBrush(source)
            {
                AutoLayoutContent = false,
                Stretch = Stretch.Fill
            };

            var uiScale = source.Scale();

            //Test with high dpi.
            //For some reason, an InkCanvas with Strokes going beyond the bounds will report a strange bound even if clipped.
            if (bounds.Width > size.Width / uiScale)
                bounds.Width = size.Width / uiScale;

            if (bounds.Height > size.Height / uiScale)
                bounds.Height = size.Height / uiScale;

            if (bounds.X < 0)
                bounds.X = 0;

            if (bounds.Y < 0)
                bounds.Y = 0;

            var locationRect = new System.Windows.Point(bounds.X * scale, bounds.Y * scale);
            var sizeRect = new System.Windows.Size(bounds.Width * scale, bounds.Height * scale);

            ctx.DrawRectangle(vb, null, new Rect(locationRect, sizeRect));
        }

        rtb.Render(dv);

        //source.Clip = null;

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

    /// <summary>
    /// Reads a given image resource into a WinForms icon.
    /// </summary>
    /// <param name="imageSource">Image source pointing to an icon file (*.ico).</param>
    /// <returns>An icon object that can be used with the taskbar area.</returns>
    public static Icon ToIcon(this ImageSource imageSource)
    {
        if (imageSource == null)
            return null;

        StreamResourceInfo streamInfo = null;

        try
        {
            var uri = new Uri(imageSource.ToString());
            streamInfo = Application.GetResourceStream(uri);

            if (streamInfo == null)
                throw new ArgumentException($"It was not possible to load the image source: '{imageSource}'.");

            return new Icon(streamInfo.Stream);
        }
        catch (Win32Exception e)
        {
            LogWriter.Log(e, "It was not possible to load the notification area icon.", $"StreamInfo is null? {streamInfo == null}, Native error code: {e.NativeErrorCode}");
            return null;
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "It was not possible to load the notification area icon.", $"StreamInfo is null? {streamInfo == null}");
            return null;
        }
        finally
        {
            streamInfo?.Stream?.Dispose();
        }
    }

    #endregion
}