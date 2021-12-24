using System.IO;

namespace ScreenToGif.Util.Codification.Apng.Chunks;

/// <summary>
/// The frame control chunk.
/// </summary>
internal class FctlChunk : Chunk
{
    ///<summary>
    ///Sequence number of the animation chunk, starting from 0.
    ///</summary>
    internal uint SequenceNumber { get; private set; }

    ///<summary>
    ///Width of the following frame.
    ///</summary>
    internal uint Width { get; private set; }

    ///<summary>
    ///Height of the following frame.
    ///</summary>
    internal uint Height { get; private set; }

    ///<summary>
    ///X position at which to render the following frame.
    ///</summary>
    internal uint XOffset { get; private set; }

    ///<summary>
    ///Y position at which to render the following frame.
    ///</summary>
    internal uint YOffset { get; private set; }

    ///<summary>
    ///Frame delay fraction numerator.
    ///</summary>
    internal ushort DelayNum { get; private set; }

    ///<summary>
    ///Frame delay fraction denominator.
    ///</summary>
    internal ushort DelayDen { get; private set; }

    ///<summary>
    ///Type of frame area disposal to be done after rendering this frame.
    ///</summary>
    internal Apng.DisposeOps DisposeOp { get; private set; }

    ///<summary>
    ///Type of frame area rendering for this frame.
    ///</summary>
    internal Apng.BlendOps BlendOp { get; private set; }


    /// <summary>
    /// Attempts to read 26 bytes of the stream.
    /// </summary>
    internal static FctlChunk Read(uint length, byte[] array)
    {
        var chunk = new FctlChunk
        {
            Length = length, //Chunk length, 4 bytes.
            ChunkType = "fcTL" //Chunk type, 4 bytes.
        };

        using (var stream = new MemoryStream(array))
        {
            //Chunk details, 26 bytes.
            chunk.SequenceNumber = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.Width = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.Height = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.XOffset = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.YOffset = BitHelper.ConvertEndian(stream.ReadUInt32());
            chunk.DelayNum = BitHelper.ConvertEndian(stream.ReadUInt16());
            chunk.DelayDen = BitHelper.ConvertEndian(stream.ReadUInt16());
            chunk.DisposeOp = (Apng.DisposeOps)stream.ReadByte();
            chunk.BlendOp = (Apng.BlendOps)stream.ReadByte();
        }

        return chunk;
    }
}