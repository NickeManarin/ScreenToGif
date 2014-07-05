namespace ScreenToGif.Pages
{
    partial class ErrorSender
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorSender));
            this.tbSubject = new System.Windows.Forms.TextBox();
            this.tbMessage = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.gbType = new System.Windows.Forms.GroupBox();
            this.cbSuggestion = new System.Windows.Forms.CheckBox();
            this.cbError = new System.Windows.Forms.CheckBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.gbType.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbSubject
            // 
            this.tbSubject.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSubject.Location = new System.Drawing.Point(12, 104);
            this.tbSubject.Name = "tbSubject";
            this.tbSubject.Size = new System.Drawing.Size(460, 23);
            this.tbSubject.TabIndex = 10;
            // 
            // tbMessage
            // 
            this.tbMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMessage.Location = new System.Drawing.Point(12, 148);
            this.tbMessage.Multiline = true;
            this.tbMessage.Name = "tbMessage";
            this.tbMessage.Size = new System.Drawing.Size(460, 125);
            this.tbMessage.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 130);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 15);
            this.label1.TabIndex = 8;
            this.label1.Text = "Message:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 15);
            this.label2.TabIndex = 11;
            this.label2.Text = "Subject:";
            // 
            // gbType
            // 
            this.gbType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbType.Controls.Add(this.cbSuggestion);
            this.gbType.Controls.Add(this.cbError);
            this.gbType.Location = new System.Drawing.Point(282, 12);
            this.gbType.Name = "gbType";
            this.gbType.Size = new System.Drawing.Size(190, 72);
            this.gbType.TabIndex = 12;
            this.gbType.TabStop = false;
            this.gbType.Text = "Type";
            // 
            // cbSuggestion
            // 
            this.cbSuggestion.AutoSize = true;
            this.cbSuggestion.Image = global::ScreenToGif.Properties.Resources.Comment;
            this.cbSuggestion.Location = new System.Drawing.Point(6, 47);
            this.cbSuggestion.Name = "cbSuggestion";
            this.cbSuggestion.Size = new System.Drawing.Size(101, 19);
            this.cbSuggestion.TabIndex = 1;
            this.cbSuggestion.Text = "Suggestion";
            this.cbSuggestion.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.cbSuggestion.UseVisualStyleBackColor = true;
            this.cbSuggestion.CheckedChanged += new System.EventHandler(this.cbSuggestion_CheckedChanged);
            // 
            // cbError
            // 
            this.cbError.AutoSize = true;
            this.cbError.Image = global::ScreenToGif.Properties.Resources.Alert;
            this.cbError.Location = new System.Drawing.Point(6, 22);
            this.cbError.Name = "cbError";
            this.cbError.Size = new System.Drawing.Size(129, 19);
            this.cbError.TabIndex = 0;
            this.cbError.Text = "Bug/Error/Glitch";
            this.cbError.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.cbError.UseVisualStyleBackColor = true;
            this.cbError.CheckedChanged += new System.EventHandler(this.cbError_CheckedChanged);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.btnSend.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnSend.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.Image = global::ScreenToGif.Properties.Resources.Send;
            this.btnSend.Location = new System.Drawing.Point(-2, 284);
            this.btnSend.Margin = new System.Windows.Forms.Padding(0);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(490, 39);
            this.btnSend.TabIndex = 13;
            this.btnSend.Text = "Send";
            this.btnSend.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnSend.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnSend.UseVisualStyleBackColor = false;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // ErrorSender
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(484, 321);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.gbType);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbSubject);
            this.Controls.Add(this.tbMessage);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(500, 360);
            this.Name = "ErrorSender";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bug/Suggestion";
            this.TopMost = true;
            this.gbType.ResumeLayout(false);
            this.gbType.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbSubject;
        private System.Windows.Forms.TextBox tbMessage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox gbType;
        private System.Windows.Forms.CheckBox cbSuggestion;
        private System.Windows.Forms.CheckBox cbError;
        private System.Windows.Forms.Button btnSend;
    }
}