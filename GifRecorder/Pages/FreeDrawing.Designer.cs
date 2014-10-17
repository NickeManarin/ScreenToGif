using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class FreeDrawing
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FreeDrawing));
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnDone = new System.Windows.Forms.Button();
            this.pbSeparator = new System.Windows.Forms.PictureBox();
            this.cbCircle = new System.Windows.Forms.CheckBox();
            this.cbSquare = new System.Windows.Forms.CheckBox();
            this.cbEraser = new System.Windows.Forms.CheckBox();
            this.btnConfig = new System.Windows.Forms.Button();
            this.pbColor = new System.Windows.Forms.PictureBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.panelDrawing = new ScreenToGif.Controls.FreeDrawPanel();
            this.panelConfig = new System.Windows.Forms.TableLayoutPanel();
            this.trackBrush = new System.Windows.Forms.TrackBar();
            this.lblEraserSize = new System.Windows.Forms.Label();
            this.numEraser = new System.Windows.Forms.NumericUpDown();
            this.numBrush = new System.Windows.Forms.NumericUpDown();
            this.trackEraser = new System.Windows.Forms.TrackBar();
            this.lblBrushSize = new System.Windows.Forms.Label();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbColor)).BeginInit();
            this.panelConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBrush)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEraser)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBrush)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackEraser)).BeginInit();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnDone);
            this.flowLayoutPanel1.Controls.Add(this.pbSeparator);
            this.flowLayoutPanel1.Controls.Add(this.cbCircle);
            this.flowLayoutPanel1.Controls.Add(this.cbSquare);
            this.flowLayoutPanel1.Controls.Add(this.cbEraser);
            this.flowLayoutPanel1.Controls.Add(this.btnConfig);
            this.flowLayoutPanel1.Controls.Add(this.pbColor);
            this.flowLayoutPanel1.Controls.Add(this.btnClear);
            this.flowLayoutPanel1.Cursor = System.Windows.Forms.Cursors.Default;
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(384, 35);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.AutoSize = true;
            this.btnCancel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel_small;
            this.btnCancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCancel.Location = new System.Drawing.Point(315, 0);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 4);
            this.btnCancel.Size = new System.Drawing.Size(69, 34);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.TabStop = false;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnDone
            // 
            this.btnDone.AutoSize = true;
            this.btnDone.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnDone.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnDone.FlatAppearance.BorderSize = 0;
            this.btnDone.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDone.Image = global::ScreenToGif.Properties.Resources.Done_small;
            this.btnDone.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnDone.Location = new System.Drawing.Point(254, 0);
            this.btnDone.Margin = new System.Windows.Forms.Padding(0);
            this.btnDone.Name = "btnDone";
            this.btnDone.Padding = new System.Windows.Forms.Padding(0, 5, 0, 4);
            this.btnDone.Size = new System.Drawing.Size(61, 34);
            this.btnDone.TabIndex = 3;
            this.btnDone.TabStop = false;
            this.btnDone.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnDone.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnDone.UseVisualStyleBackColor = true;
            this.btnDone.Click += new System.EventHandler(this.btnDone_Click);
            // 
            // pbSeparator
            // 
            this.pbSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.pbSeparator.BackColor = System.Drawing.Color.Transparent;
            this.pbSeparator.Image = global::ScreenToGif.Properties.Resources.Separator;
            this.pbSeparator.Location = new System.Drawing.Point(249, 4);
            this.pbSeparator.Margin = new System.Windows.Forms.Padding(3, 4, 3, 3);
            this.pbSeparator.Name = "pbSeparator";
            this.pbSeparator.Size = new System.Drawing.Size(2, 27);
            this.pbSeparator.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbSeparator.TabIndex = 39;
            this.pbSeparator.TabStop = false;
            // 
            // cbCircle
            // 
            this.cbCircle.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbCircle.Checked = true;
            this.cbCircle.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCircle.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.cbCircle.FlatAppearance.BorderSize = 0;
            this.cbCircle.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.cbCircle.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.cbCircle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbCircle.Image = global::ScreenToGif.Properties.Resources.round;
            this.cbCircle.Location = new System.Drawing.Point(216, 0);
            this.cbCircle.Margin = new System.Windows.Forms.Padding(0);
            this.cbCircle.Name = "cbCircle";
            this.cbCircle.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.cbCircle.Size = new System.Drawing.Size(30, 34);
            this.cbCircle.TabIndex = 4;
            this.cbCircle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.cbCircle, global::ScreenToGif.Properties.Resources.Tooltip_RoundBrush);
            this.cbCircle.UseVisualStyleBackColor = true;
            this.cbCircle.Click += new System.EventHandler(this.cbCircle_Click);
            // 
            // cbSquare
            // 
            this.cbSquare.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbSquare.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.cbSquare.FlatAppearance.BorderSize = 0;
            this.cbSquare.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.cbSquare.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.cbSquare.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbSquare.Image = global::ScreenToGif.Properties.Resources.square;
            this.cbSquare.Location = new System.Drawing.Point(186, 0);
            this.cbSquare.Margin = new System.Windows.Forms.Padding(0);
            this.cbSquare.Name = "cbSquare";
            this.cbSquare.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.cbSquare.Size = new System.Drawing.Size(30, 34);
            this.cbSquare.TabIndex = 46;
            this.cbSquare.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.cbSquare, global::ScreenToGif.Properties.Resources.Tooltip_SquareBrush);
            this.cbSquare.UseVisualStyleBackColor = true;
            this.cbSquare.Click += new System.EventHandler(this.cbSquare_Click);
            // 
            // cbEraser
            // 
            this.cbEraser.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbEraser.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.cbEraser.FlatAppearance.BorderSize = 0;
            this.cbEraser.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.cbEraser.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.cbEraser.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbEraser.Image = global::ScreenToGif.Properties.Resources.eraser;
            this.cbEraser.Location = new System.Drawing.Point(156, 0);
            this.cbEraser.Margin = new System.Windows.Forms.Padding(0);
            this.cbEraser.Name = "cbEraser";
            this.cbEraser.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.cbEraser.Size = new System.Drawing.Size(30, 34);
            this.cbEraser.TabIndex = 47;
            this.cbEraser.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTip.SetToolTip(this.cbEraser, global::ScreenToGif.Properties.Resources.Tooltip_Eraser);
            this.cbEraser.UseVisualStyleBackColor = true;
            this.cbEraser.Click += new System.EventHandler(this.cbEraser_Click);
            // 
            // btnConfig
            // 
            this.btnConfig.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnConfig.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnConfig.FlatAppearance.BorderSize = 0;
            this.btnConfig.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnConfig.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfig.Image = global::ScreenToGif.Properties.Resources.Gear_17;
            this.btnConfig.Location = new System.Drawing.Point(126, 0);
            this.btnConfig.Margin = new System.Windows.Forms.Padding(0);
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Padding = new System.Windows.Forms.Padding(0, 5, 0, 3);
            this.btnConfig.Size = new System.Drawing.Size(30, 34);
            this.btnConfig.TabIndex = 42;
            this.btnConfig.TabStop = false;
            this.toolTip.SetToolTip(this.btnConfig, global::ScreenToGif.Properties.Resources.Tooltip_DrawConfig);
            this.btnConfig.UseVisualStyleBackColor = true;
            this.btnConfig.Click += new System.EventHandler(this.btnConfig_Click);
            // 
            // pbColor
            // 
            this.pbColor.BackColor = System.Drawing.Color.Black;
            this.pbColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbColor.Location = new System.Drawing.Point(98, 5);
            this.pbColor.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.pbColor.Name = "pbColor";
            this.pbColor.Size = new System.Drawing.Size(25, 25);
            this.pbColor.TabIndex = 44;
            this.pbColor.TabStop = false;
            this.toolTip.SetToolTip(this.pbColor, global::ScreenToGif.Properties.Resources.Tooltip_BrushColor);
            this.pbColor.Click += new System.EventHandler(this.pbColor_Click);
            // 
            // btnClear
            // 
            this.btnClear.AutoSize = true;
            this.btnClear.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnClear.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Image = global::ScreenToGif.Properties.Resources.Reset;
            this.btnClear.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnClear.Location = new System.Drawing.Point(35, 0);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Padding = new System.Windows.Forms.Padding(0, 5, 0, 4);
            this.btnClear.Size = new System.Drawing.Size(60, 34);
            this.btnClear.TabIndex = 45;
            this.btnClear.TabStop = false;
            this.btnClear.Text = global::ScreenToGif.Properties.Resources.btnClear;
            this.btnClear.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip.SetToolTip(this.btnClear, "Clear All");
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            this.colorDialog.FullOpen = true;
            // 
            // panelDrawing
            // 
            this.panelDrawing.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panelDrawing.BackColor = System.Drawing.Color.Transparent;
            this.panelDrawing.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panelDrawing.CachedBitmap = ((System.Drawing.Bitmap)(resources.GetObject("panelDrawing.CachedBitmap")));
            this.panelDrawing.Cursor = System.Windows.Forms.Cursors.Cross;
            this.panelDrawing.Location = new System.Drawing.Point(12, 41);
            this.panelDrawing.Name = "panelDrawing";
            this.panelDrawing.Size = new System.Drawing.Size(360, 124);
            this.panelDrawing.TabIndex = 4;
            this.panelDrawing.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelDrawing_MouseDown);
            this.panelDrawing.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelDrawing_MouseMove);
            this.panelDrawing.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panelDrawing_MouseUp);
            // 
            // panelConfig
            // 
            this.panelConfig.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelConfig.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.panelConfig.ColumnCount = 3;
            this.panelConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelConfig.Controls.Add(this.trackBrush, 2, 0);
            this.panelConfig.Controls.Add(this.lblEraserSize, 0, 1);
            this.panelConfig.Controls.Add(this.numEraser, 1, 1);
            this.panelConfig.Controls.Add(this.numBrush, 1, 0);
            this.panelConfig.Controls.Add(this.trackEraser, 2, 1);
            this.panelConfig.Controls.Add(this.lblBrushSize, 0, 0);
            this.panelConfig.Cursor = System.Windows.Forms.Cursors.Default;
            this.panelConfig.Location = new System.Drawing.Point(15, 43);
            this.panelConfig.Name = "panelConfig";
            this.panelConfig.RowCount = 2;
            this.panelConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.panelConfig.Size = new System.Drawing.Size(354, 69);
            this.panelConfig.TabIndex = 9;
            this.panelConfig.Visible = false;
            // 
            // trackBrush
            // 
            this.trackBrush.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBrush.AutoSize = false;
            this.trackBrush.Cursor = System.Windows.Forms.Cursors.Default;
            this.trackBrush.Location = new System.Drawing.Point(119, 3);
            this.trackBrush.Maximum = 50;
            this.trackBrush.Minimum = 1;
            this.trackBrush.Name = "trackBrush";
            this.trackBrush.Size = new System.Drawing.Size(232, 28);
            this.trackBrush.TabIndex = 2;
            this.trackBrush.Value = 5;
            this.trackBrush.ValueChanged += new System.EventHandler(this.trackBrush_ValueChanged);
            // 
            // lblEraserSize
            // 
            this.lblEraserSize.AutoSize = true;
            this.lblEraserSize.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblEraserSize.Location = new System.Drawing.Point(3, 37);
            this.lblEraserSize.Margin = new System.Windows.Forms.Padding(3);
            this.lblEraserSize.Name = "lblEraserSize";
            this.lblEraserSize.Size = new System.Drawing.Size(61, 15);
            this.lblEraserSize.TabIndex = 1;
            this.lblEraserSize.Text = "Eraser Size";
            // 
            // numEraser
            // 
            this.numEraser.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numEraser.Location = new System.Drawing.Point(70, 37);
            this.numEraser.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numEraser.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numEraser.Name = "numEraser";
            this.numEraser.Size = new System.Drawing.Size(43, 23);
            this.numEraser.TabIndex = 7;
            this.numEraser.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numEraser.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numEraser.ValueChanged += new System.EventHandler(this.numEraser_ValueChanged);
            // 
            // numBrush
            // 
            this.numBrush.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numBrush.Location = new System.Drawing.Point(70, 3);
            this.numBrush.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numBrush.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numBrush.Name = "numBrush";
            this.numBrush.Size = new System.Drawing.Size(43, 23);
            this.numBrush.TabIndex = 6;
            this.numBrush.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numBrush.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numBrush.ValueChanged += new System.EventHandler(this.numBrush_ValueChanged);
            // 
            // trackEraser
            // 
            this.trackEraser.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackEraser.AutoSize = false;
            this.trackEraser.Location = new System.Drawing.Point(119, 37);
            this.trackEraser.Maximum = 50;
            this.trackEraser.Minimum = 1;
            this.trackEraser.Name = "trackEraser";
            this.trackEraser.Size = new System.Drawing.Size(232, 29);
            this.trackEraser.TabIndex = 3;
            this.trackEraser.Value = 5;
            this.trackEraser.ValueChanged += new System.EventHandler(this.trackEraser_ValueChanged);
            // 
            // lblBrushSize
            // 
            this.lblBrushSize.AutoSize = true;
            this.lblBrushSize.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBrushSize.Location = new System.Drawing.Point(3, 3);
            this.lblBrushSize.Margin = new System.Windows.Forms.Padding(3);
            this.lblBrushSize.Name = "lblBrushSize";
            this.lblBrushSize.Size = new System.Drawing.Size(60, 15);
            this.lblBrushSize.TabIndex = 0;
            this.lblBrushSize.Text = "Brush Size";
            // 
            // FreeDrawing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::ScreenToGif.Properties.Resources.grid;
            this.ClientSize = new System.Drawing.Size(384, 176);
            this.Controls.Add(this.panelConfig);
            this.Controls.Add(this.panelDrawing);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Cursor = System.Windows.Forms.Cursors.No;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FreeDrawing";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Free Drawing";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FreeDraw_FormClosing);
            this.Load += new System.EventHandler(this.FreeDraw_Load);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSeparator)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbColor)).EndInit();
            this.panelConfig.ResumeLayout(false);
            this.panelConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBrush)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEraser)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numBrush)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackEraser)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.NumericUpDown numEraser;
        private System.Windows.Forms.NumericUpDown numBrush;
        private System.Windows.Forms.TrackBar trackEraser;
        private System.Windows.Forms.TrackBar trackBrush;
        private System.Windows.Forms.Label lblEraserSize;
        private System.Windows.Forms.Label lblBrushSize;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnDone;
        private Controls.FreeDrawPanel panelDrawing;
        private System.Windows.Forms.PictureBox pbSeparator;
        private System.Windows.Forms.Button btnConfig;
        private System.Windows.Forms.PictureBox pbColor;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.CheckBox cbCircle;
        private System.Windows.Forms.CheckBox cbSquare;
        private System.Windows.Forms.CheckBox cbEraser;
        private System.Windows.Forms.TableLayoutPanel panelConfig;
    }
}