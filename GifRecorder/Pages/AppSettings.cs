using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    public partial class AppSettings : UserControl
    {
        private bool _legacy;
        public AppSettings(bool legacy)
        {
            _legacy = legacy;

            InitializeComponent();

            if (!_legacy)
            {
                cbModernStyle.Text = Resources.CB_LegacyStyle;
            }
        }

        private void AppSettings_Load(object sender, EventArgs e)
        {
            #region Load Save Data

            cbShowCursor.Checked = Settings.Default.STshowCursor;
            cbAllowEdit.Checked = Settings.Default.STallowEdit;
            cbSaveDirectly.Checked = Settings.Default.STsaveLocation;

            if (_legacy)
            {
                cbModernStyle.Checked = Settings.Default.STmodernStyle;
            }
            else
            {
                cbModernStyle.Checked = !Settings.Default.STmodernStyle;
            }
            
            //Gets the Hotkeys
            comboStartPauseKey.Text = Settings.Default.STstartPauseKey.ToString();
            comboStopKey.Text = Settings.Default.STstopKey.ToString();

            #endregion
        }

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
    }
}
