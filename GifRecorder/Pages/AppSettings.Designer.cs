using System.Windows.Forms;
using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class AppSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cbModernStyle = new System.Windows.Forms.CheckBox();
            this.comboStartPauseKey = new System.Windows.Forms.ComboBox();
            this.comboStopKey = new System.Windows.Forms.ComboBox();
            this.lblStop = new System.Windows.Forms.Label();
            this.lblStartPause = new System.Windows.Forms.Label();
            this.cbSaveDirectly = new System.Windows.Forms.CheckBox();
            this.cbAllowEdit = new System.Windows.Forms.CheckBox();
            this.cbShowCursor = new System.Windows.Forms.CheckBox();
            this.toolTipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.gbQuickSettings = new System.Windows.Forms.GroupBox();
            this.cbShowFinished = new System.Windows.Forms.CheckBox();
            this.cbPreStart = new System.Windows.Forms.CheckBox();
            this.btnFolder = new System.Windows.Forms.Button();
            this.gbHotkeys = new System.Windows.Forms.GroupBox();
            this.gbLang = new System.Windows.Forms.GroupBox();
            this.cbLang = new System.Windows.Forms.ComboBox();
            this.btnRestart = new System.Windows.Forms.Button();
            this.gbQuickSettings.SuspendLayout();
            this.gbHotkeys.SuspendLayout();
            this.gbLang.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbModernStyle
            // 
            this.cbModernStyle.AutoSize = true;
            this.cbModernStyle.Location = new System.Drawing.Point(6, 90);
            this.cbModernStyle.Name = "cbModernStyle";
            this.cbModernStyle.Size = new System.Drawing.Size(245, 19);
            this.cbModernStyle.TabIndex = 24;
            this.cbModernStyle.Text = global::ScreenToGif.Properties.Resources.CB_ModernStyle;
            this.toolTipHelp.SetToolTip(this.cbModernStyle, "To use the Modern style window, close and open this program");
            this.cbModernStyle.UseVisualStyleBackColor = true;
            this.cbModernStyle.CheckedChanged += new System.EventHandler(this.cbModernStyle_CheckedChanged);
            // 
            // comboStartPauseKey
            // 
            this.comboStartPauseKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStartPauseKey.FormattingEnabled = true;
            this.comboStartPauseKey.Items.AddRange(new object[] {
            "F1",
            "F2",
            "F3",
            "F4",
            "F5",
            "F6",
            "F7",
            "F8",
            "F9",
            "F10",
            "F11",
            "F12"});
            this.comboStartPauseKey.Location = new System.Drawing.Point(6, 22);
            this.comboStartPauseKey.Name = "comboStartPauseKey";
            this.comboStartPauseKey.Size = new System.Drawing.Size(61, 23);
            this.comboStartPauseKey.TabIndex = 23;
            this.comboStartPauseKey.SelectedValueChanged += new System.EventHandler(this.comboStartPauseKey_SelectedValueChanged);
            // 
            // comboStopKey
            // 
            this.comboStopKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboStopKey.FormattingEnabled = true;
            this.comboStopKey.Items.AddRange(new object[] {
            "F1",
            "F2",
            "F3",
            "F4",
            "F5",
            "F6",
            "F7",
            "F8",
            "F9",
            "F10",
            "F11",
            "F12"});
            this.comboStopKey.Location = new System.Drawing.Point(6, 51);
            this.comboStopKey.Name = "comboStopKey";
            this.comboStopKey.Size = new System.Drawing.Size(61, 23);
            this.comboStopKey.TabIndex = 22;
            this.comboStopKey.SelectedValueChanged += new System.EventHandler(this.comboStopKey_SelectedValueChanged);
            // 
            // lblStop
            // 
            this.lblStop.AutoSize = true;
            this.lblStop.Location = new System.Drawing.Point(73, 54);
            this.lblStop.Name = "lblStop";
            this.lblStop.Size = new System.Drawing.Size(31, 15);
            this.lblStop.TabIndex = 20;
            this.lblStop.Text = "Stop";
            this.lblStop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStartPause
            // 
            this.lblStartPause.AutoSize = true;
            this.lblStartPause.Location = new System.Drawing.Point(73, 25);
            this.lblStartPause.Name = "lblStartPause";
            this.lblStartPause.Size = new System.Drawing.Size(80, 15);
            this.lblStartPause.TabIndex = 19;
            this.lblStartPause.Text = "Record/Pause";
            this.lblStartPause.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSaveDirectly
            // 
            this.cbSaveDirectly.AutoSize = true;
            this.cbSaveDirectly.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbSaveDirectly.Location = new System.Drawing.Point(6, 65);
            this.cbSaveDirectly.Name = "cbSaveDirectly";
            this.cbSaveDirectly.Size = new System.Drawing.Size(149, 19);
            this.cbSaveDirectly.TabIndex = 18;
            this.cbSaveDirectly.Text = global::ScreenToGif.Properties.Resources.CB_SaveToFolder;
            this.toolTipHelp.SetToolTip(this.cbSaveDirectly, "Saves the Gif file automatically to a folder of choice");
            this.cbSaveDirectly.UseVisualStyleBackColor = true;
            this.cbSaveDirectly.CheckedChanged += new System.EventHandler(this.cbSaveDirectly_CheckedChanged);
            // 
            // cbAllowEdit
            // 
            this.cbAllowEdit.AutoSize = true;
            this.cbAllowEdit.Checked = true;
            this.cbAllowEdit.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAllowEdit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbAllowEdit.Location = new System.Drawing.Point(6, 40);
            this.cbAllowEdit.Name = "cbAllowEdit";
            this.cbAllowEdit.Size = new System.Drawing.Size(166, 19);
            this.cbAllowEdit.TabIndex = 17;
            this.cbAllowEdit.Text = global::ScreenToGif.Properties.Resources.CB_AllowEdit;
            this.toolTipHelp.SetToolTip(this.cbAllowEdit, "Opens the Frame Editor after recording");
            this.cbAllowEdit.UseVisualStyleBackColor = true;
            this.cbAllowEdit.CheckedChanged += new System.EventHandler(this.cbAllowEdit_CheckedChanged);
            // 
            // cbShowCursor
            // 
            this.cbShowCursor.AutoSize = true;
            this.cbShowCursor.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbShowCursor.Location = new System.Drawing.Point(6, 17);
            this.cbShowCursor.Name = "cbShowCursor";
            this.cbShowCursor.Size = new System.Drawing.Size(91, 19);
            this.cbShowCursor.TabIndex = 15;
            this.cbShowCursor.Text = global::ScreenToGif.Properties.Resources.CB_ShowCursor;
            this.toolTipHelp.SetToolTip(this.cbShowCursor, "Tracks and shows the cursor of the system");
            this.cbShowCursor.UseVisualStyleBackColor = true;
            this.cbShowCursor.CheckedChanged += new System.EventHandler(this.cbShowCursor_CheckedChanged);
            // 
            // toolTipHelp
            // 
            this.toolTipHelp.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTipHelp.ToolTipTitle = global::ScreenToGif.Properties.Resources.Tooltip_Title;
            // 
            // gbQuickSettings
            // 
            this.gbQuickSettings.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.gbQuickSettings.Controls.Add(this.cbShowFinished);
            this.gbQuickSettings.Controls.Add(this.cbPreStart);
            this.gbQuickSettings.Controls.Add(this.btnFolder);
            this.gbQuickSettings.Controls.Add(this.cbShowCursor);
            this.gbQuickSettings.Controls.Add(this.cbModernStyle);
            this.gbQuickSettings.Controls.Add(this.cbAllowEdit);
            this.gbQuickSettings.Controls.Add(this.cbSaveDirectly);
            this.gbQuickSettings.Location = new System.Drawing.Point(3, 3);
            this.gbQuickSettings.Name = "gbQuickSettings";
            this.gbQuickSettings.Size = new System.Drawing.Size(310, 167);
            this.gbQuickSettings.TabIndex = 26;
            this.gbQuickSettings.TabStop = false;
            this.gbQuickSettings.Text = "Quick Settings";
            this.toolTipHelp.SetToolTip(this.gbQuickSettings, global::ScreenToGif.Properties.Resources.Tooltip_AppSettings);
            // 
            // cbShowFinished
            // 
            this.cbShowFinished.AutoSize = true;
            this.cbShowFinished.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbShowFinished.Location = new System.Drawing.Point(6, 140);
            this.cbShowFinished.Name = "cbShowFinished";
            this.cbShowFinished.Size = new System.Drawing.Size(201, 19);
            this.cbShowFinished.TabIndex = 31;
            this.cbShowFinished.Text = global::ScreenToGif.Properties.Resources.CB_ShowFinished;
            this.toolTipHelp.SetToolTip(this.cbShowFinished, "Shows a status page after finishing enconding the gif");
            this.cbShowFinished.UseVisualStyleBackColor = true;
            this.cbShowFinished.CheckedChanged += new System.EventHandler(this.cbShowFinished_CheckedChanged);
            // 
            // cbPreStart
            // 
            this.cbPreStart.AutoSize = true;
            this.cbPreStart.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbPreStart.Location = new System.Drawing.Point(6, 115);
            this.cbPreStart.Name = "cbPreStart";
            this.cbPreStart.Size = new System.Drawing.Size(227, 19);
            this.cbPreStart.TabIndex = 30;
            this.cbPreStart.Text = global::ScreenToGif.Properties.Resources.CB_PrestartCountdown;
            this.toolTipHelp.SetToolTip(this.cbPreStart, "Uses a countdown of 3 seconds before recording");
            this.cbPreStart.UseVisualStyleBackColor = true;
            this.cbPreStart.CheckedChanged += new System.EventHandler(this.cbPreStart_CheckedChanged);
            // 
            // btnFolder
            // 
            this.btnFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnFolder.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnFolder.FlatAppearance.BorderSize = 0;
            this.btnFolder.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnFolder.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFolder.Image = global::ScreenToGif.Properties.Resources.folder;
            this.btnFolder.Location = new System.Drawing.Point(278, 64);
            this.btnFolder.Margin = new System.Windows.Forms.Padding(0);
            this.btnFolder.Name = "btnFolder";
            this.btnFolder.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnFolder.Size = new System.Drawing.Size(29, 19);
            this.btnFolder.TabIndex = 29;
            this.toolTipHelp.SetToolTip(this.btnFolder, "Choose folder");
            this.btnFolder.Click += new System.EventHandler(this.btnFolder_Click);
            // 
            // gbHotkeys
            // 
            this.gbHotkeys.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbHotkeys.Controls.Add(this.lblStartPause);
            this.gbHotkeys.Controls.Add(this.lblStop);
            this.gbHotkeys.Controls.Add(this.comboStartPauseKey);
            this.gbHotkeys.Controls.Add(this.comboStopKey);
            this.gbHotkeys.Location = new System.Drawing.Point(319, 3);
            this.gbHotkeys.Name = "gbHotkeys";
            this.gbHotkeys.Size = new System.Drawing.Size(210, 78);
            this.gbHotkeys.TabIndex = 27;
            this.gbHotkeys.TabStop = false;
            this.gbHotkeys.Text = "Hotkeys";
            this.toolTipHelp.SetToolTip(this.gbHotkeys, "Global Hotkeys to make your life easier.");
            // 
            // gbLang
            // 
            this.gbLang.Controls.Add(this.cbLang);
            this.gbLang.Location = new System.Drawing.Point(3, 176);
            this.gbLang.Name = "gbLang";
            this.gbLang.Size = new System.Drawing.Size(310, 58);
            this.gbLang.TabIndex = 28;
            this.gbLang.TabStop = false;
            this.gbLang.Text = "Language";
            // 
            // cbLang
            // 
            this.cbLang.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLang.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbLang.FormattingEnabled = true;
            this.cbLang.Items.AddRange(new object[] {
            "Auto Detect",
            "English",
            "French",
            "Greek",
            "Italian",
            "Portuguese",
            "Romanian",
            "Russian",
            "Simplified Chinese",
            "Spanish",
            "Swedish",
            "Tamil",
            "Vietnamese"});
            this.cbLang.Location = new System.Drawing.Point(6, 21);
            this.cbLang.Name = "cbLang";
            this.cbLang.Size = new System.Drawing.Size(298, 25);
            this.cbLang.Sorted = true;
            this.cbLang.TabIndex = 0;
            this.cbLang.SelectionChangeCommitted += new System.EventHandler(this.cbLang_SelectionChangeCommitted);
            // 
            // btnRestart
            // 
            this.btnRestart.AutoSize = true;
            this.btnRestart.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRestart.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnRestart.FlatAppearance.BorderSize = 0;
            this.btnRestart.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnRestart.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnRestart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRestart.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.btnRestart.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRestart.Location = new System.Drawing.Point(319, 85);
            this.btnRestart.Margin = new System.Windows.Forms.Padding(0);
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnRestart.Size = new System.Drawing.Size(69, 33);
            this.btnRestart.TabIndex = 1;
            this.btnRestart.Text = global::ScreenToGif.Properties.Resources.btnRestart;
            this.btnRestart.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnRestart.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRestart.UseVisualStyleBackColor = true;
            this.btnRestart.Click += new System.EventHandler(this.btnRestart_Click);
            // 
            // AppSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.btnRestart);
            this.Controls.Add(this.gbLang);
            this.Controls.Add(this.gbHotkeys);
            this.Controls.Add(this.gbQuickSettings);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "AppSettings";
            this.Size = new System.Drawing.Size(532, 237);
            this.Tag = "Page";
            this.toolTipHelp.SetToolTip(this, "To close this page, click again in the Gears button");
            this.Load += new System.EventHandler(this.AppSettings_Load);
            this.gbQuickSettings.ResumeLayout(false);
            this.gbQuickSettings.PerformLayout();
            this.gbHotkeys.ResumeLayout(false);
            this.gbHotkeys.PerformLayout();
            this.gbLang.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbModernStyle;
        private System.Windows.Forms.ComboBox comboStartPauseKey;
        private System.Windows.Forms.ComboBox comboStopKey;
        private System.Windows.Forms.Label lblStop;
        private System.Windows.Forms.Label lblStartPause;
        private System.Windows.Forms.CheckBox cbSaveDirectly;
        private System.Windows.Forms.CheckBox cbAllowEdit;
        private System.Windows.Forms.CheckBox cbShowCursor;
        private System.Windows.Forms.ToolTip toolTipHelp;
        private GroupBox gbQuickSettings;
        private GroupBox gbHotkeys;
        private GroupBox gbLang;
        private ComboBox cbLang;
        private Button btnRestart;
        private Button btnFolder;
        private CheckBox cbPreStart;
        private CheckBox cbShowFinished;
    }
}
