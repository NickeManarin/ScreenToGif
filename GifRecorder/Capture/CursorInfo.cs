using System.Drawing;

namespace ScreenToGif.Capture
{
    /// <summary>
    /// The info of the cursor, position and image.
    /// </summary>
    public class CursorInfo
    {
        #region Variables

        private Point _position;
        private Icon _icon;

        #endregion

        #region Getters and Setters

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

        #endregion
    }
}
