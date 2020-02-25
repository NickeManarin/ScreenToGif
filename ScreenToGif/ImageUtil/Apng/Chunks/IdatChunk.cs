using System.IO;
using System.Text;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    /// <summary>
    /// The image data chunk.
    /// </summary>
    internal class IdatChunk : Chunk
    {
        /// <summary>
        /// The image data.
        /// </summary>
        internal byte[] FrameData { get; private set; }

        /// <summary>
        /// Attempts to read XX bytes of the stream.
        /// </summary>
        internal static IdatChunk Read(uint length, byte[] array)
        {
            var chunk = new IdatChunk
            {
                Length = length, //Chunk length, 4 bytes.
                ChunkType = "IDAT" //Chunk type, 4 bytes.
            };

            using (var stream = new MemoryStream(array))
            {
                //Chunk details, XX bytes.
                chunk.FrameData = stream.ReadBytes(length); // - 4
            }

            return chunk;
        }

        internal new void Write(Stream stream)
        {
            stream.WriteUInt32(BitHelper.ConvertEndian(Length)); //4 bytes.
            stream.WriteBytes(Encoding.ASCII.GetBytes(ChunkType)); //4 bytes.
            stream.WriteBytes(FrameData); //XX bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(stream.PeekBytes(stream.Position - (Length + 4), (int)Length + 4)))); //CRC, 4 bytes.
        }
    }
}