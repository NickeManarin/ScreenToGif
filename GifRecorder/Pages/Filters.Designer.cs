using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class Filters
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
            this.pictureBoxFilter = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.filtersToAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GrayscaleAll = new System.Windows.Forms.ToolStripMenuItem();
            this.PixelateAll = new System.Windows.Forms.ToolStripMenuItem();
            this.BlurAll = new System.Windows.Forms.ToolStripMenuItem();
            this.ColorizeAll = new System.Windows.Forms.ToolStripMenuItem();
            this.NegativeAll = new System.Windows.Forms.ToolStripMenuItem();
            this.TransparencyAll = new System.Windows.Forms.ToolStripMenuItem();
            this.sepiaToneAll = new System.Windows.Forms.ToolStripMenuItem();
            this.filtersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GrayscaleOne = new System.Windows.Forms.ToolStripMenuItem();
            this.PixelateOne = new System.Windows.Forms.ToolStripMenuItem();
            this.BlurOne = new System.Windows.Forms.ToolStripMenuItem();
            this.ColorizeOne = new System.Windows.Forms.ToolStripMenuItem();
            this.NegativeOne = new System.Windows.Forms.ToolStripMenuItem();
            this.TransparencyOne = new System.Windows.Forms.ToolStripMenuItem();
            this.sepiaToneOne = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.doneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.tooltipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.bwBlur = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFilter)).BeginInit();
            this.contextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxFilter
            // 
            this.pictureBoxFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxFilter.ContextMenuStrip = this.contextMenu;
            this.pictureBoxFilter.Location = new System.Drawing.Point(12, 12);
            this.pictureBoxFilter.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.pictureBoxFilter.Name = "pictureBoxFilter";
            this.pictureBoxFilter.Size = new System.Drawing.Size(565, 192);
            this.pictureBoxFilter.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBoxFilter.TabIndex = 0;
            this.pictureBoxFilter.TabStop = false;
            this.tooltipHelp.SetToolTip(this.pictureBoxFilter, Resources.Tooltip_FiltersPage);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetToolStripMenuItem,
            this.filtersToAllToolStripMenuItem,
            this.filtersToolStripMenuItem,
            this.toolStripSeparator1,
            this.doneToolStripMenuItem,
            this.cancelToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(153, 142);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.resetToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Reset;
            this.resetToolStripMenuItem.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
            // 
            // filtersToAllToolStripMenuItem
            // 
            this.filtersToAllToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GrayscaleAll,
            this.PixelateAll,
            this.BlurAll,
            this.ColorizeAll,
            this.NegativeAll,
            this.TransparencyAll,
            this.sepiaToneAll});
            this.filtersToAllToolStripMenuItem.Name = "filtersToAllToolStripMenuItem";
            this.filtersToAllToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.filtersToAllToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_FiltersAll;
            // 
            // GrayscaleAll
            // 
            this.GrayscaleAll.Name = "GrayscaleAll";
            this.GrayscaleAll.Size = new System.Drawing.Size(152, 22);
            this.GrayscaleAll.Text = global::ScreenToGif.Properties.Resources.Con_FiltersGray;
            this.GrayscaleAll.Click += new System.EventHandler(this.GrayscaleAll_Click);
            // 
            // PixelateAll
            // 
            this.PixelateAll.Name = "PixelateAll";
            this.PixelateAll.Size = new System.Drawing.Size(152, 22);
            this.PixelateAll.Text = global::ScreenToGif.Properties.Resources.Con_Filters_Pixelate;
            this.PixelateAll.Click += new System.EventHandler(this.PixelateAll_Click);
            // 
            // BlurAll
            // 
            this.BlurAll.Name = "BlurAll";
            this.BlurAll.Size = new System.Drawing.Size(152, 22);
            this.BlurAll.Text = global::ScreenToGif.Properties.Resources.Con_Blur;
            this.BlurAll.Click += new System.EventHandler(this.BlurAll_Click);
            // 
            // ColorizeAll
            // 
            this.ColorizeAll.Name = "ColorizeAll";
            this.ColorizeAll.Size = new System.Drawing.Size(152, 22);
            this.ColorizeAll.Text = "Colorize";
            this.ColorizeAll.Visible = false;
            // 
            // NegativeAll
            // 
            this.NegativeAll.Name = "NegativeAll";
            this.NegativeAll.Size = new System.Drawing.Size(152, 22);
            this.NegativeAll.Text = global::ScreenToGif.Properties.Resources.Con_Negative;
            this.NegativeAll.Click += new System.EventHandler(this.NegativeAll_Click);
            // 
            // TransparencyAll
            // 
            this.TransparencyAll.Name = "TransparencyAll";
            this.TransparencyAll.Size = new System.Drawing.Size(152, 22);
            this.TransparencyAll.Text = global::ScreenToGif.Properties.Resources.Con_Transparency;
            this.TransparencyAll.Click += new System.EventHandler(this.TransparencyAll_Click);
            // 
            // sepiaToneAll
            // 
            this.sepiaToneAll.Name = "sepiaToneAll";
            this.sepiaToneAll.Size = new System.Drawing.Size(152, 22);
            this.sepiaToneAll.Text = global::ScreenToGif.Properties.Resources.Con_Sepia;
            this.sepiaToneAll.Click += new System.EventHandler(this.sepiaToneAll_Click);
            // 
            // filtersToolStripMenuItem
            // 
            this.filtersToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GrayscaleOne,
            this.PixelateOne,
            this.BlurOne,
            this.ColorizeOne,
            this.NegativeOne,
            this.TransparencyOne,
            this.sepiaToneOne});
            this.filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
            this.filtersToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.filtersToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_FiltersThis;
            // 
            // GrayscaleOne
            // 
            this.GrayscaleOne.Name = "GrayscaleOne";
            this.GrayscaleOne.Size = new System.Drawing.Size(152, 22);
            this.GrayscaleOne.Text = global::ScreenToGif.Properties.Resources.Con_FiltersGray;
            this.GrayscaleOne.Click += new System.EventHandler(this.GrayscaleOne_Click);
            // 
            // PixelateOne
            // 
            this.PixelateOne.Name = "PixelateOne";
            this.PixelateOne.Size = new System.Drawing.Size(152, 22);
            this.PixelateOne.Text = global::ScreenToGif.Properties.Resources.Con_Filters_Pixelate;
            this.PixelateOne.Click += new System.EventHandler(this.PixelateOne_Click);
            // 
            // BlurOne
            // 
            this.BlurOne.Name = "BlurOne";
            this.BlurOne.Size = new System.Drawing.Size(152, 22);
            this.BlurOne.Text = global::ScreenToGif.Properties.Resources.Con_Blur;
            this.BlurOne.Click += new System.EventHandler(this.BlurOne_Click);
            // 
            // ColorizeOne
            // 
            this.ColorizeOne.Name = "ColorizeOne";
            this.ColorizeOne.Size = new System.Drawing.Size(152, 22);
            this.ColorizeOne.Text = "Colorize";
            this.ColorizeOne.Visible = false;
            this.ColorizeOne.Click += new System.EventHandler(this.ColorizeOne_Click);
            // 
            // NegativeOne
            // 
            this.NegativeOne.Name = "NegativeOne";
            this.NegativeOne.Size = new System.Drawing.Size(152, 22);
            this.NegativeOne.Text = global::ScreenToGif.Properties.Resources.Con_Negative;
            this.NegativeOne.Click += new System.EventHandler(this.NegativeOne_Click);
            // 
            // TransparencyOne
            // 
            this.TransparencyOne.Name = "TransparencyOne";
            this.TransparencyOne.Size = new System.Drawing.Size(152, 22);
            this.TransparencyOne.Text = global::ScreenToGif.Properties.Resources.Con_Transparency;
            this.TransparencyOne.Click += new System.EventHandler(this.TransparencyOne_Click);
            // 
            // sepiaToneOne
            // 
            this.sepiaToneOne.Name = "sepiaToneOne";
            this.sepiaToneOne.Size = new System.Drawing.Size(152, 22);
            this.sepiaToneOne.Text = global::ScreenToGif.Properties.Resources.Con_Sepia;
            this.sepiaToneOne.Click += new System.EventHandler(this.sepiaToneOne_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // doneToolStripMenuItem
            // 
            this.doneToolStripMenuItem.Name = "doneToolStripMenuItem";
            this.doneToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.doneToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Done;
            this.doneToolStripMenuItem.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.cancelToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Cancel;
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // trackBar
            // 
            this.trackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar.AutoSize = false;
            this.trackBar.Location = new System.Drawing.Point(12, 207);
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(565, 27);
            this.trackBar.TabIndex = 3;
            this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
            // 
            // tooltipHelp
            // 
            this.tooltipHelp.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.tooltipHelp.ToolTipTitle = global::ScreenToGif.Properties.Resources.Tooltip_Title;
            // 
            // Filters
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(589, 246);
            this.Controls.Add(this.trackBar);
            this.Controls.Add(this.pictureBoxFilter);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Filters";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = global::ScreenToGif.Properties.Resources.Title_Filters;
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFilter)).EndInit();
            this.contextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxFilter;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.TrackBar trackBar;
        private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filtersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GrayscaleOne;
        private System.Windows.Forms.ToolStripMenuItem PixelateOne;
        private System.Windows.Forms.ToolStripMenuItem BlurOne;
        private System.Windows.Forms.ToolStripMenuItem ColorizeOne;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem doneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filtersToAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GrayscaleAll;
        private System.Windows.Forms.ToolStripMenuItem PixelateAll;
        private System.Windows.Forms.ToolStripMenuItem BlurAll;
        private System.Windows.Forms.ToolStripMenuItem ColorizeAll;
        private System.Windows.Forms.ToolStripMenuItem NegativeOne;
        private System.Windows.Forms.ToolStripMenuItem NegativeAll;
        private System.Windows.Forms.ToolTip tooltipHelp;
        private System.ComponentModel.BackgroundWorker bwBlur;
        private System.Windows.Forms.ToolStripMenuItem TransparencyOne;
        private System.Windows.Forms.ToolStripMenuItem TransparencyAll;
        private System.Windows.Forms.ToolStripMenuItem sepiaToneAll;
        private System.Windows.Forms.ToolStripMenuItem sepiaToneOne;
    }
}