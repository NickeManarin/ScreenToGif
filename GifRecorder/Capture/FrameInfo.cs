using System.Drawing;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// Frame info class.
    /// </summary>
    public class FrameInfo
    {
        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="posUp">The TopLeft point.</param>
        public FrameInfo(string bitmap, Point posUp)
        {
            Image = bitmap;
            PositionTopLeft = posUp;
            //PositionBottomRight = posDown;
            //FrameSize = size;
        }

        /// <summary>
        /// The frame image full path.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// The frame position. Usually 0,0 except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Point PositionTopLeft { get; set; }

        ///// <summary>
        ///// The size of the frame, usually the same size of the animation. Except when "analyze unchanged pixels" is set to true.
        ///// </summary>
        //public Size FrameSize { get; set; }

        ///// <summary>
        ///// The frame position. Usually the size of the image except when "analyze unchanged pixels" is set to true.
        ///// </summary>
        //public Point PositionBottomRight { get; set; }
    }
}
