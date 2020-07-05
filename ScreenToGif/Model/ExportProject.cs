using System.Collections.Generic;

namespace ScreenToGif.Model
{
    internal class ExportProject
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
        /// List of frames.
        /// </summary>
        public List<ExportFrame> Frames { get; set; } = new List<ExportFrame>();

        /// <summary>
        /// List of frames.
        /// </summary>
        public List<FrameInfo> FramesFiles { get; set; } = new List<FrameInfo>();

        /// <summary>
        /// Frame count.
        /// </summary>
        public int FrameCount => UsesFiles ? FramesFiles.Count : Frames.Count;
    }
}