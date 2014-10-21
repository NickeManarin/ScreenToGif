using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class ExceptionViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExceptionViewer));
            this.pbStatus = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.tbError = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.tbMessage = new System.Windows.Forms.TextBox();
            this.tbSource = new System.Windows.Forms.TextBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btnInnerException = new System.Windows.Forms.Button();
            this.panelTitle = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pbStatus)).BeginInit();
            this.panelTitle.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbStatus
            // 
            this.pbStatus.Image = global::ScreenToGif.Properties.Resources.ShieldCritical32x;
            this.pbStatus.Location = new System.Drawing.Point(12, 11);
            this.pbStatus.Name = "pbStatus";
            this.pbStatus.Size = new System.Drawing.Size(32, 49);
            this.pbStatus.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pbStatus.TabIndex = 0;
            this.pbStatus.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semilight", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(51)))), ((int)(((byte)(153)))));
            this.lblTitle.Location = new System.Drawing.Point(50, 11);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(465, 49);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Error Title";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolTip.SetToolTip(this.lblTitle, "Exception Type");
            // 
            // tbError
            // 
            this.tbError.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbError.BackColor = System.Drawing.Color.White;
            this.tbError.Location = new System.Drawing.Point(12, 130);
            this.tbError.Multiline = true;
            this.tbError.Name = "tbError";
            this.tbError.ReadOnly = true;
            this.tbError.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbError.Size = new System.Drawing.Size(649, 188);
            this.tbError.TabIndex = 1;
            this.toolTip.SetToolTip(this.tbError, "StackTrace");
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btnClose.CausesValidation = false;
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Image = global::ScreenToGif.Properties.Resources.Done;
            this.btnClose.Location = new System.Drawing.Point(0, 356);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(675, 43);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = global::ScreenToGif.Properties.Resources.btnClose;
            this.btnClose.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnClose.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // tbMessage
            // 
            this.tbMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMessage.BackColor = System.Drawing.Color.White;
            this.tbMessage.Location = new System.Drawing.Point(12, 75);
            this.tbMessage.Multiline = true;
            this.tbMessage.Name = "tbMessage";
            this.tbMessage.ReadOnly = true;
            this.tbMessage.Size = new System.Drawing.Size(649, 49);
            this.tbMessage.TabIndex = 0;
            this.toolTip.SetToolTip(this.tbMessage, "Message");
            // 
            // tbSource
            // 
            this.tbSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSource.BackColor = System.Drawing.Color.White;
            this.tbSource.Location = new System.Drawing.Point(12, 324);
            this.tbSource.Name = "tbSource";
            this.tbSource.ReadOnly = true;
            this.tbSource.Size = new System.Drawing.Size(649, 23);
            this.tbSource.TabIndex = 2;
            this.toolTip.SetToolTip(this.tbSource, "Name of the source application");
            // 
            // btnInnerException
            // 
            this.btnInnerException.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInnerException.AutoSize = true;
            this.btnInnerException.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnInnerException.Enabled = false;
            this.btnInnerException.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnInnerException.FlatAppearance.BorderSize = 0;
            this.btnInnerException.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnInnerException.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnInnerException.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnInnerException.Image = global::ScreenToGif.Properties.Resources.InnerException16x;
            this.btnInnerException.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnInnerException.Location = new System.Drawing.Point(518, 19);
            this.btnInnerException.Margin = new System.Windows.Forms.Padding(0);
            this.btnInnerException.Name = "btnInnerException";
            this.btnInnerException.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnInnerException.Size = new System.Drawing.Size(146, 33);
            this.btnInnerException.TabIndex = 0;
            this.btnInnerException.Text = global::ScreenToGif.Properties.Resources.btnOpenInnerException;
            this.btnInnerException.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnInnerException.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnInnerException.UseVisualStyleBackColor = true;
            this.btnInnerException.Click += new System.EventHandler(this.btnInnerException_Click);
            // 
            // panelTitle
            // 
            this.panelTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(249)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.panelTitle.Controls.Add(this.pbStatus);
            this.panelTitle.Controls.Add(this.btnInnerException);
            this.panelTitle.Controls.Add(this.lblTitle);
            this.panelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTitle.Location = new System.Drawing.Point(0, 0);
            this.panelTitle.Name = "panelTitle";
            this.panelTitle.Size = new System.Drawing.Size(673, 69);
            this.panelTitle.TabIndex = 4;
            // 
            // ExceptionViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(673, 399);
            this.Controls.Add(this.panelTitle);
            this.Controls.Add(this.tbSource);
            this.Controls.Add(this.tbMessage);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.tbError);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(570, 350);
            this.Name = "ExceptionViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Exception Viewer";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.pbStatus)).EndInit();
            this.panelTitle.ResumeLayout(false);
            this.panelTitle.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbStatus;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox tbError;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.TextBox tbMessage;
        private System.Windows.Forms.TextBox tbSource;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button btnInnerException;
        private System.Windows.Forms.Panel panelTitle;
    }
}