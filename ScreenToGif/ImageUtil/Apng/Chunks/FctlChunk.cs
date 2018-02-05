namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    internal enum DisposeOps
    {
        DisposeOpNone = 0,
        DisposeOpBackground = 1,
        DisposeOpPrevious = 2,
    }

    internal enum BlendOps
    {
        BlendOpSource = 0,
        BlendOpOver = 1,
    }

    internal class FctlChunk
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
        internal DisposeOps DisposeOp { get; private set; }

        ///<summary>
        ///Type of frame area rendering for this frame.
        ///</summary>
        internal BlendOps BlendOp { get; private set; }
    }
}