using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AnimatedGifEncoder = ScreenToGif.Encoding.AnimatedGifEncoder;

namespace ScreenToGif
{
    public partial class Principal :Form
    {
        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        private string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private int numOfFile = 0;
        private int preStart = 1;
        private bool screenSizeEdit = false;
        private string outputpath;
        private int recording = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart
        private List<IntPtr> listBitmap;
        private Point posCursor;
        private Point sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Bitmap bt;
        private Graphics gr;

        public Principal()
        {
            InitializeComponent();

            tbHeight.Text = (this.Height - 64).ToString();
            tbWidth.Text = (this.Width - 16).ToString();
        }

        private void Principal_Resize(object sender, EventArgs e) //To show the exactly size of the form.
        {
            if (!screenSizeEdit)
            {
                tbHeight.Text = (this.Height - 64).ToString();
                tbWidth.Text = (this.Width - 16).ToString();
            }
        }

        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
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

            //Take a screenshot of the area.
            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, painel.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            listBitmap.Add(bt.GetHbitmap());
        }

        public void DoWork() //Thread
        {
            int numImage = 0;
            foreach (var image in listBitmap)
            {
                numImage++;
                try
                {
                    this.Invoke((Action)delegate //Needed because it's a cross thread call.
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
            timerCapture.Stop();
            if (recording != 0 && recording != 3)
            {
                cursor.Visible = false;
                cursorTimer.Stop();

                if (cbAllowEdit.Checked)
                {
                    FrameEdit frameEdit = new FrameEdit(listBitmap);

                    if (frameEdit.ShowDialog(this) == DialogResult.OK)
                    {
                       listBitmap = frameEdit.getList();
                    }

                }

                if (!cbSaveDirectly.Checked)
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "GIF file (*.gif)|*gif";
                    sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    sfd.DefaultExt = "gif";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        encoder.Start(sfd.FileName);

                        encoder.SetRepeat(0); // 0 = Always
                        encoder.SetSize(painel.Size.Width, painel.Size.Height);
                        encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));
                        timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                        var workerThread = new Thread(DoWork);
                        workerThread.IsBackground = true;
                        workerThread.Start();
                    }
                    
                }
                else
                {
                    var workerThread = new Thread(DoWork);
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }

                recording = 0;
                numMaxFps.Enabled = true;
                this.Text = "Screen to Gif ■";
            }
            else if (recording == 3)
            {
                PreStart.Stop();
                recording = 0;
                numMaxFps.Enabled = true;
                btnPauseRecord.Enabled = true;
                this.Text = "Screen to Gif ■";
            }
        } //STOP

        private void btnInfo_Click(object sender, EventArgs e)
        {
            if (recording != 1)
            {
                Info info = new Info();
                info.Show();
            }
        } //INFO

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

                btnConfig.ToolTipText = "Automatic filename is: " + outputpath;

                encoder.Start(outputpath);

                encoder.SetRepeat(0); // 0 = Always
                encoder.SetSize(painel.Size.Width, painel.Size.Height);
                encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));
                timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<IntPtr>(); //List that contains all the frames.

                bt = new Bitmap(painel.Width, painel.Height);
                gr = Graphics.FromImage(bt);

                this.Text = "Screen (2 seconds to go)";
                btnPauseRecord.Text = "Pause";
                btnPauseRecord.Image = Properties.Resources.pause;
                btnPauseRecord.Enabled = false;
                recording = 3;
                numMaxFps.Enabled = false;
                preStart = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2
                PreStart.Start();
                //timerCapture.Start();

            }
            else if (recording == 1)
            {
                this.Text = "Screen to Gif (Paused)";
                btnPauseRecord.Text = "Continue";
                btnPauseRecord.Image = Properties.Resources.record;
                recording = 2;

                timerCapture.Enabled = false;
            }
            else if (recording == 2)
            {
                this.Text = "Screen to Gif ►";
                btnPauseRecord.Text = "Pause";
                btnPauseRecord.Image = Properties.Resources.pause;
                recording = 1;

                timerCapture.Enabled = true;
            }
        } //RECORD-PAUSE

        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (recording != 0)
            {
                timerCapture.Stop();

                var workerThread = new Thread(DoWork);
                workerThread.Start();
            }
        }

        private void PreStart_Tick(object sender, EventArgs e)
        {
            if (preStart >= 1)
            {
                this.Text = "Screen (" + preStart + " seconds to go)";
                preStart--;
            }
            else
            {
                this.Text = "Screen to Gif ►";
                PreStart.Stop();
                if (cbShowCursor.Checked)
                {
                    //Cursor position
                    cursor.Visible = true;
                    posCursor = this.PointToClient(Cursor.Position);
                    cursor.Location = posCursor;

                    cursorTimer.Start(); //Cursor position   
                }
                recording = 1;
                btnPauseRecord.Enabled = true;
                timerCapture.Start(); //Frame recording
            }
        } //PRE START SEQUENCE

        private void cursorTimer_Tick(object sender, EventArgs e)
        {
            posCursor = this.PointToClient(Cursor.Position);
            posCursor.X++;
            posCursor.Y++;
            cursor.Location = posCursor;
        } //CURSOR TIMER

        private void btnConfig_Click(object sender, EventArgs e)
        {
            panelConfig.Visible = !panelConfig.Visible;
        } //CONFIG

        private void btnDone_Click(object sender, EventArgs e)
        {
            panelConfig.Visible = false;
        } //DONE CONFIG

        #region TextBox Size

        private void tbWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) ||
                char.IsSymbol(e.KeyChar) ||
                char.IsWhiteSpace(e.KeyChar) ||
                char.IsPunctuation(e.KeyChar))
                e.Handled = true;
        } // TB SIZE

        private void tbHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) ||
                char.IsSymbol(e.KeyChar) ||
                char.IsWhiteSpace(e.KeyChar) ||
                char.IsPunctuation(e.KeyChar))
                e.Handled = true;
        } //TB SIZE

        private void tbHeight_Leave(object sender, EventArgs e)
        {
            screenSizeEdit = true;
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (sizeScreen.Y > heightTb)
            {
                this.Size = new Size(widthTb + 16, heightTb + 64);
            }
            else
            {
                this.Size = new Size(widthTb + 16, sizeScreen.Y - 1);
            }
            screenSizeEdit = false;
        }

        private void tbWidth_Leave(object sender, EventArgs e)
        {
            screenSizeEdit = true; //So the Resize event won't trigger
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (sizeScreen.X > widthTb)
            {
                this.Size = new Size(widthTb + 16, heightTb + 64);
            }
            else
            {
                this.Size = new Size(sizeScreen.X - 1, heightTb + 64);
            }
            screenSizeEdit = false;
        }

        #endregion
    }
}
