using ScreenToGif.Properties;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TitleFrameSettings));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.tbTitle = new System.Windows.Forms.TextBox();
            this.gbText = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblFontTitle = new System.Windows.Forms.Label();
            this.pbForeColor = new System.Windows.Forms.PictureBox();
            this.btnSelectFont = new System.Windows.Forms.Button();
            this.lblFont = new System.Windows.Forms.LinkLabel();
            this.flowContent = new System.Windows.Forms.TableLayoutPanel();
            this.lblContent = new System.Windows.Forms.Label();
            this.gbBackground = new System.Windows.Forms.GroupBox();
            this.btnBackColor = new System.Windows.Forms.Button();
            this.pbBackground = new System.Windows.Forms.PictureBox();
            this.rbBlured = new System.Windows.Forms.RadioButton();
            this.rbSolidColor = new System.Windows.Forms.RadioButton();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.lblExample = new System.Windows.Forms.Label();
            this.gbText.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbForeColor)).BeginInit();
            this.flowContent.SuspendLayout();
            this.gbBackground.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).BeginInit();
            this.SuspendLayout();
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
            this.btnCancel.Location = new System.Drawing.Point(420, 211);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(190, 41);
            this.btnCancel.TabIndex = 3;
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
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOk.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Image = global::ScreenToGif.Properties.Resources.Done;
            this.btnOk.Location = new System.Drawing.Point(-1, 211);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(421, 41);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOk.UseVisualStyleBackColor = false;
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
            this.tbTitle.Location = new System.Drawing.Point(56, 3);
            this.tbTitle.Multiline = true;
            this.tbTitle.Name = "tbTitle";
            this.tbTitle.Size = new System.Drawing.Size(511, 50);
            this.tbTitle.TabIndex = 0;
            this.tbTitle.TextChanged += new System.EventHandler(this.tbTitle_TextChanged);
            // 
            // gbText
            // 
            this.gbText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbText.Controls.Add(this.tableLayoutPanel1);
            this.gbText.Controls.Add(this.flowContent);
            this.gbText.Location = new System.Drawing.Point(12, 12);
            this.gbText.Name = "gbText";
            this.gbText.Size = new System.Drawing.Size(585, 112);
            this.gbText.TabIndex = 0;
            this.gbText.TabStop = false;
            this.gbText.Text = "Text";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.lblFontTitle, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.pbForeColor, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnSelectFont, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblFont, 2, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(9, 81);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(570, 28);
            this.tableLayoutPanel1.TabIndex = 27;
            // 
            // lblFontTitle
            // 
            this.lblFontTitle.AutoSize = true;
            this.lblFontTitle.Location = new System.Drawing.Point(0, 7);
            this.lblFontTitle.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            this.lblFontTitle.Name = "lblFontTitle";
            this.lblFontTitle.Size = new System.Drawing.Size(52, 15);
            this.lblFontTitle.TabIndex = 20;
            this.lblFontTitle.Text = "      Font:";
            this.lblFontTitle.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pbForeColor
            // 
            this.pbForeColor.BackColor = System.Drawing.Color.Black;
            this.pbForeColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbForeColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbForeColor.Location = new System.Drawing.Point(55, 5);
            this.pbForeColor.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.pbForeColor.Name = "pbForeColor";
            this.pbForeColor.Size = new System.Drawing.Size(30, 20);
            this.pbForeColor.TabIndex = 24;
            this.pbForeColor.TabStop = false;
            this.pbForeColor.Click += new System.EventHandler(this.btnSelectFont_Click);
            this.pbForeColor.MouseHover += new System.EventHandler(this.pbForeColor_MouseHover);
            // 
            // btnSelectFont
            // 
            this.btnSelectFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectFont.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSelectFont.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnSelectFont.FlatAppearance.BorderSize = 0;
            this.btnSelectFont.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnSelectFont.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnSelectFont.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectFont.Location = new System.Drawing.Point(486, 3);
            this.btnSelectFont.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.btnSelectFont.Name = "btnSelectFont";
            this.btnSelectFont.Size = new System.Drawing.Size(84, 23);
            this.btnSelectFont.TabIndex = 1;
            this.btnSelectFont.Text = global::ScreenToGif.Properties.Resources.btnSelect;
            this.btnSelectFont.UseVisualStyleBackColor = true;
            this.btnSelectFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // lblFont
            // 
            this.lblFont.AutoSize = true;
            this.lblFont.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblFont.Location = new System.Drawing.Point(91, 7);
            this.lblFont.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.lblFont.Name = "lblFont";
            this.lblFont.Size = new System.Drawing.Size(16, 15);
            this.lblFont.TabIndex = 25;
            this.lblFont.TabStop = true;
            this.lblFont.Text = "...";
            this.lblFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // flowContent
            // 
            this.flowContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowContent.ColumnCount = 2;
            this.flowContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.flowContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.flowContent.Controls.Add(this.lblContent, 0, 0);
            this.flowContent.Controls.Add(this.tbTitle, 1, 0);
            this.flowContent.Location = new System.Drawing.Point(9, 22);
            this.flowContent.Name = "flowContent";
            this.flowContent.RowCount = 1;
            this.flowContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.flowContent.Size = new System.Drawing.Size(570, 56);
            this.flowContent.TabIndex = 26;
            // 
            // lblContent
            // 
            this.lblContent.AutoSize = true;
            this.lblContent.Location = new System.Drawing.Point(0, 7);
            this.lblContent.Margin = new System.Windows.Forms.Padding(0, 7, 0, 0);
            this.lblContent.Name = "lblContent";
            this.lblContent.Size = new System.Drawing.Size(53, 15);
            this.lblContent.TabIndex = 19;
            this.lblContent.Text = "Content:";
            this.lblContent.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // gbBackground
            // 
            this.gbBackground.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbBackground.Controls.Add(this.btnBackColor);
            this.gbBackground.Controls.Add(this.pbBackground);
            this.gbBackground.Controls.Add(this.rbBlured);
            this.gbBackground.Controls.Add(this.rbSolidColor);
            this.gbBackground.Location = new System.Drawing.Point(12, 130);
            this.gbBackground.Name = "gbBackground";
            this.gbBackground.Size = new System.Drawing.Size(263, 69);
            this.gbBackground.TabIndex = 1;
            this.gbBackground.TabStop = false;
            this.gbBackground.Text = "Background";
            // 
            // btnBackColor
            // 
            this.btnBackColor.AutoSize = true;
            this.btnBackColor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnBackColor.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnBackColor.FlatAppearance.BorderSize = 0;
            this.btnBackColor.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnBackColor.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnBackColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBackColor.Location = new System.Drawing.Point(179, 19);
            this.btnBackColor.Margin = new System.Windows.Forms.Padding(0);
            this.btnBackColor.Name = "btnBackColor";
            this.btnBackColor.Size = new System.Drawing.Size(57, 25);
            this.btnBackColor.TabIndex = 0;
            this.btnBackColor.Text = global::ScreenToGif.Properties.Resources.btnSelect;
            this.btnBackColor.UseVisualStyleBackColor = true;
            this.btnBackColor.Click += new System.EventHandler(this.btnBackColor_Click);
            // 
            // pbBackground
            // 
            this.pbBackground.BackColor = System.Drawing.Color.Black;
            this.pbBackground.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbBackground.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbBackground.Location = new System.Drawing.Point(140, 21);
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
            this.rbBlured.Text = global::ScreenToGif.Properties.Resources.Radio_NextFrame;
            this.tooltip.SetToolTip(this.rbBlured, "This option makes your Title Frame\'s  background using a blured version of the\r\n " +
        "next frame.");
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
            this.rbSolidColor.Text = global::ScreenToGif.Properties.Resources.Radio_SolidColor;
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
            // lblExample
            // 
            this.lblExample.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblExample.BackColor = System.Drawing.Color.Black;
            this.lblExample.Font = new System.Drawing.Font("Segoe UI", 15F);
            this.lblExample.ForeColor = System.Drawing.Color.White;
            this.lblExample.Location = new System.Drawing.Point(281, 130);
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
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(609, 251);
            this.Controls.Add(this.lblExample);
            this.Controls.Add(this.gbBackground);
            this.Controls.Add(this.gbText);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(625, 290);
            this.Name = "TitleFrameSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Title Frame";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TitleFrameSettings_FormClosing);
            this.gbText.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbForeColor)).EndInit();
            this.flowContent.ResumeLayout(false);
            this.flowContent.PerformLayout();
            this.gbBackground.ResumeLayout(false);
            this.gbBackground.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbBackground)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.TextBox tbTitle;
        private System.Windows.Forms.GroupBox gbText;
        private System.Windows.Forms.GroupBox gbBackground;
        private System.Windows.Forms.Label lblContent;
        private System.Windows.Forms.RadioButton rbBlured;
        private System.Windows.Forms.RadioButton rbSolidColor;
        private System.Windows.Forms.PictureBox pbBackground;
        private System.Windows.Forms.Label lblFontTitle;
        private System.Windows.Forms.Button btnBackColor;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.Button btnSelectFont;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.Label lblExample;
        private System.Windows.Forms.PictureBox pbForeColor;
        private System.Windows.Forms.LinkLabel lblFont;
        private System.Windows.Forms.TableLayoutPanel flowContent;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}