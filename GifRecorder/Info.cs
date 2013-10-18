using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifRecorder
{
    public partial class Info :Form
    {
        public Info()
        {
            InitializeComponent();
        }

        private void link1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("iexplore.exe", "http://www.codeproject.com/Articles/11505/NGif-Animated-GIF-Encoder-for-NET");
            }
            catch (Exception)
            {
                MessageBox.Show("Open this: http://www.codeproject.com/Articles/11505/NGif-Animated-GIF-Encoder-for-NET");
            }
        }

        private void link2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("iexplore.exe", "http://icons8.com/download-huge-windows8-set/");
            }
            catch (Exception)
            {
                MessageBox.Show("Open this: http://icons8.com/download-huge-windows8-set/");
                throw;
            }
        }
    }
}
