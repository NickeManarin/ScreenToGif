using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
using ScreenToGif.Properties;
using ScreenToGif.Util;

namespace ScreenToGif
{
    partial class Legacy
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Legacy));
            this.panelTransparent = new System.Windows.Forms.Panel();
            this.panelEdit = new System.Windows.Forms.Panel();
            this.lblDelay = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnDeleteFrame = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnFilters = new System.Windows.Forms.Button();
            this.btnOptions = new System.Windows.Forms.Button();
            this.btnPreview = new System.Windows.Forms.Button();
            this.pictureBitmap = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.con_addText = new System.Windows.Forms.ToolStripMenuItem();
            this.addQuickCaptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem15 = new System.Windows.Forms.ToolStripMenuItem();
            this.con_tbCaption = new System.Windows.Forms.ToolStripTextBox();
            this.con_typeEnter = new System.Windows.Forms.ToolStripMenuItem();
            this.con_tbNumFrames = new System.Windows.Forms.ToolStripTextBox();
            this.con_addCaption = new System.Windows.Forms.ToolStripMenuItem();
            this.addFrameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageItem = new System.Windows.Forms.ToolStripMenuItem();
            this.titleImageItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editFrameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resizeAllItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cropAllItem = new System.Windows.Forms.ToolStripMenuItem();
            this.revertOrderItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yoyoItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sloMotionItem = new System.Windows.Forms.ToolStripMenuItem();
            this.borderItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportFrameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_showGrid = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.con_DeleteAfter = new System.Windows.Forms.ToolStripMenuItem();
            this.con_DeleteBefore = new System.Windows.Forms.ToolStripMenuItem();
            this.con_DeleteThis = new System.Windows.Forms.ToolStripMenuItem();
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.timerCapture = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.timerPreStart = new System.Windows.Forms.Timer(this.components);
            this.flowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnRecordPause = new System.Windows.Forms.Button();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.lblSize = new System.Windows.Forms.Label();
            this.numMaxFps = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnConfig = new System.Windows.Forms.Button();
            this.btnGifConfig = new System.Windows.Forms.Button();
            this.btnInfo = new System.Windows.Forms.Button();
            this.timerCapWithCursor = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.contextDelay = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.typeYouDesiredFrameDelayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_tbDelay = new System.Windows.Forms.ToolStripTextBox();
            this.between10MsAnd1000MsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextSmall = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem14 = new System.Windows.Forms.ToolStripMenuItem();
            this.panelEdit.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBitmap)).BeginInit();
            this.contextMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.flowPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.contextDelay.SuspendLayout();
            this.contextSmall.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTransparent
            // 
            this.panelTransparent.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelTransparent.BackColor = System.Drawing.Color.LimeGreen;
            this.panelTransparent.Location = new System.Drawing.Point(0, 0);
            this.panelTransparent.Name = "panelTransparent";
            this.panelTransparent.Size = new System.Drawing.Size(599, 220);
            this.panelTransparent.TabIndex = 1;
            // 
            // panelEdit
            // 
            this.panelEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.panelEdit.Controls.Add(this.lblDelay);
            this.panelEdit.Controls.Add(this.flowLayoutPanel2);
            this.panelEdit.Controls.Add(this.pictureBitmap);
            this.panelEdit.Controls.Add(this.trackBar);
            this.panelEdit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.panelEdit.Location = new System.Drawing.Point(0, 0);
            this.panelEdit.Name = "panelEdit";
            this.panelEdit.Size = new System.Drawing.Size(599, 220);
            this.panelEdit.TabIndex = 15;
            this.panelEdit.Visible = false;
            // 
            // lblDelay
            // 
            this.lblDelay.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDelay.AutoSize = true;
            this.lblDelay.BackColor = System.Drawing.Color.Gainsboro;
            this.lblDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDelay.CausesValidation = false;
            this.lblDelay.Cursor = System.Windows.Forms.Cursors.SizeNS;
            this.lblDelay.Location = new System.Drawing.Point(12, 108);
            this.lblDelay.Name = "lblDelay";
            this.lblDelay.Size = new System.Drawing.Size(40, 17);
            this.lblDelay.TabIndex = 20;
            this.lblDelay.Text = "66 ms";
            this.toolTip.SetToolTip(this.lblDelay, "Frame delay. Right click to set number.");
            this.lblDelay.UseMnemonic = false;
            this.lblDelay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lblDelay_MouseDown);
            this.lblDelay.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lblDelay_MouseMove);
            this.lblDelay.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lblDelay_MouseUp);
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
            this.flowLayoutPanel2.Controls.Add(this.btnFilters);
            this.flowLayoutPanel2.Controls.Add(this.btnOptions);
            this.flowLayoutPanel2.Controls.Add(this.btnPreview);
            this.flowLayoutPanel2.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(599, 35);
            this.flowLayoutPanel2.TabIndex = 19;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(530, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnCancel.Size = new System.Drawing.Size(69, 33);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.btnCancel, "Shortcut: Esc");
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnDone
            // 
            this.btnDone.AutoSize = true;
            this.btnDone.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnDone.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.btnDone.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDone.Location = new System.Drawing.Point(469, 0);
            this.btnDone.Margin = new System.Windows.Forms.Padding(0);
            this.btnDone.Name = "btnDone";
            this.btnDone.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDone.Size = new System.Drawing.Size(61, 33);
            this.btnDone.TabIndex = 1;
            this.btnDone.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnDone.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.btnDone, "Shortcut: Enter");
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // btnDeleteFrame
            // 
            this.btnDeleteFrame.AutoSize = true;
            this.btnDeleteFrame.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDeleteFrame.FlatAppearance.BorderSize = 0;
            this.btnDeleteFrame.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnDeleteFrame.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnDeleteFrame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteFrame.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.btnDeleteFrame.Location = new System.Drawing.Point(367, 0);
            this.btnDeleteFrame.Margin = new System.Windows.Forms.Padding(0);
            this.btnDeleteFrame.Name = "btnDeleteFrame";
            this.btnDeleteFrame.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDeleteFrame.Size = new System.Drawing.Size(102, 33);
            this.btnDeleteFrame.TabIndex = 2;
            this.btnDeleteFrame.Text = global::ScreenToGif.Properties.Resources.btnDeleteFrame;
            this.btnDeleteFrame.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDeleteFrame.UseVisualStyleBackColor = true;
            this.btnDeleteFrame.Click += new System.EventHandler(this.btnDeleteFrame_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.AutoSize = true;
            this.btnUndo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnUndo.Enabled = false;
            this.btnUndo.FlatAppearance.BorderSize = 0;
            this.btnUndo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnUndo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUndo.Image = global::ScreenToGif.Properties.Resources.Undo;
            this.btnUndo.Location = new System.Drawing.Point(305, 0);
            this.btnUndo.Margin = new System.Windows.Forms.Padding(0);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnUndo.Size = new System.Drawing.Size(62, 33);
            this.btnUndo.TabIndex = 3;
            this.btnUndo.Text = global::ScreenToGif.Properties.Resources.btnUndo;
            this.btnUndo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // btnReset
            // 
            this.btnReset.AutoSize = true;
            this.btnReset.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnReset.Enabled = false;
            this.btnReset.FlatAppearance.BorderSize = 0;
            this.btnReset.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnReset.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.btnReset.Location = new System.Drawing.Point(244, 0);
            this.btnReset.Margin = new System.Windows.Forms.Padding(0);
            this.btnReset.Name = "btnReset";
            this.btnReset.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnReset.Size = new System.Drawing.Size(61, 33);
            this.btnReset.TabIndex = 4;
            this.btnReset.Text = global::ScreenToGif.Properties.Resources.btnReset;
            this.btnReset.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnFilters
            // 
            this.btnFilters.AutoSize = true;
            this.btnFilters.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnFilters.FlatAppearance.BorderSize = 0;
            this.btnFilters.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnFilters.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnFilters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilters.Image = global::ScreenToGif.Properties.Resources.filters;
            this.btnFilters.Location = new System.Drawing.Point(180, 0);
            this.btnFilters.Margin = new System.Windows.Forms.Padding(0);
            this.btnFilters.Name = "btnFilters";
            this.btnFilters.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnFilters.Size = new System.Drawing.Size(64, 33);
            this.btnFilters.TabIndex = 7;
            this.btnFilters.Text = global::ScreenToGif.Properties.Resources.Title_Filters;
            this.btnFilters.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnFilters.UseVisualStyleBackColor = true;
            this.btnFilters.Click += new System.EventHandler(this.btnFilters_Click);
            // 
            // btnOptions
            // 
            this.btnOptions.AutoSize = true;
            this.btnOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnOptions.FlatAppearance.BorderSize = 0;
            this.btnOptions.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOptions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOptions.Image = global::ScreenToGif.Properties.Resources.add;
            this.btnOptions.Location = new System.Drawing.Point(105, 0);
            this.btnOptions.Margin = new System.Windows.Forms.Padding(0);
            this.btnOptions.Name = "btnOptions";
            this.btnOptions.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnOptions.Size = new System.Drawing.Size(75, 33);
            this.btnOptions.TabIndex = 8;
            this.btnOptions.Text = global::ScreenToGif.Properties.Resources.btnOptions;
            this.btnOptions.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOptions.UseVisualStyleBackColor = true;
            this.btnOptions.Click += new System.EventHandler(this.btnOptions_Click);
            // 
            // btnPreview
            // 
            this.btnPreview.AutoSize = true;
            this.btnPreview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPreview.FlatAppearance.BorderSize = 0;
            this.btnPreview.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnPreview.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.Image = global::ScreenToGif.Properties.Resources.Play_17Green;
            this.btnPreview.Location = new System.Drawing.Point(5, 0);
            this.btnPreview.Margin = new System.Windows.Forms.Padding(0);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnPreview.Size = new System.Drawing.Size(100, 33);
            this.btnPreview.TabIndex = 5;
            this.btnPreview.Text = global::ScreenToGif.Properties.Resources.Con_PlayPreview;
            this.btnPreview.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBitmap_MouseClick);
            // 
            // pictureBitmap
            // 
            this.pictureBitmap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBitmap.BackColor = System.Drawing.Color.Transparent;
            this.pictureBitmap.ContextMenuStrip = this.contextMenu;
            this.pictureBitmap.Location = new System.Drawing.Point(9, 44);
            this.pictureBitmap.Margin = new System.Windows.Forms.Padding(9);
            this.pictureBitmap.Name = "pictureBitmap";
            this.pictureBitmap.Size = new System.Drawing.Size(581, 134);
            this.pictureBitmap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBitmap.TabIndex = 1;
            this.pictureBitmap.TabStop = false;
            this.pictureBitmap.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBitmap_MouseClick);
            // 
            // contextMenu
            // 
            this.contextMenu.BackColor = System.Drawing.Color.Azure;
            this.contextMenu.DropShadowEnabled = false;
            this.contextMenu.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.con_addText,
            this.addQuickCaptionToolStripMenuItem,
            this.addFrameItem,
            this.editFrameItem,
            this.exportFrameItem,
            this.con_showGrid,
            this.toolStripSeparator1,
            this.con_DeleteAfter,
            this.con_DeleteBefore,
            this.con_DeleteThis});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.contextMenu.ShowItemToolTips = false;
            this.contextMenu.Size = new System.Drawing.Size(315, 226);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            // 
            // con_addText
            // 
            this.con_addText.Image = global::ScreenToGif.Properties.Resources.Text1;
            this.con_addText.Name = "con_addText";
            this.con_addText.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.con_addText.Size = new System.Drawing.Size(314, 24);
            this.con_addText.Text = "Add Text...";
            this.con_addText.Click += new System.EventHandler(this.con_addText_Click);
            // 
            // addQuickCaptionToolStripMenuItem
            // 
            this.addQuickCaptionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem15,
            this.con_tbCaption,
            this.con_typeEnter,
            this.con_tbNumFrames,
            this.con_addCaption});
            this.addQuickCaptionToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Comment1;
            this.addQuickCaptionToolStripMenuItem.Name = "addQuickCaptionToolStripMenuItem";
            this.addQuickCaptionToolStripMenuItem.Size = new System.Drawing.Size(314, 24);
            this.addQuickCaptionToolStripMenuItem.Text = "Add Quick Caption";
            // 
            // toolStripMenuItem15
            // 
            this.toolStripMenuItem15.Enabled = false;
            this.toolStripMenuItem15.Image = global::ScreenToGif.Properties.Resources.Comment;
            this.toolStripMenuItem15.Name = "toolStripMenuItem15";
            this.toolStripMenuItem15.Size = new System.Drawing.Size(310, 24);
            this.toolStripMenuItem15.Text = "Text:";
            // 
            // con_tbCaption
            // 
            this.con_tbCaption.AcceptsReturn = true;
            this.con_tbCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.con_tbCaption.CausesValidation = false;
            this.con_tbCaption.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.con_tbCaption.MaxLength = 255;
            this.con_tbCaption.Name = "con_tbCaption";
            this.con_tbCaption.Size = new System.Drawing.Size(250, 23);
            // 
            // con_typeEnter
            // 
            this.con_typeEnter.Enabled = false;
            this.con_typeEnter.ForeColor = System.Drawing.Color.Black;
            this.con_typeEnter.Image = global::ScreenToGif.Properties.Resources.ArrowNext;
            this.con_typeEnter.Name = "con_typeEnter";
            this.con_typeEnter.Size = new System.Drawing.Size(310, 24);
            this.con_typeEnter.Text = "For the Next # of Frames";
            // 
            // con_tbNumFrames
            // 
            this.con_tbNumFrames.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.con_tbNumFrames.Name = "con_tbNumFrames";
            this.con_tbNumFrames.Size = new System.Drawing.Size(50, 23);
            this.con_tbNumFrames.Text = "0";
            this.con_tbNumFrames.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // con_addCaption
            // 
            this.con_addCaption.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.con_addCaption.Name = "con_addCaption";
            this.con_addCaption.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.con_addCaption.Size = new System.Drawing.Size(310, 24);
            this.con_addCaption.Text = "Done";
            this.con_addCaption.Click += new System.EventHandler(this.con_addCaption_Click);
            // 
            // addFrameItem
            // 
            this.addFrameItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.imageItem,
            this.titleImageItem});
            this.addFrameItem.Image = global::ScreenToGif.Properties.Resources.add;
            this.addFrameItem.Name = "addFrameItem";
            this.addFrameItem.Size = new System.Drawing.Size(314, 24);
            this.addFrameItem.Text = global::ScreenToGif.Properties.Resources.Con_AddFrame;
            // 
            // imageItem
            // 
            this.imageItem.Image = global::ScreenToGif.Properties.Resources.Image_17;
            this.imageItem.Name = "imageItem";
            this.imageItem.Size = new System.Drawing.Size(152, 24);
            this.imageItem.Text = global::ScreenToGif.Properties.Resources.Con_Image;
            this.imageItem.Click += new System.EventHandler(this.con_image_Click);
            // 
            // titleImageItem
            // 
            this.titleImageItem.Image = global::ScreenToGif.Properties.Resources.Text1;
            this.titleImageItem.Name = "titleImageItem";
            this.titleImageItem.Size = new System.Drawing.Size(152, 24);
            this.titleImageItem.Text = "Title Frame";
            this.titleImageItem.Click += new System.EventHandler(this.con_titleImage_Click);
            // 
            // editFrameItem
            // 
            this.editFrameItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resizeAllItem,
            this.cropAllItem,
            this.revertOrderItem,
            this.yoyoItem,
            this.sloMotionItem,
            this.borderItem});
            this.editFrameItem.Image = global::ScreenToGif.Properties.Resources.edit;
            this.editFrameItem.Name = "editFrameItem";
            this.editFrameItem.Size = new System.Drawing.Size(314, 24);
            this.editFrameItem.Text = global::ScreenToGif.Properties.Resources.Con_EditFrame;
            // 
            // resizeAllItem
            // 
            this.resizeAllItem.Image = global::ScreenToGif.Properties.Resources.resize;
            this.resizeAllItem.Name = "resizeAllItem";
            this.resizeAllItem.Size = new System.Drawing.Size(157, 24);
            this.resizeAllItem.Text = global::ScreenToGif.Properties.Resources.Con_ResizeAll;
            this.resizeAllItem.Click += new System.EventHandler(this.con_resizeAllFrames_Click);
            // 
            // cropAllItem
            // 
            this.cropAllItem.Image = global::ScreenToGif.Properties.Resources.crop;
            this.cropAllItem.Name = "cropAllItem";
            this.cropAllItem.Size = new System.Drawing.Size(157, 24);
            this.cropAllItem.Text = global::ScreenToGif.Properties.Resources.Con_CropAll;
            this.cropAllItem.Click += new System.EventHandler(this.con_cropAll_Click);
            // 
            // revertOrderItem
            // 
            this.revertOrderItem.Image = global::ScreenToGif.Properties.Resources.revert;
            this.revertOrderItem.Name = "revertOrderItem";
            this.revertOrderItem.Size = new System.Drawing.Size(157, 24);
            this.revertOrderItem.Text = global::ScreenToGif.Properties.Resources.Con_Revert;
            this.revertOrderItem.Click += new System.EventHandler(this.con_revertOrder_Click);
            // 
            // yoyoItem
            // 
            this.yoyoItem.Image = global::ScreenToGif.Properties.Resources.Yoyo;
            this.yoyoItem.Name = "yoyoItem";
            this.yoyoItem.Size = new System.Drawing.Size(157, 24);
            this.yoyoItem.Text = global::ScreenToGif.Properties.Resources.Con_Yoyo;
            this.yoyoItem.Click += new System.EventHandler(this.con_yoyo_Click);
            // 
            // sloMotionItem
            // 
            this.sloMotionItem.Image = global::ScreenToGif.Properties.Resources.Delay;
            this.sloMotionItem.Name = "sloMotionItem";
            this.sloMotionItem.Size = new System.Drawing.Size(157, 24);
            this.sloMotionItem.Text = global::ScreenToGif.Properties.Resources.Con_SloMotion;
            this.sloMotionItem.Click += new System.EventHandler(this.con_sloMotion_Click);
            // 
            // borderItem
            // 
            this.borderItem.Name = "borderItem";
            this.borderItem.Size = new System.Drawing.Size(157, 24);
            this.borderItem.Text = "Add Borders";
            this.borderItem.Click += new System.EventHandler(this.con_Border_Click);
            // 
            // exportFrameItem
            // 
            this.exportFrameItem.Image = global::ScreenToGif.Properties.Resources.Export;
            this.exportFrameItem.Name = "exportFrameItem";
            this.exportFrameItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.exportFrameItem.Size = new System.Drawing.Size(314, 24);
            this.exportFrameItem.Text = global::ScreenToGif.Properties.Resources.Con_ExportFrame;
            this.exportFrameItem.Click += new System.EventHandler(this.con_exportFrame_Click);
            // 
            // con_showGrid
            // 
            this.con_showGrid.CheckOnClick = true;
            this.con_showGrid.Image = global::ScreenToGif.Properties.Resources.Grid_Icon;
            this.con_showGrid.Name = "con_showGrid";
            this.con_showGrid.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
            this.con_showGrid.Size = new System.Drawing.Size(314, 24);
            this.con_showGrid.Text = global::ScreenToGif.Properties.Resources.Con_ShowGrid;
            this.con_showGrid.CheckedChanged += new System.EventHandler(this.con_showGrid_CheckedChanged);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(311, 6);
            // 
            // con_DeleteAfter
            // 
            this.con_DeleteAfter.Image = global::ScreenToGif.Properties.Resources.ArrowNext;
            this.con_DeleteAfter.Name = "con_DeleteAfter";
            this.con_DeleteAfter.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Right)));
            this.con_DeleteAfter.Size = new System.Drawing.Size(314, 24);
            this.con_DeleteAfter.Text = global::ScreenToGif.Properties.Resources.Context_DelAfter;
            this.con_DeleteAfter.Click += new System.EventHandler(this.con_DeleteAfter_Click);
            // 
            // con_DeleteBefore
            // 
            this.con_DeleteBefore.Image = global::ScreenToGif.Properties.Resources.ArrowPrev;
            this.con_DeleteBefore.Name = "con_DeleteBefore";
            this.con_DeleteBefore.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Left)));
            this.con_DeleteBefore.Size = new System.Drawing.Size(314, 24);
            this.con_DeleteBefore.Text = global::ScreenToGif.Properties.Resources.Context_DelBefore;
            this.con_DeleteBefore.Click += new System.EventHandler(this.con_DeleteBefore_Click);
            // 
            // con_DeleteThis
            // 
            this.con_DeleteThis.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.con_DeleteThis.Name = "con_DeleteThis";
            this.con_DeleteThis.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.con_DeleteThis.Size = new System.Drawing.Size(314, 24);
            this.con_DeleteThis.Text = global::ScreenToGif.Properties.Resources.Con_DeleteThis;
            this.con_DeleteThis.Click += new System.EventHandler(this.con_deleteThisFrame_Click);
            // 
            // trackBar
            // 
            this.trackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar.AutoSize = false;
            this.trackBar.Location = new System.Drawing.Point(12, 190);
            this.trackBar.Maximum = 40;
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(575, 25);
            this.trackBar.TabIndex = 0;
            this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
            this.trackBar.ValueChanged += new System.EventHandler(this.trackBar_ValueChanged);
            this.trackBar.Enter += new System.EventHandler(this.trackBar_Enter);
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
            this.timerPreStart.Tick += new System.EventHandler(this.PreStart_Tick);
            // 
            // flowPanel
            // 
            this.flowPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.flowPanel.Controls.Add(this.btnStop);
            this.flowPanel.Controls.Add(this.btnRecordPause);
            this.flowPanel.Controls.Add(this.tbHeight);
            this.flowPanel.Controls.Add(this.label5);
            this.flowPanel.Controls.Add(this.tbWidth);
            this.flowPanel.Controls.Add(this.lblSize);
            this.flowPanel.Controls.Add(this.numMaxFps);
            this.flowPanel.Controls.Add(this.label7);
            this.flowPanel.Controls.Add(this.pictureBox1);
            this.flowPanel.Controls.Add(this.btnConfig);
            this.flowPanel.Controls.Add(this.btnGifConfig);
            this.flowPanel.Controls.Add(this.btnInfo);
            this.flowPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowPanel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowPanel.Location = new System.Drawing.Point(0, 221);
            this.flowPanel.Name = "flowPanel";
            this.flowPanel.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.flowPanel.Size = new System.Drawing.Size(599, 31);
            this.flowPanel.TabIndex = 16;
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.AutoSize = true;
            this.btnStop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnStop.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Image = global::ScreenToGif.Properties.Resources.Stop_17Red;
            this.btnStop.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnStop.Location = new System.Drawing.Point(541, 0);
            this.btnStop.Margin = new System.Windows.Forms.Padding(0);
            this.btnStop.Name = "btnStop";
            this.btnStop.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.btnStop.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnStop.Size = new System.Drawing.Size(58, 31);
            this.btnStop.TabIndex = 22;
            this.btnStop.Text = global::ScreenToGif.Properties.Resources.btnStop;
            this.btnStop.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnRecordPause
            // 
            this.btnRecordPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRecordPause.AutoSize = true;
            this.btnRecordPause.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRecordPause.FlatAppearance.BorderSize = 0;
            this.btnRecordPause.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnRecordPause.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnRecordPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecordPause.Image = global::ScreenToGif.Properties.Resources.Record;
            this.btnRecordPause.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRecordPause.Location = new System.Drawing.Point(468, 0);
            this.btnRecordPause.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnRecordPause.Name = "btnRecordPause";
            this.btnRecordPause.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.btnRecordPause.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnRecordPause.Size = new System.Drawing.Size(70, 31);
            this.btnRecordPause.TabIndex = 25;
            this.btnRecordPause.Text = global::ScreenToGif.Properties.Resources.btnRecordPause_Record;
            this.btnRecordPause.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRecordPause.Click += new System.EventHandler(this.btnPauseRecord_Click);
            // 
            // tbHeight
            // 
            this.tbHeight.Location = new System.Drawing.Point(419, 4);
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
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(401, 8);
            this.label5.Margin = new System.Windows.Forms.Padding(1, 8, 0, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(14, 15);
            this.label5.TabIndex = 24;
            this.label5.Text = "X";
            // 
            // tbWidth
            // 
            this.tbWidth.Location = new System.Drawing.Point(352, 4);
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
            // lblSize
            // 
            this.lblSize.AutoSize = true;
            this.lblSize.Location = new System.Drawing.Point(321, 7);
            this.lblSize.Margin = new System.Windows.Forms.Padding(1, 7, 1, 3);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(27, 15);
            this.lblSize.TabIndex = 27;
            this.lblSize.Text = "Size";
            // 
            // numMaxFps
            // 
            this.numMaxFps.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.numMaxFps.AutoSize = true;
            this.numMaxFps.Location = new System.Drawing.Point(283, 4);
            this.numMaxFps.Margin = new System.Windows.Forms.Padding(2, 0, 3, 4);
            this.numMaxFps.Maximum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.numMaxFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxFps.Name = "numMaxFps";
            this.numMaxFps.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.numMaxFps.Size = new System.Drawing.Size(35, 23);
            this.numMaxFps.TabIndex = 21;
            this.toolTip.SetToolTip(this.numMaxFps, global::ScreenToGif.Properties.Resources.Tooltip_NumFPS);
            this.numMaxFps.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(253, 7);
            this.label7.Margin = new System.Windows.Forms.Padding(1, 7, 1, 3);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(26, 15);
            this.label7.TabIndex = 30;
            this.label7.Text = "FPS";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = global::ScreenToGif.Properties.Resources.Separator;
            this.pictureBox1.Location = new System.Drawing.Point(247, 5);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(2, 23);
            this.pictureBox1.TabIndex = 31;
            this.pictureBox1.TabStop = false;
            // 
            // btnConfig
            // 
            this.btnConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConfig.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnConfig.FlatAppearance.BorderSize = 0;
            this.btnConfig.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnConfig.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfig.Image = ((System.Drawing.Image)(resources.GetObject("btnConfig.Image")));
            this.btnConfig.Location = new System.Drawing.Point(220, 0);
            this.btnConfig.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnConfig.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnConfig.Size = new System.Drawing.Size(23, 31);
            this.btnConfig.TabIndex = 28;
            this.btnConfig.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip.SetToolTip(this.btnConfig, global::ScreenToGif.Properties.Resources.Tooltip_AppSettings);
            this.btnConfig.UseVisualStyleBackColor = true;
            this.btnConfig.Click += new System.EventHandler(this.btnConfig_Click);
            // 
            // btnGifConfig
            // 
            this.btnGifConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnGifConfig.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnGifConfig.FlatAppearance.BorderSize = 0;
            this.btnGifConfig.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnGifConfig.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnGifConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGifConfig.Image = global::ScreenToGif.Properties.Resources.Image_17;
            this.btnGifConfig.Location = new System.Drawing.Point(196, 0);
            this.btnGifConfig.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.btnGifConfig.Name = "btnGifConfig";
            this.btnGifConfig.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnGifConfig.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnGifConfig.Size = new System.Drawing.Size(22, 31);
            this.btnGifConfig.TabIndex = 32;
            this.btnGifConfig.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip.SetToolTip(this.btnGifConfig, global::ScreenToGif.Properties.Resources.Tooltip_GifSettings);
            this.btnGifConfig.UseVisualStyleBackColor = true;
            this.btnGifConfig.Click += new System.EventHandler(this.btnGifConfig_Click);
            // 
            // btnInfo
            // 
            this.btnInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInfo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnInfo.FlatAppearance.BorderSize = 0;
            this.btnInfo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnInfo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInfo.Image = global::ScreenToGif.Properties.Resources.Info_17Blue;
            this.btnInfo.Location = new System.Drawing.Point(171, 0);
            this.btnInfo.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnInfo.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnInfo.Size = new System.Drawing.Size(23, 31);
            this.btnInfo.TabIndex = 29;
            this.toolTip.SetToolTip(this.btnInfo, global::ScreenToGif.Properties.Resources.Tooltip_Info);
            this.btnInfo.UseCompatibleTextRendering = true;
            this.btnInfo.UseVisualStyleBackColor = true;
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
            // 
            // timerCapWithCursor
            // 
            this.timerCapWithCursor.Interval = 15;
            this.timerCapWithCursor.Tick += new System.EventHandler(this.timerCapWithCursor_Tick);
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 200;
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
            // contextDelay
            // 
            this.contextDelay.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextDelay.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.typeYouDesiredFrameDelayToolStripMenuItem,
            this.con_tbDelay,
            this.between10MsAnd1000MsToolStripMenuItem});
            this.contextDelay.Name = "contextDelay";
            this.contextDelay.Size = new System.Drawing.Size(273, 77);
            // 
            // typeYouDesiredFrameDelayToolStripMenuItem
            // 
            this.typeYouDesiredFrameDelayToolStripMenuItem.Enabled = false;
            this.typeYouDesiredFrameDelayToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Delay;
            this.typeYouDesiredFrameDelayToolStripMenuItem.Name = "typeYouDesiredFrameDelayToolStripMenuItem";
            this.typeYouDesiredFrameDelayToolStripMenuItem.Size = new System.Drawing.Size(272, 24);
            this.typeYouDesiredFrameDelayToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_DesiredFrameDelay;
            // 
            // con_tbDelay
            // 
            this.con_tbDelay.AutoCompleteCustomSource.AddRange(new string[] {
            "10",
            "50",
            "100",
            "150",
            "200",
            "250",
            "500",
            "1000"});
            this.con_tbDelay.MaxLength = 4;
            this.con_tbDelay.Name = "con_tbDelay";
            this.con_tbDelay.Size = new System.Drawing.Size(100, 23);
            this.con_tbDelay.Text = "66";
            this.con_tbDelay.KeyDown += new System.Windows.Forms.KeyEventHandler(this.con_tbDelay_KeyDown);
            this.con_tbDelay.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbSize_KeyPress);
            this.con_tbDelay.TextChanged += new System.EventHandler(this.con_tbDelay_TextChanged);
            // 
            // between10MsAnd1000MsToolStripMenuItem
            // 
            this.between10MsAnd1000MsToolStripMenuItem.Enabled = false;
            this.between10MsAnd1000MsToolStripMenuItem.Name = "between10MsAnd1000MsToolStripMenuItem";
            this.between10MsAnd1000MsToolStripMenuItem.Size = new System.Drawing.Size(272, 24);
            this.between10MsAnd1000MsToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Between10_1000;
            // 
            // contextSmall
            // 
            this.contextSmall.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextSmall.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem8});
            this.contextSmall.Name = "contextSmall";
            this.contextSmall.Size = new System.Drawing.Size(185, 52);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(184, 24);
            this.toolStripMenuItem1.Text = global::ScreenToGif.Properties.Resources.Con_FiltersAll;
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem2.Text = global::ScreenToGif.Properties.Resources.Con_FiltersGray;
            this.toolStripMenuItem2.Click += new System.EventHandler(this.GrayscaleAll_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem3.Text = global::ScreenToGif.Properties.Resources.Con_Filters_Pixelate;
            this.toolStripMenuItem3.Click += new System.EventHandler(this.PixelateAll_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem4.Text = global::ScreenToGif.Properties.Resources.Con_Blur;
            this.toolStripMenuItem4.Click += new System.EventHandler(this.BlurAll_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem5.Text = global::ScreenToGif.Properties.Resources.Con_Negative;
            this.toolStripMenuItem5.Click += new System.EventHandler(this.NegativeAll_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem6.Text = global::ScreenToGif.Properties.Resources.Con_Transparency;
            this.toolStripMenuItem6.Visible = false;
            this.toolStripMenuItem6.Click += new System.EventHandler(this.TransparencyAll_Click);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem7.Text = global::ScreenToGif.Properties.Resources.Con_Sepia;
            this.toolStripMenuItem7.Click += new System.EventHandler(this.sepiaToneAll_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem9,
            this.toolStripMenuItem10,
            this.toolStripMenuItem11,
            this.toolStripMenuItem12,
            this.toolStripMenuItem13,
            this.toolStripMenuItem14});
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(184, 24);
            this.toolStripMenuItem8.Text = global::ScreenToGif.Properties.Resources.Con_FiltersThis;
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem9.Text = global::ScreenToGif.Properties.Resources.Con_FiltersGray;
            this.toolStripMenuItem9.Click += new System.EventHandler(this.GrayscaleOne_Click);
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            this.toolStripMenuItem10.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem10.Text = global::ScreenToGif.Properties.Resources.Con_Filters_Pixelate;
            this.toolStripMenuItem10.Click += new System.EventHandler(this.PixelateOne_Click);
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            this.toolStripMenuItem11.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem11.Text = global::ScreenToGif.Properties.Resources.Con_Blur;
            this.toolStripMenuItem11.Click += new System.EventHandler(this.BlurOne_Click);
            // 
            // toolStripMenuItem12
            // 
            this.toolStripMenuItem12.Name = "toolStripMenuItem12";
            this.toolStripMenuItem12.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem12.Text = global::ScreenToGif.Properties.Resources.Con_Negative;
            this.toolStripMenuItem12.Click += new System.EventHandler(this.NegativeOne_Click);
            // 
            // toolStripMenuItem13
            // 
            this.toolStripMenuItem13.Name = "toolStripMenuItem13";
            this.toolStripMenuItem13.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem13.Text = global::ScreenToGif.Properties.Resources.Con_Transparency;
            this.toolStripMenuItem13.Visible = false;
            this.toolStripMenuItem13.Click += new System.EventHandler(this.TransparencyOne_Click);
            // 
            // toolStripMenuItem14
            // 
            this.toolStripMenuItem14.Name = "toolStripMenuItem14";
            this.toolStripMenuItem14.Size = new System.Drawing.Size(159, 24);
            this.toolStripMenuItem14.Text = global::ScreenToGif.Properties.Resources.Con_Sepia;
            this.toolStripMenuItem14.Click += new System.EventHandler(this.sepiaToneOne_Click);
            // 
            // Legacy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.ClientSize = new System.Drawing.Size(599, 252);
            this.Controls.Add(this.flowPanel);
            this.Controls.Add(this.panelEdit);
            this.Controls.Add(this.panelTransparent);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(100, 100);
            this.Name = "Legacy";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Screen To Gif";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.LimeGreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Principal_FormClosing);
            this.Resize += new System.EventHandler(this.Principal_Resize);
            this.panelEdit.ResumeLayout(false);
            this.panelEdit.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBitmap)).EndInit();
            this.contextMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.flowPanel.ResumeLayout(false);
            this.flowPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.contextDelay.ResumeLayout(false);
            this.contextDelay.PerformLayout();
            this.contextSmall.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTransparent;
        private System.Windows.Forms.Timer timerCapture;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.Timer timerPreStart;
        private System.Windows.Forms.Panel panelEdit;
        private System.Windows.Forms.TrackBar trackBar;
        private System.Windows.Forms.PictureBox pictureBitmap;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem con_DeleteAfter;
        private System.Windows.Forms.ToolStripMenuItem con_DeleteBefore;
        private System.Windows.Forms.FlowLayoutPanel flowPanel;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnRecordPause;
        private System.Windows.Forms.TextBox tbHeight;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbWidth;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.NumericUpDown numMaxFps;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnConfig;
        private System.Windows.Forms.Button btnGifConfig;
        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.Timer timerCapWithCursor;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolTip toolTipHelp;
        private System.Windows.Forms.ToolStripMenuItem addFrameItem;
        private System.Windows.Forms.ToolStripMenuItem imageItem;
        private System.Windows.Forms.ToolStripMenuItem exportFrameItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem con_DeleteThis;
        private System.Windows.Forms.ToolStripMenuItem editFrameItem;
        private System.Windows.Forms.ToolStripMenuItem resizeAllItem;
        private System.Windows.Forms.ToolStripMenuItem cropAllItem;
        private System.Windows.Forms.OpenFileDialog openImageDialog;
        private System.Windows.Forms.ToolStripMenuItem revertOrderItem;
        private System.Windows.Forms.ToolStripMenuItem yoyoItem;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnDeleteFrame;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Label lblDelay;
        private System.Windows.Forms.ContextMenuStrip contextDelay;
        private System.Windows.Forms.ToolStripMenuItem typeYouDesiredFrameDelayToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox con_tbDelay;
        private System.Windows.Forms.ToolStripMenuItem between10MsAnd1000MsToolStripMenuItem;
        private System.Windows.Forms.Button btnFilters;
        private System.Windows.Forms.ContextMenuStrip contextSmall;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem11;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem12;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem13;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem14;
        private System.Windows.Forms.Button btnOptions;
        private System.Windows.Forms.ToolStripMenuItem con_showGrid;
        private System.Windows.Forms.ToolStripMenuItem sloMotionItem;
        private System.Windows.Forms.ToolStripMenuItem titleImageItem;
        private System.Windows.Forms.ToolStripMenuItem con_addText;
        private System.Windows.Forms.ToolStripMenuItem addQuickCaptionToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox con_tbCaption;
        private System.Windows.Forms.ToolStripMenuItem con_typeEnter;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem15;
        private System.Windows.Forms.ToolStripTextBox con_tbNumFrames;
        private System.Windows.Forms.ToolStripMenuItem con_addCaption;
        private System.Windows.Forms.ToolStripMenuItem borderItem;

    }
}