namespace ScreenToGif.Util.Parameters
{
    public class VideoParameters : Parameters
    {
        public VideoEncoderType VideoEncoder { get; set; }

        public string Command { get; set; }

        /// <summary>
        /// Used with the standalone Avi encoder, 0 to 10.000.
        /// </summary>
        public uint Quality { get; set; }
    }
}
