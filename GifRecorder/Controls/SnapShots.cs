using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenToGif.Controls
{
    /// <summary>
    /// The Snapshot control.
    /// </summary>
    public partial class SnapShots : ScrollableControl
    {
        public static List<Bitmap> images;
        private static int _count = 30;

        private bool _horizontalMode = true;
        private int _cumulativeHeight = 0;
        private int _cumulativeWidth = 0;
        private int _margin = 0;
        private int _verticalWidth = 0;
        private int _squareWidth = 70;
        private int _squareHeight = 55; //63
        private int _numSquares = 100;
        private float _zoom = 1.0f;

        /// <summary>
        /// True if the control should use the Horizontal orientation.
        /// </summary>
        public bool HorizontalMode
        {
            get { return _horizontalMode; }
            set
            {
                if (_horizontalMode != value)
                {
                    _horizontalMode = value;
                    Invalidate();
                }
            }
        }
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SnapShots()
        {
            InitializeComponent();

            this.AutoScroll = true;
        }

        /// <summary>
        /// Clears the internal list of images.
        /// </summary>
        public void Clear()
        {
            images = null;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if ((images == null) || (images.Count < 1))
                return;

            Graphics g = pe.Graphics;
            Brush theBrush = Brushes.Black;
            var flags = StringFormatFlags.MeasureTrailingSpaces;
            using (var sf = new StringFormat(flags))
            {
                sf.Alignment = StringAlignment.Center;

                base.OnPaint(pe);
                Point pt = AutoScrollPosition;
                g.TranslateTransform(pt.X, pt.Y);

                int startX = 0;
                int startY = 0;

                _cumulativeHeight = _squareHeight + (_margin * 2);
                _cumulativeWidth = 0;

                for (int index = 1; index < (_numSquares + 1); index++)
                {
                    if ((!_horizontalMode) && (startX > (_verticalWidth - 1)))
                    {
                        startX = 0;
                        startY += _squareHeight + _margin;
                        _cumulativeHeight += (_squareHeight + _margin);
                    }

                    var theRect = new Rectangle(_squareWidth * (startX++) + _margin, startY, _squareWidth, _squareHeight);
                    var textRect = new Rectangle(theRect.X + 50, theRect.Y + 50, 20, 20);

                    //Image imageBack = Image.FromFile(exePath + "/frame.gif");
                    //Image imageBack = Resources.Frame;
                    //g.DrawImage(imageBack, theRect);

                    var textRectInnner = new Rectangle(theRect.X, theRect.Y + 0, 68, 45); //theRect.Y + 9
                    int i = index - 1;
                    g.DrawImage(images[i], textRectInnner);

                    var borderPen = new Pen(theBrush);
                    //g.DrawRectangle(borderPen, theRect);
                    //g.DrawString(index.ToString(), this.Font, theBrush, textRect, sf);
                    _cumulativeWidth += _squareWidth + _margin;
                }

                AutoScrollMinSize = new Size(_cumulativeWidth, _cumulativeHeight);
            }

            AutoScrollMinSize = new Size(_cumulativeWidth, _cumulativeHeight);
        }

        protected int GetSquareID(Point location)
        {
            int returnID = -1;

            if (_horizontalMode)
            {
                int curXPos = Math.Abs(location.X);
                if (curXPos == 0)
                    return 0;

                int cellWidth = _squareWidth;

                returnID = (curXPos / cellWidth);
            }
            else
            {
                int cellWidth = _squareWidth;
                int cellHeight = _squareHeight + _margin;

                int curXPos = Math.Abs(location.X);
                int curYPos = Math.Abs(location.Y);
                int curCol = (curXPos != 0) ? (curXPos / cellWidth) : 0;
                int curRow = (curYPos != 0) ? (curYPos / cellHeight) : 0;

                if (curCol >= _verticalWidth)
                    return -1;

                returnID = curCol + (curRow * _verticalWidth);
            }
            if (++returnID <= _numSquares)
                return returnID;
            else
                return -1;
        }

        protected Point GetVirtualMouseLocation(MouseEventArgs e)
        {
            using (var mx = new Matrix(_zoom, 0, 0, _zoom, 0, 0))
            {
                mx.Translate(this.AutoScrollPosition.X * (1.0f / _zoom), this.AutoScrollPosition.Y * (1.0f / _zoom));
                mx.Invert();
                var pa = new Point[] { new Point(e.X, e.Y) };
                mx.TransformPoints(pa);
                return pa[0];
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Point mouseLocation = GetVirtualMouseLocation(e);
            int squareID = GetSquareID(mouseLocation);

            if (squareID > 0)
            {
                //int index = Int16.Parse(((PictureBox)sender).Name);
                int index = squareID - 1;

                //// Use a delegate here which is probably better instead of calling Parent
                //if ((((MainForm)this.Parent).mediaManager.currentMedia != null) && (((MainForm)this.Parent).images != null))
                //    ((MainForm)this.Parent).mediaManager.currentMedia.setPosition((((MainForm)this.Parent).images[index]).time);
            }
        }

        public void LoadFrames(List<Bitmap> list)
        {
            if ((list == null) || (list.Count < 1))
                return;

            images = list;
            _count = list.Count;
            _numSquares = _count;
            Invalidate();
        }
    }
}
