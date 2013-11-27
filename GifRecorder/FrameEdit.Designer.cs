namespace GifRecorder
{
    partial class FrameEdit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrameEdit));
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.trackBar = new System.Windows.Forms.TrackBar();
            this.btnDeleteFrame = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnUndoAll = new System.Windows.Forms.Button();
            this.btnUndoOne = new System.Windows.Forms.Button();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.nenuDeleteAfter = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteBefore = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).BeginInit();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox.ContextMenuStrip = this.contextMenu;
            this.pictureBox.Location = new System.Drawing.Point(12, 12);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(424, 128);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // trackBar
            // 
            this.trackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar.Location = new System.Drawing.Point(12, 148);
            this.trackBar.Maximum = 100;
            this.trackBar.Name = "trackBar";
            this.trackBar.Size = new System.Drawing.Size(424, 45);
            this.trackBar.TabIndex = 1;
            this.trackBar.Scroll += new System.EventHandler(this.trackBar_Scroll);
            // 
            // btnDeleteFrame
            // 
            this.btnDeleteFrame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeleteFrame.Location = new System.Drawing.Point(176, 199);
            this.btnDeleteFrame.Name = "btnDeleteFrame";
            this.btnDeleteFrame.Size = new System.Drawing.Size(98, 23);
            this.btnDeleteFrame.TabIndex = 2;
            this.btnDeleteFrame.Text = "Delete Frame";
            this.btnDeleteFrame.UseVisualStyleBackColor = true;
            this.btnDeleteFrame.Click += new System.EventHandler(this.btnDeleteFrame_Click);
            // 
            // btnDone
            // 
            this.btnDone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDone.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDone.Location = new System.Drawing.Point(280, 199);
            this.btnDone.Name = "btnDone";
            this.btnDone.Size = new System.Drawing.Size(75, 23);
            this.btnDone.TabIndex = 4;
            this.btnDone.Text = "Done";
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCancel.Location = new System.Drawing.Point(361, 199);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnUndoAll
            // 
            this.btnUndoAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUndoAll.Location = new System.Drawing.Point(9, 199);
            this.btnUndoAll.Name = "btnUndoAll";
            this.btnUndoAll.Size = new System.Drawing.Size(80, 23);
            this.btnUndoAll.TabIndex = 6;
            this.btnUndoAll.Text = "Undo All";
            this.btnUndoAll.UseVisualStyleBackColor = true;
            this.btnUndoAll.Click += new System.EventHandler(this.btnUndoAll_Click);
            // 
            // btnUndoOne
            // 
            this.btnUndoOne.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUndoOne.Location = new System.Drawing.Point(95, 199);
            this.btnUndoOne.Name = "btnUndoOne";
            this.btnUndoOne.Size = new System.Drawing.Size(75, 23);
            this.btnUndoOne.TabIndex = 7;
            this.btnUndoOne.Text = "Undo One";
            this.btnUndoOne.UseVisualStyleBackColor = true;
            this.btnUndoOne.Click += new System.EventHandler(this.btnUndoOne_Click);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nenuDeleteAfter,
            this.menuDeleteBefore});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(245, 70);
            // 
            // nenuDeleteAfter
            // 
            this.nenuDeleteAfter.Name = "nenuDeleteAfter";
            this.nenuDeleteAfter.Size = new System.Drawing.Size(244, 22);
            this.nenuDeleteAfter.Text = "Delete everything after this >>";
            this.nenuDeleteAfter.Click += new System.EventHandler(this.nenuDeleteAfter_Click);
            // 
            // menuDeleteBefore
            // 
            this.menuDeleteBefore.Name = "menuDeleteBefore";
            this.menuDeleteBefore.Size = new System.Drawing.Size(244, 22);
            this.menuDeleteBefore.Text = "<< Delete everything before this";
            this.menuDeleteBefore.Click += new System.EventHandler(this.menuDeleteBefore_Click);
            // 
            // FrameEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(448, 234);
            this.Controls.Add(this.btnUndoOne);
            this.Controls.Add(this.btnUndoAll);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnDone);
            this.Controls.Add(this.btnDeleteFrame);
            this.Controls.Add(this.trackBar);
            this.Controls.Add(this.pictureBox);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(464, 273);
            this.Name = "FrameEdit";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Frame Editor";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar)).EndInit();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.TrackBar trackBar;
        private System.Windows.Forms.Button btnDeleteFrame;
        private System.Windows.Forms.Button btnDone;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnUndoAll;
        private System.Windows.Forms.Button btnUndoOne;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem nenuDeleteAfter;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteBefore;
    }
}