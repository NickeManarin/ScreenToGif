using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace ScreenToGif
{
    public partial class Info :Form
    {
        public Info()
        {
            InitializeComponent(); 

            this.labelVersion.Text = String.Format("Version: {0}", AssemblyVersion);
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

        private void linkCodeplex_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("iexplore.exe", "https://screentogif.codeplex.com/");
            }
            catch (Exception)
            {
                MessageBox.Show("Open this: https://screentogif.codeplex.com/");
                throw;
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
    }
}
