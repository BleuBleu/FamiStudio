namespace FamiStudio
{
    partial class SongEditDialog
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.upDownTempo = new System.Windows.Forms.NumericUpDown();
            this.labelTempo = new System.Windows.Forms.Label();
            this.upDownSpeed = new System.Windows.Forms.NumericUpDown();
            this.labelSpeed = new System.Windows.Forms.Label();
            this.upDownPatternLen = new System.Windows.Forms.NumericUpDown();
            this.labelPatternLen = new System.Windows.Forms.Label();
            this.upDownSongLen = new System.Windows.Forms.NumericUpDown();
            this.labelSongLen = new System.Windows.Forms.Label();
            this.labelBarLen = new System.Windows.Forms.Label();
            this.upDownBarLen = new System.Windows.Forms.DomainUpDown();
            this.buttonNo = new FamiStudio.NoFocusButton();
            this.buttonYes = new FamiStudio.NoFocusButton();
            this.pictureBox1 = new FamiStudio.NoInterpolationPictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.upDownTempo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownSpeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownPatternLen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownSongLen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(178)))), ((int)(((byte)(185)))), ((int)(((byte)(198)))));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(5, 5);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(202, 26);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "This is a test";
            // 
            // upDownTempo
            // 
            this.upDownTempo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownTempo.Location = new System.Drawing.Point(114, 37);
            this.upDownTempo.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.upDownTempo.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.upDownTempo.Name = "upDownTempo";
            this.upDownTempo.Size = new System.Drawing.Size(93, 20);
            this.upDownTempo.TabIndex = 2;
            this.upDownTempo.Value = new decimal(new int[] {
            150,
            0,
            0,
            0});
            // 
            // labelTempo
            // 
            this.labelTempo.AutoSize = true;
            this.labelTempo.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelTempo.Location = new System.Drawing.Point(2, 37);
            this.labelTempo.Name = "labelTempo";
            this.labelTempo.Size = new System.Drawing.Size(46, 13);
            this.labelTempo.TabIndex = 3;
            this.labelTempo.Text = "Tempo :";
            // 
            // upDownSpeed
            // 
            this.upDownSpeed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownSpeed.Location = new System.Drawing.Point(114, 67);
            this.upDownSpeed.Maximum = new decimal(new int[] {
            31,
            0,
            0,
            0});
            this.upDownSpeed.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownSpeed.Name = "upDownSpeed";
            this.upDownSpeed.Size = new System.Drawing.Size(93, 20);
            this.upDownSpeed.TabIndex = 4;
            this.upDownSpeed.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});
            // 
            // labelSpeed
            // 
            this.labelSpeed.AutoSize = true;
            this.labelSpeed.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelSpeed.Location = new System.Drawing.Point(2, 67);
            this.labelSpeed.Name = "labelSpeed";
            this.labelSpeed.Size = new System.Drawing.Size(44, 13);
            this.labelSpeed.TabIndex = 5;
            this.labelSpeed.Text = "Speed :";
            // 
            // upDownPatternLen
            // 
            this.upDownPatternLen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownPatternLen.Location = new System.Drawing.Point(114, 97);
            this.upDownPatternLen.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.upDownPatternLen.Minimum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.upDownPatternLen.Name = "upDownPatternLen";
            this.upDownPatternLen.Size = new System.Drawing.Size(93, 20);
            this.upDownPatternLen.TabIndex = 6;
            this.upDownPatternLen.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this.upDownPatternLen.ValueChanged += new System.EventHandler(this.upDownPatternLen_ValueChanged);
            // 
            // labelPatternLen
            // 
            this.labelPatternLen.AutoSize = true;
            this.labelPatternLen.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelPatternLen.Location = new System.Drawing.Point(2, 97);
            this.labelPatternLen.Name = "labelPatternLen";
            this.labelPatternLen.Size = new System.Drawing.Size(83, 13);
            this.labelPatternLen.TabIndex = 7;
            this.labelPatternLen.Text = "Pattern Length :";
            // 
            // upDownSongLen
            // 
            this.upDownSongLen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownSongLen.Location = new System.Drawing.Point(114, 157);
            this.upDownSongLen.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.upDownSongLen.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownSongLen.Name = "upDownSongLen";
            this.upDownSongLen.Size = new System.Drawing.Size(93, 20);
            this.upDownSongLen.TabIndex = 8;
            this.upDownSongLen.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelSongLen
            // 
            this.labelSongLen.AutoSize = true;
            this.labelSongLen.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelSongLen.Location = new System.Drawing.Point(2, 157);
            this.labelSongLen.Name = "labelSongLen";
            this.labelSongLen.Size = new System.Drawing.Size(74, 13);
            this.labelSongLen.TabIndex = 9;
            this.labelSongLen.Text = "Song Length :";
            // 
            // labelBarLen
            // 
            this.labelBarLen.AutoSize = true;
            this.labelBarLen.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelBarLen.Location = new System.Drawing.Point(2, 127);
            this.labelBarLen.Name = "labelBarLen";
            this.labelBarLen.Size = new System.Drawing.Size(65, 13);
            this.labelBarLen.TabIndex = 11;
            this.labelBarLen.Text = "Bar Length :";
            // 
            // upDownBarLen
            // 
            this.upDownBarLen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownBarLen.Location = new System.Drawing.Point(114, 127);
            this.upDownBarLen.Name = "upDownBarLen";
            this.upDownBarLen.ReadOnly = true;
            this.upDownBarLen.Size = new System.Drawing.Size(93, 20);
            this.upDownBarLen.TabIndex = 12;
            this.upDownBarLen.Text = "1";
            // 
            // buttonNo
            // 
            this.buttonNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNo.FlatAppearance.BorderSize = 0;
            this.buttonNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNo.Image = global::FamiStudio.Properties.Resources.No;
            this.buttonNo.Location = new System.Drawing.Point(172, 353);
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(32, 32);
            this.buttonNo.TabIndex = 14;
            this.buttonNo.UseVisualStyleBackColor = true;
            this.buttonNo.Click += new System.EventHandler(this.buttonNo_Click);
            // 
            // buttonYes
            // 
            this.buttonYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonYes.FlatAppearance.BorderSize = 0;
            this.buttonYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonYes.Image = global::FamiStudio.Properties.Resources.Yes;
            this.buttonYes.Location = new System.Drawing.Point(134, 353);
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(32, 32);
            this.buttonYes.TabIndex = 13;
            this.buttonYes.UseVisualStyleBackColor = true;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(6, 187);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(200, 160);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDoubleClick);
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseMove);
            // 
            // SongEditDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(51)))));
            this.ClientSize = new System.Drawing.Size(211, 392);
            this.ControlBox = false;
            this.Controls.Add(this.buttonNo);
            this.Controls.Add(this.buttonYes);
            this.Controls.Add(this.upDownBarLen);
            this.Controls.Add(this.labelBarLen);
            this.Controls.Add(this.labelSongLen);
            this.Controls.Add(this.upDownSongLen);
            this.Controls.Add(this.labelPatternLen);
            this.Controls.Add(this.upDownPatternLen);
            this.Controls.Add(this.labelSpeed);
            this.Controls.Add(this.upDownSpeed);
            this.Controls.Add(this.labelTempo);
            this.Controls.Add(this.upDownTempo);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.textBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SongEditDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RenameColorDialog_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.upDownTempo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownSpeed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownPatternLen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownSongLen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private NoInterpolationPictureBox pictureBox1;
        private System.Windows.Forms.NumericUpDown upDownTempo;
        private System.Windows.Forms.Label labelTempo;
        private System.Windows.Forms.NumericUpDown upDownSpeed;
        private System.Windows.Forms.Label labelSpeed;
        private System.Windows.Forms.NumericUpDown upDownPatternLen;
        private System.Windows.Forms.Label labelPatternLen;
        private System.Windows.Forms.NumericUpDown upDownSongLen;
        private System.Windows.Forms.Label labelSongLen;
        private System.Windows.Forms.Label labelBarLen;
        private System.Windows.Forms.DomainUpDown upDownBarLen;
        private NoFocusButton buttonYes;
        private NoFocusButton buttonNo;
    }
}