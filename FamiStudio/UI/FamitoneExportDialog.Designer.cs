namespace FamiStudio
{
    partial class FamitoneExportDialog
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
            this.labelFormat = new System.Windows.Forms.Label();
            this.labelSeparate = new System.Windows.Forms.Label();
            this.buttonNo = new FamiStudio.NoFocusButton();
            this.buttonYes = new FamiStudio.NoFocusButton();
            this.comboBoxFormat = new System.Windows.Forms.ComboBox();
            this.checkSeperate = new System.Windows.Forms.CheckBox();
            this.labelName = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.checkedListSongs = new FamiStudio.PaddedCheckedListBox();
            this.SuspendLayout();
            // 
            // labelFormat
            // 
            this.labelFormat.AutoSize = true;
            this.labelFormat.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelFormat.Location = new System.Drawing.Point(2, 8);
            this.labelFormat.Name = "labelFormat";
            this.labelFormat.Size = new System.Drawing.Size(45, 13);
            this.labelFormat.TabIndex = 3;
            this.labelFormat.Text = "Format :";
            // 
            // labelSeparate
            // 
            this.labelSeparate.AutoSize = true;
            this.labelSeparate.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelSeparate.Location = new System.Drawing.Point(2, 38);
            this.labelSeparate.Name = "labelSeparate";
            this.labelSeparate.Size = new System.Drawing.Size(80, 13);
            this.labelSeparate.TabIndex = 5;
            this.labelSeparate.Text = "Separate Files :";
            // 
            // buttonNo
            // 
            this.buttonNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNo.FlatAppearance.BorderSize = 0;
            this.buttonNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNo.Image = global::FamiStudio.Properties.Resources.No;
            this.buttonNo.Location = new System.Drawing.Point(255, 355);
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(32, 32);
            this.buttonNo.TabIndex = 14;
            this.buttonNo.UseVisualStyleBackColor = true;
            this.buttonNo.Click += new System.EventHandler(this.buttonNo_Click);
            // 
            // buttonYes
            // 
            this.buttonYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonYes.FlatAppearance.BorderSize = 0;
            this.buttonYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonYes.Image = global::FamiStudio.Properties.Resources.Yes;
            this.buttonYes.Location = new System.Drawing.Point(217, 355);
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(32, 32);
            this.buttonYes.TabIndex = 13;
            this.buttonYes.UseVisualStyleBackColor = true;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // comboBoxFormat
            // 
            this.comboBoxFormat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxFormat.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(178)))), ((int)(((byte)(185)))), ((int)(((byte)(198)))));
            this.comboBoxFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFormat.FormattingEnabled = true;
            this.comboBoxFormat.Items.AddRange(new object[] {
            "NESASM",
            "CA65",
            "ASM6"});
            this.comboBoxFormat.Location = new System.Drawing.Point(114, 8);
            this.comboBoxFormat.Name = "comboBoxFormat";
            this.comboBoxFormat.Size = new System.Drawing.Size(176, 21);
            this.comboBoxFormat.TabIndex = 15;
            // 
            // checkSeperate
            // 
            this.checkSeperate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkSeperate.AutoSize = true;
            this.checkSeperate.Location = new System.Drawing.Point(114, 45);
            this.checkSeperate.Name = "checkSeperate";
            this.checkSeperate.Size = new System.Drawing.Size(15, 14);
            this.checkSeperate.TabIndex = 16;
            this.checkSeperate.UseVisualStyleBackColor = true;
            this.checkSeperate.CheckedChanged += new System.EventHandler(this.checkSeperate_CheckedChanged);
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelName.Location = new System.Drawing.Point(2, 68);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(78, 13);
            this.labelName.TabIndex = 7;
            this.labelName.Text = "Name Pattern :";
            // 
            // textBoxName
            // 
            this.textBoxName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(178)))), ((int)(((byte)(185)))), ((int)(((byte)(198)))));
            this.textBoxName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxName.Enabled = false;
            this.textBoxName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxName.Location = new System.Drawing.Point(114, 69);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(176, 26);
            this.textBoxName.TabIndex = 19;
            this.textBoxName.Text = "{project}_{song}";
            // 
            // checkedListSongs
            // 
            this.checkedListSongs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListSongs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(178)))), ((int)(((byte)(185)))), ((int)(((byte)(198)))));
            this.checkedListSongs.FormattingEnabled = true;
            this.checkedListSongs.Location = new System.Drawing.Point(5, 109);
            this.checkedListSongs.Name = "checkedListSongs";
            this.checkedListSongs.Size = new System.Drawing.Size(285, 232);
            this.checkedListSongs.TabIndex = 20;
            // 
            // FamitoneExportDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(51)))));
            this.ClientSize = new System.Drawing.Size(294, 394);
            this.ControlBox = false;
            this.Controls.Add(this.checkedListSongs);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.checkSeperate);
            this.Controls.Add(this.comboBoxFormat);
            this.Controls.Add(this.buttonNo);
            this.Controls.Add(this.buttonYes);
            this.Controls.Add(this.labelName);
            this.Controls.Add(this.labelSeparate);
            this.Controls.Add(this.labelFormat);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FamitoneExportDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RenameColorDialog_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label labelFormat;
        private System.Windows.Forms.Label labelSeparate;
        private NoFocusButton buttonYes;
        private NoFocusButton buttonNo;
        private System.Windows.Forms.ComboBox comboBoxFormat;
        private System.Windows.Forms.CheckBox checkSeperate;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.TextBox textBoxName;
        private PaddedCheckedListBox checkedListSongs;
    }
}