using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class Processing
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
            this.progressBarEncoding = new System.Windows.Forms.ProgressBar();
            this.lblValue = new System.Windows.Forms.Label();
            this.linkOpenFile = new System.Windows.Forms.LinkLabel();
            this.linkClose = new System.Windows.Forms.LinkLabel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.lblProcessing = new System.Windows.Forms.Label();
            this.lblSize = new System.Windows.Forms.Label();
            this.pbSize = new System.Windows.Forms.PictureBox();
            this.pbOpen = new System.Windows.Forms.PictureBox();
            this.picStatus = new System.Windows.Forms.PictureBox();
            this.panelBottom = new System.Windows.Forms.TableLayoutPanel();
            this.panelTitle = new System.Windows.Forms.TableLayoutPanel();
            this.panelBottomFinish = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pbSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbOpen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picStatus)).BeginInit();
            this.panelBottom.SuspendLayout();
            this.panelTitle.SuspendLayout();
            this.panelBottomFinish.SuspendLayout();
            this.SuspendLayout();
            // 
            // progressBarEncoding
            // 
            this.progressBarEncoding.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBarEncoding.Location = new System.Drawing.Point(3, 31);
            this.progressBarEncoding.MarqueeAnimationSpeed = 5;
            this.progressBarEncoding.Name = "progressBarEncoding";
            this.progressBarEncoding.Size = new System.Drawing.Size(527, 23);
            this.progressBarEncoding.Step = 1;
            this.progressBarEncoding.TabIndex = 1;
            // 
            // lblValue
            // 
            this.lblValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblValue.Font = new System.Drawing.Font("Segoe UI Semilight", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblValue.Location = new System.Drawing.Point(3, 0);
            this.lblValue.Name = "lblValue";
            this.lblValue.Size = new System.Drawing.Size(527, 28);
            this.lblValue.TabIndex = 2;
            this.lblValue.Text = "0 out of XX";
            this.lblValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // linkOpenFile
            // 
            this.linkOpenFile.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.linkOpenFile.AutoSize = true;
            this.linkOpenFile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.linkOpenFile.Font = new System.Drawing.Font("Segoe UI Semilight", 12F);
            this.linkOpenFile.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.linkOpenFile.Location = new System.Drawing.Point(25, 6);
            this.linkOpenFile.Name = "linkOpenFile";
            this.linkOpenFile.Size = new System.Drawing.Size(127, 21);
            this.linkOpenFile.TabIndex = 3;
            this.linkOpenFile.TabStop = true;
            this.linkOpenFile.Text = "Open the .Gif file";
            this.linkOpenFile.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.linkOpenFile.Visible = false;
            this.linkOpenFile.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkOpenFile_LinkClicked);
            this.linkOpenFile.MouseHover += new System.EventHandler(this.linkOpenFile_MouseHover);
            // 
            // linkClose
            // 
            this.linkClose.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.linkClose.AutoSize = true;
            this.linkClose.Font = new System.Drawing.Font("Segoe UI Semilight", 10F);
            this.linkClose.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.linkClose.Location = new System.Drawing.Point(429, 17);
            this.linkClose.Name = "linkClose";
            this.linkClose.Size = new System.Drawing.Size(101, 19);
            this.linkClose.TabIndex = 4;
            this.linkClose.TabStop = true;
            this.linkClose.Text = "Close this page";
            this.linkClose.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.linkClose.Visible = false;
            this.linkClose.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkClose_LinkClicked);
            // 
            // lblProcessing
            // 
            this.lblProcessing.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblProcessing.AutoSize = true;
            this.lblProcessing.Font = new System.Drawing.Font("Segoe UI Semilight", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProcessing.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.lblProcessing.Location = new System.Drawing.Point(44, 14);
            this.lblProcessing.Name = "lblProcessing";
            this.lblProcessing.Size = new System.Drawing.Size(99, 25);
            this.lblProcessing.TabIndex = 1;
            this.lblProcessing.Text = "Processing";
            this.lblProcessing.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSize
            // 
            this.lblSize.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblSize.AutoSize = true;
            this.lblSize.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSize.Location = new System.Drawing.Point(498, 6);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(32, 21);
            this.lblSize.TabIndex = 5;
            this.lblSize.Text = "0 B";
            this.lblSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.lblSize, "File Size");
            // 
            // pbSize
            // 
            this.pbSize.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pbSize.Image = global::ScreenToGif.Properties.Resources.FilePropertie26x;
            this.pbSize.Location = new System.Drawing.Point(476, 9);
            this.pbSize.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.pbSize.Name = "pbSize";
            this.pbSize.Size = new System.Drawing.Size(16, 16);
            this.pbSize.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbSize.TabIndex = 37;
            this.pbSize.TabStop = false;
            this.toolTip.SetToolTip(this.pbSize, "File Size");
            // 
            // pbOpen
            // 
            this.pbOpen.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pbOpen.Image = global::ScreenToGif.Properties.Resources.OpenFile16x;
            this.pbOpen.Location = new System.Drawing.Point(3, 9);
            this.pbOpen.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.pbOpen.Name = "pbOpen";
            this.pbOpen.Size = new System.Drawing.Size(16, 16);
            this.pbOpen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbOpen.TabIndex = 36;
            this.pbOpen.TabStop = false;
            this.toolTip.SetToolTip(this.pbOpen, "Open Gif");
            // 
            // picStatus
            // 
            this.picStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.picStatus.Image = global::ScreenToGif.Properties.Resources.Processing35x;
            this.picStatus.Location = new System.Drawing.Point(3, 8);
            this.picStatus.Name = "picStatus";
            this.picStatus.Size = new System.Drawing.Size(35, 36);
            this.picStatus.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picStatus.TabIndex = 0;
            this.picStatus.TabStop = false;
            // 
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.panelBottom.ColumnCount = 4;
            this.panelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelBottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelBottom.Controls.Add(this.pbSize, 2, 0);
            this.panelBottom.Controls.Add(this.pbOpen, 0, 0);
            this.panelBottom.Controls.Add(this.linkOpenFile, 1, 0);
            this.panelBottom.Controls.Add(this.lblSize, 3, 0);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 275);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.RowCount = 1;
            this.panelBottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelBottom.Size = new System.Drawing.Size(533, 33);
            this.panelBottom.TabIndex = 33;
            this.panelBottom.Visible = false;
            // 
            // panelTitle
            // 
            this.panelTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.panelTitle.ColumnCount = 3;
            this.panelTitle.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelTitle.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTitle.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelTitle.Controls.Add(this.linkClose, 2, 0);
            this.panelTitle.Controls.Add(this.picStatus, 0, 0);
            this.panelTitle.Controls.Add(this.lblProcessing, 1, 0);
            this.panelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitle.Location = new System.Drawing.Point(0, 0);
            this.panelTitle.Name = "panelTitle";
            this.panelTitle.RowCount = 1;
            this.panelTitle.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelTitle.Size = new System.Drawing.Size(533, 53);
            this.panelTitle.TabIndex = 34;
            // 
            // panelBottomFinish
            // 
            this.panelBottomFinish.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.panelBottomFinish.ColumnCount = 1;
            this.panelBottomFinish.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelBottomFinish.Controls.Add(this.progressBarEncoding, 0, 1);
            this.panelBottomFinish.Controls.Add(this.lblValue, 0, 0);
            this.panelBottomFinish.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottomFinish.Location = new System.Drawing.Point(0, 218);
            this.panelBottomFinish.Name = "panelBottomFinish";
            this.panelBottomFinish.RowCount = 2;
            this.panelBottomFinish.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelBottomFinish.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelBottomFinish.Size = new System.Drawing.Size(533, 57);
            this.panelBottomFinish.TabIndex = 35;
            // 
            // Processing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.panelBottomFinish);
            this.Controls.Add(this.panelTitle);
            this.Controls.Add(this.panelBottom);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Processing";
            this.Size = new System.Drawing.Size(533, 308);
            ((System.ComponentModel.ISupportInitialize)(this.pbSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbOpen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picStatus)).EndInit();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.panelTitle.ResumeLayout(false);
            this.panelTitle.PerformLayout();
            this.panelBottomFinish.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBarEncoding;
        private System.Windows.Forms.Label lblValue;
        private System.Windows.Forms.LinkLabel linkOpenFile;
        private System.Windows.Forms.LinkLabel linkClose;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label lblSize;
        private System.Windows.Forms.PictureBox picStatus;
        private System.Windows.Forms.Label lblProcessing;
        private System.Windows.Forms.TableLayoutPanel panelBottom;
        private System.Windows.Forms.TableLayoutPanel panelTitle;
        private System.Windows.Forms.TableLayoutPanel panelBottomFinish;
        private System.Windows.Forms.PictureBox pbOpen;
        private System.Windows.Forms.PictureBox pbSize;
    }
}
