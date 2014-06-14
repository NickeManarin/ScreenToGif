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
            this.internal1 = new ScreenToGif.Pages.Internal(this);
            this.SuspendLayout();
            // 
            // internal1
            // 
            this.internal1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.internal1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.internal1.Location = new System.Drawing.Point(0, 0);
            this.internal1.Name = "internal1";
            this.internal1.Size = new System.Drawing.Size(544, 220);
            this.internal1.TabIndex = 0;
            // 
            // TestForm
            // 
            this.ClientSize = new System.Drawing.Size(544, 220);
            this.Controls.Add(this.internal1);
            this.Name = "TestForm";
            this.TransparencyKey = System.Drawing.Color.LimeGreen;
            this.ResumeLayout(false);

        }

        #endregion

        private Pages.Internal internal1;
    }
}