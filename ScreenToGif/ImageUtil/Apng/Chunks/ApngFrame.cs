namespace ScreenToGif.ImageUtil.Apng.Chunks
{
    /// <summary>
    /// Frame that contains the image data and playback details.
    /// </summary>
    internal class ApngFrame
    {
        /// <summary>
        /// The image width.
        /// </summary>
        internal uint Width { get; set; }

        /// <summary>
        /// The image height.
        /// </summary>
        internal uint Height { get; set; }

        /// <summary>
        /// The left offset of the image.
        /// </summary>
        internal uint Left { get; set; }

        /// <summary>
        /// The top offset of the image.
        /// </summary>
        internal uint Top { get; set; }

        /// <summary>
        /// The color type of the image.
        /// PNG image type        • Colour type • Allowed bit depths • Interpretation
        /// Greyscale             • 0           • 1, 2, 4, 8, 16     • Each pixel is a greyscale sample
        /// Truecolour            • 2           • 8, 16              • Each pixel is an R,G,B triple
        /// Indexed-colour        • 3           • 1, 2, 4, 8         • Each pixel is a palette index; a PLTE chunk shall appear.
        /// Greyscale with alpha  • 4           • 8, 16              • Each pixel is a greyscale sample followed by an alpha sample.
        /// Truecolour with alpha • 6           • 8, 16              • Each pixel is an R,G,B triple followed by an alpha sample.
        /// </summary>
        internal byte ColorType { get; set; }

        /// <summary>
        /// The bit depth of the image.
        /// </summary>
        internal byte BitDepth { get; set; }

        /// <summary>
        /// The whole image data, including auxiliar chunks.
        /// </summary>
        internal byte[] ImageData { get; set; }

        /// <summary>
        /// The delay of the frame in miliseconds.
        /// </summary>
        internal int Delay { get; set; }

        ///<summary>
        ///Type of frame area disposal to be done after rendering this frame.
        ///</summary>
        internal Apng.DisposeOps DisposeOp { get; set; }

        ///<summary>
        ///Type of frame area rendering for this frame.
        ///</summary>
        internal Apng.BlendOps BlendOp { get; set; }
    }
}