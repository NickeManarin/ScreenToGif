using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gif.Components;

namespace GifRecorder
{
    public partial class Principal :Form
    {
        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        private string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private int numOfFile = 0;
        private string outputpath;
        private int recording = 0; //0 Stoped, 1 Recording, 2 Paused
        private List<IntPtr> listBitmap; 

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

        private void timerTela_Tick(object sender, EventArgs e)
        {

            Point lefttop = new Point(this.Location.X + 8, this.Location.Y + 31);

            #region DEV-Only
            //Point leftbottom = new Point(lefttop.X, lefttop.Y + painel.Height);
            //Point righttop = new Point(lefttop.X + painel.Width, lefttop.Y);
            //Point rightbottom = new Point(lefttop.X + painel.Width, lefttop.Y + painel.Height);

            //lbltopleft.Text = lefttop.ToString();
            //lbltopright.Text = righttop.ToString();
            //lblleftbottom.Text = leftbottom.ToString();
            //lblbottomright.Text = rightbottom.ToString();
            #endregion

            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, painel.Bounds.Size, CopyPixelOperation.SourceCopy);

            //encoder.AddFrame(Image.FromHbitmap(bt.GetHbitmap()));
            listBitmap.Add(bt.GetHbitmap());
        }

        public void DoWork()
        {
            int numImage = 0;
            foreach (var image in listBitmap)
            {
                numImage++;
                try
                {
                    this.Invoke((Action)delegate
                    {
                        this.Text = "Processing (Frame " + numImage + ")";
                    });
                }
                catch (Exception)
                {
                }

                encoder.AddFrame(Image.FromHbitmap(image));
            }

            try
            {
                this.Invoke((Action)delegate
                {
                    this.Text = "Screen to Gif ■ (Encoding Done)";
                    recording = 0;
                    numMaxFps.Enabled = true;
                    btnPauseRecord.Text = "Record";
                    btnPauseRecord.Image = Properties.Resources.record;
                });
            }
            catch (Exception)
            {
            }

            encoder.Finish();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timerTela.Stop();
            if (recording != 0)
            {
                var workerThread = new Thread(DoWork);
                workerThread.IsBackground = true;
                workerThread.Start();

                recording = 0;
                numMaxFps.Enabled = true;
                this.Text = "Screen to Gif ■";
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            if (recording != 1)
            {
                Info info = new Info();
                info.Show();
            }
        }

        private void btnPauseRecord_Click(object sender, EventArgs e)
        {
            if (recording == 0)
            {

                #region Search For Filename
                bool searchForName = true;
                while (searchForName)
                {
                    if (!File.Exists(path + "\\Animation " + numOfFile + ".gif"))
                    {
                        outputpath = path + "\\Animation " + numOfFile + ".gif";
                        searchForName = false;
                    }
                    else
                    {
                        if (numOfFile > 999)
                        {
                            searchForName = false;
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                outputpath = saveFileDialog.FileName;
                            }
                            else
                            {
                                outputpath = "Fuck this filename";
                            }
                        }
                        numOfFile++;
                    }
                }
                #endregion

                btnConfig.ToolTipText = "File name is: " + outputpath;

                encoder.Start(outputpath);
                //encoder.SetDelay(100 / Convert.ToInt32(numMaxFps.Value));
                encoder.SetRepeat(0);
                encoder.SetSize(painel.Size.Width, painel.Size.Height);
                encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));
                timerTela.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<IntPtr>(); 

                bt = new Bitmap(painel.Width, painel.Height);
                gr = Graphics.FromImage(bt);

                this.Text = "Screen to Gif ►";
                btnPauseRecord.Text = "Pause";
                btnPauseRecord.Image = Properties.Resources.pause;
                recording = 1;
                numMaxFps.Enabled = false;
                timerTela.Start();

            }
            else if (recording == 1)
            {
                this.Text = "Screen to Gif (Paused)";
                btnPauseRecord.Text = "Record";
                btnPauseRecord.Image = Properties.Resources.record;
                recording = 2;

                timerTela.Enabled = false;
            }
            else if (recording == 2)
            {
                this.Text = "Screen to Gif ►";
                btnPauseRecord.Text = "Pause";
                btnPauseRecord.Image = Properties.Resources.pause;
                recording = 1;

                timerTela.Enabled = true;
            }
        }

        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (recording != 0)
            {
                timerTela.Stop();

                var workerThread = new Thread(DoWork);
                workerThread.Start();
            }
        }
    }
}
