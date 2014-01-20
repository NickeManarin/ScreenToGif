using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ScreenToGif_WPF.Capture;
using ScreenToGif_WPF.Encoding;
using ScreenToGif_WPF.Pages;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Windows.Size;

namespace ScreenToGif_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        AnimatedGifEncoder encoder = new AnimatedGifEncoder();
        private int preStart = 1;
        private Size lastSize; //The editor may increase the size of the form, use this to go back to the last size
        private bool screenSizeEdit;
        private string outputpath;
        private int stage = 0; //0 Stoped, 1 Recording, 2 Paused, 3 PreStart, 4 Editing, 5 Encoding

        public List<Bitmap> listBitmap;
        public List<CursorInfo> listCursor = new List<CursorInfo>(); //List that stores the icon

        //private CursorInfo cursorInfo;
        private Rectangle rect;

        private Point posCursor;
        //private Point sizeScreen = new Point(SystemInformation.PrimaryMonitorSize);
        private Bitmap bt;
        private Graphics gr;
        private Thread workerThread;

        System.Windows.Forms.Timer _preStartTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer _captureCursorTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer _captureTimer = new System.Windows.Forms.Timer();

        public MainWindow()
        {
            InitializeComponent();

            _preStartTimer.Tick += _preStartTimer_Tick;
            _preStartTimer.Interval = 1000; // 1 second interval
        }

        private void _preStartTimer_Tick(object sender, EventArgs e)
        {
            if (preStart >= 1)
            {
                this.Title = "Screen To Gif (" + preStart + Properties.Resources.TitleSecondsToGo;
                preStart--;
            }
            else //if 0, starts (timer OR timer with cursor)
            {
                this.Title = Properties.Resources.TitleRecording;
                _preStartTimer.Stop();

                
                if (Properties.Settings.Default.STshowCursor)
                {
                    stage = 1;
                    btnRecordPause.IsEnabled = true;

                    _captureCursorTimer.Start(); //Record with the cursor
                }
                else
                {
                    stage = 1;
                    btnRecordPause.IsEnabled = true;

                    _captureTimer.Start(); //Frame recording
                }
            }
        }

        public void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }

        private void btnRecordPause_Click(object sender, RoutedEventArgs e)
        {
            frameAlpha.Source = null; // removes all pages from the top
            this.ShowMinButton = false;
            this.ShowMaxRestoreButton = false;

            RecordPause(); //and start the pre-start tick
        }

        private void RecordPause()
        {
            if (stage == 0) //if stoped
            {
                _captureTimer.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);
                _captureCursorTimer.Interval = 1000 / Convert.ToInt32(numMaxFps.Value);

                listBitmap = new List<Bitmap>(); //List that contains all the frames.
                listCursor = new List<CursorInfo>(); //List that contains all the icon information

                bt = new Bitmap((int)frameAlpha.ActualWidth, (int)frameAlpha.ActualHeight);
                gr = Graphics.FromImage(bt);

                this.Title = "Screen To Gif (2 " + Properties.Resources.TitleSecondsToGo;
                btnRecordPause.Content = Properties.Resources.Pause;
                //btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                btnRecordPause.IsEnabled = false;

                //tbHeight.Enabled = false;
                //tbWidth.Enabled = false;

                stage = 3;
                numMaxFps.IsEnabled = false;
                preStart = 1; //Reset timer to 2 seconds, 1 second to trigger the timer so 1 + 1 = 2

                _preStartTimer.Start();
                this.Topmost = true;
            }
            else if (stage == 1) // if recording
            {
                this.Title = Properties.Resources.TitlePaused;
                btnRecordPause.Content = Properties.Resources.btnRecordPause_Continue;
                //btnRecordPause.Image = Properties.Resources.Play_17Green;
                stage = 2;

                if (Properties.Settings.Default.STshowCursor) //if show cursor
                {
                    _captureCursorTimer.Enabled = false;
                }
                else
                {
                    _captureTimer.Enabled = false;
                }

            }
            else if (stage == 2) //if paused
            {
                this.Title = Properties.Resources.TitleRecording;
                btnRecordPause.Content = Properties.Resources.Pause;
                //btnRecordPause.Image = Properties.Resources.Pause_17Blue;
                stage = 1;

                if (Properties.Settings.Default.STshowCursor) //if show cursor
                {
                    _captureCursorTimer.Enabled = true;
                }
                else
                {
                    _captureTimer.Enabled = true;
                }
            }
        }

        private void Stop()
        {
            //actHook.Stop();
            //actHook.KeyDown -= KeyHookTarget;

            _captureTimer.Stop();
            _captureCursorTimer.Stop();

            if (Properties.Settings.Default.STshowCursor) //If show cursor is true
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
                if (Properties.Settings.Default.STallowEdit)
                {
                    //lastSize = this.Size; //To return back to the last form size after the editor
                    stage = 4;
                    //EditFrames();
                    //flowPanel.Enabled = false;
                }
                else
                {
                    //lastSize = this.Size;
                    //Save();
                }
            }
            else if (stage == 3) // if pre starting
            {
                _preStartTimer.Stop();
                stage = 0;
                numMaxFps.IsEnabled = true;
                btnRecordPause.IsEnabled = true;
                numMaxFps.IsEnabled = true;
                //tbHeight.Enabled = true;
                //tbWidth.Enabled = true;

                this.ShowMaxRestoreButton = true;
                this.ShowMinButton = true;

                btnRecordPause.Content = Properties.Resources.btnRecordPause_Record;
                //btnRecordPause.Image = Properties.Resources.Play_17Green;
                this.Content = Properties.Resources.TitleStoped;

                //actHook.KeyDown += KeyHookTarget;
                //actHook.Start(false, true);
            }

        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            frameAlpha.NavigationService.Navigate(new Settings());
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.ShowMaxRestoreButton = true;
            this.ShowMinButton = true;

            Stop();
        }
    }
}
