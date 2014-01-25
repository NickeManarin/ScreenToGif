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
            this.labelSlow = new System.Windows.Forms.Label();
            this.labelFast = new System.Windows.Forms.Label();
            this.LabelWorst = new System.Windows.Forms.Label();
            this.labelBetter = new System.Windows.Forms.Label();
            this.labelGifSettings = new System.Windows.Forms.Label();
            this.labelCompression = new System.Windows.Forms.Label();
            this.trackBarQuality = new System.Windows.Forms.TrackBar();
            this.radioGif = new System.Windows.Forms.RadioButton();
            this.radioPaint = new System.Windows.Forms.RadioButton();
            this.numRepeatCount = new System.Windows.Forms.NumericUpDown();
            this.cbRepeatForever = new System.Windows.Forms.CheckBox();
            this.lblRepeatCount = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarQuality)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRepeatCount)).BeginInit();
            this.SuspendLayout();
            // 
            // labelQuality
            // 
            this.labelQuality.AutoSize = true;
            this.labelQuality.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelQuality.Location = new System.Drawing.Point(179, 61);
            this.labelQuality.Name = "labelQuality";
            this.labelQuality.Size = new System.Drawing.Size(19, 15);
            this.labelQuality.TabIndex = 26;
            this.labelQuality.Text = "10";
            this.labelQuality.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cbLoop
            // 
            this.cbLoop.AutoSize = true;
            this.cbLoop.Location = new System.Drawing.Point(6, 126);
            this.cbLoop.Name = "cbLoop";
            this.cbLoop.Size = new System.Drawing.Size(84, 19);
            this.cbLoop.TabIndex = 25;
            this.cbLoop.Text = global::ScreenToGif.Properties.Resources.CB_Looped;
            this.cbLoop.UseVisualStyleBackColor = true;
            this.cbLoop.CheckedChanged += new System.EventHandler(this.cbLoop_CheckedChanged);
            // 
            // labelSlow
            // 
            this.labelSlow.AutoSize = true;
            this.labelSlow.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.labelSlow.Location = new System.Drawing.Point(99, 107);
            this.labelSlow.Name = "labelSlow";
            this.labelSlow.Size = new System.Drawing.Size(32, 15);
            this.labelSlow.TabIndex = 24;
            this.labelSlow.Text = "Slow";
            // 
            // labelFast
            // 
            this.labelFast.AutoSize = true;
            this.labelFast.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(210)))));
            this.labelFast.Location = new System.Drawing.Point(240, 107);
            this.labelFast.Name = "labelFast";
            this.labelFast.Size = new System.Drawing.Size(28, 15);
            this.labelFast.TabIndex = 23;
            this.labelFast.Text = "Fast";
            this.labelFast.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelWorst
            // 
            this.LabelWorst.AutoSize = true;
            this.LabelWorst.ForeColor = System.Drawing.Color.DarkGoldenrod;
            this.LabelWorst.Location = new System.Drawing.Point(245, 61);
            this.LabelWorst.Name = "LabelWorst";
            this.LabelWorst.Size = new System.Drawing.Size(38, 15);
            this.LabelWorst.TabIndex = 22;
            this.LabelWorst.Text = "Worst";
            // 
            // labelBetter
            // 
            this.labelBetter.AutoSize = true;
            this.labelBetter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(91)))), ((int)(((byte)(210)))));
            this.labelBetter.Location = new System.Drawing.Point(99, 61);
            this.labelBetter.Name = "labelBetter";
            this.labelBetter.Size = new System.Drawing.Size(38, 15);
            this.labelBetter.TabIndex = 21;
            this.labelBetter.Text = "Better";
            // 
            // labelGifSettings
            // 
            this.labelGifSettings.AutoSize = true;
            this.labelGifSettings.Location = new System.Drawing.Point(3, 5);
            this.labelGifSettings.Name = "labelGifSettings";
            this.labelGifSettings.Size = new System.Drawing.Size(70, 15);
            this.labelGifSettings.TabIndex = 20;
            this.labelGifSettings.Text = "Gif Settings:";
            // 
            // labelCompression
            // 
            this.labelCompression.AutoSize = true;
            this.labelCompression.Location = new System.Drawing.Point(17, 83);
            this.labelCompression.Name = "labelCompression";
            this.labelCompression.Size = new System.Drawing.Size(80, 15);
            this.labelCompression.TabIndex = 19;
            this.labelCompression.Text = "Compression:";
            this.labelCompression.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // trackBarQuality
            // 
            this.trackBarQuality.AutoSize = false;
            this.trackBarQuality.Enabled = false;
            this.trackBarQuality.Location = new System.Drawing.Point(99, 79);
            this.trackBarQuality.Maximum = 20;
            this.trackBarQuality.Minimum = 1;
            this.trackBarQuality.Name = "trackBarQuality";
            this.trackBarQuality.Size = new System.Drawing.Size(190, 25);
            this.trackBarQuality.TabIndex = 18;
            this.trackBarQuality.Value = 10;
            this.trackBarQuality.Scroll += new System.EventHandler(this.trackBarQuality_Scroll);
            this.trackBarQuality.ValueChanged += new System.EventHandler(this.trackBarQuality_ValueChanged);
            // 
            // radioGif
            // 
            this.radioGif.AutoSize = true;
            this.radioGif.Location = new System.Drawing.Point(6, 23);
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
            this.radioPaint.Location = new System.Drawing.Point(204, 23);
            this.radioPaint.Name = "radioPaint";
            this.radioPaint.Size = new System.Drawing.Size(130, 19);
            this.radioPaint.TabIndex = 28;
            this.radioPaint.TabStop = true;
            this.radioPaint.Text = global::ScreenToGif.Properties.Resources.Radio_PaintEncoding;
            this.radioPaint.UseVisualStyleBackColor = true;
            // 
            // numRepeatCount
            // 
            this.numRepeatCount.Location = new System.Drawing.Point(20, 151);
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
            this.cbRepeatForever.Location = new System.Drawing.Point(20, 180);
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
            this.lblRepeatCount.Location = new System.Drawing.Point(64, 153);
            this.lblRepeatCount.Name = "lblRepeatCount";
            this.lblRepeatCount.Size = new System.Drawing.Size(79, 15);
            this.lblRepeatCount.TabIndex = 31;
            this.lblRepeatCount.Text = "Repeat Count";
            // 
            // GifSettings
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.lblRepeatCount);
            this.Controls.Add(this.cbRepeatForever);
            this.Controls.Add(this.numRepeatCount);
            this.Controls.Add(this.radioPaint);
            this.Controls.Add(this.radioGif);
            this.Controls.Add(this.labelQuality);
            this.Controls.Add(this.cbLoop);
            this.Controls.Add(this.labelSlow);
            this.Controls.Add(this.labelFast);
            this.Controls.Add(this.LabelWorst);
            this.Controls.Add(this.labelBetter);
            this.Controls.Add(this.labelGifSettings);
            this.Controls.Add(this.labelCompression);
            this.Controls.Add(this.trackBarQuality);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "GifSettings";
            this.Size = new System.Drawing.Size(532, 247);
            this.Load += new System.EventHandler(this.GifSettings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarQuality)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRepeatCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelQuality;
        private System.Windows.Forms.CheckBox cbLoop;
        private System.Windows.Forms.Label labelSlow;
        private System.Windows.Forms.Label labelFast;
        private System.Windows.Forms.Label LabelWorst;
        private System.Windows.Forms.Label labelBetter;
        private System.Windows.Forms.Label labelGifSettings;
        private System.Windows.Forms.Label labelCompression;
        private System.Windows.Forms.TrackBar trackBarQuality;
        private System.Windows.Forms.RadioButton radioGif;
        private System.Windows.Forms.RadioButton radioPaint;
        private System.Windows.Forms.NumericUpDown numRepeatCount;
        private System.Windows.Forms.CheckBox cbRepeatForever;
        private System.Windows.Forms.Label lblRepeatCount;
    }
}
