using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.Encoding;
using ScreenToGif.Pages;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using DataFormats = System.Windows.Forms.DataFormats;
using DragDropEffects = System.Windows.Forms.DragDropEffects;

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
        readonly CaptureCursor _capture = new CaptureCursor();
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
        private Stage _stage = Stage.Stopped; //0 Stopped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding
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
        /// True if it is a IBeam cursor.
        /// </summary>
        private bool _isIbeam;
        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private Point _sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);

        /// <summary>
        /// The resolution of the primary screen.
        /// </summary>
        private Size _sizeResolution;

        private Bitmap _bt;
        private Graphics _gr;

        /// <summary>
        /// The encode worker thread.
        /// </summary>
        private Thread _workerThread;

        /// <summary>
        /// The point position on the bitmap, used to insert text.
        /// </summary>
        private Point _pointTextPosition;

        /// <summary>
        /// The text of the "All" label of the TreeView.
        /// </summary>
        private readonly string _parentNodeLabel = Resources.Label_All;

        /// <summary>
        /// Displays a tray icon.
        /// </summary>
        private readonly ScreenToGifTrayIcon _trayIcon = new ScreenToGifTrayIcon();

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp = Path.GetTempPath() +
            String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));

        #region Enums

        private enum Stage : int
        {
            Stopped = 0,
            Recording = 1,
            Paused = 2,
            PreStarting = 3,
            Editing = 4,
            Encoding = 5,
            Snapping = 6,
        };

        private enum ActionEnum : int
        {
            Grayscale = 0,
            Color,
            Pixelate,
            Blur,
            Negative,
            Sepia,
            Delete,
            Border,
            Speed,
            Caption,
            Export,
            AddText,
            FlipRotate,
            FreeDraw,
            Progress,
        };

        #endregion

        #region Lists

        /// <summary>
        /// Lists of frames as file names.
        /// </summary>
        List<string> _listFrames = new List<string>();

        /// <summary>
        /// Lists of frames as file names.
        /// </summary>
        List<string> _listFramesEdit;
        List<string> _listFramesUndo;

        /// <summary>
        /// List of delays that will be edited.
        /// </summary>
        private List<int> _listDelayEdit;

        /// <summary>
        /// List of delays that holds the last alteration.
        /// </summary>
        private List<int> _listDelayUndo;

        #endregion

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
            numMaxFps.Value = Properties.Settings.Default.maxFps;

            //Load last saved window size
            this.Size = new Size(Settings.Default.size.Width, Settings.Default.size.Height);

            //Put the grid as background.
            RightSplit.Panel2.BackgroundImage =
                (con_showGrid.Checked = Settings.Default.ShowGrid) ? //If
                Properties.Resources.grid : null;  //True-False

            //Recording options.
            con_Fullscreen.Checked = Settings.Default.fullscreen;
            con_Snapshot.Checked = Settings.Default.snapshot;

            #endregion

            //Gets the window size and show in the textBoxes
            tbHeight.Text = panelTransparent.Height.ToString();
            tbWidth.Text = panelTransparent.Width.ToString();

            #region Performance and flickering tweaks

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);

            #endregion

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
                _actHook.Start(false, true);
            }
            catch (Exception) { }

            #endregion

            #region If there is a gif file as argument, read the file and jump directly to the editor.

            if (!String.IsNullOrEmpty(ArgumentUtil.FileName))
            {
                CreateTemp();

                //TODO: Redo this code. It works, but...
                _listFramesEdit = new List<string>();
                _listDelayEdit = new List<int>();

                _listFramesUndo = new List<string>();
                _listDelayUndo = new List<int>();

                _delay = 66; //We should get the true delay.

                AddPictures(ArgumentUtil.FileName);

                int width = _listFramesEdit[0].From().Size.Width < 500 ? 500 : _listFramesEdit[0].From().Size.Width;
                int height = _listFramesEdit[0].From().Size.Height < 300 ? 300 : _listFramesEdit[0].From().Size.Height;

                _lastSize = new Size(width, height);

                btnUndo.Enabled = false;
                btnReset.Enabled = false;

                _listFrames = new List<string>(_listFramesEdit);
                _listDelay = new List<int>(_listDelayEdit);

                this.TopMost = false;

                EditFrames();
                ShowHideButtons(true);

                pictureBitmap.AllowDrop = true;
                _stage = Stage.Editing;
                ArgumentUtil.FileName = null;
            }

            #endregion

            _trayIcon.NotifyIconClicked += NotifyIconClicked;

            #region Detect High DPI Settings

            float dpiX;
            using (Graphics graphics = this.CreateGraphics())
            {
                dpiX = graphics.DpiX;
            }

            if (dpiX > 96)
            {
                if (Settings.Default.showHighDpiWarn)
                {
                    var highDpi = new HighDpi();
                    highDpi.ShowDialog();

                    highDpi.Dispose();
                }
            }
            else
            {
                Settings.Default.showHighDpiWarn = false;
            }

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
            if (_stage == Stage.Stopped || _stage == Stage.Editing)
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
                    con_deleteSelectedFrame_Click(null, null);
                    return true;
                case Keys.Control | Keys.G: //Show grid
                    con_showGrid.Checked = !con_showGrid.Checked; //Inverse the checkState.
                    con_showGrid_CheckedChanged(null, null);
                    return true;
                case Keys.Right: //Next frame
                    btnNext_Click(null, null);
                    return true;
                case Keys.Left: //Previous frame
                    btnPrevious_Click(null, null);
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
                case Keys.Shift | Keys.Escape: //Cancel
                    btnCancel_Click(null, null);
                    return true;
                case Keys.Shift | Keys.Enter: //Done
                    btnDone_Click(null, null);
                    return true;
                case Keys.Alt | Keys.D: //Show the Delay Context
                    #region Show Delay

                    contextDelay.Show(lblDelay, 0, lblDelay.Height);
                    con_tbDelay.Text = _delay.ToString();
                    con_tbDelay.Focus();

                    #endregion
                    return true;
                case Keys.Control | Keys.Up: //Move Frame Upwards
                    con_MoveUpwards_Click(null, null);
                    return true;
                case Keys.Control | Keys.Down: //Move Frame Downwards
                    con_MoveDownwards_Click(null, null);
                    return true;
                case Keys.F2: //Rename Frame
                    con_RenameFrame_Click(null, null);
                    return true;
                case Keys.Control | Keys.C: //Make a Copy
                    con_MakeACopy_Click(null, null);
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

        #region Main Form Move/Resize/Closing

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
                btnMaximize.Image = Resources.MaximizeMinus;

            AutoFitButtons();
        }

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (panelTransparent.Width < 200)
            {
                if (btnRecordPause.ImageAlign == ContentAlignment.MiddleLeft)
                {
                    btnRecordPause.Text = String.Empty;
                    btnRecordPause.ImageAlign = ContentAlignment.MiddleCenter;
                    btnStop.Text = String.Empty;
                    btnStop.ImageAlign = ContentAlignment.MiddleCenter;
                }
            }
            else
            {
                if (btnRecordPause.ImageAlign == ContentAlignment.MiddleCenter)
                {
                    if (_stage == Stage.Recording)
                        btnRecordPause.Text = Resources.Pause;
                    else if (_stage == Stage.Paused)
                        btnRecordPause.Text = Resources.btnRecordPause_Continue;
                    else if (Settings.Default.snapshot)
                        btnRecordPause.Text = Resources.btnSnap;
                    else
                        btnRecordPause.Text = Resources.btnRecordPause_Record;

                    btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                    btnStop.Text = Resources.Label_Stop;
                    btnStop.ImageAlign = ContentAlignment.MiddleLeft;
                }
            }
        }

        /// <summary>
        /// Before close, all settings must be saved and the timer must be disposed.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            #region Save Settings

            Settings.Default.maxFps = Convert.ToInt32(numMaxFps.Value);

            if (_stage == Stage.Editing)
            {
                Settings.Default.size = _lastSize;
            }
            else
            {
                Settings.Default.size = this.Size;
            }

            Settings.Default.Save();

            #endregion

            try
            {
                _actHook.Stop(); //Stop the keyboard watcher.
            }
            catch (Exception) { }

            if (_stage != (int)Stage.Stopped)
            {
                timerCapture.Stop();
                timerCapture.Dispose();

                timerCapWithCursor.Stop();
                timerCapWithCursor.Dispose();
            }

            _trayIcon.Dispose();
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

        private readonly Control _info = new Information();
        private readonly Control _appSettings = new AppSettings(false); //false = modern, true = legacy
        private readonly Control _gifSettings = new GifSettings();

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnMaximize.Enabled = true;
            btnMinimize.Enabled = true;

            Stop();
        }

        private void btnRecordPause_Click(object sender, EventArgs e)
        {
            RecordPause(); //And start the pre-start tick
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (!btnConfig.Checked)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;
            }
            else
            {
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);

                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(_appSettings);
                _appSettings.Dock = DockStyle.Fill;
                panelTransparent.Visible = true;

                btnGifConfig.Checked = false;
                btnInfo.Checked = false;

                this.TransparencyKey = Color.Empty;
            }
        }

        private void btnGifConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_stage == Stage.Editing)
            {
                if (!btnInfo.Checked)
                {
                    panelEdit.Visible = !panelEdit.Visible;
                }
            }

            if (!btnGifConfig.Checked)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;
            }
            else
            {
                //Need this line, because there is a pictureBox with color, so if the user select the same color 
                //as this.TransparencyKey, the color won't be showed. This needs to be re-set after closing the gif config page.
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);

                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(_gifSettings);
                panelTransparent.Visible = true;
                _gifSettings.Dock = DockStyle.Fill;

                btnConfig.Checked = false;
                btnInfo.Checked = false;

                this.TransparencyKey = Color.Empty;
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_stage == Stage.Editing)
            {
                if (!btnGifConfig.Checked)
                {
                    panelEdit.Visible = !panelEdit.Visible;
                }
            }

            if (!btnInfo.Checked)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;

                GC.Collect();
            }
            else
            {
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);

                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(_info);
                panelTransparent.Visible = true;
                _info.Dock = DockStyle.Fill;

                btnConfig.Checked = false;
                btnGifConfig.Checked = false;

                GC.Collect();

                this.TransparencyKey = Color.Empty;
            }
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            StopPreview(true);

            if (trackBar.Value == 0)
            {
                trackBar.Value = trackBar.Maximum;
            }
            else
            {
                trackBar.Value = trackBar.Value - 1;
            }

            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);
            DelayUpdate();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            StopPreview(true);

            if (trackBar.Value == trackBar.Maximum)
            {
                trackBar.Value = 0;
            }
            else
            {
                trackBar.Value = trackBar.Value + 1;
            }

            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);
            DelayUpdate();
        }

        private void btnFreeDrawing_Click(object sender, EventArgs e)
        {
            StopPreview(true);
            FreeDrawing freeDraw = null;

            if (tvFrames.SelectedFrames().Count > 0)
            {
                freeDraw = new FreeDrawing(Resources.grid, ImageLayout.Tile, pictureBitmap.Image.Size);
            }
            else
            {
                freeDraw = new FreeDrawing(pictureBitmap.Image, ImageLayout.None, pictureBitmap.Image.Size);
            }

            if (freeDraw.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.AppStarting;

                ApplyActionToFrames("FreeDraw", ActionEnum.FreeDraw, 0F, freeDraw.ImagePainted);

                this.Cursor = Cursors.Default;
            }

            freeDraw.Dispose();
            GC.Collect();
        }

        #endregion

        #region Functions

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
        private void KeyHookTarget(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Settings.Default.startPauseKey)
            {
                btnRecordPause_Click(null, null);
            }
            else if (e.KeyCode == Settings.Default.stopKey)
            {
                btnStop_Click(null, null);
            }
        }

        /// <summary>
        /// MouseHook event method, not implemented.
        /// </summary>
        private void MouseHookTarget(object sender, MouseEventArgs keyEventArgs)
        {
            _recordClicked = keyEventArgs.Button == MouseButtons.Left;
        }

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void RecordPause()
        {
            CreateTemp();

            if (_stage == Stage.Stopped) //if stoped, starts recording
            {
                #region To Record

                #region Remove all pages

                panelTransparent.BackColor = Color.LimeGreen;
                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.Controls.Clear(); //Removes all pages from the top

                btnConfig.Checked = false;
                btnGifConfig.Checked = false;
                btnInfo.Checked = false;

                #endregion

                timerCapture.Interval =
                timerCaptureFull.Interval =
                timerCapWithCursor.Interval =
                timerCapWithCursorFull.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                _listFrames = new List<string>(); //List that contains the path of all frames.
                _listCursor = new List<CursorInfo>(); //List that contains all the icon information

                if (Settings.Default.fullscreen)
                {
                    _sizeResolution = new Size(_sizeScreen);
                    _bt = new Bitmap(_sizeResolution.Width, _sizeResolution.Height);

                    HideWindowAndShowTrayIcon();
                }
                else
                {
                    _bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                }
                _gr = Graphics.FromImage(_bt);

                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                numMaxFps.Enabled = false;
                this.TopMost = true;

                _addDel = AddFrames;

                if (Settings.Default.preStart) //if should show the pre start countdown
                {
                    this.Text = "Screen To Gif (2" + Resources.TitleSecondsToGo;
                    btnRecordPause.Enabled = false;

                    _actHook.OnMouseActivity += MouseHookTarget;

                    _stage = Stage.PreStarting;
                    _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                    timerPreStart.Start();
                }
                else
                {
                    btnRecordPause.Enabled = true;
                    btnMaximize.Enabled = false;

                    if (Settings.Default.showCursor) //if show cursor
                    {
                        #region If show cursor

                        if (!Settings.Default.snapshot)
                        {
                            #region Normal Recording

                            _actHook.OnMouseActivity += MouseHookTarget;

                            if (!Settings.Default.fullscreen)
                            {
                                //To start recording right away, I call the tick before starting the timer,
                                //because the first tick will only occur after the delay.
                                btnMinimize.Enabled = false;

                                timerCapWithCursor_Tick(null, null);
                                timerCapWithCursor.Start();
                            }
                            else
                            {
                                this.MinimizeBox = false;
                                timerCapWithCursorFull_Tick(null, null);
                                timerCapWithCursorFull.Start();
                            }

                            _stage = Stage.Recording;
                            btnRecordPause.Text = Resources.Pause;
                            btnRecordPause.Image = Resources.Pause_17Blue;
                            btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                            AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region SnapShot Recording

                            //Set to snapshot mode, change the text of the record button to "Snap" and 
                            //every press of the button, takes a screenshot
                            _stage = Stage.Snapping;
                            btnRecordPause.Image = Properties.Resources.Snap16x;
                            btnRecordPause.Text = Resources.btnSnap;
                            btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                            this.Text = "Screen To Gif - " + Resources.Con_SnapshotMode;

                            AutoFitButtons();

                            #endregion
                        }

                        #endregion
                    }
                    else
                    {
                        #region If not

                        if (!Settings.Default.snapshot)
                        {
                            #region Normal Recording

                            if (!Settings.Default.fullscreen)
                            {
                                btnMaximize.Enabled = false;
                                timerCapture_Tick(null, null);
                                timerCapture.Start();
                            }
                            else
                            {
                                timerCaptureFull_Tick(null, null);
                                timerCaptureFull.Start();
                            }

                            _stage = Stage.Recording;
                            btnRecordPause.Text = Resources.Pause;
                            btnRecordPause.Image = Resources.Pause_17Blue;
                            btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                            AutoFitButtons();

                            #endregion
                        }
                        else
                        {
                            #region SnapShot Recording

                            _stage = Stage.Snapping;
                            btnRecordPause.Image = Resources.Snap16x;
                            btnRecordPause.Text = Resources.btnSnap;
                            btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                            this.Text = "Screen To Gif - " + Resources.Con_SnapshotMode;

                            AutoFitButtons();

                            #endregion
                        }

                        #endregion
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
                btnRecordPause.Image = Resources.Record;
                btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                _stage = Stage.Paused;

                AutoFitButtons();

                ModifyCaptureTimerAndChangeTrayIconVisibility(false);

                #endregion
            }
            else if (_stage == Stage.Paused) //if paused, starts recording again
            {
                #region To Record Again

                this.Text = Resources.TitleRecording;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Resources.Pause_17Blue;
                btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                _stage = Stage.Recording;

                AutoFitButtons();

                ModifyCaptureTimerAndChangeTrayIconVisibility(true);

                #endregion
            }
            else if (_stage == Stage.Snapping)
            {
                #region Take Screenshot (All possibles types)

                if (Settings.Default.showCursor)
                {
                    if (Settings.Default.fullscreen)
                    {
                        timerCapWithCursorFull_Tick(null, null);
                    }
                    else
                    {
                        timerCapWithCursor_Tick(null, null);
                    }
                }
                else
                {
                    if (Settings.Default.fullscreen)
                    {
                        timerCaptureFull_Tick(null, null);
                    }
                    else
                    {
                        timerCapture_Tick(null, null);
                    }
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
                _frameCount = 0; //put this in other location

                timerCapture.Stop();
                timerCaptureFull.Stop();
                timerCapWithCursor.Stop();
                timerCapWithCursorFull.Stop();

                if (_stage != Stage.Stopped && _stage != Stage.PreStarting && _listFrames.Any()) //if not already stoped or pre starting, stops
                {
                    #region To Stop and Save

                    try
                    {
                        _actHook.Stop(); //Stops the hook.
                    }
                    catch (Exception ex) { }

                    _stopDel = StopAsync;
                    _stopDel.BeginInvoke(CallBackStop, null);

                    this.Cursor = Cursors.AppStarting;
                    panelBottom.Enabled = false;

                    return;

                    #endregion
                }
                else if ((_stage == Stage.PreStarting || _stage == Stage.Snapping) && !_listFrames.Any()) // if Pre-Starting or in Snapmode and no frames, stops.
                {
                    #region To Stop

                    timerPreStart.Stop();
                    _stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Enabled = true;
                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                    this.Text = Resources.TitleStoped;

                    AutoFitButtons();
                    this.Invalidate();

                    try
                    {
                        //Re-starts the keyboard hook.
                        _actHook.OnMouseActivity += null;
                        _actHook.Start(false, true);
                    }
                    catch (Exception) { }

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

        #region Async Stop

        private delegate void StopDelegate();

        private StopDelegate _stopDel;

        private void StopAsync()
        {
            #region Processing Page

            this.Invoke((Action)delegate //Needed because it's a cross thread call.
            {
                #region Verifies the minimum size to display the Processing page

                if (this.Size.Height < 220)
                {
                    this.Size = new Size(this.Size.Width, 220);
                }

                if (this.Size.Width < 400)
                {
                    this.Size = new Size(400, this.Size.Height);
                }

                #endregion

                this.TransparencyKey = Color.Empty;
                panelBottom.Visible = false;

                //Using this makes all Processing calls to change threads.
                panelTransparent.Controls.Add(Processing.Page = new Processing { Dock = DockStyle.Fill });
                Processing.Undefined("Finishing record...");
                Application.DoEvents();
            });

            #endregion

            #region If show cursor is true, merge all bitmaps

            //TODO: Make lighter
            if (Settings.Default.showCursor)
            {
                this.Invoke((Action)(() => Processing.Undefined("Merging Cursors...")));

                #region Merge Cursor and Bitmap

                int numImage = 0;

                foreach (var filename in _listFrames)
                {
                    try
                    {
                        var imageTemp = filename.From();

                        using (var graph = Graphics.FromImage(imageTemp))
                        {
                            #region Mouse Clicks

                            if (_listCursor[numImage].Clicked && Settings.Default.showMouseClick)
                            {
                                //Draws the ellipse first, to  get behind the cursor.
                                var rectEllipse = new Rectangle(
                                        _listCursor[numImage].Position.X - (_listCursor[numImage].IconImage.Width / 2),
                                        _listCursor[numImage].Position.Y - (_listCursor[numImage].IconImage.Height / 2),
                                        _listCursor[numImage].IconImage.Width - 10,
                                        _listCursor[numImage].IconImage.Height - 10);

                                graph.DrawEllipse(new Pen(new SolidBrush(Color.Yellow), 3), rectEllipse);
                            }

                            #endregion

                            var rect = new Rectangle(_listCursor[numImage].Position.X,
                                _listCursor[numImage].Position.Y, _listCursor[numImage].IconImage.Width,
                                _listCursor[numImage].IconImage.Height);

                            graph.DrawImage(_listCursor[numImage].IconImage, rect);
                            graph.Flush();

                            _listCursor[numImage].IconImage.Dispose();
                        }

                        imageTemp.Save(filename);
                        imageTemp.Dispose();
                    }
                    catch (Exception) { }

                    numImage++;
                }

                #endregion
            }

            #endregion

            #region If fullscreen, resizes all the images, half of the size

            if (Settings.Default.fullscreen)
            {
                this.Invoke((Action)ShowWindowAndHideTrayIcon);

                this.Invoke((Action)(() => Processing.Undefined("Resizing Fullscreen Recording...")));

                var bitmapAux = _listFrames[0].From();

                ImageUtil.ResizeBitmap(_listFrames,
                    Convert.ToInt32(bitmapAux.Size.Width / 2),
                    Convert.ToInt32(bitmapAux.Size.Height / 2));

                bitmapAux.Dispose();
                GC.Collect();
            }

            #endregion

            #region Creates the Delay

            _listDelay = new List<int>();

            int delayGlobal;
            if (Settings.Default.snapshot)
            {
                delayGlobal = 600;
            }
            else
            {
                delayGlobal = 1000 / (int)numMaxFps.Value;
            }

            for (int i = 0; i < _listFrames.Count; i++)
            {
                _listDelay.Add(delayGlobal);
            }

            #endregion
        }

        private void CallBackStop(IAsyncResult r)
        {
            if (IsDisposed) return;

            _stopDel.EndInvoke(r);

            this.Invoke((Action)delegate
            {
                this.Cursor = Cursors.Default;
                panelBottom.Enabled = true;

                this.TransparencyKey = Color.LimeGreen;
                this.Invalidate();

                panelBottom.Visible = true;
                panelTransparent.Visible = true;

                #region If the user wants to edit the frames or not

                if (Settings.Default.allowEdit)
                {
                    //To return back to the last form size after the editor
                    _lastSize = this.Size;
                    _stage = Stage.Editing;
                    btnMaximize.Enabled = true;
                    btnMinimize.Enabled = true;
                    this.TopMost = false;

                    EditFrames();

                    ShowHideButtons(true);
                }
                else
                {
                    _lastSize = this.Size; //Not sure why this is here
                    Save();

                    Processing.Page.Dispose();
                }

                #endregion
            });
        }

        #endregion

        /// <summary>
        /// Prepares the recorded frames to be saved/edited
        /// </summary>
        private void Save()
        {
            this.Cursor = Cursors.WaitCursor;

            this.Size = _lastSize;
            this.Invalidate();
            Application.DoEvents();

            if (!Settings.Default.saveLocation) // to choose the location to save the gif
            {
                #region If Not Save Directly to the Desktop

                var sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                this.Cursor = Cursors.Default;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _outputpath = sfd.FileName;

                    _workerThread = new Thread(DoWork);
                    _workerThread.IsBackground = true;
                    _workerThread.Name = "Gif Encoding";
                    _workerThread.Start();
                }
                else //if user don't want to save the gif
                {
                    FinishState();

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                    this.Text = Resources.TitleStoped;

                    AutoFitButtons();
                    this.Invalidate();

                    try
                    {
                        //_actHook.KeyDown += KeyHookTarget;
                        _actHook.OnMouseActivity += null;
                        _actHook.Start(false, true);
                    }
                    catch (Exception) { }

                    Directory.Delete(_pathTemp, true);
                }

                #endregion
            }
            else
            {
                #region Search For Filename

                bool searchForName = true;
                int numOfFile = 0;

                string path;

                //If there is no defined save location, saves in the desktop.
                if (String.IsNullOrEmpty(Settings.Default.folder))
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                }
                else
                {
                    path = Settings.Default.folder;
                }

                this.Cursor = Cursors.Default;

                #region Ask if should encode

                DialogResult ask = MessageBox.Show(this, Resources.Msg_WantEncodeAnimation + path, "Screen To Gif",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                //Only saves the recording if the user wants to.
                if (ask != DialogResult.Yes)
                {
                    //If the user don't want to save the recording.
                    this.Text = Resources.TitleStoped;

                    #region Clear Variables

                    _listFrames.Clear();
                    _listDelay.Clear();

                    //These variables are only used in the editor.
                    if (_listFramesEdit != null)
                    {
                        _listFramesEdit.Clear();
                        _listFramesUndo.Clear();

                        _listDelayEdit.Clear();
                        _listDelayUndo.Clear();
                    }

                    GC.Collect();

                    #endregion

                    Directory.Delete(_pathTemp, true);
                    FinishState();

                    try
                    {
                        //Restart the keyhook.
                        //_actHook.KeyDown += KeyHookTarget;
                        _actHook.Start(false, true);
                    }
                    catch (Exception) { }

                    return;
                }

                #endregion

                #region FileName

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

                #endregion

                _workerThread = new Thread(DoWork);
                _workerThread.IsBackground = true;
                _workerThread.Name = "Gif Encoding";
                _workerThread.Start();
            }

            //FinishState();
        }

        /// <summary>
        /// Cancels the editor phase, returning to the recording.
        /// </summary>
        private void Cancel()
        {
            FinishState();

            btnRecordPause.Text = Resources.btnRecordPause_Record;
            btnRecordPause.Image = Resources.Record;
            btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
            this.Text = Resources.TitleStoped;

            AutoFitButtons();

            try
            {
                //_actHook.KeyDown += KeyHookTarget;
                _actHook.OnMouseActivity += null;
                _actHook.Start(true, true);
            }
            catch (Exception) { }

            Directory.Delete(_pathTemp, true);
        }

        /// <summary>
        /// Do all the work to set the controls to the finished state. (i.e. Finished encoding)
        /// </summary>
        private void FinishState()
        {
            this.Cursor = Cursors.Default;
            //panelTransparent.Visible = true;
            panelBottom.Visible = true;
            _stage = Stage.Stopped;
            this.MinimumSize = new Size(100, 100);
            this.Size = _lastSize;

            numMaxFps.Enabled = true;
            tbHeight.Enabled = true;
            tbWidth.Enabled = true;
            btnMaximize.Enabled = true;
            btnMinimize.Enabled = true;

            btnRecordPause.Text = Resources.btnRecordPause_Record;
            btnRecordPause.Image = Resources.Record;
            btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

            AutoFitButtons();

            this.Invalidate(this.DisplayRectangle);
        }

        /// <summary>
        /// Thread method that encodes the list of frames.
        /// </summary>
        private void DoWork()
        {
            _stage = Stage.Encoding;
            int countList = _listFrames.Count;

            #region Show Processing

            this.Invoke((Action)delegate //Needed because it's a cross thread call.
            {
                #region Verifies the minimum size to display the Processing page

                if (this.Size.Height < 220)
                {
                    this.Size = new Size(this.Size.Width, 220);
                }

                if (this.Size.Width < 400)
                {
                    this.Size = new Size(400, this.Size.Height);
                }

                #endregion

                this.TransparencyKey = Color.Empty;
                panelBottom.Visible = false;

                //Control ctrlParent = panelTransparent;

                //The Modern one needs to use the panelTransparent.
                panelTransparent.Controls.Add(Processing.Page = new Processing { Dock = DockStyle.Fill });
                Processing.MaximumValue(countList);

                this.Text = "Screen To Gif - " + Resources.Label_Processing;

                //Only set the dispose of the Processing page, if needed.
                if (Settings.Default.showFinished)
                {
                    Processing.Page.Disposed += processing_Disposed;
                }

                Processing.Status(0);
                Application.DoEvents();
            });

            #endregion

            if (Settings.Default.encodingCustom)
            {
                #region Ngif encoding

                int numImage = 0;

                #region Paint Unchanged Pixels

                var listToEncode = new List<FrameInfo>();

                if (Settings.Default.paintTransparent)
                {
                    this.Invoke((Action)(delegate
                    {
                        Processing.Defined(Resources.Label_AnalyzingUnchanged);
                        Processing.Status(0);
                    }));

                    listToEncode = ImageUtil.PaintTransparentAndCut(_listFrames, Settings.Default.transparentColor);
                }

                #endregion

                using (_encoder = new AnimatedGifEncoder())
                {
                    _encoder.Start(_outputpath);
                    _encoder.SetQuality(Settings.Default.quality);
                    _encoder.SetRepeat(Settings.Default.loop ? (Settings.Default.repeatForever ? 0 : Settings.Default.repeatCount) : -1); // 0 = Always, -1 once

                    this.Invoke((Action)(() => Processing.Defined(Resources.Label_Processing)));

                    #region For Each Frame

                    try
                    {
                        if (Settings.Default.paintTransparent)
                        {
                            #region With Transparency

                            _encoder.SetTransparent(Settings.Default.transparentColor);
                            _encoder.SetDispose(1); //Undraw Method, "Leave".

                            foreach (FrameInfo image in listToEncode)
                            {
                                var bitmapAux = new Bitmap(image.Image);

                                _encoder.SetDelay(_listDelay[numImage]);
                                _encoder.AddFrame(bitmapAux, image.PositionTopLeft.X, image.PositionTopLeft.Y);

                                bitmapAux.Dispose();
                                numImage++;

                                this.BeginInvoke((Action)(() => Processing.Status(numImage)));
                            }

                            #endregion
                        }
                        else
                        {
                            #region Without

                            foreach (var image in _listFrames)
                            {
                                _encoder.SetDelay(_listDelay[numImage]);
                                _encoder.AddFrame(image.From());
                                numImage++;

                                this.BeginInvoke((Action)(() => Processing.Status(numImage)));
                            }

                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Error in the Ngif encoding.");
                    }

                    #endregion
                }

                #region Clean Speciffic Variables

                listToEncode.Clear();
                listToEncode = null;

                #endregion

                #endregion
            }
            else
            {
                #region paint.NET encoding

                //BUG: The minimum amount of iterations is -1 (no repeat) or 3 (repeat number being 2), if you set repeat as 1, it will repeat 2 times, instead of just 1.
                //0 = Always, -1 = no repeat, n = repeat number (first shown + repeat number = total number of iterations)
                var repeat = (Settings.Default.loop ? (Settings.Default.repeatForever ? 0 : Settings.Default.repeatCount) : -1); // 0 = Always, -1 once

                using (var stream = new MemoryStream())
                {
                    using (var encoderNet = new GifEncoder(stream, null, null, repeat))
                    {
                        for (int i = 0; i < _listFrames.Count; i++)
                        {
                            var bitmapAux = new Bitmap(_listFrames[i]);
                            encoderNet.AddFrame(bitmapAux, 0, 0, TimeSpan.FromMilliseconds(_listDelay[i]));
                            bitmapAux.Dispose();

                            this.BeginInvoke((Action)(() => Processing.Status(i)));
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

            Directory.Delete(_pathTemp, true);

            #region Finish

            if (Settings.Default.showFinished)
            {
                this.Invoke((Action)delegate
                {
                    Processing.FinishedState(_outputpath, Resources.btnDone + "!");
                    this.Text = Resources.Title_EncodingDone;
                });

                //After the user hits "Close", the processing_Disposed is called. To set to the right stage.
            }
            else
            {
                this.Invoke((Action)delegate
                {
                    Processing.Page.Dispose();
                    this.Text = Resources.Title_EncodingDone;

                    this.Invalidate();
                    //Set again the transparency color
                    this.TransparencyKey = Color.LimeGreen;

                    FinishState();
                });

                try
                {
                    _actHook.Start(false, true); //start again the keyboard hook watcher
                }
                catch (Exception) { }
            }

            #endregion

            #region Memory Clearing

            _listFrames.Clear();
            _listDelay.Clear();

            if (_listFramesEdit != null)
            {
                _listFramesEdit.Clear();
                _listFramesUndo.Clear();

                _listDelayEdit.Clear();
                _listDelayUndo.Clear();
            }

            _listFrames = null;
            _listFramesEdit = null;
            _listFramesUndo = null;

            _listDelay = null;
            _listDelayEdit = null;
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
            this.Text = Resources.TitleStoped;
            this.TransparencyKey = Color.LimeGreen;
            this.Invalidate();

            FinishState();

            try
            {
                _actHook.Start(false, true); //start again the keyboard hook watcher
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Insert text in the picture with specific font and color.
        /// </summary>
        /// <param name="text">Content to insert</param>
        public void InsertText(String text)
        {
            this.Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("AddText", ActionEnum.AddText, 0, text);
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Show or hide the panelBottom buttons.
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

            //Inverted

            btnNext.Visible = isEdit;
            btnPrevious.Visible = isEdit;
            pbSeparator2.Visible = isEdit;
            btnFreeDrawing.Visible = isEdit;
            btnAddText.Visible = isEdit;
            lblDelay.Visible = isEdit;
            pictureBitmap.AllowDrop = isEdit;

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
            if (index >= 0 && index < _listFramesEdit.Count)
            {
                trackBar.Value = index;
                StopPreview(false);
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
        private void ApplyActionToFrames(string actionLabel, ActionEnum actionType, float pickerValue = 0, object param = null)
        {
            IList listIndexSelectedFrames;

            this.Invoke((Action)ResetUndoProp);

            int actualFrame = 0;

            this.Invoke((Action)delegate
            { actualFrame = trackBar.Value; });

            #region Apply Action to the Selected Frames

            if (tvFrames.IsFrameSelected(out listIndexSelectedFrames, actualFrame))
            {
                int removedCount = 0;
                bool overwrite = true;

                for (int i = 0; i < listIndexSelectedFrames.Count; i++)
                {
                    var frameIndex = (int)listIndexSelectedFrames[i];
                    var currentFrame = _listFramesEdit[frameIndex - removedCount].From();

                    #region Switch ActionType

                    switch (actionType)
                    {
                        case ActionEnum.Pixelate:
                            currentFrame = ImageUtil.Pixelate(currentFrame,
                                                        new Rectangle(0, 0, currentFrame.Width,
                                                        currentFrame.Height), Convert.ToInt32(pickerValue));
                            break;

                        case ActionEnum.Blur:
                            currentFrame = ImageUtil.Blur(currentFrame,
                                                        new Rectangle(0, 0, currentFrame.Width,
                                                        currentFrame.Height), Convert.ToInt32(pickerValue));
                            break;

                        case ActionEnum.Grayscale:
                            currentFrame =
                                        ImageUtil.Grayscale(currentFrame);
                            break;

                        case ActionEnum.Color:
                            currentFrame =
                                        ImageUtil.Colorize(currentFrame, (Color)param);
                            break;

                        case ActionEnum.Negative:
                            currentFrame =
                                        ImageUtil.Negative(currentFrame);
                            break;

                        case ActionEnum.Sepia:
                            currentFrame =
                                        ImageUtil.SepiaTone(currentFrame);
                            break;

                        case ActionEnum.Border:
                            currentFrame =
                                        ImageUtil.Border(currentFrame, pickerValue, (Color) param);
                            break;
                        case ActionEnum.Delete:
                            #region Delete

                            //index - 1 to delete the right frame.
                            _listFramesEdit.RemoveAt(frameIndex - removedCount);
                            _listDelayEdit.RemoveAt(frameIndex - removedCount);
                            tvFrames.Remove(1);
                            trackBar.Maximum = _listFramesEdit.Count - 1;
                            removedCount++;
                            overwrite = false;

                            #endregion
                            break;

                        case ActionEnum.Speed:
                            #region Speed

                            int value = Convert.ToInt32(_listDelayEdit[frameIndex] * pickerValue);

                            if (value >= 10 && value <= 2500)
                            {
                                _listDelayEdit[frameIndex] = value;
                            }
                            else if (value < 10) //Minimum
                            {
                                _listDelayEdit[frameIndex] = 10;
                            }
                            else if (value > 2500) //Maximum
                            {
                                _listDelayEdit[frameIndex] = 2500;
                            }

                            #endregion
                            break;

                        case ActionEnum.Caption:

                            #region Caption

                            if (param == null) break;

                            currentFrame = _listFramesEdit[frameIndex].From();

                            using (Graphics imgGr = Graphics.FromImage(currentFrame))
                            {
                                var graphPath = new GraphicsPath();

                                var fSt = (int)Settings.Default.fontCaption.Style;
                                var fF = Settings.Default.fontCaption.FontFamily;

                                StringFormat sFr = StringFormat.GenericDefault;

                                #region Horizontal Alignment

                                if (Settings.Default.captionHorizontalAlign == StringAlignment.Near)
                                {
                                    sFr.Alignment = StringAlignment.Near;
                                }
                                else if (Settings.Default.captionHorizontalAlign == StringAlignment.Center)
                                {
                                    sFr.Alignment = StringAlignment.Center;
                                }
                                else
                                {
                                    sFr.Alignment = StringAlignment.Far;
                                }

                                #endregion

                                #region Vertical Alignment

                                if (Settings.Default.captionVerticalAlign == StringAlignment.Near)
                                {
                                    sFr.LineAlignment = StringAlignment.Near;
                                }
                                else if (Settings.Default.captionVerticalAlign == StringAlignment.Center)
                                {
                                    sFr.LineAlignment = StringAlignment.Center;
                                }
                                else
                                {
                                    sFr.LineAlignment = StringAlignment.Far;
                                }

                                #endregion

                                #region Draw the string using a specific sizing type. Percentage of the height or Points

                                if (Settings.Default.fontSizeAsPercentage)
                                {
                                    graphPath.AddString(param.ToString(), fF, fSt, (currentFrame.Height * Settings.Default.fontCaptionPercentage),
                                        new Rectangle(new Point(0, 0), currentFrame.Size), sFr);
                                }
                                else
                                {
                                    graphPath.AddString(param.ToString(), fF, fSt, Settings.Default.fontCaption.Size, new Rectangle(new Point(0, 0), currentFrame.Size), sFr);
                                }

                                #endregion

                                #region Draw the path to the surface

                                if (Settings.Default.captionUseOutline)
                                {
                                    imgGr.DrawPath(new Pen(Settings.Default.captionOutlineColor,
                                            Settings.Default.captionOutlineThick), graphPath);
                                }
                                else
                                {
                                    imgGr.DrawPath(new Pen(Color.Transparent), graphPath);
                                }

                                #endregion

                                #region Fill the path with a solid color or a hatch brush

                                if (Settings.Default.captionUseHatch)
                                {
                                    imgGr.FillPath(new HatchBrush(Settings.Default.captionHatch, Settings.Default.captionHatchColor,
                                            Settings.Default.fontCaptionColor), graphPath);
                                }
                                else
                                {
                                    imgGr.FillPath(new SolidBrush(Settings.Default.fontCaptionColor), graphPath);
                                }

                                #endregion
                            }

                            #endregion
                            break;
                        case ActionEnum.Export:

                            #region Export

                            if (param.Equals(ImageFormat.Png))
                            {
                                currentFrame.Save(actionLabel + " (" + frameIndex + ").png", ImageFormat.Png);
                            }
                            else
                            {
                                currentFrame.Save(actionLabel + " (" + frameIndex + ").jpg", ImageFormat.Jpeg);
                            }

                            #endregion
                            break;
                        case ActionEnum.AddText:

                            #region AddText

                            Brush textBrush = new SolidBrush(Settings.Default.forecolorInsertText);

                            using (var myGraphic = Graphics.FromImage(currentFrame))
                            {
                                //Define the rectangle size by taking in consideration [X] and [Y] of 
                                // [_pointTextPosition] so the text matches the Bitmap l
                                var rectangleSize = new Size(currentFrame.Width - _pointTextPosition.X,
                                    currentFrame.Height - _pointTextPosition.Y);

                                //Insert text in the specified Point
                                myGraphic.DrawString(param.ToString(), Settings.Default.fontInsertText, textBrush, new Rectangle(_pointTextPosition,
                                    rectangleSize), new StringFormat());
                            }

                            #endregion
                            break;
                        case ActionEnum.FlipRotate:

                            currentFrame.RotateFlip((RotateFlipType)param);

                            break;
                        case ActionEnum.FreeDraw:

                            #region FreeDraw

                            using (var graphics = Graphics.FromImage(currentFrame))
                            {
                                graphics.DrawImage((Image)param, 0, 0);
                            }

                            #endregion
                            break;
                        case ActionEnum.Progress:

                            #region Progress

                            using (Graphics imgGr = Graphics.FromImage(currentFrame))
                            {
                                float actualValue = frameIndex + 1;
                                float maxValue = _listFramesEdit.Count;

                                Brush fillBrush = null;

                                #region Pen

                                float thickness = Settings.Default.progressThickAsPercentage ? Settings.Default.progressThickPercentage : Settings.Default.progressThickness;

                                if (Settings.Default.progressUseHatch)
                                {
                                    fillBrush = new HatchBrush(Settings.Default.progressHatch, Settings.Default.progressColor, Color.Transparent);
                                }
                                else
                                {
                                    fillBrush = new SolidBrush(Settings.Default.progressColor);
                                }

                                #endregion

                                #region Position

                                var rectangle = new Rectangle();

                                if (Settings.Default.progressPosition.Equals('T'))
                                {
                                    // 100 * 50 / 100
                                    float width = ((100 * actualValue) / maxValue) / 100;
                                    rectangle = new Rectangle(0, 0, (int)(currentFrame.Size.Width * width), (int)Settings.Default.progressThickness);
                                }
                                else if (Settings.Default.progressPosition.Equals('B'))
                                {
                                    float width = ((100 * actualValue) / maxValue) / 100;
                                    rectangle = new Rectangle(0, (int)(currentFrame.Size.Height - Settings.Default.progressThickness), (int)(currentFrame.Size.Width * width), (int)Settings.Default.progressThickness);
                                }
                                else if (Settings.Default.progressPosition.Equals('L'))
                                {
                                    float height = ((100 * actualValue) / maxValue) / 100;
                                    rectangle = new Rectangle(0, 0, (int)Settings.Default.progressThickness, (int)(currentFrame.Size.Height * height));
                                }
                                else
                                {
                                    float height = ((100 * actualValue) / maxValue) / 100;
                                    rectangle = new Rectangle((int)(currentFrame.Size.Width - Settings.Default.progressThickness), 0, (int)Settings.Default.progressThickness, (int)(currentFrame.Size.Height * height));
                                }

                                imgGr.FillRectangle(fillBrush, rectangle);

                                #endregion
                            }

                            #endregion

                            break;
                    }

                    #endregion

                    if (overwrite)
                    {
                        File.Delete(_listFramesEdit[frameIndex - removedCount]);

                        currentFrame.Save(_listFramesEdit[frameIndex - removedCount]);
                        currentFrame.Dispose();
                    }

                    GC.Collect(1);
                }

                this.Invoke((Action)delegate
                {
                    trackBar_ValueChanged(null, null);

                    //Highlight the node.
                    tvFrames.Focus();
                    tvFrames.SelectedNode =
                        tvFrames.Nodes[_parentNodeLabel].Nodes[trackBar.Value];
                });
            }

            #endregion
        }

        private void AddPictures(string fileName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                if (_listFramesEdit.Any())
                {
                    ResetUndoProp();
                }

                Size imageSize = _listFramesEdit.Count == 0 ? new Size(0, 0) : _listFramesEdit[0].From().Size;


                // Insert the frame(s) and set the last delay used.
                List<Bitmap> bitmapsList = ImageUtil.GetBitmapsFromFile(fileName, _listFramesEdit.Count, imageSize);

                bitmapsList.Reverse();

                int frame = 0;
                foreach (Bitmap item in bitmapsList)
                {
                    string endName = String.Format("{0}{1}I{2}.bmp", _pathTemp, frame, DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss"));

                    item.Save(endName, ImageFormat.Bmp);
                    _listFramesEdit.Insert(trackBar.Value, endName);
                    _listDelayEdit.Insert(trackBar.Value, _delay); //TODO: Get the delay.

                    frame++;
                }

                tvFrames.Add(bitmapsList.Count);

                bitmapsList.Clear();
                bitmapsList = null;

                GC.Collect();
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error importing image");

                var errorViewer = new ExceptionViewer(ex);
                errorViewer.ShowDialog();
            }
            finally
            {
                // Update the content for user
                trackBar.Maximum = _listFramesEdit.Count - 1;
                pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);
                this.Cursor = Cursors.Default;

                DelayUpdate();
            }
        }

        /// <summary>
        /// Creates the temp folder that holds all frames.
        /// </summary>
        private void CreateTemp()
        {
            #region Temp Folder

            if (!Directory.Exists(_pathTemp))
            {
                Directory.CreateDirectory(_pathTemp);
                Directory.CreateDirectory(_pathTemp + "Undo");
                Directory.CreateDirectory(_pathTemp + "Edit");
            }

            #endregion
        }

        #region NotifyIcon Methods

        private void ModifyCaptureTimerAndChangeTrayIconVisibility(bool isTimerAndTrayIconEnabled)
        {
            if (Settings.Default.fullscreen)
            {
                if (isTimerAndTrayIconEnabled)
                {
                    HideWindowAndShowTrayIcon();
                }
                else
                {
                    ShowWindowAndHideTrayIcon();
                }
            }

            ModifyCaptureTimer(isTimerAndTrayIconEnabled);
        }

        private void ModifyCaptureTimer(bool isEnabled)
        {
            if (Settings.Default.fullscreen)
            {
                if (Settings.Default.showCursor)
                {
                    timerCapWithCursorFull.Enabled = isEnabled;
                }
                else
                {
                    timerCaptureFull.Enabled = isEnabled;
                }
            }
            else
            {
                if (Settings.Default.showCursor)
                {
                    timerCapWithCursor.Enabled = isEnabled;
                }
                else
                {
                    timerCapture.Enabled = isEnabled;
                }
            }
        }

        private void HideWindowAndShowTrayIcon()
        {
            _trayIcon.ShowTrayIcon();
            this.Visible = false;
        }

        private void ShowWindowAndHideTrayIcon()
        {
            _trayIcon.HideTrayIcon();
            this.Visible = true;
        }

        private void NotifyIconClicked(object sender, EventArgs eventArgs)
        {
            this.Visible = true;
        }

        #endregion

        #endregion

        #region Record Async

        /// <summary>
        /// Saves the Bitmap to the disk and adds the filename in the list of frames.
        /// </summary>
        /// <param name="filename">The final filename of the Bitmap.</param>
        /// <param name="bitmap">The Bitmap to save in the disk.</param>
        public delegate void AddFrame(string filename, Bitmap bitmap);

        private AddFrame _addDel;

        private void AddFrames(string filename, Bitmap bitmap)
        {
            _listFrames.Add(filename);
            bitmap.Save(filename);
            bitmap.Dispose();
        }

        private void CallBack(IAsyncResult r)
        {
            if (this.IsDisposed) return;

            _addDel.EndInvoke(r);
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
                this.Text = String.Format("Screen To Gif ({0}", _preStartCount + Resources.TitleSecondsToGo);
                _preStartCount--;
            }
            else
            {
                timerPreStart.Stop();
                btnRecordPause.Enabled = true;
                this.Invalidate(); //To redraw the form, activating OnPaint again

                if (Settings.Default.showCursor)
                {
                    #region If Show Cursor

                    if (!con_Snapshot.Checked)
                    {
                        if (!Settings.Default.fullscreen)
                        {
                            btnMinimize.Enabled = false;
                            timerCapWithCursor.Start(); //Record with the cursor
                        }
                        else
                        {
                            timerCapWithCursorFull.Start();
                        }

                        _stage = Stage.Recording;
                        btnMaximize.Enabled = false;
                        btnRecordPause.Text = Resources.Pause;
                        btnRecordPause.Image = Resources.Pause_17Blue;
                        btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                        AutoFitButtons();
                    }
                    else
                    {
                        _stage = Stage.Snapping;
                        btnRecordPause.Text = Resources.btnSnap;
                        this.Text = String.Format("Screen To Gif - {0}", Resources.Con_SnapshotMode);
                        btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                        AutoFitButtons();
                    }

                    #endregion
                }
                else
                {
                    #region If Not

                    if (!con_Snapshot.Checked)
                    {
                        if (!Settings.Default.fullscreen)
                        {
                            btnMinimize.Enabled = false;
                            timerCapture.Start();
                        }
                        else
                        {
                            timerCaptureFull.Start();
                        }

                        _stage = Stage.Recording;
                        btnMaximize.Enabled = false;
                        btnRecordPause.Text = Resources.Pause;
                        btnRecordPause.Image = Resources.Pause_17Blue;
                        btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                        AutoFitButtons();
                    }
                    else
                    {
                        _stage = Stage.Snapping;
                        btnRecordPause.Text = Resources.btnSnap;
                        this.Text = String.Format("Screen To Gif - {0}", Resources.Con_SnapshotMode);
                        btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                        AutoFitButtons();
                    }

                    #endregion
                }
            }
        }

        #region FullScreen

        /// <summary>
        /// Takes a screenshot of the entire area and add to the list.
        /// </summary>
        private void timerCaptureFull_Tick(object sender, EventArgs e)
        {
            //Take a screenshot of the entire.
            _gr.CopyFromScreen(0, 0, 0, 0, _sizeResolution, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list

            _addDel.BeginInvoke(String.Format("{0}{1}.bmp", _pathTemp, _frameCount), new Bitmap(_bt), CallBack, null);

            this.Invoke((Action)(() => this.Text = String.Format("Screen To Gif • {0}", _frameCount)));

            _frameCount++;
        }

        /// <summary>
        /// Takes a screenshot of th entire area and add to the list, plus add to the list the position and icon of the cursor.
        /// </summary>
        private void timerCapWithCursorFull_Tick(object sender, EventArgs e)
        {
            _cursorInfo = new CursorInfo
            {
                IconImage = _capture.CaptureImageCursor(ref _posCursor),
                Position = panelTransparent.PointToClient(_posCursor),
                Clicked = _recordClicked,
            };


            //saves to list the actual icon and position of the cursor
            _listCursor.Add(_cursorInfo);

            //Take a screenshot of the area.
            _gr.CopyFromScreen(0, 0, 0, 0, _sizeResolution, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list

            _addDel.BeginInvoke(String.Format("{0}{1}.bmp", _pathTemp, _frameCount), new Bitmap(_bt), CallBack, null);

            this.Invoke((Action)(() => this.Text = String.Format("Screen To Gif • {0}", _frameCount)));

            _frameCount++;
            GC.Collect(1);
        }

        #endregion

        #region Area

        /// <summary>
        /// Takes a screenshot of desired area and add to the list.
        /// </summary>
        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = new Point(this.Location.X + panelTransparent.Location.X, this.Location.Y + panelTransparent.Location.Y);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _addDel.BeginInvoke(String.Format("{0}{1}.bmp", _pathTemp, _frameCount), new Bitmap(_bt), CallBack, null);

            this.Invoke((Action)(() => this.Text = String.Format("Screen To Gif • {0}", _frameCount)));

            _frameCount++;
            GC.Collect(1);
        }

        /// <summary>
        /// Takes a screenshot of desired area and add to the list, plus add to the list the position and icon of the cursor.
        /// </summary>
        private void timerCapWithCursor_Tick(object sender, EventArgs e)
        {
            _cursorInfo = new CursorInfo
            {
                IconImage = _capture.CaptureImageCursor(ref _posCursor),
                Position = panelTransparent.PointToClient(_posCursor),
                Clicked = _recordClicked,
            };

            //Get actual icon of the cursor
            _listCursor.Add(_cursorInfo);
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + panelTransparent.Location.X, this.Location.Y + panelTransparent.Location.Y);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list

            _addDel.BeginInvoke(String.Format("{0}{1}.bmp", _pathTemp, _frameCount), new Bitmap(_bt), CallBack, null);

            this.Invoke((Action)(() => this.Text = String.Format("Screen To Gif • {0}", _frameCount)));

            _frameCount++;
            GC.Collect(1);
        }

        #endregion

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

            using (var bitmap = _listFramesEdit[0].From())
            {
                var sizeBitmap = new Size(bitmap.Size.Width + 100, bitmap.Size.Height + 160);

                //Only resize if the form is smaller than the image.
                if (this.Size.Width >= sizeBitmap.Width && this.Size.Height >= sizeBitmap.Height)
                {
                    bitmap.Dispose();
                    return;
                }

                if (!(sizeBitmap.Width > 700)) //700 minimum width
                {
                    sizeBitmap.Width = 700;
                }

                if (!(sizeBitmap.Height > 300)) //300 minimum height
                {
                    sizeBitmap.Height = 300;
                }

                this.Size = sizeBitmap;
            }

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

            foreach (var name in _listFramesEdit)
            {
                _listFramesUndo.Add(name.Replace("Edit", "Undo"));
                File.Copy(name, name.Replace("Edit", "Undo"), true);
            }

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayEdit);

            #endregion
        }

        /// <summary>
        /// Constructor of the Frame Edit Page.
        /// </summary>
        private void EditFrames()
        {
            this.Cursor = Cursors.WaitCursor;

            _listFramesEdit = new List<string>();
            _listFramesUndo = new List<string>();

            int frameActual = 0;
            foreach (var frame in _listFrames)
            {
                File.Copy(frame, _pathTemp + String.Format(@"Edit\{0}.bmp", frameActual));
                _listFramesEdit.Add(_pathTemp + String.Format(@"Edit\{0}.bmp", frameActual));

                File.Copy(frame, _pathTemp + String.Format(@"Undo\{0}.bmp", frameActual));
                _listFramesUndo.Add(_pathTemp + String.Format(@"Undo\{0}.bmp", frameActual));

                frameActual++;
            }

            //Copies the listDelay to all the lists
            _listDelayEdit = new List<int>(_listDelay);
            _listDelayUndo = new List<int>(_listDelay);

            Application.DoEvents();

            panelEdit.Visible = true;

            trackBar.Value = 0;
            trackBar.Maximum = _listFramesEdit.Count - 1;

            this.MinimumSize = new Size(100, 100);
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            ResizeFormToImage(); //Resizes the form to hold the image.

            pictureBitmap.Image = _listFramesEdit.First().From();

            #region Preview Config.

            _timerPlayPreview.Interval = _listDelayEdit[trackBar.Value];

            #endregion

            #region Delay Properties

            //Sets the initial location
            _lastPosition = lblDelay.Location;
            _delay = _listDelayEdit[0];
            lblDelay.Text = _delay + " ms";

            #endregion

            MainSplit.Panel1Collapsed = true;

            #region Add List of frames to the TreeView

            tvFrames.UpdateListFrames(_listFramesEdit.Count, _parentNodeLabel);

            Application.DoEvents();

            #endregion

            if (String.IsNullOrEmpty(ArgumentUtil.FileName))
            {
                this.Invoke((Action)(() => Processing.Page.Dispose()));
            }

            GC.Collect(3);

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Accepts all the alterations and hides this page.
        /// </summary>
        private void btnDone_Click(object sender, EventArgs e)
        {
            StopPreview();
            _listFrames.Clear();

            foreach (var name in _listFramesEdit)
            {
                _listFrames.Add(name.Replace(@"\Edit", ""));
                File.Copy(name, name.Replace(@"\Edit", ""), true);
            }

            _listDelay = new List<int>(_listDelayEdit);

            pictureBitmap.Cursor = Cursors.Default;
            btnHideListFrames_Click(null, null);
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
            btnHideListFrames_Click(null, null);
            pictureBitmap.Image = null;
            panelEdit.Visible = false;
            ShowHideButtons(false);

            Cancel();

            GC.Collect();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);
            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();

            GC.Collect(2);

            DelayUpdate();
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            StopPreview();

            if (_listFramesEdit.Count > 1) //If more than 1 image in the list
            {
                ResetUndoProp();

                File.Delete(_listFramesEdit[trackBar.Value]);
                _listFramesEdit.RemoveAt(trackBar.Value); //delete the selected frame
                _listDelayEdit.RemoveAt(trackBar.Value); //and delay.
                tvFrames.Nodes[0].Nodes.RemoveAt(trackBar.Value);

                trackBar.Maximum = _listFramesEdit.Count - 1;
                pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();

                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

                DelayUpdate();
                GC.Collect();
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            StopPreview();

            int countDiff = _listFramesEdit.Count - _listFramesUndo.Count;

            #region Revert Alterations - This one is the oposite of ResetUndoProp()

            //Resets the list to a previous state
            _listFramesEdit.Clear();
            //_listFramesEdit = new List<string>(_listFramesUndo);

            foreach (var name in _listFramesUndo)
            {
                _listFramesEdit.Add(name.Replace("Undo", "Edit"));
                File.Copy(name, name.Replace("Undo", "Edit"), true);
            }

            //Resets the list to a previous state
            _listDelayEdit.Clear();
            _listDelayEdit = new List<int>(_listDelayUndo);

            #endregion

            trackBar.Maximum = _listFramesEdit.Count - 1;
            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            btnUndo.Enabled = false;

            ResizeFormToImage();

            #region Update TreeView

            if (countDiff > 0)
            {
                //If Undo is smaller, it means that we need to remove
                tvFrames.Remove(countDiff);
            }
            else if (countDiff < 0)
            {
                //If Undo is greater, it means that we need to add
                tvFrames.Add(Math.Abs(countDiff));
            }

            #endregion

            DelayUpdate();
            GC.Collect();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            StopPreview(false);
            btnUndo.Enabled = true;

            int countDiff = _listFramesEdit.Count - _listFrames.Count;

            #region Resets the list of frames

            _listFramesUndo.Clear();

            foreach (var name in _listFramesEdit)
            {
                _listFramesUndo.Add(name.Replace("Edit", "Undo"));
                File.Copy(name, name.Replace("Edit", "Undo"), true);
            }

            _listFramesEdit.Clear();

            int frameActual = 0;
            foreach (var frame in _listFrames)
            {
                File.Copy(frame, _pathTemp + String.Format(@"Edit\{0}.bmp", frameActual), true);
                _listFramesEdit.Add(_pathTemp + String.Format(@"Edit\{0}.bmp", frameActual));

                frameActual++;
            }

            #endregion

            #region Resets the list of delays

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayEdit);

            _listDelayEdit.Clear();
            _listDelayEdit = new List<int>(_listDelay);

            #endregion

            trackBar.Maximum = _listFramesEdit.Count - 1;
            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            ResizeFormToImage();

            #region Update TreeView

            if (countDiff > 0)
            {
                //If Undo is smaller, it means that we need to remove
                tvFrames.Remove(countDiff);
            }
            else if (countDiff < 0)
            {
                //If Undo is greater, it means that we need to add
                tvFrames.Add(Math.Abs(countDiff));
            }

            #endregion

            DelayUpdate();
            GC.Collect();
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
            contextMenu.Show(btnOptions, 0, btnOptions.Height);
        }

        #region Context Menu Itens

        private void con_addText_Click(object sender, EventArgs e)
        {
            if (pictureBitmap.Cursor == Cursors.Cross)
            {
                pictureBitmap.Cursor = Cursors.Default;
                return;
            }

            StopPreview(true);
            ResetUndoProp();

            var tip = new ToolTip();
            tip.Show(Resources.Tooltip_SelectPoint,
                this, PointToClient(MousePosition), 5 * 1000);

            //Displays a Cross cursor, to indicate a point selection
            pictureBitmap.Cursor = Cursors.Cross;
        }

        private void con_addCaption_Click(object sender, EventArgs e)
        {
            if (!con_tbCaption.Text.Equals(String.Empty))
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Caption", ActionEnum.Caption, 0, con_tbCaption.Text);

                this.Cursor = Cursors.Default;
            }
        }

        private void con_CaptionOptions_Click(object sender, EventArgs e)
        {
            var bitmapAux = new Bitmap(_listFramesEdit[trackBar.Value]);

            var capOptions = new CaptionOptions(bitmapAux.Height);
            capOptions.ShowDialog();

            bitmapAux.Dispose();
            capOptions.Dispose();
            GC.Collect();
        }

        private void con_deleteAfter_Click(object sender, EventArgs e)
        {
            if (_listFramesEdit.Count <= 1) return;

            ResetUndoProp();

            int countList = _listFramesEdit.Count - 1; //So we have a fixed value
            int removeCount = 0;

            for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
            {
                File.Delete(_listFramesEdit[i]);
                _listFramesEdit.RemoveAt(i);
                _listDelayEdit.RemoveAt(i);
                removeCount++;
            }

            trackBar.Maximum = _listFramesEdit.Count - 1;
            trackBar.Value = _listFramesEdit.Count - 1;

            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            tvFrames.Remove(removeCount);
            DelayUpdate();
            GC.Collect();
        }

        private void con_deleteBefore_Click(object sender, EventArgs e)
        {
            if (_listFramesEdit.Count <= 1) return;

            ResetUndoProp();

            int removeCount = 0;

            for (int i = trackBar.Value - 1; i >= 0; i--)
            {
                File.Delete(_listFramesEdit[i]);
                _listFramesEdit.RemoveAt(i); // I should use removeAt everywhere
                _listDelayEdit.RemoveAt(i);
                removeCount++;
            }

            trackBar.Maximum = _listFramesEdit.Count - 1;
            trackBar.Value = 0;

            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            tvFrames.Remove(removeCount);
            DelayUpdate();
            GC.Collect();
        }

        private void con_exportFrame_Click(object sender, EventArgs e)
        {
            StopPreview();

            var sfdExport = new SaveFileDialog();
            sfdExport.DefaultExt = "png";
            sfdExport.Filter = "JPG Image (*.jpg)|*.jpg| PNG Image (*.png)|*.png";
            sfdExport.FileName = Resources.Msg_Frame.TrimEnd();// + trackBar.Value;

            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                if (sfdExport.FileName.EndsWith(".png"))
                {
                    ApplyActionToFrames(sfdExport.FileName.Replace(".jpg", "").Replace(".png", ""), ActionEnum.Export, 0F, ImageFormat.Png);
                }
                else
                {
                    ApplyActionToFrames(sfdExport.FileName.Replace(".jpg", "").Replace(".png", ""), ActionEnum.Export, 0F, ImageFormat.Jpeg);
                }

                GC.Collect();

                this.Cursor = Cursors.Default;
            }

            sfdExport.Dispose();
            GC.Collect();
        }

        private void con_showGrid_CheckedChanged(object sender, EventArgs e)
        {
            //Change the background and the setting.
            RightSplit.Panel2.BackgroundImage =
                (Settings.Default.ShowGrid = con_showGrid.Checked) ? //If
                Resources.grid : null; //True-False
        }

        private void con_resizeAllFrames_Click(object sender, EventArgs e)
        {
            Bitmap bitmapResize = _listFramesEdit[trackBar.Value].From();

            var resize = new Resize(bitmapResize);

            if (resize.ShowDialog(this) == DialogResult.OK)
            {
                ResetUndoProp();

                Size resized = resize.GetSize();

                ImageUtil.ResizeBitmap(_listFramesEdit, resized.Width, resized.Height);

                pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();

                ResizeFormToImage();
            }

            resize.Dispose();
            GC.Collect();
        }

        private void con_cropAll_Click(object sender, EventArgs e)
        {
            var crop = new Crop(_listFramesEdit[trackBar.Value].From());

            if (crop.ShowDialog(this) == DialogResult.OK)
            {
                ResetUndoProp();

                ImageUtil.Crop(_listFramesEdit, crop.Rectangle);
                pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();

                ResizeFormToImage();
            }

            crop.Dispose();
            GC.Collect();
        }

        private void con_flipRotate_Click(object sender, EventArgs e)
        {
            var context = sender as ToolStripMenuItem;

            if (context == null) return;

            this.Cursor = Cursors.WaitCursor;
            ResetUndoProp();

            switch (context.AccessibleDescription)
            {
                case "Vertical":
                    ApplyActionToFrames("FlipRotate", ActionEnum.FlipRotate, 0, RotateFlipType.RotateNoneFlipY);
                    break;
                case "Horizontal":
                    ApplyActionToFrames("FlipRotate", ActionEnum.FlipRotate, 0, RotateFlipType.RotateNoneFlipX);
                    break;
                case "90C":
                    tvFrames.CheckAll();
                    ApplyActionToFrames("FlipRotate", ActionEnum.FlipRotate, 0, RotateFlipType.Rotate90FlipNone);
                    break;
                case "90CC":
                    tvFrames.CheckAll();
                    ApplyActionToFrames("FlipRotate", ActionEnum.FlipRotate, 0, RotateFlipType.Rotate270FlipNone);
                    break;
                case "180":
                    ApplyActionToFrames("FlipRotate", ActionEnum.FlipRotate, 0, RotateFlipType.Rotate180FlipNone);
                    break;
            }

            GC.Collect();
            ResizeFormToImage();
            this.Cursor = Cursors.Default;
        }

        private void con_deleteSelectedFrame_Click(object sender, EventArgs e)
        {
            if (_listFramesEdit.Count > 1 && !tvFrames.IsAllChecked())
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Delete", ActionEnum.Delete);
                tvFrames.UncheckAll();
                GC.Collect();

                this.Cursor = Cursors.Default;
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
            if (openImageDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            this.Cursor = Cursors.AppStarting;

            AddPictures(openImageDialog.FileName);
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// ListFramesInverted
        /// </summary>
        private void con_revertOrder_Click(object sender, EventArgs e)
        {
            if (_listFramesEdit.Count <= 1) return;

            this.Cursor = Cursors.AppStarting;

            ResetUndoProp();

            _listFramesEdit.Reverse();
            _listDelayEdit.Reverse();

            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            DelayUpdate();
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Make a Yoyo with the frames (listFrames + listFramesInverted)
        /// </summary>
        private void con_yoyo_Click(object sender, EventArgs e)
        {
            if (_listFramesEdit.Count <= 1) return;

            this.Cursor = Cursors.AppStarting;

            ResetUndoProp();

            int countDiff = _listFramesEdit.Count;
            _listFramesEdit = ImageUtil.Yoyo(_listFramesEdit); //TODO
            _listDelayEdit = ImageUtil.Yoyo<int>(_listDelayEdit);
            countDiff -= _listFramesEdit.Count;

            trackBar.Maximum = _listFramesEdit.Count - 1;
            pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

            tvFrames.Add(Math.Abs(countDiff));
            DelayUpdate();
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        private void con_Progress_Click(object sender, EventArgs e)
        {
            var progress = new Progress();

            if (progress.ShowDialog(this) == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Progress", ActionEnum.Progress);

                this.Cursor = Cursors.Default;
            }

            progress.Dispose();

            GC.Collect();
        }

        private void con_Transitions_Click(object sender, EventArgs e)
        {
            #region Gets the Next Frame

            int indexLast = trackBar.Value == trackBar.Maximum ?
                0 : (trackBar.Value + 1);

            string last = _listFramesEdit[indexLast];

            #endregion

            var transitions = new Transitions(_listFramesEdit[trackBar.Value].From(), last.From(), trackBar.Value, indexLast);

            if (transitions.ShowDialog() == DialogResult.OK)
            {
                #region If Ok

                ResetUndoProp();

                var listToAdd = new List<string>();
                int transitionCount = 0;

                foreach (Bitmap bitmap in transitions.ListToExport)
                {
                    string fileName = _listFramesEdit[trackBar.Value].
                        Replace(".bmp", String.Format("TR{0}.bmp", transitionCount));
                    bitmap.Save(fileName);
                    transitionCount++;

                    listToAdd.Add(fileName);
                }

                _listFramesEdit.InsertRange(trackBar.Value + 1, listToAdd);
                _listDelayEdit.InsertRange(trackBar.Value + 1, transitions.ListDelayExport);

                tvFrames.Add(transitions.ListToExport.Count);
                trackBar.Maximum += transitions.ListToExport.Count;

                #endregion
            }
        }

        /// <summary>
        /// Adds a border in all images. The border is painted in the image, the image don't increase in size.
        /// </summary>
        private void con_Border_Click(object sender, EventArgs e)
        {
            var borderOptions = new BorderOptions();

            if (borderOptions.ShowDialog(this) == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Border", ActionEnum.Border, Settings.Default.borderThickness, Settings.Default.borderColor);
                GC.Collect();

                this.Cursor = Cursors.Default;
            }
        }

        private void con_changeSpeed_Click(object sender, EventArgs e)
        {
            var valuePicker = new ValuePicker(Resources.MsgSelectSpeed, 500, 15, Resources.Label_Normal, 100);
            valuePicker.Lower = Resources.Label_Slower;
            valuePicker.Greater = Resources.Label_Faster;
            valuePicker.Normal = Resources.Label_Normal;

            if (valuePicker.ShowDialog(this) == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                if (valuePicker.Value != 0)
                {
                    float speed = 1;

                    //TODO: Remove range.
                    speed = valuePicker.Value / 100F;

                    ApplyActionToFrames("Slo-Motion", ActionEnum.Speed, speed);
                    GC.Collect();
                }

                this.Cursor = Cursors.Default;
            }
        }

        private void con_titleImage_Click(object sender, EventArgs e)
        {
            Size titleFrameSize = _listFramesEdit[trackBar.Value].From().Size;
            var titleBitmap = new Bitmap(titleFrameSize.Width, titleFrameSize.Height);
            var title = new TitleFrame(titleBitmap);

            if (title.ShowDialog() == DialogResult.OK)
            {
                #region If Ok

                ResetUndoProp();

                using (Graphics grp = Graphics.FromImage(titleBitmap))
                {
                    #region Create Title Image

                    if (title.Blured)
                    {
                        #region Blured

                        this.Cursor = Cursors.WaitCursor;
                        Bitmap blured;

                        if (_listFramesEdit.Count > (trackBar.Value - 1))
                        {
                            blured = _listFramesEdit[trackBar.Value + 1].From();
                        }
                        else
                        {
                            blured = _listFramesEdit[0].From(); //If the users wants to place the Title Frame in the end, the logical next frame will be the first.
                        }

                        blured = ImageUtil.Blur(blured, new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), 3);

                        Image bluredI = blured;
                        grp.DrawImage(bluredI, 0, 0);
                        this.Cursor = Cursors.Default;

                        blured.Dispose();
                        bluredI.Dispose();

                        #endregion
                    }
                    else
                    {
                        grp.FillRectangle(new SolidBrush(title.ColorBackground), 0, 0, titleBitmap.Width, titleBitmap.Height);
                    }

                    var strFormat = new StringFormat();
                    strFormat.Alignment = StringAlignment.Center;
                    strFormat.LineAlignment = StringAlignment.Center;

                    grp.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    grp.DrawString(title.Content, title.FontTitle, new SolidBrush(title.ColorForeground),
                        new RectangleF(0, 0, titleBitmap.Width, titleBitmap.Height), strFormat);

                    string fileName = _listFramesEdit[trackBar.Value].Replace(".bmp", "T.bmp");
                    titleBitmap.Save(fileName);
                    _listFramesEdit.Insert(trackBar.Value, fileName);

                    _listDelayEdit.Insert(trackBar.Value, 1000); //Inserts 1s delay.

                    trackBar.Maximum = _listFramesEdit.Count - 1;
                    pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
                    this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);

                    tvFrames.Add(1);
                    DelayUpdate();
                    GC.Collect();

                    #endregion
                }

                #endregion
            }
        }

        #region Filters

        #region Async Stuff

        private delegate void FilterDelegate(string actionLabel, ActionEnum actionType, float pickerValue = 0, string text = "");

        private FilterDelegate filterDel;

        private void CallBackFilter(IAsyncResult r)
        {
            filterDel.EndInvoke(r);

            this.Invoke((Action)delegate
            {
                pictureBitmap.Image = _listFramesEdit[trackBar.Value].From();
                panelEdit.Enabled = true;
                panelBottom.Enabled = true;
                GC.Collect();

                this.Cursor = Cursors.Default;
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
            GC.Collect();

            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Apply selected image to a pixelated filter
        /// </summary>
        private void Pixelate_Click(object sender, EventArgs e)
        {
            const string filterLabel = "Pixelate filter";

            //User first need to choose the intensity of the pixelate
            var valuePicker = new ValuePicker(100, 2, Resources.Msg_PixelSize);

            if (valuePicker.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;
                panelEdit.Enabled = false;
                panelBottom.Enabled = false;

                filterDel = ApplyActionToFrames;
                filterDel.BeginInvoke(filterLabel, ActionEnum.Pixelate, valuePicker.Value, null, CallBackFilter, null);
            }

            valuePicker.Dispose();
        }

        /// <summary>
        /// Apply selected image to a blur filter
        /// </summary>
        private void Blur_Click(object sender, EventArgs e)
        {
            const string filterLabel = "Blur filter";

            var valuePicker = new ValuePicker(5, 1, Resources.Msg_BlurIntense);

            if (valuePicker.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;
                panelEdit.Enabled = false;
                panelBottom.Enabled = false;

                filterDel = ApplyActionToFrames;
                filterDel.BeginInvoke(filterLabel, ActionEnum.Blur, valuePicker.Value, null, CallBackFilter, null);
            }

            valuePicker.Dispose();
        }

        /// <summary>
        /// Convert selected image to negative filter
        /// </summary>
        private void Negative_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Negative filter", ActionEnum.Negative);
            GC.Collect();

            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert selected image to SepiaTone filter
        /// </summary>
        private void SepiaTone_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Sepia filter", ActionEnum.Sepia);
            GC.Collect();

            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert selected image to a color filter
        /// </summary>
        private void con_Color_Click(object sender, EventArgs e)
        {
            var colorFilter = new ColorFilter();

            if (colorFilter.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Color filter", ActionEnum.Color, 0F, Color.FromArgb(colorFilter.Alpha, colorFilter.Red, colorFilter.Green, colorFilter.Blue));
                GC.Collect();

                this.Cursor = Cursors.Default;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Play Preview

        System.Windows.Forms.Timer _timerPlayPreview = new System.Windows.Forms.Timer();
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

            var bitmapAux = _listFramesEdit[trackBar.Value].From();

            //Calculates the exact position of the cursor over the image
            int crossY = e.Y - (pictureBitmap.Height - bitmapAux.Height) / 2;
            int crossX = e.X - (pictureBitmap.Width - bitmapAux.Width) / 2;

            //If position is out of bounds
            if ((crossX > bitmapAux.Width) || (crossY > bitmapAux.Height) ||
                crossX < 0 || crossY < 0)
            {
                toolTip.Show(Resources.Msg_WrongPosition, pictureBitmap, 0, pictureBitmap.Height, 2500);
                bitmapAux.Dispose();
                GC.Collect();
                return;
            }

            // Store point coordinates to insert text
            _pointTextPosition = new Point(crossX, crossY);

            // Initialize cursor for [pictureBitmap]
            pictureBitmap.Cursor = Cursors.Default;

            bitmapAux.Dispose();

            //Show TitleFrameSettings form as modal
            (new InsertText(false)).ShowDialog(this);

            GC.Collect();
        }

        private void PlayPreview()
        {
            if (_timerPlayPreview.Enabled)
            {
                _timerPlayPreview.Tick -= timerPlayPreview_Tick;
                _timerPlayPreview.Stop();

                lblDelay.Visible = true;
                trackBar.Value = _actualFrame;
                trackBar.Visible = true;
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);
                btnPreview.Text = Resources.Con_PlayPreview;
                btnPreview.Image = Resources.Play_17Green;

                DelayUpdate();
            }
            else
            {
                lblDelay.Visible = false;
                this.Text = String.Format("Screen To Gif - {0}", Resources.Title_PlayingAnimation);
                btnPreview.Text = Resources.Con_StopPreview;
                btnPreview.Image = Resources.Stop_17Red;
                _actualFrame = trackBar.Value;

                #region Starts playing the next frame

                if (_listFramesEdit.Count - 1 == _actualFrame)
                {
                    _actualFrame = 0;
                }
                else
                {
                    _actualFrame++;
                }

                #endregion

                _timerPlayPreview.Tick += timerPlayPreview_Tick;
                _timerPlayPreview.Start();

                trackBar.Visible = false;
            }
        }

        /// <summary>
        /// Stops the preview
        /// </summary>
        /// <param name="shouldSincronize">True if should update the value of the trackBar</param>
        private void StopPreview(bool shouldSincronize = true)
        {
            if (_timerPlayPreview.Enabled)
            {
                _timerPlayPreview.Tick -= timerPlayPreview_Tick;
                _timerPlayPreview.Stop();

                if (shouldSincronize)
                {
                    trackBar.Value = _actualFrame;
                }
            }

            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesEdit.Count - 1);
            lblDelay.Visible = true;
            trackBar.Visible = true;
            btnPreview.Text = Resources.Con_PlayPreview;
            btnPreview.Image = Resources.Play_17Green;

            DelayUpdate();
        }

        private void timerPlayPreview_Tick(object sender, EventArgs e)
        {
            _timerPlayPreview.Tick -= timerPlayPreview_Tick;

            //Sets the interval for this frame. If this frame has 500ms, the next frame will take 500ms to show.
            _timerPlayPreview.Interval = _listDelayEdit[_actualFrame];

            pictureBitmap.Image = new Bitmap(_listFramesEdit[_actualFrame]);

            if (_listFramesEdit.Count - 1 == _actualFrame)
            {
                _actualFrame = 0;
            }
            else
            {
                _actualFrame++;
            }

            _timerPlayPreview.Tick += timerPlayPreview_Tick;

            GC.Collect(2);
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
            //Application.DoEvents();

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
            _listDelayEdit[trackBar.Value] = _delay;
        }

        /// <summary>
        /// Called after the user changes the text of the toolstrip of the delay.
        /// </summary>
        private void con_tbDelay_TextChanged(object sender, EventArgs e)
        {
            if (con_tbDelay.Text.Equals(""))
                con_tbDelay.Text = "0";

            _delay = Convert.ToInt32(con_tbDelay.Text);

            if (_delay > 2500)
            {
                _listDelayEdit[trackBar.Value] = _delay = 2500;
            }
            else if (_delay < 10)
            {
                _listDelayEdit[trackBar.Value] = _delay = 10;
            }
            else
            {
                _listDelayEdit[trackBar.Value] = _delay;
            }

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

            _delay = _listDelayEdit[trackBar.Value];
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
                {
                    //TODO: Make to unselect as well.
                    if (tvFrames.Shift && tvFrames.First != -1) // last != -1 &&
                    {
                        #region If Shift-clicking and first frame selected

                        if (tvFrames.Last == -1)
                        {
                            tvFrames.Last = e.Node.Index;
                        }

                        //Remove the AfterCheck to prevent conflict
                        tvFrames.AfterCheck -= tvFrames_AfterCheck;

                        #region Check all

                        if (tvFrames.First < tvFrames.Last)
                        {
                            for (int i = tvFrames.First; i < tvFrames.Last; i++)
                            {
                                tvFrames.Nodes[0].Nodes[i].Checked = true;
                            }
                        }
                        else
                        {
                            for (int i = tvFrames.First; i > tvFrames.Last; i--)
                            {
                                tvFrames.Nodes[0].Nodes[i].Checked = true;
                            }
                        }

                        #endregion

                        tvFrames.First = e.Node.Index;
                        tvFrames.Last = -1;

                        tvFrames.Shift = false;
                        tvFrames.AfterCheck += tvFrames_AfterCheck;

                        #endregion
                    }
                    else if (!tvFrames.Shift)
                    {
                        tvFrames.First = e.Node.Index;
                        tvFrames.Last = -1;
                    }

                    //Or display the (un)checked frame
                    SelectFrame(e.Node.Index);
                }


                //Select current node
                tvFrames.SelectedNode = e.Node;

                #endregion
            }
        }

        private void tvFrames_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Display selected frame 
            if (e.Action != TreeViewAction.Unknown && e.Node != tvFrames.Nodes[0])
                SelectFrame(e.Node.Index);
        }

        #region Context Menu

        private void tvFrames_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            StopPreview();

            if (e.Button == MouseButtons.Right)
            {
                tvFrames.SelectedNode = e.Node;

                if (e.Node != tvFrames.Nodes[0])
                    SelectFrame(e.Node.Index);
            }
        }

        private void contextMenuTreeview_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopPreview();

            if (tvFrames.SelectedNode != tvFrames.Nodes[0])
            {
                con_MoveUpwards.Enabled = tvFrames.SelectedNode.Index != 0;
                con_MoveDownwards.Enabled = tvFrames.SelectedNode.Index != tvFrames.Nodes[0].Nodes.Count - 1;

                contextMenuTreeview.Show();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void con_MakeACopy_Click(object sender, EventArgs e)
        {
            StopPreview();

            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;

            string fileName = _listFramesEdit[trackBar.Value].Replace(".bmp", "C.bmp");
            File.Copy(_listFramesEdit[trackBar.Value], fileName);

            _listFramesEdit.Insert(trackBar.Value + 1, fileName);
            _listDelayEdit.Insert(trackBar.Value + 1, _listDelayEdit[trackBar.Value]);

            trackBar.Maximum = _listDelayEdit.Count - 1;

            tvFrames.Nodes[0].Nodes.Insert(trackBar.Value + 1, tvFrames.SelectedNode.Text + Resources.Tree_Copy);

            DelayUpdate();
            GC.Collect();
        }

        private void con_MoveUpwards_Click(object sender, EventArgs e)
        {
            StopPreview();

            //A frame need to be selected.
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;
            //This action can't be applied to the top frame.
            if (tvFrames.SelectedNode.Index == 0) return;

            //Copy, RemoveAt, Insert;
            var aux = _listFramesEdit[trackBar.Value];
            var auxDelay = _listDelayEdit[trackBar.Value];
            var auxNode = tvFrames.Nodes[0].Nodes[trackBar.Value];

            _listFramesEdit.RemoveAt(trackBar.Value);
            _listDelayEdit.RemoveAt(trackBar.Value);
            tvFrames.Nodes[0].Nodes.RemoveAt(trackBar.Value);

            _listFramesEdit.Insert(trackBar.Value - 1, aux);
            _listDelayEdit.Insert(trackBar.Value - 1, auxDelay);
            tvFrames.Nodes[0].Nodes.Insert(trackBar.Value - 1, auxNode);

            tvFrames.SelectedNode = tvFrames.Nodes[0].Nodes[trackBar.Value - 1];
            SelectFrame(trackBar.Value - 1);

            DelayUpdate();
            GC.Collect();
        }

        private void con_MoveDownwards_Click(object sender, EventArgs e)
        {
            StopPreview();

            //A frame need to be selected.
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;
            //This action can't be applied to the bottom frame.
            if (tvFrames.SelectedNode.Index == tvFrames.Nodes[0].Nodes.Count - 1) return;

            //Copy, RemoveAt, Insert;
            var aux = _listFramesEdit[trackBar.Value];
            var auxDelay = _listDelayEdit[trackBar.Value];
            var auxNode = tvFrames.Nodes[0].Nodes[trackBar.Value];

            _listFramesEdit.RemoveAt(trackBar.Value);
            _listDelayEdit.RemoveAt(trackBar.Value);
            tvFrames.Nodes[0].Nodes.RemoveAt(trackBar.Value);

            _listFramesEdit.Insert(trackBar.Value + 1, aux);
            _listDelayEdit.Insert(trackBar.Value + 1, auxDelay);
            tvFrames.Nodes[0].Nodes.Insert(trackBar.Value + 1, auxNode);

            tvFrames.SelectedNode = tvFrames.Nodes[0].Nodes[trackBar.Value + 1];
            SelectFrame(trackBar.Value + 1);

            DelayUpdate();
            GC.Collect();
        }

        private void con_RenameFrame_Click(object sender, EventArgs e)
        {
            StopPreview();

            //A frame need to be selected.
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;

            tvFrames.SelectedNode.BeginEdit();
        }

        #endregion

        #endregion

        #region Drag And Drop Events

        private void pictureBitmap_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void pictureBitmap_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (null != fileNames)
            {
                foreach (string fileName in fileNames)
                {
                    AddPictures(fileName);
                }
            }
        }

        #endregion

        #region Other Events

        #region Context_Record

        private void con_Fullscreen_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.fullscreen = con_Fullscreen.Checked;
        }

        private void con_Snapshot_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.snapshot = con_Snapshot.Checked;
        }

        private void contextRecord_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            btnRecordPause.BackColor = Color.DodgerBlue;

            con_Snapshot.Enabled = con_Fullscreen.Enabled = _stage == Stage.Stopped;
        }

        private void contextRecord_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            btnRecordPause.BackColor = Color.FromArgb(239, 239, 242);
        }

        #endregion

        private void con_tbCaption_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                con_addCaption_Click(null, null);
                contextMenu.Close(ToolStripDropDownCloseReason.ItemClicked);
            }
        }

        #endregion
    }
}
