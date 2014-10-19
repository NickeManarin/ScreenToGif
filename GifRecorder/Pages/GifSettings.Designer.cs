using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class GifSettings
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
            this.labelQuality = new System.Windows.Forms.Label();
            this.cbLoop = new System.Windows.Forms.CheckBox();
            this.lblSlow = new System.Windows.Forms.Label();
            this.lblFast = new System.Windows.Forms.Label();
            this.lblWorst = new System.Windows.Forms.Label();
            this.lblBetter = new System.Windows.Forms.Label();
            this.trackBarQuality = new System.Windows.Forms.TrackBar();
            this.radioGif = new System.Windows.Forms.RadioButton();
            this.radioPaint = new System.Windows.Forms.RadioButton();
            this.numRepeatCount = new System.Windows.Forms.NumericUpDown();
            this.cbRepeatForever = new System.Windows.Forms.CheckBox();
            this.lblRepeatCount = new System.Windows.Forms.Label();
            this.gbLoop = new System.Windows.Forms.GroupBox();
            this.gbQuality = new System.Windows.Forms.GroupBox();
            this.gbGifSettings = new System.Windows.Forms.GroupBox();
            this.btnTranspColor = new System.Windows.Forms.Button();
            this.pbTranspColor = new System.Windows.Forms.PictureBox();
            this.cbPaintTransparent = new System.Windows.Forms.CheckBox();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarQuality)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRepeatCount)).BeginInit();
            this.gbLoop.SuspendLayout();
            this.gbQuality.SuspendLayout();
            this.gbGifSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbTranspColor)).BeginInit();
            this.SuspendLayout();
            // 
            // labelQuality
            // 
            this.labelQuality.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.labelQuality.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelQuality.Location = new System.Drawing.Point(120, 31);
            this.labelQuality.Name = "labelQuality";
            this.labelQuality.Size = new System.Drawing.Size(25, 19);
            this.labelQuality.TabIndex = 26;
            this.labelQuality.Text = "10";
            this.labelQuality.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cbLoop
            // 
            this.cbLoop.AutoSize = true;
            this.cbLoop.Image = global::ScreenToGif.Properties.Resources.Loop16x;
            this.cbLoop.Location = new System.Drawing.Point(10, 18);
            this.cbLoop.Name = "cbLoop";
            this.cbLoop.Size = new System.Drawing.Size(100, 19);
            this.cbLoop.TabIndex = 25;
            this.cbLoop.Text = global::ScreenToGif.Properties.Resources.CB_Looped;
            this.cbLoop.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.cbLoop.UseVisualStyleBackColor = true;
            this.cbLoop.CheckedChanged += new System.EventHandler(this.cbLoop_CheckedChanged);
            // 
            // lblSlow
            // 
            this.lblSlow.AutoSize = true;
            this.lblSlow.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.lblSlow.Location = new System.Drawing.Point(6, 38);
            this.lblSlow.Name = "lblSlow";
            this.lblSlow.Size = new System.Drawing.Size(32, 15);
            this.lblSlow.TabIndex = 24;
            this.lblSlow.Text = "Slow";
            // 
            // lblFast
            // 
            this.lblFast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(210)))));
            this.lblFast.Location = new System.Drawing.Point(149, 38);
            this.lblFast.Name = "lblFast";
            this.lblFast.Size = new System.Drawing.Size(123, 15);
            this.lblFast.TabIndex = 23;
            this.lblFast.Text = "Fast";
            this.lblFast.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblWorst
            // 
            this.lblWorst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblWorst.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.lblWorst.Location = new System.Drawing.Point(152, 20);
            this.lblWorst.Name = "lblWorst";
            this.lblWorst.Size = new System.Drawing.Size(120, 15);
            this.lblWorst.TabIndex = 22;
            this.lblWorst.Text = "Worst";
            this.lblWorst.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblBetter
            // 
            this.lblBetter.AutoSize = true;
            this.lblBetter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(210)))));
            this.lblBetter.Location = new System.Drawing.Point(6, 20);
            this.lblBetter.Name = "lblBetter";
            this.lblBetter.Size = new System.Drawing.Size(38, 15);
            this.lblBetter.TabIndex = 21;
            this.lblBetter.Text = "Better";
            // 
            // trackBarQuality
            // 
            this.trackBarQuality.AutoSize = false;
            this.trackBarQuality.Enabled = false;
            this.trackBarQuality.Location = new System.Drawing.Point(7, 56);
            this.trackBarQuality.Maximum = 20;
            this.trackBarQuality.Minimum = 1;
            this.trackBarQuality.Name = "trackBarQuality";
            this.trackBarQuality.Size = new System.Drawing.Size(265, 25);
            this.trackBarQuality.TabIndex = 18;
            this.trackBarQuality.Value = 10;
            this.trackBarQuality.Scroll += new System.EventHandler(this.trackBarQuality_Scroll);
            this.trackBarQuality.ValueChanged += new System.EventHandler(this.trackBarQuality_ValueChanged);
            // 
            // radioGif
            // 
            this.radioGif.AutoSize = true;
            this.radioGif.Location = new System.Drawing.Point(7, 22);
            this.radioGif.Name = "radioGif";
            this.radioGif.Size = new System.Drawing.Size(140, 19);
            this.radioGif.TabIndex = 27;
            this.radioGif.TabStop = true;
            this.radioGif.Text = global::ScreenToGif.Properties.Resources.Radio_CustomEncoding;
            this.radioGif.UseVisualStyleBackColor = true;
            this.radioGif.CheckedChanged += new System.EventHandler(this.radioGif_CheckedChanged);
            // 
            // radioPaint
            // 
            this.radioPaint.AutoSize = true;
            this.radioPaint.Location = new System.Drawing.Point(282, 22);
            this.radioPaint.Name = "radioPaint";
            this.radioPaint.Size = new System.Drawing.Size(130, 19);
            this.radioPaint.TabIndex = 28;
            this.radioPaint.TabStop = true;
            this.radioPaint.Text = global::ScreenToGif.Properties.Resources.Radio_PaintEncoding;
            this.radioPaint.UseVisualStyleBackColor = true;
            // 
            // numRepeatCount
            // 
            this.numRepeatCount.Location = new System.Drawing.Point(24, 43);
            this.numRepeatCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numRepeatCount.Name = "numRepeatCount";
            this.numRepeatCount.Size = new System.Drawing.Size(38, 23);
            this.numRepeatCount.TabIndex = 29;
            this.numRepeatCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cbRepeatForever
            // 
            this.cbRepeatForever.AutoSize = true;
            this.cbRepeatForever.Location = new System.Drawing.Point(24, 72);
            this.cbRepeatForever.Name = "cbRepeatForever";
            this.cbRepeatForever.Size = new System.Drawing.Size(104, 19);
            this.cbRepeatForever.TabIndex = 30;
            this.cbRepeatForever.Text = global::ScreenToGif.Properties.Resources.CB_RepeatForever;
            this.cbRepeatForever.UseVisualStyleBackColor = true;
            this.cbRepeatForever.CheckedChanged += new System.EventHandler(this.cbRepeatForever_CheckedChanged);
            // 
            // lblRepeatCount
            // 
            this.lblRepeatCount.AutoSize = true;
            this.lblRepeatCount.Location = new System.Drawing.Point(68, 45);
            this.lblRepeatCount.Name = "lblRepeatCount";
            this.lblRepeatCount.Size = new System.Drawing.Size(79, 15);
            this.lblRepeatCount.TabIndex = 31;
            this.lblRepeatCount.Text = "Repeat Count";
            // 
            // gbLoop
            // 
            this.gbLoop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbLoop.Controls.Add(this.lblRepeatCount);
            this.gbLoop.Controls.Add(this.cbLoop);
            this.gbLoop.Controls.Add(this.cbRepeatForever);
            this.gbLoop.Controls.Add(this.numRepeatCount);
            this.gbLoop.Location = new System.Drawing.Point(285, 108);
            this.gbLoop.Name = "gbLoop";
            this.gbLoop.Size = new System.Drawing.Size(158, 95);
            this.gbLoop.TabIndex = 32;
            this.gbLoop.TabStop = false;
            this.gbLoop.Text = "Loop";
            // 
            // gbQuality
            // 
            this.gbQuality.Controls.Add(this.trackBarQuality);
            this.gbQuality.Controls.Add(this.lblBetter);
            this.gbQuality.Controls.Add(this.lblWorst);
            this.gbQuality.Controls.Add(this.labelQuality);
            this.gbQuality.Controls.Add(this.lblFast);
            this.gbQuality.Controls.Add(this.lblSlow);
            this.gbQuality.Location = new System.Drawing.Point(3, 108);
            this.gbQuality.Name = "gbQuality";
            this.gbQuality.Size = new System.Drawing.Size(276, 95);
            this.gbQuality.TabIndex = 33;
            this.gbQuality.TabStop = false;
            this.gbQuality.Text = "Quality";
            // 
            // gbGifSettings
            // 
            this.gbGifSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbGifSettings.Controls.Add(this.btnTranspColor);
            this.gbGifSettings.Controls.Add(this.pbTranspColor);
            this.gbGifSettings.Controls.Add(this.cbPaintTransparent);
            this.gbGifSettings.Controls.Add(this.radioPaint);
            this.gbGifSettings.Controls.Add(this.radioGif);
            this.gbGifSettings.Location = new System.Drawing.Point(3, 3);
            this.gbGifSettings.Name = "gbGifSettings";
            this.gbGifSettings.Size = new System.Drawing.Size(440, 99);
            this.gbGifSettings.TabIndex = 34;
            this.gbGifSettings.TabStop = false;
            this.gbGifSettings.Text = "Gif Settings";
            // 
            // btnTranspColor
            // 
            this.btnTranspColor.AutoEllipsis = true;
            this.btnTranspColor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnTranspColor.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnTranspColor.FlatAppearance.BorderSize = 0;
            this.btnTranspColor.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnTranspColor.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnTranspColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTranspColor.Location = new System.Drawing.Point(57, 71);
            this.btnTranspColor.Margin = new System.Windows.Forms.Padding(0);
            this.btnTranspColor.Name = "btnTranspColor";
            this.btnTranspColor.Size = new System.Drawing.Size(219, 22);
            this.btnTranspColor.TabIndex = 36;
            this.btnTranspColor.Text = global::ScreenToGif.Properties.Resources.btnTransparentColor;
            this.btnTranspColor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnTranspColor.UseVisualStyleBackColor = true;
            this.btnTranspColor.Click += new System.EventHandler(this.btnTranspColor_Click);
            // 
            // pbTranspColor
            // 
            this.pbTranspColor.BackColor = System.Drawing.Color.LimeGreen;
            this.pbTranspColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbTranspColor.Location = new System.Drawing.Point(24, 71);
            this.pbTranspColor.Name = "pbTranspColor";
            this.pbTranspColor.Size = new System.Drawing.Size(30, 22);
            this.pbTranspColor.TabIndex = 37;
            this.pbTranspColor.TabStop = false;
            this.pbTranspColor.Click += new System.EventHandler(this.btnTranspColor_Click);
            // 
            // cbPaintTransparent
            // 
            this.cbPaintTransparent.AutoSize = true;
            this.cbPaintTransparent.Enabled = false;
            this.cbPaintTransparent.Location = new System.Drawing.Point(24, 47);
            this.cbPaintTransparent.Name = "cbPaintTransparent";
            this.cbPaintTransparent.Size = new System.Drawing.Size(175, 19);
            this.cbPaintTransparent.TabIndex = 35;
            this.cbPaintTransparent.Text = global::ScreenToGif.Properties.Resources.CB_PaintTransparent;
            this.cbPaintTransparent.UseVisualStyleBackColor = true;
            this.cbPaintTransparent.CheckedChanged += new System.EventHandler(this.cbPaintTransparent_CheckedChanged);
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            this.colorDialog.SolidColorOnly = true;
            // 
            // GifSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.gbGifSettings);
            this.Controls.Add(this.gbQuality);
            this.Controls.Add(this.gbLoop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(446, 207);
            this.Name = "GifSettings";
            this.Size = new System.Drawing.Size(446, 207);
            this.Load += new System.EventHandler(this.GifSettings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarQuality)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRepeatCount)).EndInit();
            this.gbLoop.ResumeLayout(false);
            this.gbLoop.PerformLayout();
            this.gbQuality.ResumeLayout(false);
            this.gbQuality.PerformLayout();
            this.gbGifSettings.ResumeLayout(false);
            this.gbGifSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbTranspColor)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelQuality;
        private System.Windows.Forms.CheckBox cbLoop;
        private System.Windows.Forms.Label lblSlow;
        private System.Windows.Forms.Label lblFast;
        private System.Windows.Forms.Label lblWorst;
        private System.Windows.Forms.Label lblBetter;
        private System.Windows.Forms.TrackBar trackBarQuality;
        private System.Windows.Forms.RadioButton radioGif;
        private System.Windows.Forms.RadioButton radioPaint;
        private System.Windows.Forms.NumericUpDown numRepeatCount;
        private System.Windows.Forms.CheckBox cbRepeatForever;
        private System.Windows.Forms.Label lblRepeatCount;
        private System.Windows.Forms.GroupBox gbLoop;
        private System.Windows.Forms.GroupBox gbQuality;
        private System.Windows.Forms.GroupBox gbGifSettings;
        private System.Windows.Forms.CheckBox cbPaintTransparent;
        private System.Windows.Forms.Button btnTranspColor;
        private System.Windows.Forms.PictureBox pbTranspColor;
        private System.Windows.Forms.ColorDialog colorDialog;
    }
}
