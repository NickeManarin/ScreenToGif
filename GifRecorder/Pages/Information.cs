using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif.Pages
{
    public partial class Information : UserControl
    {
        #region Getters/Variables

        private string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 3);
            }
        }

        private string _fileName = "";

        #endregion

        /// <summary>
        /// Initializes the Information Page. This page shows the basic information about the program.
        /// </summary>
        public Information()
        {
            InitializeComponent();

            #region Update Labels

            labelVersion.Text = String.Format(Resources.Label_Version + ": {0}", AssemblyVersion);
            lblLibraries.Text = Resources.Label_Libraries + ":";
            lblAuthor.Text = Resources.Label_Author + ": Nicke S. Manarin";
            tabPage1.Text = Resources.Label_Translations;
            tabPage3.Text = Resources.Label_Devs;
            tabPage2.Text = Resources.Label_Libraries;
            linkReportBug.Text = Resources.Label_Report;
            linkUpdates.Text = Resources.Label_CheckUpdate;

            #endregion
        }

        #region Links

        private void linkNgif_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.codeproject.com/Articles/11505/NGif-Animated-GIF-Encoder-for-NET");
        }

        private void linkBumpkit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/DataDink/Bumpkit");
        }

        private void linkIcon8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.microsoft.com/en-us/download/details.aspx?id=35825&751be11f-ede8-5a0c-058c-2ee190a24fa6=True");
        }

        private void linkCodeplex_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://screentogif.codeplex.com/");
        }

        private void linkSammdon_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/sammdon");
        }

        private void linkBadrfoot_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/badrfoot");
        }

        private void linkFreaksterrao_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/freaksterrao");
        }

        private void linkWebfool_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/WebFooL");
        }

        private void linkThecentury_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/Thecentury");
        }

        private void linkGiorgos_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/Giorgos241");
        }

        private void linkTirzan_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/tirzan");
        }

        private void linkPierre_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/PierreLeLidgeu");
        }

        private void linkInuya_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/inuyashazaq");         
        }

        private void linkJwramz_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/jwramz");       
        }

        private void linkInd01_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/indi01");
        }

        private void linkNarendhar_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/narendhar");
        }

        private void linkNhok35_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/nhok35");
        }

        private void linkKagen_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.codeplex.com/site/users/view/kagen");
        }

        private void linkReportBug_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://screentogif.codeplex.com/workitem/list/basic");
            //We can make our own email sender. It's easy. But we need a webmail account.
        }

        #endregion

        #region License Links

        private void linkCPOL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.codeproject.com/info/cpol10.aspx");
        }

        #endregion

        #region Updates

        private void linkUpdates_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Cursor = Cursors.AppStarting;

            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += webClient_DownloadStringCompleted;
            webClient.DownloadStringAsync(new Uri("https://screentogif.codeplex.com/wikipage?title=Files"));
        }

        private void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Result.Equals(""))
            {
                string info = GetStringInBetween("updateString", "updateString", e.Result, false, false);
                info = info.Replace("<br>", "");
                info = info.Replace("</br>", "");

                char[] splitter = { '|' };
                string[] elements = info.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

                if (elements.Length == 4)
                {
                    //Version|Name|DownloadURL|Description
                    double version = Convert.ToDouble(elements[0], CultureInfo.InvariantCulture);

                    if (version > Convert.ToDouble(AssemblyVersion, CultureInfo.InvariantCulture))
                    {
                        string name = elements[1];
                        string downloadUrl = elements[2];
                        string description = elements[3];

                        DialogResult dialog = MessageBox.Show(this, "New version available, " + name + ", Download?  " + description, name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (dialog == DialogResult.Yes)
                        {
                            var webProgram = new WebClient();
                            webProgram.DownloadFileCompleted += webProgram_DownloadFileCompleted;
                            webProgram.DownloadProgressChanged += webProgram_DownloadProgressChanged;
                            _fileName = Directory.GetCurrentDirectory() + @"\ScreenToGif_" + version + ".zip";

                            if (File.Exists(_fileName))
                            {
                                File.Delete(_fileName);
                            }

                            webProgram.DownloadFileAsync(new Uri(downloadUrl), _fileName);

                            labelPercent.Text = "0 %";
                            labelPercent.Visible = true;
                        }
                    }
                    else
                    {
                        this.Cursor = Cursors.Default;
                        MessageBox.Show(this, "You are up to the date", "No New Release", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }


        }

        private void webProgram_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.Cursor = Cursors.Default;
            try
            {
                //.Net 4.0 don't have native Zip libraries (only the .Net 4.5), so I'll just open the archive.
                Process.Start(_fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error opening the downloaded file: " + ex.Message, "Error While Openning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogWriter.Log(ex, "Opening Downloaded File");
            }

        }

        private void webProgram_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //Updates the progress label.
            labelPercent.Text = e.ProgressPercentage + " %";
        }

        #endregion

        #region Functions

        /// <summary>
        /// Gets the string between given delimiters.
        /// </summary>
        /// <param name="strBegin">The Begin delimiter</param>
        /// <param name="strEnd">The End delimiter</param>
        /// <param name="strSource">String source</param>
        /// <param name="includeBegin">True if should include the begin delimiter in the returned string.</param>
        /// <param name="includeEnd">True if should include the end delimiter in the returned string.</param>
        /// <returns>The string found</returns>
        private static string GetStringInBetween(string strBegin, string strEnd, string strSource, bool includeBegin, bool includeEnd)
        {
            string result = "";
            int iIndexOfBegin = strSource.IndexOf(strBegin);

            if (iIndexOfBegin != -1)
            {
                // include the Begin string if desired  
                if (includeBegin)
                {
                    iIndexOfBegin -= strBegin.Length;
                }

                strSource = strSource.Substring(iIndexOfBegin + strBegin.Length);

                int iEnd = strSource.IndexOf(strEnd);

                if (iEnd != -1)
                {
                    // include the End string if desired  
                    if (includeEnd)
                    {
                        iEnd += strEnd.Length;
                    }

                    result = strSource.Substring(0, iEnd);
                }
            }

            else

                // stay where we are
                result = strSource;

            return result;
        }

        #endregion

    }
}
