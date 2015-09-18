using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Controls;
using ScreenToGif.Util;
using ScreenToGif.Util.ActivityHook;
using ScreenToGif.Util.Enum;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for WhiteBoard.xaml
    /// </summary>
    public partial class Board : LightWindow
    {
        #region Variables

        /// <summary>
        /// The object of the keyboard and mouse hooks.
        /// </summary>
        private readonly UserActivityHook _actHook;

        #region Flags

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
        private readonly string _pathTemp = System.IO.Path.GetTempPath() +
            String.Format(@"ScreenToGif\Recording\{0}\", DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")); //TODO: Change to a more dynamic folder naming.

        private Timer _timer = new Timer();

        #endregion

        public Board()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void SizeBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            
        }

        private void HeightTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
