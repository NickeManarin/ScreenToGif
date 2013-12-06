using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using ScreenToGif.Capture;
using ScreenToGif.Properties;
using AnimatedGifEncoder = ScreenToGif.Encoding.AnimatedGifEncoder;
using Cursor = System.Windows.Forms.Cursor;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace ScreenToGif
{
    public partial class Principal :Form
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
        CaptureScreen capture = new CaptureScreen();

        private UserActivityHook actHook;
        private Thread HookThread;
        //private string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        private int numOfFile = 0;
        private int preStart = 1;
        private bool frameEdit = false;
        private Size lastSize; //The editor may increase the size of the form, use this to go back to the last size
        private bool screenSizeEdit = false;
        private string outputpath;
        private int stage = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding

        private List<IntPtr> listBitmap;
        private List<CursorInfo> listCursor = new List<CursorInfo>(); //List that stores the icon

        private CursorInfo cursorInfo;
        private Rectangle rect;

        private Point posCursor;
        private Point sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Bitmap bt;
        private Graphics gr;
        private Thread workerThread;

        LocalizationString local = new LocalizationString();

        public Principal() //Constructor
        {
            #region Load Localized Strings
            //Before the initialize components

            //CultureInfo ci = CultureInfo.InstalledUICulture;

            //local.ChangeStrings(ci.TwoLetterISOLanguageName, true);

            #endregion

            InitializeComponent();

            #region Load Save Data

            cbShowCursor.Checked = Settings.Default.STshowCursor;
            cbAllowEdit.Checked = Settings.Default.STallowEdit;
            cbSaveDirectly.Checked = Settings.Default.STsaveLocation;
            cbModernStyle.Checked = Settings.Default.STmodernStyle;
            numMaxFps.Value = Settings.Default.STmaxFps;

            //Gets the Hotkeys
            comboStartPauseKey.Text = Settings.Default.STstartPauseKey.ToString();
            comboStopKey.Text = Settings.Default.STstopKey.ToString();

            //Gets the Gif settings
            trackBarQuality.Value = Properties.Settings.Default.STquality;
            cbLoop.Checked = Properties.Settings.Default.STloop;

            //Load last saved window size
            this.Size = new Size(Properties.Settings.Default.STsize.Width, Properties.Settings.Default.STsize.Height);

            #endregion

            tbHeight.Text = (this.Height - 71).ToString();
            tbWidth.Text = (this.Width - 16).ToString();

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

        private void MouseHookTarget(object sender, System.Windows.Forms.MouseEventArgs keyEventArgs)
        {
            //e.X, e.Y
        }

        private void RecordPause()
        {
            if (stage == 0)
            {
                encoder.SetQuality(trackBarQuality.Value);

                encoder.SetRepeat(cbLoop.Checked ? 0 : -1); // 0 = Always, -1 once

                encoder.SetSize(panelTransparent.Size.Width, panelTransparent.Size.Height);
                encoder.SetFrameRate(Convert.ToInt32(numMaxFps.Value));
                timerCapture.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);
                timerCapWithCursor.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<IntPtr>(); //List that contains all the frames.
                listCursor = new List<CursorInfo>(); //List that contains all the icon information

                bt = new Bitmap(panelTransparent.Width, panelTransparent.Height);
                gr = Graphics.FromImage(bt);

                this.Text = "Screen To Gif (2 " + Resources.TitleSecondsToGo;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                btnRecordPause.Enabled = false;
                tbHeight.Enabled = false;
                tbWidth.Enabled = false;
                //this.FormBorderStyle = FormBorderStyle.FixedSingle;
                stage = 3;
                numMaxFps.Enabled = false;
                preStart = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                timerPreStart.Start();
                this.TopMost = true;
            }
            else if (stage == 1)
            {
                this.Text = Resources.TitlePaused;
                btnRecordPause.Text = Resources.btnRecordPause_Continue;
                btnRecordPause.Image = Properties.Resources.Play_17Green;
                stage = 2;

                timerCapture.Enabled = false;
            }
            else if (stage == 2)
            {
                this.Text = Resources.TitleRecording;
                btnRecordPause.Text = Resources.Pause;
                btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                stage = 1;

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
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    EditFrames();
                    flowPanel.Enabled = false;
                }
                else
                {
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

                btnRecordPause.Image = Properties.Resources.Play_17Green;
                this.Text = Resources.TitleStoped;

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
                    this.MinimumSize = new Size(200, 100);
                    stage = 0; //Stoped
                    numMaxFps.Enabled = true;
                    tbWidth.Enabled = true;
                    tbHeight.Enabled = true;
                    this.TopMost = false;
                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Resources.Play_17Green;
                    this.Text = Resources.TitleStoped;

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

            this.MinimumSize = new Size(250, 100);
            stage = 0; //Stoped
            numMaxFps.Enabled = true;
            tbHeight.Enabled = false;
            tbWidth.Enabled = false;
            this.TopMost = false;
            this.Text = Resources.TitleStoped;
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
                        this.Text = Resources.Title_Thread_ProcessingFrame + numImage + Resources.Title_Thread_out_of + countList + ")";
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

                    actHook.KeyDown += KeyHookTarget;
                    actHook.Start();
                });
            }
            catch (Exception)
            {
            }

            encoder.Finish();

            this.Invoke((Action)(() => actHook.Start()));
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
                        this.Text = Resources.Title_Thread_ProcessingFrame + numImage + Resources.Title_Thread_out_of + countList + ")";
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
                    this.Text = Resources.Title_EncodingDone;
                    stage = 0;
                    numMaxFps.Enabled = true;
                    btnRecordPause.Text = Resources.btnRecordPause_Record;
                    btnRecordPause.Image = Properties.Resources.Play_17Green;
                    flowPanel.Enabled = true;
                    this.TopMost = false;

                    tbHeight.Enabled = true;
                    tbWidth.Enabled = true;

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

        #region Main Form Resize/Closing

        private void Principal_Resize(object sender, EventArgs e) //To show the exactly size of the form.
        {
            if (!screenSizeEdit)
            {
                tbHeight.Text = (this.Height - 71).ToString();
                tbWidth.Text = (this.Width - 16).ToString();
            }
        }

        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.STmaxFps = Convert.ToInt32(numMaxFps.Value);
            Properties.Settings.Default.STsize = new Size(this.Size.Width, this.Size.Height);
            Properties.Settings.Default.STshowCursor = cbShowCursor.Checked;
            Properties.Settings.Default.STallowEdit = cbAllowEdit.Checked;
            Properties.Settings.Default.STsaveLocation = cbSaveDirectly.Checked;
            Properties.Settings.Default.STmodernStyle = cbModernStyle.Checked;

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

        #region Timers

        private void PreStart_Tick(object sender, EventArgs e)
        {
            if (preStart >= 1)
            {
                this.Text = "Screen To Gif (" + preStart + Resources.TitleSecondsToGo;
                preStart--;
            }
            else
            {
                this.Text = Resources.TitleRecording;
                timerPreStart.Stop();
                if (cbShowCursor.Checked)
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
            listBitmap.Add(bt.GetHbitmap());
        } //CAPTURE TIMER

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

        #region Bottom buttons

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            Stop();
        } //STOP

        private void btnPauseRecord_Click(object sender, EventArgs e)
        {
            panelConfig.Visible = false;
            panelGifConfig.Visible = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            RecordPause();
        } //RECORD-PAUSE

        private void btnConfig_Click(object sender, EventArgs e)
        {
            if (panelConfig.Visible) //Save the setting
            {
                Properties.Settings.Default.STshowCursor = cbShowCursor.Checked;
                Properties.Settings.Default.STallowEdit = cbAllowEdit.Checked;
                Properties.Settings.Default.STsaveLocation = cbSaveDirectly.Checked;
                Properties.Settings.Default.STmodernStyle = cbModernStyle.Checked;

                Properties.Settings.Default.STstartPauseKey = getKeys(comboStartPauseKey.Text);
                Properties.Settings.Default.STstopKey = getKeys(comboStopKey.Text);
                Properties.Settings.Default.Save();
            }

            panelGifConfig.Visible = false;
            panelConfig.Visible = !panelConfig.Visible;
        } //CONFIG

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
            if (stage != 1)
            {
                Info info = new Info();
                info.Show();
            }
        } //INFO

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

        #region ComboBoxes Events

        private void comboStartPauseKey_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboStartPauseKey.Text.Equals(comboStopKey.Text))
            {
                comboStartPauseKey.Text = Properties.Settings.Default.STstartPauseKey.ToString();
            }
        }

        private void comboStopKey_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboStopKey.Text.Equals(comboStartPauseKey.Text))
            {
                comboStopKey.Text = Properties.Settings.Default.STstopKey.ToString();
            }
        }

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
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

            #region Window size
            Bitmap bitmap = Bitmap.FromHbitmap(listFramesPrivate[0]);

            Size sizeBitmap = new Size(bitmap.Size.Width + 80, bitmap.Size.Height + 160);

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

        }

        private void btnDone_Click(object sender, EventArgs e)
        {
            listBitmap = listFramesPrivate;

            if (cbShowCursor.Checked)
            listCursor = listCursorPrivate;

            panelEdit.Visible = false;
            frameEdit = false;
            this.Text = Resources.Title_Edit_PromptToSave;
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
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
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
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
            }
            else
            {
                MessageBox.Show(Resources.MsgBox_Message_CantDelete, Resources.MsgBox_Title_CantDelete, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndoOne_Click(object sender, EventArgs e)
        {
            listFramesPrivate = listFramesUndo;

            if (cbShowCursor.Checked)
            listCursorPrivate = listCursorUndo;

            trackBar.Maximum = listFramesPrivate.Count - 1;
            pictureBitmap.Image = Bitmap.FromHbitmap(listFramesPrivate[trackBar.Value]);
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);

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
            this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
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
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
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
                this.Text = Resources.Title_EditorFrame + trackBar.Value + " - " + (listFramesPrivate.Count - 1);
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
