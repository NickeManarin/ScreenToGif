using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif.Pages
{
    /// <summary>
    /// Settings page. These settings controls the behavior of the program.
    /// </summary>
    public partial class AppSettings : UserControl
    {
        #region Variables

        /// <summary>
        /// True if Legacy theme, False if Modern theme.
        /// </summary>
        private bool _legacy;

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private readonly string _pathTemp = Path.GetTempPath() + @"ScreenToGif\Recording\";

        /// <summary>
        /// The Path of the Temp folder.
        /// </summary>
        private List<string> _listFolders = new List<string>();

        #endregion

        #region Contructor

        /// <summary>
        /// Constructor of the App settings page.
        /// </summary>
        /// <param name="legacy">True if executing the Legacy page, false if modern.</param>
        public AppSettings(bool legacy)
        {
            _legacy = legacy;

            InitializeComponent();

            #region Localize Labels (thanks to the form designer that resets every label)

            lblStop.Text = Resources.Label_Stop;
            lblStartPause.Text = Resources.Label_RecordPause;
            gbAppSettings.Text = Resources.Label_Title_AppSettings;
            gbHotkeys.Text = Resources.Label_Hotkeys;
            gbLang.Text = Resources.Label_Language;

            #endregion

            if (!_legacy)
            {
                cbModernStyle.Text = Resources.CB_LegacyStyle;
            }
        }

        #endregion

        #region Load

        private void AppSettings_Load(object sender, EventArgs e)
        {
            #region Load Save Data

            #region Generic App Settings

            cbShowCursor.Checked = Settings.Default.showCursor;
            cbShowMouseClicks.Checked = Settings.Default.showMouseClick;
            cbAllowEdit.Checked = Settings.Default.allowEdit;
            cbSaveDirectly.Checked = Settings.Default.saveLocation;

            #region Theme

            if (_legacy)
            {
                cbModernStyle.Checked = Settings.Default.modernStyle;
            }
            else
            {
                cbModernStyle.Checked = !Settings.Default.modernStyle;
            }

            #endregion

            cbPreStart.Checked = Settings.Default.preStart;
            cbShowFinished.Checked = Settings.Default.showFinished;

            #endregion

            #region Hotkeys

            comboStartPauseKey.Text = Settings.Default.startPauseKey.ToString();
            comboStopKey.Text = Settings.Default.stopKey.ToString();

            #endregion

            #region Language

            //string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            switch (Settings.Default.language)
            {
                //I need to use findIndex to set properly
                case "detect":
                    cbLang.Text = "Auto Detect";
                    break;
                case "en":
                    cbLang.Text = "English";
                    break;
                case "es":
                    cbLang.Text = "Spanish";
                    break;
                case "pt":
                    cbLang.Text = "Portuguese";
                    break;
                case "it":
                    cbLang.Text = "Italian";
                    break;
                case "fr":
                    cbLang.Text = "French";
                    break;
                case "el":
                    cbLang.Text = "Greek";
                    break;
                case "ro":
                    cbLang.Text = "Romanian";
                    break;
                case "ru":
                    cbLang.Text = "Russian";
                    break;
                case "zh-cn":
                    cbLang.Text = "Simplified Chinese";
                    break;
                case "zh-tw":
                    cbLang.Text = "Traditional Chinese";
                    break;
                case "sv":
                    cbLang.Text = "Swedish";
                    break;
                case "ta":
                    cbLang.Text = "Tamil";
                    break;
                case "vi":
                    cbLang.Text = "Vietnamese";
                    break;
                case "ja":
                    cbLang.Text = "Japanese";
                    break;
            }

            #endregion

            #endregion

            #region Load Temp Information

            var date = new DateTime();
            _listFolders = Directory.GetDirectories(_pathTemp).Where(x =>
                x.Split('\\').Last().Length == 19 && DateTime.TryParse(x.Split('\\').Last().Substring(0, 10), out date)).ToList();
            
            lblSize.Text = "Folders to Clear: " + _listFolders.Count();

            #endregion
        }

        #endregion

        #region Generic App Settings - Events

        private void cbShowCursor_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.showCursor = cbShowCursor.Checked;

            cbShowMouseClicks.Enabled = cbShowCursor.Checked;
        }

        private void cbShowMouseClicks_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.showMouseClick = cbShowMouseClicks.Checked;
        }

        private void cbAllowEdit_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.allowEdit = cbAllowEdit.Checked;
        }

        private void cbSaveDirectly_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.saveLocation = cbSaveDirectly.Checked;
        }

        private void btnFolder_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.RootFolder = Environment.SpecialFolder.Desktop;

            if (!String.IsNullOrEmpty(Settings.Default.folder))
            {
                fbd.SelectedPath = Settings.Default.folder;
            }
            
            fbd.Description = Resources.Dialog_SaveLocation;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.folder = fbd.SelectedPath;
                Settings.Default.Save();
            }
        }

        private void cbModernStyle_CheckedChanged(object sender, EventArgs e)
        {
            if (_legacy)
            {
                Settings.Default.modernStyle = cbModernStyle.Checked;
            }
            else
            {
                Settings.Default.modernStyle = !cbModernStyle.Checked;
            }

        }

        private void cbPreStart_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.preStart = cbPreStart.Checked;
        }

        private void cbShowFinished_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.showFinished = cbShowFinished.Checked;
        }

        #endregion

        #region Hotkeys - Events

        private void comboStartPauseKey_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStartPauseKey.Text.Equals(comboStopKey.Text))
            {
                comboStartPauseKey.Text = Properties.Settings.Default.startPauseKey.ToString();
            }
            else
            {
                Properties.Settings.Default.startPauseKey = getKeys(comboStartPauseKey.Text);
            }
        }

        private void comboStopKey_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStopKey.Text.Equals(comboStartPauseKey.Text))
            {
                comboStopKey.Text = Properties.Settings.Default.stopKey.ToString();
            }
            else
            {
                Properties.Settings.Default.stopKey = getKeys(comboStopKey.Text);
            }
        }

        private Keys getKeys(string name)
        {
            var keysSelected = new Keys();

            #region Switch Case Keys
            switch (name)
            {
                case "F1":
                    keysSelected = Keys.F1;
                    break;
                case "F2":
                    keysSelected = Keys.F2;
                    break;
                case "F3":
                    keysSelected = Keys.F3;
                    break;
                case "F4":
                    keysSelected = Keys.F4;
                    break;
                case "F5":
                    keysSelected = Keys.F5;
                    break;
                case "F6":
                    keysSelected = Keys.F6;
                    break;
                case "F7":
                    keysSelected = Keys.F7;
                    break;
                case "F8":
                    keysSelected = Keys.F8;
                    break;
                case "F9":
                    keysSelected = Keys.F9;
                    break;
                case "F10":
                    keysSelected = Keys.F10;
                    break;
                case "F11":
                    keysSelected = Keys.F11;
                    break;
                case "F12":
                    keysSelected = Keys.F12;
                    break;
            }
            #endregion

            return keysSelected;
        }

        #endregion

        #region Language - Event

        private void cbLang_SelectionChangeCommitted(object sender, EventArgs e)
        {
            switch (cbLang.Text)
            {
                case "Auto Detect":
                    Settings.Default.language = "detect";
                    break;
                case "English":
                    Settings.Default.language = "en";
                    break;
                case "Spanish":
                    Settings.Default.language = "es";
                    break;
                case "Italian":
                    Settings.Default.language = "it";
                    break;
                case "Portuguese":
                    Settings.Default.language = "pt";
                    break;
                case "French":
                    Settings.Default.language = "fr";
                    break;
                case "Greek":
                    Settings.Default.language = "el";
                    break;
                case "Romanian":
                    Settings.Default.language = "ro";
                    break;
                case "Russian":
                    Settings.Default.language = "ru";
                    break;
                case "Simplified Chinese":
                    Settings.Default.language = "zh-cn";
                    break;
                case "Traditional Chinese":
                    Settings.Default.language = "zh-tw";
                    break;
                case "Swedish":
                    Settings.Default.language = "sv";
                    break;
                case "Tamil":
                    Settings.Default.language = "ta";
                    break;
                case "Vietnamese":
                    Settings.Default.language = "vi";
                    break;
                case "Japanese":
                    Settings.Default.language = "ja";
                    break;
            }

            gbLang.Text = Resources.Label_Language + " (" + Resources.btnRestart + ")";

            Settings.Default.Save();
        }

        #endregion

        #region Restart

        private void btnRestart_Click(object sender, EventArgs e)
        {
            Settings.Default.Save();
            Application.Restart();
        }

        #endregion

        //new
        private void btnClearTemp_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (string folder in _listFolders)
                {
                    //TODO: Detects if there is a STG instance using one of this folders...
                    Directory.Delete(folder, true);
                }

                //TODO: Localize.

                #region Update the Information

                var date = new DateTime();
                _listFolders = Directory.GetDirectories(_pathTemp).Where(x =>
                    x.Split('\\').Last().Length == 19 && DateTime.TryParse(x.Split('\\').Last().Substring(0, 10), out date)).ToList();

                lblSize.Text = "Folders to Clear: " + _listFolders.Count;

                #endregion

                toolTip.ToolTipTitle = "Temp Folder Cleared";
                toolTip.ToolTipIcon = ToolTipIcon.Info;
                toolTip.Show("Clear complete.", btnClearTemp, 0, btnClearTemp.Height, 2500);
            }
            catch (Exception ex)
            {
                LogWriter.Log(ex, "Error while cleaning Temp");

                toolTip.ToolTipTitle = "Error While Cleaning";
                toolTip.ToolTipIcon = ToolTipIcon.Error;
                toolTip.Show(ex.Message, btnClearTemp, 0, btnClearTemp.Height, 3500);
            }
        }

        private void linkOpenFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(_pathTemp);
            }
            catch (Exception ex)
            {
                toolTip.ToolTipTitle = "Error Openning the Temp Folder";
                toolTip.ToolTipIcon = ToolTipIcon.Error;
                toolTip.Show(ex.Message, linkOpenFolder, 0, linkOpenFolder.Height, 3000);

                LogWriter.Log(ex, "Error while trying to open the Temp Folder.");
            }
        }

        private void btnClickProperties_Click(object sender, EventArgs e)
        {
            var click = new ClickProperties();
            click.ShowDialog();
            click.Dispose();

            GC.Collect(1);
        }
    }
}
