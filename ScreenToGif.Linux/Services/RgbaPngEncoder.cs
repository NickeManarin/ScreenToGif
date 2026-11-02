using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace ScreenToGif.Linux.Services;

/// <summary>
/// Writes opaque 8-bit RGBA PNG files from BGRA pixel buffers without a platform imaging stack,
/// matching the editor's normalized RGBA frame format.
/// </summary>
public static class RgbaPngEncoder
{
    private const int BytesPerPixel = 4;
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static void WriteBgra(Stream output, int width, int height, ReadOnlySpan<byte> bgra)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "PNG dimensions must be positive.");
        if (bgra.Length != (long)width * height * BytesPerPixel)
            throw new ArgumentException($"Expected {(long)width * height * BytesPerPixel} BGRA bytes but received {bgra.Length}.", nameof(bgra));

        output.Write(Signature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8; // bit depth
        header[9] = 6; // color type: truecolor with alpha
        header[10] = 0; // deflate
        header[11] = 0; // adaptive filtering
        header[12] = 0; // no interlace
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", CompressScanlines(width, height, bgra));
        WriteChunk(output, "IEND", []);
    }

    private static byte[] CompressScanlines(int width, int height, ReadOnlySpan<byte> bgra)
    {
        var stride = width * BytesPerPixel;
        var row = new byte[1 + stride];
        using var buffer = new MemoryStream(stride * height / 4);
        using (var deflate = new ZLibStream(buffer, CompressionLevel.Fastest, leaveOpen: true))
        {
            for (var y = 0; y < height; y++)
            {
                var source = bgra.Slice(y * stride, stride);
                row[0] = 0; // filter: none
                for (var x = 0; x < stride; x += BytesPerPixel)
                {
                    row[1 + x] = source[x + 2];
                    row[2 + x] = source[x + 1];
                    row[3 + x] = source[x];
                    row[4 + x] = byte.MaxValue;
                }

                deflate.Write(row);
            }
        }

        return buffer.ToArray();
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        Span<byte> typeBytes = stackalloc byte[4];
        Encoding.ASCII.GetBytes(type, typeBytes);
        output.Write(typeBytes);
        output.Write(data);

        var crc = UpdateCrc(0xFFFFFFFF, typeBytes);
        crc = UpdateCrc(crc, data) ^ 0xFFFFFFFF;
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < table.Length; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }

        return table;
    }
}
