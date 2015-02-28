using System.Drawing;
using Point = System.Windows.Point;

namespace ScreenToGif.Util
{
    /// <summary>
    /// The info of the cursor, position and image.
    /// </summary>
    public class CursorInfo
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="image">The Image of teh cursor.</param>
        /// <param name="position">The Position of the cursor.</param>
        /// <param name="clicked">True if clicked.</param>
        public CursorInfo(Bitmap image, Point position, bool clicked)
        {
            Image = image;
            Position = position;
            Clicked = clicked;
        }

        #endregion

        #region Auto Properties

        /// <summary>
        /// The position of the cursor.
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        /// The image of the icon.
        /// </summary>
        public Icon Icon { get; set; }

        /// <summary>
        /// The image of the icon as Bitmap.
        /// </summary>
        public Bitmap Image { get; set; }

        /// <summary>
        /// True if clicked.
        /// </summary>
        public bool Clicked { get; set; }

        #endregion
    }
}
