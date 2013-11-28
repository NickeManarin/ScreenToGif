using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace ScreenToGif
{
    public partial class FrameEdit :Form
    {
        private List<IntPtr> listFramesPrivate;
        private List<IntPtr> listFramesUndoAll;
        private List<IntPtr> listFramesUndo;
 
        public FrameEdit(List<IntPtr> listFrames)
        {
            InitializeComponent();
            listFramesPrivate = new List<IntPtr>(listFrames);
            listFramesUndoAll = new List<IntPtr>(listFrames);
            listFramesUndo = new List<IntPtr>(listFrames);
            Application.DoEvents();

            trackBar.Maximum = listFramesPrivate.Count - 1;

            #region Window size
            Bitmap bitmap = Bitmap.FromHbitmap(listFramesPrivate[0]);

            Size sizeBitmap = new Size(bitmap.Size.Width + 50, bitmap.Size.Height + 100);

            if (bitmap.Size.Width > this.MinimumSize.Width)
            {
                sizeBitmap.Width = bitmap.Size.Width + 50;
            }
            else
            {
                sizeBitmap.Width = this.MinimumSize.Width;
            }

            if (bitmap.Size.Height > this.MinimumSize.Height)
            {
                sizeBitmap.Height = bitmap.Size.Height + 100;
            }
            else
            {
                sizeBitmap.Height = this.MinimumSize.Height;
            }
            
            this.Size = sizeBitmap;
            #endregion

            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[0]);
            this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo = new List<IntPtr>(listFramesPrivate);
                listFramesPrivate.Remove(listFramesPrivate[trackBar.Value]);
                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show("You can't delete the last frame.", "Minimum Ammount of Frames");
            }
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public List<IntPtr> getList()
        {
            return listFramesPrivate;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnUndoAll_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            listFramesUndo = new List<IntPtr>(listFramesPrivate); //To undo one
            listFramesPrivate = listFramesUndoAll;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void btnUndoOne_Click(object sender, EventArgs e)
        {
            listFramesPrivate = listFramesUndo;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            btnUndoOne.Enabled = false;
        }

        #region Context Menu
        private void nenuDeleteAfter_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            listFramesUndo = new List<IntPtr>(listFramesPrivate);

            if (listFramesPrivate.Count > 1)
            {
                int countList = listFramesPrivate.Count - 1; //So we have a fixed value

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    listFramesPrivate.Remove(listFramesPrivate[i]);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = listFramesPrivate.Count - 1;
                pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void menuDeleteBefore_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            listFramesUndo = new List<IntPtr>(listFramesPrivate);

            if (listFramesPrivate.Count > 1)
            {
                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    listFramesPrivate.Remove(listFramesPrivate[i]);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                this.Text = "Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }
        #endregion

    }
}
