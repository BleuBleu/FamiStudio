namespace FamiStudio
{
    partial class FamiStudioForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FamiStudioForm));
            this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sequencer = new Sequencer();
            this.pianoRoll = new PianoRoll();
            this.projectExplorer = new ProjectExplorer();
            this.toolbar = new Toolbar();
            this.tableLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayout
            // 
            this.tableLayout.ColumnCount = 1;
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayout.Controls.Add(this.sequencer, 0, 0);
            this.tableLayout.Controls.Add(this.pianoRoll, 0, 1);
            this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayout.Location = new System.Drawing.Point(0, 40);
            this.tableLayout.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayout.Name = "tableLayout";
            this.tableLayout.RowCount = 2;
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 298F));
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayout.Size = new System.Drawing.Size(1004, 641);
            this.tableLayout.TabIndex = 5;
            // 
            // sequencer
            // 
            this.sequencer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sequencer.Location = new System.Drawing.Point(0, 0);
            this.sequencer.Margin = new System.Windows.Forms.Padding(0);
            this.sequencer.Name = "sequencer";
            this.sequencer.Size = new System.Drawing.Size(1004, 298);
            this.sequencer.TabIndex = 3;
            // 
            // pianoRoll
            // 
            this.pianoRoll.CurrentInstrument = null;
            this.pianoRoll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pianoRoll.Location = new System.Drawing.Point(0, 298);
            this.pianoRoll.Margin = new System.Windows.Forms.Padding(0);
            this.pianoRoll.Name = "pianoRoll";
            this.pianoRoll.Size = new System.Drawing.Size(1004, 343);
            this.pianoRoll.TabIndex = 2;
            // 
            // projectExplorer
            // 
            this.projectExplorer.Dock = System.Windows.Forms.DockStyle.Right;
            this.projectExplorer.Location = new System.Drawing.Point(1004, 40);
            this.projectExplorer.Margin = new System.Windows.Forms.Padding(0);
            this.projectExplorer.Name = "projectExplorer";
            this.projectExplorer.Size = new System.Drawing.Size(260, 641);
            this.projectExplorer.TabIndex = 4;
            // 
            // toolbar
            // 
            this.toolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolbar.Location = new System.Drawing.Point(0, 0);
            this.toolbar.Name = "toolbar";
            this.toolbar.Size = new System.Drawing.Size(1264, 40);
            this.toolbar.TabIndex = 6;
            this.toolbar.ToolTip = "";
            // 
            // FamiStudioForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.tableLayout);
            this.Controls.Add(this.projectExplorer);
            this.Controls.Add(this.toolbar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "FamiStudioForm";
            this.Text = "FamiStudio";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.tableLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private PianoRoll pianoRoll;
        private ProjectExplorer projectExplorer;
        private System.Windows.Forms.TableLayoutPanel tableLayout;
        private Sequencer sequencer;
        private Toolbar toolbar;
    }
}

