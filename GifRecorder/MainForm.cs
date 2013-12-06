using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using ScreenToGif.Capture;
using ScreenToGif.Encoding;
using ScreenToGif.Properties;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ScreenToGif
{
    public partial class MainForm :Form
    {
        private const int CS_DROPSHADOW = 0x00020000;
        public const int WM_NCHITTEST = 0x84;

        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        CaptureScreen capture = new CaptureScreen();

        private UserActivityHook actHook;
        private Point sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Size lastSize; //The editor may increase the size of the form, use this to go back to the last size
        private bool screenSizeEdit = false;
        private bool frameEdit = false;
        private int preStart = 1;
        private int numOfFile = 0;
        private string outputpath;
        private int stage = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding

        private List<IntPtr> listBitmap;
        private List<CursorInfo> listCursor = new List<CursorInfo>(); //List that stores the icon

        private CursorInfo cursorInfo;
        private Rectangle rect;

        static Point lastClick;
        private Bitmap bt;
        private Graphics gr;
        private Point posCursor;
        private Thread workerThread;

        LocalizationString local = new LocalizationString();

        #region Native

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        private static extern bool DeleteObject(System.IntPtr hObject);

        //For the hit test area
        private const int cGrip = 20;
        private const int cCaption = 35;
        private const int cBorder = 7;

        /// <summary>
        /// Indicates the position of the cursor hot spot.
        /// </summary>
        public enum HitTest :int
        {
            /// <summary>
            /// On the screen background or on a dividing line between windows (same as HTNOWHERE, except that the DefWindowProc function produces a system beep to indicate an error).
            /// </summary>
            HTERROR = -2,

            /// <summary>
            /// In a window currently covered by another window in the same thread (the message will be sent to underlying windows in the same thread until one of them returns a code that is not HTTRANSPARENT).
            /// </summary>
            HTTRANSPARENT = -1,

            /// <summary>
            /// On the screen background or on a dividing line between windows.
            /// </summary>
            HTNOWHERE = 0,

            /// <summary>
            /// In a client area.
            /// </summary>
            HTCLIENT = 1,

            /// <summary>
            /// In a title bar.
            /// </summary>
            HTCAPTION = 2,

            /// <summary>
            /// In a window menu or in a Close button in a child window.
            /// </summary>
            HTSYSMENU = 3,

            /// <summary>
            /// In a size box (same as HTSIZE).
            /// </summary>
            HTGROWBOX = 4,

            /// <summary>
            /// In a size box (same as HTGROWBOX).
            /// </summary>
            HTSIZE = 4,

            /// <summary>
            /// In a menu.
            /// </summary>
            HTMENU = 5,

            /// <summary>
            /// In a horizontal scroll bar.
            /// </summary>
            HTHSCROLL = 6,

            /// <summary>
            /// In the vertical scroll bar.
            /// </summary>
            HTVSCROLL = 7,

            /// <summary>
            /// In a Minimize button.
            /// </summary>
            HTMINBUTTON = 8,

            /// <summary>
            /// In a Minimize button.
            /// </summary>
            HTREDUCE = 8,

            /// <summary>
            /// In a Maximize button.
            /// </summary>
            HTMAXBUTTON = 9,

            /// <summary>
            /// In a Maximize button.
            /// </summary>
            HTZOOM = 9,

            /// <summary>
            /// In the left border of a resizable window (the user can click the mouse to resize the window horizontally).
            /// </summary>
            HTLEFT = 10,

            /// <summary>
            /// In the right border of a resizable window (the user can click the mouse to resize the window horizontally).
            /// </summary>
            HTRIGHT = 11,

            /// <summary>
            /// In the upper-horizontal border of a window.
            /// </summary>
            HTTOP = 12,

            /// <summary>
            /// In the upper-left corner of a window border.
            /// </summary>
            HTTOPLEFT = 13,

            /// <summary>
            /// In the upper-right corner of a window border.
            /// </summary>
            HTTOPRIGHT = 14,

            /// <summary>
            /// In the lower-horizontal border of a resizable window (the user can click the mouse to resize the window vertically).
            /// </summary>
            HTBOTTOM = 15,

            /// <summary>
            /// In the lower-left corner of a border of a resizable window (the user can click the mouse to resize the window diagonally).
            /// </summary>
            HTBOTTOMLEFT = 16,

            /// <summary>
            /// In the lower-right corner of a border of a resizable window (the user can click the mouse to resize the window diagonally).
            /// </summary>
            HTBOTTOMRIGHT = 17,

            /// <summary>
            /// In the border of a window that does not have a sizing border.
            /// </summary>
            HTBORDER = 18,

            /// <summary>
            /// In a Close button.
            /// </summary>
            HTCLOSE = 20,

            /// <summary>
            /// In a Help button.
            /// </summary>
            HTHELP = 21,
        }; 

        #endregion

        public MainForm()
        {
            #region Load Localized Strings
            //Before the initialize components

            //local.ChangeStrings("en", true);

            #endregion

            InitializeComponent();

            #region Load Save Data

            cbShowCursor.Checked = Properties.Settings.Default.STshowCursor;
            cbAllowEdit.Checked = Properties.Settings.Default.STallowEdit;
            cbSaveDirectly.Checked = Properties.Settings.Default.STsaveLocation;
            cbLegacyStyle.Checked = !Properties.Settings.Default.STmodernStyle;
            numMaxFps.Value = Properties.Settings.Default.STmaxFps;

            //Gets the Hotkeys
            comboStartPauseKey.Text = Properties.Settings.Default.STstartPauseKey.ToString();
            comboStopKey.Text = Properties.Settings.Default.STstopKey.ToString();

            //Gets the Gif settings
            trackBarQuality.Value = Properties.Settings.Default.STquality;
            cbLoop.Checked = Properties.Settings.Default.STloop;

            //Load last saved window size
            this.Size = new Size(Properties.Settings.Default.STsize.Width, Properties.Settings.Default.STsize.Height);

            #endregion

            //Gets the window size and show in the textBoxes
            tbHeight.Text = (this.Height - 71).ToString();
            tbWidth.Text = (this.Width - 24).ToString();

            //Performance and flickering tweaks
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);

            #region Global Hook
            actHook = new UserActivityHook();
            actHook.KeyDown += KeyHookTarget;
            actHook.Start();
            #endregion
        }

        #region Override
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //Creates a border around the form
            Graphics g = e.Graphics;
            Rectangle rect = new Rectangle(new Point(0, 0), new Size(this.Width - 1, this.Height - 1));
            Rectangle rectInside = new Rectangle(new Point(11, 33), new Size(panelTransparent.Width + 1, panelTransparent.Height + 1));

            Pen pen;

            if (stage == 1)
            {
                pen = new Pen(Color.FromArgb(255, 25, 0));  
            }
            else if (stage == 4)
            {
                pen = new Pen(Color.FromArgb(30, 180, 30)); 
            }
            else
            {
                pen = new Pen(Color.FromArgb(0, 151, 251));
            }
            
            g.DrawRectangle(pen, rect);
            g.DrawRectangle(pen, rectInside);

            //Create a round border, not so beautiful
            //System.IntPtr ptrBorder = CreateRoundRectRgn(0, 0,
            //this.ClientSize.Width, this.ClientSize.Height, 15, 15);
            //SetWindowRgn(this.Handle, ptrBorder, true);

            //Create a rectangle around the form, to make a hit test
            Rectangle rc = new Rectangle(this.ClientSize.Width - cGrip,
                this.ClientSize.Height - cGrip, cGrip, cGrip);

            //Paints the size grip
            ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, rc);

        }

        protected override void WndProc(ref Message m)
        {
            if (stage == 0 || stage == 4)
            {
                if (m.Msg == (int)WM_NCHITTEST)
                {
                    #region Hit Test
                    // Trap WM_NCHITTEST
                    Point pos = new Point(m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16);
                    pos = this.PointToClient(pos);

                    //Bottom Left
                    if (pos.X <= cGrip && pos.Y >= this.ClientSize.Height - cGrip)
                    {
                        m.Result = (IntPtr)HitTest.HTBOTTOMLEFT;
                        return;
                    }

                    //Bottom Right
                    else if (pos.X >= this.ClientSize.Width - cGrip &&
                        pos.Y >= this.ClientSize.Height - cGrip)
                    {
                        m.Result = (IntPtr)HitTest.HTBOTTOMRIGHT;
                        return;
                    }

                    //Top Left
                    else if (pos.Y <= cBorder && pos.X <= cBorder)
                    {
                        m.Result = (IntPtr)HitTest.HTTOPLEFT;
                        return;
                    }

                    //Top Right
                    else if (pos.Y <= cBorder && pos.X >= this.ClientSize.Width - cBorder)
                    {
                        m.Result = (IntPtr)HitTest.HTTOPRIGHT;
                        return;
                    }

                    //Top
                    else if (pos.Y <= cBorder)
                    {
                        m.Result = (IntPtr)HitTest.HTTOP;
                        return;
                    }

                    //Caption
                    else if (pos.Y < cCaption && pos.X > 50 && pos.Y < ClientSize.Width)
                    {
                        m.Result = (IntPtr)HitTest.HTCAPTION;
                        return;
                    }

                    //Bottom
                    else if (pos.Y >= this.ClientSize.Height - cBorder)
                    {
                        m.Result = (IntPtr)HitTest.HTBOTTOM;
                        return;
                    }

                    //Left
                    else if (pos.X <= cBorder)
                    {
                        m.Result = (IntPtr)HitTest.HTLEFT;
                        return;
                    }

                    //Right
                    else if (pos.X >= this.ClientSize.Width - cBorder)
                    {
                        m.Result = (IntPtr)HitTest.HTRIGHT;
                        return;
                    }
                    #endregion
                }
            }

            base.WndProc(ref m);
        }

        #endregion 

        #region Main Form Move/Resize /Closing

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            Application.DoEvents();
            if (e.Button == MouseButtons.Left)
            {
                lastClick = new Point(e.X, e.Y); //We'll need this for when the Form starts to move
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            //Application.DoEvents();
            if (e.Button == MouseButtons.Left)
            {
                //Move the Form the same difference the mouse cursor moved;
                this.Left += e.X - lastClick.X;
                this.Top += e.Y - lastClick.Y;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (!screenSizeEdit)
            {
                tbHeight.Text = (this.Height - 71).ToString();
                tbWidth.Text = (this.Width - 24).ToString();
            }

            if (this.WindowState == FormWindowState.Normal)
                btnMaximize.Image = Properties.Resources.MaximizeMinus;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.STmaxFps = Convert.ToInt32(numMaxFps.Value);
            Properties.Settings.Default.STsize = new Size(this.Size.Width, this.Size.Height);
            Properties.Settings.Default.STshowCursor = cbShowCursor.Checked;
            Properties.Settings.Default.STallowEdit = cbAllowEdit.Checked;
            Properties.Settings.Default.STsaveLocation = cbSaveDirectly.Checked;
            Properties.Settings.Default.STmodernStyle = !cbLegacyStyle.Checked;

            Properties.Settings.Default.STstartPauseKey = getKeys(comboStartPauseKey.Text);
            Properties.Settings.Default.STstopKey = getKeys(comboStopKey.Text);

            Properties.Settings.Default.STquality = trackBarQuality.Value;
            Properties.Settings.Default.STloop = cbLoop.Checked;

            Properties.Settings.Default.Save();

            actHook.Stop();

            if (stage != 0)
            {
                timerCapture.Stop();
                timerCapture.Dispose();
            }
        }
        
        #endregion

        #region Bottom buttons

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnMaximize.Enabled = true;
            btnMinimize.Enabled = true;

            Stop();
        }

        private void btnRecordPause_Click(object sender, EventArgs e)
        {
            panelConfig.Visible = false;
            panelGifConfig.Visible = false;
            btnMaximize.Enabled = false;
            btnMinimize.Enabled = false;

            RecordPause();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            if (panelConfig.Visible) //Save the setting
            {
                Properties.Settings.Default.STshowCursor = cbShowCursor.Checked;
                Properties.Settings.Default.STallowEdit = cbAllowEdit.Checked;
                Properties.Settings.Default.STsaveLocation = cbSaveDirectly.Checked;
                Properties.Settings.Default.STmodernStyle = !cbLegacyStyle.Checked;

                Properties.Settings.Default.STstartPauseKey = getKeys(comboStartPauseKey.Text);
                Properties.Settings.Default.STstopKey = getKeys(comboStopKey.Text);
                Properties.Settings.Default.Save();
            }
            panelGifConfig.Visible = false;
            panelConfig.Visible = !panelConfig.Visible;
        }

        private void btnGifConfig_Click(object sender, EventArgs e)
        {
            if (panelGifConfig.Visible) //Save the settings
            {
                Properties.Settings.Default.STquality = trackBarQuality.Value;
                Properties.Settings.Default.STloop = cbLoop.Checked;
                //Properties.Settings.Default.STsaveLocation = cbSaveDirectly.Checked;

                Properties.Settings.Default.Save();
            }
            panelConfig.Visible = false;
            panelGifConfig.Visible = !panelGifConfig.Visible;
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            //Change to modern style
            Info info = new Info();
            info.Show(this);
        }

        #endregion

        #region Functions

        private void KeyHookTarget(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Properties.Settings.Default.STstartPauseKey)
            {
                RecordPause();
            }
            else if (e.KeyCode == Properties.Settings.Default.STstopKey)
            {
                Stop();
            }
        }

        private void RecordPause()
        {
            if (stage == 0) //if stoped
            {
                encoder.SetQuality(trackBarQuality.Value);
                
                //encoder.SetDispose(3);
                //encoder.SetTransparent(Color.White);

                encoder.SetRepeat(cbLoop.Checked ? 0 : -1); //if checked, 0 else -1

                encoder.SetSize(panelTransparent.Size.Width, panelTransparent.Size.Height);
                encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));
                timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);
                timerCapWithCursor.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<IntPtr>(); //List that contains all the frames.
                listCursor = new List<CursorInfo>(); //List that contains all the icon information

                bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                gr = Graphics.FromImage(bt);

                labelTitle.Text = Resources.Title_2SecToGo;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                btnRecordPause.Enabled = false;
                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                btnMaximize.Enabled = false;
                btnMinimize.Enabled = false;
                stage = 3;
                numMaxFps.Enabled = false;
                preStart = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                timerPreStart.Start();
                this.TopMost = true;
                this.Invalidate();
            }
            else if (stage == 1) //if recording
            {
                labelTitle.Text = Resources.TitlePaused;
                btnRecordPause.Text = Resources.btnRecordPause_Continue;
                btnRecordPause.Image = Properties.Resources.Play_17Green;
                stage = 2;
                this.Invalidate(); //To redraw the form, activating OnPaint again
                timerCapture.Enabled = false;
            }
            else if (stage == 2) //if paused
            {
                labelTitle.Text = Resources.TitleRecording;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                stage = 1;
                this.Invalidate(); //To redraw the form, activating OnPaint again
                timerCapture.Enabled = true;
            }
        }

        private void Stop()
        {
            actHook.Stop();
            actHook.KeyDown -= KeyHookTarget;

            timerCapture.Stop();
            timerCapWithCursor.Stop();

            if (stage != 0 && stage != 3) //if not already stop or pre starting
            {
                if (cbAllowEdit.Checked)
                {
                    lastSize = this.Size; //To return back to the last form size after the editor
                    stage = 4;
                    this.Invalidate();
                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;
                    EditFrames();
                    flowPanel.Enabled = false;
                }
                else
                {
                    Save();
                }
            }
            else if (stage == 3) //if pre starting
            {
                timerPreStart.Stop();
                stage = 0;
                numMaxFps.Enabled = true;
                btnRecordPause.Enabled = true;
                tbHeight.Enabled = true;
                tbWidth.Enabled = true;
                btnRecordPause.Image = Properties.Resources.Play_17Green;
                btnRecordPause.Text = "Record";
                labelTitle.Text = "Screen to Gif ■";
                this.Invalidate();

                actHook.KeyDown += KeyHookTarget;
                actHook.Start();
            }
        }

        private void Save()
        {
            this.Size = lastSize;
            if (!cbSaveDirectly.Checked) // to choose the location to save the gif
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    encoder.Start(sfd.FileName);

                    if (cbShowCursor.Checked)
                    {
                        workerThread = new Thread(DoWorkWithCursor);
                        workerThread.IsBackground = true;
                        workerThread.Start();
                    }
                    else
                    {
                        workerThread = new Thread(DoWork);
                        workerThread.IsBackground = true;
                        workerThread.Start();
                    }
                }
                else
                {
                    flowPanel.Enabled = true;
                    this.MinimumSize = new Size(300, 200);
                    stage = 0; //Stoped
                    numMaxFps.Enabled = true;
                    tbWidth.Enabled = true;
                    tbHeight.Enabled = true;
                    this.TopMost = false;
                    btnRecordPause.Text = "Record";
                    btnRecordPause.Image = Resources.Play_17Green;
                    labelTitle.Text = "Screen to Gif ■";
                    this.Invalidate();

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start();

                    return;
                }
            }
            else
            {
                #region Search For Filename

                bool searchForName = true;
                string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

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
                                outputpath = "No filename for you";
                            }
                        }
                        numOfFile++;
                    }
                }
                #endregion

                encoder.Start(outputpath);

                if (cbShowCursor.Checked)
                {
                    workerThread = new Thread(DoWorkWithCursor);
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }
                else
                {
                    workerThread = new Thread(DoWork);
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }
            }

            this.MinimumSize = new Size(300, 200);
            stage = 0; //Stoped
            numMaxFps.Enabled = true;
            tbHeight.Enabled = false;
            tbWidth.Enabled = false;
            this.TopMost = false;
            this.Text = "Screen to Gif ■";
            this.Invalidate();
        }

        private void DoWork() //Thread
        {
            int numImage = 0;
            int countList = listBitmap.Count;
            foreach (var image in listBitmap)
            {
                numImage++;
                try
                {
                    this.Invoke((Action)delegate //Needed because it's a cross thread call.
                    {
                        labelTitle.Text = "Processing (Frame " + numImage + " out of " + countList + ")";
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
                    labelTitle.Text = "Screen To Gif ■ (Encoding Done)";
                    stage = 0;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Text = "Record";
                    btnRecordPause.Image = Properties.Resources.Play_17Green;
                    flowPanel.Enabled = true;
                    this.TopMost = false;

                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start();
                });
            }
            catch (Exception)
            {
            }

            encoder.Finish();
        }

        private void DoWorkWithCursor() //Thread function 2
        {
            int numImage = 0;
            int countList = listBitmap.Count;
            Bitmap bitmapEnc;

            foreach (IntPtr image in listBitmap)
            {
                try
                {
                    this.Invoke((Action)delegate //Needed because it's a cross thread call.
                    {
                        labelTitle.Text = "Processing (Frame " + numImage + " out of " + countList + ")";
                    });
                }
                catch (Exception)
                {
                }

                bitmapEnc = Bitmap.FromHbitmap(image);
                Graphics graph = Graphics.FromImage(bitmapEnc);
                rect = new Rectangle(listCursor[numImage].Position.X, listCursor[numImage].Position.Y, listCursor[numImage].Icon.Width, listCursor[numImage].Icon.Height);

                graph.DrawIcon(listCursor[numImage].Icon, rect);
                graph.Flush();

                encoder.AddFrame(bitmapEnc);
                numImage++;
            }

            try
            {
                this.Invoke((Action)delegate
                {
                    labelTitle.Text = "Screen To Gif ■ (Encoding Done)";
                    stage = 0;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Text = "Record";
                    btnRecordPause.Image = Properties.Resources.Play_17Green;
                    flowPanel.Enabled = true;
                    this.TopMost = false;

                    //tbHeight.Enabled = true;
                    //tbWidth.Enabled = true;

                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start();
                });
            }
            catch (Exception)
            {
            }

            encoder.Finish();
        }

        #endregion

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
            Application.DoEvents();
            screenSizeEdit = true;
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (sizeScreen.Y > heightTb)
            {
                this.Size = new Size(widthTb + 24, heightTb + 71);
            }
            else
            {
                this.Size = new Size(widthTb + 24, sizeScreen.Y - 1);
            }
            screenSizeEdit = false;
        }

        private void tbWidth_Leave(object sender, EventArgs e)
        {
            Application.DoEvents();
            screenSizeEdit = true; //So the Resize event won't trigger
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (sizeScreen.X > widthTb)
            {
                this.Size = new Size(widthTb + 24, heightTb + 71);
            }
            else
            {
                this.Size = new Size(sizeScreen.X - 1, heightTb + 71);
            }
            screenSizeEdit = false;
        }

        private void tbWidth_KeyDown(object sender, KeyEventArgs e) //Enter press
        {
            Application.DoEvents();
            if (e.KeyData == Keys.Enter)
            {
                screenSizeEdit = true;
                int heightTb = Convert.ToInt32(tbHeight.Text);
                int widthTb = Convert.ToInt32(tbWidth.Text);

                if (sizeScreen.Y > heightTb)
                {
                    this.Size = new Size(widthTb + 24, heightTb + 71);
                }
                else
                {
                    this.Size = new Size(widthTb + 24, sizeScreen.Y - 1);
                }
                screenSizeEdit = false;
            }
        }

        private void tbHeight_KeyDown(object sender, KeyEventArgs e)
        {
            Application.DoEvents();
            if (e.KeyData == Keys.Enter)
            {
                screenSizeEdit = true; //So the Resize event won't trigger
                int heightTb = Convert.ToInt32(tbHeight.Text);
                int widthTb = Convert.ToInt32(tbWidth.Text);

                if (sizeScreen.X > widthTb)
                {
                    this.Size = new Size(widthTb + 24, heightTb + 71);
                }
                else
                {
                    this.Size = new Size(sizeScreen.X - 1, heightTb + 71);
                }
                screenSizeEdit = false;
            }
        }

        #endregion

        #region TitleBar Buttons

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
                btnMaximize.Image = Resources.MaximizePlus;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                btnMaximize.Image = Resources.MaximizeMinus;
            }
            
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion

        #region Timers

        private void timerPreStart_Tick(object sender, EventArgs e)
        {
            if (preStart >= 1)
            {
                labelTitle.Text = "Screen To Gif (" + preStart + Resources.TitleSecondsToGo;
                preStart--;
            }
            else
            {
                labelTitle.Text = "Screen To Gif ►";
                timerPreStart.Stop();
                if (cbShowCursor.Checked)
                {
                    //Cursor position
                    //cursor.Visible = true;
                    //posCursor = this.PointToClient(Cursor.Position);
                    //cursor.Location = posCursor;

                    //timerCursor.Start(); //Cursor position

                    stage = 1;
                    btnRecordPause.Enabled = true;
                    this.Invalidate(); //To redraw the form, activating OnPaint again
                    timerCapWithCursor.Start(); //Record with the cursor
                }
                else
                {
                    stage = 1;
                    btnRecordPause.Enabled = true;
                    this.Invalidate(); //To redraw the form, activating OnPaint again
                    timerCapture.Start(); //Frame recording without the cursor
                }

            }
        }

        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + 12, this.Location.Y + 34);

            //Take a screenshot of the area.
            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            listBitmap.Add(bt.GetHbitmap());
        }

        private void timerCapWithCursor_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + 12, this.Location.Y + 34);

            //Take a screenshot of the area.
            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            listBitmap.Add(bt.GetHbitmap());

            cursorInfo = new CursorInfo
            {
                Icon = capture.CaptureIconCursor(ref posCursor),
                Position = panelTransparent.PointToClient(posCursor)
            };

            //Get actual icon of the cursor

            listCursor.Add(cursorInfo);
        }

        #endregion

        #region Frame Edit Stuff

        private List<IntPtr> listFramesPrivate;
        private List<IntPtr> listFramesUndoAll;
        private List<IntPtr> listFramesUndo;

        private List<CursorInfo> listCursorPrivate;
        private List<CursorInfo> listCursorUndoAll;
        private List<CursorInfo> listCursorUndo;

        private void EditFrames()
        {
            listFramesPrivate = new List<IntPtr>(listBitmap);
            listFramesUndoAll = new List<IntPtr>(listBitmap);
            listFramesUndo = new List<IntPtr>(listBitmap);

            if (cbShowCursor.Checked) //Cursor
            {
                listCursorPrivate = new List<CursorInfo>(listCursor);
                listCursorUndoAll = new List<CursorInfo>(listCursor);
                listCursorUndo = new List<CursorInfo>(listCursor);
            }
            Application.DoEvents();

            frameEdit = true;
            panelEdit.Visible = true;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            trackBar.Value = 0;
            this.MinimumSize = new Size(543, 308);
            labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            #region Window size
            Bitmap bitmap = Bitmap.FromHbitmap(listFramesPrivate[0]);

            Size sizeBitmap = new Size(bitmap.Size.Width + 80, bitmap.Size.Height + 170);

            if (!(sizeBitmap.Width > this.MinimumSize.Width))
            {
                sizeBitmap.Width = this.MinimumSize.Width;
            }

            if (!(sizeBitmap.Height > this.MinimumSize.Height))
            {
                sizeBitmap.Height = this.MinimumSize.Height;
            }

            this.Size = sizeBitmap;
            #endregion

            pictureBitmap.Image = bitmap;

            
            //if (cbShowCursor.Checked)
            //{
            //    CursorInfo cursorAux = listCursor[0];
            //    grEdit = Graphics.FromImage(bitmap);

            //    rect = new Rectangle(cursorAux.Position.X, cursorAux.Position.Y, cursorAux.Icon.Width, cursorAux.Icon.Height);

            //    grEdit.DrawIcon(cursorAux.Icon, rect);
            //    grEdit.Flush();
            //}
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            listBitmap = listFramesPrivate;

            if (cbShowCursor.Checked)
            listCursor = listCursorPrivate;

            panelEdit.Visible = false;
            frameEdit = false;
            labelTitle.Text = "Screen To Gif - Prompt to Save";
            Save();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            panelEdit.Visible = false;
            frameEdit = false;
            Save();
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo = new List<IntPtr>(listFramesPrivate);

                if (cbShowCursor.Checked)
                listCursorUndo = new List<CursorInfo>(listCursorPrivate);

                listFramesPrivate.RemoveAt(trackBar.Value);

                if (cbShowCursor.Checked)
                listCursorPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show("You can't delete the last frame.", "Minimum Amount of Frames", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndoOne_Click(object sender, EventArgs e)
        {
            listFramesPrivate = listFramesUndo;

            if (cbShowCursor.Checked)
            listCursorPrivate = listCursorUndo;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            btnUndoOne.Enabled = false;
        }

        private void btnUndoAll_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;

            listFramesUndo = new List<IntPtr>(listFramesPrivate); //To undo one

            if (cbShowCursor.Checked)
            listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            listFramesPrivate = listFramesUndoAll;

            if (cbShowCursor.Checked)
            listCursorPrivate = listCursorUndoAll;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            btnUndoAll.Enabled = false;
        }

        private void nenuDeleteAfter_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            listFramesUndo = new List<IntPtr>(listFramesPrivate);

            if (cbShowCursor.Checked)
            listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            if (listFramesPrivate.Count > 1)
            {
                int countList = listFramesPrivate.Count - 1; //So we have a fixed value

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    listFramesPrivate.RemoveAt(i);

                    if (cbShowCursor.Checked)
                    listCursorPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = listFramesPrivate.Count - 1;
                pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void menuDeleteBefore_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            listFramesUndo = new List<IntPtr>(listFramesPrivate);

            if (cbShowCursor.Checked)
            listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            if (listFramesPrivate.Count > 1)
            {
                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere

                    if (cbShowCursor.Checked)
                    listCursorPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
                labelTitle.Text = "Screen To Gif - Editor: Frame " + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        #endregion

        #region ComboBoxes Events

        private Keys getKeys(string name)
        {
            var keysSelected = new Keys();

            #region Switch Case Keys
            switch (name)
            {
                case "F1":
                    keysSelected = Keys.F1;
                    break;
                case "F2":
                    keysSelected = Keys.F2;
                    break;
                case "F3":
                    keysSelected = Keys.F3;
                    break;
                case "F4":
                    keysSelected = Keys.F4;
                    break;
                case "F5":
                    keysSelected = Keys.F5;
                    break;
                case "F6":
                    keysSelected = Keys.F6;
                    break;
                case "F7":
                    keysSelected = Keys.F7;
                    break;
                case "F8":
                    keysSelected = Keys.F8;
                    break;
                case "F9":
                    keysSelected = Keys.F9;
                    break;
                case "F10":
                    keysSelected = Keys.F10;
                    break;
                case "F11":
                    keysSelected = Keys.F11;
                    break;
                case "F12":
                    keysSelected = Keys.F12;
                    break;
            }
            #endregion

            return keysSelected;
        }

        private void comboStopKey_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStopKey.Text.Equals(comboStartPauseKey.Text))
            {
                comboStopKey.Text = Properties.Settings.Default.STstopKey.ToString();
            }
        }

        private void comboStartPauseKey_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStartPauseKey.Text.Equals(comboStopKey.Text))
            {
                comboStartPauseKey.Text = Properties.Settings.Default.STstartPauseKey.ToString();
            }
        }

        #endregion

        #region Gif Config Page

        private void trackBarQuality_Scroll(object sender, EventArgs e)
        {
            labelQuality.Text = (-(trackBarQuality.Value - 20)).ToString();

            if (trackBarQuality.Value >= 11)
            {
                labelQuality.ForeColor = Color.FromArgb(62, 91, 210);
            }
            else if (trackBarQuality.Value <= 9)
            {
                labelQuality.ForeColor = Color.DarkGoldenrod;
            }
            else if (trackBarQuality.Value == 10)
            {
                labelQuality.ForeColor = Color.FromArgb(0, 0, 0);
            }
            //Converts 1 to 20 and 20 to 1 (because the quality scroll is inverted, 1 better images, 20 worst)
            //But for the user, 20 is the best, so I need to invert the value.
        }

        #endregion



    }
}
