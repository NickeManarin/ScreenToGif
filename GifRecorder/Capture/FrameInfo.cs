using System.Drawing;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// Frame info class.
    /// </summary>
    public class FrameInfo
    {
        #region Constructor

        /// <summary>
        /// Initialises a FrameInfo instance.
        /// </summary>
        /// <param name="bitmap">The Bitmap.</param>
        /// <param name="posUp">The TopLeft point.</param>
        public FrameInfo(string bitmap, Point posUp)
        {
            Image = bitmap;
            PositionTopLeft = posUp;
        }

        #endregion

        #region Auto Properties

        /// <summary>
        /// The frame image full path.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// The frame position. Usually 0,0 except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Point PositionTopLeft { get; set; }

        /// <summary>
        /// The delay of the frame.
        /// </summary>
        public int Delay { get; set; }

        #endregion
    }
}
