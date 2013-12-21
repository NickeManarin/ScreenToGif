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
using AnimatedGifEncoder = ScreenToGif.Encoding.AnimatedGifEncoder;
using Cursor = System.Windows.Forms.Cursor;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace ScreenToGif
{
    /// <summary>
    /// Legacy
    /// </summary>
    public partial class Legacy : Form
    {
        #region Form Dragging API Support
        //The SendMessage function sends a message to a window or windows.  
        //[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        //static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        //ReleaseCapture releases a mouse capture 
        //[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        //public static extern bool ReleaseCapture();

        #endregion

        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        readonly CaptureScreen capture = new CaptureScreen();

        private readonly UserActivityHook actHook;
        private int preStart = 1;
        private Size lastSize; //The editor may increase the size of the form, use this to go back to the last size
        private bool screenSizeEdit;
        private string outputpath;
        private int stage = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding

        public List<Bitmap> listBitmap;
        public List<CursorInfo> listCursor = new List<CursorInfo>(); //List that stores the icon

        private CursorInfo cursorInfo;
        private Rectangle rect;

        private Point posCursor;
        private Point sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Bitmap bt;
        private Graphics gr;
        private Thread workerThread;

        private bool _isPageGifOpen;
        private bool _isPageAppOpen;
        private bool _isPageInfoOpen;

        public Legacy() //Constructor
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
            actHook = new UserActivityHook();
            actHook.KeyDown += KeyHookTarget;
            actHook.Start(false, true); //false for the mouse, true for the keyboard.
            #endregion
        }

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
            if (stage == 0) //if stoped
            {
                timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);
                timerCapWithCursor.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<Bitmap>(); //List that contains all the frames.
                listCursor = new List<CursorInfo>(); //List that contains all the icon information

                bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                gr = Graphics.FromImage(bt);

                this.Text = "Screen To Gif (2 " + Resources.TitleSecondsToGo;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                btnRecordPause.Enabled = false;
                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                //this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                stage = 3;
                numMaxFps.Enabled = false;
                preStart = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                timerPreStart.Start();
                this.TopMost = true;
            }
            else if (stage == 1) // if recording
            {
                this.Text = Resources.TitlePaused;
                btnRecordPause.Text = Resources.btnRecordPause_Continue;
                btnRecordPause.Image = Properties.Resources.Play_17Green;
                stage = 2;

                if (Settings.Default.STshowCursor) //if show cursor
                {
                    timerCapWithCursor.Enabled = false;
                }
                else
                {
                    timerCapture.Enabled = false;
                }

            }
            else if (stage == 2) //if paused
            {
                this.Text = Resources.TitleRecording;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                stage = 1;

                if (Settings.Default.STshowCursor) //if show cursor
                {
                    timerCapWithCursor.Enabled = true;
                }
                else
                {
                    timerCapture.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Stops the recording or stops the pre-start timer
        /// </summary>
        private void Stop()
        {
            actHook.Stop();
            actHook.KeyDown -= KeyHookTarget;

            timerCapture.Stop();
            timerCapWithCursor.Stop();

            if (Settings.Default.STshowCursor) //If show cursor is true
            {
                Graphics graph;
                int numImage = 0;

                foreach (var bitmap in listBitmap)
                {
                    graph = Graphics.FromImage(bitmap);
                    rect = new Rectangle(listCursor[numImage].Position.X, listCursor[numImage].Position.Y, listCursor[numImage].Icon.Width, listCursor[numImage].Icon.Height);

                    graph.DrawIcon(listCursor[numImage].Icon, rect);
                    graph.Flush();
                    numImage++;
                }
            }

            if (stage != 0 && stage != 3) //if not already stop or pre starting
            {
                if (Settings.Default.STallowEdit)
                {
                    lastSize = this.Size; //To return back to the last form size after the editor
                    stage = 4;
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    EditFrames();
                    flowPanel.Enabled = false;
                }
                else
                {
                    lastSize = this.Size;
                    Save();
                }
            }
            else if (stage == 3) // if pre starting
            {
                timerPreStart.Stop();
                stage = 0;
                numMaxFps.Enabled = true;
                btnRecordPause.Enabled = true;
                numMaxFps.Enabled = true;
                tbHeight.Enabled = true;
                tbWidth.Enabled = true;

                this.MaximizeBox = true;
                this.MinimizeBox = true;

                btnRecordPause.Text = Resources.btnRecordPause_Record;
                btnRecordPause.Image = Properties.Resources.Play_17Green;
                this.Text = Resources.TitleStoped;

                actHook.KeyDown += KeyHookTarget;
                actHook.Start(false, true);
            }

        }

        /// <summary>
        /// Prepares the recorded frames to be saved/edited
        /// </summary>
        private void Save()
        {
            this.Size = lastSize;
            if (!Settings.Default.STsaveLocation) // to choose the location to save the gif
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "GIF file (*.gif)|*gif";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                sfd.DefaultExt = "gif";

                if (sfd.ShowDialog() == DialogResult.OK) //if ok
                {
                    outputpath = sfd.FileName;

                    workerThread = new Thread(DoWork);
                    workerThread.IsBackground = true;
                    workerThread.Start();
                }
                else //if user son't want to save the gif
                {
                    flowPanel.Enabled = true;
                    this.MinimumSize = new Size(250, 100);
                    stage = 0; //Stoped
                    numMaxFps.Enabled = true;
                    tbWidth.Enabled = true;
                    tbHeight.Enabled = true;

                    this.TopMost = false;
                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Play_17Green;
                    this.Text = Resources.TitleStoped;

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start(false, true);

                    return;
                }
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

            this.MinimumSize = new Size(250, 100);
            stage = 0; //Stoped
            numMaxFps.Enabled = true;
            tbHeight.Enabled = false;
            tbWidth.Enabled = false;
            this.TopMost = false;
            this.Text = Resources.TitleStoped;
        }

        /// <summary>
        /// Thread method that encodes the list of frames.
        /// </summary>
        private void DoWork() //Thread
        {

            if (Settings.Default.STencodingCustom) // if NGif encoding
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
                                this.Text = Resources.Title_Thread_ProcessingFrame + numImage + Resources.Title_Thread_out_of + countList + ")";
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
            else //if paint.NET encoding
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
                                this.Text = Resources.Title_Thread_ProcessingFrame + i + Resources.Title_Thread_out_of +
                                            countList + ")";
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

            //Clear all the lists,  MUST do this, or else it will steal lots of RAM
            if (Settings.Default.STshowCursor) //if show cursor
            {
                listCursorPrivate.Clear();
                listCursorUndoAll.Clear();
                listCursorUndo.Clear();

                listCursorPrivate = null;
                listCursorUndoAll = null;
                listCursorUndo = null;
            }

            listFramesPrivate.Clear();
            listFramesUndo.Clear();
            listFramesUndoAll.Clear();

            listFramesPrivate = null;
            listFramesUndo = null;
            listFramesUndoAll = null;
            encoder = null;

            GC.Collect(); //call the garbage colector to empty the memory

            try
            {
                this.Invoke((Action)delegate //must use invoke because it's a cross thread call
                {
                    this.Text = Resources.Title_EncodingDone;
                    stage = 0;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Properties.Resources.Play_17Green;
                    flowPanel.Enabled = true;
                    this.TopMost = false;

                    numMaxFps.Enabled = true;
                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

                    this.MaximizeBox = true;
                    this.MinimizeBox = true;

                    actHook.KeyDown += KeyHookTarget; //Set again the keyboard hook method
                    actHook.Start(false, true); //start again the keyboard hook watcher
                });
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Main Form Resize/Closing

        /// <summary>
        /// Used to update the Size values in the bottom of the window
        /// </summary>
        private void Principal_Resize(object sender, EventArgs e) //To show the exactly size of the form.
        {
            this.Invalidate(true);
            panelTransparent.Invalidate();

            if (!screenSizeEdit) //if not typing in the textBoxes, I use this to prevent flickering of the size. It only updates after typing.
            {
                tbHeight.Text = (this.Height - 71).ToString(); //71 px is the size of Top + Bottom borders
                tbWidth.Text = (this.Width - 16).ToString(); //16 px is the size of Left + Right borders
            }
        }

        /// <summary>
        /// Before close all settings must be saved and the timer must be disposed.
        /// </summary>
        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.STmaxFps = Convert.ToInt32(numMaxFps.Value);
            Properties.Settings.Default.STsize = new Size(this.Size.Width, this.Size.Height);

            Properties.Settings.Default.Save();

            actHook.Stop(); //Stop the keyboard watcher.

            if (stage != 0)
            {
                timerCapture.Stop();
                timerCapture.Dispose();
            }

            this.Dispose(true);
        }

        #endregion

        #region Timers

        /// <summary>
        /// Timer used after clicking in Record, to give the user a shor time to prepare recording
        /// </summary>
        private void PreStart_Tick(object sender, EventArgs e)
        {
            if (preStart >= 1)
            {
                this.Text = "Screen To Gif (" + preStart + Resources.TitleSecondsToGo;
                preStart--;
            }
            else //if 0, starts (timer OR timer with cursor)
            {
                this.Text = Resources.TitleRecording;
                timerPreStart.Stop();
                if (Settings.Default.STshowCursor)
                {

                    stage = 1;
                    btnRecordPause.Enabled = true;

                    timerCapWithCursor.Start(); //Record with the cursor
                }
                else
                {
                    stage = 1;
                    btnRecordPause.Enabled = true;

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
            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            listBitmap.Add((Bitmap)bt.Clone());
        } //CAPTURE TIMER

        /// <summary>
        /// Takes a screenshot of desired area and add to the list, plus add to the list the position and icon of the cursor.
        /// </summary>
        private void timerCapWithCursor_Tick(object sender, EventArgs e)
        {
            cursorInfo = new CursorInfo
            {
                Icon = capture.CaptureIconCursor(ref posCursor),
                Position = panelTransparent.PointToClient(posCursor)
            };

            //saves to list the actual icon and position of the cursor
            listCursor.Add(cursorInfo);

            //Get the actual position of the form.
            Point lefttop = new Point(this.Location.X + 8, this.Location.Y + 31);

            //Take a screenshot of the area.
            gr.CopyFromScreen(lefttop.X, lefttop.Y, 0, 0, panelTransparent.Bounds.Size, CopyPixelOperation.SourceCopy);
            //Add the bitmap to a list
            listBitmap.Add((Bitmap)bt.Clone());
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

        #region TextBox Size

        /// <summary>
        /// Prevents keys!=numbers
        /// </summary>
        private void tbWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if only numbers, allow
            if (char.IsLetter(e.KeyChar) ||
                char.IsSymbol(e.KeyChar) ||
                char.IsWhiteSpace(e.KeyChar) ||
                char.IsPunctuation(e.KeyChar))
                e.Handled = true;
        } // TB SIZE

        /// <summary>
        /// Prevents keys!=numbers
        /// </summary>
        private void tbHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if only numbers, allow
            if (char.IsLetter(e.KeyChar) ||
                char.IsSymbol(e.KeyChar) ||
                char.IsWhiteSpace(e.KeyChar) ||
                char.IsPunctuation(e.KeyChar))
                e.Handled = true;
        } //TB SIZE

        /// <summary>
        /// After leaving the textBox, updates the size of the form with what is typed
        /// </summary>
        private void tbHeight_Leave(object sender, EventArgs e)
        {
            screenSizeEdit = true;
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (sizeScreen.Y > heightTb)
            {
                this.Size = new Size(widthTb + 16, heightTb + 71);
            }
            else
            {
                this.Size = new Size(widthTb + 16, sizeScreen.Y - 1);
            }
            screenSizeEdit = false;
        }

        /// <summary>
        /// After leaving the textBox, updates the size of the form with what is typed
        /// </summary>
        /// <param name="data"></param>
        private void tbWidth_Leave(object sender, EventArgs e)
        {
            screenSizeEdit = true; //So the Resize event won't trigger
            int heightTb = Convert.ToInt32(tbHeight.Text);
            int widthTb = Convert.ToInt32(tbWidth.Text);

            if (sizeScreen.X > widthTb)
            {
                this.Size = new Size(widthTb + 16, heightTb + 71);
            }
            else
            {
                this.Size = new Size(sizeScreen.X - 1, heightTb + 71);
            }
            screenSizeEdit = false;
        }

        /// <summary>
        /// If user press Enter, updates the form size (I should put this in a method)
        /// </summary>
        private void tbWidth_KeyDown(object sender, KeyEventArgs e) //Enter press
        {
            if (e.KeyData == Keys.Enter)
            {
                screenSizeEdit = true;
                int heightTb = Convert.ToInt32(tbHeight.Text);
                int widthTb = Convert.ToInt32(tbWidth.Text);

                if (sizeScreen.Y > heightTb)
                {
                    this.Size = new Size(widthTb + 16, heightTb + 71);
                }
                else
                {
                    this.Size = new Size(widthTb + 16, sizeScreen.Y - 1);
                }
                screenSizeEdit = false;
            }
        }

        /// <summary>
        /// User press Enter, updates the form size.
        /// </summary>
        private void tbHeight_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                screenSizeEdit = true; //So the Resize event won't trigger
                int heightTb = Convert.ToInt32(tbHeight.Text);
                int widthTb = Convert.ToInt32(tbWidth.Text);

                if (sizeScreen.X > widthTb)
                {
                    this.Size = new Size(widthTb + 16, heightTb + 71);
                }
                else
                {
                    this.Size = new Size(sizeScreen.X - 1, heightTb + 71);
                }
                screenSizeEdit = false;
            }
        }

        #endregion

        #region Frame Edit Stuff

        private List<Bitmap> listFramesPrivate; //List of frames that will be edited.
        private List<Bitmap> listFramesUndoAll; //List of frames that holds the last alteration
        private List<Bitmap> listFramesUndo; //List of frames used to reset, I should use the listBitmap, because it's the same

        //same for here:
        private List<CursorInfo> listCursorPrivate;
        private List<CursorInfo> listCursorUndoAll;
        private List<CursorInfo> listCursorUndo;

        /// <summary>
        /// "Constructor of the Frame Edit Page"
        /// </summary>
        private void EditFrames()
        {
            //Copies the listBitmap to all the lists
            listFramesPrivate = new List<Bitmap>(listBitmap);
            listFramesUndoAll = new List<Bitmap>(listBitmap);
            listFramesUndo = new List<Bitmap>(listBitmap);

            if (Settings.Default.STshowCursor) //if Cursor
            {
                listCursorPrivate = new List<CursorInfo>(listCursor);
                listCursorUndoAll = new List<CursorInfo>(listCursor);
                listCursorUndo = new List<CursorInfo>(listCursor);
            }
            Application.DoEvents();

            panelEdit.Visible = true;
            trackBar.Maximum = listFramesPrivate.Count - 1;
            trackBar.Value = 0;
            this.MinimumSize = new Size(100, 100);
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            ResizeFormToImage(); //Resizes the form to hold the image

            pictureBitmap.Image = listFramesPrivate.First(); //Puts the first image of the list in the pictureBox
        }

        /// <summary>
        /// Accepts all the alterations and hides this page.
        /// </summary>
        private void btnDone_Click(object sender, EventArgs e)
        {
            listBitmap = new List<Bitmap>(listFramesPrivate);

            if (Settings.Default.STshowCursor)
                listCursor = listCursorPrivate;

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
            panelEdit.Visible = false;
            Save();

            GC.Collect();
        }

        /// <summary>
        /// When the user slides the trackBar, the image updates.
        /// </summary>
        private void trackBar_Scroll(object sender, EventArgs e)
        {
            pictureBitmap.Image = (Bitmap)listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
        }

        private void btnDeleteFrame_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            if (listFramesPrivate.Count > 1) //If more than 1 image in the list
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                if (Settings.Default.STshowCursor) //if cursor
                    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

                listFramesPrivate.RemoveAt(trackBar.Value); //delete the select frame

                if (Settings.Default.STshowCursor)
                    listCursorPrivate.RemoveAt(trackBar.Value);

                trackBar.Maximum = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndoOne_Click(object sender, EventArgs e)
        {
            listFramesPrivate.Clear();
            listFramesPrivate = new List<Bitmap>(listFramesUndo);

            if (Settings.Default.STshowCursor)
                listCursorPrivate = listCursorUndo;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            btnUndoOne.Enabled = false;

            ResizeFormToImage();
        }

        private void btnUndoAll_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate); //To undo one

            if (Settings.Default.STshowCursor)
                listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            listFramesPrivate = listFramesUndoAll;

            if (Settings.Default.STshowCursor)
                listCursorPrivate = listCursorUndoAll;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = listFramesPrivate[trackBar.Value];
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            ResizeFormToImage();
        }

        private void nenuDeleteAfter_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            if (Settings.Default.STshowCursor)
                listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            if (listFramesPrivate.Count > 1)
            {
                int countList = listFramesPrivate.Count - 1; //So we have a fixed value

                for (int i = countList; i > trackBar.Value; i--) //from the end to the middle
                {
                    listFramesPrivate.RemoveAt(i);

                    if (Settings.Default.STshowCursor)
                        listCursorPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = listFramesPrivate.Count - 1;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
        }

        private void menuDeleteBefore_Click(object sender, EventArgs e)
        {
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            listFramesUndo.Clear();
            listFramesUndo = new List<Bitmap>(listFramesPrivate);

            if (Settings.Default.STshowCursor)
                listCursorUndo = new List<CursorInfo>(listCursorPrivate);

            if (listFramesPrivate.Count > 1)
            {
                for (int i = trackBar.Value - 1; i >= 0; i--)
                {
                    listFramesPrivate.RemoveAt(i); // I should use removeAt everywhere

                    if (Settings.Default.STshowCursor)
                        listCursorPrivate.RemoveAt(i);
                }

                trackBar.Maximum = listFramesPrivate.Count - 1;
                trackBar.Value = 0;
                pictureBitmap.Image = listFramesPrivate[trackBar.Value];
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
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
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

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
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

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
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

            if (listFramesPrivate.Count > 1)
            {
                listFramesUndo.Clear();
                listFramesUndo = new List<Bitmap>(listFramesPrivate);

                if (Settings.Default.STshowCursor)
                    listCursorUndo = new List<CursorInfo>(listCursorPrivate);

                listFramesPrivate.RemoveAt(trackBar.Value);

                if (Settings.Default.STshowCursor)
                    listCursorPrivate.RemoveAt(trackBar.Value);

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
                btnUndoOne.Enabled = true;
                btnUndoAll.Enabled = true;

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
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

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
            btnUndoOne.Enabled = true;
            btnUndoAll.Enabled = true;

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
    }
}
