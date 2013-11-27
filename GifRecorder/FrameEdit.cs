using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifRecorder
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

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[0]);
            lblFrame.Text = "Frame " + trackBar.Value + " of " + listFramesPrivate.Count;
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            lblFrame.Text = "Frame " + trackBar.Value + " of " + listFramesPrivate.Count;
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = false;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo = new List<IntPtr>(listFramesPrivate);
                listFramesPrivate.Remove(listFramesPrivate[trackBar.Value]);
                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                lblFrame.Text = "Frame " + trackBar.Value + " of " + listFramesPrivate.Count;
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
            btnUndoOne.Enabled = false;
            listFramesUndo = new List<IntPtr>(listFramesPrivate); //To undo one
            listFramesPrivate = listFramesUndoAll;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            lblFrame.Text = "Frame " + trackBar.Value + " of " + listFramesPrivate.Count;
        }

        private void btnUndoOne_Click(object sender, EventArgs e)
        {
            listFramesPrivate = listFramesUndo;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBox.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            lblFrame.Text = "Frame " + trackBar.Value + " of " + listFramesPrivate.Count;

            btnUndoOne.Enabled = false;
        }
    }
}
