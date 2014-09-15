using System;
using System.Drawing;
using System.Windows.Forms;
using Rectangle = System.Drawing.Rectangle;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// Panel able to receive paint events.
    /// </summary>
    public class FreeDrawPanel : Panel
    {
        /// <summary>
        /// The image painted by the user.
        /// </summary>
        public Bitmap CachedBitmap { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawCachedImage(e.Graphics);
        }

        /// <summary>
        /// Paint as Color.Transparent the selected area.
        /// </summary>
        /// <param name="e">A MouseEventsArgs object</param>
        /// <param name="eraserSize">The Size of the eraser.</param>
        public void EraseEvent(MouseEventArgs e, int eraserSize)
        {
            InitializeCachedImage();

            var point = new Point(e.X - (eraserSize / 2), e.Y - (eraserSize / 2));
            if (point.X < 0) point.X = 0;
            if (point.Y < 0) point.Y = 0;

            var section = new Rectangle(point, new Size(eraserSize, eraserSize));

            for (int i = point.X; i <= section.Right; ++i)
            {
                for (int j = point.Y; j <= section.Bottom; ++j)
                {
                    if (CachedBitmap.Height - 1 >= j && CachedBitmap.Width - 1 >= i)
                        CachedBitmap.SetPixel(i, j, Color.Transparent);
                }
            }

            Invalidate(section);
            DrawCachedImage(CreateGraphics());
        }

        /// <summary>
        /// Applies the Action to the cached image.
        /// </summary>
        /// <param name="drawAction"></param>
        public void Draw(Action<Graphics> drawAction)
        {
            InitializeCachedImage();

            using (Graphics g = Graphics.FromImage(CachedBitmap))
            {
                drawAction(g);
            }

            DrawCachedImage(CreateGraphics());
        }

        /// <summary>
        /// Clears the cached image.
        /// </summary>
        public void EraseAll()
        {
            CachedBitmap = new Bitmap(Width, Height);

            Refresh();
        }

        /// <summary>
        /// Draws the cached image with current Graphics.
        /// </summary>
        /// <param name="graphics">The current graphics to be applied.</param>
        public void DrawCachedImage(Graphics graphics)
        {
            InitializeCachedImage();

            graphics.DrawImage(CachedBitmap, 0, 0, Width, Height);
        }

        private void InitializeCachedImage()
        {
            if (CachedBitmap == null)
                CachedBitmap = new Bitmap(Width, Height);
        }
    }
}
