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
            this.lblProcessing = new System.Windows.Forms.Label();
            this.progressBarEncoding = new System.Windows.Forms.ProgressBar();
            this.lblValue = new System.Windows.Forms.Label();
            this.linkOpenFile = new System.Windows.Forms.LinkLabel();
            this.linkClose = new System.Windows.Forms.LinkLabel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.lblSize = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblProcessing
            // 
            this.lblProcessing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProcessing.Font = new System.Drawing.Font("Segoe UI Semilight", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProcessing.Location = new System.Drawing.Point(3, 70);
            this.lblProcessing.Name = "lblProcessing";
            this.lblProcessing.Size = new System.Drawing.Size(514, 32);
            this.lblProcessing.TabIndex = 0;
            this.lblProcessing.Text = Resources.Label_Processing;
            this.lblProcessing.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // progressBarEncoding
            // 
            this.progressBarEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarEncoding.Location = new System.Drawing.Point(3, 226);
            this.progressBarEncoding.MarqueeAnimationSpeed = 5;
            this.progressBarEncoding.Name = "progressBarEncoding";
            this.progressBarEncoding.Size = new System.Drawing.Size(514, 23);
            this.progressBarEncoding.Step = 1;
            this.progressBarEncoding.TabIndex = 1;
            // 
            // lblValue
            // 
            this.lblValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblValue.Font = new System.Drawing.Font("Segoe UI Semilight", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblValue.Location = new System.Drawing.Point(3, 188);
            this.lblValue.Name = "lblValue";
            this.lblValue.Size = new System.Drawing.Size(514, 35);
            this.lblValue.TabIndex = 2;
            this.lblValue.Text = "0 out of XX";
            this.lblValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // linkOpenFile
            // 
            this.linkOpenFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.linkOpenFile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.linkOpenFile.Font = new System.Drawing.Font("Segoe UI Semilight", 15.75F);
            this.linkOpenFile.Location = new System.Drawing.Point(3, 192);
            this.linkOpenFile.Name = "linkOpenFile";
            this.linkOpenFile.Size = new System.Drawing.Size(514, 31);
            this.linkOpenFile.TabIndex = 3;
            this.linkOpenFile.TabStop = true;
            this.linkOpenFile.Text = Resources.Label_OpenGif;
            this.linkOpenFile.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.linkOpenFile.Visible = false;
            this.linkOpenFile.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkOpenFile_LinkClicked);
            this.linkOpenFile.MouseHover += new System.EventHandler(this.linkOpenFile_MouseHover);
            // 
            // linkClose
            // 
            this.linkClose.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.linkClose.Font = new System.Drawing.Font("Segoe UI Semilight", 10F);
            this.linkClose.Location = new System.Drawing.Point(3, 226);
            this.linkClose.Name = "linkClose";
            this.linkClose.Size = new System.Drawing.Size(514, 23);
            this.linkClose.TabIndex = 4;
            this.linkClose.TabStop = true;
            this.linkClose.Text = Resources.Label_Close;
            this.linkClose.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.linkClose.Visible = false;
            this.linkClose.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkClose_LinkClicked);
            // 
            // lblSize
            // 
            this.lblSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSize.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSize.Location = new System.Drawing.Point(3, 167);
            this.lblSize.Name = "lblSize";
            this.lblSize.Size = new System.Drawing.Size(514, 25);
            this.lblSize.TabIndex = 5;
            this.lblSize.Text = "          ";
            this.lblSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Processing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.Controls.Add(this.lblSize);
            this.Controls.Add(this.linkClose);
            this.Controls.Add(this.linkOpenFile);
            this.Controls.Add(this.lblValue);
            this.Controls.Add(this.progressBarEncoding);
            this.Controls.Add(this.lblProcessing);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Processing";
            this.Size = new System.Drawing.Size(520, 252);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblProcessing;
        private System.Windows.Forms.ProgressBar progressBarEncoding;
        private System.Windows.Forms.Label lblValue;
        private System.Windows.Forms.LinkLabel linkOpenFile;
        private System.Windows.Forms.LinkLabel linkClose;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Label lblSize;
    }
}
