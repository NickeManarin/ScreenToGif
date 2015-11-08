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

        #region Methods

        /// <summary>
        /// Changes the way that the Record and Stop buttons are shown.
        /// </summary>
        private void AutoFitButtons()
        {
            if (LowerGrid.ActualWidth < 250)
            {
                RecordPauseButton.Style = (Style)FindResource("Style.Button.NoText");
                StopButton.Style = RecordPauseButton.Style;

                HideMinimizeAndMaximize(true);
            }
            else
            {
                if (RecordPauseButton.HorizontalContentAlignment != System.Windows.HorizontalAlignment.Center) return;

                RecordPauseButton.Style = (Style)FindResource("Style.Button.Horizontal");
                StopButton.Style = RecordPauseButton.Style;

                HideMinimizeAndMaximize(false);
            }
        }

        #endregion

        #region Sizing

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AdjustToSize();
            }
        }

        private void HeightTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AdjustToSize();
        }

        private void LightWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HeightTextBox.Value = (int)Math.Round(Height, MidpointRounding.AwayFromZero);
            WidthTextBox.Value = (int)Math.Round(Width, MidpointRounding.AwayFromZero);

            AutoFitButtons();
        }

        private void SizeBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var textBox = sender as NumericTextBox;

            if (textBox == null) return;

            textBox.Value = e.Delta > 0 ? textBox.Value + 1 : textBox.Value - 1;

            AdjustToSize();
        }

        private void AdjustToSize()
        {
            HeightTextBox.Value = Convert.ToInt32(HeightTextBox.Text) + 69; //was 65
            WidthTextBox.Value = Convert.ToInt32(WidthTextBox.Text) + 18; //was 16

            Width = WidthTextBox.Value;
            Height = HeightTextBox.Value;
        }

        #endregion

        #region Upper Grid Events

        private void BoardTipColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        #endregion

        #region Buttons

        private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Stage != Stage.Recording && Stage != Stage.PreStarting;
        }

        private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Topmost = false;

            var options = new Options();
            options.ShowDialog(); //TODO: If recording started, maybe disable some properties.

            Topmost = true;
        }


        #endregion
    }
}
