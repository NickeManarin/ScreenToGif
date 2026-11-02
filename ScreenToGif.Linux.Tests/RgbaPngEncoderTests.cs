using ScreenToGif.Linux.Services;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace ScreenToGif.Linux.Tests;

/// <summary>
/// Decodes what the encoder writes with an independent chunk reader, CRC and inflater, so the PNG
/// stays verifiable without a platform imaging stack and without a capture device.
/// </summary>
public sealed class RgbaPngEncoderTests
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void AnEncodedImageOpensWithThePngSignatureAnIhdrAndClosesWithAnIend()
    {
        var png = Encode(3, 2);

        Assert.Equal(Signature, png.Take(Signature.Length));
        var chunks = ReadChunks(png);
        var header = chunks[0];
        Assert.Equal("IHDR", header.Type);
        Assert.Equal(13, header.Data.Length);
        Assert.Equal(3, BinaryPrimitives.ReadInt32BigEndian(header.Data));
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(header.Data.AsSpan(4)));
        Assert.Equal(8, header.Data[8]);
        Assert.Equal(6, header.Data[9]);
        Assert.Contains(chunks, chunk => chunk.Type == "IDAT");
        Assert.Equal("IEND", chunks[^1].Type);
        Assert.Empty(chunks[^1].Data);
    }

    [Fact]
    public void EveryChunkCarriesTheCrcOfItsTypeAndData()
    {
        var chunks = ReadChunks(Encode(3, 2));

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.Equal(
            Crc32([.. Encoding.ASCII.GetBytes(chunk.Type), .. chunk.Data]),
            chunk.StoredCrc));
    }

    [Fact]
    public void BgraPixelsBecomeOpaqueRgbaPixelsInTheDecodedImage()
    {
        const int width = 3;
        const int height = 2;
        var bgra = PixelBuffer(width, height);
        using var stream = new MemoryStream();

        RgbaPngEncoder.WriteBgra(stream, width, height, bgra);

        var expected = new byte[width * height * 4];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            expected[(pixel * 4) + 0] = bgra[(pixel * 4) + 2];
            expected[(pixel * 4) + 1] = bgra[(pixel * 4) + 1];
            expected[(pixel * 4) + 2] = bgra[(pixel * 4) + 0];
            expected[(pixel * 4) + 3] = byte.MaxValue;
        }

        Assert.Equal(expected, DecodePixels(stream.ToArray(), width, height));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-4, 2)]
    [InlineData(2, -4)]
    public void NonPositiveDimensionsAreRejected(int width, int height)
    {
        using var stream = new MemoryStream();

        Assert.Throws<ArgumentOutOfRangeException>(() => RgbaPngEncoder.WriteBgra(stream, width, height, []));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(0)]
    public void ABufferThatDoesNotCoverEveryPixelIsRejected(int length)
    {
        using var stream = new MemoryStream();
        var bgra = new byte[length];

        Assert.Throws<ArgumentException>(() => RgbaPngEncoder.WriteBgra(stream, 2, 2, bgra));
    }

    [Fact]
    public void AMissingOutputStreamIsRejected()
    {
        var bgra = new byte[4];

        Assert.Throws<ArgumentNullException>(() => RgbaPngEncoder.WriteBgra(null!, 1, 1, bgra));
    }

    private static byte[] Encode(int width, int height)
    {
        using var stream = new MemoryStream();
        RgbaPngEncoder.WriteBgra(stream, width, height, PixelBuffer(width, height));
        return stream.ToArray();
    }

    private static byte[] PixelBuffer(int width, int height)
    {
        var bgra = new byte[width * height * 4];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            bgra[(pixel * 4) + 0] = (byte)(10 * (pixel + 1));
            bgra[(pixel * 4) + 1] = (byte)(20 * (pixel + 1));
            bgra[(pixel * 4) + 2] = (byte)(30 * (pixel + 1));
            bgra[(pixel * 4) + 3] = (byte)(pixel % 2 == 0 ? 0 : 77);
        }

        return bgra;
    }

    private static byte[] DecodePixels(byte[] png, int width, int height)
    {
        using var compressed = new MemoryStream();
        foreach (var chunk in ReadChunks(png).Where(chunk => chunk.Type == "IDAT"))
            compressed.Write(chunk.Data);
        compressed.Position = 0;

        var stride = width * 4;
        var raw = new byte[(stride + 1) * height];
        using (var inflate = new ZLibStream(compressed, CompressionMode.Decompress))
            inflate.ReadExactly(raw);

        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            Assert.Equal(0, raw[y * (stride + 1)]);
            raw.AsSpan((y * (stride + 1)) + 1, stride).CopyTo(pixels.AsSpan(y * stride));
        }

        return pixels;
    }

    private static IReadOnlyList<PngChunk> ReadChunks(byte[] png)
    {
        var chunks = new List<PngChunk>();
        var offset = Signature.Length;
        while (offset + 12 <= png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png.AsSpan(offset + 8, length).ToArray();
            var crc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + 8 + length, 4));
            chunks.Add(new PngChunk(type, data, crc));
            offset += 12 + length;
        }

        Assert.Equal(png.Length, offset);
        return chunks;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private sealed record PngChunk(string Type, byte[] Data, uint StoredCrc);
}
