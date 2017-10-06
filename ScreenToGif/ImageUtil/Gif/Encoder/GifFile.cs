using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ScreenToGif.ImageUtil.Gif.Encoder.Quantization;
using ScreenToGif.Util;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.ImageUtil.Gif.Encoder
{
    /// <summary>
    /// New gif encoder. Made by Nicke Manarin.
    /// </summary>
    public class GifFile : IDisposable
    {
        #region Properties

        /// <summary>
        /// Repeat Count for the gif.
        /// </summary>
        public int RepeatCount { get; set; } = 0;

        /// <summary>
        /// The color marked as transparent. Null if not in use.
        /// </summary>
        public Color? TransparentColor { get; set; }

        /// <summary>
        /// The current color being used as transparent. Null if not in use.
        /// </summary>
        public Color? CurrentTransparentColor { get; set; }

        /// <summary>
        /// The maximum number of colors of each frame of the gif.
        /// </summary>
        public int MaximumNumberColor { get; set; } = 256;

        /// <summary>
        /// The maximum number of colors of each frame of the gif.
        /// </summary>
        public ColorQuantizationType QuantizationType { get; set; } = ColorQuantizationType.Octree;

        /// <summary>
        /// True if the gif should use a global color table instead of a local one.
        /// </summary>
        public bool UseGlobalColorTable { get; set; } = false;

        /// <summary>
        /// The stream which the gif is writen on.
        /// </summary>
        private Stream InternalStream { get; set; }

        /// <summary>
        /// True if it's the first frame of the gif.
        /// </summary>
        private bool IsFirstFrame { get; set; } = true;

        /// <summary>
        /// The full size rect of the gif.
        /// </summary>
        private Int32Rect FullSize { get; set; }

        /// <summary>
        /// The list of indexed pixels, based on a color table (palette).
        /// </summary>
        private byte[] IndexedPixels { get; set; }

        /// <summary>
        /// The current color table. Global or local.
        /// </summary>
        private List<Color> ColorTable { get; set; }

        /// <summary>
        /// True if the frame will use a transparent flag. Not necessary if there's no presence of the color marked as transparent.
        /// </summary>
        private bool WillUseTransparency { get; set; }

        /// <summary>
        /// The size of the current color table.
        /// </summary>
        private int ColorTableSize { get; set; }

        #endregion

        public GifFile(Stream stream, Color? transparent, int repeatCount = 0)
        {
            InternalStream = stream;
            TransparentColor = transparent;
            RepeatCount = repeatCount;
        }

        public GifFile(Stream stream, int repeatCount = 0)
        {
            InternalStream = stream;
            RepeatCount = repeatCount;
        }

        #region Public Methods

        public void AddFrame(string path, Int32Rect rect, int delay = 66)
        {
            CurrentTransparentColor = TransparentColor;

            ReadPixels(path);
            //HandleTransparency();

            //For global color table, only generate a new palette if it's the first frame.
            if (!UseGlobalColorTable || IsFirstFrame)
                CalculateColorTableSize();

            if (IsFirstFrame)
            {
                FullSize = rect;

                WriteLogicalScreenDescriptor(rect);

                //Global color table.
                if (UseGlobalColorTable)
                    WritePalette();

                //TODO: Only repeat if there's more than 1 frame.
                if (RepeatCount > -1)
                    WriteApplicationExtension();
            }

            WriteGraphicControlExtension(delay);
            WriteImageDescriptor(rect);

            IsFirstFrame = false;

            //Local color table.
            if (!UseGlobalColorTable)
                WritePalette();

            WriteImage();
        }

        #endregion

        #region Main Methods

        private void WriteLogicalScreenDescriptor(Int32Rect rect)
        {
            //File Header, 6 bytes
            WriteString("GIF89a");

            //Initial Logical Size (Width, Height), 4 bytes
            WriteShort(rect.Width);
            WriteShort(rect.Height);

            //Packed fields, 1 byte
            var bitArray = new BitArray(8);
            bitArray.Set(0, UseGlobalColorTable);

            //Color resolution: 111 = (8 bits - 1)
            //Color depth - 1
            //Global colors count = 2^color depth
            var pixelBits = ToBitValues(ColorTableSize);

            bitArray.Set(1, pixelBits[0]);
            bitArray.Set(2, pixelBits[1]);
            bitArray.Set(3, pixelBits[2]);

            //Sort flag (for the global color table): 0
            bitArray.Set(4, true);

            //Size of the Global Color Table (Zero, if not used.): 
            var sizeInBits = ToBitValues(UseGlobalColorTable ? ColorTableSize : 0);

            bitArray.Set(5, sizeInBits[0]);
            bitArray.Set(6, sizeInBits[1]);
            bitArray.Set(7, sizeInBits[2]);

            WriteByte(ConvertToByte(bitArray));
            WriteByte(0); //Background color index, 1 byte
            WriteByte(0); //Pixel aspect ratio - Assume 1:1, 1 byte
        }

        private void WritePalette()
        {
            foreach (var color in ColorTable)
            {
                WriteByte(color.R);
                WriteByte(color.G);
                WriteByte(color.B);
            }

            //Do I need to fill up the rest of the color table? 
            //Or just seek the stream to the next place?

            //(MaximumColorsCount -  ColorCount) * 3 channels [rgb]
            var emptySpace = (GetMaximumColorCount() - ColorTable.Count) * 3;

            for (var index = 0; index < emptySpace; index++)
                WriteByte(0);
        }

        private void WriteApplicationExtension()
        {
            WriteByte(0x21); //Extension Introducer.
            WriteByte(0xff); //Extension Label.

            WriteByte(0x0b); //Application Block Size. It says "11 bytes".
            WriteString("NETSCAPE2.0"); //Extension type, 11 bytes
            WriteByte(0x03); // Application block length
            WriteByte(0x01); //Loop sub-block ID. 1 byte
            WriteShort(RepeatCount); // Repeat count. 2 bytes.
            WriteByte(0x00); //Terminator
        }

        private void WriteGraphicControlExtension(int delay)
        {
            WriteByte(0x21); //Extension Introducer.
            WriteByte(0xf9); //Extension Label.
            WriteByte(0x04); //Block size.

            //Packed fields
            var bitArray = new BitArray(8);

            //Reserved for future use. Hahahaha. Yeah...
            bitArray.Set(0, false);
            bitArray.Set(1, false);
            bitArray.Set(2, false);

            #region Disposal Method

            //Use Inplace if you want to Leave the last frame pixel.
            //GCE_DISPOSAL_NONE = Undefined = 0
            //GCE_DISPOSAL_INPLACE = Leave = 1
            //GCE_DISPOSAL_BACKGROUND = Restore Background = 2
            //GCE_DISPOSAL_RESTORE = Restore Previous = 3

            //If transparency is set
            //First frame as "Leave" with no Transparency. IsFirstFrame
            //Following frames as "Undefined" with Transparency.

            //Was TransparentColor.HasValue && 
            if (IsFirstFrame)
            {
                //Leave.
                bitArray.Set(3, false);
                bitArray.Set(4, false);
                bitArray.Set(5, true);
            }
            else
            {
                //Undefined.
                bitArray.Set(3, false);
                bitArray.Set(4, false);
                bitArray.Set(5, false);
            }

            #endregion

            //User Input Flag.
            bitArray.Set(6, false);

            //Transparent Color Flag, uses tranparency?
            bitArray.Set(7, !IsFirstFrame && CurrentTransparentColor.HasValue && ColorTable.Contains(CurrentTransparentColor.Value));

            //Write the packed fields.
            WriteByte(ConvertToByte(bitArray));
            WriteShort(delay / 10); //Delay x 1/100 seconds. Minimum of 10ms. Centiseconds.
            WriteByte(FindTransparentColorIndex()); //Transparency Index.
            WriteByte(0); //Terminator.
        }

        private void WriteImageDescriptor(Int32Rect rect)
        {
            WriteByte(0x2c); //Image Separator.
            WriteShort(rect.X); //Position X. 2 bytes.
            WriteShort(rect.Y); //Position Y. 2 bytes.
            WriteShort(rect.Width); //Width. 2 bytes.
            WriteShort(rect.Height); //Height. 2 bytes.

            if (UseGlobalColorTable)
            {
                //No Local Color Table. Every packed field values are zero.
                WriteByte(0);
                return;
            }

            //Packed fields.
            var bitArray = new BitArray(8);

            //Uses local color table?
            bitArray.Set(0, true);

            //Interlace Flag.
            bitArray.Set(1, false);

            //Sort Flag.
            bitArray.Set(2, true);

            //Reserved for future use. Hahahah again.
            bitArray.Set(3, false);
            bitArray.Set(4, false);

            //Size of Local Color Table.
            var sizeInBits = ToBitValues(ColorTableSize);

            bitArray.Set(5, sizeInBits[0]);
            bitArray.Set(6, sizeInBits[1]);
            bitArray.Set(7, sizeInBits[2]);

            //Write the packed fields.
            WriteByte(ConvertToByte(bitArray));
        }

        private void WriteImage()
        {
            //TODO: Fix the new LZW encoder when ColorTableSize == 7. It's getting corrupted. 
            //if (ColorTableSize < 6)
            //{
            //    //New LZW encoder, ColorTableSize from 1 to 8.
            //    var encoder = new LzwEncoder(IndexedPixels, ColorTableSize + 1);
            //    encoder.Encode(InternalStream);
            //}
            //else
            //{
            //Old LZW encoder, only works with ColorTableSize 8.
            var encoder = new Gif.LegacyEncoder.LzwEncoder(0, 0, IndexedPixels, 8);
            encoder.Encode(InternalStream);
            //}
        }

        #endregion

        #region Helper Methods

        private void ReadPixels(string path)
        {
            var image = path.SourceFrom();
            var pixelUtil = new PixelUtil(image);
            pixelUtil.LockBits();

            CurrentTransparentColor = TransparentColor;

            //if (QuantizationType == ColorQuantizationType.Grayscale)
            //{
            //    var quantizer = new GrayscaleQuantizer { MaxColors = MaximumNumberColor };
            //    IndexedPixels = quantizer.Quantize(pixelUtil.Pixels);
            //    ColorTable = quantizer.ColorTable;
            //}
            //else
            //{
            var quantizer = new OctreeQuantizer
            {
                MaxColors = MaximumNumberColor,
                TransparentColor = !IsFirstFrame || UseGlobalColorTable ? CurrentTransparentColor : null
            };

            IndexedPixels = quantizer.Quantize(pixelUtil.Pixels);
            ColorTable = quantizer.ColorTable;
            //}

            pixelUtil.UnlockBits();

            if (quantizer.TransparentColor != null)
                CurrentTransparentColor = quantizer.TransparentColor;

            //I need to signal the other method that I won't need transparency.
            WillUseTransparency = !IsFirstFrame && CurrentTransparentColor.HasValue && ColorTable.Contains(CurrentTransparentColor.Value);
        }

        private void HandleTransparency()
        {
            //ColorTable = NonIndexedPixels.AsParallel().GroupBy(x => x)
            //    .OrderByDescending(g => g.Count()) //Order by most frequent values
            //    .Select(g => g.FirstOrDefault()) //take the first among the group
            //    .Take(MaximumNumberColor).ToList();

            //Make sure that the transparent color is added to list.
            if (TransparentColor.HasValue && (!IsFirstFrame || UseGlobalColorTable))// && ColorTable.Count == MaximumNumberColor)
            {
                //Only adds if there is MaximumNumberColor colors, so I need to make sure that the color won't be ignored.
                //If there is less than MaximumNumberColor selected colors, it means that the transparent color is already selected.

                //If the color isn't on the list, add or replace.
                if (ColorTable.AsParallel().All(x => x != TransparentColor.Value))
                {
                    //Adds to the last spot, keeping it sorted. (Since all the colors are ordered by descending)
                    ColorTable.Insert(ColorTable.Count - 1, TransparentColor.Value);

                    //Remove the exceding value at the last position.

                    if (ColorTable.Count > MaximumNumberColor)
                        ColorTable.RemoveAt(MaximumNumberColor);
                }
            }

            //I need to signal the other method that I won't need transparency.
            WillUseTransparency = !IsFirstFrame && TransparentColor.HasValue && ColorTable.Contains(TransparentColor.Value);
        }

        private void WriteByte(int value)
        {
            InternalStream.WriteByte(Convert.ToByte(value));
        }

        /// <summary>
        /// Writes a int value as 2 bytes, but inverted. 
        /// 100 = 64 00 instead of 00 64.
        /// </summary>
        /// <param name="value"></param>
        private void WriteShort(int value)
        {
            //Writes the second part first.
            //The "& 0xff" makes sure that the int will stay on range 0-255, it will cut any number above 255.
            InternalStream.WriteByte(Convert.ToByte(value & 0xff));
            InternalStream.WriteByte(Convert.ToByte((value >> 8) & 0xff));
        }

        private void WriteString(string value)
        {
            InternalStream.Write(value.ToArray().Select(c => (byte)c).ToArray(), 0, value.Length);
        }

        private byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
                throw new ArgumentException("bits");

            var bytes = new byte[1];
            var reversed = new BitArray(bits.Cast<bool>().Reverse().ToArray());
            reversed.CopyTo(bytes, 0);
            return bytes[0];
        }

        private void CalculateColorTableSize()
        {
            //Logical Screen Description, Number of Colors, Byte Lenght
            //0 = 2 = 6
            //1 = 4 = 12
            //2 = 8 = 24
            //3 = 16 = 48
            //4 = 32 = 96
            //5 = 64 = 192
            //6 = 128 = 384
            //7 = 256 = 768
            //The inverse calculation is: 2^(N + 1) 
            //and x3 for the byte lenght.

            //If the colorsCount == 1, 
            //return zero instead of calculating it, because of the Log(0) call.
            //The "-1" assures that the count stays in range.
            ColorTableSize = ColorTable.Count > 1 ? (int)Math.Log(ColorTable.Count - 1, 2) : 0;
        }

        //private byte[] IndexPixels(List<Color> palette)
        //{
        //    return NonIndexedPixels.AsParallel().Select((x, i) => new { Index = i, Indexed = (byte)ColorExtensions.ClosestColorRgb(palette, x) }).Select(x => x.Indexed).ToArray();

        //    #region Old code

        //    //var pixels = new byte[NonIndexedPixels.Count];
        //    //var pixelCount = 0;
        //    //foreach (var color in NonIndexedPixels)
        //    //{
        //    //    var index = palette.IndexOf(color);

        //    //    if (index == -1)
        //    //    {
        //    //        //Search for nearby colors.
        //    //        index = ColorExtensions.ClosestColorRgb(palette, color);
        //    //        //index = ColorExtensions.ClosestColorHue(palette, color);
        //    //        //index = ColorExtensions.ClosestColorRsb(palette, color);

        //    //        //Add colors to a dictionary, if available, no need to search.
        //    //        //TODO: Make this available for choice.
        //    //    }

        //    //    //Map the pixel to a color in the Color Table.
        //    //    pixels[pixelCount] = (byte)index;
        //    //    pixelCount++;
        //    //}

        //    //return pixels;

        //    #endregion
        //}

        /// <summary>
        /// Calculates the maximum number of colors for the 
        /// specified Logical Screen Description value.
        /// </summary>
        /// <returns>The maximum number of colors in the Color Table.</returns>
        private int GetMaximumColorCount()
        {
            //2^(N+1)
            return (int)Math.Pow(2, ColorTableSize + 1);
        }

        private int FindTransparentColorIndex()
        {
            if (IsFirstFrame || !CurrentTransparentColor.HasValue || !ColorTable.Contains(CurrentTransparentColor.Value)) return 0;

            var index = ColorTable.IndexOf(CurrentTransparentColor.Value);

            return index > -1 ? index : 0;
        }

        /// <summary>
        /// Transforms a number to a bool array of 3 positions.
        /// </summary>
        /// <param name="number">The number to convert.</param>
        /// <returns>A 3-sized byte array.</returns>
        private bool[] ToBitValues(int number)
        {
            return new BitArray(new[] { number }).Cast<bool>().Take(3).Reverse().ToArray();
        }

        #endregion

        public void Dispose()
        {
            // Complete File
            WriteByte(0x3b);
            // Pushing data
            InternalStream.Flush();
            //Resets the stream position to save afterwards.
            InternalStream.Position = 0;
        }
    }
}