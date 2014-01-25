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
            this.pictureCrop = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.doneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureCrop)).BeginInit();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureCrop
            // 
            this.pictureCrop.ContextMenuStrip = this.contextMenu;
            this.pictureCrop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureCrop.Location = new System.Drawing.Point(0, 0);
            this.pictureCrop.Name = "pictureCrop";
            this.pictureCrop.Size = new System.Drawing.Size(284, 111);
            this.pictureCrop.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureCrop.TabIndex = 0;
            this.pictureCrop.TabStop = false;
            this.pictureCrop.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureCrop_MouseDown);
            this.pictureCrop.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureCrop_MouseMove);
            this.pictureCrop.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureCrop_MouseUp);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.doneToolStripMenuItem,
            this.cancelToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(111, 48);
            // 
            // doneToolStripMenuItem
            // 
            this.doneToolStripMenuItem.Name = "doneToolStripMenuItem";
            this.doneToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.doneToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Done;
            this.doneToolStripMenuItem.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.cancelToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Cancel;
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // Crop
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(284, 111);
            this.Controls.Add(this.pictureCrop);
            this.Cursor = System.Windows.Forms.Cursors.Cross;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Crop";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Crop";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pictureCrop)).EndInit();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureCrop;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem doneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
    }
}