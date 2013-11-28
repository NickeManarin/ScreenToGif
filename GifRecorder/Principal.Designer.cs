namespace ScreenToGif
{
    partial class Principal
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Principal));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnStop = new System.Windows.Forms.ToolStripButton();
            this.btnPauseRecord = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tbHeight = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.tbWidth = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.btnConfig = new System.Windows.Forms.ToolStripButton();
            this.btnInfo = new System.Windows.Forms.ToolStripButton();
            this.painel = new System.Windows.Forms.Panel();
            this.panelConfig = new System.Windows.Forms.Panel();
            this.comboStartPauseKey = new System.Windows.Forms.ComboBox();
            this.comboStopKey = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbFollowPointer = new System.Windows.Forms.CheckBox();
            this.cbSaveDirectly = new System.Windows.Forms.CheckBox();
            this.btnDone = new System.Windows.Forms.Button();
            this.cbAllowEdit = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbShowCursor = new System.Windows.Forms.CheckBox();
            this.cursor = new System.Windows.Forms.PictureBox();
            this.timerCapture = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.numMaxFps = new System.Windows.Forms.NumericUpDown();
            this.PreStart = new System.Windows.Forms.Timer(this.components);
            this.cursorTimer = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1.SuspendLayout();
            this.panelConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cursor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.CanOverflow = false;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnStop,
            this.btnPauseRecord,
            this.toolStripSeparator1,
            this.tbHeight,
            this.toolStripLabel1,
            this.tbWidth,
            this.toolStripLabel2,
            this.toolStripSeparator2,
            this.toolStripLabel3,
            this.btnConfig,
            this.btnInfo});
            this.toolStrip1.Location = new System.Drawing.Point(0, 216);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.toolStrip1.Size = new System.Drawing.Size(430, 25);
            this.toolStrip1.TabIndex = 0;
            // 
            // btnStop
            // 
            this.btnStop.Image = ((System.Drawing.Image)(resources.GetObject("btnStop.Image")));
            this.btnStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStop.Name = "btnStop";
            this.btnStop.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnStop.Size = new System.Drawing.Size(51, 22);
            this.btnStop.Text = "Stop";
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnPauseRecord
            // 
            this.btnPauseRecord.Image = global::ScreenToGif.Properties.Resources.record;
            this.btnPauseRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPauseRecord.Name = "btnPauseRecord";
            this.btnPauseRecord.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnPauseRecord.Size = new System.Drawing.Size(64, 22);
            this.btnPauseRecord.Text = "Record";
            this.btnPauseRecord.Click += new System.EventHandler(this.btnPauseRecord_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.AutoSize = false;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(12, 25);
            // 
            // tbHeight
            // 
            this.tbHeight.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbHeight.Size = new System.Drawing.Size(50, 25);
            this.tbHeight.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbHeight.Leave += new System.EventHandler(this.tbHeight_Leave);
            this.tbHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbHeight_KeyDown);
            this.tbHeight.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbHeight_KeyPress);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(12, 22);
            this.toolStripLabel1.Text = "x";
            // 
            // tbWidth
            // 
            this.tbWidth.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbWidth.Size = new System.Drawing.Size(50, 25);
            this.tbWidth.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbWidth.Leave += new System.EventHandler(this.tbWidth_Leave);
            this.tbWidth.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbWidth_KeyDown);
            this.tbWidth.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbWidth_KeyPress);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(27, 22);
            this.toolStripLabel2.Text = "Size";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.AutoSize = false;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(12, 25);
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStripLabel3.Size = new System.Drawing.Size(99, 22);
            this.toolStripLabel3.Text = "Max FPS                ";
            this.toolStripLabel3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnConfig
            // 
            this.btnConfig.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnConfig.Image = global::ScreenToGif.Properties.Resources.config;
            this.btnConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Size = new System.Drawing.Size(23, 22);
            this.btnConfig.Click += new System.EventHandler(this.btnConfig_Click);
            // 
            // btnInfo
            // 
            this.btnInfo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnInfo.Image = ((System.Drawing.Image)(resources.GetObject("btnInfo.Image")));
            this.btnInfo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Size = new System.Drawing.Size(23, 22);
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
            // 
            // painel
            // 
            this.painel.BackColor = System.Drawing.SystemColors.Control;
            this.painel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.painel.Location = new System.Drawing.Point(0, 0);
            this.painel.Name = "painel";
            this.painel.Size = new System.Drawing.Size(430, 216);
            this.painel.TabIndex = 1;
            // 
            // panelConfig
            // 
            this.panelConfig.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.panelConfig.Controls.Add(this.comboStartPauseKey);
            this.panelConfig.Controls.Add(this.cursor);
            this.panelConfig.Controls.Add(this.comboStopKey);
            this.panelConfig.Controls.Add(this.label4);
            this.panelConfig.Controls.Add(this.label3);
            this.panelConfig.Controls.Add(this.label2);
            this.panelConfig.Controls.Add(this.cbFollowPointer);
            this.panelConfig.Controls.Add(this.cbSaveDirectly);
            this.panelConfig.Controls.Add(this.btnDone);
            this.panelConfig.Controls.Add(this.cbAllowEdit);
            this.panelConfig.Controls.Add(this.label1);
            this.panelConfig.Controls.Add(this.cbShowCursor);
            this.panelConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelConfig.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panelConfig.Location = new System.Drawing.Point(0, 0);
            this.panelConfig.Name = "panelConfig";
            this.panelConfig.Size = new System.Drawing.Size(430, 216);
            this.panelConfig.TabIndex = 1;
            this.panelConfig.Visible = false;
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
            this.comboStartPauseKey.Location = new System.Drawing.Point(357, 150);
            this.comboStartPauseKey.Name = "comboStartPauseKey";
            this.comboStartPauseKey.Size = new System.Drawing.Size(61, 23);
            this.comboStartPauseKey.TabIndex = 13;
            this.comboStartPauseKey.SelectedIndexChanged += new System.EventHandler(this.comboStartPauseKey_SelectedIndexChanged);
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
            this.comboStopKey.Location = new System.Drawing.Point(357, 180);
            this.comboStopKey.Name = "comboStopKey";
            this.comboStopKey.Size = new System.Drawing.Size(61, 23);
            this.comboStopKey.TabIndex = 12;
            this.comboStopKey.SelectedIndexChanged += new System.EventHandler(this.comboStopKey_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(291, 128);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(94, 15);
            this.label4.TabIndex = 11;
            this.label4.Text = "Shortcuts (Beta):";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(317, 183);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 15);
            this.label3.TabIndex = 10;
            this.label3.Text = "Stop:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(281, 153);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 15);
            this.label2.TabIndex = 9;
            this.label2.Text = "Start/Pause:";
            // 
            // cbFollowPointer
            // 
            this.cbFollowPointer.AutoSize = true;
            this.cbFollowPointer.Location = new System.Drawing.Point(12, 100);
            this.cbFollowPointer.Name = "cbFollowPointer";
            this.cbFollowPointer.Size = new System.Drawing.Size(155, 19);
            this.cbFollowPointer.TabIndex = 6;
            this.cbFollowPointer.Text = "Follow the user\'s pointer";
            this.cbFollowPointer.UseVisualStyleBackColor = true;
            this.cbFollowPointer.Visible = false;
            // 
            // cbSaveDirectly
            // 
            this.cbSaveDirectly.AutoSize = true;
            this.cbSaveDirectly.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbSaveDirectly.Location = new System.Drawing.Point(12, 75);
            this.cbSaveDirectly.Name = "cbSaveDirectly";
            this.cbSaveDirectly.Size = new System.Drawing.Size(220, 19);
            this.cbSaveDirectly.TabIndex = 5;
            this.cbSaveDirectly.Text = "Use standard save location (Desktop)";
            this.cbSaveDirectly.UseVisualStyleBackColor = true;
            // 
            // btnDone
            // 
            this.btnDone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDone.Location = new System.Drawing.Point(12, 179);
            this.btnDone.Name = "btnDone";
            this.btnDone.Size = new System.Drawing.Size(75, 23);
            this.btnDone.TabIndex = 3;
            this.btnDone.Text = "Done";
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // cbAllowEdit
            // 
            this.cbAllowEdit.AutoSize = true;
            this.cbAllowEdit.Checked = true;
            this.cbAllowEdit.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAllowEdit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbAllowEdit.Location = new System.Drawing.Point(12, 50);
            this.cbAllowEdit.Name = "cbAllowEdit";
            this.cbAllowEdit.Size = new System.Drawing.Size(177, 19);
            this.cbAllowEdit.TabIndex = 2;
            this.cbAllowEdit.Text = "Allow editing after recording";
            this.cbAllowEdit.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(221, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Quick Configuration (This will be saved):";
            // 
            // cbShowCursor
            // 
            this.cbShowCursor.AutoSize = true;
            this.cbShowCursor.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbShowCursor.Location = new System.Drawing.Point(12, 27);
            this.cbShowCursor.Name = "cbShowCursor";
            this.cbShowCursor.Size = new System.Drawing.Size(91, 19);
            this.cbShowCursor.TabIndex = 0;
            this.cbShowCursor.Text = "Show cursor";
            this.cbShowCursor.UseVisualStyleBackColor = true;
            // 
            // cursor
            // 
            this.cursor.BackColor = System.Drawing.Color.Transparent;
            this.cursor.Image = global::ScreenToGif.Properties.Resources.aero_arrow;
            this.cursor.Location = new System.Drawing.Point(103, 179);
            this.cursor.Name = "cursor";
            this.cursor.Size = new System.Drawing.Size(17, 27);
            this.cursor.TabIndex = 0;
            this.cursor.TabStop = false;
            this.cursor.Visible = false;
            // 
            // timerCapture
            // 
            this.timerCapture.Tick += new System.EventHandler(this.timerCapture_Tick);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "gif";
            this.saveFileDialog.FileName = "Animation";
            this.saveFileDialog.Filter = " Gif files (*.gif)|*.gif|All files (*.*)|*.*";
            // 
            // numMaxFps
            // 
            this.numMaxFps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numMaxFps.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.numMaxFps.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numMaxFps.Location = new System.Drawing.Point(103, 218);
            this.numMaxFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxFps.Name = "numMaxFps";
            this.numMaxFps.Size = new System.Drawing.Size(42, 20);
            this.numMaxFps.TabIndex = 4;
            this.numMaxFps.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // PreStart
            // 
            this.PreStart.Interval = 1000;
            this.PreStart.Tick += new System.EventHandler(this.PreStart_Tick);
            // 
            // cursorTimer
            // 
            this.cursorTimer.Interval = 50;
            this.cursorTimer.Tick += new System.EventHandler(this.cursorTimer_Tick);
            // 
            // Principal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(430, 241);
            this.Controls.Add(this.numMaxFps);
            this.Controls.Add(this.panelConfig);
            this.Controls.Add(this.painel);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(266, 164);
            this.Name = "Principal";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Screen to Gif";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Principal_FormClosing);
            this.Resize += new System.EventHandler(this.Principal_Resize);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panelConfig.ResumeLayout(false);
            this.panelConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cursor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnStop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripTextBox tbHeight;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox tbWidth;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.Panel painel;
        private System.Windows.Forms.Timer timerCapture;
        private System.Windows.Forms.Label lbltopright;
        private System.Windows.Forms.Label lblbottomright;
        private System.Windows.Forms.Label lblleftbottom;
        private System.Windows.Forms.Label lbltopleft;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.NumericUpDown numMaxFps;
        private System.Windows.Forms.ToolStripButton btnInfo;
        private System.Windows.Forms.ToolStripButton btnConfig;
        private System.Windows.Forms.ToolStripButton btnPauseRecord;
        private System.Windows.Forms.Timer PreStart;
        private System.Windows.Forms.PictureBox cursor;
        private System.Windows.Forms.Timer cursorTimer;
        private System.Windows.Forms.Panel panelConfig;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.CheckBox cbAllowEdit;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbShowCursor;
        private System.Windows.Forms.CheckBox cbSaveDirectly;
        private System.Windows.Forms.CheckBox cbFollowPointer;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboStartPauseKey;
        private System.Windows.Forms.ComboBox comboStopKey;

    }
}