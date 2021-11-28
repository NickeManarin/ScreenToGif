using ScreenToGif.Domain.Interfaces;

namespace ScreenToGif.Domain.Models;

public class ExportProject
{
    /// <summary>
    /// True if the project will be passed as files instead of byte array.
    /// </summary>
    public bool UsesFiles { get; set; }

    /// <summary>
    /// The path of frame chunk.
    /// </summary>
    public string ChunkPath { get; set; }

    /// <summary>
    /// The path of frame chunk that is used to hold the new frame data when cutting the images.
    /// </summary>
    public string NewChunkPath { get; set; }

    /// <summary>
    /// Path of the folder where the files are located.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// List of frames.
    /// </summary>
    public List<ExportFrame> Frames { get; set; } = new();

    /// <summary>
    /// List of frames.
    /// </summary>
    public List<IFrame> FramesFiles { get; set; } = new();

    /// <summary>
    /// Frame count.
    /// </summary>
    public int FrameCount => UsesFiles ? FramesFiles.Count : Frames.Count;
}