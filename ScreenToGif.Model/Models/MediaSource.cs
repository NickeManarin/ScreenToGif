using System.Diagnostics;

namespace ScreenToGif.Domain.Models;

[DebuggerDisplay("Resolution = {Width}x{Height}, Framerate: {Framerate}, Format: {Format}")]
public class MediaSource
{
    public int StreamIndex { get; set; }

    public int MediaIndex { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public double Framerate { get; set; }

    public string Format { get; set; }
}