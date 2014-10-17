using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Form tool used to resize images.
    /// </summary>
    public partial class Resize : Form
    {
        #region Variables

        private bool _freeAspectRatio;
        private readonly Size _initialSize;

        double _widthRatio = 16;
        double _heightRatio = 9;

        #endregion

        #region P/Invoke Constants

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

        #endregion

        /// <summary>
        /// Constructor of the form Resize.
        /// </summary>
        /// <param name="bitmap">The example bitmap.</param>
        public Resize(Bitmap bitmap)
        {
            InitializeComponent();

            #region Window Chrome Measurement

            this.Size = new Size(100, 100);
            var sizepictureBox = pbImage.Size;
            int diffW = Math.Abs(this.Size.Width - sizepictureBox.Width);
            int diffH = Math.Abs(this.Size.Height - sizepictureBox.Height);

            #endregion

            this.Size = new Size(bitmap.Size.Width + diffW, bitmap.Size.Height + diffH);
            
            pbImage.Size = bitmap.Size;
            pbImage.Image = bitmap;

            base.AutoSize = false;

            _initialSize = this.Size;

            double gcd = Gcd(this.Size.Height, this.Size.Width);

            _widthRatio = this.Size.Width / gcd;
            _heightRatio = this.Size.Height / gcd;
        }

        /// <summary>
        /// Gets the new size of the frames resized by the user.
        /// </summary>
        /// <returns>The new size of the frames.</returns>
        public Size GetSize()
        {
            return pbImage.Size;
        }

        /// <summary>
        /// The Greater Common Divisor.
        /// </summary>
        /// <param name="a">Size a</param>
        /// <param name="b">Size b</param>
        /// <returns>The GCD number.</returns>
        static int Gcd(int a, int b)
        {
            return b == 0 ? a : Gcd(b, a % b);
        }

        protected override void WndProc(ref Message m)
        {
            if (!_freeAspectRatio)
            {
                if (m.Msg == WM_SIZING)
                {
                    #region Sizing Validation

                    var rc = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));

                    int res = m.WParam.ToInt32();
                    if (res == WMSZ_LEFT || res == WMSZ_RIGHT)
                    {
                        //Left or right resize -> adjust height (bottom)
                        rc.Bottom = rc.Top + (int)(_heightRatio * this.Width / _widthRatio);
                    }
                    else if (res == WMSZ_TOP || res == WMSZ_BOTTOM)
                    {
                        //Up or down resize -> adjust width (right)
                        rc.Right = rc.Left + (int)(_widthRatio * this.Height / _heightRatio);
                    }
                    else if (res == WMSZ_RIGHT + WMSZ_BOTTOM)
                    {
                        //Lower-right corner resize -> adjust height (could have been width)
                        rc.Bottom = rc.Top + (int)(_heightRatio * this.Width / _widthRatio);
                    }
                    else if (res == WMSZ_LEFT + WMSZ_TOP)
                    {
                        //Upper-left corner -> adjust width (could have been height)
                        rc.Left = rc.Right - (int)(_widthRatio * this.Height / _heightRatio);
                    }
                    else if (res == WMSZ_BOTTOM + WMSZ_LEFT)
                    {
                        //Lower-left corner -> adjust height
                        rc.Bottom = rc.Top + (int)(_heightRatio * this.Width / _widthRatio);
                    }
                    else if (res == WMSZ_TOP + WMSZ_RIGHT)
                    {
                        //Upper-right corner -> adjust height
                        rc.Top = rc.Bottom - (int)(_heightRatio * this.Width / _widthRatio);
                    }

                    Marshal.StructureToPtr(rc, m.LParam, true);

                    #endregion
                }
            }

            base.WndProc(ref m);
        }

        private void Resize_Resize(object sender, EventArgs e)
        {
            this.Text = pbImage.Size.Width + " x " + pbImage.Size.Height;

            if (_freeAspectRatio)
            {
                double gcd = Gcd(this.Size.Height, this.Size.Width);

                _widthRatio = this.Size.Width / gcd;
                _heightRatio = this.Size.Height / gcd;
            }
        }

        private void freeAspectRatioToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            _freeAspectRatio = freeAspectRatioToolStripMenuItem.Checked;
        }

        private void resetSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Size = _initialSize;

            double gcd = Gcd(this.Size.Height, this.Size.Width);

            _widthRatio = this.Size.Width / gcd;
            _heightRatio = this.Size.Height / gcd;
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void pbImage_Click(object sender, EventArgs e)
        {
            contextMenu.Show(MousePosition);
        }
    }
}
