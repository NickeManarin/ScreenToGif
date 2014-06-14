using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
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
using ScreenToGif.Util;
using Application = System.Windows.Forms.Application;
using FontStyle = System.Drawing.FontStyle;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace ScreenToGif
{
    /// <summary>
    /// Modern
    /// </summary>
    public partial class Modern : Form
    {
        #region Variables

        /// <summary>
        /// The animated gif encoder, this encodes the list of frames to a gif format.
        /// </summary>
        private AnimatedGifEncoder _encoder = new AnimatedGifEncoder();
        /// <summary>
        /// This object retrieves the icon of the cursor.
        /// </summary>
        readonly CaptureScreen _capture = new CaptureScreen();
        /// <summary>
        /// The object of the keyboard hook.
        /// </summary>
        private readonly UserActivityHook _actHook;
        /// <summary>
        /// The editor may increase the size of the form, use this to go back to the last size (The size before opening the editor).
        /// </summary>
        private Size _lastSize;
        /// <summary>
        /// To hold the update of the size of the form while typing in the size textBoxes.
        /// </summary>
        private bool _screenSizeEdit;
        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;
        /// <summary>
        /// The numbers of frames, this is updated while recording.
        /// </summary>
        private int _frameCount = 0;
        /// <summary>
        /// The output path of the recording.
        /// </summary>
        private string _outputpath;
        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        private Stage _stage = Stage.Stoped; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding
        /// <summary>
        /// The list of bitmaps recorded.
        /// </summary>
        private List<Bitmap> _listBitmap;
        /// <summary>
        /// The list of information about the cursor.
        /// </summary>
        private List<CursorInfo> _listCursor = new List<CursorInfo>(); //List that stores the icon
        /// <summary>
        /// The list of individual delays.
        /// </summary>
        private List<int> _listDelay = new List<int>();
        /// <summary>
        /// Object that holds the information of the cursor.
        /// </summary>
        private CursorInfo _cursorInfo;
        /// <summary>
        /// Stores the last position of the click, this is used in the drag of the window.
        /// </summary>
        static Point _lastClick;
        /// <summary>
        /// Holds the position of the cursor.
        /// </summary>
        private Point _posCursor;
        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private Point _sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Bitmap _bt;
        private Graphics _gr;

        /// <summary>
        /// The encode worker thread.
        /// </summary>
        private Thread _workerThread;

        /// <summary>
        /// The amount of pixels of the window border. Width.
        /// </summary>
        private int _offsetX;

        /// <summary>
        /// The amout of pixels of the window border. Height.
        /// </summary>
        private int _offsetY;

        /// <summary>
        /// The point position on the bitmap, used to insert text.
        /// </summary>
        private Point _pointTextPosition;

        /// <summary>
        /// The text of the "All" label of the TreeView.
        /// </summary>
        private string _parentNodeLabel = "All"; //Localize.

        #region Page Flags

        /// <summary>
        /// Tells if the page "Gif Settings" is open or not
        /// </summary>
        private bool _isPageGifOpen;
        /// <summary>
        /// Tells if the page "App Settings" is open or not
        /// </summary>
        private bool _isPageAppOpen;
        /// <summary>
        /// Tells if the page "Information" is open or not
        /// </summary>
        private bool _isPageInfoOpen;

        #endregion

        private enum Stage : int
        {
            Stoped = 0,
            Recording = 1,
            Paused = 2,
            PreStarting = 3,
            Editing = 4,
            Encoding = 5
        };

        private enum ActionEnum : int
        {
            Grayscale = 0,
            Pixelate,
            Blur,
            Negative,
            Transparency,
            Sepia,
            Delete //Add more
        };

        #endregion

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

        /// <summary>
        /// Default constructor of the form Modern.
        /// </summary>
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
            tbHeight.Text = panelTransparent.Height.ToString();
            tbWidth.Text = panelTransparent.Width.ToString();

            //Performance and flickering tweaks
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
                _actHook.Start(false, true);
            }
            catch (Exception){}

            #endregion
        }

        #region Override

        /// <summary>
        /// System's constant used to define a drop shadow of the form.
        /// </summary>
        private const int CS_DROPSHADOW = 0x00020000;
        /// <summary>
        /// System's constant that defines the hit test of the form.
        /// </summary>
        public const int WM_NCHITTEST = 0x84;

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
            var rectOutside = new Rectangle(new Point(0, 0), new Size(this.Width - 1, this.Height - 1));
            var rectInside = new Rectangle(new Point(panelTransparent.Left - 1, panelTransparent.Top - 1), new Size(panelTransparent.Width + 1, panelTransparent.Height + 1));

            Pen pen;

            if (_stage == Stage.Recording)
            {
                pen = new Pen(Color.FromArgb(255, 25, 0));
            }
            else if (_stage == Stage.Editing)
            {
                pen = new Pen(Color.FromArgb(30, 180, 30));
            }
            else
            {
                pen = new Pen(Color.FromArgb(0, 151, 251));
            }

            g.DrawRectangle(pen, rectOutside);
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
            if (_stage == Stage.Stoped || _stage == Stage.Editing)
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

        /// <summary>
        /// Process the Key events, such as pressing. This handles the keyboard shortcuts.
        /// </summary>
        /// <param name="msg">A Message, passed by reference, that represents the window message to process. </param>
        /// <param name="keyData">One of the Keys values that represents the key to process. </param>
        /// <returns><code>true</code> if the character was processed by the control; otherwise, <code>false</code>.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //If not in the editor page.
            if (_stage != Stage.Editing)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            switch (keyData)
            {
                case Keys.Delete: //Delete this frame
                    con_deleteThisFrame_Click(null, null);
                    return true;
                case Keys.Control | Keys.G: //Show grid
                    con_showGrid.Checked = !con_showGrid.Checked; //Inverse the checkState.
                    con_showGrid_CheckedChanged(null, null);
                    return true;
                case Keys.Right: //Next frame
                    #region Move to the right
                    if (trackBar.Value == trackBar.Maximum)
                    {
                        trackBar.Value = 0;
                    }
                    else
                    {
                        trackBar.Value = trackBar.Value + 1;
                    }
                    #endregion
                    return true;
                case Keys.Left: //Previous frame
                    #region Move to the Left
                    if (trackBar.Value == 0)
                    {
                        trackBar.Value = trackBar.Maximum;
                    }
                    else
                    {
                        trackBar.Value = trackBar.Value - 1;
                    }
                    #endregion
                    return true;
                case Keys.Alt | Keys.Right: //Delete everything after this
                    con_deleteAfter_Click(null, null);
                    return true;
                case Keys.Alt | Keys.Left: //Delete everything before this
                    con_deleteBefore_Click(null, null);
                    return true;
                case Keys.Control | Keys.E: //Export Frame
                    con_exportFrame_Click(null, null);
                    return true;
                case Keys.Control | Keys.T: //Export Frame
                    con_addText_Click(null, null);
                    return true;
                case Keys.Escape: //Cancel
                    btnCancel_Click(null, null);
                    return true;
                case Keys.Enter: //Done
                    btnDone_Click(null, null);
                    return true;
                case Keys.Alt | Keys.D: //Show the Delay Context
                    #region Show Delay

                    contextDelay.Show(lblDelay, 0, lblDelay.Height);
                    con_tbDelay.Text = _delay.ToString();
                    con_tbDelay.Focus();

                    #endregion
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
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

        #region Main Form Move/Resize /Closing

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _lastClick = new Point(e.X, e.Y); //We'll need this for when the Form starts to move
            }

            Application.DoEvents();
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            //Application.DoEvents();
            if (e.Button == MouseButtons.Left)
            {
                //Move the Form the same difference the mouse cursor moved;
                this.Left += e.X - _lastClick.X;
                this.Top += e.Y - _lastClick.Y;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (!_screenSizeEdit)
            {
                tbHeight.Text = panelTransparent.Height.ToString();
                tbWidth.Text = panelTransparent.Width.ToString();
            }

            if (this.WindowState == FormWindowState.Normal)
                btnMaximize.Image = Properties.Resources.MaximizeMinus;
        }

        /// <summary>
        /// Before close, all settings must be saved and the timer must be disposed.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Default.STmaxFps = Convert.ToInt32(numMaxFps.Value);
            Settings.Default.STsize = new Size(this.Size.Width, this.Size.Height);

            Settings.Default.Save();

            try
            {
                _actHook.Stop(); //Stop the keyboard watcher.
            }
            catch (Exception){}

            if (_stage != (int)Stage.Stoped)
            {
                timerCapture.Stop();
                timerCapture.Dispose();

                timerCapWithCursor.Stop();
                timerCapWithCursor.Dispose();
            }
        }

        /// <summary>
        /// When the Text property changes, set the text to the labelTitle.
        /// </summary>
        private void Modern_TextChanged(object sender, EventArgs e)
        {
            labelTitle.Text = this.Text;
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
            this.TransparencyKey = Color.LimeGreen;
            panelTransparent.BackColor = Color.LimeGreen;
            panelTransparent.Controls.Clear(); //Removes all pages from the top

            btnMaximize.Enabled = false;
            btnMinimize.Enabled = false;

            RecordPause(); //And start the pre-start tick
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_isPageAppOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;

                _isPageAppOpen = false;
            }
            else
            {
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);

                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(appSettings);
                panelTransparent.Visible = true;
                appSettings.Dock = DockStyle.Fill;

                _isPageAppOpen = true;
                _isPageGifOpen = false;
                _isPageInfoOpen = false;

                this.TransparencyKey = Color.Empty;
            }
        }

        private void btnGifConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_isPageGifOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;

                _isPageGifOpen = false;
            }
            else
            {
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);

                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(gifSettings);
                panelTransparent.Visible = true;
                gifSettings.Dock = DockStyle.Fill;

                _isPageInfoOpen = false;
                _isPageAppOpen = false;
                _isPageGifOpen = true;

                this.TransparencyKey = Color.Empty;
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_isPageInfoOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;

                _isPageInfoOpen = false;
                GC.Collect();
            }
            else
            {
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);

                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(info);
                panelTransparent.Visible = true;
                info.Dock = DockStyle.Fill;

                _isPageAppOpen = false;
                _isPageGifOpen = false;
                _isPageInfoOpen = true;
                GC.Collect();

                this.TransparencyKey = Color.Empty;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Settings.Default.STstartPauseKey)
            {
                btnRecordPause_Click(null, null);
            }
            else if (e.KeyCode == Settings.Default.STstopKey)
            {
                btnStop_Click(null, null);
            }
        }

        /// <summary>
        /// MouseHook event method, not implemented.
        /// </summary>
        private void MouseHookTarget(object sender, System.Windows.Forms.MouseEventArgs keyEventArgs)
        {
            //e.X, e.Y
        }

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void RecordPause()
        {
            if (_stage == Stage.Stoped) //if stoped, starts recording
            {
                #region To Record

                timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);
                timerCapWithCursor.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                _listBitmap = new List<Bitmap>(); //List that contains all the frames.
                _listCursor = new List<CursorInfo>(); //List that contains all the icon information

                _bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                _gr = Graphics.FromImage(_bt);

                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;

                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                numMaxFps.Enabled = false;
                this.TopMost = true;

                if (Settings.Default.STpreStart) //if should show the pre start countdown
                {
                    this.Text = "Screen To Gif (2" + Resources.TitleSecondsToGo;
                    btnRecordPause.Enabled = false;

                    _stage = Stage.PreStarting;
                    numMaxFps.Enabled = false;
                    _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                    timerPreStart.Start();
                }
                else
                {
                    //this.Text = Resources.TitleRecording;
                    _stage = Stage.Recording;
                    btnRecordPause.Enabled = true;

                    if (Settings.Default.STshowCursor) //if show cursor
                    {
                        //To start recording right away, I call the tick before starting the timer,
                        //because the first tick will only occur after the delay.
                        timerCapWithCursor_Tick(null, null);
                        timerCapWithCursor.Start();
                    }
                    else
                    {
                        timerCapture_Tick(null, null);
                        timerCapture.Start();
                    }
                }

                this.Invalidate();

                #endregion
            }
            else if (_stage == Stage.Recording) //if recording, pauses
            {
                #region To Pause

                this.Text = Resources.TitlePaused;
                btnRecordPause.Text = Resources.btnRecordPause_Continue;
                btnRecordPause.Image = Properties.Resources.Record;
                _stage = Stage.Paused;
                this.Invalidate(); //To redraw the form, activating OnPaint again

                if (Settings.Default.STshowCursor) //if show cursor
                {
                    timerCapWithCursor.Enabled = false;
                }
                else
                {
                    timerCapture.Enabled = false;
                }

                #endregion
            }
            else if (_stage == Stage.Paused) //if paused, starts recording again
            {
                #region To Record Again

                this.Text = Resources.TitleRecording;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                _stage = Stage.Recording;
                this.Invalidate(); //To redraw the form, activating OnPaint again

                if (Settings.Default.STshowCursor) //if show cursor
                {
                    timerCapWithCursor.Enabled = true;
                }
                else
                {
                    timerCapture.Enabled = true;
                }

                #endregion
            }
        }

        /// <summary>
        /// Stops the recording or stops the pre-start timer.
        /// </summary>
        private void Stop()
        {
            try
            {
                try
                {
                    _actHook.Stop(); //Stops the hook.
                    _actHook.KeyDown -= KeyHookTarget; //Removes the event.
                }
                catch (Exception){}

                timerCapture.Stop();
                timerCapWithCursor.Stop();

                if (_stage != (int)Stage.Stoped && _stage != Stage.PreStarting) //if not already stoped or pre starting, stops
                {
                    #region To Stop and Save

                    if (Settings.Default.STshowCursor) //If show cursor is true, merge all bitmaps.
                    {
                        #region Merge Cursor and Bitmap

                        this.Cursor = Cursors.WaitCursor;
                        Graphics graph;
                        int numImage = 0;

                        Application.DoEvents();

                        try
                        {
                            foreach (var bitmap in _listBitmap) //Change this to stop the hang time
                            {
                                graph = Graphics.FromImage(bitmap);
                                var rect = new Rectangle(_listCursor[numImage].Position.X, _listCursor[numImage].Position.Y, _listCursor[numImage].Icon.Width, _listCursor[numImage].Icon.Height);

                                graph.DrawIcon(_listCursor[numImage].Icon, rect);
                                graph.Flush();
                                numImage++;
                            }
                        }
                        catch (Exception){}

                        this.Cursor = Cursors.Default;

                        #endregion
                    }

                    #region Creates the Delay

                    this.Cursor = Cursors.WaitCursor;
                    _listDelay = new List<int>();

                    int delayGlobal = 1000 / (int)numMaxFps.Value;
                    foreach (var item in _listBitmap)
                    {
                        _listDelay.Add(delayGlobal);
                    }
                    this.Cursor = Cursors.Default;

                    #endregion

                    if (Settings.Default.STallowEdit) //If the user wants to edit the frames.
                    {
                        _lastSize = this.Size; //To return back to the last form size after the editor
                        _stage = Stage.Editing;
                        this.Invalidate();
                        btnMaximize.Enabled = true;
                        btnMinimize.Enabled = true;

                        EditFrames();

                        ShowHideButtons(true);
                    }
                    else
                    {
                        _lastSize = this.Size; //Not sure why this is here
                        Save();
                    }

                    #endregion
                }
                else if (_stage == Stage.PreStarting) //if pre starting, stops
                {
                    #region To Stop

                    timerPreStart.Stop();
                    _stage = Stage.Stoped;

                    //Enables the controls that are disabled while recording;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Enabled = true;
                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    this.Text = Resources.TitleStoped;
                    this.Invalidate();

                    try
                    {
                        //Re-starts the keyboard hook.
                        _actHook.KeyDown += KeyHookTarget;
                        _actHook.Start(false, true);
                    }
                    catch (Exception){}

                    #endregion
                }
            }
            catch (NullReferenceException nll)
            {
                MessageBox.Show(nll.Message, "NullReference", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogWriter.Log(nll, "NullPointer in the Stop function");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogWriter.Log(ex, "Error in the Stop function");
            }

        }

        /// <summary>
        /// Prepares the recorded frames to be saved/edited
        /// </summary>
        private void Save()
        {
            this.Cursor = Cursors.WaitCursor;

            this.Size = _lastSize;
            this.Invalidate();
            Application.DoEvents();

            if (!Settings.Default.STsaveLocation) // to choose the location to save the gif
            {
                #region If Not Save Directly to the Desktop

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                this.Cursor = Cursors.Default;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _outputpath = sfd.FileName;

                    _workerThread = new Thread(DoWork);
                    _workerThread.IsBackground = true;
                    _workerThread.Start();
                }
                else //if user don't want to save the gif
                {
                    FinishState();

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    this.Text = Resources.TitleStoped;
                    this.Invalidate();

                    try
                    {
                        _actHook.KeyDown += KeyHookTarget;
                        _actHook.Start(false, true);
                    }
                    catch (Exception){}

                    return;
                }

                #endregion
            }
            else
            {
                #region Search For Filename

                bool searchForName = true;
                int numOfFile = 0;

                string path;

                //if there is no defined save location, saves in the desktop.
                if (!Settings.Default.STfolder.Equals(""))
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                }
                else
                {
                    path = Settings.Default.STfolder;
                }

                this.Cursor = Cursors.Default;

                #region Ask if should encode

                DialogResult ask = MessageBox.Show(this, "Do you want to encode the animation? Saving location: " + path, "Screen To Gif",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question); //LOCALIZE

                //Only saves the recording if the user wants to.
                if (ask != DialogResult.Yes)
                {
                    //If the user don't want to save the recording.
                    FinishState();

                    try
                    {
                        //Restart the keyhook.
                        _actHook.KeyDown += KeyHookTarget;
                        _actHook.Start(false, true);
                    }
                    catch (Exception){}

                    return;
                }

                #endregion

                while (searchForName)
                {
                    if (!File.Exists(path + "\\Animation " + numOfFile + ".gif"))
                    {
                        _outputpath = path + "\\Animation " + numOfFile + ".gif";
                        searchForName = false;
                    }
                    else
                    {
                        if (numOfFile > 999)
                        {
                            searchForName = false;
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                _outputpath = saveFileDialog.FileName;
                            }
                            else
                            {
                                _outputpath = "No filename for you.gif";
                            }
                        }
                        numOfFile++;
                    }
                }
                #endregion

                _workerThread = new Thread(DoWork);
                _workerThread.IsBackground = true;
                _workerThread.Name = "Gif Encoding";
                _workerThread.Start();
            }

            //FinishState();
        }

        /// <summary>
        /// Do all the work to set the controls to the finished state. (i.e. Finished encoding)
        /// </summary>
        private void FinishState()
        {
            this.Cursor = Cursors.Default;
            flowPanel.Enabled = true;
            _stage = Stage.Stoped;
            this.MinimumSize = new Size(100, 100);
            this.Size = _lastSize;
            numMaxFps.Enabled = true;
            tbHeight.Enabled = true;
            tbWidth.Enabled = true;
            btnMaximize.Enabled = true;
            btnMinimize.Enabled = true;

            btnRecordPause.Text = Resources.btnRecordPause_Record;
            btnRecordPause.Image = Properties.Resources.Record;
            this.Text = Resources.TitleStoped;
            this.Invalidate(this.DisplayRectangle);
        }

        /// <summary>
        /// Thread method that encodes the list of frames.
        /// </summary>
        private void DoWork()
        {
            int countList = _listBitmap.Count;

            #region Show Processing

            var processing = new Processing();

            this.Invoke((Action)delegate //Needed because it's a cross thread call.
            {
                Control ctrlParent = panelTransparent;

                panelTransparent.Controls.Add(processing);
                processing.Dock = DockStyle.Fill;
                processing.SetMaximumValue(countList);

                this.Text = "Screen To Gif - " + Resources.Label_Processing;

                //Only set the dispose of the Processing page, if needed.
                if (Settings.Default.STshowFinished)
                {
                    processing.Disposed += processing_Disposed;
                }

                processing.SetStatus(0);

            });

            #endregion

            if (Settings.Default.STencodingCustom)
            {
                #region Ngif encoding

                int numImage = 0;

                #region Paint Unchanged Pixels

                if (Settings.Default.STpaintTransparent)
                {
                    this.Invoke((Action)(delegate { processing.SetPreEncoding(Resources.Label_AnalizingUnchanged); }));

                    ImageUtil.PaintTransparent(_listBitmap, Settings.Default.STtransparentColor);

                    //List<FrameInfo> ListToEncode = ImageUtil.PaintTransparent(_listBitmap, Settings.Default.STtransparentColor);
                }

                #endregion

                using (_encoder = new AnimatedGifEncoder())
                {
                    _encoder.Start(_outputpath);
                    _encoder.SetQuality(Settings.Default.STquality);

                    if (Settings.Default.STpaintTransparent)
                    {
                        _encoder.SetTransparent(Settings.Default.STtransparentColor);
                        _encoder.SetDispose(1); //Undraw Method, "Leave".
                    }

                    _encoder.SetRepeat(Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : -1); // 0 = Always, -1 once

                    this.Invoke((Action)(delegate { processing.SetEncoding(Resources.Label_Processing); }));

                    try
                    {
                        foreach (var image in _listBitmap)
                        {
                            _encoder.SetDelay(_listDelay[numImage]);
                            _encoder.AddFrame(image);
                            numImage++;

                            this.BeginInvoke((Action)(() => processing.SetStatus(numImage)));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Ngif encoding.");
                        //Show a message, maybe?
                    }

                }

                #endregion
            }
            else
            {
                #region paint.NET encoding

                //BUG: The minimum amount of iterations is -1 (no repeat) or 3 (repeat number being 2), if you set repeat as 1, it will repeat 2 times, instead of just 1.
                //0 = Always, -1 = no repeat, n = repeat number (first shown + repeat number = total number of iterations)
                var repeat = (Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : -1); // 0 = Always, -1 once

                using (var stream = new MemoryStream())
                {
                    using (var encoderNet = new GifEncoder(stream, null, null, repeat))
                    {
                        for (int i = 0; i < _listBitmap.Count; i++)
                        {
                            encoderNet.AddFrame((_listBitmap[i]).CopyImage(), 0, 0, TimeSpan.FromMilliseconds(_listDelay[i]));

                            this.BeginInvoke((Action)(() => processing.SetStatus(i)));
                        }
                    }

                    stream.Position = 0;

                    try
                    {
                        using (var fileStream = new FileStream(_outputpath, FileMode.Create, FileAccess.Write, FileShare.None,
                        Constants.BufferSize, false))
                        {
                            stream.WriteTo(fileStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Error while writing to disk");
                    }
                }

                #endregion
            }

            #region Finish

            if (Settings.Default.STshowFinished)
            {
                this.Invoke((Action)delegate
                {
                    processing.SetFinishedState(_outputpath, Resources.btnDone + "!");
                });

                //After the user hits "Close", the processing_Disposed is called. To set to the right stage.
            }
            else
            {
                this.Text = Resources.Title_EncodingDone;
                _stage = Stage.Stoped;

                this.Invalidate();

                FinishState();

                try
                {
                    _actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
                    _actHook.Start(false, true); //start again the keyboard hook watcher
                }
                catch (Exception){}

            }

            #endregion

            #region Memory Clearing

            _listBitmap.Clear();
            _listDelay.Clear();

            if (_listFramesPrivate != null)
            {
                _listFramesPrivate.Clear();
                _listFramesUndo.Clear();

                _listDelayPrivate.Clear();
                _listDelayUndo.Clear();
            }

            _listBitmap = null;
            _listFramesPrivate = null;
            _listFramesUndo = null;

            _listDelay = null;
            _listDelayPrivate = null;
            _listDelayUndo = null;
            _encoder = null;

            GC.Collect(); //call the garbage colector to empty the memory

            #endregion
        }

        /// <summary>
        /// Resets the state of the program, after closing the "Finished" page.
        /// </summary>
        private void processing_Disposed(object sender, EventArgs e)
        {
            this.Text = Resources.Title_EncodingDone;
            _stage = Stage.Stoped;

            this.Invalidate();

            FinishState();

            try
            {
                _actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
                _actHook.Start(false, true); //start again the keyboard hook watcher
            }
            catch (Exception){}
        }

        /// <summary>
        /// Insert text in the picture with specific font and color.
        /// </summary>
        /// <param name="text">Content to insert</param>
        /// <param name="font">Font of the text</param>
        /// <param name="foreColor">Color of the text</param>
        public void InsertText(String text, Font font, Color foreColor)
        {
            Brush textBrush = new SolidBrush(foreColor);
            Bitmap currentBitmap = new Bitmap(_listFramesPrivate[trackBar.Value]);
            Graphics myGraphic = Graphics.FromImage(currentBitmap);

            // Define the rectangle size by taking in consideration [X] and [Y] of 
            // [_pointTextPosition] so the text matches the Bitmap l
            Size rectangleSize = new Size(currentBitmap.Width - _pointTextPosition.X,
                currentBitmap.Height - _pointTextPosition.Y);

            // Insert text in the specified Point
            myGraphic.DrawString(text, font, textBrush, new Rectangle(_pointTextPosition,
                rectangleSize), new StringFormat());

            _listFramesPrivate[trackBar.Value] = new Bitmap(currentBitmap);

            // Refresh to display current change
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
        }

        /// <summary>
        /// Show or hide the flowPanel buttons.
        /// </summary>
        /// <param name="isEdit">True if Editor is openning</param>
        private void ShowHideButtons(bool isEdit)
        {
            #region Enable/Disable Buttons

            btnStop.Visible = !isEdit;
            btnRecordPause.Visible = !isEdit;
            tbHeight.Visible = !isEdit;
            lblX.Visible = !isEdit;
            tbWidth.Visible = !isEdit;
            lblSize.Visible = !isEdit;
            numMaxFps.Visible = !isEdit;
            lblFps.Visible = !isEdit;
            pbSeparator.Visible = !isEdit;
            btnConfig.Visible = !isEdit;

            //inverted

            btnNext.Visible = isEdit;
            btnPrevious.Visible = isEdit;
            pbSeparator2.Visible = isEdit;
            btnAddText.Visible = isEdit;
            lblDelay.Visible = isEdit;

            #endregion
        }

        /// <summary>
        /// Select frame by its System.Int32 index.
        /// </summary>
        /// <param name="index"> The value of index</param>
        /// <exception cref="IndexOutOfRangeException">
        /// When index value doesn't match any frame.
        /// </exception>
        private void SelectFrame(int index)
        {
            if (index >= 0 && index < _listFramesPrivate.Count)
            {
                trackBar.Value = index;
                trackBar_Scroll(null, null);
            }
            else
                throw new IndexOutOfRangeException
                    ("Frame index: " + index + ", is out of range.");
        }

        /// <summary>
        /// Apply desired option on selected frame(s)
        /// </summary>
        /// <param name="actionLabel">
        /// Name of option used to display error message</param>
        /// <param name="actionType">
        /// Type of option referenced by an enumerator </param>
        /// <param name="pickerValue">
        /// Value of picker used for Pixelate and Blur filter</param>
        /// <exception cref="ArgumentException">
        /// Thrown when we have an invalid value of pickerValue</exception>
        /// /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when we have an unknown option [optionType]</exception>
        /// <remarks> 
        /// The purpose of this function is that we can apply 
        /// almost every action/option to the selected frame(s), so if you want to use it
        /// please be sure that the desired action is added to [_actionEnum] Enum.
        /// </remarks>
        private void ApplyActionToFrames(string actionLabel, ActionEnum actionType, int pickerValue = 0)
        {
            IList listIndexSelectedFrames;
            int frameIndex = 0;
            Bitmap currentFrame;

            Invoke((Action)delegate
            {
                ResetUndoProp();
            });

            #region Apply Filter to the Selected Frames

            if (IsFrameSelected(out listIndexSelectedFrames, actionLabel))
            {
                for (int i = 0; i < listIndexSelectedFrames.Count; i++)
                {
                    frameIndex = (int)listIndexSelectedFrames[i];
                    currentFrame = (Bitmap)_listFramesPrivate[frameIndex];

                    #region Switch ActionType

                    switch (actionType)
                    {
                        case ActionEnum.Pixelate:
                            _listFramesPrivate[frameIndex] = ImageUtil.Pixelate(currentFrame,
                                                        new Rectangle(0, 0, currentFrame.Width,
                                                        currentFrame.Height), pickerValue);
                            break;

                        case ActionEnum.Blur:
                            _listFramesPrivate[frameIndex] = ImageUtil.Blur(currentFrame,
                                                        new Rectangle(0, 0, currentFrame.Width,
                                                        currentFrame.Height), pickerValue);
                            break;

                        case ActionEnum.Grayscale:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.Grayscale(currentFrame);
                            break;

                        case ActionEnum.Negative:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.Negative(currentFrame);
                            break;

                        case ActionEnum.Sepia:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.SepiaTone(currentFrame);
                            break;
                    }

                    #endregion
                }

                Invoke((Action)delegate
                {
                    trackBar_ValueChanged(null, null);

                    //highlight the node.
                    tvFrames.Focus();
                    tvFrames.SelectedNode =
                        tvFrames.Nodes[_parentNodeLabel].Nodes[trackBar.Value];
                });

            }

            #endregion
        }

        #endregion

        #region Timers

        /// <summary>
        /// Timer used after clicking in Record, to give the user a shor time to prepare recording
        /// </summary>
        private void timerPreStart_Tick(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                this.Text = "Screen To Gif (" + _preStartCount + Resources.TitleSecondsToGo;
                _preStartCount--;
            }
            else
            {
                this.Text = Resources.TitleRecording;
                timerPreStart.Stop();
                _stage = Stage.Recording;
                btnRecordPause.Enabled = true;
                this.Invalidate(); //To redraw the form, activating OnPaint again

                if (Settings.Default.STshowCursor)
                {
                    timerCapWithCursor_Tick(null, null);
                    timerCapWithCursor.Start();
                }
                else
                {
                    timerCapture_Tick(null, null);
                    timerCapture.Start();
                }
            }
        }

        /// <summary>
        /// Takes a screenshot of desired area and add to the list.
        /// </summary>
        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + panelTransparent.Location.X, this.Location.Y + panelTransparent.Location.Y);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.CopyImage());

            this.Text = "ScreenToGif • " + _frameCount;
            _frameCount++;
        }

        /// <summary>
        /// Takes a screenshot of desired area and add to the list, plus add to the list the position and icon of the cursor.
        /// </summary>
        private void timerCapWithCursor_Tick(object sender, EventArgs e)
        {
            _cursorInfo = new CursorInfo
            {
                Icon = _capture.CaptureIconCursor(ref _posCursor),
                Position = panelTransparent.PointToClient(_posCursor)
            };

            //Get actual icon of the cursor
            _listCursor.Add(_cursorInfo);
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + panelTransparent.Location.X, this.Location.Y + panelTransparent.Location.Y);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.CopyImage());

            this.Text = "ScreenToGif • " + _frameCount;
            _frameCount++;
        }

        #endregion

        #region TextBox Size

        /// <summary>
        /// Prevents keys!=numbers
        /// </summary>
        private void tbSize_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsLetter(e.KeyChar) ||
                char.IsSymbol(e.KeyChar) ||
                char.IsWhiteSpace(e.KeyChar) ||
                char.IsPunctuation(e.KeyChar))
                e.Handled = true;
        }

        /// <summary>
        /// After leaving the textBox, updates the size of the form with what is typed
        /// </summary>
        private void tbSize_Leave(object sender, EventArgs e)
        {
            Application.DoEvents();
            _screenSizeEdit = true;
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            int offsetY = this.Height - panelTransparent.Height;
            int offsetX = this.Width - panelTransparent.Width;

            heightTb += offsetY;
            widthTb += offsetX;

            #region Checks if size is smaller than screen size

            if (heightTb > _sizeScreen.Y)
            {
                heightTb = _sizeScreen.Y;
                tbHeight.Text = _sizeScreen.Y.ToString();
            }

            if (widthTb > _sizeScreen.X)
            {
                widthTb = _sizeScreen.X;
                tbWidth.Text = _sizeScreen.X.ToString();
            }

            #endregion

            this.Size = new Size(widthTb, heightTb);

            _screenSizeEdit = false;
        }

        /// <summary>
        /// User press Enter, updates the form size.
        /// </summary>
        private void tbSize_KeyDown(object sender, KeyEventArgs e)
        {
            Application.DoEvents();
            if (e.KeyData == Keys.Enter)
            {
                _screenSizeEdit = true;
                int heightTb = Convert.ToInt32(tbHeight.Text);
                int widthTb = Convert.ToInt32(tbWidth.Text);

                int offsetY = this.Height - panelTransparent.Height;
                int offsetX = this.Width - panelTransparent.Width;

                heightTb += offsetY;
                widthTb += offsetX;

                #region Checks if size is smaller than screen size

                if (heightTb > _sizeScreen.Y)
                {
                    heightTb = _sizeScreen.Y;
                    tbHeight.Text = _sizeScreen.Y.ToString();
                }

                if (widthTb > _sizeScreen.X)
                {
                    widthTb = _sizeScreen.X;
                    tbWidth.Text = _sizeScreen.X.ToString();
                }

                #endregion

                this.Size = new Size(widthTb, heightTb);

                _screenSizeEdit = false;
            }
        }

        #endregion

        #region Frame Edit Stuff

        /// <summary>
        /// Resizes the form to hold the image
        /// </summary>
        private void ResizeFormToImage()
        {
            #region Window size

            Bitmap bitmap = new Bitmap(_listFramesPrivate[0]);

            Size sizeBitmap = new Size(bitmap.Size.Width + 90, bitmap.Size.Height + 160);

            if (!(sizeBitmap.Width > 700)) //700 minimum width
            {
                sizeBitmap.Width = 700;
            }

            if (!(sizeBitmap.Height > 300)) //300 minimum height
            {
                sizeBitmap.Height = 300;
            }

            this.Size = sizeBitmap;

            bitmap.Dispose();

            #endregion
        }

        /// <summary>
        /// Reset and Undo properties
        /// </summary>
        private void ResetUndoProp()
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion
        }

        #region Edit - Variables

        /// <summary>
        /// List of frames that will be edited.
        /// </summary>
        private List<Bitmap> _listFramesPrivate;

        /// <summary>
        /// List of frames that holds the last alteration.
        /// </summary>
        private List<Bitmap> _listFramesUndo;

        /// <summary>
        /// List of delays that will be edited.
        /// </summary>
        private List<int> _listDelayPrivate;

        /// <summary>
        /// List of delays that holds the last alteration.
        /// </summary>
        private List<int> _listDelayUndo;

        #endregion

        /// <summary>
        /// Constructor of the Frame Edit Page.
        /// </summary>
        private void EditFrames()
        {
            this.Cursor = Cursors.WaitCursor;

            //Copies the _listFramesPrivate to all the lists
            _listFramesPrivate = new List<Bitmap>(_listBitmap);
            _listFramesUndo = new List<Bitmap>(_listBitmap);

            //Copies the listDelay to all the lists
            _listDelayPrivate = new List<int>(_listDelay);
            _listDelayUndo = new List<int>(_listDelay);

            Application.DoEvents();

            panelEdit.Visible = true;

            trackBar.Value = 0;
            trackBar.Maximum = _listFramesPrivate.Count - 1;
            this.MinimumSize = new Size(100, 100);
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

            ResizeFormToImage(); //Resizes the form to hold the image.

            pictureBitmap.Image = _listFramesPrivate.First(); //Puts the first image of the list in the pictureBox.

            #region Preview Config.

            timerPlayPreview.Tick += timerPlayPreview_Tick;
            timerPlayPreview.Interval = _listDelayPrivate[trackBar.Value];

            #endregion

            #region Delay Properties

            //Sets the initial location
            _lastPosition = lblDelay.Location;
            _delay = _listDelayPrivate[0];
            lblDelay.Text = _delay + " ms";

            #endregion

            MainSplit.Panel1Collapsed = true;

            #region Add List of frames to the TreeView

            if (_listFramesPrivate != null)
            {
                tvFrames.UpdateListFrames
                    (_listFramesPrivate.Count, _parentNodeLabel);
            }

            Application.DoEvents();

            #endregion

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Accepts all the alterations and hides this page.
        /// </summary>
        private void btnDone_Click(object sender, EventArgs e)
        {
            StopPreview();
            _listBitmap = new List<Bitmap>(_listFramesPrivate);
            _listDelay = new List<int>(_listDelayPrivate);

            pictureBitmap.Cursor = Cursors.Default;
            panelEdit.Visible = false;
            ShowHideButtons(false);

            this.Text = Resources.Title_Edit_PromptToSave;
            Save();

            GC.Collect();
        }

        /// <summary>
        /// Ignores all the alterations and hides this page.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            StopPreview();

            pictureBitmap.Cursor = Cursors.Default;
            pictureBitmap.Image = null;
            panelEdit.Visible = false;
            ShowHideButtons(false);

            Save();

            GC.Collect();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            pictureBitmap.Image = (Bitmap)_listFramesPrivate[trackBar.Value];

            DelayUpdate();
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            StopPreview();

            if (_listFramesPrivate.Count > 1) //If more than 1 image in the list
            {
                ResetUndoProp();

                _listFramesPrivate.RemoveAt(trackBar.Value); //delete the selected frame
                _listDelayPrivate.RemoveAt(trackBar.Value); //delete the selected delay

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            StopPreview();

            #region Revert Alterations - This one is the oposite of ResetUndoProp()

            //Resets the list to a previously state
            _listFramesPrivate.Clear();
            _listFramesPrivate = new List<Bitmap>(_listFramesUndo);

            //Resets the list to a previously state
            _listDelayPrivate.Clear();
            _listDelayPrivate = new List<int>(_listDelayUndo);

            #endregion

            trackBar.Maximum = _listFramesPrivate.Count - 1;
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

            btnUndo.Enabled = false;

            ResizeFormToImage();

            DelayUpdate();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            StopPreview();
            btnUndo.Enabled = true;

            #region Resets the list of frames

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate); //To undo one

            _listFramesPrivate.Clear();
            _listFramesPrivate = new List<Bitmap>(_listBitmap);

            #endregion

            #region Resets the list of delays

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            _listDelayPrivate.Clear();
            _listDelayPrivate = new List<int>(_listDelay);

            #endregion

            trackBar.Maximum = _listFramesPrivate.Count - 1;
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

            ResizeFormToImage();

            DelayUpdate();
        }

        /// <summary>
        /// Opens the filter context menu.
        /// </summary>
        private void btnFilters_Click(object sender, EventArgs e)
        {
            StopPreview();
            contextSmall.Show(btnFilters, 0, btnFilters.Height);
        }

        /// <summary>
        /// Opens the options context menu.
        /// </summary>
        private void btnOptions_Click(object sender, EventArgs e)
        {
            StopPreview();
            contextMenu.Show(btnOptions, 0, btnOptions.Height);
        }

        #region Context Menu Itens

        private void con_addText_Click(object sender, EventArgs e)
        {
            ResetUndoProp();

            var tip = new ToolTip();
            tip.Show(Resources.Tooltip_SelectPoint,
                this, PointToClient(MousePosition), 5 * 1000);

            //Displays a Cross cursor, to indicate a point selection
            pictureBitmap.Cursor = Cursors.Cross;
        }

        private void con_addCaption_Click(object sender, EventArgs e)
        {
            if (con_tbCaption.Text.Equals(String.Empty)) return;

            ResetUndoProp();

            GraphicsPath graphPath;
            Graphics imgGr;

            for (int i = 0; i < int.Parse(con_tbNumFrames.Text); i++)
            {
                if (trackBar.Value + i == trackBar.Maximum + 1)
                {
                    pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                    return;
                }

                Bitmap image = new Bitmap(_listFramesPrivate[trackBar.Value + i]);

                using (imgGr = Graphics.FromImage(image))
                {
                    graphPath = new GraphicsPath();

                    float witdh = imgGr.MeasureString(con_tbCaption.Text,
                        new Font(new FontFamily("Segoe UI"), (image.Height * 0.15F), FontStyle.Bold)).Width;

                    int fSt = (int)FontStyle.Bold;

                    FontFamily fF = new FontFamily("Segoe UI");
                    StringFormat sFr = StringFormat.GenericDefault;
                    sFr.Alignment = StringAlignment.Center;
                    sFr.LineAlignment = StringAlignment.Far;

                    graphPath.AddString(con_tbCaption.Text, fF, fSt, (image.Height * 0.10F), new Rectangle(new Point(0, 0), image.Size), sFr);

                    imgGr.TextRenderingHint = TextRenderingHint.AntiAlias;

                    imgGr.DrawPath(new Pen(Color.Black, 1.8F), graphPath);  // Draw the path to the surface
                    imgGr.FillPath(Brushes.White, graphPath);  // Fill the path with a color. In this case, White.

                    _listFramesPrivate[trackBar.Value + i] = new Bitmap(image); //Use the "new" keyword to prevent problems with the referenced image.
                }
            }

            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
        }

        private void con_deleteAfter_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                int countList = _listFramesPrivate.Count - 1; //So we have a fixed value

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    _listFramesPrivate.RemoveAt(i);
                    _listDelayPrivate.RemoveAt(i);
                }

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                trackBar.Value = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
            }
        }

        private void con_deleteBefore_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    _listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere
                    _listDelayPrivate.RemoveAt(i);
                }

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
            }
        }

        private void con_exportFrame_Click(object sender, EventArgs e)
        {
            StopPreview();
            SaveFileDialog sfdExport = new SaveFileDialog();
            sfdExport.DefaultExt = "jpg";
            sfdExport.Filter = "JPG Image (*.jpg)|*.jpg";
            sfdExport.FileName = Resources.Msg_Frame + trackBar.Value;

            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                Bitmap expBitmap = _listFramesPrivate[trackBar.Value];
                expBitmap.Save(sfdExport.FileName, ImageFormat.Jpeg);
                MessageBox.Show(Resources.Msg_Frame + trackBar.Value + Resources.Msg_Exported, Resources.Msg_ExportedTitle);
                expBitmap.Dispose();
            }

            sfdExport.Dispose();
        }

        private void con_showGrid_CheckedChanged(object sender, EventArgs e)
        {
            RightSplit.Panel2.BackgroundImage = con_showGrid.Checked ? Properties.Resources.grid : null;
        }

        private void con_resizeAllFrames_Click(object sender, EventArgs e)
        {
            Bitmap bitmapResize = _listFramesPrivate[trackBar.Value];

            var resize = new Resize(bitmapResize);

            if (resize.ShowDialog(this) == DialogResult.OK)
            {
                ResetUndoProp();

                Size resized = resize.GetSize();

                _listFramesPrivate = ImageUtil.ResizeBitmap(_listFramesPrivate, resized.Width, resized.Height);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            resize.Dispose();
        }

        private void con_cropAll_Click(object sender, EventArgs e)
        {
            Crop crop = new Crop(_listFramesPrivate[trackBar.Value]);

            if (crop.ShowDialog(this) == DialogResult.OK)
            {
                ResetUndoProp();

                _listFramesPrivate = ImageUtil.Crop(_listFramesPrivate, crop.Rectangle);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            crop.Dispose();
        }

        private void con_deleteThisFrame_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                _listFramesPrivate.RemoveAt(trackBar.Value);
                _listDelayPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Insert one image to desired position in the list.
        /// </summary>
        private void con_image_Click(object sender, EventArgs e)
        {
            List<Bitmap> bitmapsList;

            try
            {
                if (openImageDialog.ShowDialog() == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    ResetUndoProp();

                    //Image openBitmap = Bitmap.FromFile(openImageDialog.FileName);

                    //Bitmap bitmapResized = ImageUtil.ResizeBitmap((Bitmap)openBitmap, _listFramesPrivate[0].Size.Width,
                    //    _listFramesPrivate[0].Size.Height);

                    // Insert the frame(s) and set the last delay used.
                    bitmapsList = ImageUtil.GetBitmapsFromFile(openImageDialog.FileName,
                        _listFramesPrivate);
                    // Reverse [bitmapsList] order before insertion
                    // if not the Gif will be inserted from the end
                    bitmapsList.Reverse();
                    foreach (Bitmap item in bitmapsList)
                    {
                        _listFramesPrivate.Insert(trackBar.Value, item);
                        _listDelayPrivate.Insert(trackBar.Value, _delay);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error type: " + ex.GetType().ToString() + "\n" +
                    "Message: " + ex.Message, "Error in " + MethodBase.GetCurrentMethod().Name);

                LogWriter.Log(ex, "Error importing image");
            }
            finally
            {
                // Update the content for user
                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
                Cursor = Cursors.Default;

                DelayUpdate();
            }
        }

        /// <summary>
        /// ListFramesInverted
        /// </summary>
        private void con_revertOrder_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                //_listFramesPrivate = ImageUtil.Revert(_listFramesPrivate); //change this.

                _listFramesPrivate.Reverse();
                _listDelayPrivate.Reverse();


                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
            }
        }

        /// <summary>
        /// Make a Yoyo with the frames (listFrames + listFramesInverted)
        /// </summary>
        private void con_yoyo_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                _listFramesPrivate = ImageUtil.Yoyo(_listFramesPrivate);
                _listDelayPrivate = ImageUtil.Yoyo<int>(_listDelayPrivate);

                #region Old Code

                //var listFramesAux = new List<int>(_listDelayPrivate);
                //listFramesAux.Reverse();
                //_listDelayPrivate.AddRange(new List<int> (listFramesAux));
                //listFramesAux.Clear();

                //var listFramesAux2 = new List<Bitmap>(_listFramesPrivate);
                //listFramesAux2.Reverse();
                //_listFramesPrivate.AddRange(new List<Bitmap> (listFramesAux2));
                //listFramesAux2.Clear();

                #endregion

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
            }
        }

        /// <summary>
        /// Adds a border in all images. The border is painted in the image, the image don't increase in size.
        /// </summary>
        private void con_Border_Click(object sender, EventArgs e)
        {
            //In the future, user could choose a color too.

            ValuePicker valuePicker = new ValuePicker(10, 1, Resources.Msg_ChooseThick);

            if (valuePicker.ShowDialog(this) == DialogResult.OK)
            {
                ResetUndoProp();

                _listFramesPrivate = ImageUtil.Border(_listFramesPrivate, valuePicker.Value);
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            }
        }

        private void con_sloMotion_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                for (int i = 0; i < _listDelayPrivate.Count; i++)
                {
                    //Change this, later, let user pick desired velocity.
                    _listDelayPrivate[i] = _listDelayPrivate[i] * 2; //currently, it doubles the  delay.
                }

                DelayUpdate();
            }
        }

        private void con_titleImage_Click(object sender, EventArgs e)
        {
            Size titleFrameSize = _listFramesPrivate[trackBar.Value].Size;
            Bitmap titleBitmap = new Bitmap(titleFrameSize.Width, titleFrameSize.Height);
            var title = new TitleFrameSettings(titleBitmap);

            if (title.ShowDialog() == DialogResult.OK)
            {
                #region If OK

                ResetUndoProp();

                using (Graphics grp = Graphics.FromImage(titleBitmap))
                {
                    if (title.Blured)
                    {
                        #region Blured

                        Cursor = Cursors.WaitCursor;
                        Bitmap blured;

                        if (_listFramesPrivate.Count > (trackBar.Value - 1))
                        {
                            blured = _listFramesPrivate[trackBar.Value + 1];
                        }
                        else
                        {
                            blured = _listFramesPrivate[0]; //If the users wants to place the Title Frame in the end, the logical next frame will be the first.
                        }

                        blured = ImageUtil.Blur(blured, new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), 3);

                        Image bluredI = blured;
                        grp.DrawImage(bluredI, 0, 0);
                        Cursor = Cursors.Default;

                        blured.Dispose();
                        bluredI.Dispose();

                        #endregion
                    }
                    else
                    {
                        grp.FillRectangle(new SolidBrush(title.ColorBackground), 0, 0, titleBitmap.Width, titleBitmap.Height);
                    }

                    StringFormat strFormat = new StringFormat();
                    strFormat.Alignment = StringAlignment.Center;
                    strFormat.LineAlignment = StringAlignment.Center;

                    grp.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    grp.DrawString(title.Content, title.FontTitle, new SolidBrush(title.ColorForeground),
                        new RectangleF(0, 0, titleBitmap.Width, titleBitmap.Height), strFormat);

                    _listFramesPrivate.Insert(trackBar.Value, titleBitmap);

                    _listDelayPrivate.Insert(trackBar.Value, 1000); //Inserts 1s delay.

                    pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                    trackBar.Maximum = _listFramesPrivate.Count - 1;
                    pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                    Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                    DelayUpdate();
                }

                #endregion
            }
        }

        #region Filters

        #region Async Stuff

        private delegate void FilterDelegate(string actionLabel, ActionEnum actionType, int pickerValue = 0);

        private FilterDelegate filterDel;

        private void CallBackFilter(IAsyncResult r)
        {
            filterDel.EndInvoke(r);

            //Cross thread call;
            Invoke((Action)delegate
            {
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                panelEdit.Enabled = true;
                flowPanel.Enabled = true;
                Cursor = Cursors.Default;
            });

        }

        #endregion

        /// <summary>
        /// Apply selected image to a grayscale filter
        /// </summary>
        private void Grayscale_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Grayscale filter", ActionEnum.Grayscale);

            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Apply selected image to a pixelated filter
        /// </summary>
        private void Pixelate_Click(object sender, EventArgs e)
        {
            IList tempList;
            string filterLabel = "Pixelate filter";

            //Only id there is a frame selected
            if (IsFrameSelected(out tempList, filterLabel))
            {
                //User first need to choose the intensity of the pixelate
                ValuePicker valuePicker = new ValuePicker(100, 2, Resources.Msg_PixelSize);

                if (valuePicker.ShowDialog() == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    panelEdit.Enabled = false;
                    flowPanel.Enabled = false;

                    filterDel = ApplyActionToFrames;
                    filterDel.BeginInvoke(filterLabel, ActionEnum.Pixelate, valuePicker.Value, CallBackFilter, null);

                }
                else
                {
                    Cursor = Cursors.Default;
                }
                valuePicker.Dispose();
            }
        }

        /// <summary>
        /// Apply selected image to a blur filter
        /// </summary>
        private void Blur_Click(object sender, EventArgs e)
        {
            IList tempList;
            string filterLabel = "Blur filter";

            if (IsFrameSelected(out tempList, filterLabel))
            {
                ValuePicker valuePicker = new ValuePicker(5, 1, Resources.Msg_BlurIntense);

                if (valuePicker.ShowDialog() == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    panelEdit.Enabled = false;
                    flowPanel.Enabled = false;

                    filterDel = ApplyActionToFrames;
                    filterDel.BeginInvoke(filterLabel, ActionEnum.Blur, valuePicker.Value, CallBackFilter, null);
                }
                else
                {
                    Cursor = Cursors.Default;
                }
                valuePicker.Dispose();
            }
        }

        /// <summary>
        /// Convert selected image to negative filter
        /// </summary>
        private void Negative_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Negative filter", ActionEnum.Negative);

            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert selected image to SepiaTone filter
        /// </summary>
        private void SepiaTone_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Sepia filter", ActionEnum.Sepia);

            Cursor = Cursors.Default;
        }

        #endregion

        #endregion

        #endregion

        #region Play Preview

        System.Windows.Forms.Timer timerPlayPreview = new System.Windows.Forms.Timer();
        private int _actualFrame = 0;

        private void pictureBitmap_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //Cursor.Cross is for point selection.
            if (pictureBitmap.Cursor != Cursors.Cross)
            {
                // Play/Stop Animation.
                if (e.Button.Equals(MouseButtons.Left))
                    PlayPreview();

                return;
            }

            //Calculates the exact position of the cursor over the image
            int crossY = e.Y - (pictureBitmap.Height - _listFramesPrivate[trackBar.Value].Height) / 2;
            int crossX = e.X - (pictureBitmap.Width - _listFramesPrivate[trackBar.Value].Width) / 2;

            //If position is out of bounds
            if ((crossX > _listFramesPrivate[trackBar.Value].Width) || (crossY > _listFramesPrivate[trackBar.Value].Height) ||
                crossX < 0 || crossY < 0)
            {
                // Display error message and exit function
                MessageBox.Show(Resources.Msg_WrongPosition, Resources.Title_InsertText,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // Store point coordinates to insert text
            _pointTextPosition = new Point(crossX, crossY);

            // Initialize cursor for [pictureBitmap]
            pictureBitmap.Cursor = Cursors.Default;

            //Show TitleFrameSettings form as modal
            (new InsertText(false)).ShowDialog(this);
        }

        private void PlayPreview()
        {
            if (timerPlayPreview.Enabled)
            {
                timerPlayPreview.Stop();
                lblDelay.Visible = true;
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
                btnPreview.Text = Resources.Con_PlayPreview;
                btnPreview.Image = Resources.Play_17Green;

                DelayUpdate();
            }
            else
            {
                lblDelay.Visible = false;
                this.Text = "Screen To Gif - " + Resources.Title_PlayingAnimation;
                btnPreview.Text = Resources.Con_StopPreview;
                btnPreview.Image = Resources.Stop_17Red;
                _actualFrame = trackBar.Value;
                timerPlayPreview.Start();
            }

        }

        private void StopPreview()
        {
            timerPlayPreview.Stop();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            lblDelay.Visible = true;
            btnPreview.Text = Resources.Con_PlayPreview;
            btnPreview.Image = Resources.Play_17Green;

            DelayUpdate();
        }

        private void timerPlayPreview_Tick(object sender, EventArgs e)
        {
            //Sets the interval for this frame. If this frame has 500ms, the next frame will take 500ms to show.
            timerPlayPreview.Interval = _listDelayPrivate[_actualFrame];

            pictureBitmap.Image = _listFramesPrivate[_actualFrame];
            trackBar.Value = _actualFrame;

            if (_listFramesPrivate.Count - 1 == _actualFrame)
            {
                _actualFrame = 0;
            }
            else
            {
                _actualFrame++;
            }
        }

        private void trackBar_Enter(object sender, EventArgs e)
        {
            StopPreview();
        }

        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopPreview();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_Scroll(object sender, EventArgs e)
        {
            StopPreview();

            if (e == null) return; //To prevent loop.

            #region Select the node when user scrolls

            var parentNode = tvFrames.Nodes[_parentNodeLabel];
            if (!parentNode.IsExpanded)
            {
                parentNode.Expand();
                Application.DoEvents();
            }

            tvFrames.SelectedNode = parentNode.Nodes[trackBar.Value];
            tvFrames.Focus();
            Application.DoEvents();

            #endregion
        }

        #endregion

        #region Frame Delay

        /// <summary>
        /// The last position of the mouse cursor.
        /// </summary>
        private Point _lastPosition;

        /// <summary>
        /// The amount of delay selected.
        /// </summary>
        private int _delay;

        /// <summary>
        /// Flag that tells when the user is clicking in the label or not.
        /// </summary>
        private bool _clicked;

        /// <summary>
        /// Updates the <see cref="_clicked"/> flag or opens the contextMenu.
        /// </summary>
        private void lblDelay_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _clicked = true;
                _lastPosition = e.Location; //not sure if this is needed here, there is another one in the EditFrames().
            }
            else
            {
                con_tbDelay.Text = _delay.ToString();
                contextDelay.Show(lblDelay, 0, lblDelay.Height);
            }
        }

        /// <summary>
        /// Gets if the user is draging to up or down and updates the values.
        /// </summary>
        private void lblDelay_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //If the user is not clicking, returns.
            if (!_clicked)
                return;

            if ((e.Y - _lastPosition.Y) < 0)
            {
                if (_delay < 1000)
                {
                    _delay++;
                    lblDelay.Text = _delay + " ms";
                }

            }
            else if ((e.Y - _lastPosition.Y) > 0)
            {
                if (_delay > 10)
                {
                    _delay--;
                    lblDelay.Text = _delay + " ms";
                }

            }

            _lastPosition = e.Location;
        }

        /// <summary>
        /// Updates the flag <see cref="_clicked"/> to false and sets the delay value to the list.
        /// </summary>
        private void lblDelay_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _clicked = false;

            //Only sets the delay value when user finishes selecting.
            _listDelayPrivate[trackBar.Value] = _delay;
        }

        /// <summary>
        /// Called after the user changes the text of the toolstrip of the delay.
        /// </summary>
        private void con_tbDelay_TextChanged(object sender, EventArgs e)
        {
            if (con_tbDelay.Text.Equals(""))
                con_tbDelay.Text = "0";

            _delay = Convert.ToInt32(con_tbDelay.Text);

            if (_delay > 1000)
            {
                _listDelayPrivate[trackBar.Value] = _delay = 1000;
            }

            if (_delay < 10)
            {
                _listDelayPrivate[trackBar.Value] = _delay = 10;
            }

            _listDelayPrivate[trackBar.Value] = _delay;
            lblDelay.Text = _delay + " ms";
        }

        /// <summary>
        /// Called when the user hits the key "Enter" while focused in the con_tbDelay text box. It closes the context menu.
        /// </summary>
        private void con_tbDelay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (char)Keys.Enter)
            {
                contextDelay.Close(ToolStripDropDownCloseReason.Keyboard);
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Updates the label that shows the delay of the current frame
        /// </summary>
        private void DelayUpdate()
        {
            #region Delay Display

            _delay = _listDelayPrivate[trackBar.Value];
            lblDelay.Text = _delay + " ms";

            #endregion
        }

        #endregion

        #region Treeview of Frames

        private void btnShowListFrames_Click(object sender, EventArgs e)
        {
            #region Show tvFrames

            MainSplit.Panel1Collapsed = false;
            RightSplit.Panel1Collapsed = true;

            #endregion
        }

        private void btnHideListFrames_Click(object sender, EventArgs e)
        {
            #region Hide tvFrames

            MainSplit.Panel1Collapsed = true;
            RightSplit.Panel1Collapsed = false;

            #endregion
        }

        /// <summary>
        /// Check if there is at least one frame, 
        /// and return list of indexes for selected frames as parameter.
        /// </summary>
        /// <param name="listIndexSelectedFrames">
        /// List of indexes that reference selected frames </param>
        /// <param name="optionLabel" example="Negative filter">Name of option</param>
        /// <returns>bool to indicate if there is frame(s) or not</returns>
        private bool IsFrameSelected(out IList listIndexSelectedFrames, string optionLabel = "")
        {
            listIndexSelectedFrames = new ArrayList();

            #region Get indexes of selected frames

            foreach (TreeNode node in tvFrames.Nodes[_parentNodeLabel].Nodes)
            {
                if (node.Checked)
                    listIndexSelectedFrames.Add(node.Index);
            }

            #endregion

            // Check if there is at least one frame.            
            if (listIndexSelectedFrames.Count <= 0)
            {
                #region If No Frame Selected

                //Show treeViewLstFrames if hidden
                btnShowListFrames_Click(null, null);

                toolTipHelp.ToolTipTitle = "No frames selected";
                toolTipHelp.ToolTipIcon = ToolTipIcon.Info;
                toolTipHelp.Show("You need to select at least one frame before applying a filter.", tvFrames, tvFrames.Width, 0, 3500);

                #endregion

                return false;
            }

            return true;
        }

        private void tvFrames_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action != TreeViewAction.Unknown)
            {
                #region If Known Action

                //(Un)check all nodes
                if (e.Node.Name == _parentNodeLabel)
                {
                    foreach (TreeNode node in e.Node.Nodes)
                        node.Checked = e.Node.Checked;
                }
                else
                    //Or display the (un)checked frame
                    SelectFrame(e.Node.Index);

                //Select current node
                tvFrames.SelectedNode = e.Node;

                #endregion
            }
        }

        private void tvFrames_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Display selected frame 
            if (e.Action != TreeViewAction.Unknown && e.Node.Name != _parentNodeLabel)
                SelectFrame(e.Node.Index);
        }

        #endregion

        //Organize
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (trackBar.Value == 0)
            {
                trackBar.Value = trackBar.Maximum;
            }
            else
            {
                trackBar.Value = trackBar.Value - 1;
            }

            Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            DelayUpdate();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (trackBar.Value == trackBar.Maximum)
            {
                trackBar.Value = 0;
            }
            else
            {
                trackBar.Value = trackBar.Value + 1;
            }

            Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            DelayUpdate();
        }
    }
}
