using System.IO;
using System.Text;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    /// <summary>
    /// The animation control chunk.
    /// </summary>
    internal class ActlChunk : Chunk
    {
        public uint NumFrames { get; private set; }

        public uint NumPlays { get; private set; }

        /// <summary>
        /// Attempts to read 16 bytes of the stream.
        /// </summary>
        internal static ActlChunk Read(Stream stream)
        {
            var chunk = new ActlChunk
            {
                Length = BitHelper.ConvertEndian(stream.ReadUInt32()), //Chunk length, 4 bytes.
                ChunkType = Encoding.ASCII.GetString(stream.ReadBytes(4)) //Chunk type, 4 bytes.
            };

            //If the second chunk is not the animation control (acTL), it means that this is a normal PNG.
            if (chunk.ChunkType != "acTL")
                return null;

            //var pos = stream.Position;
            //chunk.ChunkData = stream.ReadBytes(chunk.Length);
            //stream.Position = pos;

            //Chunk details + CRC, 8 bytes + 4 bytes.
            chunk.NumFrames = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.NumPlays = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.Crc = BitHelper.ConvertEndian(stream.ReadUInt32());

            return chunk;
        }

        internal new void Write(Stream stream)
        {
            stream.WriteUInt32(BitHelper.ConvertEndian(Length)); //4 bytes.
            stream.WriteBytes(Encoding.ASCII.GetBytes(ChunkType)); //4 bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(NumFrames)); //4 bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(NumPlays)); //4 bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(stream.PeekBytes(stream.Position - (Length + 4), (int)Length + 4)))); //CRC, 4 bytes.
        }
    }
}