using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ScreenToGif.Capture;
using ScreenToGif.Encoding;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif.Pages
{
    public partial class Content : UserControl
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
        /// The form that calls this User Control. This is needed to access the form properties.
        /// </summary>
        private Form _caller;

        /// <summary>
        /// Unused constructor, just for the designer.
        /// </summary>
        //[Obsolete("This method should not be called.")]
        public Content()
        {
            InitializeComponent();

            #region Load Save Data

            //Gets and sets the fps
            

            //Load last saved window size
            //_caller.Size = new Size(Properties.Settings.Default.STsize.Width, Properties.Settings.Default.STsize.Height);

            try
            {
                numMaxFps.Value = Settings.Default.STmaxFps;
                this.Parent.Size = new Size(Properties.Settings.Default.STsize.Width, Properties.Settings.Default.STsize.Height);

            }
            catch (Exception e)
            {
                
            }
            
            #endregion
        }

        /// <summary>
        /// Constructor of the Internal page.
        /// </summary>
        public Content(Form caller)
        {
            InitializeComponent();

            //Set as global, the form caller.
            _caller = caller;

            //Gets the recording area size and show in the textBoxes
            tbHeight.Text = panelTransparent.Height.ToString();
            tbWidth.Text = panelTransparent.Width.ToString();

            //Gets the window chrome offset - CHECK
            _offsetX = (_caller.Size.Width - panelTransparent.Width) / 2;
            _offsetY = (_caller.Size.Height - panelTransparent.Height - _offsetX - flowPanel.Height - 1);

            //Starts the global keyboard hook.
            #region Global Hook
            _actHook = new UserActivityHook();
            _actHook.KeyDown += KeyHookTarget;
            _actHook.Start(false, true); //false for the mouse, true for the keyboard.
            #endregion

        }

        /// <summary>
        /// Destructor of this page, I had to use this, because there is no Closing event for this form. (TESTING)
        /// </summary>
        ~Content()
        {
            MessageBox.Show("Saindo", "Saindo");

            Properties.Settings.Default.STmaxFps = Convert.ToInt32(numMaxFps.Value);
            Properties.Settings.Default.STsize = new Size(_caller.Size.Width, _caller.Size.Height);

            Properties.Settings.Default.Save();

            _actHook.Stop(); //Stop the keyboard watcher.

            if (_stage != (int)Stage.Stoped)
            {
                timerCapture.Stop();
                timerCapture.Dispose();

                timerCapWithCursor.Stop();
                timerCapWithCursor.Dispose();
            }

            this.Dispose(true);
        }

        #region UserControl Events

        private void Content_Resize(object sender, EventArgs e)
        {
            if (!_screenSizeEdit) //if not typing in the textBoxes, I use this to prevent flickering of the size. It only updates after typing.
            {
                tbHeight.Text = panelTransparent.Height.ToString();
                tbWidth.Text = panelTransparent.Width.ToString();
            }
        }

        private void Content_Load(object sender, EventArgs e)
        {
            //This code needs to be executed AFTER the loading of the form.
            //Because it needs to have a parent form, or it will trigger a null exception.
            //#region Load Save Data

            ////Gets and sets the fps
            //numMaxFps.Value = Settings.Default.STmaxFps;

            ////Load last saved window size
            //_caller.Size = new Size(Properties.Settings.Default.STsize.Width, Properties.Settings.Default.STsize.Height);

            //#endregion
        }

        #endregion

        #region Bottom Buttons

        readonly Control info = new Information(); //Information page
        readonly Control appSettings = new AppSettings(true); //App Settings page, true = legacy, false = modern
        readonly Control gifSettings = new GifSettings(); //Gif Settings page

        private void btnStop_Click(object sender, EventArgs e)
        {
            _caller.MaximizeBox = true;
            _caller.MinimizeBox = true;

            Stop();
        }

        private void btnRecordPause_Click(object sender, EventArgs e)
        {
            panelTransparent.Controls.Clear(); // removes all pages from the top
            _caller.MaximizeBox = false;
            _caller.MinimizeBox = false;

            RecordPause(); //and start the pre-start tick
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
                _listCursor = new List<CursorInfo>(); //List that contains all the icon information

                _bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                _gr = Graphics.FromImage(_bt);

                _caller.Text = "Screen To Gif (2 " + Resources.TitleSecondsToGo;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                btnRecordPause.Enabled = false;
                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                //_caller.FormBorderStyle = FormBorderStyle.FixedSingle;
                _stage = (int)Stage.PreStarting;
                numMaxFps.Enabled = false;
                _preStartCount = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                timerPreStart.Start();
                _caller.TopMost = true;

                #endregion
            }
            else if (_stage == (int)Stage.Recording) //if recording, pauses
            {
                #region To Pause

                _caller.Text = Resources.TitlePaused;
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

                _caller.Text = Resources.TitleRecording;
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

                        _caller.Cursor = Cursors.WaitCursor;
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
                        _caller.Cursor = Cursors.Default;

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
                        _lastSize = _caller.Size; //To return back to the last form size after the editor
                        _stage = (int)Stage.Editing;
                        _caller.MaximizeBox = true;
                        _caller.MinimizeBox = true;
                        //_caller.FormBorderStyle = FormBorderStyle.Sizable;
                        EditFrames();
                        flowPanel.Enabled = false;
                    }
                    else
                    {
                        _lastSize = _caller.Size; //Not sure why this is here
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
                    //_caller.FormBorderStyle = FormBorderStyle.Sizable;

                    _caller.MaximizeBox = true;
                    _caller.MinimizeBox = true;

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Properties.Resources.Record;
                    _caller.Text = Resources.TitleStoped;

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
            _caller.Cursor = Cursors.WaitCursor;
            _caller.Size = _lastSize;
            
            Application.DoEvents();

            if (!Settings.Default.STsaveLocation) // to choose the location to save the gif
            {
                #region If Not Save Directly to the desktop

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                _caller.Cursor = Cursors.Default;

                if (sfd.ShowDialog() == DialogResult.OK) //if ok
                {
                    _outputpath = sfd.FileName;

                    _workerThread = new Thread(DoWork);
                    _workerThread.IsBackground = true;
                    _workerThread.Start();
                }
                else //if user son't want to save the gif
                {
                    FinishState();

                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Record;
                    _caller.Text = Resources.TitleStoped;

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

        }

        /// <summary>
        /// Do all the work to set the controls to the finished state. (i.e. Finished encoding)
        /// </summary>
        private void FinishState()
        {
            _caller.Cursor = Cursors.Default;
            flowPanel.Enabled = true;
            _stage = (int)Stage.Stoped;
            _caller.MinimumSize = new Size(100, 100);
            _caller.Size = _lastSize;

            numMaxFps.Enabled = true;
            tbHeight.Enabled = true;
            tbWidth.Enabled = true;
            _caller.MaximizeBox = true;
            _caller.MinimizeBox = true;

            btnRecordPause.Text = Resources.btnRecordPause_Record;
            btnRecordPause.Image = Properties.Resources.Record;
            _caller.Text = Resources.TitleStoped;
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
                processing.SetStatus(1);

            });

            #endregion

            if (Settings.Default.STencodingCustom) // if NGif encoding
            {
                #region Ngif encoding

                int numImage = 0;

                using (_encoder = new AnimatedGifEncoder())
                {
                    _encoder.Start(_outputpath);
                    _encoder.SetQuality(Settings.Default.STquality);

                    //BUG: The minimum amount of iterations is -1 (no repeat) or 3 (repeat number being 2), if you set repeat as 1, it will repeat 2 times, instead of just 1.
                    //0 = Always, -1 = no repeat, n = repeat number (first shown + repeat number = total number of iterations)
                    _encoder.SetRepeat(Settings.Default.STloop ? (Settings.Default.STrepeatForever ? 0 : Settings.Default.STrepeatCount) : -1); // 0 = Always, -1 once

                    try
                    {
                        foreach (var image in _listBitmap)
                        {
                            this.BeginInvoke((Action)(() => processing.SetStatus(numImage)));

                            _encoder.SetDelay(_listDelay[numImage]);
                            _encoder.AddFrame(image);
                            numImage++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWriter.Log(ex, "Ngif encoding.");
                    }

                }

                #endregion
            }
            else //if paint.NET encoding
            {
                #region paint.NET encoding

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

                    using (var fileStream = new FileStream(_outputpath, FileMode.Create, FileAccess.Write, FileShare.None,
                            Constants.BufferSize, false))
                    {
                        stream.WriteTo(fileStream);
                    }
                }

                #endregion
            }

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

            #region Finish

            try
            {
                this.BeginInvoke((Action)delegate //must use invoke because it's a cross thread call
                {
                    _caller.Text = Resources.Title_EncodingDone;
                    _stage = (int)Stage.Stoped;

                    panelTransparent.Controls.Clear(); //Clears the processing page.
                    processing.Dispose();
                    _caller.Invalidate();

                    FinishState();

                    _actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
                    _actHook.Start(false, true); //start again the keyboard hook watcher
                });
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Invoke error.");
            }

            #endregion
        }

        #endregion

        #region Timer's Events

        /// <summary>
        /// Timer used after clicking in Record, to give the user a shor time to prepare recording
        /// </summary>
        private void timerPreStart_Tick(object sender, EventArgs e)
        {
            if (_preStartCount >= 1)
            {
                _caller.Text = "Screen To Gif (" + _preStartCount + Resources.TitleSecondsToGo;
                _preStartCount--;
            }
            else //if 0, starts (timer OR timer with cursor)
            {
                _caller.Text = Resources.TitleRecording;
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
        }

        /// <summary>
        /// Takes a screenshot of desired area and add to the list.
        /// </summary>
        private void timerCapture_Tick(object sender, EventArgs e)
        {
            //Get the actual position of the form.
            Point lefttop = new Point(_caller.Location.X + _offsetX, _caller.Location.Y + _offsetY);
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
            Point lefttop = new Point(_caller.Location.X + _offsetX, _caller.Location.Y + _offsetY);
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

            int offsetY = _caller.Height - panelTransparent.Height;
            int offsetX = _caller.Width - panelTransparent.Width;

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

            _caller.Size = new Size(widthTb, heightTb);

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

                int offsetY = _caller.Height - panelTransparent.Height;
                int offsetX = _caller.Width - panelTransparent.Width;

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

                _caller.Size = new Size(widthTb, heightTb);

                _screenSizeEdit = false;
            }
        }

        #endregion

        #region Frame Edit Stuff

        private List<Bitmap> _listFramesPrivate; //List of frames that will be edited.
        private List<Bitmap> _listFramesUndo;  //List of frames that holds the last alteration

        private List<int> _listDelayPrivate;
        private List<int> _listDelayUndo;

        /// <summary>
        /// Constructor of the Frame Edit Page.
        /// </summary>
        private void EditFrames()
        {
            _caller.Cursor = Cursors.WaitCursor;

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

            _caller.MinimumSize = new Size(100, 100);
            _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

            ResizeFormToImage(); //Resizes the form to hold the image

            pictureBitmap.Image = _listFramesPrivate.First(); //Puts the first image of the list in the pictureBox

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

            _caller.Cursor = Cursors.Default;
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
            _caller.Text = Resources.Title_Edit_PromptToSave;
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
            pictureBitmap.Image = (Bitmap)_listFramesPrivate[trackBar.Value];
            _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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
            _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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
            _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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

            _caller.Size = sizeBitmap;

            bitmap.Dispose();

            #endregion
        }

        #region Context Menu Itens

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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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
                toolTip.Show(Resources.Msg_Frame + trackBar.Value + Resources.Msg_Exported, btnOptions, 0, btnOptions.Height);
                //MessageBox.Show(Resources.Msg_Frame + trackBar.Value + Resources.Msg_Exported, Resources.Msg_ExportedTitle);
                expBitmap.Dispose();
            }
            sfdExport.Dispose();
        }

        /// <summary>
        /// Show Grid event handler.
        /// </summary>
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

                _listFramesPrivate = ImageUtil.ResizeAllBitmap(_listFramesPrivate, resized.Width, resized.Height);

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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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
        /// Insert one image to desired position in the list
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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
            }
        }

        /// <summary>
        /// ListFrames Inverted
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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);

                #region Delay Display

                _delay = _listDelayPrivate[trackBar.Value];
                lblDelay.Text = _delay + " ms";

                #endregion
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

            _listFramesPrivate[trackBar.Value] = ImageUtil.MakeGrayscale((Bitmap)pictureBitmap.Image);
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
            btnReset.Enabled = true;
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
            btnReset.Enabled = true;
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
            btnReset.Enabled = true;
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
            btnReset.Enabled = true;
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
            btnReset.Enabled = true;

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
            _listFramesPrivate = ImageUtil.GrayScale(_listFramesPrivate);
            pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
            this.Cursor = Cursors.Default;
            btnReset.Enabled = true;
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
                _listFramesPrivate = ImageUtil.Pixelate(_listFramesPrivate,
                    new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value);
                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Cursor = Cursors.Default;
            }

            valuePicker.Dispose();
            btnReset.Enabled = true;
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

                //This thing down here didn't make any difference. Still hangs.
                // http://www.codeproject.com/Articles/45787/Easy-asynchronous-operations-with-AsyncVar Oh, I see now, this thing it's good only with multiple actions.
                var asyncVar =
                    new AsyncVar<List<Bitmap>>(
                        () =>
                            ImageUtil.Blur(_listFramesPrivate,
                                new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height),
                                valuePicker.Value));
                //_listFramesPrivate = ImageUtil.Blur(_listFramesPrivate, new Rectangle(0, 0, pictureBitmap.Image.Width, pictureBitmap.Image.Height), valuePicker.Value);

                _listFramesPrivate = new List<Bitmap>(asyncVar.Value);

                pictureBitmap.Image = _listFramesPrivate[trackBar.Value];
                this.Cursor = Cursors.Default;
            }

            valuePicker.Dispose();
            btnReset.Enabled = true;
        }

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
            btnReset.Enabled = true;
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
            btnReset.Enabled = true;
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
            btnReset.Enabled = true;
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
                _caller.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (_listFramesPrivate.Count - 1);
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
                _caller.Text = "Screen To Gif - Playing Animation"; //Localize this.
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

            #region Delay Display

            _delay = _listDelayPrivate[trackBar.Value];
            lblDelay.Text = _delay + " ms";

            #endregion
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
                toolStripTextBox.Text = _delay.ToString();
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
        private void toolStripTextBox_TextChanged(object sender, EventArgs e)
        {
            if (toolStripTextBox.Text.Equals(""))
                toolStripTextBox.Text = "0";

            _delay = Convert.ToInt32(toolStripTextBox.Text);

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

        #endregion
    }
}
