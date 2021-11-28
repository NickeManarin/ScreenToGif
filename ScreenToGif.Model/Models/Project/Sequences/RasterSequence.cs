using ScreenToGif.Domain.Models.Project.Sequences.SubSequences;

namespace ScreenToGif.Domain.Models.Project.Sequences;

public class RasterSequence : SizeableSequence
{
    /// <summary>
    /// Origin of the raster frames.
    /// It could be from capture (screen or webcam), media import (gif, apng, image or video) or rasterization of other sequences.
    /// </summary>
    public string Origin { get; set; }

    /// <summary>
    /// The bit depth of the raster images.
    /// Usually 24 or 32 bits.
    /// </summary>
    public int BitDepth { get; set; }

    public List<Frame> Frames { get; set; }


    public RasterSequence()
    {
        Type = Types.Raster;
    }
}