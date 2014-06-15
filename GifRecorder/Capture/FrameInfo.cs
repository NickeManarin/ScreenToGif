using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ScreenToGif.Capture
{
    public class FrameInfo
    {
        public FrameInfo(Bitmap bitmap, Point posUp, Point posDown, Size size)
        {
            Image = bitmap;
            PositionTopLeft = posUp;
            PositionBottomRight = posDown;
            FrameSize = size;
        }

        /// <summary>
        /// The frame image.
        /// </summary>
        public Bitmap Image { get; set; }

        /// <summary>
        /// The size of the frame, usually the same size of the animation. Except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Size FrameSize { get; set; }

        /// <summary>
        /// The frame position. Usually 0,0 except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Point PositionTopLeft { get; set; }

        /// <summary>
        /// The frame position. Usually the size of the image except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Point PositionBottomRight { get; set; }
    }
}
