using System;
using System.Drawing;
using System.Windows.Forms;
using ScreenToGif.Properties;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Form used to crop images.
    /// </summary>
    public partial class Crop : Form
    {
        #region Variables

        private readonly Bitmap _bitmap;
        private Graphics _g;
        readonly Pen _pen = new Pen(Color.Blue, 2F);
        private Rectangle _rectangle;
        private int _posX, _posY, _width, _height, _posXmove, _posYmove;

        /// <summary>
        /// The area to cut.
        /// </summary>
        public Rectangle Rectangle
        {
            get { return _rectangle; }
            set { _rectangle = value; }
        }

        #endregion

        /// <summary>
        /// The constructor of the Croping tool.
        /// </summary>
        /// <param name="bitmap">The example of bitmap.</param>
        public Crop(Bitmap bitmap)
        {
            InitializeComponent();

            #region Window Chrome Measurement

            //I had to use this logic, because not all versions of the OS have the same window chrome values. 
            //For instance, W8 have diffW = 16 (8+8), diffH = 39 (31+8)
            this.Size = new Size(100, 100);
            var panelSize = this.panel.Size;
            int diffW = Math.Abs(this.Size.Width - panelSize.Width);
            int diffH = Math.Abs(this.Size.Height - panelSize.Height);

            #endregion

            this.Size = new Size(bitmap.Size.Width + diffW, bitmap.Size.Height + diffH);

            pictureCrop.Size = bitmap.Size;
            pictureCrop.Image = bitmap;
            _bitmap = new Bitmap(bitmap);

            #region If image is smaller than 120x100

            if (bitmap.Size.Width < 320)
            {
                this.Size = new Size(320 + diffW, bitmap.Size.Height);
            }

            if (bitmap.Size.Height < 100)
            {
                this.Size = new Size(this.Size.Width, 100 + diffH);
            }

            #endregion

            _g = pictureCrop.CreateGraphics();

            #region Localize Labels

            this.Text = Resources.Title_CropNoSelection;

            #endregion

            pictureCrop.Focus();
        }

        private void pictureCrop_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _posX = e.X;
                _posY = e.Y; ;

                _posXmove = e.X;
                _posYmove = e.Y;
            }
        }

        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    base.OnPaint(e);

        //    Graphics graph = e.Graphics;
        //    graph.DrawImage(_bitmap, 0, 0);
        //    var rec = new Rectangle(_posX, _posY, _width, _height);
        //    graph.DrawRectangle(_pen, rec);
        //}

        private void pictureCrop_MouseUp(object sender, MouseEventArgs e)
        {
            //Quite a work with this thing huh...

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
            else if (e.X > _posX && e.Y < _posY)  //bottom left to top right
            {
                _width = Math.Abs(_posX - e.X);
                _height = Math.Abs(_posY - e.Y);

                _posY = e.Y;
            }
            else if (e.X == _posX && e.Y == _posY)
            {
                this.Text = Resources.Title_CropNoSelection;
                tbHeight.Text = "0"; 
                tbWidth.Text = "0";
                _width = 0;
                _height = 0;
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

            this.Text = Resources.Title_Crop;
            tbWidth.Text = _width.ToString();
            tbHeight.Text = _height.ToString();

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

                this.Text = Resources.Title_Crop;
                tbWidth.Text = _width.ToString();
                tbHeight.Text = _height.ToString();

                _g.DrawImage(_bitmap, 0, 0);
                Rectangle = new Rectangle(_posXmove, _posYmove, _width, _height);
                _g = pictureCrop.CreateGraphics();
                _g.DrawRectangle(_pen, Rectangle);
            }
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_width >= 10 && _height >= 10)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Width and Height must be bigger than 10px each.",
                    "Minimum size", MessageBoxButtons.OK, MessageBoxIcon.Information); //TODO: Localize.
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
