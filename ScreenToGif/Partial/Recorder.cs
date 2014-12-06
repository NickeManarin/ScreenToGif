using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.Enum;

// ReSharper disable once CheckNamespace
namespace ScreenToGif.Windows
{
    public partial class Recorder
    {
        #region Variables

        /// <summary>
        /// The amount of seconds of the pre start delay, plus 1 (1+1=2);
        /// </summary>
        private int _preStartCount = 1;

        /// <summary>
        /// Lists of frames as file names.
        /// </summary>
        List<string> _listFrames = new List<string>();

        /// <summary>
        /// Lists of cursors.
        /// </summary>
        List<CursorInfo> _listCursor = new List<CursorInfo>();

        /// <summary>
        /// The actual stage of the program.
        /// </summary>
        private Stage _stage = Stage.Stopped;

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp = Path.GetTempPath() +
            String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")); //TODO: Change to a more dynamic folder naming.
        
        private Bitmap _bt;
        private Graphics _gr;

        /// <summary>
        /// The numbers of frames, this is updated while recording.
        /// </summary>
        private int _frameCount = 0;

        #endregion

        #region Timer

        System.Windows.Forms.Timer _capture = new System.Windows.Forms.Timer();

        #endregion
    }
}
