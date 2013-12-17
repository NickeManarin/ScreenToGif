using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScreenToGif.Encoding;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    public partial class Filters : Form
    {
        public List<Bitmap> ListBitmap { get; private set; }
        private List<Bitmap> listBitmapReset;

        private Thread thread;

        public Filters(List<Bitmap> bitmap)
        {
            InitializeComponent();

            listBitmapReset = new List<Bitmap>(bitmap);
            ListBitmap = new List<Bitmap>(bitmap);

            this.Size = new Size(bitmap[0].Size.Width + 24, bitmap[0].Size.Height + (110));

            pictureBoxFilter.Image = bitmap[0];

            trackBar.Maximum = ListBitmap.Count - 1;
            this.Text = "Filters - Frame " + trackBar.Value + " - " + (ListBitmap.Count - 1);
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            pictureBoxFilter.Image = (Bitmap)ListBitmap[trackBar.Value];
            this.Text = Resources.Title_FiltersFrame + trackBar.Value + " - " + (ListBitmap.Count - 1);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListBitmap.Clear();
            ListBitmap = new List<Bitmap>(listBitmapReset);
            pictureBoxFilter.Image = ListBitmap[trackBar.Value];
        }

        private void GrayscaleOne_Click(object sender, EventArgs e)
        {
            ListBitmap[trackBar.Value] = ImageUtil.MakeGrayscale((Bitmap)pictureBoxFilter.Image);
            pictureBoxFilter.Image = ListBitmap[trackBar.Value];
        }

        private void PixelateOne_Click(object sender, EventArgs e)
        {
            ValuePicker valuePicker = new ValuePicker(100, 2, Resources.Msg_PixelSize);
            valuePicker.ShowDialog();

            ListBitmap[trackBar.Value] = ImageUtil.Pixelate((Bitmap)pictureBoxFilter.Image, new Rectangle(0, 0, pictureBoxFilter.Image.Width, pictureBoxFilter.Image.Height), valuePicker.Value);
            pictureBoxFilter.Image = ListBitmap[trackBar.Value];

            valuePicker.Dispose();
        }

        private void BlurOne_Click(object sender, EventArgs e)
        {
            ValuePicker valuePicker = new ValuePicker(5, 1, Resources.Msg_BlurIntense);
            valuePicker.ShowDialog();

            ListBitmap[trackBar.Value] = ImageUtil.Blur((Bitmap)pictureBoxFilter.Image, new Rectangle(0, 0, pictureBoxFilter.Image.Width, pictureBoxFilter.Image.Height), valuePicker.Value);

            pictureBoxFilter.Image = ListBitmap[trackBar.Value];
            valuePicker.Dispose();
        }

        private void ColorizeOne_Click(object sender, EventArgs e)
        {
            //here colorize code
           pictureBoxFilter.Image = ListBitmap[trackBar.Value];
        }

        private void doneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void GrayscaleAll_Click(object sender, EventArgs e)
        {
            ListBitmap = ImageUtil.GrayScale(ListBitmap);
            pictureBoxFilter.Image = ListBitmap[trackBar.Value];
        }

        private void PixelateAll_Click(object sender, EventArgs e)
        {
            ValuePicker valuePicker = new ValuePicker(100, 2, Resources.Msg_PixelSize);
            valuePicker.ShowDialog();

            ListBitmap = ImageUtil.Pixelate(ListBitmap, new Rectangle(0, 0, pictureBoxFilter.Image.Width, pictureBoxFilter.Image.Height), valuePicker.Value);
            pictureBoxFilter.Image = ListBitmap[trackBar.Value];

            valuePicker.Dispose();
        }

        private void BlurAll_Click(object sender, EventArgs e)
        {
            ValuePicker valuePicker = new ValuePicker(5, 1, Resources.Msg_BlurIntense);
            valuePicker.ShowDialog();

            ListBitmap = ImageUtil.Blur(ListBitmap, new Rectangle(0, 0, pictureBoxFilter.Image.Width, pictureBoxFilter.Image.Height), valuePicker.Value);
            pictureBoxFilter.Image = ListBitmap[trackBar.Value];

            valuePicker.Dispose();
        }
    }
}
