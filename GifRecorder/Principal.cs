using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gif.Components;

namespace GifRecorder
{
    public partial class Principal :Form
    {
        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        private string outputpath;
        private bool recording = false;

        private Bitmap bt;
        private Graphics gr;

        public Principal()
        {
            InitializeComponent();

            tbHeight.Text = (this.Height - 64).ToString();
            tbWidth.Text = (this.Width - 16).ToString();
        }

        private void Principal_Resize(object sender, EventArgs e)
        {
            tbHeight.Text = (this.Height - 64).ToString();
            tbWidth.Text = (this.Width - 16).ToString();
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                outputpath = saveFileDialog.FileName;
                encoder.Start(outputpath);
                encoder.SetDelay(1000/Convert.ToInt32(numMaxFps.Value));
                encoder.SetRepeat(0);
                timerTela.Interval = 1000/Convert.ToInt32(numMaxFps.Value);

                bt = new Bitmap(painel.Width, painel.Height);
                gr = Graphics.FromImage(bt);

                this.Text = "Screen to Gif ►";
                recording = true;
                numMaxFps.Enabled = false;
                timerTela.Start();
            }
        }

        private void timerTela_Tick(object sender, EventArgs e)
        {
            Point lefttop  = new Point(this.Location.X + 8, this.Location.Y + 31);
            Point leftbottom = new Point(lefttop.X, lefttop.Y + painel.Height);
            Point righttop = new Point(lefttop.X + painel.Width, lefttop.Y);
            Point rightbottom = new Point(lefttop.X + painel.Width, lefttop.Y + painel.Height);

            lbltopleft.Text = lefttop.ToString();
            lbltopright.Text = righttop.ToString();
            lblleftbottom.Text = leftbottom.ToString();
            lblbottomright.Text = rightbottom.ToString();

            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, painel.Bounds.Size, CopyPixelOperation.SourceCopy);

            encoder.AddFrame(Image.FromHbitmap(bt.GetHbitmap()));
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timerTela.Stop();
            if (recording)
            {
                encoder.Finish();
                recording = false;
                numMaxFps.Enabled = true;
                this.Text = "Screen to Gif ■";
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            if (!recording)
            {
                Info info = new Info();
                info.Show();
            }
        }
    }
}
