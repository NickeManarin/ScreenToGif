using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class Crop
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Crop));
            this.pictureCrop = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.doneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolHelp = new System.Windows.Forms.ToolTip(this.components);
            this.flowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.pbSeparator2 = new System.Windows.Forms.PictureBox();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.lblX = new System.Windows.Forms.Label();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.lblSize = new System.Windows.Forms.Label();
            this.panel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureCrop)).BeginInit();
            this.contextMenu.SuspendLayout();
            this.flowPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator2)).BeginInit();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureCrop
            // 
            this.pictureCrop.ContextMenuStrip = this.contextMenu;
            this.pictureCrop.Cursor = System.Windows.Forms.Cursors.Cross;
            this.pictureCrop.Location = new System.Drawing.Point(0, 0);
            this.pictureCrop.Margin = new System.Windows.Forms.Padding(0);
            this.pictureCrop.Name = "pictureCrop";
            this.pictureCrop.Size = new System.Drawing.Size(138, 46);
            this.pictureCrop.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureCrop.TabIndex = 0;
            this.pictureCrop.TabStop = false;
            this.pictureCrop.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureCrop_MouseDown);
            this.pictureCrop.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureCrop_MouseMove);
            this.pictureCrop.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureCrop_MouseUp);
            // 
            // contextMenu
            // 
            this.contextMenu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.doneToolStripMenuItem,
            this.cancelToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(119, 52);
            // 
            // doneToolStripMenuItem
            // 
            this.doneToolStripMenuItem.ForeColor = System.Drawing.Color.Navy;
            this.doneToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Done;
            this.doneToolStripMenuItem.Name = "doneToolStripMenuItem";
            this.doneToolStripMenuItem.Size = new System.Drawing.Size(118, 24);
            this.doneToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Done;
            this.doneToolStripMenuItem.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.ForeColor = System.Drawing.Color.Maroon;
            this.cancelToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(118, 24);
            this.cancelToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Cancel;
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // toolHelp
            // 
            this.toolHelp.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolHelp.ToolTipTitle = "Help";
            // 
            // flowPanel
            // 
            this.flowPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.flowPanel.Controls.Add(this.btnCancel);
            this.flowPanel.Controls.Add(this.btnDone);
            this.flowPanel.Controls.Add(this.pbSeparator2);
            this.flowPanel.Controls.Add(this.tbHeight);
            this.flowPanel.Controls.Add(this.lblX);
            this.flowPanel.Controls.Add(this.tbWidth);
            this.flowPanel.Controls.Add(this.lblSize);
            this.flowPanel.Cursor = System.Windows.Forms.Cursors.Default;
            this.flowPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowPanel.Location = new System.Drawing.Point(0, 0);
            this.flowPanel.Name = "flowPanel";
            this.flowPanel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.flowPanel.Size = new System.Drawing.Size(318, 31);
            this.flowPanel.TabIndex = 30;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(249, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.btnCancel.Size = new System.Drawing.Size(69, 31);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.TabStop = false;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnDone
            // 
            this.btnDone.AutoSize = true;
            this.btnDone.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDone.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnDone.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.btnDone.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDone.Location = new System.Drawing.Point(188, 0);
            this.btnDone.Margin = new System.Windows.Forms.Padding(0);
            this.btnDone.Name = "btnDone";
            this.btnDone.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.btnDone.Size = new System.Drawing.Size(61, 31);
            this.btnDone.TabIndex = 3;
            this.btnDone.TabStop = false;
            this.btnDone.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnDone.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // pbSeparator2
            // 
            this.pbSeparator2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbSeparator2.BackColor = System.Drawing.Color.Transparent;
            this.pbSeparator2.Image = global::ScreenToGif.Properties.Resources.Separator;
            this.pbSeparator2.Location = new System.Drawing.Point(183, 4);
            this.pbSeparator2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.pbSeparator2.Name = "pbSeparator2";
            this.pbSeparator2.Size = new System.Drawing.Size(2, 24);
            this.pbSeparator2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSeparator2.TabIndex = 39;
            this.pbSeparator2.TabStop = false;
            this.pbSeparator2.Visible = false;
            // 
            // tbHeight
            // 
            this.tbHeight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.tbHeight.Enabled = false;
            this.tbHeight.Location = new System.Drawing.Point(142, 4);
            this.tbHeight.Margin = new System.Windows.Forms.Padding(2, 4, 1, 4);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.ReadOnly = true;
            this.tbHeight.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbHeight.ShortcutsEnabled = false;
            this.tbHeight.Size = new System.Drawing.Size(36, 23);
            this.tbHeight.TabIndex = 40;
            this.tbHeight.Text = "0";
            this.tbHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(127, 8);
            this.lblX.Margin = new System.Windows.Forms.Padding(0, 8, 0, 3);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(14, 15);
            this.lblX.TabIndex = 41;
            this.lblX.Text = "X";
            // 
            // tbWidth
            // 
            this.tbWidth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.tbWidth.Enabled = false;
            this.tbWidth.Location = new System.Drawing.Point(89, 4);
            this.tbWidth.Margin = new System.Windows.Forms.Padding(2, 4, 2, 4);
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.ReadOnly = true;
            this.tbWidth.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbWidth.ShortcutsEnabled = false;
            this.tbWidth.Size = new System.Drawing.Size(36, 23);
            this.tbWidth.TabIndex = 42;
            this.tbWidth.Text = "0";
            this.tbWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblSize
            // 
            this.lblSize.Image = global::ScreenToGif.Properties.Resources.Size;
            this.lblSize.Location = new System.Drawing.Point(64, 7);
            this.lblSize.Margin = new System.Windows.Forms.Padding(1, 7, 1, 3);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(22, 17);
            this.lblSize.TabIndex = 43;
            this.lblSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel
            // 
            this.panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel.AutoSize = true;
            this.panel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel.Controls.Add(this.pictureCrop);
            this.panel.Location = new System.Drawing.Point(0, 37);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(138, 46);
            this.panel.TabIndex = 31;
            // 
            // Crop
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::ScreenToGif.Properties.Resources.grid;
            this.ClientSize = new System.Drawing.Size(318, 154);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.flowPanel);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Crop";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Crop";
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.Crop_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureCrop)).EndInit();
            this.contextMenu.ResumeLayout(false);
            this.flowPanel.ResumeLayout(false);
            this.flowPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator2)).EndInit();
            this.panel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureCrop;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem doneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolHelp;
        private System.Windows.Forms.FlowLayoutPanel flowPanel;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.PictureBox pbSeparator2;
        private System.Windows.Forms.TextBox tbHeight;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.TextBox tbWidth;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.Panel panel;
    }
}