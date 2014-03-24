using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using ScreenToGif.Capture;
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
        AnimatedGifEncoder _encoder = new AnimatedGifEncoder();
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
        /// The output path of the recording.
        /// </summary>
        private string _outputpath;
        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        private int _stage = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding
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

        #endregion

        /// <summary>
        /// The contructor of the Legacy Form.
        /// </summary>
        public Legacy()
        {
            InitializeComponent();

            #region Load Save Data

            //Gets and sets the fps
            numMaxFps.Value = Settings.Default.STmaxFps;

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

            //Gets the window chrome offset
            _offsetX = (this.Size.Width - panelTransparent.Width) / 2;
            _offsetY = (this.Size.Height - panelTransparent.Height - _offsetX - flowPanel.Height - 1);

            //Windows 8 Bugfix.
            var osInfo = System.Environment.OSVersion;

            if (osInfo.Version.Major == 6 && osInfo.Version.Minor == 3)
            {
                this.ResizeBegin += Legacy_ResizeBegin;
                this.ResizeEnd += Legacy_ResizeEnd;
            }

            //Starts the global keyboard hook.
            #region Global Hook
            _actHook = new UserActivityHook();
            _actHook.KeyDown += KeyHookTarget;
            _actHook.Start(false, true); //false for the mouse, true for the keyboard.
            #endregion

        }

        #region Override

        /// <summary>
        /// Process the Key events, such as pressing. This handles the keyboard shortcuts.
        /// </summary>
        /// <param name="msg">A Message, passed by reference, that represents the window message to process. </param>
        /// <param name="keyData">One of the Keys values that represents the key to process. </param>
        /// <returns><code>true</code> if the character was processed by the control; otherwise, <code>false</code>.</returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //If not in the editor page.
            if (_stage != (int)Stage.Editing)
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
                    con_DeleteAfter_Click(null, null);
                    return true;
                case Keys.Alt | Keys.Left: //Delete everything before this
                    con_DeleteBefore_Click(null, null);
                    return true;
                case Keys.Control | Keys.E: //Export Frame
                    con_exportFrame_Click(null, null);
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

        #region Main Form Resize/Closing

        /// <summary>
        /// Used to update the Size values in the bottom of the window
        /// </summary>
        private void Principal_Resize(object sender, EventArgs e) //To show the exactly size of the form.
        {
            //this.Invalidate(true);
            //panelTransparent.Invalidate();

            if (!_screenSizeEdit) //if not typing in the textBoxes, I use this to prevent flickering of the size. It only updates after typing.
            {
                tbHeight.Text = panelTransparent.Height.ToString();
                tbWidth.Text = panelTransparent.Width.ToString();
            }
        }

        /// <summary>
        /// Before close, all settings must be saved and the timer must be disposed.
        /// </summary>
        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.STmaxFps = Convert.ToInt32(numMaxFps.Value);
            Properties.Settings.Default.STsize = new Size(this.Size.Width, this.Size.Height);

            Properties.Settings.Default.Save();

            _actHook.Stop(); //Stop the keyboard watcher.

            if (_stage != (int)Stage.Stoped)
            {
                timerCapture.Stop();
                timerCapture.Dispose();

                timerCapWithCursor.Stop();
                timerCapWithCursor.Dispose();
            }
        }

        #endregion

        #region Bottom buttons

        readonly Control info = new Information(); //Information page
        readonly Control appSettings = new AppSettings(true); //App Settings page, true = legacy, false = modern
        readonly Control gifSettings = new GifSettings(); //Gif Settings page

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            Stop();
        }

        private void btnPauseRecord_Click(object sender, EventArgs e)
        {
            this.TransparencyKey = Color.LimeGreen;
            panelTransparent.BackColor = Color.LimeGreen;
            panelTransparent.Controls.Clear(); //Removes all pages from the top

            this.MaximizeBox = false;
            this.MinimizeBox = false;

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
                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(appSettings);
                panelTransparent.Visible = true;
                appSettings.Dock = DockStyle.Fill;

                _isPageAppOpen = true;
                _isPageGifOpen = false;
                _isPageInfoOpen = false;

                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);
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
                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(gifSettings);
                panelTransparent.Visible = true;
                gifSettings.Dock = DockStyle.Fill;

                _isPageInfoOpen = false;
                _isPageAppOpen = false;
                _isPageGifOpen = true;

                //Need this line, because there is a pictureBox with color, so if the user select the same color 
                //as this.TransparencyKey, the color won't be showed. This needs to be re-set after closing the gif config page.
                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);
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
                panelTransparent.Visible = false;
                ctrlParent.Controls.Clear(); //Removes all pages
                ctrlParent.Controls.Add(info);
                panelTransparent.Visible = true;
                info.Dock = DockStyle.Fill;

                _isPageAppOpen = false;
                _isPageGifOpen = false;
                _isPageInfoOpen = true;
                GC.Collect();

                panelTransparent.BackColor = Color.FromArgb(239, 239, 242);
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
            if (e.KeyCode == Properties.Settings.Default.STstartPauseKey)
            {
                btnPauseRecord_Click(null, null);
            }
            else if (e.KeyCode == Properties.Settings.Default.STstopKey)
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
            if (_stage == (int)Stage.Stoped) //if stoped, starts recording
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
                    this.Text = "Screen To Gif (2 " + Resources.TitleSecondsToGo;
                    btnRecordPause.Enabled = false;

                    _stage = (int)Stage.PreStarting;
                    numMaxFps.Enabled = false;
                    _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                    timerPreStart.Start();
                }
                else
                {
                    this.Text = Resources.TitleRecording;
                    _stage = (int)Stage.Recording;
                    btnRecordPause.Enabled = true;

                    if (Settings.Default.STshowCursor) //if show cursor
                    {
                        timerCapWithCursor.Start();
                    }
                    else
                    {
                        timerCapture.Start();
                    }
                }

                #endregion
            }
            else if (_stage == (int)Stage.Recording) //if recording, pauses
            {
                #region To Pause

                this.Text = Resources.TitlePaused;
                btnRecordPause.Text = Resources.btnRecordPause_Continue;
                btnRecordPause.Image = Properties.Resources.Record;
                _stage = (int)Stage.Paused;

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
            else if (_stage == (int)Stage.Paused) //if paused, starts recording again
            {
                #region To Record Again

                this.Text = Resources.TitleRecording;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                _stage = (int)Stage.Recording;

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
                _actHook.Stop(); //Stops the hook.
                _actHook.KeyDown -= KeyHookTarget; //Removes the event.

                timerCapture.Stop();
                timerCapWithCursor.Stop();

                if (_stage != (int)Stage.Stoped && _stage != (int)Stage.PreStarting) //if not already stoped or pre starting, stops
                {
                    #region To Stop and Save

                    if (Settings.Default.STshowCursor) //If show cursor is true, merge all bitmaps.
                    {
                        #region Merge Cursor and Bitmap

                        this.Cursor = Cursors.WaitCursor;
                        Graphics graph;
                        int numImage = 0;

                        Application.DoEvents();

                        foreach (var bitmap in _listBitmap)
                        {
                            graph = Graphics.FromImage(bitmap);
                            var rect = new Rectangle(_listCursor[numImage].Position.X, _listCursor[numImage].Position.Y, _listCursor[numImage].Icon.Width, _listCursor[numImage].Icon.Height);

                            graph.DrawIcon(_listCursor[numImage].Icon, rect);
                            graph.Flush();
                            numImage++;
                        }
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
                        _stage = (int)Stage.Editing;
                        this.MaximizeBox = true;
                        this.MinimizeBox = true;
                        //this.FormBorderStyle = FormBorderStyle.Sizable;
                        EditFrames();
                        flowPanel.Enabled = false;
                    }
                    else
                    {
                        _lastSize = this.Size; //Not sure why this is here
                        Save();
                    }

                    #endregion
                }
                else if (_stage == (int)Stage.PreStarting) // if pre starting, stops
                {
                    #region To Stop

                    timerPreStart.Stop();
                    _stage = (int)Stage.Stoped;

                    //Enables the controls that are disabled while recording;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Enabled = true;
                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;
                    //this.FormBorderStyle = FormBorderStyle.Sizable;

                    this.MaximizeBox = true;
                    this.MinimizeBox = true;

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Properties.Resources.Record;
                    this.Text = Resources.TitleStoped;

                    //Re-starts the keyboard hook.
                    _actHook.KeyDown += KeyHookTarget;
                    _actHook.Start(false, true);

                    #endregion
                }
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
                #region If Not Save Directly to the desktop

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                this.Cursor = Cursors.Default;

                if (sfd.ShowDialog() == DialogResult.OK) //if ok
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

                    _actHook.KeyDown += KeyHookTarget;
                    _actHook.Start(false, true);

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

                    //Restart the keyhook.
                    _actHook.KeyDown += KeyHookTarget;
                    _actHook.Start(false, true);

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
            _stage = (int)Stage.Stoped;
            this.MinimumSize = new Size(100, 100);
            this.Size = _lastSize;

            numMaxFps.Enabled = true;
            tbHeight.Enabled = true;
            tbWidth.Enabled = true;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            btnRecordPause.Text = Resources.btnRecordPause_Record;
            btnRecordPause.Image = Properties.Resources.Record;
            this.Text = Resources.TitleStoped;
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

            if (Settings.Default.STencodingCustom) // if NGif encoding
            {
                #region Ngif encoding

                int numImage = 0;

                #region Paint Unchanged Pixels

                if (Settings.Default.STpaintTransparent)
                {
                    this.Invoke((Action)(delegate { processing.SetPreEncoding("Analizing Unchanged Pixels"); }));

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
            else //if paint.NET encoding
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
                    processing.SetFinishedState(_outputpath, "Done!"); //LOCALIZE
                });

                //After the user hits "Close", the processing_Disposed is called. To set to the right stage.
            }
            else
            {
                this.Text = Resources.Title_EncodingDone;
                _stage = (int)Stage.Stoped;

                this.Invalidate();

                FinishState();

                _actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
                _actHook.Start(false, true); //start again the keyboard hook watcher
            }

            #endregion

            #region Memory Clearing

            _listBitmap.Clear();
            _listFramesPrivate.Clear();
            _listFramesUndo.Clear();

            _listDelay.Clear();
            _listDelayPrivate.Clear();
            _listDelayUndo.Clear();

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
            _stage = (int)Stage.Stoped;

            this.Invalidate();

            FinishState();

            _actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
            _actHook.Start(false, true); //start again the keyboard hook watcher
        }

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
                this.Text = Resources.TitleRecording;
                timerPreStart.Stop();
                _stage = (int)Stage.Recording;
                btnRecordPause.Enabled = true;

                if (Settings.Default.STshowCursor)
                {
                    timerCapWithCursor.Start(); //Record with the cursor
                }
                else
                {
                    timerCapture.Start(); //Frame recording
                }
            }
        } //PRE START SEQUENCE

        /// <summary>
        /// Takes a screenshot of desired area and add to the list.
        /// </summary>
        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + _offsetX, this.Location.Y + _offsetY);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.Clone());
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

            //saves to list the actual icon and position of the cursor
            _listCursor.Add(_cursorInfo);
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + _offsetX, this.Location.Y + _offsetY);
            //Take a screenshot of the area.
            _gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            _listBitmap.Add((Bitmap)_bt.Clone());
        }

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
            //Fix this, it should only resize if the form is smaller than the image.
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

            panelEdit.Visible = false;
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

            pictureBitmap.Image = null;
            panelEdit.Visible = false;
            Save();

            GC.Collect();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_ValueChanged(object sender, EventArgs e)
        {
            //StopPreview();
            pictureBitmap.Image = (Bitmap)_listFramesPrivate[trackBar.Value];

            #region Delay Display

            _delay = _listDelayPrivate[trackBar.Value];
            lblDelay.Text = _delay + " ms";

            #endregion
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            StopPreview();
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (_listFramesPrivate.Count > 1) //If more than 1 image in the list
            {
                //Clears and set the undo of the frames.
                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                //Clears and set the undo of the delays.
                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                _listFramesPrivate.RemoveAt(trackBar.Value); //delete the selected frame
                _listDelayPrivate.RemoveAt(trackBar.Value); //delete the selected delay

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                //I should make this as a function.
                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            StopPreview();

            //Resets the list to a previously state
            _listFramesPrivate.Clear();
            _listFramesPrivate = new List<Bitmap>(_listFramesUndo);

            //Resets the list to a previously state
            _listDelayPrivate.Clear();
            _listDelayPrivate = new List<int>(_listDelayUndo);

            trackBar.Maximum = _listFramesPrivate.Count - 1;
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

            btnUndo.Enabled = false;

            ResizeFormToImage();

            #region Delay Display

            _delay = _listDelayPrivate[trackBar.Value];
            lblDelay.Text = _delay + " ms";

            #endregion
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

            #region Delay Display

            _delay = _listDelayPrivate[trackBar.Value];
            lblDelay.Text = _delay + " ms";

            #endregion
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
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            //TODO
        }

        private void con_addCaption_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            if (!con_tbCaption.Text.Equals(string.Empty))
            {
                GraphicsPath graphPath;
                Graphics imgGraph;

                Bitmap image = new Bitmap(_listFramesPrivate[trackBar.Value]);
                imgGraph = Graphics.FromImage(image);
                graphPath = new GraphicsPath();

                float witdh = imgGraph.MeasureString(con_tbCaption.Text,
                        new Font(new FontFamily("Segoe UI"), (image.Height * 0.1F), FontStyle.Bold)).Width;

                int fSt = (int)FontStyle.Bold;

                //500 - 500 (1000px image / 2)
                //25 - 25 (50px text /2)
                //475 - 50 - 475 (50px text inserted in the middle)

                Point xy = new Point((int)((image.Width / 2) - (witdh / 2)), (int)(image.Height - (image.Height * 0.15F))); //calculate the height too
                FontFamily fF = new FontFamily("Segoe UI");
                StringFormat sFr = StringFormat.GenericDefault;

                graphPath.AddString(con_tbCaption.Text, fF, fSt, (image.Height * 0.1F), xy, sFr);  // Add the string to the path, 10% of the size of the image

                imgGraph.TextRenderingHint = TextRenderingHint.AntiAlias;

                //imgGraph.FillPath(Brushes.Gray, graphPath);

                imgGraph.FillPath(Brushes.White, graphPath);  // Draw the path to the surface
                imgGraph.DrawPath(new Pen(Color.Black, 1.8F), graphPath);  // Draw the path to the surface

                _listFramesPrivate.RemoveAt(trackBar.Value);
                _listFramesPrivate.Insert(trackBar.Value, image);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
                //TODO
            }
        }

        private void con_DeleteAfter_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            if (_listFramesPrivate.Count > 1)
            {
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

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
            }
        }

        private void con_DeleteBefore_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            if (_listFramesPrivate.Count > 1)
            {
                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    _listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere
                    _listDelayPrivate.RemoveAt(i);
                }

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
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
            if (con_showGrid.Checked)
            {
                panelEdit.BackgroundImage = Properties.Resources.grid;
            }
            else
            {
                panelEdit.BackgroundImage = null;
            }
        }

        private void con_resizeAllFrames_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            Bitmap bitmapResize = _listFramesPrivate[trackBar.Value];

            var resize = new Resize(bitmapResize);

            if (resize.ShowDialog(this) == DialogResult.OK)
            {
                Size resized = resize.GetSize();

                _listFramesPrivate = ImageUtil.ResizeBitmap(_listFramesPrivate, resized.Width, resized.Height);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            resize.Dispose();
        }

        private void con_cropAll_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            Crop crop = new Crop(_listFramesPrivate[trackBar.Value]);
            if (crop.ShowDialog(this) == DialogResult.OK)
            {
                _listFramesPrivate = ImageUtil.Crop(_listFramesPrivate, crop.Rectangle);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];

                ResizeFormToImage();
            }

            crop.Dispose();
        }

        private void con_deleteThisFrame_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (_listFramesPrivate.Count > 1)
            {
                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                _listFramesPrivate.RemoveAt(trackBar.Value);
                _listDelayPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
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
            if (openImageDialog.ShowDialog() == DialogResult.OK)
            {

                btnUndo.Enabled = true;
                btnReset.Enabled = true;

                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                Image openBitmap = Bitmap.FromFile(openImageDialog.FileName);

                Bitmap bitmapResized = ImageUtil.ResizeBitmap((Bitmap)openBitmap, _listFramesPrivate[0].Size.Width,
                    _listFramesPrivate[0].Size.Height);

                _listFramesPrivate.Insert(trackBar.Value, bitmapResized);

                _listDelayPrivate.Insert(trackBar.Value, _delay); //Sets the last delay used.

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            }
        }

        /// <summary>
        /// ListFramesInverted
        /// </summary>
        private void con_revertOrder_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (_listFramesPrivate.Count > 1)
            {
                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                //_listFramesPrivate = ImageUtil.Revert(_listFramesPrivate); //change this.

                _listFramesPrivate.Reverse();
                _listDelayPrivate.Reverse();


                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
            }
        }

        /// <summary>
        /// Make a Yoyo with the frames (listFrames + listFramesInverted)
        /// </summary>
        private void con_yoyo_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (_listFramesPrivate.Count > 1)
            {
                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                _listFramesPrivate = ImageUtil.Yoyo(_listFramesPrivate);

                //Test this
                var listFramesAux = new List<int>(_listDelayPrivate);
                listFramesAux.Reverse();
                _listDelayPrivate.AddRange(listFramesAux);
                listFramesAux.Clear();

                trackBar.Maximum = _listFramesPrivate.Count - 1;
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
            }
        }

        /// <summary>
        /// Adds a border in all images. The border is painted in the image, the image don't increase in size.
        /// </summary>
        private void con_Border_Click(object sender, EventArgs e)
        {
            //Maybe in the future, user could choose a color too.

            ValuePicker valuePicker = new ValuePicker(10, 1, "Choose the tickness of the border"); //LOCALIZE

            if (valuePicker.ShowDialog(this) == DialogResult.OK)
            {
                btnUndo.Enabled = true;
                btnReset.Enabled = true;

                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                _listFramesPrivate = ImageUtil.Border(_listFramesPrivate, valuePicker.Value);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            }


        }

        private void con_sloMotion_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (_listFramesPrivate.Count > 1)
            {
                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                for (int i = 0; i < _listDelayPrivate.Count; i++)
                {
                    //Change this, later, let user pick desired velocity.
                    _listDelayPrivate[i] = _listDelayPrivate[i] * 2; //currently, it doubles the  delay.
                }

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion

            }
        }

        private void con_titleImage_Click(object sender, EventArgs e)
        {
            Size titleFrameSize = _listFramesPrivate[trackBar.Value].Size;
            Bitmap titleBitmap = new Bitmap(titleFrameSize.Width, titleFrameSize.Height);
            var title = new TitleFrameSettings(titleBitmap);

            if (title.ShowDialog() == DialogResult.OK)
            {
                #region Undo/Reset Settings

                btnUndo.Enabled = true;
                btnReset.Enabled = true;

                _listFramesUndo.Clear();
                _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

                _listDelayUndo.Clear();
                _listDelayUndo = new List<int>(_listDelayPrivate);

                #endregion

                using (Graphics grp = Graphics.FromImage(titleBitmap))
                {
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
                    this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                    #region Delay Display

                    _delay = _listDelayPrivate[trackBar.Value];
                    lblDelay.Text = _delay + " ms";

                    #endregion
                }
            }
        }

        #region Filters

        #region To One

        /// <summary>
        /// Apply selected image to a grayscale filter
        /// </summary>
        private void GrayscaleOne_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            _listFramesPrivate[trackBar.Value] = ImageUtil.Grayscale((Bitmap)pictureBitmap.Image);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
        }

        /// <summary>
        /// Apply selected image to a pixelated filter
        /// </summary>
        private void PixelateOne_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            //User first need to choose the intensity of the pixelate
            ValuePicker valuePicker = new ValuePicker(100, 2, Resources.Msg_PixelSize);

            if (valuePicker.ShowDialog() == DialogResult.OK) //Should I keep everything inside here? (the undo and reset stuff)
            {
                _listFramesPrivate[trackBar.Value] = ImageUtil.Pixelate((Bitmap)pictureBitmap.Image, new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value);
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            }

            valuePicker.Dispose();
        }

        /// <summary>
        /// Apply selected image to a blur filter
        /// </summary>
        private void BlurOne_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            ValuePicker valuePicker = new ValuePicker(5, 1, Resources.Msg_BlurIntense);

            if (valuePicker.ShowDialog() == DialogResult.OK)
            {

                _listFramesPrivate[trackBar.Value] = ImageUtil.Blur((Bitmap)pictureBitmap.Image,
                    new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            }
            valuePicker.Dispose();
        }

        /// <summary>
        /// Convert selected image to negative filter
        /// </summary>
        private void NegativeOne_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            pictureBitmap.Image = _listFramesPrivate[trackBar.Value] = ImageUtil.Negative(pictureBitmap.Image);
        }

        /// <summary>
        /// Convert selected image to transparency filter
        /// </summary>
        private void TransparencyOne_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            pictureBitmap.Image = _listFramesPrivate[trackBar.Value] = ImageUtil.Transparency(pictureBitmap.Image);
        }

        /// <summary>
        /// Convert selected image to SepiaTone filter
        /// </summary>
        private void sepiaToneOne_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            pictureBitmap.Image = _listFramesPrivate[trackBar.Value] = ImageUtil.SepiaTone(pictureBitmap.Image);
        }

        #endregion

        #region To All

        /// <summary>
        /// Convert all images to grayscale filter
        /// </summary>
        private void GrayscaleAll_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            this.Cursor = Cursors.WaitCursor;
            _listFramesPrivate = ImageUtil.Grayscale(_listFramesPrivate);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Cursor = Cursors.Default;
        }

        #region Pixelate All - With Async

        private delegate List<Bitmap> PixelateDelegate(List<Bitmap> list, Rectangle area, int pixelSize);

        private PixelateDelegate pixDel;

        private void CallBackPixelate(IAsyncResult r)
        {
            _listFramesPrivate = pixDel.EndInvoke(r);

            //Cross thread call;
            this.Invoke((Action)delegate
            {
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                panelEdit.Enabled = true;
                this.Cursor = Cursors.Default;
            });

        }

        /// <summary>
        /// Apply all images to a Pixelated filter
        /// </summary>
        private void PixelateAll_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            ValuePicker valuePicker = new ValuePicker(100, 2, Resources.Msg_PixelSize);

            if (valuePicker.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;
                panelEdit.Enabled = false;

                pixDel = ImageUtil.Pixelate;
                pixDel.BeginInvoke(_listFramesPrivate,
                    new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value, CallBackPixelate, null);

                #region Old Code

                //_listFramesPrivate = ImageUtil.Pixelate(_listFramesPrivate,
                //    new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value);
                //pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                //this.Cursor = Cursors.Default;

                #endregion
            }

            valuePicker.Dispose();
        }

        #endregion

        #region Blur All - With Async

        private delegate List<Bitmap> BlurDelegate(List<Bitmap> list, Rectangle area, int pixelSize);

        private BlurDelegate blurDel;

        private void CallBackBlur(IAsyncResult r)
        {
            _listFramesPrivate = blurDel.EndInvoke(r);

            //Cross thread call;
            this.Invoke((Action)delegate
            {
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                panelEdit.Enabled = true;
                this.Cursor = Cursors.Default;
            });

        }

        /// <summary>
        /// Apply all images to a blur filter
        /// </summary>
        private void BlurAll_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            ValuePicker valuePicker = new ValuePicker(5, 1, Resources.Msg_BlurIntense);

            if (valuePicker.ShowDialog() == DialogResult.OK)
            {
                this.Cursor = Cursors.WaitCursor;

                #region Old Code
                //This thing down here didn't make any difference. Still hangs.
                // http://www.codeproject.com/Articles/45787/Easy-asynchronous-operations-with-AsyncVar Oh, I see now, this thing it's good only with multiple actions.
                //var asyncVar =
                //    new AsyncVar<List<Bitmap>>(
                //        () =>
                //            ImageUtil.Blur(_listFramesPrivate,
                //                new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height),
                //                valuePicker.Value));
                //_listFramesPrivate = new List<Bitmap>(asyncVar.Value);
                //pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                //this.Cursor = Cursors.Default;
                #endregion

                blurDel = ImageUtil.Blur;

                blurDel.BeginInvoke(_listFramesPrivate, new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value, CallBackBlur, null);
            }

            valuePicker.Dispose();
        }

        #endregion

        /// <summary>
        /// Convert all images to negative filter
        /// </summary>
        private void NegativeAll_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            this.Cursor = Cursors.WaitCursor;
            _listFramesPrivate = ImageUtil.Negative(_listFramesPrivate);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert all images to transparency filter
        /// </summary>
        private void TransparencyAll_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            this.Cursor = Cursors.WaitCursor;
            _listFramesPrivate = ImageUtil.Transparency(_listFramesPrivate);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Convert all images to SepiaTone filter
        /// </summary>
        private void sepiaToneAll_Click(object sender, EventArgs e)
        {
            #region Reset and Undo Properties

            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            _listFramesUndo.Clear();
            _listFramesUndo = new List<Bitmap>(_listFramesPrivate);

            _listDelayUndo.Clear();
            _listDelayUndo = new List<int>(_listDelayPrivate);

            #endregion

            this.Cursor = Cursors.WaitCursor;
            _listFramesPrivate = ImageUtil.SepiaTone(_listFramesPrivate);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Cursor = Cursors.Default;
        }

        #endregion

        #endregion

        #endregion

        #endregion

        #region Play Preview

        System.Windows.Forms.Timer timerPlayPreview = new System.Windows.Forms.Timer();
        private int _actualFrame = 0;

        private void pictureBitmap_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Left))
            {
                PlayPreview();
            }
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

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
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
        /// Stops the preview if the user scrolls the trackbar
        /// </summary>
        private void trackBar_Scroll(object sender, EventArgs e)
        {
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            StopPreview();
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
                contextSmall.Close(ToolStripDropDownCloseReason.Keyboard);
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

        #region Windows 8 bug fix

        private void Legacy_ResizeBegin(object sender, EventArgs e)
        {
            //This fix the bug of Windows 8
            panelTransparent.BackColor = Color.WhiteSmoke;
            this.TransparencyKey = Color.WhiteSmoke;
        }

        private void Legacy_ResizeEnd(object sender, EventArgs e)
        {
            panelTransparent.BackColor = Color.LimeGreen;
            this.TransparencyKey = Color.LimeGreen;
        }

        #endregion
    }
}
