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
            this.lblHotkeys = new System.Windows.Forms.Label();
            this.lblStop = new System.Windows.Forms.Label();
            this.lblStartPause = new System.Windows.Forms.Label();
            this.cbSaveDirectly = new System.Windows.Forms.CheckBox();
            this.cbAllowEdit = new System.Windows.Forms.CheckBox();
            this.labelQuickSettings = new System.Windows.Forms.Label();
            this.cbShowCursor = new System.Windows.Forms.CheckBox();
            this.toolTipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbModernStyle
            // 
            this.cbModernStyle.AutoSize = true;
            this.cbModernStyle.Location = new System.Drawing.Point(17, 102);
            this.cbModernStyle.Name = "cbModernStyle";
            this.cbModernStyle.Size = new System.Drawing.Size(336, 19);
            this.cbModernStyle.TabIndex = 24;
            this.cbModernStyle.Text = global::ScreenToGif.Properties.Resources.CB_ModernStyle;
            this.toolTipHelp.SetToolTip(this.cbModernStyle, "To use the Modern style window, close and open this program");
            this.cbModernStyle.UseVisualStyleBackColor = true;
            this.cbModernStyle.CheckedChanged += new System.EventHandler(this.cbModernStyle_CheckedChanged);
            // 
            // comboStartPauseKey
            // 
            this.comboStartPauseKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
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
            this.comboStartPauseKey.Location = new System.Drawing.Point(150, 22);
            this.comboStartPauseKey.Name = "comboStartPauseKey";
            this.comboStartPauseKey.Size = new System.Drawing.Size(61, 23);
            this.comboStartPauseKey.TabIndex = 23;
            this.comboStartPauseKey.SelectedValueChanged += new System.EventHandler(this.comboStartPauseKey_SelectedValueChanged);
            // 
            // comboStopKey
            // 
            this.comboStopKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
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
            this.comboStopKey.Location = new System.Drawing.Point(150, 49);
            this.comboStopKey.Name = "comboStopKey";
            this.comboStopKey.Size = new System.Drawing.Size(61, 23);
            this.comboStopKey.TabIndex = 22;
            this.comboStopKey.SelectedValueChanged += new System.EventHandler(this.comboStopKey_SelectedValueChanged);
            // 
            // lblHotkeys
            // 
            this.lblHotkeys.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHotkeys.AutoSize = true;
            this.lblHotkeys.Location = new System.Drawing.Point(150, 4);
            this.lblHotkeys.Name = "lblHotkeys";
            this.lblHotkeys.Size = new System.Drawing.Size(61, 15);
            this.lblHotkeys.TabIndex = 21;
            this.lblHotkeys.Text = Resources.Label_Hotkeys;
            this.toolTipHelp.SetToolTip(this.lblHotkeys, "Global Hotkeys to make your life easier");
            // 
            // lblStop
            // 
            this.lblStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStop.AutoSize = true;
            this.lblStop.Location = new System.Drawing.Point(89, 58);
            this.lblStop.Name = "lblStop";
            this.lblStop.Size = new System.Drawing.Size(51, 15);
            this.lblStop.TabIndex = 20;
            this.lblStop.Text = Resources.Label_Stop;
            this.lblStop.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRecordPause
            // 
            this.lblStartPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStartPause.AutoSize = true;
            this.lblStartPause.Location = new System.Drawing.Point(28, 31);
            this.lblStartPause.Name = "lblRecordPause";
            this.lblStartPause.Size = new System.Drawing.Size(112, 15);
            this.lblStartPause.TabIndex = 19;
            this.lblStartPause.Text = Resources.Label_RecordPause;
            this.lblStartPause.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSaveDirectly
            // 
            this.cbSaveDirectly.AutoSize = true;
            this.cbSaveDirectly.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbSaveDirectly.Location = new System.Drawing.Point(17, 77);
            this.cbSaveDirectly.Name = "cbSaveDirectly";
            this.cbSaveDirectly.Size = new System.Drawing.Size(165, 19);
            this.cbSaveDirectly.TabIndex = 18;
            this.cbSaveDirectly.Text = global::ScreenToGif.Properties.Resources.CB_SaveDesktop;
            this.toolTipHelp.SetToolTip(this.cbSaveDirectly, "Saves the Gif file automatically to the Desktop");
            this.cbSaveDirectly.UseVisualStyleBackColor = true;
            this.cbSaveDirectly.CheckedChanged += new System.EventHandler(this.cbSaveDirectly_CheckedChanged);
            // 
            // cbAllowEdit
            // 
            this.cbAllowEdit.AutoSize = true;
            this.cbAllowEdit.Checked = true;
            this.cbAllowEdit.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAllowEdit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbAllowEdit.Location = new System.Drawing.Point(17, 52);
            this.cbAllowEdit.Name = "cbAllowEdit";
            this.cbAllowEdit.Size = new System.Drawing.Size(202, 19);
            this.cbAllowEdit.TabIndex = 17;
            this.cbAllowEdit.Text = global::ScreenToGif.Properties.Resources.CB_AllowEdit;
            this.toolTipHelp.SetToolTip(this.cbAllowEdit, "Opens the Frame Editor after recording");
            this.cbAllowEdit.UseVisualStyleBackColor = true;
            this.cbAllowEdit.CheckedChanged += new System.EventHandler(this.cbAllowEdit_CheckedChanged);
            // 
            // labelQuickSettings
            // 
            this.labelQuickSettings.AutoSize = true;
            this.labelQuickSettings.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelQuickSettings.Location = new System.Drawing.Point(8, 11);
            this.labelQuickSettings.Name = "labelQuickSettings";
            this.labelQuickSettings.Size = new System.Drawing.Size(180, 15);
            this.labelQuickSettings.TabIndex = 16;
            this.labelQuickSettings.Text = Resources.Label_Title_AppSettings;
            this.toolTipHelp.SetToolTip(this.labelQuickSettings, "This settings are saved when you close the program");
            // 
            // cbShowCursor
            // 
            this.cbShowCursor.AutoSize = true;
            this.cbShowCursor.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbShowCursor.Location = new System.Drawing.Point(17, 29);
            this.cbShowCursor.Name = "cbShowCursor";
            this.cbShowCursor.Size = new System.Drawing.Size(100, 19);
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
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.82243F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.17757F));
            this.tableLayoutPanel1.Controls.Add(this.lblStop, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblStartPause, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboStopKey, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.comboStartPauseKey, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblHotkeys, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(315, 174);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 41.30435F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58.69565F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(214, 73);
            this.tableLayoutPanel1.TabIndex = 25;
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // AppSettings
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.cbModernStyle);
            this.Controls.Add(this.cbSaveDirectly);
            this.Controls.Add(this.cbAllowEdit);
            this.Controls.Add(this.labelQuickSettings);
            this.Controls.Add(this.cbShowCursor);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "AppSettings";
            this.Size = new System.Drawing.Size(532, 247);
            this.Tag = "Page";
            this.toolTipHelp.SetToolTip(this, "To close this page, click again in the Gears button");
            this.Load += new System.EventHandler(this.AppSettings_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbModernStyle;
        private System.Windows.Forms.ComboBox comboStartPauseKey;
        private System.Windows.Forms.ComboBox comboStopKey;
        private System.Windows.Forms.Label lblHotkeys;
        private System.Windows.Forms.Label lblStop;
        private System.Windows.Forms.Label lblStartPause;
        private System.Windows.Forms.CheckBox cbSaveDirectly;
        private System.Windows.Forms.CheckBox cbAllowEdit;
        private System.Windows.Forms.Label labelQuickSettings;
        private System.Windows.Forms.CheckBox cbShowCursor;
        private System.Windows.Forms.ToolTip toolTipHelp;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
