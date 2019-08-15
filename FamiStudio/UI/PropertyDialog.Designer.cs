namespace FamiStudio
{
    partial class PropertyDialog
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
            this.propertyPage = new FamiStudio.PropertyPage();
            this.buttonYes = new FamiStudio.NoFocusButton();
            this.buttonNo = new FamiStudio.NoFocusButton();
            this.SuspendLayout();
            // 
            // propertyPage
            // 
            this.propertyPage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.propertyPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(51)))));
            this.propertyPage.Dock = System.Windows.Forms.DockStyle.Top;
            this.propertyPage.Location = new System.Drawing.Point(0, 0);
            this.propertyPage.Name = "propertyPage";
            this.propertyPage.Padding = new System.Windows.Forms.Padding(3);
            this.propertyPage.Size = new System.Drawing.Size(298, 200);
            this.propertyPage.TabIndex = 17;
            this.propertyPage.PropertyWantsClose += new FamiStudio.PropertyPage.PropertyWantsCloseDelegate(this.propertyPage_PropertyWantsClose);
            // 
            // buttonYes
            // 
            this.buttonYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonYes.FlatAppearance.BorderSize = 0;
            this.buttonYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonYes.Image = global::FamiStudio.Properties.Resources.Yes;
            this.buttonYes.Location = new System.Drawing.Point(224, 361);
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(32, 32);
            this.buttonYes.TabIndex = 15;
            this.buttonYes.UseVisualStyleBackColor = true;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // buttonNo
            // 
            this.buttonNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNo.FlatAppearance.BorderSize = 0;
            this.buttonNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNo.Image = global::FamiStudio.Properties.Resources.No;
            this.buttonNo.Location = new System.Drawing.Point(261, 361);
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(32, 32);
            this.buttonNo.TabIndex = 16;
            this.buttonNo.UseVisualStyleBackColor = true;
            this.buttonNo.Click += new System.EventHandler(this.buttonNo_Click);
            // 
            // PropertyDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(51)))));
            this.ClientSize = new System.Drawing.Size(298, 398);
            this.ControlBox = false;
            this.Controls.Add(this.propertyPage);
            this.Controls.Add(this.buttonYes);
            this.Controls.Add(this.buttonNo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PropertyDialog";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Shown += new System.EventHandler(this.PropertyDialog_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PropertyDialog_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private NoFocusButton buttonNo;
        private NoFocusButton buttonYes;
        private PropertyPage propertyPage;
    }
}