namespace ScreenToGif
{
    partial class TestForm
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
            this.content1 = new ScreenToGif.Pages.Content(this);
            this.SuspendLayout();
            // 
            // content1
            // 
            this.content1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.content1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.content1.Location = new System.Drawing.Point(0, 0);
            this.content1.Name = "content1";
            this.content1.Size = new System.Drawing.Size(783, 478);
            this.content1.TabIndex = 0;
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(783, 478);
            this.Controls.Add(this.content1);
            this.Name = "TestForm";
            this.Text = "TestForm";
            this.TransparencyKey = System.Drawing.Color.LimeGreen;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private Pages.Content content1;
    }
}