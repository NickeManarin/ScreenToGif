using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class Resize
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
            this.pbImage = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.resetSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.freeAspectRatioToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.doneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).BeginInit();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbImage
            // 
            this.pbImage.ContextMenuStrip = this.contextMenu;
            this.pbImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbImage.Location = new System.Drawing.Point(0, 0);
            this.pbImage.Name = "pbImage";
            this.pbImage.Size = new System.Drawing.Size(467, 235);
            this.pbImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbImage.TabIndex = 0;
            this.pbImage.TabStop = false;
            this.pbImage.Click += new System.EventHandler(this.pbImage_Click);
            // 
            // contextMenu
            // 
            this.contextMenu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetSizeToolStripMenuItem,
            this.freeAspectRatioToolStripMenuItem,
            this.toolStripSeparator1,
            this.doneToolStripMenuItem,
            this.cancelToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(185, 106);
            // 
            // resetSizeToolStripMenuItem
            // 
            this.resetSizeToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.resetSizeToolStripMenuItem.Name = "resetSizeToolStripMenuItem";
            this.resetSizeToolStripMenuItem.Size = new System.Drawing.Size(184, 24);
            this.resetSizeToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_ResetSize;
            this.resetSizeToolStripMenuItem.Click += new System.EventHandler(this.resetSizeToolStripMenuItem_Click);
            // 
            // freeAspectRatioToolStripMenuItem
            // 
            this.freeAspectRatioToolStripMenuItem.CheckOnClick = true;
            this.freeAspectRatioToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.aspectRatio;
            this.freeAspectRatioToolStripMenuItem.Name = "freeAspectRatioToolStripMenuItem";
            this.freeAspectRatioToolStripMenuItem.Size = new System.Drawing.Size(184, 24);
            this.freeAspectRatioToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_FreeAspectRatio;
            this.freeAspectRatioToolStripMenuItem.CheckedChanged += new System.EventHandler(this.freeAspectRatioToolStripMenuItem_CheckedChanged);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(181, 6);
            // 
            // doneToolStripMenuItem
            // 
            this.doneToolStripMenuItem.ForeColor = System.Drawing.Color.Navy;
            this.doneToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.doneToolStripMenuItem.Name = "doneToolStripMenuItem";
            this.doneToolStripMenuItem.Size = new System.Drawing.Size(184, 24);
            this.doneToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Done;
            this.doneToolStripMenuItem.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.cancelToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(184, 24);
            this.cancelToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Cancel;
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // Resize
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(467, 235);
            this.Controls.Add(this.pbImage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(50, 50);
            this.Name = "Resize";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Resources.Title_ResizeAllFrames;
            this.TopMost = true;
            this.Resize += new System.EventHandler(this.Resize_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).EndInit();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbImage;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem resetSizeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem freeAspectRatioToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem doneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
    }
}