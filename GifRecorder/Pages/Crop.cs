using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Media;
using ScreenToGif.Properties;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace ScreenToGif.Pages
{
    public partial class Crop : Form
    {
        private Bitmap _bitmap;
        private Graphics _g;
        readonly Pen _pen = new System.Drawing.Pen(Color.Blue, 2F);
        private Rectangle _rectangle;
        private int _posX, _posY, _width, _height, _posXmove, _posYmove;

        /// <summary>
        /// The constructor of the Croping tool.
        /// </summary>
        /// <param name="bitmap">The example of bitmap.</param>
        public Crop(Bitmap bitmap)
        {
            InitializeComponent();

            //I had to use this logic, because not all versions of the OS have the same window chrome values. For instance, W8 have diffW = 16 (8+8), diffH = 39 (31+8)
            this.Size = new Size(100, 100);
            var SizepictureBox = this.pictureCrop.Size;
            int diffW = (this.Size.Width - SizepictureBox.Width);
            int diffH = (this.Size.Height - SizepictureBox.Height);

            this.Size = new Size(bitmap.Size.Width + diffW, bitmap.Size.Height + diffH);

            pictureCrop.Image = bitmap;
            _bitmap = new Bitmap(bitmap);
            
            _g = pictureCrop.CreateGraphics();
        }

        /// <summary>
        /// The new size.
        /// </summary>
        public Rectangle Rectangle
        {
            get { return _rectangle; }
            set { _rectangle = value; }
        }

        private void pictureCrop_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _posX = e.X;
                _posY = e.Y;;

                _posXmove = e.X;
                _posYmove = e.Y;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graph = e.Graphics;
            graph.DrawImage(_bitmap, 0, 0);
            Rectangle rec = new Rectangle(_posX, _posY, _width, _height);
            graph.DrawRectangle(_pen, rec);
        }

        private void pictureCrop_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (e.X > _posX && e.Y > _posY)
            {
                _width = Math.Abs(e.X - _posX);
                _height = Math.Abs(e.Y - _posY);
            }
            else if (e.X < _posX && e.Y < _posY)
            {
                _width = Math.Abs(_posX - e.X);
                _height = Math.Abs(_posY - e.Y);

                _posX = e.X;
                _posY = e.Y;
            }
            else if (e.X < _posX && e.Y > _posY) // top right to bottom left
            {
                _width = Math.Abs(_posX - e.X);
                _height = Math.Abs(_posY - e.Y);

                _posX = e.X;
            }
            else if (e.X > _posX && e.Y < _posY)  // bottom left to top right
            {
                _width = Math.Abs(_posX - e.X);
                _height = Math.Abs(_posY - e.Y);

                _posY = e.Y;
            }
            else if (e.X == _posX && e.Y == _posY)
            {
                this.Text = Resources.Title_CropNoSelection;
                _g.DrawImage(_bitmap, 0, 0);
                return;
            }

            if (_posX + _width > pictureCrop.Size.Width)
                _width = pictureCrop.Size.Width - _posX;

            if (_posY + _height > pictureCrop.Size.Height)
                _height = pictureCrop.Size.Height - _posY;

            if (_posX < 0)
            {
                _width = _width - Math.Abs(_posX);
                _posX = 0;
            }

            if (_posY < 0)
            {
                _height = _height - Math.Abs(_posY);
                _posY = 0;
            }


            this.Text = Resources.Title_Crop + " " + _width + "x" + _height;

            _g.DrawImage(_bitmap, 0, 0);
            Rectangle = new Rectangle(_posX, _posY, _width, _height);
            _g = pictureCrop.CreateGraphics();
            _g.DrawRectangle(_pen, Rectangle);
        }

        private void pictureCrop_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.X > _posX && e.Y > _posY)
                {
                    _width = Math.Abs(e.X - _posX);
                    _height = Math.Abs(e.Y - _posY);
                }
                else if (e.X < _posX && e.Y < _posY)
                {
                    _width = Math.Abs(_posX - e.X);
                    _height = Math.Abs(_posY - e.Y);

                    _posXmove = e.X;
                    _posYmove = e.Y;
                }
                else if (e.X < _posX && e.Y > _posY) // top right to bottom left
                {
                    _width = Math.Abs(_posX - e.X);
                    _height = Math.Abs(_posY - e.Y);

                    _posXmove = e.X;
                }
                else if (e.X > _posX && e.Y < _posY) // bottom left to top right
                {
                    _width = Math.Abs(_posX - e.X);
                    _height = Math.Abs(_posY - e.Y);

                    _posYmove = e.Y;
                }
                else
                {
                    return;
                }

                this.Text = Resources.Title_Crop + " " + _width + "x" + _height;
                _g.DrawImage(_bitmap, 0, 0);
                Rectangle = new Rectangle(_posXmove, _posYmove, _width, _height);
                _g = pictureCrop.CreateGraphics();
                _g.DrawRectangle(_pen, Rectangle);
            }
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_width > 10 && _height > 10)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Width and Height must be bigger than 10px each.", "Minimum size");
            }
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void Crop_Shown(object sender, EventArgs e)
        {
            toolHelp.Show(Resources.Tooltip_Crop, this, 2000);
        }
    }
}
