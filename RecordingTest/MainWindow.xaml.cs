using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RecordingTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Photo_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            //Exactly like version 1.4.2
            var bt = new Bitmap((int)Width, (int)Height);
            var gr = Graphics.FromImage(bt);
            gr.CopyFromScreen((int)Left, (int)Top ,0,0, new System.Drawing.Size((int)Width, (int)Height));

            bt.Save(System.IO.Path.Combine(fbd.SelectedPath, "Image 1.png"));


            var bt2 = Native.Capture(new System.Windows.Size((int) Width, (int) Height), (int)Left, (int)Top);
            bt2.Save(System.IO.Path.Combine(fbd.SelectedPath, "Image 2.png"));

            //Exactly like version 2.1
        }
    }
}
