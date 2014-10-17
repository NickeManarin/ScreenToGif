namespace ScreenToGif.Pages
{
    partial class BorderOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BorderOptions));
            this.pbExample = new System.Windows.Forms.PictureBox();
            this.flowOutline = new System.Windows.Forms.FlowLayoutPanel();
            this.lblColor = new System.Windows.Forms.Label();
            this.pbOutlineColor = new System.Windows.Forms.PictureBox();
            this.lblColorName = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblThick = new System.Windows.Forms.Label();
            this.numThick = new System.Windows.Forms.NumericUpDown();
            this.lblPoints = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            ((System.ComponentModel.ISupportInitialize)(this.pbExample)).BeginInit();
            this.flowOutline.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOutlineColor)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numThick)).BeginInit();
            this.SuspendLayout();
            // 
            // pbExample
            // 
            this.pbExample.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbExample.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbExample.Location = new System.Drawing.Point(9, 9);
            this.pbExample.Margin = new System.Windows.Forms.Padding(0);
            this.pbExample.Name = "pbExample";
            this.pbExample.Size = new System.Drawing.Size(366, 109);
            this.pbExample.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbExample.TabIndex = 15;
            this.pbExample.TabStop = false;
            // 
            // flowOutline
            // 
            this.flowOutline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowOutline.Controls.Add(this.lblColor);
            this.flowOutline.Controls.Add(this.pbOutlineColor);
            this.flowOutline.Controls.Add(this.lblColorName);
            this.flowOutline.Location = new System.Drawing.Point(9, 121);
            this.flowOutline.Name = "flowOutline";
            this.flowOutline.Size = new System.Drawing.Size(366, 28);
            this.flowOutline.TabIndex = 16;
            // 
            // lblColor
            // 
            this.lblColor.AutoSize = true;
            this.lblColor.Location = new System.Drawing.Point(3, 3);
            this.lblColor.Margin = new System.Windows.Forms.Padding(3);
            this.lblColor.Name = "lblColor";
            this.lblColor.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblColor.Size = new System.Drawing.Size(39, 18);
            this.lblColor.TabIndex = 4;
            this.lblColor.Text = "Color:";
            // 
            // pbOutlineColor
            // 
            this.pbOutlineColor.BackColor = System.Drawing.Color.Black;
            this.pbOutlineColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbOutlineColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbOutlineColor.Location = new System.Drawing.Point(48, 3);
            this.pbOutlineColor.Name = "pbOutlineColor";
            this.pbOutlineColor.Size = new System.Drawing.Size(30, 20);
            this.pbOutlineColor.TabIndex = 3;
            this.pbOutlineColor.TabStop = false;
            this.pbOutlineColor.Click += new System.EventHandler(this.pbOutlineColor_Click);
            // 
            // lblColorName
            // 
            this.lblColorName.AutoSize = true;
            this.lblColorName.Location = new System.Drawing.Point(84, 3);
            this.lblColorName.Margin = new System.Windows.Forms.Padding(3);
            this.lblColorName.Name = "lblColorName";
            this.lblColorName.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblColorName.Size = new System.Drawing.Size(16, 18);
            this.lblColorName.TabIndex = 5;
            this.lblColorName.Text = "...";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.lblThick);
            this.flowLayoutPanel1.Controls.Add(this.numThick);
            this.flowLayoutPanel1.Controls.Add(this.lblPoints);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(9, 155);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(366, 28);
            this.flowLayoutPanel1.TabIndex = 17;
            // 
            // lblThick
            // 
            this.lblThick.AutoSize = true;
            this.lblThick.Location = new System.Drawing.Point(3, 3);
            this.lblThick.Margin = new System.Windows.Forms.Padding(3);
            this.lblThick.Name = "lblThick";
            this.lblThick.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblThick.Size = new System.Drawing.Size(62, 18);
            this.lblThick.TabIndex = 0;
            this.lblThick.Text = "Thickness:";
            // 
            // numThick
            // 
            this.numThick.DecimalPlaces = 2;
            this.numThick.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numThick.Location = new System.Drawing.Point(71, 3);
            this.numThick.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numThick.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numThick.Name = "numThick";
            this.numThick.Size = new System.Drawing.Size(58, 23);
            this.numThick.TabIndex = 4;
            this.numThick.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numThick.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numThick.ValueChanged += new System.EventHandler(this.numThick_ValueChanged);
            // 
            // lblPoints
            // 
            this.lblPoints.AutoSize = true;
            this.lblPoints.Location = new System.Drawing.Point(135, 3);
            this.lblPoints.Margin = new System.Windows.Forms.Padding(3);
            this.lblPoints.Name = "lblPoints";
            this.lblPoints.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblPoints.Size = new System.Drawing.Size(43, 18);
            this.lblPoints.TabIndex = 6;
            this.lblPoints.Text = "points.";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(228, 189);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(156, 44);
            this.btnCancel.TabIndex = 19;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.btnOk.CausesValidation = false;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOk.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Image = global::ScreenToGif.Properties.Resources.Done;
            this.btnOk.Location = new System.Drawing.Point(-2, 189);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(230, 44);
            this.btnOk.TabIndex = 18;
            this.btnOk.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOk.UseVisualStyleBackColor = false;
            // 
            // BorderOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(384, 231);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.flowOutline);
            this.Controls.Add(this.pbExample);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(400, 270);
            this.Name = "BorderOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Border Options";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BorderOptions_FormClosing);
            this.Resize += new System.EventHandler(this.BorderOptions_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbExample)).EndInit();
            this.flowOutline.ResumeLayout(false);
            this.flowOutline.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOutlineColor)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numThick)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbExample;
        private System.Windows.Forms.FlowLayoutPanel flowOutline;
        private System.Windows.Forms.PictureBox pbOutlineColor;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label lblThick;
        private System.Windows.Forms.NumericUpDown numThick;
        private System.Windows.Forms.Label lblPoints;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label lblColor;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.Label lblColorName;
    }
}