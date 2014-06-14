namespace ScreenToGif.Pages
{
    partial class Internal
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Internal));
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resizeAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cropAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.revertOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yoyoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.applyFiltersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.nenuDeleteAfter = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteBefore = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteThisFrameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timerCapture = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.timerPreStart = new System.Windows.Forms.Timer(this.components);
            this.timerCapWithCursor = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.panelTransparent = new System.Windows.Forms.Panel();
            this.btnInfo = new System.Windows.Forms.Button();
            this.btnGifConfig = new System.Windows.Forms.Button();
            this.btnConfig = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label7 = new System.Windows.Forms.Label();
            this.numMaxFps = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.btnRecordPause = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.flowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.pictureBitmap = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPreview = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnDeleteFrame = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panelEdit = new System.Windows.Forms.Panel();
            this.contextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).BeginInit();
            this.flowPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBitmap)).BeginInit();
            this.flowLayoutPanel2.SuspendLayout();
            this.panelEdit.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenu
            // 
            this.contextMenu.BackColor = System.Drawing.Color.Azure;
            this.contextMenu.DropShadowEnabled = false;
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addFrameToolStripMenuItem,
            this.editFrameToolStripMenuItem,
            this.applyFiltersToolStripMenuItem,
            this.exportFrameToolStripMenuItem,
            this.toolStripSeparator1,
            this.nenuDeleteAfter,
            this.menuDeleteBefore,
            this.deleteThisFrameToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.contextMenu.ShowItemToolTips = false;
            this.contextMenu.Size = new System.Drawing.Size(226, 164);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            // 
            // addFrameToolStripMenuItem
            // 
            this.addFrameToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.imageToolStripMenuItem});
            this.addFrameToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.add;
            this.addFrameToolStripMenuItem.Name = "addFrameToolStripMenuItem";
            this.addFrameToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
            this.addFrameToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_AddFrame;
            // 
            // imageToolStripMenuItem
            // 
            this.imageToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Image_17;
            this.imageToolStripMenuItem.Name = "imageToolStripMenuItem";
            this.imageToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.imageToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Image;
            this.imageToolStripMenuItem.Click += new System.EventHandler(this.imageToolStripMenuItem_Click);
            // 
            // editFrameToolStripMenuItem
            // 
            this.editFrameToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resizeAllToolStripMenuItem,
            this.cropAllToolStripMenuItem,
            this.revertOrderToolStripMenuItem,
            this.yoyoToolStripMenuItem});
            this.editFrameToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.edit;
            this.editFrameToolStripMenuItem.Name = "editFrameToolStripMenuItem";
            this.editFrameToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
            this.editFrameToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_EditFrame;
            // 
            // resizeAllToolStripMenuItem
            // 
            this.resizeAllToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.resize;
            this.resizeAllToolStripMenuItem.Name = "resizeAllToolStripMenuItem";
            this.resizeAllToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.resizeAllToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_ResizeAll;
            this.resizeAllToolStripMenuItem.Click += new System.EventHandler(this.resizeAllFramesToolStripMenuItem_Click);
            // 
            // cropAllToolStripMenuItem
            // 
            this.cropAllToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.crop;
            this.cropAllToolStripMenuItem.Name = "cropAllToolStripMenuItem";
            this.cropAllToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.cropAllToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_CropAll;
            this.cropAllToolStripMenuItem.Click += new System.EventHandler(this.cropAllToolStripMenuItem_Click);
            // 
            // revertOrderToolStripMenuItem
            // 
            this.revertOrderToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.revert;
            this.revertOrderToolStripMenuItem.Name = "revertOrderToolStripMenuItem";
            this.revertOrderToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.revertOrderToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Revert;
            this.revertOrderToolStripMenuItem.Click += new System.EventHandler(this.revertOrderToolStripMenuItem_Click);
            // 
            // yoyoToolStripMenuItem
            // 
            this.yoyoToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Yoyo;
            this.yoyoToolStripMenuItem.Name = "yoyoToolStripMenuItem";
            this.yoyoToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.yoyoToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Yoyo;
            this.yoyoToolStripMenuItem.Click += new System.EventHandler(this.yoyoToolStripMenuItem_Click);
            // 
            // applyFiltersToolStripMenuItem
            // 
            this.applyFiltersToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.filters;
            this.applyFiltersToolStripMenuItem.Name = "applyFiltersToolStripMenuItem";
            this.applyFiltersToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
            this.applyFiltersToolStripMenuItem.Text = "Apply";
            this.applyFiltersToolStripMenuItem.Click += new System.EventHandler(this.applyFiltersToolStripMenuItem_Click);
            // 
            // exportFrameToolStripMenuItem
            // 
            this.exportFrameToolStripMenuItem.Name = "exportFrameToolStripMenuItem";
            this.exportFrameToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
            this.exportFrameToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_ExportFrame;
            this.exportFrameToolStripMenuItem.Click += new System.EventHandler(this.exportFrameToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(222, 6);
            // 
            // nenuDeleteAfter
            // 
            this.nenuDeleteAfter.Name = "nenuDeleteAfter";
            this.nenuDeleteAfter.Size = new System.Drawing.Size(225, 22);
            this.nenuDeleteAfter.Text = global::ScreenToGif.Properties.Resources.Context_DelAfter;
            this.nenuDeleteAfter.Click += new System.EventHandler(this.nenuDeleteAfter_Click);
            // 
            // menuDeleteBefore
            // 
            this.menuDeleteBefore.Name = "menuDeleteBefore";
            this.menuDeleteBefore.Size = new System.Drawing.Size(225, 22);
            this.menuDeleteBefore.Text = global::ScreenToGif.Properties.Resources.Context_DelBefore;
            this.menuDeleteBefore.Click += new System.EventHandler(this.menuDeleteBefore_Click);
            // 
            // deleteThisFrameToolStripMenuItem
            // 
            this.deleteThisFrameToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.deleteThisFrameToolStripMenuItem.Name = "deleteThisFrameToolStripMenuItem";
            this.deleteThisFrameToolStripMenuItem.Size = new System.Drawing.Size(225, 22);
            this.deleteThisFrameToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_DeleteThis;
            this.deleteThisFrameToolStripMenuItem.Click += new System.EventHandler(this.deleteThisFrameToolStripMenuItem_Click);
            // 
            // timerCapture
            // 
            this.timerCapture.Tick += new System.EventHandler(this.timerCapture_Tick);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "gif";
            this.saveFileDialog.FileName = global::ScreenToGif.Properties.Resources.SFDialog_Filename;
            this.saveFileDialog.Filter = " Gif files (*.gif)|*.gif|All files (*.*)|*.*";
            this.saveFileDialog.Title = global::ScreenToGif.Properties.Resources.SFDialog_Title;
            // 
            // timerPreStart
            // 
            this.timerPreStart.Interval = 1000;
            this.timerPreStart.Tick += new System.EventHandler(this.timerPreStart_Tick);
            // 
            // timerCapWithCursor
            // 
            this.timerCapWithCursor.Interval = 15;
            this.timerCapWithCursor.Tick += new System.EventHandler(this.timerCapWithCursor_Tick);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 300;
            this.toolTip.BackColor = System.Drawing.SystemColors.Menu;
            // 
            // toolTipHelp
            // 
            this.toolTipHelp.AutomaticDelay = 300;
            this.toolTipHelp.BackColor = System.Drawing.SystemColors.Menu;
            this.toolTipHelp.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.toolTipHelp.ToolTipTitle = global::ScreenToGif.Properties.Resources.Tooltip_Title;
            // 
            // openImageDialog
            // 
            this.openImageDialog.Filter = "Image Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png";
            this.openImageDialog.Title = global::ScreenToGif.Properties.Resources.Dialog_OpenImage;
            // 
            // panelTransparent
            // 
            this.panelTransparent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelTransparent.BackColor = System.Drawing.Color.LimeGreen;
            this.panelTransparent.Location = new System.Drawing.Point(0, 0);
            this.panelTransparent.Name = "panelTransparent";
            this.panelTransparent.Size = new System.Drawing.Size(600, 270);
            this.panelTransparent.TabIndex = 18;
            // 
            // btnInfo
            // 
            this.btnInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInfo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnInfo.FlatAppearance.BorderSize = 0;
            this.btnInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInfo.Image = global::ScreenToGif.Properties.Resources.Info_17Blue;
            this.btnInfo.Location = new System.Drawing.Point(169, 3);
            this.btnInfo.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnInfo.Size = new System.Drawing.Size(23, 23);
            this.btnInfo.TabIndex = 29;
            this.btnInfo.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnInfo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnInfo.UseVisualStyleBackColor = true;
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
            // 
            // btnGifConfig
            // 
            this.btnGifConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGifConfig.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnGifConfig.FlatAppearance.BorderSize = 0;
            this.btnGifConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGifConfig.Image = global::ScreenToGif.Properties.Resources.Image_17;
            this.btnGifConfig.Location = new System.Drawing.Point(194, 3);
            this.btnGifConfig.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.btnGifConfig.Name = "btnGifConfig";
            this.btnGifConfig.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnGifConfig.Size = new System.Drawing.Size(23, 23);
            this.btnGifConfig.TabIndex = 32;
            this.btnGifConfig.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnGifConfig.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnGifConfig.UseVisualStyleBackColor = true;
            this.btnGifConfig.Click += new System.EventHandler(this.btnGifConfig_Click);
            // 
            // btnConfig
            // 
            this.btnConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConfig.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnConfig.FlatAppearance.BorderSize = 0;
            this.btnConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfig.Image = ((System.Drawing.Image)(resources.GetObject("btnConfig.Image")));
            this.btnConfig.Location = new System.Drawing.Point(219, 3);
            this.btnConfig.Margin = new System.Windows.Forms.Padding(1, 3, 1, 3);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnConfig.Size = new System.Drawing.Size(23, 23);
            this.btnConfig.TabIndex = 28;
            this.btnConfig.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnConfig.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnConfig.UseVisualStyleBackColor = true;
            this.btnConfig.Click += new System.EventHandler(this.btnConfig_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::ScreenToGif.Properties.Resources.Separator;
            this.pictureBox1.Location = new System.Drawing.Point(246, 4);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(2, 23);
            this.pictureBox1.TabIndex = 31;
            this.pictureBox1.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(252, 7);
            this.label7.Margin = new System.Windows.Forms.Padding(1, 7, 1, 3);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(24, 21);
            this.label7.TabIndex = 30;
            this.label7.Text = "FPS";
            this.label7.UseCompatibleTextRendering = true;
            // 
            // numMaxFps
            // 
            this.numMaxFps.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.numMaxFps.AutoSize = true;
            this.numMaxFps.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.numMaxFps.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numMaxFps.Location = new System.Drawing.Point(280, 4);
            this.numMaxFps.Margin = new System.Windows.Forms.Padding(2, 0, 3, 4);
            this.numMaxFps.Maximum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.numMaxFps.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numMaxFps.Name = "numMaxFps";
            this.numMaxFps.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.numMaxFps.Size = new System.Drawing.Size(35, 23);
            this.numMaxFps.TabIndex = 21;
            this.numMaxFps.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(318, 4);
            this.label6.Margin = new System.Windows.Forms.Padding(1, 4, 1, 3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(26, 21);
            this.label6.TabIndex = 27;
            this.label6.Text = "Size";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label6.UseCompatibleTextRendering = true;
            // 
            // tbWidth
            // 
            this.tbWidth.Location = new System.Drawing.Point(348, 4);
            this.tbWidth.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbWidth.Size = new System.Drawing.Size(46, 23);
            this.tbWidth.TabIndex = 26;
            this.tbWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbWidth.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbSize_KeyDown);
            this.tbWidth.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbSize_KeyPress);
            this.tbWidth.Leave += new System.EventHandler(this.tbSize_Leave);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(397, 8);
            this.label5.Margin = new System.Windows.Forms.Padding(1, 8, 0, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(14, 15);
            this.label5.TabIndex = 24;
            this.label5.Text = "X";
            // 
            // tbHeight
            // 
            this.tbHeight.Location = new System.Drawing.Point(415, 4);
            this.tbHeight.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbHeight.Size = new System.Drawing.Size(46, 23);
            this.tbHeight.TabIndex = 23;
            this.tbHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbSize_KeyDown);
            this.tbHeight.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbSize_KeyPress);
            this.tbHeight.Leave += new System.EventHandler(this.tbSize_Leave);
            // 
            // btnRecordPause
            // 
            this.btnRecordPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRecordPause.AutoSize = true;
            this.btnRecordPause.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRecordPause.FlatAppearance.BorderSize = 0;
            this.btnRecordPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecordPause.Image = global::ScreenToGif.Properties.Resources.Record;
            this.btnRecordPause.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRecordPause.Location = new System.Drawing.Point(465, 3);
            this.btnRecordPause.Margin = new System.Windows.Forms.Padding(3, 3, 1, 3);
            this.btnRecordPause.Name = "btnRecordPause";
            this.btnRecordPause.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnRecordPause.Size = new System.Drawing.Size(70, 25);
            this.btnRecordPause.TabIndex = 25;
            this.btnRecordPause.Text = global::ScreenToGif.Properties.Resources.btnRecordPause_Record;
            this.btnRecordPause.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRecordPause.UseVisualStyleBackColor = true;
            this.btnRecordPause.Click += new System.EventHandler(this.btnRecordPause_Click);
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.AutoSize = true;
            this.btnStop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Image = global::ScreenToGif.Properties.Resources.Stop_17Red;
            this.btnStop.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.btnStop.Location = new System.Drawing.Point(541, 3);
            this.btnStop.Margin = new System.Windows.Forms.Padding(1, 3, 3, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnStop.Size = new System.Drawing.Size(58, 25);
            this.btnStop.TabIndex = 22;
            this.btnStop.Text = global::ScreenToGif.Properties.Resources.btnStop;
            this.btnStop.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // flowPanel
            // 
            this.flowPanel.Controls.Add(this.btnStop);
            this.flowPanel.Controls.Add(this.btnRecordPause);
            this.flowPanel.Controls.Add(this.tbHeight);
            this.flowPanel.Controls.Add(this.label5);
            this.flowPanel.Controls.Add(this.tbWidth);
            this.flowPanel.Controls.Add(this.label6);
            this.flowPanel.Controls.Add(this.numMaxFps);
            this.flowPanel.Controls.Add(this.label7);
            this.flowPanel.Controls.Add(this.pictureBox1);
            this.flowPanel.Controls.Add(this.btnConfig);
            this.flowPanel.Controls.Add(this.btnGifConfig);
            this.flowPanel.Controls.Add(this.btnInfo);
            this.flowPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowPanel.Location = new System.Drawing.Point(0, 269);
            this.flowPanel.Name = "flowPanel";
            this.flowPanel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.flowPanel.Size = new System.Drawing.Size(600, 31);
            this.flowPanel.TabIndex = 17;
            // 
            // trackBar
            // 
            this.trackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar.AutoSize = false;
            this.trackBar.Location = new System.Drawing.Point(12, 240);
            this.trackBar.Maximum = 40;
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(576, 25);
            this.trackBar.TabIndex = 0;
            this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
            this.trackBar.Enter += new System.EventHandler(this.trackBar_Enter);
            // 
            // pictureBitmap
            // 
            this.pictureBitmap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBitmap.ContextMenuStrip = this.contextMenu;
            this.pictureBitmap.Location = new System.Drawing.Point(9, 44);
            this.pictureBitmap.Margin = new System.Windows.Forms.Padding(9);
            this.pictureBitmap.Name = "pictureBitmap";
            this.pictureBitmap.Size = new System.Drawing.Size(582, 184);
            this.pictureBitmap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBitmap.TabIndex = 1;
            this.pictureBitmap.TabStop = false;
            this.pictureBitmap.Click += new System.EventHandler(this.pictureBitmap_Click);
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel2.Controls.Add(this.btnCancel);
            this.flowLayoutPanel2.Controls.Add(this.btnDone);
            this.flowLayoutPanel2.Controls.Add(this.btnDeleteFrame);
            this.flowLayoutPanel2.Controls.Add(this.btnUndo);
            this.flowLayoutPanel2.Controls.Add(this.btnReset);
            this.flowLayoutPanel2.Controls.Add(this.btnPreview);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(600, 35);
            this.flowLayoutPanel2.TabIndex = 19;
            // 
            // btnPreview
            // 
            this.btnPreview.AutoSize = true;
            this.btnPreview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPreview.FlatAppearance.BorderSize = 0;
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.Image = global::ScreenToGif.Properties.Resources.Play_17Green;
            this.btnPreview.Location = new System.Drawing.Point(130, 0);
            this.btnPreview.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnPreview.Size = new System.Drawing.Size(100, 33);
            this.btnPreview.TabIndex = 5;
            this.btnPreview.Text = "Play Preview";
            this.btnPreview.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.pictureBitmap_Click);
            // 
            // btnReset
            // 
            this.btnReset.AutoSize = true;
            this.btnReset.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReset.Enabled = false;
            this.btnReset.FlatAppearance.BorderSize = 0;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.btnReset.Location = new System.Drawing.Point(233, 0);
            this.btnReset.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnReset.Name = "btnReset";
            this.btnReset.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnReset.Size = new System.Drawing.Size(61, 33);
            this.btnReset.TabIndex = 4;
            this.btnReset.Text = "Reset";
            this.btnReset.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.AutoSize = true;
            this.btnUndo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnUndo.Enabled = false;
            this.btnUndo.FlatAppearance.BorderSize = 0;
            this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUndo.Image = global::ScreenToGif.Properties.Resources.Undo;
            this.btnUndo.Location = new System.Drawing.Point(297, 0);
            this.btnUndo.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnUndo.Size = new System.Drawing.Size(62, 33);
            this.btnUndo.TabIndex = 3;
            this.btnUndo.Text = "Undo";
            this.btnUndo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // btnDeleteFrame
            // 
            this.btnDeleteFrame.AutoSize = true;
            this.btnDeleteFrame.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDeleteFrame.FlatAppearance.BorderSize = 0;
            this.btnDeleteFrame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteFrame.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.btnDeleteFrame.Location = new System.Drawing.Point(362, 0);
            this.btnDeleteFrame.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnDeleteFrame.Name = "btnDeleteFrame";
            this.btnDeleteFrame.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDeleteFrame.Size = new System.Drawing.Size(102, 33);
            this.btnDeleteFrame.TabIndex = 2;
            this.btnDeleteFrame.Text = "Delete Frame";
            this.btnDeleteFrame.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDeleteFrame.UseVisualStyleBackColor = true;
            this.btnDeleteFrame.Click += new System.EventHandler(this.btnDeleteFrame_Click);
            // 
            // btnDone
            // 
            this.btnDone.AutoSize = true;
            this.btnDone.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.btnDone.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDone.Location = new System.Drawing.Point(467, 0);
            this.btnDone.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnDone.Name = "btnDone";
            this.btnDone.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDone.Size = new System.Drawing.Size(61, 33);
            this.btnDone.TabIndex = 1;
            this.btnDone.Text = "Done";
            this.btnDone.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(531, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnCancel.Size = new System.Drawing.Size(69, 33);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // panelEdit
            // 
            this.panelEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.panelEdit.Controls.Add(this.flowLayoutPanel2);
            this.panelEdit.Controls.Add(this.pictureBitmap);
            this.panelEdit.Controls.Add(this.trackBar);
            this.panelEdit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.panelEdit.Location = new System.Drawing.Point(0, 0);
            this.panelEdit.Name = "panelEdit";
            this.panelEdit.Size = new System.Drawing.Size(600, 270);
            this.panelEdit.TabIndex = 19;
            this.panelEdit.Visible = false;
            // 
            // Internal
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.panelEdit);
            this.Controls.Add(this.panelTransparent);
            this.Controls.Add(this.flowPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Internal";
            this.Size = new System.Drawing.Size(600, 300);
            this.Load += new System.EventHandler(this.Internal_Load);
            this.Resize += new System.EventHandler(this.Internal_Resize);
            this.contextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).EndInit();
            this.flowPanel.ResumeLayout(false);
            this.flowPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBitmap)).EndInit();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.panelEdit.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem addFrameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem imageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editFrameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resizeAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cropAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem revertOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem yoyoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem applyFiltersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportFrameToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem nenuDeleteAfter;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteBefore;
        private System.Windows.Forms.ToolStripMenuItem deleteThisFrameToolStripMenuItem;
        private System.Windows.Forms.Timer timerCapture;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.Timer timerPreStart;
        private System.Windows.Forms.Timer timerCapWithCursor;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolTip toolTipHelp;
        private System.Windows.Forms.OpenFileDialog openImageDialog;
        private System.Windows.Forms.Panel panelTransparent;
        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.Button btnGifConfig;
        private System.Windows.Forms.Button btnConfig;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numMaxFps;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbWidth;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbHeight;
        private System.Windows.Forms.Button btnRecordPause;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.FlowLayoutPanel flowPanel;
        private System.Windows.Forms.TrackBar trackBar;
        private System.Windows.Forms.PictureBox pictureBitmap;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnDeleteFrame;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Panel panelEdit;
    }
}
