using System.IO;
using System.IO.Compression;

namespace ScreenToGif.Util.Codification.Psd;

internal class ImageChannelData : IPsdContent
{
    /// <summary>
    /// 0 = Raw Data
    /// 1 = RLE compressed
    /// 2 = ZIP without prediction
    /// 3 = ZIP with prediction.
    /// </summary>
    internal ushort Encoding { get; set; }

    internal List<Channel> ChannelList { get; set; } = new();

    public long Length => Content?.Length ?? 0;

    public byte[] Content
    {
        get
        {
            using (var stream = new MemoryStream())
            {
                foreach (var channel in ChannelList)
                {
                    stream.WriteUInt16(BitHelper.ConvertEndian(Encoding)); //Encoding type, 2 bytes.
                    stream.WriteBytes(channel.Content); //Channel data, XX bytes.                        
                }

                return stream.ToArray();
            }
        }
    }

    public ImageChannelData(int depth, byte[] pixels, int rows, int columns, bool compress)
    {
        Encoding = (ushort)(compress ? 1 : 0);

        var alpha = new List<byte>();
        var blue = new List<byte>();
        var green = new List<byte>();
        var red = new List<byte>();

        if (depth == 32)
        {
            for (var i = 0; i < pixels.Length - 3; i += 4)
            {
                blue.Add(pixels[i]);        //Blue
                green.Add(pixels[i + 1]);   //Green
                red.Add(pixels[i + 2]);     //Red
                alpha.Add(pixels[i + 3]);   //Alpha
            }
        }
        else //24
        {
            for (var i = 0; i < pixels.Length - 2; i += 3)
            {
                blue.Add(pixels[i]);        //Blue
                green.Add(pixels[i + 1]);   //Green
                red.Add(pixels[i + 2]);     //Red
                alpha.Add(255);             //Alpha
            }
        }

        if (Encoding == 0)
        {
            ChannelList.Add(new Channel(alpha.ToArray()));
            ChannelList.Add(new Channel(red.ToArray()));
            ChannelList.Add(new Channel(green.ToArray()));
            ChannelList.Add(new Channel(blue.ToArray()));
        }
        else
        {
            ChannelList.Add(new Channel(RleCompression(alpha.ToArray(), rows, columns)));
            ChannelList.Add(new Channel(RleCompression(red.ToArray(), rows, columns)));
            ChannelList.Add(new Channel(RleCompression(green.ToArray(), rows, columns)));
            ChannelList.Add(new Channel(RleCompression(blue.ToArray(), rows, columns)));
        }
    }

    internal static byte[][] RleCompression(byte[] pixels, int rows, int columns)
    {
        var scanlines = new List<byte[]>();

        //For each scanline of this channel.
        for (var r = 0; r < rows; r++)
            scanlines.Add(Pack(SubArray(pixels, r * columns, columns)));
            
        return scanlines.ToArray();
    }

    public static byte[] Compress(byte[] buffer)
    {
        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
                zip.Close();
            }

            ms.Position = 0;

            compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);
        }

        //var gzBuffer = new byte[compressed.Length + 4];
        //Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
        //Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
        //return gzBuffer;

        return compressed;
    }

    public static byte[] Compress2(byte[] data)
    {
        using (var compressedStream = new MemoryStream())
        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return compressedStream.ToArray();
        }
    }

    public static byte[] Compress3(byte[] data)
    {
        byte[] compressArray = null;

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    deflateStream.Write(data, 0, data.Length);
                    deflateStream.Close(); //Must!
                }

                compressArray = memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            LogWriter.Log(ex, "Error while compressing the channel.");
        }

        return compressArray;
    }

    public static byte[] Decompress(byte[] gzBuffer)
    {
        byte[] buffer;
        using (var ms = new MemoryStream())
        {
            var msgLength = BitConverter.ToInt32(gzBuffer, 0);
            ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

            buffer = new byte[msgLength];

            ms.Position = 0;
            using (var zip = new GZipStream(ms, CompressionMode.Decompress))
                zip.Read(buffer, 0, buffer.Length);
        }

        return buffer;
    }

    /// <summary> 
    /// Packs the specified byte array using the packbits algorithm. 
    /// </summary> 
    /// <param name="source">The source byte[] to pack.</param> 
    /// <returns>A byte[] array that has been compressed.</returns> 
    public static byte[] Pack(byte[] source)
    {
        IList<byte> result = new List<byte>(source.Length);
        const int maxLength = 127;
        IList<byte> literals = new List<byte>(maxLength);

        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];

            if (i + 1 != source.Length)
            {
                var next = source[i + 1];

                if (next == current)
                {
                    AddLiterals(result, literals);

                    var max = i + maxLength >= source.Length ? source.Length - i - 1 : maxLength;
                    var hitMax = true;
                    byte runLength = 1;

                    for (var j = 2; j <= max; j++)
                    {
                        var run = source[i + j];

                        if (run != current)
                        {
                            hitMax = false;
                            var count = (byte)(0 - runLength);
                            i = i + j - 1;
                            result.Add(count);
                            result.Add(current);
                            break;
                        }

                        runLength++;
                    }

                    if (hitMax)
                    {
                        result.Add((byte)(sbyte)(0 - max));
                        result.Add(current);
                        i = i + max;
                    }
                }
                else
                {
                    literals.Add(current);

                    if (literals.Count == maxLength)
                        AddLiterals(result, literals);
                }
            }
            else
            {
                literals.Add(current);
                AddLiterals(result, literals);
            }
        }

        return result.ToArray();
    }

    /// <summary> 
    /// Adds the literal run to the result 
    /// </summary> 
    /// <param name="result">The result where the literals should be added.</param> 
    /// <param name="literals">The list of literals that will be appended to the result.</param> 
    private static void AddLiterals(IList<byte> result, IList<byte> literals)
    {
        if (literals.Count > 0)
        {
            result.Add((byte)(literals.Count - 1));

            foreach (var literal in literals)
                result.Add(literal);
        }

        literals.Clear();
    }

    public static T[] SubArray<T>(T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}