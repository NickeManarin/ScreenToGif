using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;

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

            cbShowCursor.Checked = Settings.Default.STshowCursor;
            cbAllowEdit.Checked = Settings.Default.STallowEdit;
            cbSaveDirectly.Checked = Settings.Default.STsaveLocation;

            #region Theme

            if (_legacy)
            {
                cbModernStyle.Checked = Settings.Default.STmodernStyle;
            }
            else
            {
                cbModernStyle.Checked = !Settings.Default.STmodernStyle;
            }

            #endregion

            cbPreStart.Checked = Settings.Default.STpreStart;
            cbShowFinished.Checked = Settings.Default.STshowFinished;

            #endregion

            #region Hotkeys

            comboStartPauseKey.Text = Settings.Default.STstartPauseKey.ToString();
            comboStopKey.Text = Settings.Default.STstopKey.ToString();

            #endregion

            #region Language

            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            switch (Settings.Default.STlanguage)
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
                case "fr":
                    cbLang.Text = "French";
                    break;
                case "el":
                    cbLang.Text = "Greek";
                    break;
                case "ro":
                    cbLang.Text = "Romanian";
                    break;
                case "zh":
                    cbLang.Text = "Simplified Chinese";
                    break;
                case "sv":
                    cbLang.Text = "Swedish";
                    break;
            }

            #endregion

            #endregion
        }

        #endregion

        #region Generic App Settings - Events

        private void cbShowCursor_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.STshowCursor = cbShowCursor.Checked;
        }

        private void cbAllowEdit_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.STallowEdit = cbAllowEdit.Checked;
        }

        private void cbSaveDirectly_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.STsaveLocation = cbSaveDirectly.Checked;
        }

        private void btnFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.ShowNewFolderButton = true;
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = Resources.Dialog_SaveLocation;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                Settings.Default.STfolder = fbd.SelectedPath;
                Settings.Default.Save();
            }
        }

        private void cbModernStyle_CheckedChanged(object sender, EventArgs e)
        {
            if (_legacy)
            {
                Properties.Settings.Default.STmodernStyle = cbModernStyle.Checked;
            }
            else
            {
                Properties.Settings.Default.STmodernStyle = !cbModernStyle.Checked;
            }
            
        }

        private void cbPreStart_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.STpreStart = cbPreStart.Checked;
        }

        private void cbShowFinished_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.STshowFinished = cbShowFinished.Checked;
        }

        #endregion

        #region Hotkeys - Events

        private void comboStartPauseKey_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStartPauseKey.Text.Equals(comboStopKey.Text))
            {
                comboStartPauseKey.Text = Properties.Settings.Default.STstartPauseKey.ToString();
            }
            else
            {
                Properties.Settings.Default.STstartPauseKey = getKeys(comboStartPauseKey.Text);
            }
        }

        private void comboStopKey_SelectedValueChanged(object sender, EventArgs e)
        {
            if (comboStopKey.Text.Equals(comboStartPauseKey.Text))
            {
                comboStopKey.Text = Properties.Settings.Default.STstopKey.ToString();
            }
            else
            {
                Properties.Settings.Default.STstopKey = getKeys(comboStopKey.Text);
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
                    Settings.Default.STlanguage = "detect";
                    break;
                case "English":
                    Settings.Default.STlanguage = "en";
                    break;
                case "Spanish":
                    Settings.Default.STlanguage = "es";
                    break;
                case "Portuguese":
                    Settings.Default.STlanguage = "pt";
                    break;
                case "French":
                    Settings.Default.STlanguage = "fr";
                    break;
                case "Greek":
                    Settings.Default.STlanguage = "el";
                    break;
                case "Romanian":
                    Settings.Default.STlanguage = "ro";
                    break;
                case "Simplified Chinese":
                    Settings.Default.STlanguage = "zh";
                    break;
                case "Swedish":
                    Settings.Default.STlanguage = "sv";
                    break;
            }

            gbLang.Text = "Language " + "(Restart)"; //LOCALIZE 

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
    }
}
