namespace ScreenToGif.Pages
{
    partial class TitleFrameSettings
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.tbTitle = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblFont = new System.Windows.Forms.LinkLabel();
            this.pbForeColor = new System.Windows.Forms.PictureBox();
            this.btnSelectFont = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnBackColor = new System.Windows.Forms.Button();
            this.pbBackground = new System.Windows.Forms.PictureBox();
            this.rbBlured = new System.Windows.Forms.RadioButton();
            this.rbSolidColor = new System.Windows.Forms.RadioButton();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.pbExample = new System.Windows.Forms.PictureBox();
            this.lblExample = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbForeColor)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbExample)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(455, 218);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(190, 41);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOk.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Image = global::ScreenToGif.Properties.Resources.Done;
            this.btnOk.Location = new System.Drawing.Point(-1, 218);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(456, 41);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            this.colorDialog.SolidColorOnly = true;
            // 
            // tbTitle
            // 
            this.tbTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTitle.Location = new System.Drawing.Point(65, 16);
            this.tbTitle.Multiline = true;
            this.tbTitle.Name = "tbTitle";
            this.tbTitle.Size = new System.Drawing.Size(549, 69);
            this.tbTitle.TabIndex = 0;
            this.tbTitle.TextChanged += new System.EventHandler(this.tbTitle_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.lblFont);
            this.groupBox1.Controls.Add(this.pbForeColor);
            this.groupBox1.Controls.Add(this.btnSelectFont);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbTitle);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(620, 119);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Text";
            // 
            // lblFont
            // 
            this.lblFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblFont.AutoSize = true;
            this.lblFont.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblFont.Location = new System.Drawing.Point(101, 93);
            this.lblFont.Name = "lblFont";
            this.lblFont.Size = new System.Drawing.Size(16, 15);
            this.lblFont.TabIndex = 25;
            this.lblFont.TabStop = true;
            this.lblFont.Text = "...";
            this.lblFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // pbForeColor
            // 
            this.pbForeColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pbForeColor.BackColor = System.Drawing.Color.Black;
            this.pbForeColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbForeColor.Location = new System.Drawing.Point(65, 91);
            this.pbForeColor.Name = "pbForeColor";
            this.pbForeColor.Size = new System.Drawing.Size(30, 20);
            this.pbForeColor.TabIndex = 24;
            this.pbForeColor.TabStop = false;
            this.pbForeColor.Click += new System.EventHandler(this.btnSelectFont_Click);
            this.pbForeColor.MouseHover += new System.EventHandler(this.pbForeColor_MouseHover);
            // 
            // btnSelectFont
            // 
            this.btnSelectFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectFont.AutoSize = true;
            this.btnSelectFont.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSelectFont.FlatAppearance.BorderSize = 0;
            this.btnSelectFont.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnSelectFont.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnSelectFont.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectFont.Location = new System.Drawing.Point(557, 88);
            this.btnSelectFont.Margin = new System.Windows.Forms.Padding(0);
            this.btnSelectFont.Name = "btnSelectFont";
            this.btnSelectFont.Size = new System.Drawing.Size(57, 25);
            this.btnSelectFont.TabIndex = 1;
            this.btnSelectFont.Text = "Select...";
            this.btnSelectFont.UseVisualStyleBackColor = true;
            this.btnSelectFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 15);
            this.label2.TabIndex = 20;
            this.label2.Text = "Font:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 15);
            this.label1.TabIndex = 19;
            this.label1.Text = "Content:";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnBackColor);
            this.groupBox2.Controls.Add(this.pbBackground);
            this.groupBox2.Controls.Add(this.rbBlured);
            this.groupBox2.Controls.Add(this.rbSolidColor);
            this.groupBox2.Location = new System.Drawing.Point(12, 137);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(298, 69);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Background";
            // 
            // btnBackColor
            // 
            this.btnBackColor.AutoSize = true;
            this.btnBackColor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnBackColor.FlatAppearance.BorderSize = 0;
            this.btnBackColor.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnBackColor.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnBackColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackColor.Location = new System.Drawing.Point(146, 19);
            this.btnBackColor.Margin = new System.Windows.Forms.Padding(0);
            this.btnBackColor.Name = "btnBackColor";
            this.btnBackColor.Size = new System.Drawing.Size(57, 25);
            this.btnBackColor.TabIndex = 0;
            this.btnBackColor.Text = "Select...";
            this.btnBackColor.UseVisualStyleBackColor = true;
            this.btnBackColor.Click += new System.EventHandler(this.btnBackColor_Click);
            // 
            // pbBackground
            // 
            this.pbBackground.BackColor = System.Drawing.Color.Black;
            this.pbBackground.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbBackground.Location = new System.Drawing.Point(113, 21);
            this.pbBackground.Name = "pbBackground";
            this.pbBackground.Size = new System.Drawing.Size(30, 20);
            this.pbBackground.TabIndex = 2;
            this.pbBackground.TabStop = false;
            this.pbBackground.Click += new System.EventHandler(this.btnBackColor_Click);
            this.pbBackground.MouseHover += new System.EventHandler(this.pbBackground_MouseHover);
            // 
            // rbBlured
            // 
            this.rbBlured.AutoSize = true;
            this.rbBlured.Location = new System.Drawing.Point(9, 47);
            this.rbBlured.Name = "rbBlured";
            this.rbBlured.Size = new System.Drawing.Size(161, 19);
            this.rbBlured.TabIndex = 1;
            this.rbBlured.Text = "Next Frame (Blured Copy)";
            this.rbBlured.UseVisualStyleBackColor = true;
            // 
            // rbSolidColor
            // 
            this.rbSolidColor.AutoSize = true;
            this.rbSolidColor.Checked = true;
            this.rbSolidColor.Location = new System.Drawing.Point(9, 22);
            this.rbSolidColor.Name = "rbSolidColor";
            this.rbSolidColor.Size = new System.Drawing.Size(83, 19);
            this.rbSolidColor.TabIndex = 0;
            this.rbSolidColor.TabStop = true;
            this.rbSolidColor.Text = "Solid Color";
            this.rbSolidColor.UseVisualStyleBackColor = true;
            this.rbSolidColor.CheckedChanged += new System.EventHandler(this.rbSolidColor_CheckedChanged);
            // 
            // fontDialog
            // 
            this.fontDialog.Color = System.Drawing.Color.White;
            this.fontDialog.Font = new System.Drawing.Font("Segoe UI", 15F);
            this.fontDialog.MaxSize = 72;
            this.fontDialog.MinSize = 5;
            this.fontDialog.ShowColor = true;
            // 
            // pbExample
            // 
            this.pbExample.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pbExample.BackColor = System.Drawing.Color.Black;
            this.pbExample.Location = new System.Drawing.Point(316, 137);
            this.pbExample.Name = "pbExample";
            this.pbExample.Size = new System.Drawing.Size(316, 69);
            this.pbExample.TabIndex = 21;
            this.pbExample.TabStop = false;
            // 
            // lblExample
            // 
            this.lblExample.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblExample.BackColor = System.Drawing.Color.Black;
            this.lblExample.Font = new System.Drawing.Font("Segoe UI", 15F);
            this.lblExample.ForeColor = System.Drawing.Color.White;
            this.lblExample.Location = new System.Drawing.Point(316, 137);
            this.lblExample.Name = "lblExample";
            this.lblExample.Size = new System.Drawing.Size(316, 69);
            this.lblExample.TabIndex = 22;
            this.lblExample.Text = "Example";
            this.lblExample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TitleFrameSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.ClientSize = new System.Drawing.Size(644, 258);
            this.Controls.Add(this.lblExample);
            this.Controls.Add(this.pbExample);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(580, 260);
            this.Name = "TitleFrameSettings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Title Frame";
            this.TopMost = true;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbForeColor)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbExample)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.TextBox tbTitle;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbBlured;
        private System.Windows.Forms.RadioButton rbSolidColor;
        private System.Windows.Forms.PictureBox pbBackground;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnBackColor;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.Button btnSelectFont;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.PictureBox pbExample;
        private System.Windows.Forms.Label lblExample;
        private System.Windows.Forms.PictureBox pbForeColor;
        private System.Windows.Forms.LinkLabel lblFont;
    }
}