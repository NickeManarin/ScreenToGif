using System.IO.Compression;
using System.Windows.Media;

namespace ScreenToGif.Util
{
    public class Parameters
    {
        public Export Type { get; set; }

        public string Filename { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public bool CopyToClipboard { get; set; }

        public CopyType CopyType { get; set; }

        public bool Upload { get; set; }

        public UploadService UploadDestination { get; set; }

        public bool ExecuteCommands { get; set; }

        public string PostCommands { get; set; }


        public int RepeatCount { get; set; }

        public bool DetectUnchangedPixels { get; set; }

        public Color? ChromaKey { get; set; }


        #region Gif

        public GifEncoderType EncoderType { get; set; }

        public ColorQuantizationType ColorQuantizationType { get; set; }

        /// <summary>
        /// When used with the gif encoder, 0 to 20.
        /// </summary>
        public int SamplingFactor { get; set; }

        public int MaximumNumberColors { get; set; }

        public bool UseGlobalColorTable { get; set; }

        /// <summary>
        /// When enabled, sets the selected color as the transparent color of the gif.
        /// Also sets the first frame as "RestoreBackground" and the last frame as "Leave".
        /// This option disables the "DetectUnchangedPixels" option, since it's conflicting.
        /// </summary>
        public bool EnableTransparency { get; set; }

        public Color? TransparencyColor { get; set; }
        
        #endregion

        #region Apng

        public ApngEncoderType ApngEncoder { get; set; }

        #endregion

        #region Video

        public VideoEncoderType VideoEncoder { get; set; }

        /// <summary>
        /// When used with the standalone Avi encoder, 0 to 10.000.
        /// </summary>
        public uint VideoQuality { get; set; }

        public string Command { get; set; }
        
        public string SecondPassCommand { get; set; }

        public string ExtraParameters { get; set; }

        /// <summary>
        /// True if the video should be flipped vertically.
        /// </summary>
        public bool FlipVideo { get; set; }

        public int Framerate { get; set; }

        #endregion

        #region Psd

        public bool Compress { get; set; }

        public bool SaveTimeline { get; set; }

        #endregion

        #region Project / Image

        public CompressionLevel CompressionLevel { get; set; }

        #endregion
    }
}