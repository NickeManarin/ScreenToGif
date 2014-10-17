using ScreenToGif.Properties;

namespace ScreenToGif.Pages
{
    partial class CaptionOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CaptionOptions));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.flowFont = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFontTitle = new System.Windows.Forms.Label();
            this.pbFontColor = new System.Windows.Forms.PictureBox();
            this.lblFont = new System.Windows.Forms.LinkLabel();
            this.flowPercentage = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFontSize2 = new System.Windows.Forms.Label();
            this.numFontSizePercentage = new System.Windows.Forms.NumericUpDown();
            this.lblPercentageSize = new System.Windows.Forms.Label();
            this.flowVertical = new System.Windows.Forms.FlowLayoutPanel();
            this.lblVertical = new System.Windows.Forms.Label();
            this.rbTop = new System.Windows.Forms.RadioButton();
            this.rbVerticalCenter = new System.Windows.Forms.RadioButton();
            this.rbBottom = new System.Windows.Forms.RadioButton();
            this.flowSizeAs = new System.Windows.Forms.FlowLayoutPanel();
            this.lblSizeType = new System.Windows.Forms.Label();
            this.rbPercentage = new System.Windows.Forms.RadioButton();
            this.rbPoint = new System.Windows.Forms.RadioButton();
            this.flowHorizontal = new System.Windows.Forms.FlowLayoutPanel();
            this.lblHorizontal = new System.Windows.Forms.Label();
            this.rbLeft = new System.Windows.Forms.RadioButton();
            this.rbHorizontalCenter = new System.Windows.Forms.RadioButton();
            this.rbRight = new System.Windows.Forms.RadioButton();
            this.flowLayoutPanel8 = new System.Windows.Forms.FlowLayoutPanel();
            this.pbExample = new System.Windows.Forms.PictureBox();
            this.flowHatchBrush = new System.Windows.Forms.FlowLayoutPanel();
            this.cbUseHatch = new System.Windows.Forms.CheckBox();
            this.cbHatchBrush = new System.Windows.Forms.ComboBox();
            this.pbHatchColor = new System.Windows.Forms.PictureBox();
            this.flowUseOutline = new System.Windows.Forms.FlowLayoutPanel();
            this.cbUseOutline = new System.Windows.Forms.CheckBox();
            this.flowOutline = new System.Windows.Forms.FlowLayoutPanel();
            this.pbOutlineColor = new System.Windows.Forms.PictureBox();
            this.lblThick = new System.Windows.Forms.Label();
            this.numThick = new System.Windows.Forms.NumericUpDown();
            this.lblPointsDesc = new System.Windows.Forms.Label();
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.colorDialogOutline = new System.Windows.Forms.ColorDialog();
            this.colorDialogHatch = new System.Windows.Forms.ColorDialog();
            this.flowFont.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbFontColor)).BeginInit();
            this.flowPercentage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSizePercentage)).BeginInit();
            this.flowVertical.SuspendLayout();
            this.flowSizeAs.SuspendLayout();
            this.flowHorizontal.SuspendLayout();
            this.flowLayoutPanel8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbExample)).BeginInit();
            this.flowHatchBrush.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbHatchColor)).BeginInit();
            this.flowUseOutline.SuspendLayout();
            this.flowOutline.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOutlineColor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThick)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Image = global::ScreenToGif.Properties.Resources.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(229, 368);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(208, 44);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = global::ScreenToGif.Properties.Resources.btnCancel;
            this.btnCancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(242)))));
            this.btnOk.CausesValidation = false;
            this.btnOk.FlatAppearance.BorderColor = System.Drawing.Color.SkyBlue;
            this.btnOk.FlatAppearance.BorderSize = 0;
            this.btnOk.FlatAppearance.MouseDownBackColor = System.Drawing.Color.DodgerBlue;
            this.btnOk.FlatAppearance.MouseOverBackColor = System.Drawing.Color.WhiteSmoke;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.Image = global::ScreenToGif.Properties.Resources.Done;
            this.btnOk.Location = new System.Drawing.Point(-1, 368);
            this.btnOk.Margin = new System.Windows.Forms.Padding(0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(230, 44);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = global::ScreenToGif.Properties.Resources.btnDone;
            this.btnOk.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnOk.UseVisualStyleBackColor = false;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // flowFont
            // 
            this.flowFont.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowFont.Controls.Add(this.lblFontTitle);
            this.flowFont.Controls.Add(this.pbFontColor);
            this.flowFont.Controls.Add(this.lblFont);
            this.flowFont.Location = new System.Drawing.Point(8, 42);
            this.flowFont.Name = "flowFont";
            this.flowFont.Size = new System.Drawing.Size(581, 28);
            this.flowFont.TabIndex = 7;
            // 
            // lblFontTitle
            // 
            this.lblFontTitle.AutoSize = true;
            this.lblFontTitle.Location = new System.Drawing.Point(3, 3);
            this.lblFontTitle.Margin = new System.Windows.Forms.Padding(3);
            this.lblFontTitle.Name = "lblFontTitle";
            this.lblFontTitle.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblFontTitle.Size = new System.Drawing.Size(34, 18);
            this.lblFontTitle.TabIndex = 0;
            this.lblFontTitle.Text = "Font:";
            // 
            // pbFontColor
            // 
            this.pbFontColor.BackColor = System.Drawing.Color.White;
            this.pbFontColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbFontColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbFontColor.Location = new System.Drawing.Point(43, 3);
            this.pbFontColor.Name = "pbFontColor";
            this.pbFontColor.Size = new System.Drawing.Size(30, 20);
            this.pbFontColor.TabIndex = 27;
            this.pbFontColor.TabStop = false;
            this.pbFontColor.Click += new System.EventHandler(this.pbFontColor_Click);
            // 
            // lblFont
            // 
            this.lblFont.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblFont.AutoSize = true;
            this.lblFont.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblFont.Location = new System.Drawing.Point(79, 5);
            this.lblFont.Margin = new System.Windows.Forms.Padding(3);
            this.lblFont.Name = "lblFont";
            this.lblFont.Padding = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.lblFont.Size = new System.Drawing.Size(16, 16);
            this.lblFont.TabIndex = 26;
            this.lblFont.TabStop = true;
            this.lblFont.Text = "...";
            this.lblFont.Click += new System.EventHandler(this.lblFont_Click);
            // 
            // flowPercentage
            // 
            this.flowPercentage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowPercentage.Controls.Add(this.lblFontSize2);
            this.flowPercentage.Controls.Add(this.numFontSizePercentage);
            this.flowPercentage.Controls.Add(this.lblPercentageSize);
            this.flowPercentage.Location = new System.Drawing.Point(8, 76);
            this.flowPercentage.Name = "flowPercentage";
            this.flowPercentage.Size = new System.Drawing.Size(581, 28);
            this.flowPercentage.TabIndex = 9;
            // 
            // lblFontSize2
            // 
            this.lblFontSize2.AutoSize = true;
            this.lblFontSize2.Location = new System.Drawing.Point(3, 3);
            this.lblFontSize2.Margin = new System.Windows.Forms.Padding(3);
            this.lblFontSize2.Name = "lblFontSize2";
            this.lblFontSize2.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblFontSize2.Size = new System.Drawing.Size(57, 18);
            this.lblFontSize2.TabIndex = 0;
            this.lblFontSize2.Text = "Font Size:";
            // 
            // numFontSizePercentage
            // 
            this.numFontSizePercentage.DecimalPlaces = 1;
            this.numFontSizePercentage.Location = new System.Drawing.Point(66, 3);
            this.numFontSizePercentage.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFontSizePercentage.Name = "numFontSizePercentage";
            this.numFontSizePercentage.Size = new System.Drawing.Size(57, 23);
            this.numFontSizePercentage.TabIndex = 4;
            this.numFontSizePercentage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numFontSizePercentage.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numFontSizePercentage.ValueChanged += new System.EventHandler(this.numFontSizePercentage_ValueChanged);
            // 
            // lblPercentageSize
            // 
            this.lblPercentageSize.AutoSize = true;
            this.lblPercentageSize.Location = new System.Drawing.Point(129, 3);
            this.lblPercentageSize.Margin = new System.Windows.Forms.Padding(3);
            this.lblPercentageSize.Name = "lblPercentageSize";
            this.lblPercentageSize.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblPercentageSize.Size = new System.Drawing.Size(127, 18);
            this.lblPercentageSize.TabIndex = 5;
            this.lblPercentageSize.Text = "% of the image height.";
            // 
            // flowVertical
            // 
            this.flowVertical.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowVertical.Controls.Add(this.lblVertical);
            this.flowVertical.Controls.Add(this.rbTop);
            this.flowVertical.Controls.Add(this.rbVerticalCenter);
            this.flowVertical.Controls.Add(this.rbBottom);
            this.flowVertical.Location = new System.Drawing.Point(8, 110);
            this.flowVertical.Name = "flowVertical";
            this.flowVertical.Size = new System.Drawing.Size(581, 28);
            this.flowVertical.TabIndex = 10;
            // 
            // lblVertical
            // 
            this.lblVertical.AutoSize = true;
            this.lblVertical.Location = new System.Drawing.Point(3, 3);
            this.lblVertical.Margin = new System.Windows.Forms.Padding(3);
            this.lblVertical.Name = "lblVertical";
            this.lblVertical.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblVertical.Size = new System.Drawing.Size(108, 18);
            this.lblVertical.TabIndex = 0;
            this.lblVertical.Text = "Vertical Alignment:";
            // 
            // rbTop
            // 
            this.rbTop.AutoSize = true;
            this.rbTop.Image = global::ScreenToGif.Properties.Resources.alignTop16x;
            this.rbTop.Location = new System.Drawing.Point(117, 3);
            this.rbTop.Name = "rbTop";
            this.rbTop.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.rbTop.Size = new System.Drawing.Size(62, 21);
            this.rbTop.TabIndex = 11;
            this.rbTop.Text = global::ScreenToGif.Properties.Resources.Radio_Top;
            this.rbTop.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbTop.UseVisualStyleBackColor = true;
            this.rbTop.CheckedChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // rbVerticalCenter
            // 
            this.rbVerticalCenter.AutoSize = true;
            this.rbVerticalCenter.Image = global::ScreenToGif.Properties.Resources.centerVertical16x;
            this.rbVerticalCenter.Location = new System.Drawing.Point(185, 3);
            this.rbVerticalCenter.Name = "rbVerticalCenter";
            this.rbVerticalCenter.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.rbVerticalCenter.Size = new System.Drawing.Size(76, 21);
            this.rbVerticalCenter.TabIndex = 12;
            this.rbVerticalCenter.Text = global::ScreenToGif.Properties.Resources.Radio_Center;
            this.rbVerticalCenter.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbVerticalCenter.UseVisualStyleBackColor = true;
            this.rbVerticalCenter.CheckedChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // rbBottom
            // 
            this.rbBottom.AutoSize = true;
            this.rbBottom.Checked = true;
            this.rbBottom.Image = global::ScreenToGif.Properties.Resources.alignBottom16x;
            this.rbBottom.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.rbBottom.Location = new System.Drawing.Point(267, 3);
            this.rbBottom.Name = "rbBottom";
            this.rbBottom.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.rbBottom.Size = new System.Drawing.Size(81, 21);
            this.rbBottom.TabIndex = 13;
            this.rbBottom.TabStop = true;
            this.rbBottom.Text = global::ScreenToGif.Properties.Resources.Radio_Bottom;
            this.rbBottom.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbBottom.UseVisualStyleBackColor = true;
            this.rbBottom.CheckedChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // flowSizeAs
            // 
            this.flowSizeAs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowSizeAs.Controls.Add(this.lblSizeType);
            this.flowSizeAs.Controls.Add(this.rbPercentage);
            this.flowSizeAs.Controls.Add(this.rbPoint);
            this.flowSizeAs.Location = new System.Drawing.Point(8, 8);
            this.flowSizeAs.Name = "flowSizeAs";
            this.flowSizeAs.Size = new System.Drawing.Size(581, 28);
            this.flowSizeAs.TabIndex = 11;
            // 
            // lblSizeType
            // 
            this.lblSizeType.AutoSize = true;
            this.lblSizeType.Location = new System.Drawing.Point(3, 3);
            this.lblSizeType.Margin = new System.Windows.Forms.Padding(3);
            this.lblSizeType.Name = "lblSizeType";
            this.lblSizeType.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblSizeType.Size = new System.Drawing.Size(73, 18);
            this.lblSizeType.TabIndex = 0;
            this.lblSizeType.Text = "Font Size As:";
            // 
            // rbPercentage
            // 
            this.rbPercentage.AutoSize = true;
            this.rbPercentage.Location = new System.Drawing.Point(82, 3);
            this.rbPercentage.Name = "rbPercentage";
            this.rbPercentage.Size = new System.Drawing.Size(84, 19);
            this.rbPercentage.TabIndex = 11;
            this.rbPercentage.TabStop = true;
            this.rbPercentage.Text = global::ScreenToGif.Properties.Resources.Radio_Percentage;
            this.rbPercentage.UseVisualStyleBackColor = true;
            this.rbPercentage.CheckedChanged += new System.EventHandler(this.rbPercentage_CheckedChanged);
            // 
            // rbPoint
            // 
            this.rbPoint.AutoSize = true;
            this.rbPoint.Location = new System.Drawing.Point(172, 3);
            this.rbPoint.Name = "rbPoint";
            this.rbPoint.Size = new System.Drawing.Size(53, 19);
            this.rbPoint.TabIndex = 12;
            this.rbPoint.TabStop = true;
            this.rbPoint.Text = global::ScreenToGif.Properties.Resources.Radio_Point;
            this.rbPoint.UseVisualStyleBackColor = true;
            // 
            // flowHorizontal
            // 
            this.flowHorizontal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowHorizontal.Controls.Add(this.lblHorizontal);
            this.flowHorizontal.Controls.Add(this.rbLeft);
            this.flowHorizontal.Controls.Add(this.rbHorizontalCenter);
            this.flowHorizontal.Controls.Add(this.rbRight);
            this.flowHorizontal.Location = new System.Drawing.Point(8, 144);
            this.flowHorizontal.Name = "flowHorizontal";
            this.flowHorizontal.Size = new System.Drawing.Size(581, 28);
            this.flowHorizontal.TabIndex = 12;
            // 
            // lblHorizontal
            // 
            this.lblHorizontal.AutoSize = true;
            this.lblHorizontal.Location = new System.Drawing.Point(3, 3);
            this.lblHorizontal.Margin = new System.Windows.Forms.Padding(3);
            this.lblHorizontal.Name = "lblHorizontal";
            this.lblHorizontal.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblHorizontal.Size = new System.Drawing.Size(124, 18);
            this.lblHorizontal.TabIndex = 0;
            this.lblHorizontal.Text = "Horizontal Alignment:";
            // 
            // rbLeft
            // 
            this.rbLeft.AutoSize = true;
            this.rbLeft.Image = global::ScreenToGif.Properties.Resources.alignLeft16x;
            this.rbLeft.Location = new System.Drawing.Point(133, 3);
            this.rbLeft.Name = "rbLeft";
            this.rbLeft.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.rbLeft.Size = new System.Drawing.Size(61, 21);
            this.rbLeft.TabIndex = 11;
            this.rbLeft.Text = global::ScreenToGif.Properties.Resources.Radio_Left;
            this.rbLeft.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbLeft.UseVisualStyleBackColor = true;
            this.rbLeft.CheckedChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // rbHorizontalCenter
            // 
            this.rbHorizontalCenter.AutoSize = true;
            this.rbHorizontalCenter.Checked = true;
            this.rbHorizontalCenter.Image = global::ScreenToGif.Properties.Resources.centerHorizontal16x;
            this.rbHorizontalCenter.Location = new System.Drawing.Point(200, 3);
            this.rbHorizontalCenter.Name = "rbHorizontalCenter";
            this.rbHorizontalCenter.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.rbHorizontalCenter.Size = new System.Drawing.Size(76, 21);
            this.rbHorizontalCenter.TabIndex = 12;
            this.rbHorizontalCenter.TabStop = true;
            this.rbHorizontalCenter.Text = global::ScreenToGif.Properties.Resources.Radio_Center;
            this.rbHorizontalCenter.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbHorizontalCenter.UseVisualStyleBackColor = true;
            this.rbHorizontalCenter.CheckedChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // rbRight
            // 
            this.rbRight.AutoSize = true;
            this.rbRight.Image = global::ScreenToGif.Properties.Resources.alignRight16x;
            this.rbRight.Location = new System.Drawing.Point(282, 3);
            this.rbRight.Name = "rbRight";
            this.rbRight.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2);
            this.rbRight.Size = new System.Drawing.Size(69, 21);
            this.rbRight.TabIndex = 13;
            this.rbRight.Text = global::ScreenToGif.Properties.Resources.Radio_Right;
            this.rbRight.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.rbRight.UseVisualStyleBackColor = true;
            this.rbRight.CheckedChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // flowLayoutPanel8
            // 
            this.flowLayoutPanel8.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel8.Controls.Add(this.flowSizeAs);
            this.flowLayoutPanel8.Controls.Add(this.flowFont);
            this.flowLayoutPanel8.Controls.Add(this.flowPercentage);
            this.flowLayoutPanel8.Controls.Add(this.flowVertical);
            this.flowLayoutPanel8.Controls.Add(this.flowHorizontal);
            this.flowLayoutPanel8.Controls.Add(this.flowHatchBrush);
            this.flowLayoutPanel8.Controls.Add(this.flowUseOutline);
            this.flowLayoutPanel8.Location = new System.Drawing.Point(-1, 105);
            this.flowLayoutPanel8.Name = "flowLayoutPanel8";
            this.flowLayoutPanel8.Padding = new System.Windows.Forms.Padding(5);
            this.flowLayoutPanel8.Size = new System.Drawing.Size(438, 260);
            this.flowLayoutPanel8.TabIndex = 13;
            // 
            // pbExample
            // 
            this.pbExample.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbExample.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbExample.Location = new System.Drawing.Point(9, 9);
            this.pbExample.Margin = new System.Windows.Forms.Padding(0);
            this.pbExample.Name = "pbExample";
            this.pbExample.Size = new System.Drawing.Size(416, 90);
            this.pbExample.TabIndex = 14;
            this.pbExample.TabStop = false;
            // 
            // flowHatchBrush
            // 
            this.flowHatchBrush.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowHatchBrush.Controls.Add(this.cbUseHatch);
            this.flowHatchBrush.Controls.Add(this.cbHatchBrush);
            this.flowHatchBrush.Controls.Add(this.pbHatchColor);
            this.flowHatchBrush.Location = new System.Drawing.Point(8, 178);
            this.flowHatchBrush.Name = "flowHatchBrush";
            this.flowHatchBrush.Size = new System.Drawing.Size(581, 28);
            this.flowHatchBrush.TabIndex = 15;
            // 
            // cbUseHatch
            // 
            this.cbUseHatch.AutoSize = true;
            this.cbUseHatch.Location = new System.Drawing.Point(3, 3);
            this.cbUseHatch.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.cbUseHatch.Name = "cbUseHatch";
            this.cbUseHatch.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.cbUseHatch.Size = new System.Drawing.Size(116, 22);
            this.cbUseHatch.TabIndex = 17;
            this.cbUseHatch.Text = global::ScreenToGif.Properties.Resources.CB_UseHatch;
            this.cbUseHatch.UseVisualStyleBackColor = true;
            this.cbUseHatch.CheckedChanged += new System.EventHandler(this.cbUseHatch_CheckedChanged);
            // 
            // cbHatchBrush
            // 
            this.cbHatchBrush.Enabled = false;
            this.cbHatchBrush.FormattingEnabled = true;
            this.cbHatchBrush.Location = new System.Drawing.Point(122, 3);
            this.cbHatchBrush.Name = "cbHatchBrush";
            this.cbHatchBrush.Size = new System.Drawing.Size(200, 23);
            this.cbHatchBrush.TabIndex = 16;
            this.cbHatchBrush.SelectedValueChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // pbHatchColor
            // 
            this.pbHatchColor.BackColor = System.Drawing.Color.White;
            this.pbHatchColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbHatchColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbHatchColor.Enabled = false;
            this.pbHatchColor.Location = new System.Drawing.Point(328, 3);
            this.pbHatchColor.Name = "pbHatchColor";
            this.pbHatchColor.Size = new System.Drawing.Size(30, 22);
            this.pbHatchColor.TabIndex = 28;
            this.pbHatchColor.TabStop = false;
            this.pbHatchColor.Click += new System.EventHandler(this.pbHatchColor_Click);
            // 
            // flowUseOutline
            // 
            this.flowUseOutline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowUseOutline.Controls.Add(this.cbUseOutline);
            this.flowUseOutline.Controls.Add(this.flowOutline);
            this.flowUseOutline.Location = new System.Drawing.Point(8, 212);
            this.flowUseOutline.Name = "flowUseOutline";
            this.flowUseOutline.Size = new System.Drawing.Size(581, 29);
            this.flowUseOutline.TabIndex = 16;
            // 
            // cbUseOutline
            // 
            this.cbUseOutline.AutoSize = true;
            this.cbUseOutline.Location = new System.Drawing.Point(3, 3);
            this.cbUseOutline.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.cbUseOutline.Name = "cbUseOutline";
            this.cbUseOutline.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.cbUseOutline.Size = new System.Drawing.Size(90, 22);
            this.cbUseOutline.TabIndex = 18;
            this.cbUseOutline.Text = global::ScreenToGif.Properties.Resources.CB_UseOutline;
            this.cbUseOutline.UseVisualStyleBackColor = true;
            this.cbUseOutline.CheckedChanged += new System.EventHandler(this.cbUseOutline_CheckedChanged);
            // 
            // flowOutline
            // 
            this.flowOutline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowOutline.Controls.Add(this.pbOutlineColor);
            this.flowOutline.Controls.Add(this.lblThick);
            this.flowOutline.Controls.Add(this.numThick);
            this.flowOutline.Controls.Add(this.lblPointsDesc);
            this.flowOutline.Location = new System.Drawing.Point(93, 0);
            this.flowOutline.Margin = new System.Windows.Forms.Padding(0);
            this.flowOutline.Name = "flowOutline";
            this.flowOutline.Size = new System.Drawing.Size(295, 28);
            this.flowOutline.TabIndex = 9;
            // 
            // pbOutlineColor
            // 
            this.pbOutlineColor.BackColor = System.Drawing.Color.Black;
            this.pbOutlineColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbOutlineColor.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbOutlineColor.Location = new System.Drawing.Point(3, 3);
            this.pbOutlineColor.Name = "pbOutlineColor";
            this.pbOutlineColor.Size = new System.Drawing.Size(30, 20);
            this.pbOutlineColor.TabIndex = 3;
            this.pbOutlineColor.TabStop = false;
            this.pbOutlineColor.Click += new System.EventHandler(this.pbOutlineColor_Click);
            // 
            // lblThick
            // 
            this.lblThick.AutoSize = true;
            this.lblThick.Location = new System.Drawing.Point(39, 3);
            this.lblThick.Margin = new System.Windows.Forms.Padding(3);
            this.lblThick.Name = "lblThick";
            this.lblThick.Padding = new System.Windows.Forms.Padding(10, 3, 0, 0);
            this.lblThick.Size = new System.Drawing.Size(72, 18);
            this.lblThick.TabIndex = 0;
            this.lblThick.Text = "Thickness:";
            // 
            // numThick
            // 
            this.numThick.DecimalPlaces = 2;
            this.numThick.Increment = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numThick.Location = new System.Drawing.Point(117, 3);
            this.numThick.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numThick.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numThick.Name = "numThick";
            this.numThick.Size = new System.Drawing.Size(58, 23);
            this.numThick.TabIndex = 4;
            this.numThick.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numThick.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numThick.ValueChanged += new System.EventHandler(this.preview_ValueChanged);
            // 
            // lblPointsDesc
            // 
            this.lblPointsDesc.AutoSize = true;
            this.lblPointsDesc.Location = new System.Drawing.Point(181, 3);
            this.lblPointsDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblPointsDesc.Name = "lblPointsDesc";
            this.lblPointsDesc.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.lblPointsDesc.Size = new System.Drawing.Size(43, 18);
            this.lblPointsDesc.TabIndex = 6;
            this.lblPointsDesc.Text = "points.";
            // 
            // fontDialog
            // 
            this.fontDialog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            // 
            // colorDialog
            // 
            this.colorDialog.Color = System.Drawing.Color.White;
            // 
            // CaptionOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(434, 411);
            this.Controls.Add(this.pbExample);
            this.Controls.Add(this.flowLayoutPanel8);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(450, 450);
            this.Name = "CaptionOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Caption Options";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.CaptionOptions_Load);
            this.ResizeEnd += new System.EventHandler(this.CaptionOptions_ResizeEnd);
            this.Resize += new System.EventHandler(this.CaptionOptions_ResizeEnd);
            this.flowFont.ResumeLayout(false);
            this.flowFont.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbFontColor)).EndInit();
            this.flowPercentage.ResumeLayout(false);
            this.flowPercentage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSizePercentage)).EndInit();
            this.flowVertical.ResumeLayout(false);
            this.flowVertical.PerformLayout();
            this.flowSizeAs.ResumeLayout(false);
            this.flowSizeAs.PerformLayout();
            this.flowHorizontal.ResumeLayout(false);
            this.flowHorizontal.PerformLayout();
            this.flowLayoutPanel8.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbExample)).EndInit();
            this.flowHatchBrush.ResumeLayout(false);
            this.flowHatchBrush.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbHatchColor)).EndInit();
            this.flowUseOutline.ResumeLayout(false);
            this.flowUseOutline.PerformLayout();
            this.flowOutline.ResumeLayout(false);
            this.flowOutline.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbOutlineColor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThick)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.FlowLayoutPanel flowFont;
        private System.Windows.Forms.Label lblFontTitle;
        private System.Windows.Forms.LinkLabel lblFont;
        private System.Windows.Forms.FlowLayoutPanel flowPercentage;
        private System.Windows.Forms.Label lblFontSize2;
        private System.Windows.Forms.NumericUpDown numFontSizePercentage;
        private System.Windows.Forms.PictureBox pbFontColor;
        private System.Windows.Forms.FlowLayoutPanel flowVertical;
        private System.Windows.Forms.Label lblVertical;
        private System.Windows.Forms.RadioButton rbTop;
        private System.Windows.Forms.RadioButton rbVerticalCenter;
        private System.Windows.Forms.RadioButton rbBottom;
        private System.Windows.Forms.FlowLayoutPanel flowSizeAs;
        private System.Windows.Forms.Label lblSizeType;
        private System.Windows.Forms.RadioButton rbPercentage;
        private System.Windows.Forms.RadioButton rbPoint;
        private System.Windows.Forms.Label lblPercentageSize;
        private System.Windows.Forms.FlowLayoutPanel flowHorizontal;
        private System.Windows.Forms.Label lblHorizontal;
        private System.Windows.Forms.RadioButton rbLeft;
        private System.Windows.Forms.RadioButton rbHorizontalCenter;
        private System.Windows.Forms.RadioButton rbRight;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel8;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.PictureBox pbExample;
        private System.Windows.Forms.ColorDialog colorDialogOutline;
        private System.Windows.Forms.FlowLayoutPanel flowHatchBrush;
        private System.Windows.Forms.ComboBox cbHatchBrush;
        private System.Windows.Forms.CheckBox cbUseHatch;
        private System.Windows.Forms.PictureBox pbHatchColor;
        private System.Windows.Forms.ColorDialog colorDialogHatch;
        private System.Windows.Forms.FlowLayoutPanel flowUseOutline;
        private System.Windows.Forms.CheckBox cbUseOutline;
        private System.Windows.Forms.FlowLayoutPanel flowOutline;
        private System.Windows.Forms.PictureBox pbOutlineColor;
        private System.Windows.Forms.Label lblThick;
        private System.Windows.Forms.NumericUpDown numThick;
        private System.Windows.Forms.Label lblPointsDesc;
    }
}