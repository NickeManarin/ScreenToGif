using System.IO;

namespace ScreenToGif.Util.Codification.Gif.Encoder;

internal class LzwEncoder
{
    /// <summary>
    /// Under GIF encoding up to 12bit, the maximum value is 4096
    /// </summary>
    protected static readonly int MaxStackSize = 4096;
    protected static readonly byte Nullcode = 0;

    public int ColorDepth { get; set; }

    public byte[] IndexedPixels { get; set; }

    private int InitDataSize { get; set; }

    public LzwEncoder(byte[] indexedPixel, int colorDepth)
    {
        IndexedPixels = indexedPixel;
        ColorDepth = Math.Max(2, colorDepth);
        InitDataSize = ColorDepth;
    }

    public void Encode(Stream internalStream)
    {
        #region Validation

        if (internalStream == null)
            throw new ArgumentNullException(nameof(internalStream), "You need to provide a stream.");

        #endregion

        //If it's the first step.
        var isFirst = true;

        var clearFlag = (1 << ColorDepth);
        var endOfFileFlag = clearFlag + 1;

        var codeTable = new Dictionary<string, int>();

        //Number of indexes of the currently processed bytes.
        var releaseCount = 0;

        var codeSize = (byte)(ColorDepth + 1);
        var availableCode = endOfFileFlag + 1;
        var maskCode = (1 << codeSize) - 1;

        var bitEncoder = new BitEncoder(codeSize);

        //Initial code size.
        internalStream.WriteByte((byte)ColorDepth);

        //First thing being added.
        bitEncoder.Add(clearFlag);

        int suffix = 0;

        while (releaseCount < IndexedPixels.Length)
        {
            #region If it's the first byte

            if (isFirst)
            {
                //The first time, the suffix is set to the first index bytes.
                suffix = IndexedPixels[releaseCount++];

                //If it's the last one.
                if (releaseCount == IndexedPixels.Length)
                {
                    bitEncoder.Add(suffix);
                    bitEncoder.Add(endOfFileFlag);
                    bitEncoder.End();

                    internalStream.WriteByte((byte)(bitEncoder.Length));
                    internalStream.Write(bitEncoder.OutList.ToArray(), 0, bitEncoder.Length);

                    bitEncoder.OutList.Clear();

                    break;
                }

                isFirst = false;
                continue;
            }

            #endregion

            #region Before and after the change, and the constituent entities
                
            var prefix = suffix;
            suffix = IndexedPixels[releaseCount++];
            string entry = $"{prefix},{suffix}";

            #endregion               

            #region If you do not know the current entity, entities encoded, and output the prefix

            if (!codeTable.ContainsKey(entry))
            {
                //If the current entity is not encoded, then output the prefix          
                bitEncoder.Add(prefix);

                //And the current entity is encoded. Inserts and after that adds the availableCode count.             
                codeTable.Add(entry, availableCode++);

                if (availableCode > (MaxStackSize - 3))
                {
                    //Insert the clear tag, reinvent
                    codeTable.Clear();
                    ColorDepth = InitDataSize;
                    codeSize = (byte)(ColorDepth + 1);
                    availableCode = endOfFileFlag + 1;
                    maskCode = (1 << codeSize) - 1;

                    bitEncoder.Add(clearFlag);
                    bitEncoder.InBit = codeSize;
                }
                else if (availableCode > (1 << codeSize))
                {
                    //If the current code is greater than the current code available to represent values
                    ColorDepth++;
                    codeSize = (byte)(ColorDepth + 1);
                    bitEncoder.InBit = codeSize;
                    maskCode = (1 << codeSize) - 1;
                }

                //Divides into more blocks.
                if (bitEncoder.Length >= 255)
                {
                    //Size of the block.
                    internalStream.WriteByte(255);
                    //Writes the 255 sized block.
                    internalStream.Write(bitEncoder.OutList.ToArray(), 0, 255);

                    if (bitEncoder.Length > 255)
                    {
                        var leftBuffer = new byte[bitEncoder.Length - 255];

                        //Removes the last written 255 bytes.
                        bitEncoder.OutList.CopyTo(255, leftBuffer, 0, leftBuffer.Length);
                        bitEncoder.OutList.Clear();
                        bitEncoder.OutList.AddRange(leftBuffer);
                    }
                    else
                    {
                        bitEncoder.OutList.Clear();
                    }
                }
            }

            #endregion

            #region If you know the current entity, set the suffixes to the current index value of an entity

            else
            {
                //Set the suffix to the current entity encoding
                suffix = codeTable[entry];
            }

            #endregion

            //if (releaseCount == 40240)
            //    suffix = suffix;

            #region To the end of an image, writes over identity, and outputs the current codes left in the data stream

            if (releaseCount == IndexedPixels.Length)
            {
                bitEncoder.Add(suffix); //Adds the last sufix.
                bitEncoder.Add(endOfFileFlag); //End of the LZW
                bitEncoder.End();

                //If the block size if greater than 255, divides into two.
                if (bitEncoder.Length > 255)
                {
                    var leftBuffer = new byte[bitEncoder.Length - 255];

                    bitEncoder.OutList.CopyTo(255, leftBuffer, 0, leftBuffer.Length);
                    bitEncoder.OutList.Clear();
                    bitEncoder.OutList.AddRange(leftBuffer);

                    internalStream.WriteByte((byte)leftBuffer.Length);
                    internalStream.Write(leftBuffer, 0, leftBuffer.Length);
                }
                else
                {
                    internalStream.WriteByte((byte)(bitEncoder.Length));
                    internalStream.Write(bitEncoder.OutList.ToArray(), 0, bitEncoder.Length);

                    bitEncoder.OutList.Clear();
                }

                break;
            }

            #endregion
        }

        //For 3 weeks I forgot to add this little piece of sh*t, my gifs were always corrupted...
        //Signals the end of the list of blocks.
        internalStream.WriteByte(0);
    }
}

//public class LZWEncoder
//{
//    #region Constants

//    /// <summary>
//    /// End of File.
//    /// </summary>
//    private const int Eof = -1;

//    /// <summary>
//    /// 80% occupancy.
//    /// </summary>
//    private const int HSize = 5003;

//    private readonly int[] _masks = { 0x0000, 0x0001, 0x0003, 0x0007, 0x000F, 0x001F, 0x003F, 0x007F, 0x00FF,
//        0x01FF, 0x03FF, 0x07FF, 0x0FFF, 0x1FFF, 0x3FFF, 0x7FFF, 0xFFFF };

//    #endregion

//    #region Properties

//    public Stream Stream { get; set; }

//    public byte[] IndexedPixels { get; set; }

//    public int ColorDepth { get; set; }

//    private int InitialCodeSize { get; set; }

//    private int ClearCode { get; set; }

//    private int EndOfFileCode { get; set; }

//    private int CurrentIndex { get; set; }

//    private bool ClearFlag { get; set; } = false;

//    private int ByteCountInPacket { get; set; } = 0;

//    #endregion

//    public LZWEncoder()
//    {
//        //Minimum of 2
//        InitialCodeSize = Math.Max(2, ColorDepth);
//    }

//    public LZWEncoder(Stream stream, byte[] indexedPixels, int colorDepth) : base()
//    {
//        Stream = stream;
//        IndexedPixels = indexedPixels;
//        Width = width;
//        Height = height;
//        ColorDepth = colorDepth;
//    }

//    #region Public Methods

//    /// <summary>
//    /// Encodes the indexed colors.
//    /// </summary>
//    /// <exception cref="EndOfStreamException">Stream is null</exception>
//    /// <exception cref="IOException">An I/O error occurs. </exception>
//    public void Encode()
//    {
//        if (Stream == null)
//            throw new EndOfStreamException("Stream is null");

//        Stream.WriteByte(Convert.ToByte(InitialCodeSize)); //Initial code size.

//        //Magic...
//        Compress();

//        Stream.WriteByte(0x00); //Terminator
//    }

//    #endregion

//    #region Private Methods

//    private void Compress()
//    {
//        //Number of bits per encoded code.
//        var codeSize = InitialCodeSize + 1;

//        var maxCode = MaxCode(codeSize);

//        //Shifts 0001 to the left
//        //Example: 0001 << 0111 (7): 1000 0000 (128)
//        //This basically gets:
//        //2 = 4, 3 = 8, 4 = 16, 5 = 32
//        //6 = 64, 7 = 128, 8 = 256
//        //Same as 2^InitialCodeSize
//        ClearCode = 1 << (InitialCodeSize);
//        EndOfFileCode = ClearCode + 1;
//        var nextAvailableCode = ClearCode + 2;

//        var current = NextPixel();

//        var hashingShift = 0;

//        //256*256=65536
//        for (int fCode = 0; fCode < 65536; fCode *= 2)
//        {
//            hashingShift++;
//        }



//        //Writes the last code.
//        Output(current);

//        //Writes the calculated End of the File code.
//        Output(EndOfFileCode);
//    }

//    /// <summary>
//    /// Calculates and returns the maximum possible code given the supplied
//    /// code size.
//    /// This is calculated as 2 to the power of the code size, minus one.
//    /// </summary>
//    /// <param name="codeSize">
//    /// Code size in bits.
//    /// </param>
//    /// <returns></returns>
//    private static int MaxCode(int codeSize)
//    {
//        //Same as (2^codeSize) - 1
//        //Shifts 0001 to the left (and subtracts)
//        //Example: 0001 << 0111 (7): 1000 0000 (128)
//        //This basically gets:
//        //3 = 7, 4 = 17, 5 = 31, 6 = 63
//        //7 = 127, 8 = 255, 9 = 511
//        return (1 << codeSize) - 1;
//        //CodeSize is InitialCodeSize + 1, that's why
//        //the range from 3 to 9
//    }

//    private int NextPixel()
//    {
//        if (CurrentIndex <= IndexedPixels.GetUpperBound(0))
//        {
//            return IndexedPixels[CurrentIndex++];
//        }

//        return Eof;
//    }

//    private void Output(int code)
//    {

//    }

//    #endregion
//}