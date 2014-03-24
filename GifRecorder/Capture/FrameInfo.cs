using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ScreenToGif.Capture
{
    public class FrameInfo
    {
        private Bitmap _image;
        private Point _positionTopLeft;
        private Point _positionBottomRight;
        private Size _size;

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
        public Bitmap Image
        {
            get { return _image; }
            set { _image = value; }
        }

        /// <summary>
        /// The size of the frame, usually the same size of the animation. Except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Size FrameSize
        {
            get { return _size; }
            set { _size = value; }
        }

        /// <summary>
        /// The frame position. Usually 0,0 except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Point PositionTopLeft
        {
            get { return _positionTopLeft; }
            set { _positionTopLeft = value; }
        }

        /// <summary>
        /// The frame position. Usually the size of the image except when "analyze unchanged pixels" is set to true.
        /// </summary>
        public Point PositionBottomRight
        {
            get { return _positionBottomRight; }
            set { _positionBottomRight = value; }
        }
    }
}
