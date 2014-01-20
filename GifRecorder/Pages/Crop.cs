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
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;

namespace ScreenToGif.Pages
{
    public partial class Crop : Form
    {
        private Bitmap _bitmap;
        private Graphics g;
        Pen pen = new System.Drawing.Pen(Color.Blue, 2F);
        private Rectangle rectangle;
        private int posX, posY, width, height, posXmove, posYmove;
        private bool click;
        private bool accept;

        public Crop(Bitmap bitmap)
        {
            InitializeComponent();

            //This is Windows 8 window chrome values (16 sides and 39 top/bottom) We need to make a logic to support other versions of the OS
            this.Size = new Size(bitmap.Size.Width + 16, bitmap.Size.Height + 39);

            pictureCrop.Image = bitmap;
            _bitmap = new Bitmap(bitmap);
            
            g = pictureCrop.CreateGraphics();
        }

        public Rectangle Rectangle
        {
            get { return rectangle; }
            set { rectangle = value; }
        }

        public bool Accept
        {
            get { return accept; }
            set { accept = value; }
        }

        private void pictureCrop_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                posX = e.X;
                posY = e.Y;;

                posXmove = e.X;
                posYmove = e.Y;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graph = e.Graphics;
            graph.DrawImage(_bitmap, 0, 0);
            Rectangle rec = new Rectangle(posX, posY, width, height);
            graph.DrawRectangle(pen, rec);
        }

        private void pictureCrop_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (e.X > posX && e.Y > posY)
            {
                width = Math.Abs(e.X - posX);
                height = Math.Abs(e.Y - posY);
            }
            else if (e.X < posX && e.Y < posY)
            {
                width = Math.Abs(posX - e.X);
                height = Math.Abs(posY - e.Y);

                posX = e.X;
                posY = e.Y;
            }
            else if (e.X < posX && e.Y > posY) // top right to bottom left
            {
                width = Math.Abs(posX - e.X);
                height = Math.Abs(posY - e.Y);

                posX = e.X;
            }
            else if (e.X > posX && e.Y < posY)  // bottom left to top right
            {
                width = Math.Abs(posX - e.X);
                height = Math.Abs(posY - e.Y);

                posY = e.Y;
            }
            else if (e.X == posX && e.Y == posY)
            {
                this.Text = "Crop - No Selection";
                accept = false;
                g.DrawImage(_bitmap, 0, 0);
                return;
            }

            if (posX + width > pictureCrop.Size.Width)
                width = pictureCrop.Size.Width - posX;

            if (posY + height > pictureCrop.Size.Height)
                height = pictureCrop.Size.Height - posY;

            if (posX < 0)
            {
                width = width - Math.Abs(posX);
                posX = 0;
            }

            if (posY < 0)
            {
                height = height - Math.Abs(posY);
                posY = 0;
            }


            this.Text = "Crop " + width + "x" + height;

            g.DrawImage(_bitmap, 0, 0);
            Rectangle = new Rectangle(posX, posY, width, height);
            g = pictureCrop.CreateGraphics();
            g.DrawRectangle(pen, Rectangle);

            accept = true;
        }

        private void pictureCrop_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (e.X > posX && e.Y > posY)
                {
                    width = Math.Abs(e.X - posX);
                    height = Math.Abs(e.Y - posY);
                }
                else if (e.X < posX && e.Y < posY)
                {
                    width = Math.Abs(posX - e.X);
                    height = Math.Abs(posY - e.Y);

                    posXmove = e.X;
                    posYmove = e.Y;
                }
                else if (e.X < posX && e.Y > posY) // top right to bottom left
                {
                    width = Math.Abs(posX - e.X);
                    height = Math.Abs(posY - e.Y);

                    posXmove = e.X;
                }
                else if (e.X > posX && e.Y < posY) // bottom left to top right
                {
                    width = Math.Abs(posX - e.X);
                    height = Math.Abs(posY - e.Y);

                    posYmove = e.Y;
                }
                else
                {
                    return;
                }

                this.Text = "Crop " + width + "x" + height;
                g.DrawImage(_bitmap, 0, 0);
                Rectangle = new Rectangle(posXmove, posYmove, width, height);
                g = pictureCrop.CreateGraphics();
                g.DrawRectangle(pen, Rectangle);
            }
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (width > 5 || height > 5)
            {
                this.Close();
            }
            else
            {
                MessageBox.Show("Width and Height must be bigger than 5px each.", "Minimum size");
            }
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            accept = false;
            this.Close();
        }
    }
}
