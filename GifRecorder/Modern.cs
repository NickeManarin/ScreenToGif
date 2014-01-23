using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
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
using ScreenToGif.Pages;
using ScreenToGif.Properties;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ScreenToGif
{
    public partial class Modern : Form
    {
        private const int CS_DROPSHADOW = 0x00020000;
        public const int WM_NCHITTEST = 0x84;

        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        readonly CaptureScreen capture = new CaptureScreen();

        private UserActivityHook actHook;
        private Point sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Size lastSize; //The editor may increase the size of the form, use this to go back to the last size
        private bool screenSizeEdit = false;
        private int preStart = 1;
        private string outputpath;
        private int stage = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding

        private List<Bitmap> listBitmap;
        private List<CursorInfo> listCursor = new List<CursorInfo>(); //List that stores the icon

        private CursorInfo cursorInfo;
        private Rectangle rect;

        static Point lastClick;
        private Bitmap bt;
        private Graphics gr;
        private Point posCursor;
        private Thread workerThread;

        private bool _isPageGifOpen;
        private bool _isPageAppOpen;
        private bool _isPageInfoOpen;

        #region Native

        //[DllImport("user32.dll")]
        //private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        //[DllImport("gdi32.dll")]
        //private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        //[DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        //private static extern bool DeleteObject(System.IntPtr hObject);

        //For the hit test area
        private const int cGrip = 20;
        private const int cCaption = 35;
        private const int cBorder = 7;

        /// <summary>
        /// Indicates the position of the cursor hot spot.
        /// </summary>
        public enum HitTest : int
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

        public Modern()
        {
            InitializeComponent();

            #region Load Save Data

            //Gets and sets the fps
            numMaxFps.Value = Properties.Settings.Default.STmaxFps;

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
            actHook.Start(false, true);
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
            //ControlPaint.DrawSizeGrip(e.Graphics, this.BackColor, rc);

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

        private readonly Control info = new Information();
        private readonly Control appSettings = new AppSettings(false); //false = modern, true = legacy
        private readonly Control gifSettings = new GifSettings();

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnMaximize.Enabled = true;
            btnMinimize.Enabled = true;

            Stop();
        }

        private void btnRecordPause_Click(object sender, EventArgs e)
        {
            panelTransparent.Controls.Clear();
            btnMaximize.Enabled = false;
            btnMinimize.Enabled = false;

            RecordPause();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_isPageAppOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                _isPageAppOpen = false;
            }
            else
            {
                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(appSettings);
                panelTransparent.Visible = true;
                appSettings.Dock = DockStyle.Fill;

                _isPageAppOpen = true;
                _isPageGifOpen = false;
                _isPageInfoOpen = false;
            }
        }

        private void btnGifConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_isPageGifOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                _isPageGifOpen = false;
            }
            else
            {
                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(gifSettings);
                panelTransparent.Visible = true;
                gifSettings.Dock = DockStyle.Fill;

                _isPageInfoOpen = false;
                _isPageAppOpen = false;
                _isPageGifOpen = true;
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_isPageInfoOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                _isPageInfoOpen = false;
                GC.Collect();
            }
            else
            {
                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(info);
                panelTransparent.Visible = true;
                info.Dock = DockStyle.Fill;

                _isPageAppOpen = false;
                _isPageGifOpen = false;
                _isPageInfoOpen = true;
                GC.Collect();
            }
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
                timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);
                timerCapWithCursor.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<Bitmap>(); //List that contains all the frames.
                listCursor = new List<CursorInfo>(); //List that contains all the icon information

                bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                gr = Graphics.FromImage(bt);

                labelTitle.Text = "Screen To Gif (2 " + Resources.TitleSecondsToGo;
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

            if (Settings.Default.STshowCursor) //If show cursor is true
            {
                this.Cursor = Cursors.WaitCursor;
                Graphics graph;
                int numImage = 0;
                foreach (var bitmap in listBitmap) //Change this to stop the hang time
                {
                    Application.DoEvents();
                    graph = Graphics.FromImage(bitmap);
                    rect = new Rectangle(listCursor[numImage].Position.X, listCursor[numImage].Position.Y, listCursor[numImage].Icon.Width, listCursor[numImage].Icon.Height);

                    graph.DrawIcon(listCursor[numImage].Icon, rect);
                    graph.Flush();
                    numImage++;
                }
                this.Cursor = Cursors.Arrow;
            }

            if (stage != 0 && stage != 3) //if not already stop or pre starting
            {
                if (Settings.Default.STallowEdit)
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
                    lastSize = this.Size;
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

                btnMaximize.Enabled = true;
                btnMinimize.Enabled = true;

                btnRecordPause.Image = Properties.Resources.Play_17Green;
                btnRecordPause.Text = Resources.btnRecordPause_Record;
                labelTitle.Text = Resources.TitleStoped;
                this.Invalidate();

                actHook.KeyDown += KeyHookTarget;
                actHook.Start(false, true);
            }
        }

        private void Save()
        {
            this.Cursor = Cursors.WaitCursor;
            this.Size = lastSize;
            if (!Settings.Default.STsaveLocation) // to choose the location to save the gif
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                this.Cursor = Cursors.Arrow;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    outputpath = sfd.FileName;

                    workerThread = new Thread(DoWork);
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }
                else
                {
                    flowPanel.Enabled = true;
                    stage = 0; //Stoped
                    numMaxFps.Enabled = true;
                    tbWidth.Enabled = true;
                    tbHeight.Enabled = true;
                    this.TopMost = true;
                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Play_17Green;
                    labelTitle.Text = Resources.TitleStoped;
                    this.MinimumSize = new Size(100, 100);
                    this.Invalidate();

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start(false, true);

                    return;
                }
            }
            else
            {
                #region Search For Filename

                bool searchForName = true;
                int numOfFile = 0;
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

                workerThread = new Thread(DoWork);
                workerThread.IsBackground = true;
                workerThread.Name = "Encoding";
                workerThread.Start();
            }

            this.Cursor = Cursors.Arrow;
            stage = 0; //Stoped
            this.MinimumSize = new Size(100, 100);
            numMaxFps.Enabled = true;
            tbHeight.Enabled = false;
            tbWidth.Enabled = false;
            this.TopMost = true;
            this.Text = Resources.TitleStoped;
            this.Invalidate();
        }

        private void DoWork() //Thread
        {
            if (Settings.Default.STencodingCustom)
            {
                #region Ngif encoding

                int numImage = 0;
                int countList = listBitmap.Count;

                using (encoder = new AnimatedGifEncoder())
                {
                    encoder.Start(outputpath);
                    encoder.SetQuality(Settings.Default.STquality);

                    encoder.SetRepeat(Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : -1); // 0 = Always, -1 once

                    encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));

                    foreach (var image in listBitmap)
                    {
                        numImage++;
                        try
                        {
                            this.Invoke((Action)delegate //Needed because it's a cross thread call.
                            {
                                this.labelTitle.Text = Resources.Title_Thread_ProcessingFrame + numImage + Resources.Title_Thread_out_of + countList + ")";
                            });
                        }
                        catch (Exception)
                        {
                        }
                        encoder.AddFrame(image);
                    }
                }

                #endregion
            }
            else
            {
                #region paint.NET encoding

                var imageArray = listBitmap.ToArray();

                var delay = 1000 / Convert.ToInt32(numMaxFps.Value);
                var repeat = (Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : 1); // 0 = Always, -1 once
                int countList = listBitmap.Count;

                using (var stream = new MemoryStream())
                {
                    using (var encoderNet = new GifEncoder(stream, null, null, repeat))
                    {
                        for (int i = 0; i < listBitmap.Count; i++)
                        {
                            encoderNet.AddFrame((listBitmap[i] as Bitmap).CopyImage(), 0, 0,
                                TimeSpan.FromMilliseconds(delay));

                            this.Invoke((Action)delegate //Needed because it's a cross thread call.
                            {
                                this.labelTitle.Text = Resources.Title_Thread_ProcessingFrame + i + Resources.Title_Thread_out_of + countList + ")";
                            });
                        }
                    }

                    stream.Position = 0;

                    using (
                        var fileStream = new FileStream(outputpath, FileMode.Create, FileAccess.Write, FileShare.None,
                            Constants.BufferSize, false))
                    {
                        stream.WriteTo(fileStream);
                    }
                }

                #endregion
            }

            //if (Settings.Default.STshowCursor)
            //{
            //    listCursorPrivate.Clear();
            //    listCursorUndoAll.Clear();
            //    listCursorUndo.Clear();

            //    listCursorPrivate = null;
            //    listCursorUndoAll = null;
            //    listCursorUndo = null;
            //}

            listFramesPrivate.Clear();
            listFramesUndo.Clear();
            listFramesUndoAll.Clear();

            listFramesPrivate = null;
            listFramesUndo = null;
            listFramesUndoAll = null;
            encoder = null;

            GC.Collect();

            try
            {
                this.Invoke((Action)delegate
                {
                    labelTitle.Text = Resources.Title_EncodingDone;
                    stage = 0;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Properties.Resources.Play_17Green;
                    flowPanel.Enabled = true;
                    this.TopMost = false;

                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start(false, true);
                });
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region TextBox Size

        private void tbSize_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) ||
                char.IsSymbol(e.KeyChar) ||
                char.IsWhiteSpace(e.KeyChar) ||
                char.IsPunctuation(e.KeyChar))
                e.Handled = true;
        } // TB SIZE

        private void tbSize_Leave(object sender, EventArgs e)
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

        private void tbSize_KeyDown(object sender, KeyEventArgs e) //Enter press
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
                labelTitle.Text = Resources.TitleRecording;
                timerPreStart.Stop();
                if (Settings.Default.STshowCursor)
                {
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
            listBitmap.Add((Bitmap)bt.CopyImage());
        }

        private void timerCapWithCursor_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + 12, this.Location.Y + 34);

            //Take a screenshot of the area.
            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            listBitmap.Add((Bitmap)bt.CopyImage());

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

        private List<Bitmap> listFramesPrivate;
        private List<Bitmap> listFramesUndoAll;
        private List<Bitmap> listFramesUndo;

        //private List<CursorInfo> listCursorPrivate;
        //private List<CursorInfo> listCursorUndoAll;
        //private List<CursorInfo> listCursorUndo;

        private void EditFrames()
        {
            listFramesPrivate = new List<Bitmap>(listBitmap);
            listFramesUndoAll = new List<Bitmap>(listBitmap);
            listFramesUndo = new List<Bitmap>(listBitmap);

            //if (Settings.Default.STshowCursor) //Cursor
            //{
            //    listCursorPrivate = new List<CursorInfo>(listCursor);
            //    listCursorUndoAll = new List<CursorInfo>(listCursor);
            //    listCursorUndo = new List<CursorInfo>(listCursor);
            //}

            Application.DoEvents();

            panelEdit.Visible = true;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            trackBar.Value = 0;
            this.MinimumSize = new Size(543, 308);
            labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            ResizeFormToImage();

            pictureBitmap.Image = listFramesPrivate.First();

            #region Preview Config.

            timerPlayPreview.Tick += timerPlayPreview_Tick;
            timerPlayPreview.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

            #endregion
        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            StopPreview();
            listBitmap = new List<Bitmap>(listFramesPrivate);

            //if (Settings.Default.STshowCursor)
            //    listCursor = listCursorPrivate;

            panelEdit.Visible = false;
            labelTitle.Text = Resources.Title_Edit_PromptToSave;
            Save();

            GC.Collect();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            StopPreview();
            panelEdit.Visible = false;
            Save();

            GC.Collect();
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            StopPreview();
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            StopPreview();
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                //if (Settings.Default.STshowCursor)
                //    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

                listFramesPrivate.RemoveAt(trackBar.Value);

                //if (Settings.Default.STshowCursor)
                //    listCursorPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndoOne_Click(object sender, EventArgs e)
        {
            StopPreview();
            listFramesPrivate.Clear();
            listFramesPrivate = new List<Bitmap>(listFramesUndo);

            //if (Settings.Default.STshowCursor)
            //    listCursorPrivate = listCursorUndo;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            btnUndo.Enabled = false;

            ResizeFormToImage();
        }

        private void btnUndoAll_Click(object sender, EventArgs e)
        {
            StopPreview();
            btnUndo.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate); //To undo one

            //if (Settings.Default.STshowCursor)
            //    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            listFramesPrivate = listFramesUndoAll;

            //if (Settings.Default.STshowCursor)
            //    listCursorPrivate = listCursorUndoAll;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            ResizeFormToImage();
        }

        private void nenuDeleteAfter_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            //if (Settings.Default.STshowCursor)
            //    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            if (listFramesPrivate.Count > 1)
            {
                int countList = listFramesPrivate.Count - 1; //So we have a fixed value

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    listFramesPrivate.RemoveAt(i);

                    //if (Settings.Default.STshowCursor)
                    //    listCursorPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void menuDeleteBefore_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            //if (Settings.Default.STshowCursor)
            //    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            if (listFramesPrivate.Count > 1)
            {
                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere

                    //if (Settings.Default.STshowCursor)
                    //    listCursorPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        /// <summary>
        /// Resizes the form to hold the image
        /// </summary>
        private void ResizeFormToImage()
        {
            #region Window size
            Bitmap bitmap = new Bitmap(listFramesPrivate[0]);

            Size sizeBitmap = new Size(bitmap.Size.Width + 80, bitmap.Size.Height + 160);

            if (!(sizeBitmap.Width > 550)) //550 minimum width
            {
                sizeBitmap.Width = 550;
            }

            if (!(sizeBitmap.Height > 300)) //300 minimum height
            {
                sizeBitmap.Height = 300;
            }

            this.Size = sizeBitmap;

            bitmap.Dispose();

            #endregion
        }

        private void exportFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfdExport = new SaveFileDialog();
            sfdExport.DefaultExt = "jpg";
            sfdExport.Filter = "JPG Image (*.jpg)|*.jpg";
            sfdExport.FileName = "Frame " + trackBar.Value;

            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                Bitmap expBitmap = listFramesPrivate[trackBar.Value];
                expBitmap.Save(sfdExport.FileName, ImageFormat.Jpeg);
                MessageBox.Show(Resources.Msg_Frame + trackBar.Value + Resources.Msg_Exported, Resources.Msg_ExportedTitle);
                expBitmap.Dispose();
            }
            sfdExport.Dispose();
        }

        private void resizeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            Bitmap bitmapResize = listFramesPrivate[trackBar.Value];

            Resize resize = new Resize(bitmapResize);
            resize.ShowDialog();

            if (resize.Accept)
            {
                Size resized = resize.GetSize();

                listFramesPrivate = ImageUtil.ResizeAllBitmap(listFramesPrivate, resized.Width, resized.Height);

                pictureBitmap.Image = listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            resize.Dispose();
        }

        private void cropAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            Crop crop = new Crop(listFramesPrivate[trackBar.Value]);
            crop.ShowDialog(this);

            if (crop.Accept)
            {
                listFramesPrivate = ImageUtil.Crop(listFramesPrivate, crop.Rectangle);

                pictureBitmap.Image = listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            crop.Dispose();
        }

        private void deleteThisFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                //if (Settings.Default.STshowCursor)
                //    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

                listFramesPrivate.RemoveAt(trackBar.Value);

                //if (Settings.Default.STshowCursor)
                //    listCursorPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void imageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)
            {
                Image openBitmap = Bitmap.FromFile(openImageDialog.FileName);

                Bitmap bitmapResized = ImageUtil.ResizeBitmap((Bitmap)openBitmap, listFramesPrivate[0].Size.Width,
                    listFramesPrivate[0].Size.Height);

                listFramesPrivate.Insert(trackBar.Value, bitmapResized);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void applyFiltersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filtersForm = new Filters(listFramesPrivate);
            if (filtersForm.ShowDialog(this) == DialogResult.OK)
            {
                btnUndo.Enabled = true;
                btnReset.Enabled = true;

                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                listFramesPrivate.Clear();
                listFramesPrivate = new List<Bitmap>(filtersForm.ListBitmap);

                pictureBitmap.Image = listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            filtersForm.Dispose();
        }

        private void revertOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                listFramesPrivate = ImageUtil.Revert(listFramesPrivate);

                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void yoyoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                listFramesPrivate = ImageUtil.Yoyo(listFramesPrivate);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        #endregion

        #region Play Preview

        System.Windows.Forms.Timer timerPlayPreview = new System.Windows.Forms.Timer();
        private int actualFrame = 0;

        private void pictureBitmap_Click(object sender, EventArgs e)
        {
            PlayPreview();
        }

        private void PlayPreview()
        {
            if (timerPlayPreview.Enabled)
            {
                timerPlayPreview.Stop();
                this.labelTitle.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
                btnPreview.Text = Resources.Con_PlayPreview;
            }
            else
            {
                this.labelTitle.Text = "Screen To Gif - Playing Animation";
                btnPreview.Text = Resources.Con_StopPreview;
                actualFrame = trackBar.Value;
                timerPlayPreview.Start();
            }

        }

        private void StopPreview()
        {
            timerPlayPreview.Stop();
            btnPreview.Text = Resources.Con_PlayPreview;
        }

        private void timerPlayPreview_Tick(object sender, EventArgs e)
        {
            pictureBitmap.Image = listFramesPrivate[actualFrame];
            trackBar.Value = actualFrame;

            if (listFramesPrivate.Count - 1 == actualFrame)
            {
                actualFrame = 0;
            }
            else
            {
                actualFrame++;
            }
        }

        private void trackBar_Enter(object sender, EventArgs e)
        {
            StopPreview();
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            StopPreview();
        }

        #endregion
    }
}
