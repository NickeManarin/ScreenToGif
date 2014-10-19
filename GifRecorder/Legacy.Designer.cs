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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Node1");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode(global::ScreenToGif.Properties.Resources.Label_All, new System.Windows.Forms.TreeNode[] {
            treeNode1});
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Legacy));
            this.panelTransparent = new System.Windows.Forms.Panel();
            this.panelEdit = new System.Windows.Forms.Panel();
            this.MainSplit = new System.Windows.Forms.SplitContainer();
            this.contextMenuTreeview = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.frameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_RenameFrame = new System.Windows.Forms.ToolStripMenuItem();
            this.con_MakeACopy = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.con_MoveUpwards = new System.Windows.Forms.ToolStripMenuItem();
            this.con_MoveDownwards = new System.Windows.Forms.ToolStripMenuItem();
            this.btnHideListFrames = new System.Windows.Forms.Button();
            this.RightSplit = new System.Windows.Forms.SplitContainer();
            this.btnShowListFrames = new System.Windows.Forms.Button();
            this.pictureBitmap = new System.Windows.Forms.PictureBox();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.con_addText = new System.Windows.Forms.ToolStripMenuItem();
            this.addQuickCaptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem15 = new System.Windows.Forms.ToolStripMenuItem();
            this.con_tbCaption = new System.Windows.Forms.ToolStripTextBox();
            this.con_CaptionOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.con_addCaption = new System.Windows.Forms.ToolStripMenuItem();
            this.addFrameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageItem = new System.Windows.Forms.ToolStripMenuItem();
            this.titleImageItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editFrameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resizeAllItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cropAllItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_flipRotate = new System.Windows.Forms.ToolStripMenuItem();
            this.con_flipVertical = new System.Windows.Forms.ToolStripMenuItem();
            this.con_flipHorizontal = new System.Windows.Forms.ToolStripMenuItem();
            this.rotate90ClockwiseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rotate90CounterclockwiseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rotate180ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.revertOrderItem = new System.Windows.Forms.ToolStripMenuItem();
            this.yoyoItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Transitions = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.changeSpeedItem = new System.Windows.Forms.ToolStripMenuItem();
            this.borderItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportFrameItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_showGrid = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.con_DeleteAfter = new System.Windows.Forms.ToolStripMenuItem();
            this.con_DeleteBefore = new System.Windows.Forms.ToolStripMenuItem();
            this.con_DeleteSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.panelTrack = new System.Windows.Forms.Panel();
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.flowPanelEditTop = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnDeleteFrame = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnFilters = new System.Windows.Forms.Button();
            this.btnOptions = new System.Windows.Forms.Button();
            this.btnPreview = new System.Windows.Forms.Button();
            this.lblDelay = new System.Windows.Forms.Label();
            this.timerCapture = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.timerPreStart = new System.Windows.Forms.Timer(this.components);
            this.panelBottom = new System.Windows.Forms.FlowLayoutPanel();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnRecordPause = new System.Windows.Forms.Button();
            this.contextRecord = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.recordingOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Fullscreen = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Snapshot = new System.Windows.Forms.ToolStripMenuItem();
            this.tbHeight = new System.Windows.Forms.TextBox();
            this.lblX = new System.Windows.Forms.Label();
            this.tbWidth = new System.Windows.Forms.TextBox();
            this.lblSize = new System.Windows.Forms.Label();
            this.numMaxFps = new System.Windows.Forms.NumericUpDown();
            this.lblFps = new System.Windows.Forms.Label();
            this.pbSeparator = new System.Windows.Forms.PictureBox();
            this.btnConfig = new System.Windows.Forms.CheckBox();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnPrevious = new System.Windows.Forms.Button();
            this.pbSeparator2 = new System.Windows.Forms.PictureBox();
            this.btnFreeDrawing = new System.Windows.Forms.Button();
            this.btnAddText = new System.Windows.Forms.Button();
            this.btnGifConfig = new System.Windows.Forms.CheckBox();
            this.btnInfo = new System.Windows.Forms.CheckBox();
            this.timerCapWithCursor = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.toolTipHelp = new System.Windows.Forms.ToolTip(this.components);
            this.openImageDialog = new System.Windows.Forms.OpenFileDialog();
            this.contextDelay = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.typeYouDesiredFrameDelayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.con_tbDelay = new System.Windows.Forms.ToolStripTextBox();
            this.between10MsAnd1000MsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextSmall = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.con_Pixelate = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Blur = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Negative = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Sepia = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Grayscale = new System.Windows.Forms.ToolStripMenuItem();
            this.con_Color = new System.Windows.Forms.ToolStripMenuItem();
            this.timerCaptureFull = new System.Windows.Forms.Timer(this.components);
            this.timerCapWithCursorFull = new System.Windows.Forms.Timer(this.components);
            this.tvFrames = new ScreenToGif.Controls.NoDoubleClickTreeView();
            this.panelEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).BeginInit();
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            this.contextMenuTreeview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RightSplit)).BeginInit();
            this.RightSplit.Panel1.SuspendLayout();
            this.RightSplit.Panel2.SuspendLayout();
            this.RightSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBitmap)).BeginInit();
            this.contextMenu.SuspendLayout();
            this.panelTrack.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.flowPanelEditTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.contextRecord.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator2)).BeginInit();
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
            this.panelTransparent.Size = new System.Drawing.Size(752, 312);
            this.panelTransparent.TabIndex = 1;
            // 
            // panelEdit
            // 
            this.panelEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.panelEdit.Controls.Add(this.MainSplit);
            this.panelEdit.Controls.Add(this.panelTrack);
            this.panelEdit.Controls.Add(this.flowPanelEditTop);
            this.panelEdit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.panelEdit.Location = new System.Drawing.Point(0, 0);
            this.panelEdit.Name = "panelEdit";
            this.panelEdit.Size = new System.Drawing.Size(752, 312);
            this.panelEdit.TabIndex = 15;
            this.panelEdit.Visible = false;
            // 
            // MainSplit
            // 
            this.MainSplit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainSplit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.MainSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainSplit.Location = new System.Drawing.Point(0, 34);
            this.MainSplit.Name = "MainSplit";
            // 
            // MainSplit.Panel1
            // 
            this.MainSplit.Panel1.Controls.Add(this.tvFrames);
            this.MainSplit.Panel1.Controls.Add(this.btnHideListFrames);
            // 
            // MainSplit.Panel2
            // 
            this.MainSplit.Panel2.Controls.Add(this.RightSplit);
            this.MainSplit.Size = new System.Drawing.Size(752, 245);
            this.MainSplit.SplitterDistance = 160;
            this.MainSplit.SplitterWidth = 1;
            this.MainSplit.TabIndex = 25;
            // 
            // contextMenuTreeview
            // 
            this.contextMenuTreeview.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextMenuTreeview.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.frameToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.con_RenameFrame,
            this.con_MakeACopy,
            this.toolStripSeparator4,
            this.con_MoveUpwards,
            this.con_MoveDownwards});
            this.contextMenuTreeview.Name = "contextMenuTreeview";
            this.contextMenuTreeview.Size = new System.Drawing.Size(267, 154);
            this.contextMenuTreeview.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuTreeview_Opening);
            // 
            // frameToolStripMenuItem
            // 
            this.frameToolStripMenuItem.Enabled = false;
            this.frameToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Frame1;
            this.frameToolStripMenuItem.Name = "frameToolStripMenuItem";
            this.frameToolStripMenuItem.Size = new System.Drawing.Size(266, 24);
            this.frameToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_ThisFrame;
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(266, 24);
            this.deleteToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Delete;
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.btnDeleteFrame_Click);
            // 
            // con_RenameFrame
            // 
            this.con_RenameFrame.Image = global::ScreenToGif.Properties.Resources.Rename;
            this.con_RenameFrame.Name = "con_RenameFrame";
            this.con_RenameFrame.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.con_RenameFrame.Size = new System.Drawing.Size(266, 24);
            this.con_RenameFrame.Text = global::ScreenToGif.Properties.Resources.Con_Rename;
            this.con_RenameFrame.Click += new System.EventHandler(this.con_RenameFrame_Click);
            // 
            // con_MakeACopy
            // 
            this.con_MakeACopy.Image = global::ScreenToGif.Properties.Resources.Copy;
            this.con_MakeACopy.Name = "con_MakeACopy";
            this.con_MakeACopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.con_MakeACopy.Size = new System.Drawing.Size(266, 24);
            this.con_MakeACopy.Text = global::ScreenToGif.Properties.Resources.Con_MakeCopy;
            this.con_MakeACopy.Click += new System.EventHandler(this.con_MakeACopy_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(263, 6);
            // 
            // con_MoveUpwards
            // 
            this.con_MoveUpwards.Image = global::ScreenToGif.Properties.Resources.upArrow;
            this.con_MoveUpwards.Name = "con_MoveUpwards";
            this.con_MoveUpwards.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
            this.con_MoveUpwards.Size = new System.Drawing.Size(266, 24);
            this.con_MoveUpwards.Text = global::ScreenToGif.Properties.Resources.Con_MoveUp;
            this.con_MoveUpwards.Click += new System.EventHandler(this.con_MoveUpwards_Click);
            // 
            // con_MoveDownwards
            // 
            this.con_MoveDownwards.Image = global::ScreenToGif.Properties.Resources.downArrow;
            this.con_MoveDownwards.Name = "con_MoveDownwards";
            this.con_MoveDownwards.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Down)));
            this.con_MoveDownwards.Size = new System.Drawing.Size(266, 24);
            this.con_MoveDownwards.Text = global::ScreenToGif.Properties.Resources.Con_MoveDown;
            this.con_MoveDownwards.Click += new System.EventHandler(this.con_MoveDownwards_Click);
            // 
            // btnHideListFrames
            // 
            this.btnHideListFrames.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnHideListFrames.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnHideListFrames.FlatAppearance.BorderSize = 0;
            this.btnHideListFrames.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHideListFrames.Image = global::ScreenToGif.Properties.Resources.ArrowToHide;
            this.btnHideListFrames.Location = new System.Drawing.Point(136, 72);
            this.btnHideListFrames.Name = "btnHideListFrames";
            this.btnHideListFrames.Size = new System.Drawing.Size(22, 100);
            this.btnHideListFrames.TabIndex = 23;
            this.btnHideListFrames.TabStop = false;
            this.toolTip.SetToolTip(this.btnHideListFrames, global::ScreenToGif.Properties.Resources.Tooltip_HideList);
            this.btnHideListFrames.UseVisualStyleBackColor = true;
            this.btnHideListFrames.Click += new System.EventHandler(this.btnHideListFrames_Click);
            // 
            // RightSplit
            // 
            this.RightSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.RightSplit.IsSplitterFixed = true;
            this.RightSplit.Location = new System.Drawing.Point(0, 0);
            this.RightSplit.Name = "RightSplit";
            // 
            // RightSplit.Panel1
            // 
            this.RightSplit.Panel1.Controls.Add(this.btnShowListFrames);
            this.RightSplit.Panel1MinSize = 16;
            // 
            // RightSplit.Panel2
            // 
            this.RightSplit.Panel2.Controls.Add(this.pictureBitmap);
            this.RightSplit.Panel2MinSize = 14;
            this.RightSplit.Size = new System.Drawing.Size(591, 245);
            this.RightSplit.SplitterDistance = 25;
            this.RightSplit.SplitterWidth = 1;
            this.RightSplit.TabIndex = 23;
            this.RightSplit.TabStop = false;
            // 
            // btnShowListFrames
            // 
            this.btnShowListFrames.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnShowListFrames.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnShowListFrames.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnShowListFrames.FlatAppearance.BorderSize = 0;
            this.btnShowListFrames.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnShowListFrames.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnShowListFrames.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnShowListFrames.Image = global::ScreenToGif.Properties.Resources.ArrowToShow;
            this.btnShowListFrames.Location = new System.Drawing.Point(2, 72);
            this.btnShowListFrames.Name = "btnShowListFrames";
            this.btnShowListFrames.Size = new System.Drawing.Size(21, 100);
            this.btnShowListFrames.TabIndex = 24;
            this.btnShowListFrames.TabStop = false;
            this.toolTip.SetToolTip(this.btnShowListFrames, global::ScreenToGif.Properties.Resources.Tooltip_ShowList);
            this.btnShowListFrames.UseVisualStyleBackColor = true;
            this.btnShowListFrames.Click += new System.EventHandler(this.btnShowListFrames_Click);
            // 
            // pictureBitmap
            // 
            this.pictureBitmap.BackColor = System.Drawing.Color.Transparent;
            this.pictureBitmap.ContextMenuStrip = this.contextMenu;
            this.pictureBitmap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBitmap.Location = new System.Drawing.Point(0, 0);
            this.pictureBitmap.Margin = new System.Windows.Forms.Padding(9);
            this.pictureBitmap.Name = "pictureBitmap";
            this.pictureBitmap.Size = new System.Drawing.Size(565, 245);
            this.pictureBitmap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBitmap.TabIndex = 1;
            this.pictureBitmap.TabStop = false;
            this.pictureBitmap.DragDrop += new System.Windows.Forms.DragEventHandler(this.pictureBitmap_DragDrop);
            this.pictureBitmap.DragEnter += new System.Windows.Forms.DragEventHandler(this.pictureBitmap_DragEnter);
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
            this.con_DeleteSelected});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.contextMenu.ShowItemToolTips = false;
            this.contextMenu.Size = new System.Drawing.Size(315, 226);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            // 
            // con_addText
            // 
            this.con_addText.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_addText.Image = global::ScreenToGif.Properties.Resources.Text1;
            this.con_addText.Name = "con_addText";
            this.con_addText.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.con_addText.Size = new System.Drawing.Size(314, 24);
            this.con_addText.Text = global::ScreenToGif.Properties.Resources.Con_AddText;
            this.con_addText.Click += new System.EventHandler(this.con_addText_Click);
            // 
            // addQuickCaptionToolStripMenuItem
            // 
            this.addQuickCaptionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem15,
            this.con_tbCaption,
            this.con_CaptionOptions,
            this.toolStripSeparator2,
            this.con_addCaption});
            this.addQuickCaptionToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.addQuickCaptionToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Comment1;
            this.addQuickCaptionToolStripMenuItem.Name = "addQuickCaptionToolStripMenuItem";
            this.addQuickCaptionToolStripMenuItem.Size = new System.Drawing.Size(314, 24);
            this.addQuickCaptionToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_AddQuickCaption;
            // 
            // toolStripMenuItem15
            // 
            this.toolStripMenuItem15.Enabled = false;
            this.toolStripMenuItem15.Image = global::ScreenToGif.Properties.Resources.Comment;
            this.toolStripMenuItem15.Name = "toolStripMenuItem15";
            this.toolStripMenuItem15.Size = new System.Drawing.Size(310, 24);
            this.toolStripMenuItem15.Text = global::ScreenToGif.Properties.Resources.Con_Text;
            // 
            // con_tbCaption
            // 
            this.con_tbCaption.AcceptsReturn = true;
            this.con_tbCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.con_tbCaption.CausesValidation = false;
            this.con_tbCaption.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_tbCaption.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
            this.con_tbCaption.MaxLength = 255;
            this.con_tbCaption.Name = "con_tbCaption";
            this.con_tbCaption.Size = new System.Drawing.Size(250, 25);
            this.con_tbCaption.KeyDown += new System.Windows.Forms.KeyEventHandler(this.con_tbCaption_KeyDown);
            // 
            // con_CaptionOptions
            // 
            this.con_CaptionOptions.ForeColor = System.Drawing.Color.Black;
            this.con_CaptionOptions.Image = global::ScreenToGif.Properties.Resources.Text1;
            this.con_CaptionOptions.Name = "con_CaptionOptions";
            this.con_CaptionOptions.Size = new System.Drawing.Size(310, 24);
            this.con_CaptionOptions.Text = global::ScreenToGif.Properties.Resources.Con_CaptionOptions;
            this.con_CaptionOptions.Click += new System.EventHandler(this.con_captionOptions_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(307, 6);
            // 
            // con_addCaption
            // 
            this.con_addCaption.Font = new System.Drawing.Font("Segoe UI", 10.75F);
            this.con_addCaption.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.con_addCaption.Name = "con_addCaption";
            this.con_addCaption.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.con_addCaption.Size = new System.Drawing.Size(310, 24);
            this.con_addCaption.Text = global::ScreenToGif.Properties.Resources.Con_Done;
            this.con_addCaption.Click += new System.EventHandler(this.con_addCaption_Click);
            // 
            // addFrameItem
            // 
            this.addFrameItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.imageItem,
            this.titleImageItem});
            this.addFrameItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.addFrameItem.Image = global::ScreenToGif.Properties.Resources.add;
            this.addFrameItem.Name = "addFrameItem";
            this.addFrameItem.Size = new System.Drawing.Size(314, 24);
            this.addFrameItem.Text = global::ScreenToGif.Properties.Resources.Con_AddFrame;
            // 
            // imageItem
            // 
            this.imageItem.Image = global::ScreenToGif.Properties.Resources.Image_17;
            this.imageItem.Name = "imageItem";
            this.imageItem.Size = new System.Drawing.Size(145, 24);
            this.imageItem.Text = global::ScreenToGif.Properties.Resources.Con_Image;
            this.imageItem.Click += new System.EventHandler(this.con_image_Click);
            // 
            // titleImageItem
            // 
            this.titleImageItem.Image = global::ScreenToGif.Properties.Resources.Text1;
            this.titleImageItem.Name = "titleImageItem";
            this.titleImageItem.Size = new System.Drawing.Size(145, 24);
            this.titleImageItem.Text = global::ScreenToGif.Properties.Resources.Title_TitleFrame;
            this.titleImageItem.Click += new System.EventHandler(this.con_titleImage_Click);
            // 
            // editFrameItem
            // 
            this.editFrameItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resizeAllItem,
            this.cropAllItem,
            this.con_flipRotate,
            this.revertOrderItem,
            this.yoyoItem,
            this.con_Transitions,
            this.toolStripSeparator3,
            this.changeSpeedItem,
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
            this.resizeAllItem.Size = new System.Drawing.Size(166, 24);
            this.resizeAllItem.Text = global::ScreenToGif.Properties.Resources.Con_ResizeAll;
            this.resizeAllItem.Click += new System.EventHandler(this.con_resizeAllFrames_Click);
            // 
            // cropAllItem
            // 
            this.cropAllItem.Image = global::ScreenToGif.Properties.Resources.crop;
            this.cropAllItem.Name = "cropAllItem";
            this.cropAllItem.Size = new System.Drawing.Size(166, 24);
            this.cropAllItem.Text = global::ScreenToGif.Properties.Resources.Con_CropAll;
            this.cropAllItem.Click += new System.EventHandler(this.con_cropAll_Click);
            // 
            // con_flipRotate
            // 
            this.con_flipRotate.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.con_flipVertical,
            this.con_flipHorizontal,
            this.rotate90ClockwiseToolStripMenuItem,
            this.rotate90CounterclockwiseToolStripMenuItem,
            this.rotate180ToolStripMenuItem});
            this.con_flipRotate.Image = global::ScreenToGif.Properties.Resources.flipRotate;
            this.con_flipRotate.Name = "con_flipRotate";
            this.con_flipRotate.Size = new System.Drawing.Size(166, 24);
            this.con_flipRotate.Text = global::ScreenToGif.Properties.Resources.Con_FlipRotate;
            // 
            // con_flipVertical
            // 
            this.con_flipVertical.AccessibleDescription = "Vertical";
            this.con_flipVertical.Image = global::ScreenToGif.Properties.Resources.FlipVertical;
            this.con_flipVertical.Name = "con_flipVertical";
            this.con_flipVertical.Size = new System.Drawing.Size(253, 24);
            this.con_flipVertical.Text = global::ScreenToGif.Properties.Resources.Con_FlipVertical;
            this.con_flipVertical.Click += new System.EventHandler(this.con_flipRotate_Click);
            // 
            // con_flipHorizontal
            // 
            this.con_flipHorizontal.AccessibleDescription = "Horizontal";
            this.con_flipHorizontal.Image = global::ScreenToGif.Properties.Resources.FlipHorizontal;
            this.con_flipHorizontal.Name = "con_flipHorizontal";
            this.con_flipHorizontal.Size = new System.Drawing.Size(253, 24);
            this.con_flipHorizontal.Text = global::ScreenToGif.Properties.Resources.Con_FlipHorizontal;
            this.con_flipHorizontal.Click += new System.EventHandler(this.con_flipRotate_Click);
            // 
            // rotate90ClockwiseToolStripMenuItem
            // 
            this.rotate90ClockwiseToolStripMenuItem.AccessibleDescription = "90C";
            this.rotate90ClockwiseToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.RotateRight;
            this.rotate90ClockwiseToolStripMenuItem.Name = "rotate90ClockwiseToolStripMenuItem";
            this.rotate90ClockwiseToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.rotate90ClockwiseToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Rotate90C;
            this.rotate90ClockwiseToolStripMenuItem.Click += new System.EventHandler(this.con_flipRotate_Click);
            // 
            // rotate90CounterclockwiseToolStripMenuItem
            // 
            this.rotate90CounterclockwiseToolStripMenuItem.AccessibleDescription = "90CC";
            this.rotate90CounterclockwiseToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.RotateLeft;
            this.rotate90CounterclockwiseToolStripMenuItem.Name = "rotate90CounterclockwiseToolStripMenuItem";
            this.rotate90CounterclockwiseToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.rotate90CounterclockwiseToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Rotate90CC;
            this.rotate90CounterclockwiseToolStripMenuItem.Click += new System.EventHandler(this.con_flipRotate_Click);
            // 
            // rotate180ToolStripMenuItem
            // 
            this.rotate180ToolStripMenuItem.AccessibleDescription = "180";
            this.rotate180ToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Rotate180;
            this.rotate180ToolStripMenuItem.Name = "rotate180ToolStripMenuItem";
            this.rotate180ToolStripMenuItem.Size = new System.Drawing.Size(253, 24);
            this.rotate180ToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Rotate180;
            this.rotate180ToolStripMenuItem.Click += new System.EventHandler(this.con_flipRotate_Click);
            // 
            // revertOrderItem
            // 
            this.revertOrderItem.Image = global::ScreenToGif.Properties.Resources.revert;
            this.revertOrderItem.Name = "revertOrderItem";
            this.revertOrderItem.Size = new System.Drawing.Size(166, 24);
            this.revertOrderItem.Text = global::ScreenToGif.Properties.Resources.Con_Revert;
            this.revertOrderItem.Click += new System.EventHandler(this.con_revertOrder_Click);
            // 
            // yoyoItem
            // 
            this.yoyoItem.Image = global::ScreenToGif.Properties.Resources.Yoyo;
            this.yoyoItem.Name = "yoyoItem";
            this.yoyoItem.Size = new System.Drawing.Size(166, 24);
            this.yoyoItem.Text = global::ScreenToGif.Properties.Resources.Con_Yoyo;
            this.yoyoItem.Click += new System.EventHandler(this.con_yoyo_Click);
            // 
            // con_Transitions
            // 
            this.con_Transitions.Image = global::ScreenToGif.Properties.Resources.Transparent16x;
            this.con_Transitions.Name = "con_Transitions";
            this.con_Transitions.Size = new System.Drawing.Size(166, 24);
            this.con_Transitions.Text = "Transitions...";
            this.con_Transitions.Click += new System.EventHandler(this.con_Transitions_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(163, 6);
            // 
            // changeSpeedItem
            // 
            this.changeSpeedItem.Image = global::ScreenToGif.Properties.Resources.Delay;
            this.changeSpeedItem.Name = "changeSpeedItem";
            this.changeSpeedItem.Size = new System.Drawing.Size(166, 24);
            this.changeSpeedItem.Text = global::ScreenToGif.Properties.Resources.Con_ChangeSpeed;
            this.changeSpeedItem.Click += new System.EventHandler(this.con_changeSpeed_Click);
            // 
            // borderItem
            // 
            this.borderItem.Image = global::ScreenToGif.Properties.Resources.border;
            this.borderItem.Name = "borderItem";
            this.borderItem.Size = new System.Drawing.Size(166, 24);
            this.borderItem.Text = global::ScreenToGif.Properties.Resources.Con_AddBorder;
            this.borderItem.Click += new System.EventHandler(this.con_Border_Click);
            // 
            // exportFrameItem
            // 
            this.exportFrameItem.Image = global::ScreenToGif.Properties.Resources.Export;
            this.exportFrameItem.Name = "exportFrameItem";
            this.exportFrameItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.exportFrameItem.Size = new System.Drawing.Size(314, 24);
            this.exportFrameItem.Text = global::ScreenToGif.Properties.Resources.Con_ExportFrame;
            this.exportFrameItem.Click += new System.EventHandler(this.con_exportFrames_Click);
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
            this.con_DeleteAfter.Image = global::ScreenToGif.Properties.Resources.ArrowNext2;
            this.con_DeleteAfter.Name = "con_DeleteAfter";
            this.con_DeleteAfter.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Right)));
            this.con_DeleteAfter.Size = new System.Drawing.Size(314, 24);
            this.con_DeleteAfter.Text = global::ScreenToGif.Properties.Resources.Context_DelAfter;
            this.con_DeleteAfter.Click += new System.EventHandler(this.con_deleteAfter_Click);
            // 
            // con_DeleteBefore
            // 
            this.con_DeleteBefore.Image = global::ScreenToGif.Properties.Resources.ArrowPrev2;
            this.con_DeleteBefore.Name = "con_DeleteBefore";
            this.con_DeleteBefore.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Left)));
            this.con_DeleteBefore.Size = new System.Drawing.Size(314, 24);
            this.con_DeleteBefore.Text = global::ScreenToGif.Properties.Resources.Context_DelBefore;
            this.con_DeleteBefore.Click += new System.EventHandler(this.con_deleteBefore_Click);
            // 
            // con_DeleteSelected
            // 
            this.con_DeleteSelected.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.con_DeleteSelected.Name = "con_DeleteSelected";
            this.con_DeleteSelected.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.con_DeleteSelected.Size = new System.Drawing.Size(314, 24);
            this.con_DeleteSelected.Text = global::ScreenToGif.Properties.Resources.Con_DeleteSelected;
            this.con_DeleteSelected.Click += new System.EventHandler(this.con_deleteSelectedFrame_Click);
            // 
            // panelTrack
            // 
            this.panelTrack.Controls.Add(this.trackBar);
            this.panelTrack.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelTrack.Location = new System.Drawing.Point(0, 278);
            this.panelTrack.Name = "panelTrack";
            this.panelTrack.Size = new System.Drawing.Size(752, 34);
            this.panelTrack.TabIndex = 21;
            // 
            // trackBar
            // 
            this.trackBar.AutoSize = false;
            this.trackBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.trackBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackBar.Location = new System.Drawing.Point(0, 0);
            this.trackBar.Maximum = 40;
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(752, 34);
            this.trackBar.TabIndex = 0;
            this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
            this.trackBar.ValueChanged += new System.EventHandler(this.trackBar_ValueChanged);
            // 
            // flowPanelEditTop
            // 
            this.flowPanelEditTop.Controls.Add(this.btnCancel);
            this.flowPanelEditTop.Controls.Add(this.btnDone);
            this.flowPanelEditTop.Controls.Add(this.btnDeleteFrame);
            this.flowPanelEditTop.Controls.Add(this.btnUndo);
            this.flowPanelEditTop.Controls.Add(this.btnReset);
            this.flowPanelEditTop.Controls.Add(this.btnFilters);
            this.flowPanelEditTop.Controls.Add(this.btnOptions);
            this.flowPanelEditTop.Controls.Add(this.btnPreview);
            this.flowPanelEditTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowPanelEditTop.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowPanelEditTop.Location = new System.Drawing.Point(0, 0);
            this.flowPanelEditTop.Name = "flowPanelEditTop";
            this.flowPanelEditTop.Size = new System.Drawing.Size(752, 35);
            this.flowPanelEditTop.TabIndex = 19;
            this.flowPanelEditTop.TabStop = true;
            this.flowPanelEditTop.WrapContents = false;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(683, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnCancel.Size = new System.Drawing.Size(69, 33);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.TabStop = false;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.btnCancel, "Shortcut: Shift + Esc");
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
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
            this.btnDone.Location = new System.Drawing.Point(622, 0);
            this.btnDone.Margin = new System.Windows.Forms.Padding(0);
            this.btnDone.Name = "btnDone";
            this.btnDone.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDone.Size = new System.Drawing.Size(61, 33);
            this.btnDone.TabIndex = 1;
            this.btnDone.TabStop = false;
            this.btnDone.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnDone.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.btnDone, "Shortcut: Shift + Enter");
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // btnDeleteFrame
            // 
            this.btnDeleteFrame.AutoSize = true;
            this.btnDeleteFrame.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDeleteFrame.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnDeleteFrame.FlatAppearance.BorderSize = 0;
            this.btnDeleteFrame.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnDeleteFrame.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnDeleteFrame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeleteFrame.Image = global::ScreenToGif.Properties.Resources.Remove;
            this.btnDeleteFrame.Location = new System.Drawing.Point(520, 0);
            this.btnDeleteFrame.Margin = new System.Windows.Forms.Padding(0);
            this.btnDeleteFrame.Name = "btnDeleteFrame";
            this.btnDeleteFrame.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnDeleteFrame.Size = new System.Drawing.Size(102, 33);
            this.btnDeleteFrame.TabIndex = 2;
            this.btnDeleteFrame.TabStop = false;
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
            this.btnUndo.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnUndo.FlatAppearance.BorderSize = 0;
            this.btnUndo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnUndo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUndo.Image = global::ScreenToGif.Properties.Resources.Undo;
            this.btnUndo.Location = new System.Drawing.Point(458, 0);
            this.btnUndo.Margin = new System.Windows.Forms.Padding(0);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnUndo.Size = new System.Drawing.Size(62, 33);
            this.btnUndo.TabIndex = 3;
            this.btnUndo.TabStop = false;
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
            this.btnReset.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnReset.FlatAppearance.BorderSize = 0;
            this.btnReset.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnReset.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.btnReset.Location = new System.Drawing.Point(397, 0);
            this.btnReset.Margin = new System.Windows.Forms.Padding(0);
            this.btnReset.Name = "btnReset";
            this.btnReset.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnReset.Size = new System.Drawing.Size(61, 33);
            this.btnReset.TabIndex = 4;
            this.btnReset.TabStop = false;
            this.btnReset.Text = global::ScreenToGif.Properties.Resources.btnReset;
            this.btnReset.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnFilters
            // 
            this.btnFilters.AutoSize = true;
            this.btnFilters.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnFilters.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnFilters.FlatAppearance.BorderSize = 0;
            this.btnFilters.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnFilters.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnFilters.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFilters.Image = global::ScreenToGif.Properties.Resources.filters;
            this.btnFilters.Location = new System.Drawing.Point(333, 0);
            this.btnFilters.Margin = new System.Windows.Forms.Padding(0);
            this.btnFilters.Name = "btnFilters";
            this.btnFilters.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnFilters.Size = new System.Drawing.Size(64, 33);
            this.btnFilters.TabIndex = 7;
            this.btnFilters.TabStop = false;
            this.btnFilters.Text = global::ScreenToGif.Properties.Resources.Title_Filters;
            this.btnFilters.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnFilters.UseVisualStyleBackColor = true;
            this.btnFilters.Click += new System.EventHandler(this.btnFilters_Click);
            // 
            // btnOptions
            // 
            this.btnOptions.AutoSize = true;
            this.btnOptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnOptions.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnOptions.FlatAppearance.BorderSize = 0;
            this.btnOptions.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOptions.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOptions.Image = global::ScreenToGif.Properties.Resources.add;
            this.btnOptions.Location = new System.Drawing.Point(258, 0);
            this.btnOptions.Margin = new System.Windows.Forms.Padding(0);
            this.btnOptions.Name = "btnOptions";
            this.btnOptions.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnOptions.Size = new System.Drawing.Size(75, 33);
            this.btnOptions.TabIndex = 8;
            this.btnOptions.TabStop = false;
            this.btnOptions.Text = global::ScreenToGif.Properties.Resources.btnOptions;
            this.btnOptions.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOptions.UseVisualStyleBackColor = true;
            this.btnOptions.Click += new System.EventHandler(this.cbOptions_Click);
            // 
            // btnPreview
            // 
            this.btnPreview.AutoSize = true;
            this.btnPreview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPreview.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnPreview.FlatAppearance.BorderSize = 0;
            this.btnPreview.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnPreview.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.Image = global::ScreenToGif.Properties.Resources.Play_17Green;
            this.btnPreview.Location = new System.Drawing.Point(158, 0);
            this.btnPreview.Margin = new System.Windows.Forms.Padding(0);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnPreview.Size = new System.Drawing.Size(100, 33);
            this.btnPreview.TabIndex = 5;
            this.btnPreview.TabStop = false;
            this.btnPreview.Text = global::ScreenToGif.Properties.Resources.Con_PlayPreview;
            this.btnPreview.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBitmap_MouseClick);
            // 
            // lblDelay
            // 
            this.lblDelay.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDelay.AutoSize = true;
            this.lblDelay.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.lblDelay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblDelay.CausesValidation = false;
            this.lblDelay.Cursor = System.Windows.Forms.Cursors.SizeNS;
            this.lblDelay.Location = new System.Drawing.Point(18, 7);
            this.lblDelay.Name = "lblDelay";
            this.lblDelay.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblDelay.Size = new System.Drawing.Size(40, 17);
            this.lblDelay.TabIndex = 20;
            this.lblDelay.Text = "66 ms";
            this.lblDelay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.lblDelay, "Frame delay. Right click to set number.");
            this.lblDelay.UseMnemonic = false;
            this.lblDelay.Visible = false;
            this.lblDelay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lblDelay_MouseDown);
            this.lblDelay.MouseMove += new System.Windows.Forms.MouseEventHandler(this.lblDelay_MouseMove);
            this.lblDelay.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lblDelay_MouseUp);
            // 
            // timerCapture
            // 
            this.timerCapture.Interval = 66;
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
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.panelBottom.Controls.Add(this.btnStop);
            this.panelBottom.Controls.Add(this.btnRecordPause);
            this.panelBottom.Controls.Add(this.tbHeight);
            this.panelBottom.Controls.Add(this.lblX);
            this.panelBottom.Controls.Add(this.tbWidth);
            this.panelBottom.Controls.Add(this.lblSize);
            this.panelBottom.Controls.Add(this.numMaxFps);
            this.panelBottom.Controls.Add(this.lblFps);
            this.panelBottom.Controls.Add(this.pbSeparator);
            this.panelBottom.Controls.Add(this.btnConfig);
            this.panelBottom.Controls.Add(this.btnNext);
            this.panelBottom.Controls.Add(this.btnPrevious);
            this.panelBottom.Controls.Add(this.pbSeparator2);
            this.panelBottom.Controls.Add(this.btnFreeDrawing);
            this.panelBottom.Controls.Add(this.btnAddText);
            this.panelBottom.Controls.Add(this.btnGifConfig);
            this.panelBottom.Controls.Add(this.btnInfo);
            this.panelBottom.Controls.Add(this.lblDelay);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panelBottom.Location = new System.Drawing.Point(0, 313);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.panelBottom.Size = new System.Drawing.Size(752, 31);
            this.panelBottom.TabIndex = 16;
            this.panelBottom.TabStop = true;
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.AutoSize = true;
            this.btnStop.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnStop.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnStop.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Image = global::ScreenToGif.Properties.Resources.Stop_17Red;
            this.btnStop.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnStop.Location = new System.Drawing.Point(694, 0);
            this.btnStop.Margin = new System.Windows.Forms.Padding(0);
            this.btnStop.Name = "btnStop";
            this.btnStop.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.btnStop.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnStop.Size = new System.Drawing.Size(58, 31);
            this.btnStop.TabIndex = 22;
            this.btnStop.TabStop = false;
            this.btnStop.Text = global::ScreenToGif.Properties.Resources.btnStop;
            this.btnStop.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnRecordPause
            // 
            this.btnRecordPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRecordPause.AutoSize = true;
            this.btnRecordPause.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnRecordPause.ContextMenuStrip = this.contextRecord;
            this.btnRecordPause.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnRecordPause.FlatAppearance.BorderSize = 0;
            this.btnRecordPause.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnRecordPause.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnRecordPause.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecordPause.Image = global::ScreenToGif.Properties.Resources.Record;
            this.btnRecordPause.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRecordPause.Location = new System.Drawing.Point(621, 0);
            this.btnRecordPause.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnRecordPause.Name = "btnRecordPause";
            this.btnRecordPause.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.btnRecordPause.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnRecordPause.Size = new System.Drawing.Size(70, 31);
            this.btnRecordPause.TabIndex = 25;
            this.btnRecordPause.TabStop = false;
            this.btnRecordPause.Text = global::ScreenToGif.Properties.Resources.btnRecordPause_Record;
            this.btnRecordPause.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnRecordPause.Click += new System.EventHandler(this.btnRecordPause_Click);
            // 
            // contextRecord
            // 
            this.contextRecord.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextRecord.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.recordingOptionsToolStripMenuItem,
            this.con_Fullscreen,
            this.con_Snapshot});
            this.contextRecord.Name = "contextRecord";
            this.contextRecord.Size = new System.Drawing.Size(204, 76);
            this.contextRecord.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.contextRecord_Closing);
            this.contextRecord.Opening += new System.ComponentModel.CancelEventHandler(this.contextRecord_Opening);
            // 
            // recordingOptionsToolStripMenuItem
            // 
            this.recordingOptionsToolStripMenuItem.Enabled = false;
            this.recordingOptionsToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.recordingOptionsToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Record;
            this.recordingOptionsToolStripMenuItem.Name = "recordingOptionsToolStripMenuItem";
            this.recordingOptionsToolStripMenuItem.Size = new System.Drawing.Size(203, 24);
            this.recordingOptionsToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_RecordingOptions;
            // 
            // con_Fullscreen
            // 
            this.con_Fullscreen.CheckOnClick = true;
            this.con_Fullscreen.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Fullscreen.Image = global::ScreenToGif.Properties.Resources.Fullscreen16x;
            this.con_Fullscreen.Name = "con_Fullscreen";
            this.con_Fullscreen.Size = new System.Drawing.Size(203, 24);
            this.con_Fullscreen.Text = global::ScreenToGif.Properties.Resources.Con_FullscreenRecording;
            this.con_Fullscreen.CheckedChanged += new System.EventHandler(this.con_Fullscreen_CheckedChanged);
            // 
            // con_Snapshot
            // 
            this.con_Snapshot.CheckOnClick = true;
            this.con_Snapshot.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Snapshot.Image = global::ScreenToGif.Properties.Resources.Snap16x;
            this.con_Snapshot.Name = "con_Snapshot";
            this.con_Snapshot.ShortcutKeyDisplayString = "";
            this.con_Snapshot.Size = new System.Drawing.Size(203, 24);
            this.con_Snapshot.Text = global::ScreenToGif.Properties.Resources.Con_SnapshotMode;
            this.con_Snapshot.CheckedChanged += new System.EventHandler(this.con_Snapshot_CheckedChanged);
            // 
            // tbHeight
            // 
            this.tbHeight.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.tbHeight.Location = new System.Drawing.Point(583, 4);
            this.tbHeight.Margin = new System.Windows.Forms.Padding(2, 4, 1, 4);
            this.tbHeight.Name = "tbHeight";
            this.tbHeight.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbHeight.Size = new System.Drawing.Size(36, 23);
            this.tbHeight.TabIndex = 23;
            this.tbHeight.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip.SetToolTip(this.tbHeight, global::ScreenToGif.Properties.Resources.Tooltip_Height);
            this.tbHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbSize_KeyDown);
            this.tbHeight.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbSize_KeyPress);
            this.tbHeight.Leave += new System.EventHandler(this.tbSize_Leave);
            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(568, 8);
            this.lblX.Margin = new System.Windows.Forms.Padding(0, 8, 0, 3);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(14, 15);
            this.lblX.TabIndex = 24;
            this.lblX.Text = "X";
            // 
            // tbWidth
            // 
            this.tbWidth.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.tbWidth.Location = new System.Drawing.Point(530, 4);
            this.tbWidth.Margin = new System.Windows.Forms.Padding(2, 4, 2, 4);
            this.tbWidth.Name = "tbWidth";
            this.tbWidth.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tbWidth.Size = new System.Drawing.Size(36, 23);
            this.tbWidth.TabIndex = 26;
            this.tbWidth.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTip.SetToolTip(this.tbWidth, global::ScreenToGif.Properties.Resources.Tooltip_Widht);
            this.tbWidth.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbSize_KeyDown);
            this.tbWidth.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbSize_KeyPress);
            this.tbWidth.Leave += new System.EventHandler(this.tbSize_Leave);
            // 
            // lblSize
            // 
            this.lblSize.Image = global::ScreenToGif.Properties.Resources.Size;
            this.lblSize.Location = new System.Drawing.Point(505, 7);
            this.lblSize.Margin = new System.Windows.Forms.Padding(1, 7, 1, 3);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(22, 17);
            this.lblSize.TabIndex = 27;
            this.lblSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.lblSize, global::ScreenToGif.Properties.Resources.Label_Size);
            // 
            // numMaxFps
            // 
            this.numMaxFps.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.numMaxFps.AutoSize = true;
            this.numMaxFps.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.numMaxFps.Location = new System.Drawing.Point(467, 4);
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
            // lblFps
            // 
            this.lblFps.Image = global::ScreenToGif.Properties.Resources.Delay;
            this.lblFps.Location = new System.Drawing.Point(445, 7);
            this.lblFps.Margin = new System.Windows.Forms.Padding(1, 7, 1, 3);
            this.lblFps.Name = "lblFps";
            this.lblFps.Size = new System.Drawing.Size(18, 17);
            this.lblFps.TabIndex = 30;
            this.toolTip.SetToolTip(this.lblFps, global::ScreenToGif.Properties.Resources.Tooltip_NumFPS);
            // 
            // pbSeparator
            // 
            this.pbSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbSeparator.BackColor = System.Drawing.Color.Transparent;
            this.pbSeparator.Image = global::ScreenToGif.Properties.Resources.Separator;
            this.pbSeparator.Location = new System.Drawing.Point(439, 4);
            this.pbSeparator.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.pbSeparator.Name = "pbSeparator";
            this.pbSeparator.Size = new System.Drawing.Size(2, 24);
            this.pbSeparator.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSeparator.TabIndex = 31;
            this.pbSeparator.TabStop = false;
            // 
            // btnConfig
            // 
            this.btnConfig.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnConfig.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnConfig.FlatAppearance.BorderSize = 0;
            this.btnConfig.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnConfig.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfig.Image = global::ScreenToGif.Properties.Resources.Gear_17;
            this.btnConfig.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnConfig.Location = new System.Drawing.Point(410, 0);
            this.btnConfig.Margin = new System.Windows.Forms.Padding(1, 0, 0, 0);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnConfig.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnConfig.Size = new System.Drawing.Size(25, 31);
            this.btnConfig.TabIndex = 42;
            this.btnConfig.TabStop = false;
            this.btnConfig.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.btnConfig, global::ScreenToGif.Properties.Resources.Tooltip_AppSettings);
            this.btnConfig.UseVisualStyleBackColor = true;
            this.btnConfig.Click += new System.EventHandler(this.btnConfig_Click);
            // 
            // btnNext
            // 
            this.btnNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNext.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnNext.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnNext.FlatAppearance.BorderSize = 0;
            this.btnNext.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnNext.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Image = global::ScreenToGif.Properties.Resources.ArrowNext;
            this.btnNext.Location = new System.Drawing.Point(360, 0);
            this.btnNext.Margin = new System.Windows.Forms.Padding(0);
            this.btnNext.Name = "btnNext";
            this.btnNext.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnNext.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnNext.Size = new System.Drawing.Size(50, 31);
            this.btnNext.TabIndex = 35;
            this.btnNext.TabStop = false;
            this.btnNext.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip.SetToolTip(this.btnNext, "Next");
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Visible = false;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnPrevious
            // 
            this.btnPrevious.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPrevious.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnPrevious.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnPrevious.FlatAppearance.BorderSize = 0;
            this.btnPrevious.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnPrevious.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnPrevious.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrevious.Image = global::ScreenToGif.Properties.Resources.ArrowPrev;
            this.btnPrevious.Location = new System.Drawing.Point(310, 0);
            this.btnPrevious.Margin = new System.Windows.Forms.Padding(0);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnPrevious.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnPrevious.Size = new System.Drawing.Size(50, 31);
            this.btnPrevious.TabIndex = 36;
            this.btnPrevious.TabStop = false;
            this.btnPrevious.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.toolTip.SetToolTip(this.btnPrevious, "Previous");
            this.btnPrevious.UseVisualStyleBackColor = true;
            this.btnPrevious.Visible = false;
            this.btnPrevious.Click += new System.EventHandler(this.btnPrevious_Click);
            // 
            // pbSeparator2
            // 
            this.pbSeparator2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbSeparator2.BackColor = System.Drawing.Color.Transparent;
            this.pbSeparator2.Image = global::ScreenToGif.Properties.Resources.Separator;
            this.pbSeparator2.Location = new System.Drawing.Point(305, 4);
            this.pbSeparator2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.pbSeparator2.Name = "pbSeparator2";
            this.pbSeparator2.Size = new System.Drawing.Size(2, 24);
            this.pbSeparator2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSeparator2.TabIndex = 38;
            this.pbSeparator2.TabStop = false;
            this.pbSeparator2.Visible = false;
            // 
            // btnFreeDrawing
            // 
            this.btnFreeDrawing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFreeDrawing.AutoSize = true;
            this.btnFreeDrawing.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnFreeDrawing.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnFreeDrawing.FlatAppearance.BorderSize = 0;
            this.btnFreeDrawing.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnFreeDrawing.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnFreeDrawing.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFreeDrawing.Image = global::ScreenToGif.Properties.Resources.edit;
            this.btnFreeDrawing.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnFreeDrawing.Location = new System.Drawing.Point(200, 0);
            this.btnFreeDrawing.Margin = new System.Windows.Forms.Padding(0);
            this.btnFreeDrawing.Name = "btnFreeDrawing";
            this.btnFreeDrawing.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.btnFreeDrawing.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnFreeDrawing.Size = new System.Drawing.Size(102, 31);
            this.btnFreeDrawing.TabIndex = 39;
            this.btnFreeDrawing.TabStop = false;
            this.btnFreeDrawing.Text = global::ScreenToGif.Properties.Resources.Title_FreeDrawing;
            this.btnFreeDrawing.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnFreeDrawing.Visible = false;
            this.btnFreeDrawing.Click += new System.EventHandler(this.btnFreeDraw_Click);
            // 
            // btnAddText
            // 
            this.btnAddText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddText.AutoSize = true;
            this.btnAddText.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnAddText.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnAddText.FlatAppearance.BorderSize = 0;
            this.btnAddText.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnAddText.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnAddText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddText.Image = global::ScreenToGif.Properties.Resources.Text1;
            this.btnAddText.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAddText.Location = new System.Drawing.Point(111, 0);
            this.btnAddText.Margin = new System.Windows.Forms.Padding(0);
            this.btnAddText.Name = "btnAddText";
            this.btnAddText.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
            this.btnAddText.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnAddText.Size = new System.Drawing.Size(89, 31);
            this.btnAddText.TabIndex = 37;
            this.btnAddText.TabStop = false;
            this.btnAddText.Text = global::ScreenToGif.Properties.Resources.Con_AddText;
            this.btnAddText.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnAddText.Visible = false;
            this.btnAddText.Click += new System.EventHandler(this.con_addText_Click);
            // 
            // btnGifConfig
            // 
            this.btnGifConfig.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnGifConfig.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnGifConfig.FlatAppearance.BorderSize = 0;
            this.btnGifConfig.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnGifConfig.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnGifConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGifConfig.Image = global::ScreenToGif.Properties.Resources.Image_17;
            this.btnGifConfig.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnGifConfig.Location = new System.Drawing.Point(86, 0);
            this.btnGifConfig.Margin = new System.Windows.Forms.Padding(0);
            this.btnGifConfig.Name = "btnGifConfig";
            this.btnGifConfig.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnGifConfig.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnGifConfig.Size = new System.Drawing.Size(25, 31);
            this.btnGifConfig.TabIndex = 41;
            this.btnGifConfig.TabStop = false;
            this.btnGifConfig.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.btnGifConfig, global::ScreenToGif.Properties.Resources.Tooltip_GifSettings);
            this.btnGifConfig.UseVisualStyleBackColor = true;
            this.btnGifConfig.Click += new System.EventHandler(this.btnGifConfig_Click);
            // 
            // btnInfo
            // 
            this.btnInfo.Appearance = System.Windows.Forms.Appearance.Button;
            this.btnInfo.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnInfo.FlatAppearance.BorderSize = 0;
            this.btnInfo.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.btnInfo.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInfo.Image = global::ScreenToGif.Properties.Resources.Info_17Blue;
            this.btnInfo.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnInfo.Location = new System.Drawing.Point(61, 0);
            this.btnInfo.Margin = new System.Windows.Forms.Padding(0);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Padding = new System.Windows.Forms.Padding(0, 2, 0, 3);
            this.btnInfo.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnInfo.Size = new System.Drawing.Size(25, 31);
            this.btnInfo.TabIndex = 40;
            this.btnInfo.TabStop = false;
            this.btnInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.btnInfo, global::ScreenToGif.Properties.Resources.Tooltip_Info);
            this.btnInfo.UseVisualStyleBackColor = true;
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
            // 
            // timerCapWithCursor
            // 
            this.timerCapWithCursor.Interval = 66;
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
            this.openImageDialog.Filter = "Image (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif";
            this.openImageDialog.Title = global::ScreenToGif.Properties.Resources.Dialog_OpenImage;
            // 
            // contextDelay
            // 
            this.contextDelay.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.contextDelay.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.typeYouDesiredFrameDelayToolStripMenuItem,
            this.con_tbDelay,
            this.between10MsAnd1000MsToolStripMenuItem});
            this.contextDelay.Name = "contextDelay";
            this.contextDelay.Size = new System.Drawing.Size(271, 79);
            // 
            // typeYouDesiredFrameDelayToolStripMenuItem
            // 
            this.typeYouDesiredFrameDelayToolStripMenuItem.Enabled = false;
            this.typeYouDesiredFrameDelayToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.typeYouDesiredFrameDelayToolStripMenuItem.Image = global::ScreenToGif.Properties.Resources.Delay;
            this.typeYouDesiredFrameDelayToolStripMenuItem.Name = "typeYouDesiredFrameDelayToolStripMenuItem";
            this.typeYouDesiredFrameDelayToolStripMenuItem.Size = new System.Drawing.Size(270, 24);
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
            this.con_tbDelay.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_tbDelay.MaxLength = 4;
            this.con_tbDelay.Name = "con_tbDelay";
            this.con_tbDelay.Size = new System.Drawing.Size(100, 25);
            this.con_tbDelay.Text = "66";
            this.con_tbDelay.KeyDown += new System.Windows.Forms.KeyEventHandler(this.con_tbDelay_KeyDown);
            this.con_tbDelay.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbSize_KeyPress);
            this.con_tbDelay.TextChanged += new System.EventHandler(this.con_tbDelay_TextChanged);
            // 
            // between10MsAnd1000MsToolStripMenuItem
            // 
            this.between10MsAnd1000MsToolStripMenuItem.Enabled = false;
            this.between10MsAnd1000MsToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.between10MsAnd1000MsToolStripMenuItem.Name = "between10MsAnd1000MsToolStripMenuItem";
            this.between10MsAnd1000MsToolStripMenuItem.Size = new System.Drawing.Size(270, 24);
            this.between10MsAnd1000MsToolStripMenuItem.Text = global::ScreenToGif.Properties.Resources.Con_Between10_1000;
            // 
            // contextSmall
            // 
            this.contextSmall.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.contextSmall.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.con_Pixelate,
            this.con_Blur,
            this.con_Negative,
            this.con_Sepia,
            this.con_Grayscale,
            this.con_Color});
            this.contextSmall.Name = "contextSmall";
            this.contextSmall.Size = new System.Drawing.Size(137, 148);
            // 
            // con_Pixelate
            // 
            this.con_Pixelate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Pixelate.Image = global::ScreenToGif.Properties.Resources.pixelize;
            this.con_Pixelate.Name = "con_Pixelate";
            this.con_Pixelate.Size = new System.Drawing.Size(136, 24);
            this.con_Pixelate.Text = global::ScreenToGif.Properties.Resources.Con_Filters_Pixelate;
            this.con_Pixelate.Click += new System.EventHandler(this.Pixelate_Click);
            // 
            // con_Blur
            // 
            this.con_Blur.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Blur.Image = global::ScreenToGif.Properties.Resources.blur;
            this.con_Blur.Name = "con_Blur";
            this.con_Blur.Size = new System.Drawing.Size(136, 24);
            this.con_Blur.Text = global::ScreenToGif.Properties.Resources.Con_Blur;
            this.con_Blur.Click += new System.EventHandler(this.Blur_Click);
            // 
            // con_Negative
            // 
            this.con_Negative.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Negative.Image = global::ScreenToGif.Properties.Resources.negative;
            this.con_Negative.Name = "con_Negative";
            this.con_Negative.Size = new System.Drawing.Size(136, 24);
            this.con_Negative.Text = global::ScreenToGif.Properties.Resources.Con_Negative;
            this.con_Negative.Click += new System.EventHandler(this.Negative_Click);
            // 
            // con_Sepia
            // 
            this.con_Sepia.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Sepia.Image = global::ScreenToGif.Properties.Resources.sepia;
            this.con_Sepia.Name = "con_Sepia";
            this.con_Sepia.Size = new System.Drawing.Size(136, 24);
            this.con_Sepia.Text = global::ScreenToGif.Properties.Resources.Con_Sepia;
            this.con_Sepia.Click += new System.EventHandler(this.SepiaTone_Click);
            // 
            // con_Grayscale
            // 
            this.con_Grayscale.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.con_Grayscale.Image = global::ScreenToGif.Properties.Resources.grayscale;
            this.con_Grayscale.Name = "con_Grayscale";
            this.con_Grayscale.Size = new System.Drawing.Size(136, 24);
            this.con_Grayscale.Text = global::ScreenToGif.Properties.Resources.Con_FiltersGray;
            this.con_Grayscale.Click += new System.EventHandler(this.Grayscale_Click);
            // 
            // con_Color
            // 
            this.con_Color.Image = global::ScreenToGif.Properties.Resources.colorize;
            this.con_Color.Name = "con_Color";
            this.con_Color.Size = new System.Drawing.Size(136, 24);
            this.con_Color.Text = global::ScreenToGif.Properties.Resources.Con_Color;
            this.con_Color.Click += new System.EventHandler(this.Color_Click);
            // 
            // timerCaptureFull
            // 
            this.timerCaptureFull.Interval = 66;
            this.timerCaptureFull.Tick += new System.EventHandler(this.timerCaptureFull_Tick);
            // 
            // timerCapWithCursorFull
            // 
            this.timerCapWithCursorFull.Interval = 66;
            this.timerCapWithCursorFull.Tick += new System.EventHandler(this.timerCapWithCursorFull_Tick);
            // 
            // tvFrames
            // 
            this.tvFrames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvFrames.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tvFrames.CausesValidation = false;
            this.tvFrames.CheckBoxes = true;
            this.tvFrames.ContextMenuStrip = this.contextMenuTreeview;
            this.tvFrames.First = -1;
            this.tvFrames.LabelEdit = true;
            this.tvFrames.Last = -1;
            this.tvFrames.LineColor = System.Drawing.Color.Navy;
            this.tvFrames.Location = new System.Drawing.Point(0, 0);
            this.tvFrames.Name = "tvFrames";
            treeNode1.Name = "Node1";
            treeNode1.Text = "Node1";
            treeNode2.Name = "Node0";
            treeNode2.Text = global::ScreenToGif.Properties.Resources.Label_All;
            this.tvFrames.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode2});
            this.tvFrames.Shift = false;
            this.tvFrames.ShowRootLines = false;
            this.tvFrames.Size = new System.Drawing.Size(137, 245);
            this.tvFrames.TabIndex = 24;
            this.tvFrames.TabStop = false;
            this.tvFrames.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.tvFrames_AfterCheck);
            this.tvFrames.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvFrames_AfterSelect);
            this.tvFrames.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvFrames_NodeMouseClick);
            // 
            // Legacy
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.ClientSize = new System.Drawing.Size(752, 344);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelEdit);
            this.Controls.Add(this.panelTransparent);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(80, 81);
            this.Name = "Legacy";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Screen To Gif";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.LimeGreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.panelEdit.ResumeLayout(false);
            this.MainSplit.Panel1.ResumeLayout(false);
            this.MainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.MainSplit)).EndInit();
            this.MainSplit.ResumeLayout(false);
            this.contextMenuTreeview.ResumeLayout(false);
            this.RightSplit.Panel1.ResumeLayout(false);
            this.RightSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RightSplit)).EndInit();
            this.RightSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBitmap)).EndInit();
            this.contextMenu.ResumeLayout(false);
            this.panelTrack.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.flowPanelEditTop.ResumeLayout(false);
            this.flowPanelEditTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.contextRecord.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numMaxFps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator2)).EndInit();
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
        private System.Windows.Forms.FlowLayoutPanel panelBottom;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnRecordPause;
        private System.Windows.Forms.TextBox tbHeight;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.TextBox tbWidth;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.NumericUpDown numMaxFps;
        private System.Windows.Forms.Label lblFps;
        private System.Windows.Forms.PictureBox pbSeparator;
        private System.Windows.Forms.Timer timerCapWithCursor;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolTip toolTipHelp;
        private System.Windows.Forms.ToolStripMenuItem addFrameItem;
        private System.Windows.Forms.ToolStripMenuItem imageItem;
        private System.Windows.Forms.ToolStripMenuItem exportFrameItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem con_DeleteSelected;
        private System.Windows.Forms.ToolStripMenuItem editFrameItem;
        private System.Windows.Forms.ToolStripMenuItem resizeAllItem;
        private System.Windows.Forms.ToolStripMenuItem cropAllItem;
        private System.Windows.Forms.OpenFileDialog openImageDialog;
        private System.Windows.Forms.ToolStripMenuItem revertOrderItem;
        private System.Windows.Forms.ToolStripMenuItem yoyoItem;
        private System.Windows.Forms.FlowLayoutPanel flowPanelEditTop;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnDeleteFrame;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Label lblDelay;
        private System.Windows.Forms.ContextMenuStrip contextDelay;
        private System.Windows.Forms.ToolStripTextBox con_tbDelay;
        private System.Windows.Forms.ToolStripMenuItem between10MsAnd1000MsToolStripMenuItem;
        private System.Windows.Forms.Button btnFilters;
        private System.Windows.Forms.ContextMenuStrip contextSmall;
        private System.Windows.Forms.Button btnOptions;
        private System.Windows.Forms.ToolStripMenuItem con_showGrid;
        private System.Windows.Forms.ToolStripMenuItem changeSpeedItem;
        private System.Windows.Forms.ToolStripMenuItem titleImageItem;
        private System.Windows.Forms.ToolStripMenuItem con_addText;
        private System.Windows.Forms.ToolStripMenuItem addQuickCaptionToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox con_tbCaption;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem15;
        private System.Windows.Forms.ToolStripMenuItem con_addCaption;
        private System.Windows.Forms.ToolStripMenuItem borderItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem typeYouDesiredFrameDelayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem con_CaptionOptions;
        private System.Windows.Forms.Panel panelTrack;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnPrevious;
        private System.Windows.Forms.PictureBox pbSeparator2;
        private System.Windows.Forms.Button btnAddText;
        private System.Windows.Forms.SplitContainer MainSplit;
        private System.Windows.Forms.Button btnHideListFrames;
        private System.Windows.Forms.SplitContainer RightSplit;
        private System.Windows.Forms.Button btnShowListFrames;
        private Controls.NoDoubleClickTreeView tvFrames;
        private System.Windows.Forms.ToolStripMenuItem con_Pixelate;
        private System.Windows.Forms.ToolStripMenuItem con_Blur;
        private System.Windows.Forms.ToolStripMenuItem con_Negative;
        private System.Windows.Forms.ToolStripMenuItem con_Sepia;
        private System.Windows.Forms.ToolStripMenuItem con_Grayscale;
        private System.Windows.Forms.ContextMenuStrip contextRecord;
        private System.Windows.Forms.ToolStripMenuItem con_Fullscreen;
        private System.Windows.Forms.ToolStripMenuItem recordingOptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem con_Snapshot;
        private System.Windows.Forms.Timer timerCaptureFull;
        private System.Windows.Forms.Timer timerCapWithCursorFull;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem con_Color;
        private System.Windows.Forms.ToolStripMenuItem con_flipRotate;
        private System.Windows.Forms.ToolStripMenuItem con_flipVertical;
        private System.Windows.Forms.ToolStripMenuItem con_flipHorizontal;
        private System.Windows.Forms.ToolStripMenuItem rotate90ClockwiseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rotate90CounterclockwiseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rotate180ToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuTreeview;
        private System.Windows.Forms.ToolStripMenuItem con_MakeACopy;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem con_MoveUpwards;
        private System.Windows.Forms.ToolStripMenuItem con_MoveDownwards;
        private System.Windows.Forms.ToolStripMenuItem con_RenameFrame;
        private System.Windows.Forms.ToolStripMenuItem frameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.Button btnFreeDrawing;
        private System.Windows.Forms.ToolStripMenuItem con_Transitions;
        private System.Windows.Forms.CheckBox btnInfo;
        private System.Windows.Forms.CheckBox btnGifConfig;
        private System.Windows.Forms.CheckBox btnConfig;
    }
}