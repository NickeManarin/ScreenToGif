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
            this.NegativeAll = new System.Windows.Forms.ToolStripMenuItem();
            this.TransparencyAll = new System.Windows.Forms.ToolStripMenuItem();
            this.sepiaToneAll = new System.Windows.Forms.ToolStripMenuItem();
            this.filtersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GrayscaleOne = new System.Windows.Forms.ToolStripMenuItem();
            this.PixelateOne = new System.Windows.Forms.ToolStripMenuItem();
            this.BlurOne = new System.Windows.Forms.ToolStripMenuItem();
            this.NegativeOne = new System.Windows.Forms.ToolStripMenuItem();
            this.TransparencyOne = new System.Windows.Forms.ToolStripMenuItem();
            this.sepiaToneOne = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.doneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.tooltipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnFilters = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFilter)).BeginInit();
            this.contextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBoxFilter
            // 
            this.pictureBoxFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxFilter.ContextMenuStrip = this.contextMenu;
            this.pictureBoxFilter.Location = new System.Drawing.Point(12, 43);
            this.pictureBoxFilter.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.pictureBoxFilter.Name = "pictureBoxFilter";
            this.pictureBoxFilter.Size = new System.Drawing.Size(565, 161);
            this.pictureBoxFilter.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBoxFilter.TabIndex = 0;
            this.pictureBoxFilter.TabStop = false;
            this.tooltipHelp.SetToolTip(this.pictureBoxFilter, global::ScreenToGif.Properties.Resources.Tooltip_FiltersPage);
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
            this.contextMenu.Size = new System.Drawing.Size(134, 120);
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.resetToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Reset;
            this.resetToolStripMenuItem.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
            // 
            // filtersToAllToolStripMenuItem
            // 
            this.filtersToAllToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GrayscaleAll,
            this.PixelateAll,
            this.BlurAll,
            this.NegativeAll,
            this.TransparencyAll,
            this.sepiaToneAll});
            this.filtersToAllToolStripMenuItem.Name = "filtersToAllToolStripMenuItem";
            this.filtersToAllToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.filtersToAllToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_FiltersAll;
            // 
            // GrayscaleAll
            // 
            this.GrayscaleAll.Name = "GrayscaleAll";
            this.GrayscaleAll.Size = new System.Drawing.Size(145, 22);
            this.GrayscaleAll.Text = global::ScreenToGif.Properties.Resources.Con_FiltersGray;
            this.GrayscaleAll.Click += new System.EventHandler(this.GrayscaleAll_Click);
            // 
            // PixelateAll
            // 
            this.PixelateAll.Name = "PixelateAll";
            this.PixelateAll.Size = new System.Drawing.Size(145, 22);
            this.PixelateAll.Text = global::ScreenToGif.Properties.Resources.Con_Filters_Pixelate;
            this.PixelateAll.Click += new System.EventHandler(this.PixelateAll_Click);
            // 
            // BlurAll
            // 
            this.BlurAll.Name = "BlurAll";
            this.BlurAll.Size = new System.Drawing.Size(145, 22);
            this.BlurAll.Text = global::ScreenToGif.Properties.Resources.Con_Blur;
            this.BlurAll.Click += new System.EventHandler(this.BlurAll_Click);
            // 
            // NegativeAll
            // 
            this.NegativeAll.Name = "NegativeAll";
            this.NegativeAll.Size = new System.Drawing.Size(145, 22);
            this.NegativeAll.Text = global::ScreenToGif.Properties.Resources.Con_Negative;
            this.NegativeAll.Click += new System.EventHandler(this.NegativeAll_Click);
            // 
            // TransparencyAll
            // 
            this.TransparencyAll.Name = "TransparencyAll";
            this.TransparencyAll.Size = new System.Drawing.Size(145, 22);
            this.TransparencyAll.Text = global::ScreenToGif.Properties.Resources.Con_Transparency;
            this.TransparencyAll.Click += new System.EventHandler(this.TransparencyAll_Click);
            // 
            // sepiaToneAll
            // 
            this.sepiaToneAll.Name = "sepiaToneAll";
            this.sepiaToneAll.Size = new System.Drawing.Size(145, 22);
            this.sepiaToneAll.Text = global::ScreenToGif.Properties.Resources.Con_Sepia;
            this.sepiaToneAll.Click += new System.EventHandler(this.sepiaToneAll_Click);
            // 
            // filtersToolStripMenuItem
            // 
            this.filtersToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.GrayscaleOne,
            this.PixelateOne,
            this.BlurOne,
            this.NegativeOne,
            this.TransparencyOne,
            this.sepiaToneOne});
            this.filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
            this.filtersToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
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
            this.toolStripSeparator1.Size = new System.Drawing.Size(130, 6);
            // 
            // doneToolStripMenuItem
            // 
            this.doneToolStripMenuItem.Name = "doneToolStripMenuItem";
            this.doneToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.doneToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Done;
            this.doneToolStripMenuItem.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
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
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnDone);
            this.flowLayoutPanel1.Controls.Add(this.btnReset);
            this.flowLayoutPanel1.Controls.Add(this.btnFilters);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(589, 37);
            this.flowLayoutPanel1.TabIndex = 4;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(520, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnCancel.Size = new System.Drawing.Size(69, 33);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // btnDone
            // 
            this.btnDone.AutoSize = true;
            this.btnDone.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.btnDone.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDone.Location = new System.Drawing.Point(456, 0);
            this.btnDone.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnDone.Name = "btnDone";
            this.btnDone.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDone.Size = new System.Drawing.Size(61, 33);
            this.btnDone.TabIndex = 3;
            this.btnDone.Text = "Done";
            this.btnDone.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.doneToolStripMenuItem_Click);
            // 
            // btnReset
            // 
            this.btnReset.AutoSize = true;
            this.btnReset.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReset.Enabled = false;
            this.btnReset.FlatAppearance.BorderSize = 0;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.btnReset.Location = new System.Drawing.Point(392, 0);
            this.btnReset.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnReset.Name = "btnReset";
            this.btnReset.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnReset.Size = new System.Drawing.Size(61, 33);
            this.btnReset.TabIndex = 5;
            this.btnReset.Text = "Reset";
            this.btnReset.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
            // 
            // btnFilters
            // 
            this.btnFilters.AutoSize = true;
            this.btnFilters.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnFilters.FlatAppearance.BorderSize = 0;
            this.btnFilters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilters.Image = global::ScreenToGif.Properties.Resources.add;
            this.btnFilters.Location = new System.Drawing.Point(325, 0);
            this.btnFilters.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnFilters.Name = "btnFilters";
            this.btnFilters.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnFilters.Size = new System.Drawing.Size(64, 33);
            this.btnFilters.TabIndex = 6;
            this.btnFilters.Text = "Filters";
            this.btnFilters.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnFilters.UseVisualStyleBackColor = true;
            this.btnFilters.Click += new System.EventHandler(this.btnFilters_Click);
            // 
            // Filters
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(589, 246);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.trackBar);
            this.Controls.Add(this.pictureBoxFilter);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Filters";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Filters";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxFilter)).EndInit();
            this.contextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
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
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem doneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem filtersToAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GrayscaleAll;
        private System.Windows.Forms.ToolStripMenuItem PixelateAll;
        private System.Windows.Forms.ToolStripMenuItem BlurAll;
        private System.Windows.Forms.ToolStripMenuItem NegativeOne;
        private System.Windows.Forms.ToolStripMenuItem NegativeAll;
        private System.Windows.Forms.ToolTip tooltipHelp;
        private System.Windows.Forms.ToolStripMenuItem TransparencyOne;
        private System.Windows.Forms.ToolStripMenuItem TransparencyAll;
        private System.Windows.Forms.ToolStripMenuItem sepiaToneAll;
        private System.Windows.Forms.ToolStripMenuItem sepiaToneOne;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnFilters;
    }
}