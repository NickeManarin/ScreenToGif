using System.IO;
using System.Text;
using ScreenToGif.Util;

namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    /// <summary>
    /// Generic chunk.
    /// </summary>
    internal class Chunk
    {
        #region Properties

        /// <summary>
        /// When reading a Apng, this sequence property remembers in which position the chunk was located.
        /// </summary>
        public int MasterSequence { get; set; }

        /// <summary>
        /// If this chunk holds frame details, it may be grouped with other chunk.
        /// </summary>
        public int FrameGroupId { get; set; } = -1;

        public uint Length { get; protected internal set; }

        public string ChunkType { get; protected internal set; }

        public byte[] ChunkData { get; protected internal set; }

        public uint Crc { get; protected internal set; }

        #endregion

        /// <summary>
        /// Attempts to read XX bytes of the stream.
        /// </summary>
        internal static Chunk Read(Stream stream, int sequence)
        {
            var chunk = new Chunk
            {
                MasterSequence = sequence,
                Length = BitHelper.ConvertEndian(stream.ReadUInt32()), //Chunk length, 4 bytes.
                ChunkType = Encoding.ASCII.GetString(stream.ReadBytes(4)) //Chunk type, 4 bytes.
            };

            //Chunk details + CRC, XX bytes + 4 bytes.
            chunk.ChunkData = stream.ReadBytes(chunk.Length);
            chunk.Crc = BitHelper.ConvertEndian(stream.ReadUInt32());
            
            return chunk;
        }

        internal void Write(Stream stream)
        {
            stream.WriteUInt32(BitHelper.ConvertEndian(Length)); //4 bytes.
            stream.WriteBytes(Encoding.ASCII.GetBytes(ChunkType)); //4 bytes.
            stream.WriteBytes(ChunkData); //XX bytes.
            stream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(stream.PeekBytes(stream.Position - (Length + 4), (int)Length + 4)))); //CRC, 4 bytes.
        }
    }
}