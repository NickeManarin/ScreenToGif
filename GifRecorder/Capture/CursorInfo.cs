using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// The info of the cursor, position and image.
    /// </summary>
    public class CursorInfo
    {
        private Point _position;
        private Icon _icon;

        /// <summary>
        /// The position of the cursor.
        /// </summary>
        public Point Position
        {
            get { return _position; }
            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// The image of the icon.
        /// </summary>
        public Icon Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }
    }
}
