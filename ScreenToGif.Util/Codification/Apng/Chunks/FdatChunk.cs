using System.IO;
using System.Text;

namespace ScreenToGif.Util.Codification.Apng.Chunks;

/// <summary>
/// The frame data chunk.
/// </summary>
internal class FdatChunk : Chunk
{
    ///<summary>
    ///Sequence number of the animation chunk, starting from 0.
    ///</summary>
    internal uint SequenceNumber { get; private set; }

    /// <summary>
    /// The image data.
    /// </summary>
    internal byte[] FrameData { get; private set; }

    /// <summary>
    /// Attempts to read XX bytes of the stream.
    /// </summary>
    internal static FdatChunk Read(uint length, byte[] array)
    {
        var chunk = new FdatChunk
        {
            Length = length, //Chunk length, 4 bytes.
            ChunkType = "fdAT" //Chunk type, 4 bytes.
        };

        using (var stream = new MemoryStream(array))
        {
            //Chunk details, 4 bytes + XX bytes.
            chunk.SequenceNumber = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.FrameData = stream.ReadBytes(length - 4); //Minus 4 because that's the size of the sequence number.
        }

        return chunk;
    }

    internal void Write(Stream stream, bool writeAsIdat = true)
    {
        stream.WriteUInt32(BitHelper.ConvertEndian(Length)); //4 bytes.
        stream.WriteBytes(Encoding.ASCII.GetBytes(writeAsIdat ? "IDAT" : ChunkType)); //4 bytes.

        if (!writeAsIdat)
            stream.WriteUInt32(BitHelper.ConvertEndian(SequenceNumber)); //4 bytes.

        stream.WriteBytes(FrameData); //XX bytes.
        stream.WriteUInt32(BitHelper.ConvertEndian(CrcHelper.Calculate(stream.PeekBytes(stream.Position - (Length + (writeAsIdat ? 4 : 8)), (int)Length + (writeAsIdat ? 4 : 8))))); //CRC, 4 bytes.
    }
}