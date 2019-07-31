namespace FamiStudio
{
    partial class EditDPCMDialog
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
            this.upDownPitch = new System.Windows.Forms.NumericUpDown();
            this.labelPitch = new System.Windows.Forms.Label();
            this.labelLoop = new System.Windows.Forms.Label();
            this.buttonNo = new NoFocusButton();
            this.buttonYes = new NoFocusButton();
            this.checkLoop = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.upDownPitch)).BeginInit();
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
            this.textBox1.Size = new System.Drawing.Size(164, 26);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "This is a test";
            // 
            // upDownPitch
            // 
            this.upDownPitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownPitch.Location = new System.Drawing.Point(76, 37);
            this.upDownPitch.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.upDownPitch.Name = "upDownPitch";
            this.upDownPitch.Size = new System.Drawing.Size(93, 20);
            this.upDownPitch.TabIndex = 2;
            this.upDownPitch.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // labelPitch
            // 
            this.labelPitch.AutoSize = true;
            this.labelPitch.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelPitch.Location = new System.Drawing.Point(2, 37);
            this.labelPitch.Name = "labelPitch";
            this.labelPitch.Size = new System.Drawing.Size(37, 13);
            this.labelPitch.TabIndex = 3;
            this.labelPitch.Text = "Pitch :";
            // 
            // labelLoop
            // 
            this.labelLoop.AutoSize = true;
            this.labelLoop.ForeColor = System.Drawing.SystemColors.AppWorkspace;
            this.labelLoop.Location = new System.Drawing.Point(2, 67);
            this.labelLoop.Name = "labelLoop";
            this.labelLoop.Size = new System.Drawing.Size(37, 13);
            this.labelLoop.TabIndex = 5;
            this.labelLoop.Text = "Loop :";
            // 
            // buttonNo
            // 
            this.buttonNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNo.FlatAppearance.BorderSize = 0;
            this.buttonNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNo.Image = global::FamiStudio.Properties.Resources.No;
            this.buttonNo.Location = new System.Drawing.Point(137, 93);
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
            this.buttonYes.Location = new System.Drawing.Point(99, 93);
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(32, 32);
            this.buttonYes.TabIndex = 13;
            this.buttonYes.UseVisualStyleBackColor = true;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // checkLoop
            // 
            this.checkLoop.AutoSize = true;
            this.checkLoop.Location = new System.Drawing.Point(154, 67);
            this.checkLoop.Name = "checkLoop";
            this.checkLoop.Size = new System.Drawing.Size(15, 14);
            this.checkLoop.TabIndex = 15;
            this.checkLoop.UseVisualStyleBackColor = true;
            // 
            // EditDPCMDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(51)))));
            this.ClientSize = new System.Drawing.Size(173, 134);
            this.ControlBox = false;
            this.Controls.Add(this.checkLoop);
            this.Controls.Add(this.buttonNo);
            this.Controls.Add(this.buttonYes);
            this.Controls.Add(this.labelLoop);
            this.Controls.Add(this.labelPitch);
            this.Controls.Add(this.upDownPitch);
            this.Controls.Add(this.textBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditDPCMDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.EditDPCMDialog_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.upDownPitch)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.NumericUpDown upDownPitch;
        private System.Windows.Forms.Label labelPitch;
        private System.Windows.Forms.Label labelLoop;
        private NoFocusButton buttonYes;
        private NoFocusButton buttonNo;
        private System.Windows.Forms.CheckBox checkLoop;
    }
}