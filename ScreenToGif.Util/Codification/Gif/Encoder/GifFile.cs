using ScreenToGif.Domain.Enums;
using ScreenToGif.Util.Codification.Gif.Encoder.Quantization;
using System.Collections;
using System.IO;
using System.Windows;
using Color = System.Windows.Media.Color;

namespace ScreenToGif.Util.Codification.Gif.Encoder;

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
    /// When enabled, the entire (star and end) gif will contain a transparent background.
    /// Not related to the option to "paint unchanged pixels".
    /// </summary>
    public bool UseFullTransparency { get; set; }

    /// <summary>
    /// The color marked as transparent. Null if not in use.
    /// </summary>
    public Color? TransparentColor { get; set; }

    /// <summary>
    /// The maximum number of colors of each frame of the gif.
    /// </summary>
    public int MaximumNumberColor { get; set; } = 256;

    /// <summary>
    /// The maximum number of colors of each frame of the gif.
    /// </summary>
    public ColorQuantizationTypes QuantizationType { get; set; } = ColorQuantizationTypes.Octree;

    /// <summary>
    /// True if the gif should use a global color table instead of a local one.
    /// </summary>
    public bool UseGlobalColorTable { get; set; } = false;

    /// <summary>
    /// The sampling factor of the neural network quantizer.
    /// </summary>
    public int SamplingFactor { get; set; }


    /// <summary>
    /// The stream which the gif is written on.
    /// </summary>
    private Stream InternalStream { get; set; }

    /// <summary>
    /// True if it's the first frame of the gif.
    /// </summary>
    private bool IsFirstFrame { get; set; } = true;

    /// <summary>
    /// The list of indexed pixels, based on a color table (palette).
    /// </summary>
    private byte[] IndexedPixels { get; set; }

    /// <summary>
    /// The current color table. Global or local.
    /// </summary>
    private List<Color> ColorTable { get; set; }

    /// <summary>
    /// True if the color table contains the color that will be treated as transparent.
    /// </summary>
    private bool ColorTableHasTransparency { get; set; }

    /// <summary>
    /// The size of the current color table.
    /// </summary>
    private int ColorTableSize { get; set; }

    /// <summary>
    /// Cumulative non adjusted time.
    /// </summary>
    private int OrganicTime { get; set; }
        
    /// <summary>
    /// Adjusted and rounded off time.
    /// </summary>
    private int AdjustedTime { get; set; }

    /// <summary>
    /// If a quantizer needs to be constructed only once (for example, to use with PaletteQuantizers), this property will be used.
    /// </summary>
    private Quantizer GlobalQuantizer { get; set; }

    #endregion


    public GifFile(Stream stream)
    {
        InternalStream = stream;
    }


    #region Public methods

    public void AddFrame(byte[] pixels, Int32Rect rect, int delay = 66, bool isLastFrame = false)
    {
        ReadPixels(pixels);

        //For global color table, only generate a new palette if it's the first frame.
        if (!UseGlobalColorTable || IsFirstFrame)
            CalculateColorTableSize();

        if (IsFirstFrame)
        {
            WriteLogicalScreenDescriptor(rect);

            //Global color table.
            if (UseGlobalColorTable)
                WritePalette();

            if (RepeatCount > -1)
                WriteApplicationExtension();
        }

        WriteGraphicControlExtension(delay, isLastFrame);
        WriteImageDescriptor(rect);

        IsFirstFrame = false;

        //Local color table.
        if (!UseGlobalColorTable)
            WritePalette();

        WriteImage();
    }

    #endregion

    #region Main methods

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
        WriteByte(UseFullTransparency ? FindTransparentColorIndex() : 0); //Background color index, 1 byte
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

    private void WriteGraphicControlExtension(int delay, bool isLastFrame)
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

        if (UseFullTransparency)
        {
            //If full "Transparency" is set:
            //All starting frames as "Restore Background".
            //The last frame as "Leave".

            if (isLastFrame)
            {
                //Leave.
                bitArray.Set(3, false);
                bitArray.Set(4, false);
                bitArray.Set(5, true);
            }
            else
            {
                //Restore background.
                bitArray.Set(3, false);
                bitArray.Set(4, true);
                bitArray.Set(5, false);
            }
        }
        else
        {
            //If "Detect Unchanged Pixels" is set:
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
        }

        #endregion

        //User Input Flag.
        bitArray.Set(6, false);

        //Transparent Color Flag, uses tranparency?
        bitArray.Set(7, (!IsFirstFrame || UseFullTransparency) && ColorTableHasTransparency);

        //Write the packed fields.
        WriteByte(ConvertToByte(bitArray));

        //Calculates the delay, taking into consideration overall rounding. Bug!
        //OrganicTime += delay;
        //delay = (int)Math.Round((OrganicTime > delay ? OrganicTime - AdjustedTime * 10 : delay) / 10.0f, MidpointRounding.AwayFromZero);
        //AdjustedTime += delay;
        //WriteShort(delay);

        WriteShort((int)Math.Round(delay / 10.0f, MidpointRounding.AwayFromZero));
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
        var encoder = new LegacyEncoder.LzwEncoder(0, 0, IndexedPixels, 8);
        encoder.Encode(InternalStream);
        //}
    }

    #endregion

    #region Helper methods

    private void ReadPixels(byte[] pixels)
    {
        if (QuantizationType == ColorQuantizationTypes.Neural)
        {
            #region Neural

            if (GlobalQuantizer == null || !UseGlobalColorTable)
            {
                GlobalQuantizer = new NeuralQuantizer(SamplingFactor, MaximumNumberColor)
                {
                    MaxColors = MaximumNumberColor,
                    TransparentColor = !IsFirstFrame || UseGlobalColorTable || UseFullTransparency ? TransparentColor : null
                };

                GlobalQuantizer.FirstPass(pixels);
                ColorTable = GlobalQuantizer.GetPalette();
            }

            //Indexes the pixels to the color table.
            IndexedPixels = GlobalQuantizer.SecondPass(pixels);
                
            #endregion
        }
        else if (QuantizationType == ColorQuantizationTypes.Octree)
        {
            #region Octree

            var quantizer = new OctreeQuantizer
            {
                MaxColors = MaximumNumberColor,
                TransparentColor = !IsFirstFrame || UseGlobalColorTable || UseFullTransparency ? TransparentColor : null
            };

            IndexedPixels = quantizer.Quantize(pixels);
            ColorTable = quantizer.ColorTable;

            #endregion
        }
        else if (QuantizationType == ColorQuantizationTypes.MedianCut)
        {
            #region Median cut

            if (GlobalQuantizer == null || !UseGlobalColorTable)
            {
                GlobalQuantizer = new MedianCutQuantizer
                {
                    MaxColors = MaximumNumberColor,
                    TransparentColor = !IsFirstFrame || UseGlobalColorTable || UseFullTransparency ? TransparentColor : null
                };

                GlobalQuantizer.FirstPass(pixels);
                ColorTable = GlobalQuantizer.GetPalette();
            }

            //Indexes the pixels to the color table.
            IndexedPixels = GlobalQuantizer.SecondPass(pixels);

            #endregion
        }
        else if (QuantizationType == ColorQuantizationTypes.Grayscale)
        {
            #region Grayscale

            //This quantizer uses a fixed palette (generated during object instantiation), so most calculations are called one time.
            if (GlobalQuantizer == null)
            {
                //Since the color table does not change among frames, it can be stored globally.
                UseGlobalColorTable = true;

                var transparent = !IsFirstFrame || UseGlobalColorTable || UseFullTransparency ? TransparentColor : null;

                GlobalQuantizer = new GrayscaleQuantizer(transparent, MaximumNumberColor)
                {
                    MaxColors = MaximumNumberColor,
                    TransparentColor = transparent
                };

                ColorTable = GlobalQuantizer.GetPalette();
            }
                
            //Each frame still needs to be quantized.
            IndexedPixels = GlobalQuantizer.SecondPass(pixels);

            #endregion
        }
        else if (QuantizationType == ColorQuantizationTypes.MostUsed)
        {
            #region Most used colors

            if (GlobalQuantizer == null || !UseGlobalColorTable)
            {
                GlobalQuantizer = new MostUsedQuantizer
                {
                    MaxColors = MaximumNumberColor,
                    TransparentColor = !IsFirstFrame || UseGlobalColorTable || UseFullTransparency ? TransparentColor : null
                };

                GlobalQuantizer.FirstPass(pixels);
                ColorTable = GlobalQuantizer.GetPalette();
            }

            //Indexes the pixels to the color table.
            IndexedPixels = GlobalQuantizer.SecondPass(pixels);

            #endregion
        }
        else
        {
            #region Palette

            //This quantizer uses a fixed palette (generated during object instantiation), so it will be only called once.
            if (GlobalQuantizer == null)
            {
                //Since the color table does not change among frames, it can be stored globally.
                UseGlobalColorTable = true;

                var transparent = !IsFirstFrame || UseGlobalColorTable || UseFullTransparency ? TransparentColor : null;

                //TODO: Pass the palette.
                //Default palettes: Windows, etc.
                //User submitted > Presets > Generate palette based on first frame.

                GlobalQuantizer = new PaletteQuantizer(new ArrayList()) 
                {
                    MaxColors = MaximumNumberColor,
                    TransparentColor = transparent
                };

                ColorTable = GlobalQuantizer.GetPalette();
            }

            //Each frame still needs to be quantized.
            IndexedPixels = GlobalQuantizer.SecondPass(pixels);

            #endregion
        }

        //I need to signal the other method that I'll need transparency.
        ColorTableHasTransparency = TransparentColor.HasValue && ColorTable.Contains(TransparentColor.Value);
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

    /// <summary>
    /// Writes the comment for the animation.
    /// </summary>
    /// <param name="comment">The comment to write to the gif.</param>
    private void WriteComment(string comment)
    {
        InternalStream.WriteByte(0x21);
        InternalStream.WriteByte(0xfe);

        //byte[] length = StringToByteArray(comment.Length.ToString("X"));

        //foreach (byte b in length)
        //    fs.WriteByte(b);

        var bytes = System.Text.Encoding.ASCII.GetBytes(comment);

        InternalStream.WriteByte((byte) bytes.Length);
        InternalStream.Write(bytes, 0, bytes.Length);
        InternalStream.WriteByte(0);
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
        //Logical Screen Description, Number of Colors, Byte length.
        //0 = 2 = 6
        //1 = 4 = 12
        //2 = 8 = 24
        //3 = 16 = 48
        //4 = 32 = 96
        //5 = 64 = 192
        //6 = 128 = 384
        //7 = 256 = 768
        //The inverse calculation is: 2^(N + 1) 
        //and x3 for the byte length.

        //If the colorsCount == 1, 
        //return zero instead of calculating it, because of the Log(0) call.
        //The "-1" assures that the count stays in range.
        ColorTableSize = ColorTable.Count > 1 ? (int)Math.Log(ColorTable.Count - 1, 2) : 0;
    }

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
        if (IsFirstFrame && !UseFullTransparency || !ColorTableHasTransparency) 
            return 0;

        //ReSharper disable once PossibleInvalidOperationException
        var index = ColorTable.IndexOf(TransparentColor.Value);

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
        //Add a comment section.
        WriteComment("Made with ScreenToGif");

        //Complete the file.
        WriteByte(0x3b);
        //Push data.
        InternalStream.Flush();
        //Resets the stream position to save afterwards.
        InternalStream.Position = 0;
    }
}