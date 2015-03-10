using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Enum;
using Point = System.Windows.Point;

namespace ScreenToGif.Windows
{
    public partial class Recorder
    {
        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Flags

        /// <summary>
        /// True if the BackButton should be hidden.
        /// </summary>
        private readonly bool _hideBackButton;

        /// <summary>
        /// Indicates when the user is mouse-clicking.
        /// </summary>
        private bool _recordClicked = false;

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        public Stage Stage { get; set; }

        /// <summary>
        /// The action to be executed after closing this Window.
        /// </summary>
        public ExitAction ExitArg = ExitAction.Return;

        #endregion

        #region Counters

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

        /// <summary>
        /// The numbers of frames, this is updated while recording.
        /// </summary>
        private int _frameCount = 0;

        #endregion

        /// <summary>
        /// Lists of cursors.
        /// </summary>
        public List<FrameInfo> ListFrames = new List<FrameInfo>();

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp = Path.GetTempPath() +
            String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")); //TODO: Change to a more dynamic folder naming.

        /// <summary>
        /// The maximum size of the recording. Also the maximum size of the window.
        /// </summary>
        private Point _sizeScreen = new Point(SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

        /// <summary>
        /// The size of the recording area.
        /// </summary>
        private Size _size;

        /// <summary>
        /// Holds the position of the cursor.
        /// </summary>
        private Point _posCursor;

        private Bitmap _bt;
        private Graphics _gr;

        /// <summary>
        /// Displays a tray icon.
        /// </summary>
        private readonly TrayIcon _trayIcon = new TrayIcon();

        #endregion

        #region Timer

        private Timer _capture = new Timer();

        private Timer _preStartTimer = new Timer();

        #endregion
    }
}
