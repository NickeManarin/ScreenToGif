using System;
using System.IO;
using System.Text;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    /// <summary>
    /// The image header chunk.
    /// </summary>
    internal class IhdrChunk : Chunk
    {
        internal uint Width { get; private set; }

        internal uint Height { get; private set; }

        internal byte BitDepth { get; private set; }

        internal byte ColorType { get; private set; }

        internal byte CompressionMethod { get; private set; }

        internal byte FilterMethod { get; private set; }

        internal byte InterlaceMethod { get; private set; }
        
        /// <summary>
        /// Attempts to read 25 bytes of the stream.
        /// </summary>
        internal static IhdrChunk Read(Stream stream)
        {
            var chunk = new IhdrChunk
            {
                Length = BitHelper.ConvertEndian(stream.ReadUInt32()), //Chunk length, 4 bytes.
                ChunkType = Encoding.ASCII.GetString(stream.ReadBytes(4)) //Chunk type, 4 bytes.
            };

            if (chunk.ChunkType != "IHDR")
                throw new Exception("Missing IHDR chunk.");

            //var pos = stream.Position;
            //chunk.ChunkData = stream.ReadBytes(chunk.Length);
            //stream.Position = pos;

            //Chunk details + CRC, 13 bytes + 4 bytes.
            chunk.Width = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.Height = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.BitDepth = (byte) stream.ReadByte();
            chunk.ColorType = (byte) stream.ReadByte();
            chunk.CompressionMethod = (byte) stream.ReadByte();
            chunk.FilterMethod = (byte) stream.ReadByte();
            chunk.InterlaceMethod = (byte) stream.ReadByte();
            chunk.Crc = BitHelper.ConvertEndian(stream.ReadUInt32());

            return chunk;
        }

        /// <summary>
        /// Write the IHDR chunk to the stream.
        /// If a custom size is given, that's what is written.
        /// </summary>
        internal void Write(Stream stream, uint? width = null, uint? height = null)
        {
            stream.WriteUInt32(BitHelper.ConvertEndian(Length)); //4 bytes.
            stream.WriteBytes(Encoding.ASCII.GetBytes(ChunkType)); //4 bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(width ?? Width)); //4 bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(height ?? Height)); //4 bytes.
            stream.WriteByte(BitDepth); //1 byte.
            stream.WriteByte(ColorType); //1 byte.
            stream.WriteByte(CompressionMethod); //1 byte.
            stream.WriteByte(FilterMethod); //1 byte.
            stream.WriteByte(InterlaceMethod); //1 byte.
            stream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(stream.PeekBytes(stream.Position - (Length + 4), (int)Length + 4)))); //CRC, 4 bytes.
        }
    }
}