using System;
using System.Drawing;
using Point = System.Windows.Point;

namespace ScreenToGif.Util
{
    /// <summary>
    /// The info of the cursor, position and image.
    /// </summary>
    [Serializable]
    public class CursorInfo
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="image">The Image of teh cursor.</param>
        /// <param name="position">The Position of the cursor.</param>
        /// <param name="clicked">True if clicked.</param>
        /// <param name="dpi">The screen dots per inches</param>
        public CursorInfo(Bitmap image, Point position, bool clicked, double dpi = 1)
        {
            Image = image;

            if (Math.Abs(dpi - 1.0) > 0)
            {
                position.X = position.X*dpi;
                position.Y = position.Y*dpi;
            }

            Position = position;
            Clicked = clicked;
        }

        #endregion

        #region Auto Properties

        /// <summary>
        /// The position of the cursor.
        /// </summary>
        public Point Position { get; set; }

        ///// <summary>
        ///// The image of the icon.
        ///// </summary>
        //public Icon Icon { get; set; }

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
