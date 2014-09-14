using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ScreenToGif.Capture;
using ScreenToGif.Controls;
using ScreenToGif.Encoding;
using ScreenToGif.Pages;
using ScreenToGif.Properties;
using ScreenToGif.Util;
using AnimatedGifEncoder = ScreenToGif.Encoding.AnimatedGifEncoder;
using Cursors = System.Windows.Forms.Cursors;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace ScreenToGif
{
    /// <summary>
    /// Legacy
    /// </summary>
    public partial class Legacy : Form
    {
        #region Variables

        /// <summary>
        /// The animated gif encoder, this encodes the list of frames to a gif format.
        /// </summary>
        private AnimatedGifEncoder _encoder = new AnimatedGifEncoder();
        /// <summary>
        /// This object retrieves the icon of the cursor.
        /// </summary>
        private readonly CaptureScreen _capture = new CaptureScreen();
        /// <summary>
        /// The object of the keyboard hook.
        /// </summary>
        private readonly UserActivityHook _actHook;
        /// <summary>
        /// The editor may increase the size of the form, use this to go back to the last size (The size before opening the editor).
        /// </summary>
        private Size _lastSize; //The editor may increase the size of the form, use this to go back to the last size
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
        /// The list of bitmaps recorded.
        /// </summary>
        private List<Bitmap> _listBitmap;
        /// <summary>
        /// The list of information about the cursor.
        /// </summary>
        private List<CursorInfo> _listCursor = new List<CursorInfo>(); //List that stores the icon.
        /// <summary>
        /// The list of individual delays.
        /// </summary>
        private List<int> _listDelay = new List<int>();
        /// <summary>
        /// Object that holds the information of the cursor.
        /// </summary>
        private CursorInfo _cursorInfo;
        /// <summary>
        /// Holds the position of the cursor.
        /// </summary>
        private Point _posCursor;
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
        private string _parentNodeLabel = Resources.Label_All;

        /// <summary>
        /// Displays a tray icon.
        /// </summary>
        private readonly ScreenToGifTrayIcon _trayIcon = new ScreenToGifTrayIcon();

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
        };

        #endregion

        #endregion

        private bool _recordClicked = false;

        /// <summary>
        /// The contructor of the Legacy Form.
        /// </summary>
        public Legacy()
        {
            InitializeComponent();

            #region Load Save Data

            //Gets and sets the fps
            numMaxFps.Value = Settings.Default.maxFps;

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

            //Performance and flickering tweaks
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);

            //Gets the window chrome offset
            _offsetX = (this.Size.Width - panelTransparent.Width) / 2;
            _offsetY = (this.Size.Height - panelTransparent.Height - _offsetX - panelBottom.Height - 1);

            #region Window Bugfix

            //var osInfo = System.Environment.OSVersion;

            //if (osInfo.Version.Major == 6 && osInfo.Version.Build >= 9000)
            //{
            this.ResizeBegin += Legacy_ResizeBegin;
            this.ResizeEnd += Legacy_ResizeEnd;
            //}
            #endregion

            #region Global Hook

            try
            {
                _actHook = new UserActivityHook();
                _actHook.KeyDown += KeyHookTarget;
                _actHook.Start(true, true); //true for the mouse, true for the keyboard.
            }
            catch (Exception) { }

            #endregion

            //If there is a gif file as argument, read the file and jump directly to the editor.
            if (!String.IsNullOrEmpty(ArgumentUtil.FileName))
            {
                //TODO: Redo this code. It works, but...
                _listFramesPrivate = new List<Bitmap>();
                _listDelayPrivate = new List<int>();

                _listFramesUndo = new List<Bitmap>();
                _listDelayUndo = new List<int>();

                _delay = 66; //We should get the true delay.

                AddPictures(ArgumentUtil.FileName);

                btnUndo.Enabled = false;
                btnReset.Enabled = false;

                _listBitmap = new List<Bitmap>(_listFramesPrivate);
                _listDelay = new List<int>(_listDelayPrivate);

                EditFrames();
            }

            _trayIcon.NotifyIconClicked += NotifyIconClicked;
        }

        #region Override (Shortcut Keys)

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
                    btnDeleteFrame_Click(null, null);
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
                    con_exportFrames_Click(null, null);
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

        #region Main Form Resize/Closing

        /// <summary>
        /// Used to update the Size values in the bottom of the window
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (_screenSizeEdit) return;

            tbHeight.Text = panelTransparent.Height.ToString();
            tbWidth.Text = panelTransparent.Width.ToString();

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

        #endregion

        #region Bottom buttons

        private readonly Control info = new Information(); //Information page
        private readonly Control appSettings = new AppSettings(true); //App Settings page, true = legacy, false = modern
        private readonly Control gifSettings = new GifSettings(); //Gif Settings page

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            Stop();
        }

        private void btnRecordPause_Click(object sender, EventArgs e)
        {
            RecordPause(); //and start the pre-start tick
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
                appSettings.Dock = DockStyle.Fill;
                panelTransparent.Visible = true;

                _isPageAppOpen = true;
                _isPageGifOpen = false;
                _isPageInfoOpen = false;

                this.TransparencyKey = Color.Empty;
            }
        }

        private void btnGifConfig_Click(object sender, EventArgs e)
        {
            Control ctrlParent = panelTransparent; //Defines the parent

            if (_stage == Stage.Editing)
            {
                panelEdit.Visible = !panelEdit.Visible;
            }

            if (_isPageGifOpen)
            {
                ctrlParent.Controls.Clear(); //Removes all pages

                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.BackColor = Color.LimeGreen;

                _isPageGifOpen = false;
            }
            else
            {
                //Need this line, because there is a pictureBox with color, so if the user select the same color 
                //as this.TransparencyKey, the color won't be showed. This needs to be re-set after closing the gif config page.
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

            if (_stage == Stage.Editing)
            {
                panelEdit.Visible = !panelEdit.Visible;
            }

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

            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
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

            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            DelayUpdate();
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
        /// MouseHook event method, detects the mouse clicks.
        /// </summary>
        private void MouseHookTarget(object sender, System.Windows.Forms.MouseEventArgs keyEventArgs)
        {
            _recordClicked = keyEventArgs.Button == MouseButtons.Left;
        }

        /// <summary>
        /// Method that starts or pauses the recording
        /// </summary>
        private void RecordPause()
        {
            if (_stage == Stage.Stopped) //if stoped, starts recording
            {
                #region To Record

                #region Remove all pages

                panelTransparent.BackColor = Color.LimeGreen;
                this.TransparencyKey = Color.LimeGreen;
                panelTransparent.Controls.Clear(); //Removes all pages from the top

                _isPageInfoOpen = false;
                _isPageAppOpen = false;
                _isPageGifOpen = false;

                #endregion

                timerCapture.Interval =
                timerCaptureFull.Interval =
                timerCapWithCursor.Interval =
                timerCapWithCursorFull.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                _listBitmap = new List<Bitmap>(); //List that contains all the frames.
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

                if (Settings.Default.preStart) //if should show the pre start countdown
                {
                    this.Text = "Screen To Gif (2" + Resources.TitleSecondsToGo;
                    btnRecordPause.Enabled = false;

                    _stage = Stage.PreStarting;
                    _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                    timerPreStart.Start();
                }
                else
                {
                    btnRecordPause.Enabled = true;
                    this.MaximizeBox = false;

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
                                this.MinimizeBox = false;

                                timerCapWithCursor_Tick(null, null);
                                timerCapWithCursor.Start();
                            }
                            else
                            {
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
                                this.MinimizeBox = false;
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

                if (_stage != Stage.Stopped && _stage != Stage.PreStarting && _listBitmap.Any()) //if not already stoped or pre starting, stops
                {
                    #region To Stop and Save

                    try
                    {
                        _actHook.Stop(); //Stops the hook.
                    }
                    catch (Exception ex) { }

                    #region If show cursor is true, merge all bitmaps

                    if (Settings.Default.showCursor)
                    {
                        #region Merge Cursor and Bitmap

                        this.Cursor = Cursors.WaitCursor;
                        Graphics graph;
                        int numImage = 0;

                        Application.DoEvents();

                        foreach (var bitmap in _listBitmap)
                        {
                            try
                            {
                                graph = Graphics.FromImage(bitmap);

                                if (_listCursor[numImage].Clicked && Settings.Default.showMouseClick)
                                {
                                    //Draws the ellipse first, to  get behind the cursor.
                                    var rectEllipse = new Rectangle(_listCursor[numImage].Position.X - (_listCursor[numImage].Icon.Width / 2),
                                        _listCursor[numImage].Position.Y - (_listCursor[numImage].Icon.Height / 2), _listCursor[numImage].Icon.Width - 10, _listCursor[numImage].Icon.Height - 10);
                                    graph.DrawEllipse(new Pen(new SolidBrush(Color.Yellow), 3), rectEllipse);
                                }

                                var rect = new Rectangle(_listCursor[numImage].Position.X, _listCursor[numImage].Position.Y, _listCursor[numImage].Icon.Width, _listCursor[numImage].Icon.Height);
                                graph.DrawIcon(_listCursor[numImage].Icon, rect);

                                graph.Flush();
                            }
                            catch (Exception) { }

                            numImage++;
                        }

                        this.Cursor = Cursors.Default;

                        #endregion
                    }

                    #endregion

                    #region If fullscreen, resizes all the images, half of the size

                    if (Settings.Default.fullscreen)
                    {
                        this.Cursor = Cursors.AppStarting;

                        ShowWindowAndHideTrayIcon();

                        _listBitmap = new List<Bitmap>(ImageUtil.ResizeBitmap(_listBitmap,
                            Convert.ToInt32(_listBitmap[0].Size.Width / 2),
                            Convert.ToInt32(_listBitmap[0].Size.Height / 2)));

                        this.Cursor = Cursors.Default;
                    }

                    #endregion

                    #region Creates the Delay

                    this.Cursor = Cursors.WaitCursor;
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

                    foreach (var item in _listBitmap)
                    {
                        _listDelay.Add(delayGlobal);
                    }
                    this.Cursor = Cursors.Default;

                    #endregion

                    #region If the user wants to edit the frames or not

                    if (Settings.Default.allowEdit)
                    {
                        //To return back to the last form size after the editor
                        _lastSize = this.Size;
                        _stage = Stage.Editing;
                        this.MaximizeBox = true;
                        this.MinimizeBox = true;
                        //this.FormBorderStyle = FormBorderStyle.Sizable;
                        EditFrames();

                        ShowHideButtons(true);
                    }
                    else
                    {
                        _lastSize = this.Size; //Not sure why this is here
                        Save();
                    }

                    #endregion

                    #endregion
                }
                else if ((_stage == Stage.PreStarting || _stage == Stage.Snapping) && !_listBitmap.Any()) // if Pre-Starting or in Snapmode and no frames, stops.
                {
                    #region To Stop

                    timerPreStart.Stop();
                    _stage = Stage.Stopped;

                    //Enables the controls that are disabled while recording;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Enabled = true;
                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;
                    //this.FormBorderStyle = FormBorderStyle.Sizable;

                    this.MaximizeBox = true;
                    this.MinimizeBox = true;

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;
                    this.Text = Resources.TitleStoped;

                    AutoFitButtons();

                    try
                    {
                        //Re-starts the keyboard hook.
                        _actHook.OnMouseActivity += null;
                        _actHook.Start(true, true);
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
                #region If Not Save Directly to the desktop

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
                    _workerThread.Start();
                }
                else //if user don't want to save the gif
                {
                    FinishState();

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    this.Text = Resources.TitleStoped;

                    try
                    {
                        //_actHook.KeyDown += KeyHookTarget;
                        _actHook.Start(true, true);
                    }
                    catch (Exception) { }

                    return;
                }

                #endregion
            }
            else //if automatic save
            {
                #region Search For Filename

                bool searchForName = true;
                int numOfFile = 0;

                string path;

                //if there is no defined save location, saves in the desktop.
                if (!Settings.Default.folder.Equals(""))
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                }
                else
                {
                    path = Settings.Default.folder;
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
                        //_actHook.KeyDown += KeyHookTarget;
                        _actHook.Start(false, true);
                    }
                    catch (Exception) { }

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
            panelTransparent.Visible = true;
            panelBottom.Visible = true;
            _stage = Stage.Stopped;
            this.MinimumSize = new Size(100, 100);
            this.Size = _lastSize;

            numMaxFps.Enabled = true;
            tbHeight.Enabled = true;
            tbWidth.Enabled = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            btnRecordPause.Text = Resources.btnRecordPause_Record;
            btnRecordPause.Image = Resources.Record;

            AutoFitButtons();
        }

        /// <summary>
        /// Thread method that encodes the list of frames.
        /// </summary>
        private void DoWork()
        {
            _stage = Stage.Encoding;
            int countList = _listBitmap.Count;

            #region Show Processing

            var processing = new Processing();

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

                panelTransparent.Visible = false;
                this.Controls.Add(processing);
                processing.Dock = DockStyle.Fill;
                processing.SetMaximumValue(countList);

                this.Text = "Screen To Gif - " + Resources.Label_Processing;

                //Only set the dispose of the Processing page, if needed.
                if (Settings.Default.showFinished)
                {
                    processing.Disposed += processing_Disposed;
                }

                processing.SetStatus(0);
            });

            #endregion

            if (Settings.Default.encodingCustom) // if NGif encoding
            {
                #region Ngif encoding

                int numImage = 0;

                #region Paint Unchanged Pixels

                var listToEncode = new List<FrameInfo>();

                if (Settings.Default.paintTransparent)
                {
                    this.Invoke((Action)(() => processing.SetPreEncoding(Resources.Label_AnalyzingUnchanged)));

                    //ImageUtil.PaintTransparent(_listBitmap, Settings.Default.transparentColor);

                    //TODO: Show progress state.
                    listToEncode = ImageUtil.PaintTransparentAndCut(_listBitmap, Settings.Default.transparentColor);
                }

                #endregion

                using (_encoder = new AnimatedGifEncoder())
                {
                    _encoder.Start(_outputpath);
                    _encoder.SetQuality(Settings.Default.quality);
                    _encoder.SetRepeat(Settings.Default.loop ? (Settings.Default.repeatForever ? 0 : Settings.Default.repeatCount) : -1); // 0 = Always, -1 once

                    this.Invoke((Action)(() => processing.SetEncoding(Resources.Label_Processing)));

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
                                _encoder.SetDelay(_listDelay[numImage]);
                                _encoder.AddFrame(image.Image, image.PositionTopLeft.X, image.PositionTopLeft.Y);
                                numImage++;

                                this.BeginInvoke((Action)(() => processing.SetStatus(numImage)));
                            }

                            #endregion
                        }
                        else
                        {
                            #region Without

                            foreach (var image in _listBitmap)
                            {
                                _encoder.SetDelay(_listDelay[numImage]);
                                _encoder.AddFrame(image);
                                numImage++;

                                this.BeginInvoke((Action)(() => processing.SetStatus(numImage)));
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
                        LogWriter.Log(ex, "Error while writing to disk.");
                    }
                }

                #endregion
            }

            #region Finish

            if (Settings.Default.showFinished)
            {
                this.Invoke((Action)delegate
                {
                    processing.SetFinishedState(_outputpath, Resources.btnDone + "!");
                    this.Text = Resources.Title_EncodingDone;
                });

                //After the user hits "Close", the processing_Disposed is called. To set to the right stage.
            }
            else
            {
                this.Invoke((Action)delegate
                {
                    processing.Dispose();
                    this.Text = Resources.Title_EncodingDone;

                    this.Invalidate();
                    //Set again the transparency color
                    this.TransparencyKey = Color.LimeGreen;

                    FinishState();
                });

                try
                {
                    _actHook.Start(true, true); //start again the keyboard hook watcher
                }
                catch (Exception) { }
            }

            #endregion

            #region Memory Cleaning

            _listBitmap.Clear();
            _listDelay.Clear();

            //These variables are only used in the editor.
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
            this.Text = Resources.TitleStoped;

            this.TransparencyKey = Color.LimeGreen;
            this.Invalidate();

            FinishState();

            try
            {
                _actHook.Start(true, true); //start again the keyboard hook watcher
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

            //Inverted

            btnNext.Visible = isEdit;
            btnPrevious.Visible = isEdit;
            pbSeparator2.Visible = isEdit;
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
            if (index >= 0 && index < _listFramesPrivate.Count)
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
        /// <param name="param">Any other param.</param>
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

                for (int i = 0; i < listIndexSelectedFrames.Count; i++)
                {
                    var frameIndex = (int)listIndexSelectedFrames[i];
                    var currentFrame = _listFramesPrivate[frameIndex - removedCount];

                    #region Switch ActionType

                    switch (actionType)
                    {
                        case ActionEnum.Pixelate:
                            _listFramesPrivate[frameIndex] = ImageUtil.Pixelate(currentFrame,
                                                        new Rectangle(0, 0, currentFrame.Width,
                                                        currentFrame.Height), Convert.ToInt32(pickerValue));
                            break;

                        case ActionEnum.Blur:
                            _listFramesPrivate[frameIndex] = ImageUtil.Blur(currentFrame,
                                                        new Rectangle(0, 0, currentFrame.Width,
                                                        currentFrame.Height), Convert.ToInt32(pickerValue));
                            break;

                        case ActionEnum.Grayscale:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.Grayscale(currentFrame);
                            break;

                        case ActionEnum.Color:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.Colorize(currentFrame, (Color)param);
                            break;

                        case ActionEnum.Negative:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.Negative(currentFrame);
                            break;

                        case ActionEnum.Sepia:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.SepiaTone(currentFrame);
                            break;

                        case ActionEnum.Border:
                            _listFramesPrivate[frameIndex] =
                                        ImageUtil.Border(currentFrame, pickerValue);
                            break;
                        case ActionEnum.Delete:
                            #region Delete

                            //index - 1 to delete the right frame.
                            _listFramesPrivate.RemoveAt(frameIndex - removedCount);
                            _listDelayPrivate.RemoveAt(frameIndex - removedCount);
                            tvFrames.Remove(1);
                            trackBar.Maximum = _listFramesPrivate.Count - 1;
                            removedCount++;

                            #endregion
                            break;

                        case ActionEnum.Speed:

                            #region Speed

                            int value = Convert.ToInt32(_listDelayPrivate[frameIndex] / pickerValue);

                            if (value >= 10 && value <= 2500)
                            {
                                _listDelayPrivate[frameIndex] = value;
                            }
                            else if (value < 10) //Minimum
                            {
                                _listDelayPrivate[frameIndex] = 10;
                            }
                            else if (value > 2500) //Maximum
                            {
                                _listDelayPrivate[frameIndex] = 2500;
                            }

                            #endregion
                            break;

                        case ActionEnum.Caption:

                            #region Caption

                            if (param == null) break;

                            this.Invoke((Action)delegate
                            {
                                if (trackBar.Value + i == trackBar.Maximum + 1)
                                {
                                    return; //Bug
                                }
                            });

                            var image = new Bitmap(_listFramesPrivate[frameIndex]);

                            using (Graphics imgGr = Graphics.FromImage(image))
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
                                    graphPath.AddString(param.ToString(), fF, fSt, (image.Height * Settings.Default.fontCaptionPercentage),
                                        new Rectangle(new Point(0, 0), image.Size), sFr);
                                }
                                else
                                {
                                    graphPath.AddString(param.ToString(), fF, fSt, Settings.Default.fontCaption.Size, new Rectangle(new Point(0, 0), image.Size), sFr);
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

                                //Use the "new" keyword to prevent problems with the referenced image.
                                _listFramesPrivate[frameIndex] = new Bitmap(image);
                            }

                            #endregion
                            break;
                        case ActionEnum.Export:

                            #region Export

                            Bitmap expBitmap = _listFramesPrivate[frameIndex];

                            if (param.Equals(ImageFormat.Png))
                            {
                                expBitmap.Save(actionLabel + " (" + frameIndex + ").png", ImageFormat.Png);
                            }
                            else
                            {
                                expBitmap.Save(actionLabel + " (" + frameIndex + ").jpg", ImageFormat.Jpeg);
                            }

                            #endregion
                            break;
                        case ActionEnum.AddText:

                            #region AddText

                            Brush textBrush = new SolidBrush(Settings.Default.forecolorInsertText);
                            var currentBitmap = new Bitmap(_listFramesPrivate[frameIndex]);
                            Graphics myGraphic = Graphics.FromImage(currentBitmap);

                            //Define the rectangle size by taking in consideration [X] and [Y] of 
                            // [_pointTextPosition] so the text matches the Bitmap l
                            var rectangleSize = new Size(currentBitmap.Width - _pointTextPosition.X,
                                currentBitmap.Height - _pointTextPosition.Y);

                            //Insert text in the specified Point
                            myGraphic.DrawString(param.ToString(), Settings.Default.fontInsertText, textBrush, new Rectangle(_pointTextPosition,
                                rectangleSize), new StringFormat());

                            pictureBitmap.Image = _listFramesPrivate[frameIndex] = new Bitmap(currentBitmap);

                            #endregion
                            break;
                        case ActionEnum.FlipRotate:

                            #region FlipRotate

                            var aux = new Bitmap(_listFramesPrivate[frameIndex]);
                            aux.RotateFlip((RotateFlipType)param);
                            _listFramesPrivate[frameIndex] = new Bitmap(aux);
                            aux.Dispose();

                            #endregion
                            break;
                    }

                    #endregion
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

        /// <summary>
        /// Add frames to the recording.
        /// </summary>
        /// <param name="fileName">The file source.</param>
        private void AddPictures(string fileName)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                ResetUndoProp();

                // Insert the frame(s) and set the last delay used.
                List<Bitmap> bitmapsList = ImageUtil.GetBitmapsFromFile(fileName, _listFramesPrivate);

                //TODO: Use InsertRange, you won't need to reverse the list.
                // Reverse [bitmapsList] order before insertion
                // if not the Gif will be inserted from the end
                bitmapsList.Reverse();
                foreach (Bitmap item in bitmapsList)
                {
                    _listFramesPrivate.Insert(trackBar.Value, item);
                    _listDelayPrivate.Insert(trackBar.Value, _delay); //TODO: Get the delay.
                }

                tvFrames.Add(bitmapsList.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error type: " + ex.GetType().ToString() + "\n" +
                                "Message: " + ex.Message, "Error in " + MethodBase.GetCurrentMethod().Name);

                LogWriter.Log(ex, "Error importing image.");
            }
            finally
            {
                // Update the content for user
                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
                this.Cursor = Cursors.Default;

                DelayUpdate();
            }
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

        #region Timers

        /// <summary>
        /// Timer used after clicking in Record, to give the user a shor time to prepare recording
        /// </summary>
        private void PreStart_Tick(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                this.Text = "Screen To Gif (" + _preStartCount + Resources.TitleSecondsToGo;
                _preStartCount--;
            }
            else //if 0, starts (timer OR timer with cursor)
            {
                timerPreStart.Stop();
                btnRecordPause.Enabled = true;

                if (Settings.Default.showCursor)
                {
                    #region If Show Cursor

                    if (!con_Snapshot.Checked)
                    {
                        if (!Settings.Default.fullscreen)
                        {
                            this.MinimizeBox = false;
                            timerCapWithCursor.Start(); //Record with the cursor
                        }
                        else
                        {
                            timerCapWithCursorFull.Start();
                        }

                        _stage = Stage.Recording;
                        this.MaximizeBox = false;
                        btnRecordPause.Text = Resources.Pause;
                        btnRecordPause.Image = Resources.Pause_17Blue;
                        btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                        AutoFitButtons();
                    }
                    else
                    {
                        _stage = Stage.Snapping;
                        btnRecordPause.Text = Resources.btnSnap;
                        this.Text = "Screen To Gif - " + Resources.Con_SnapshotMode;
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
                            this.MinimizeBox = false;
                            timerCapture.Start();
                        }
                        else
                        {
                            timerCaptureFull.Start();
                        }

                        _stage = Stage.Recording;
                        this.MaximizeBox = false;
                        btnRecordPause.Text = Resources.Pause;
                        btnRecordPause.Image = Resources.Pause_17Blue;
                        btnRecordPause.ImageAlign = ContentAlignment.MiddleLeft;

                        AutoFitButtons();
                    }
                    else
                    {
                        _stage = Stage.Snapping;
                        btnRecordPause.Text = Resources.btnSnap;
                        this.Text = "Screen To Gif - " + Resources.Con_SnapshotMode;
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
            _listBitmap.Add((Bitmap)_bt.Clone());

            this.Invoke((Action)delegate
            {
                this.Text = "Screen To Gif • " + _frameCount;
                _frameCount++;
            });
        }

        /// <summary>
        /// Takes a screenshot of th entire area and add to the list, plus add to the list the position and icon of the cursor.
        /// </summary>
        private void timerCapWithCursorFull_Tick(object sender, EventArgs e)
        {
            _cursorInfo = new CursorInfo
            {
                Icon = _capture.CaptureIconCursor(ref _posCursor),
                Position = _posCursor
            };

            //saves to list the actual icon and position of the cursor
            _listCursor.Add(_cursorInfo);

            //Take a screenshot of the area.
            _gr.CopyFromScreen(0, 0, 0, 0, _sizeResolution, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.Clone());

            this.Invoke((Action)delegate
            {
                this.Text = "Screen To Gif • " + _frameCount;
                _frameCount++;
            });
        }

        #endregion

        #region Area

        /// <summary>
        /// Takes a screenshot of desired area and add to the list.
        /// </summary>
        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            var lefttop = new Point(this.Location.X + _offsetX, this.Location.Y + _offsetY);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.Clone());

            this.BeginInvoke((Action)delegate
            {
                this.Text = "Screen To Gif • " + _frameCount;
                _frameCount++;
            });
        }

        /// <summary>
        /// Takes a screenshot of desired area and add to the list, plus add to the list the position and icon of the cursor.
        /// </summary>
        private void timerCapWithCursor_Tick(object sender, EventArgs e)
        {
            _cursorInfo = new CursorInfo
            {
                Icon = _capture.CaptureIconCursor(ref _posCursor),
                Position = panelTransparent.PointToClient(_posCursor),
                Clicked = _recordClicked
            };

            //saves to list the actual icon and position of the cursor
            _listCursor.Add(_cursorInfo);
            //Get the actual position of the form.
            var lefttop = new Point(this.Location.X + _offsetX, this.Location.Y + _offsetY);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.Clone());

            this.BeginInvoke((Action)delegate
            {
                this.Text = "Screen To Gif • " + _frameCount;
                _frameCount++;
            });
        }

        #endregion

        #endregion

        #region TextBox Size

        /// <summary>
        /// Prevents keys!=numbers
        /// </summary>
        private void tbSize_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if only numbers, allow
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

            Size sizeBitmap = new Size(bitmap.Size.Width + 100, bitmap.Size.Height + 160);

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

            _timerPlayPreview.Tick += timerPlayPreview_Tick;
            _timerPlayPreview.Interval = _listDelayPrivate[trackBar.Value];
            //Should I add the first interval to 66? - Nicke

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
                tvFrames.UpdateListFrames(_listFramesPrivate.Count, _parentNodeLabel);
            }

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

            Save();

            GC.Collect();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

            DelayUpdate();
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            StopPreview();

            if (_listFramesPrivate.Count > 1) //If more than 1 image in the list
            {
                ResetUndoProp();

                _listFramesPrivate.RemoveAt(trackBar.Value); //delete the selected frame
                _listDelayPrivate.RemoveAt(trackBar.Value); //and delay.
                tvFrames.Nodes[0].Nodes.RemoveAt(trackBar.Value);

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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

            int countDiff = _listFramesPrivate.Count - _listFramesUndo.Count;

            #region Revert Alterations - This one is the oposite of ResetUndoProp()

            //Resets the list to a previous state
            _listFramesPrivate.Clear();
            _listFramesPrivate = new List<Bitmap>(_listFramesUndo);

            //Resets the list to a previous state
            _listDelayPrivate.Clear();
            _listDelayPrivate = new List<int>(_listDelayUndo);

            #endregion

            trackBar.Maximum = _listFramesPrivate.Count - 1;
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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

            int countDiff = _listFramesPrivate.Count - _listBitmap.Count;

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
            //TODO:
            //Find a way to make the text a little bit more smooth.

            if (!con_tbCaption.Text.Equals(String.Empty))
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Caption", ActionEnum.Caption, 0, con_tbCaption.Text);
                GC.Collect();

                this.Cursor = Cursors.Default;
            }
        }

        private void con_CaptionOptions_Click(object sender, EventArgs e)
        {
            var capOptions = new CaptionOptions(_listFramesPrivate[trackBar.Value].Height);
            capOptions.ShowDialog();

            capOptions.Dispose();
        }

        private void con_deleteAfter_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                int countList = _listFramesPrivate.Count - 1; //So we have a fixed value
                int removeCount = 0;

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    _listFramesPrivate.RemoveAt(i);
                    _listDelayPrivate.RemoveAt(i);
                    removeCount++;
                }

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                trackBar.Value = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                tvFrames.Remove(removeCount);
                DelayUpdate();
                GC.Collect();
            }
        }

        private void con_deleteBefore_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                ResetUndoProp();

                int removeCount = 0;

                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    _listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere
                    _listDelayPrivate.RemoveAt(i);
                    removeCount++;
                }

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                tvFrames.Remove(removeCount);
                DelayUpdate();
                GC.Collect();
            }
        }

        private void con_exportFrames_Click(object sender, EventArgs e)
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
            GC.Collect();
        }

        private void con_cropAll_Click(object sender, EventArgs e)
        {
            var crop = new Crop(_listFramesPrivate[trackBar.Value]);

            if (crop.ShowDialog(this) == DialogResult.OK)
            {
                ResetUndoProp();

                _listFramesPrivate = ImageUtil.Crop(_listFramesPrivate, crop.Rectangle);
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

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
            if (_listFramesPrivate.Count > 1 && !tvFrames.IsAllChecked())
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
        /// ListFrames Inverted
        /// </summary>
        private void con_revertOrder_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                this.Cursor = Cursors.AppStarting;

                ResetUndoProp();

                //_listFramesPrivate = ImageUtil.Revert(_listFramesPrivate); //change this.

                _listFramesPrivate.Reverse();
                _listDelayPrivate.Reverse();

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                DelayUpdate();
                GC.Collect();

                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Make a Yoyo with the frames (listFrames + listFramesInverted)
        /// </summary>
        private void con_yoyo_Click(object sender, EventArgs e)
        {
            if (_listFramesPrivate.Count > 1)
            {
                this.Cursor = Cursors.AppStarting;

                ResetUndoProp();

                int countDiff = _listFramesPrivate.Count;
                _listFramesPrivate = ImageUtil.Yoyo(_listFramesPrivate);
                _listDelayPrivate = ImageUtil.Yoyo<int>(_listDelayPrivate);
                countDiff -= _listFramesPrivate.Count;

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
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                tvFrames.Add(Math.Abs(countDiff));
                DelayUpdate();
                GC.Collect();

                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Adds a border in all images. The border is painted in the image, the image don't increase in size.
        /// </summary>
        private void con_Border_Click(object sender, EventArgs e)
        {
            //In the future, user could choose a color too.

            var valuePicker = new ValuePicker(10, 1, Resources.Msg_ChooseThick, "px");
            valuePicker.Unit = "px";

            if (valuePicker.ShowDialog(this) == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                ApplyActionToFrames("Border", ActionEnum.Border, valuePicker.Value);
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
            Size titleFrameSize = _listFramesPrivate[trackBar.Value].Size;
            var titleBitmap = new Bitmap(titleFrameSize.Width, titleFrameSize.Height);
            var title = new TitleFrameSettings(titleBitmap);

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

                    _listFramesPrivate.Insert(trackBar.Value, titleBitmap);

                    _listDelayPrivate.Insert(trackBar.Value, 1000); //Inserts 1s delay.

                    pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                    trackBar.Maximum = _listFramesPrivate.Count - 1;
                    pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                    this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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

            //Cross thread call;
            this.Invoke((Action)delegate
            {
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
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
            this.Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Grayscale filter", ActionEnum.Grayscale);
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Apply selected image to a pixelated filter
        /// </summary>
        private void Pixelate_Click(object sender, EventArgs e)
        {
            IList tempList;
            string filterLabel = "Pixelate filter";

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
            IList tempList;
            string filterLabel = "Blur filter";

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
            this.Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Negative filter", ActionEnum.Negative);
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert selected image to SepiaTone filter
        /// </summary>
        private void SepiaTone_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            ApplyActionToFrames("Sepia filter", ActionEnum.Sepia);
            GC.Collect();

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert selected image to a color filter
        /// </summary>
        private void Color_Click(object sender, EventArgs e)
        {
            var colorFilter = new ColorFilter();

            if (colorFilter.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                //TODO: Change this to a simple merge instead of color matrix.
                //float red = colorFilter.Red / 255F;
                //float green = colorFilter.Green / 255F;
                //float blue = colorFilter.Blue / 255F;
                //float alpha = colorFilter.Alpha / 255F;

                //var color = new float[] { red, green, blue, alpha };

                ApplyActionToFrames("Color filter", ActionEnum.Color, 0F, Color.FromArgb(colorFilter.Alpha, colorFilter.Red, colorFilter.Green, colorFilter.Blue));
                GC.Collect();

                this.Cursor = Cursors.Default;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Play Preview

        readonly System.Windows.Forms.Timer _timerPlayPreview = new System.Windows.Forms.Timer();
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
            (new InsertText(true)).ShowDialog(this);

            GC.Collect();
        }

        private void PlayPreview()
        {
            if (_timerPlayPreview.Enabled)
            {
                _timerPlayPreview.Stop();
                lblDelay.Visible = true;
                trackBar.Value = _actualFrame;
                trackBar.Visible = true;
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

                #region Starts playing the next frame

                if (_listFramesPrivate.Count - 1 == _actualFrame)
                {
                    _actualFrame = 0;
                }
                else
                {
                    _actualFrame++;
                }

                #endregion

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
                _timerPlayPreview.Stop();
                if (shouldSincronize)
                {
                    trackBar.Value = _actualFrame;
                }
            }

            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
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
            _timerPlayPreview.Interval = _listDelayPrivate[_actualFrame];

            pictureBitmap.Image = _listFramesPrivate[_actualFrame];

            if (_listFramesPrivate.Count - 1 == _actualFrame)
            {
                _actualFrame = 0;
            }
            else
            {
                _actualFrame++;
            }

            _timerPlayPreview.Tick += timerPlayPreview_Tick;
        }

        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopPreview();
        }

        /// <summary>
        /// Stops the preview if the user scrolls the trackbar
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
        private bool _labelClicked;

        /// <summary>
        /// Updates the <see cref="_labelClicked"/> flag or opens the contextMenu.
        /// </summary>
        private void lblDelay_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _labelClicked = true;
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
            if (!_labelClicked)
                return;

            if ((e.Y - _lastPosition.Y) < 0)
            {
                if (_delay < 2500)
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
        /// Updates the flag <see cref="_labelClicked"/> to false and sets the delay value to the list.
        /// </summary>
        private void lblDelay_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _labelClicked = false;

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

            if (_delay > 2500)
            {
                _listDelayPrivate[trackBar.Value] = _delay = 2500;
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

        #region Window bug fix

        private void Legacy_ResizeBegin(object sender, EventArgs e)
        {
            //This fix the bug of the window
            panelTransparent.BackColor = Color.Cornsilk;
            this.TransparencyKey = Color.Cornsilk;
        }

        private void Legacy_ResizeEnd(object sender, EventArgs e)
        {
            //this.Refresh();
            this.UpdateBounds(this.Left, this.Top, this.Size.Width, this.Size.Height);

            if (!_isPageAppOpen && !_isPageGifOpen && !_isPageInfoOpen)
            {
                panelTransparent.BackColor = Color.LimeGreen;
                this.TransparencyKey = Color.LimeGreen;
            }
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
            //Display selected frame 
            if (e.Action != TreeViewAction.Unknown && e.Node != tvFrames.Nodes[0])
                SelectFrame(e.Node.Index);
        }

        #region Context Menu

        private void tvFrames_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                tvFrames.SelectedNode = e.Node;

                if (e.Node != tvFrames.Nodes[0])
                    SelectFrame(e.Node.Index);
            }
        }

        private void contextMenuTreeview_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;

            _listFramesPrivate.Insert(trackBar.Value, _listFramesPrivate[trackBar.Value]);
            _listDelayPrivate.Insert(trackBar.Value, _listDelayPrivate[trackBar.Value]);

            trackBar.Maximum = _listDelayPrivate.Count - 1;

            tvFrames.Nodes[0].Nodes.Insert(trackBar.Value + 1, tvFrames.SelectedNode.Text + " - Copy"); //TODO: Localize

            DelayUpdate();
            GC.Collect();
        }

        private void con_MoveUpwards_Click(object sender, EventArgs e)
        {
            //A frame need to be selected.
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;
            //This action can't be applied to the top frame.
            if (tvFrames.SelectedNode.Index == 0) return;

            //Copy, RemoveAt, Insert;
            var aux = new Bitmap(_listFramesPrivate[trackBar.Value]);
            var auxDelay = _listDelayPrivate[trackBar.Value];
            var auxNode = tvFrames.Nodes[0].Nodes[trackBar.Value];

            _listFramesPrivate.RemoveAt(trackBar.Value);
            _listDelayPrivate.RemoveAt(trackBar.Value);
            tvFrames.Nodes[0].Nodes.RemoveAt(trackBar.Value);

            _listFramesPrivate.Insert(trackBar.Value - 1, aux);
            _listDelayPrivate.Insert(trackBar.Value - 1, auxDelay);
            tvFrames.Nodes[0].Nodes.Insert(trackBar.Value - 1, auxNode);

            tvFrames.SelectedNode = tvFrames.Nodes[0].Nodes[trackBar.Value - 1];
            SelectFrame(trackBar.Value - 1);

            DelayUpdate();
            GC.Collect();
        }

        private void con_MoveDownwards_Click(object sender, EventArgs e)
        {
            //A frame need to be selected.
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;
            //This action can't be applied to the bottom frame.
            if (tvFrames.SelectedNode.Index == tvFrames.Nodes[0].Nodes.Count - 1) return;

            //Copy, RemoveAt, Insert;
            var aux = new Bitmap(_listFramesPrivate[trackBar.Value]);
            var auxDelay = _listDelayPrivate[trackBar.Value];
            var auxNode = tvFrames.Nodes[0].Nodes[trackBar.Value];

            _listFramesPrivate.RemoveAt(trackBar.Value);
            _listDelayPrivate.RemoveAt(trackBar.Value);
            tvFrames.Nodes[0].Nodes.RemoveAt(trackBar.Value);

            _listFramesPrivate.Insert(trackBar.Value + 1, aux);
            _listDelayPrivate.Insert(trackBar.Value + 1, auxDelay);
            tvFrames.Nodes[0].Nodes.Insert(trackBar.Value + 1, auxNode);

            tvFrames.SelectedNode = tvFrames.Nodes[0].Nodes[trackBar.Value + 1];
            SelectFrame(trackBar.Value + 1);

            DelayUpdate();
            GC.Collect();
        }

        private void con_RenameFrame_Click(object sender, EventArgs e)
        {
            //A frame need to be selected.
            if (tvFrames.SelectedNode == null) return;
            //If != 0 means that is the Main Node.
            if (tvFrames.SelectedNode.GetNodeCount(false) != 0) return;

            tvFrames.SelectedNode.BeginEdit();
        }

        #endregion

        #endregion

        #region Drag And Drop Events

        private void pictureBitmap_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void pictureBitmap_DragDrop(object sender, DragEventArgs e)
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
