using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class InsertText
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.tooltip = new System.Windows.Forms.ToolTip(this.components);
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.lblFont = new System.Windows.Forms.LinkLabel();
            this.pbForeColor = new System.Windows.Forms.PictureBox();
            this.btnSelectFont = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbContent = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbForeColor)).BeginInit();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.btnOk.CausesValidation = false;
            this.btnOk.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOk.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Image = global::ScreenToGif.Properties.Resources.Done;
            this.btnOk.Location = new System.Drawing.Point(-2, 143);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(360, 44);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
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
            this.btnCancel.Location = new System.Drawing.Point(358, 143);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(208, 44);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            this.colorDialog.SolidColorOnly = true;
            // 
            // fontDialog
            // 
            this.fontDialog.Font = new System.Drawing.Font("Segoe UI", 15F);
            this.fontDialog.MaxSize = 72;
            this.fontDialog.MinSize = 5;
            this.fontDialog.ShowColor = true;
            // 
            // lblFont
            // 
            this.lblFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblFont.AutoSize = true;
            this.lblFont.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblFont.Location = new System.Drawing.Point(123, 111);
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
            this.pbForeColor.Location = new System.Drawing.Point(87, 109);
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
            this.btnSelectFont.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSelectFont.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnSelectFont.FlatAppearance.BorderSize = 0;
            this.btnSelectFont.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnSelectFont.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnSelectFont.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectFont.Location = new System.Drawing.Point(469, 109);
            this.btnSelectFont.Margin = new System.Windows.Forms.Padding(0);
            this.btnSelectFont.Name = "btnSelectFont";
            this.btnSelectFont.Size = new System.Drawing.Size(83, 25);
            this.btnSelectFont.TabIndex = 1;
            this.btnSelectFont.Text = global::ScreenToGif.Properties.Resources.btnSelect;
            this.btnSelectFont.UseVisualStyleBackColor = true;
            this.btnSelectFont.Click += new System.EventHandler(this.btnSelectFont_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(12, 109);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 18);
            this.label2.TabIndex = 20;
            this.label2.Text = Resources.Label_Font;
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(12, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 19);
            this.label3.TabIndex = 19;
            this.label3.Text = Resources.Label_Content;
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbContent
            // 
            this.tbContent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbContent.ForeColor = System.Drawing.Color.Black;
            this.tbContent.Location = new System.Drawing.Point(87, 12);
            this.tbContent.Multiline = true;
            this.tbContent.Name = "tbContent";
            this.tbContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbContent.Size = new System.Drawing.Size(465, 91);
            this.tbContent.TabIndex = 0;
            // 
            // InsertText
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(564, 185);
            this.Controls.Add(this.lblFont);
            this.Controls.Add(this.pbForeColor);
            this.Controls.Add(this.btnSelectFont);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbContent);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(500, 200);
            this.Name = "InsertText";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Resources.Title_InsertText;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.InsertText_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbForeColor)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.LinkLabel lblFont;
        private System.Windows.Forms.PictureBox pbForeColor;
        private System.Windows.Forms.Button btnSelectFont;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbContent;

    }
}