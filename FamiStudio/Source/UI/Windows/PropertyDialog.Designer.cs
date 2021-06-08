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
            this.components = new System.ComponentModel.Container();
            this.propertyPage = new PropertyPage();
            this.buttonYes = new NoFocusButton();
            this.buttonNo = new NoFocusButton();
            this.buttonAdvanced = new NoFocusButton();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
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
            this.propertyPage.PropertyWantsClose += new PropertyPage.PropertyWantsCloseDelegate(this.propertyPage_PropertyWantsClose);
            // 
            // buttonYes
            // 
            this.buttonYes.FlatAppearance.BorderSize = 0;
            this.buttonYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonYes.Location = new System.Drawing.Point(224, 361);
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(32, 32);
            this.buttonYes.TabIndex = 15;
            this.buttonYes.UseVisualStyleBackColor = true;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // buttonNo
            // 
            this.buttonNo.FlatAppearance.BorderSize = 0;
            this.buttonNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNo.Location = new System.Drawing.Point(261, 361);
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(32, 32);
            this.buttonNo.TabIndex = 16;
            this.buttonNo.UseVisualStyleBackColor = true;
            this.buttonNo.Click += new System.EventHandler(this.buttonNo_Click);
            // 
            // buttonAdvanced
            // 
            this.buttonAdvanced.FlatAppearance.BorderSize = 0;
            this.buttonAdvanced.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAdvanced.Location = new System.Drawing.Point(5, 361);
            this.buttonAdvanced.Name = "buttonAdvanced";
            this.buttonAdvanced.Size = new System.Drawing.Size(32, 32);
            this.buttonAdvanced.TabIndex = 18;
            this.buttonAdvanced.UseVisualStyleBackColor = true;
            this.buttonAdvanced.Visible = false;
            this.buttonAdvanced.Click += new System.EventHandler(this.buttonAdvanced_Click);
            // 
            // PropertyDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(42)))), ((int)(((byte)(48)))), ((int)(((byte)(51)))));
            this.ClientSize = new System.Drawing.Size(298, 398);
            this.ControlBox = false;
            this.Controls.Add(this.buttonAdvanced);
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
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PropertyDialog_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private NoFocusButton buttonNo;
        private NoFocusButton buttonYes;
        private NoFocusButton buttonAdvanced;
        private PropertyPage propertyPage;
        private System.Windows.Forms.ToolTip toolTip;
    }
}