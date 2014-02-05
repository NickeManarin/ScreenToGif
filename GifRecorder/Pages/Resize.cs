using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ScreenToGif.Pages
{
    public partial class Resize : Form
    {
        private bool freeAspectRatio;
        private Size initialSize;

        //double so division keeps decimal points
        double widthRatio = 16;
        double heightRatio = 9;

        const int WM_SIZING = 0x214;
        const int WMSZ_LEFT = 1;
        const int WMSZ_RIGHT = 2;
        const int WMSZ_TOP = 3;
        const int WMSZ_BOTTOM = 6;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Constructor of the form Resize.
        /// </summary>
        /// <param name="bitmap">The example bitmap.</param>
        public Resize(Bitmap bitmap)
        {
            InitializeComponent();

            this.Size = new Size(bitmap.Size.Width + 16, bitmap.Size.Height + 39);
            initialSize = this.Size;

            double gcd = GCD(this.Size.Height, this.Size.Width);

            widthRatio = this.Size.Width / gcd;
            heightRatio = this.Size.Height / gcd;

            pictureBox1.Image = bitmap;
        }

        /// <summary>
        /// Gets the new size of the frames resized by the user.
        /// </summary>
        /// <returns>The new size of the frames.</returns>
        public Size GetSize()
        {
            return pictureBox1.Size;
        }

        private void Resize_Resize(object sender, EventArgs e)
        {
            this.Text = pictureBox1.Size.Width + " x " + pictureBox1.Size.Height;

            if (freeAspectRatio)
            {
                double gcd = GCD(this.Size.Height, this.Size.Width);

                widthRatio = this.Size.Width / gcd;
                heightRatio = this.Size.Height / gcd;
            }
        }

        /// <summary>
        /// The Greater Common Divisor.
        /// </summary>
        /// <param name="a">Size a</param>
        /// <param name="b">Size b</param>
        /// <returns>The GCD number.</returns>
        static int GCD(int a, int b)
        {
            return b == 0 ? a : GCD(b, a % b);
        }

        protected override void WndProc(ref Message m)
        {
            if (!freeAspectRatio)
            {
                if (m.Msg == WM_SIZING)
                {
                    RECT rc = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));

                    int res = m.WParam.ToInt32();
                    if (res == WMSZ_LEFT || res == WMSZ_RIGHT)
                    {
                        //Left or right resize -> adjust height (bottom)
                        rc.Bottom = rc.Top + (int)(heightRatio * this.Width / widthRatio);
                    }
                    else if (res == WMSZ_TOP || res == WMSZ_BOTTOM)
                    {
                        //Up or down resize -> adjust width (right)
                        rc.Right = rc.Left + (int)(widthRatio * this.Height / heightRatio);
                    }
                    else if (res == WMSZ_RIGHT + WMSZ_BOTTOM)
                    {
                        //Lower-right corner resize -> adjust height (could have been width)
                        rc.Bottom = rc.Top + (int)(heightRatio * this.Width / widthRatio);
                    }
                    else if (res == WMSZ_LEFT + WMSZ_TOP)
                    {
                        //Upper-left corner -> adjust width (could have been height)
                        rc.Left = rc.Right - (int)(widthRatio * this.Height / heightRatio);
                    }
                    else if (res == WMSZ_BOTTOM + WMSZ_LEFT)
                    {
                        //Lower-left corner -> adjust height
                        rc.Bottom = rc.Top + (int) (heightRatio*this.Width/widthRatio);
                    }
                    else if (res == WMSZ_TOP + WMSZ_RIGHT)
                    {
                        //Upper-right corner -> adjust height
                        rc.Top = rc.Bottom - (int)(heightRatio * this.Width / widthRatio);
                    }
                    Marshal.StructureToPtr(rc, m.LParam, true);
                }
            }
            base.WndProc(ref m);
        }

        private void freeAspectRatioToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            freeAspectRatio = freeAspectRatioToolStripMenuItem.Checked;
        }

        private void resetSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = initialSize;

            double gcd = GCD(this.Size.Height, this.Size.Width);

            widthRatio = this.Size.Width / gcd;
            heightRatio = this.Size.Height / gcd;
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
