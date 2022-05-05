namespace ScreenToGif.Domain.Models.Project.Sequences.SubSequences;

public class Frame
{
    /// <summary>
    /// The time, in milliseconds that the frame should be visible.
    /// </summary>
    public uint Delay { get; set; }

    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    internal byte[] Pixels { get; set; }
}