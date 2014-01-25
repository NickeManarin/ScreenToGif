using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        public List<Bitmap> _listBitmap;
        /// <summary>
        /// The list of information about the cursor.
        /// </summary>
        public List<CursorInfo> listCursor = new List<CursorInfo>(); //List that stores the icon
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
        private Thread _workerThread;

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
            tbHeight.Text = (this.Height - 71).ToString();
            tbWidth.Text = (this.Width - 16).ToString();

            //Performance and flickering tweaks
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);

            //Starts the global keyboard hook.
            #region Global Hook
            _actHook = new UserActivityHook();
            _actHook.KeyDown += KeyHookTarget;
            _actHook.Start(false, true); //false for the mouse, true for the keyboard.
            #endregion
        }

        #region Main Form Resize/Closing

        /// <summary>
        /// Used to update the Size values in the bottom of the window
        /// </summary>
        private void Principal_Resize(object sender, EventArgs e) //To show the exactly size of the form.
        {
            this.Invalidate(true);
            panelTransparent.Invalidate();

            if (!_screenSizeEdit) //if not typing in the textBoxes, I use this to prevent flickering of the size. It only updates after typing.
            {
                tbHeight.Text = (this.Height - 71).ToString(); //71 px is the size of Top + Bottom borders
                tbWidth.Text = (this.Width - 16).ToString(); //16 px is the size of Left + Right borders
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

            if (_stage != 0)
            {
                timerCapture.Stop();
                timerCapture.Dispose();
            }

            this.Dispose(true);
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
        } //STOP

        private void btnPauseRecord_Click(object sender, EventArgs e)
        {
            panelTransparent.Controls.Clear(); // removes all pages from the top
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            RecordPause(); //and start the pre-start tick
        } //RECORD-PAUSE

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
        } //CONFIG

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
        } //INFO

        #endregion

        #region Functions

        /// <summary>
        /// KeyHook event method. This fires when the user press a key.
        /// </summary>
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
                listCursor = new List<CursorInfo>(); //List that contains all the icon information

                _bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                _gr = Graphics.FromImage(_bt);

                this.Text = "Screen To Gif (2 " + Resources.TitleSecondsToGo;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                btnRecordPause.Enabled = false;
                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                //this.FormBorderStyle = FormBorderStyle.FixedSingle;
                _stage = (int)Stage.PreStarting;
                numMaxFps.Enabled = false;
                _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                timerPreStart.Start();
                this.TopMost = true;

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

                        foreach (var bitmap in _listBitmap)
                        {
                            Application.DoEvents();
                            graph = Graphics.FromImage(bitmap);
                            var rect = new Rectangle(listCursor[numImage].Position.X, listCursor[numImage].Position.Y, listCursor[numImage].Icon.Width, listCursor[numImage].Icon.Height);

                            graph.DrawIcon(listCursor[numImage].Icon, rect);
                            graph.Flush();
                            numImage++;
                        }
                        this.Cursor = Cursors.Arrow;

                        #endregion
                    }

                    if (Settings.Default.STallowEdit) //If the user wants to edit the frames.
                    {
                        _lastSize = this.Size; //To return back to the last form size after the editor
                        _stage = (int)Stage.Editing;
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
                MessageBox.Show(ex.Message, "Error");
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
            if (!Settings.Default.STsaveLocation) // to choose the location to save the gif
            {
                #region If Not Save Directly to the desktop

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                this.Cursor = Cursors.Arrow;
                if (sfd.ShowDialog() == DialogResult.OK) //if ok
                {
                    _outputpath = sfd.FileName;

                    _workerThread = new Thread(DoWork);
                    _workerThread.IsBackground = true;
                    _workerThread.Start();
                }
                else //if user son't want to save the gif
                {
                    flowPanel.Enabled = true;
                    this.MinimumSize = new Size(250, 100);
                    _stage = (int)Stage.Stoped; //Stoped
                    numMaxFps.Enabled = true;
                    tbWidth.Enabled = true;
                    tbHeight.Enabled = true;

                    this.TopMost = false;
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
                string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

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
                                _outputpath = "No filename for you";
                            }
                        }
                        numOfFile++;
                    }
                }
                #endregion

                _workerThread = new Thread(DoWork);
                _workerThread.IsBackground = true;
                _workerThread.Name = "Encoding";
                _workerThread.Start();
            }

            this.Cursor = Cursors.Arrow;
            _stage = (int)Stage.Stoped;
            this.MinimumSize = new Size(100, 100);
            numMaxFps.Enabled = true;
            tbHeight.Enabled = true;
            tbWidth.Enabled = true;
            this.TopMost = false;
            this.Text = Resources.TitleStoped;
        }

        /// <summary>
        /// Thread method that encodes the list of frames.
        /// </summary>
        private void DoWork()
        {
            if (Settings.Default.STencodingCustom) // if NGif encoding
            {
                #region Ngif encoding

                int numImage = 0;
                int countList = _listBitmap.Count;

                using (_encoder = new AnimatedGifEncoder())
                {
                    _encoder.Start(_outputpath);
                    _encoder.SetQuality(Settings.Default.STquality);

                    _encoder.SetRepeat(Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : -1); // 0 = Always, -1 once

                    _encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));

                    foreach (var image in _listBitmap)
                    {
                        numImage++;
                        try
                        {
                            this.Invoke((Action)delegate //Needed because it's a cross thread call.
                            {
                                this.Text = Resources.Title_Thread_ProcessingFrame + numImage + Resources.Title_Thread_out_of + countList + ")";
                            });
                        }
                        catch (Exception)
                        {
                        }
                        _encoder.AddFrame(image);
                    }
                }

                #endregion
            }
            else //if paint.NET encoding
            {
                #region paint.NET encoding

                var imageArray = _listBitmap.ToArray();

                var delay = 1000 / Convert.ToInt32(numMaxFps.Value);
                var repeat = (Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : -1); // 0 = Always, -1 once
                int countList = _listBitmap.Count;

                using (var stream = new MemoryStream())
                {
                    using (var encoderNet = new GifEncoder(stream, null, null, repeat))
                    {
                        for (int i = 0; i < _listBitmap.Count; i++)
                        {
                            encoderNet.AddFrame((_listBitmap[i]).CopyImage(), 0, 0,
                                TimeSpan.FromMilliseconds(delay));

                            this.Invoke((Action)delegate //Needed because it's a cross thread call.
                            {
                                this.Text = Resources.Title_Thread_ProcessingFrame + i + Resources.Title_Thread_out_of +
                                            countList + ")";
                            });
                        }
                    }

                    stream.Position = 0;

                    using (
                        var fileStream = new FileStream(_outputpath, FileMode.Create, FileAccess.Write, FileShare.None,
                            Constants.BufferSize, false))
                    {
                        stream.WriteTo(fileStream);
                    }
                }

                #endregion
            }

            #region Memory Clearing

            listFramesPrivate.Clear();
            listFramesUndo.Clear();
            listFramesUndoAll.Clear();

            listFramesPrivate = null;
            listFramesUndo = null;
            listFramesUndoAll = null;
            _encoder = null;

            GC.Collect(); //call the garbage colector to empty the memory

            #endregion

            #region Finish

            try
            {
                this.Invoke((Action)delegate //must use invoke because it's a cross thread call
                {
                    this.Text = Resources.Title_EncodingDone;
                    _stage = (int)Stage.Stoped;

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Properties.Resources.Record;
                    flowPanel.Enabled = true;
                    this.TopMost = false;

                    numMaxFps.Enabled = true;
                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

                    this.MaximizeBox = true;
                    this.MinimizeBox = true;

                    _actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
                    _actHook.Start(false, true); //start again the keyboard hook watcher
                });
            }
            catch { }

            #endregion
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
            Point lefttop = new Point(this.Location.X + 8, this.Location.Y + 31);
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
            listCursor.Add(_cursorInfo);
            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + 8, this.Location.Y + 31);
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
            _screenSizeEdit = true; //So the Resize event won't trigger
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (_sizeScreen.X > widthTb)
            {
                this.Size = new Size(widthTb + 16, heightTb + 71);
            }
            else
            {
                this.Size = new Size(_sizeScreen.X - 1, heightTb + 71);
            }
            _screenSizeEdit = false;
        }

        /// <summary>
        /// User press Enter, updates the form size.
        /// </summary>
        private void tbSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                _screenSizeEdit = true; //So the Resize event won't trigger
                int heightTb = Convert.ToInt32(tbHeight.Text);
                int widthTb = Convert.ToInt32(tbWidth.Text);

                if (_sizeScreen.X > widthTb)
                {
                    this.Size = new Size(widthTb + 16, heightTb + 71);
                }
                else
                {
                    this.Size = new Size(_sizeScreen.X - 1, heightTb + 71);
                }
                _screenSizeEdit = false;
            }
        }

        #endregion

        #region Frame Edit Stuff

        private List<Bitmap> listFramesPrivate; //List of frames that will be edited.
        private List<Bitmap> listFramesUndoAll; //List of frames that holds the last alteration
        private List<Bitmap> listFramesUndo; //List of frames used to reset, I should use the listBitmap, because it's the same

        /// <summary>
        /// Constructor of the Frame Edit Page.
        /// </summary>
        private void EditFrames()
        {
            //Copies the listBitmap to all the lists
            listFramesPrivate = new List<Bitmap>(_listBitmap);
            listFramesUndoAll = new List<Bitmap>(_listBitmap);
            listFramesUndo = new List<Bitmap>(_listBitmap);

            Application.DoEvents();

            panelEdit.Visible = true;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            //trackBar.Value = 0; //don't need this here, it's already the default

            this.MinimumSize = new Size(100, 100);
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            ResizeFormToImage(); //Resizes the form to hold the image

            pictureBitmap.Image = listFramesPrivate.First(); //Puts the first image of the list in the pictureBox

            #region Preview Config.

            timerPlayPreview.Tick += timerPlayPreview_Tick;
            timerPlayPreview.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

            #endregion
        }

        /// <summary>
        /// Accepts all the alterations and hides this page.
        /// </summary>
        private void btnDone_Click(object sender, EventArgs e)
        {
            StopPreview();
            _listBitmap = new List<Bitmap>(listFramesPrivate);

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
            panelEdit.Visible = false;
            Save();

            GC.Collect();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_Scroll(object sender, EventArgs e)
        {
            StopPreview();
            pictureBitmap.Image = (Bitmap)listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            StopPreview();
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            if (listFramesPrivate.Count > 1) //If more than 1 image in the list
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                listFramesPrivate.RemoveAt(trackBar.Value); //delete the select frame

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            StopPreview();
            listFramesPrivate.Clear();
            listFramesPrivate = new List<Bitmap>(listFramesUndo);

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            btnUndo.Enabled = false;

            ResizeFormToImage();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            StopPreview();
            btnUndo.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate); //To undo one

            listFramesPrivate.Clear();
            listFramesPrivate = new List<Bitmap>(listFramesUndoAll);

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            ResizeFormToImage();
        }

        private void nenuDeleteAfter_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            if (listFramesPrivate.Count > 1)
            {
                int countList = listFramesPrivate.Count - 1; //So we have a fixed value

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    listFramesPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void menuDeleteBefore_Click(object sender, EventArgs e)
        {
            btnUndo.Enabled = true;
            btnReset.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            if (listFramesPrivate.Count > 1)
            {
                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void exportFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopPreview();
            SaveFileDialog sfdExport = new SaveFileDialog();
            sfdExport.DefaultExt = "jpg";
            sfdExport.Filter = "JPG Image (*.jpg)|*.jpg";
            sfdExport.FileName = Resources.Msg_Frame + trackBar.Value;

            if (sfdExport.ShowDialog() == DialogResult.OK)
            {
                Bitmap expBitmap = listFramesPrivate[trackBar.Value];
                expBitmap.Save(sfdExport.FileName, ImageFormat.Jpeg);
                MessageBox.Show(Resources.Msg_Frame + trackBar.Value + Resources.Msg_Exported, Resources.Msg_ExportedTitle);
                expBitmap.Dispose();
            }
            sfdExport.Dispose();
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

        private void resizeAllFramesToolStripMenuItem_Click(object sender, EventArgs e)
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
            if (crop.ShowDialog(this) == DialogResult.OK)
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

                listFramesPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Insert one image to desired position in the list
        /// </summary>
        private void imageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)
            {

                btnUndo.Enabled = true;
                btnReset.Enabled = true;

                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

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

        /// <summary>
        /// ListFramesInverted
        /// </summary>
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

        /// <summary>
        /// Make a Yoyo with the frames (listFrames + listFramesInverted)
        /// </summary>
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
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
                btnPreview.Text = Resources.Con_PlayPreview;
                btnPreview.Image = Resources.Play_17Green;
            }
            else
            {
                this.Text = "Screen To Gif - Playing Animation";
                btnPreview.Text = Resources.Con_StopPreview;
                btnPreview.Image = Resources.Stop_17Red;
                actualFrame = trackBar.Value;
                timerPlayPreview.Start();
            }

        }

        private void StopPreview()
        {
            timerPlayPreview.Stop();
            btnPreview.Text = Resources.Con_PlayPreview;
            btnPreview.Image = Resources.Play_17Green;
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

        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopPreview();
        }

        #endregion

    }
}
