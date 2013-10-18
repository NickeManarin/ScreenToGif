namespace GifRecorder
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
            this.timerTela = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.numMaxFps = new System.Windows.Forms.NumericUpDown();
            this.toolStrip1.SuspendLayout();
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
            this.toolStrip1.Location = new System.Drawing.Point(0, 352);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.toolStrip1.Size = new System.Drawing.Size(603, 25);
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
            this.btnPauseRecord.Image = global::GifRecorder.Properties.Resources.record;
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
            this.tbHeight.Enabled = false;
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.Size = new System.Drawing.Size(50, 25);
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
            this.tbWidth.Enabled = false;
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.Size = new System.Drawing.Size(50, 25);
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
            this.btnConfig.Image = global::GifRecorder.Properties.Resources.config;
            this.btnConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Size = new System.Drawing.Size(23, 22);
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
            this.painel.Size = new System.Drawing.Size(603, 352);
            this.painel.TabIndex = 1;
            // 
            // timerTela
            // 
            this.timerTela.Tick += new System.EventHandler(this.timerTela_Tick);
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
            this.numMaxFps.Location = new System.Drawing.Point(276, 354);
            this.numMaxFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxFps.Name = "numMaxFps";
            this.numMaxFps.Size = new System.Drawing.Size(42, 20);
            this.numMaxFps.TabIndex = 4;
            this.numMaxFps.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // Principal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(603, 377);
            this.Controls.Add(this.numMaxFps);
            this.Controls.Add(this.painel);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(266, 164);
            this.Name = "Principal";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Screen";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Principal_FormClosing);
            this.Resize += new System.EventHandler(this.Principal_Resize);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
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
        private System.Windows.Forms.Timer timerTela;
        private System.Windows.Forms.Label lbltopright;
        private System.Windows.Forms.Label lblbottomright;
        private System.Windows.Forms.Label lblleftbottom;
        private System.Windows.Forms.Label lbltopleft;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.NumericUpDown numMaxFps;
        private System.Windows.Forms.ToolStripButton btnInfo;
        private System.Windows.Forms.ToolStripButton btnConfig;
        private System.Windows.Forms.ToolStripButton btnPauseRecord;

    }
}