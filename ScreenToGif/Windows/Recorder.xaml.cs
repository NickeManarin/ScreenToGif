using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenToGif.Controls.LightWindow;

namespace ScreenToGif.Windows
{
    /// <summary>
    /// Interaction logic for Recorder.xaml
    /// </summary>
    public partial class Recorder : LightWindow
    {
        //TODO:
        //NumericUpDown: http://stackoverflow.com/questions/841293/where-is-the-wpf-numeric-updown-control
        //OuterGlow: http://windowglows.codeplex.com/
        //Maximizing Window with style "None": http://blogs.msdn.com/b/llobo/archive/2006/08/01/maximizing-window-_2800_with-windowstyle_3d00_none_2900_-considering-taskbar.aspx

        #region Variables



        #endregion

        public Recorder()
        {
            InitializeComponent();
        }

        private void RecordPause_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
