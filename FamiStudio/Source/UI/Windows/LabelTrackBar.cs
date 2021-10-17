using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class LabelTrackBar : UserControl
    {
        private TrackBar trackBar;
        private Label    label;

        public TrackBar TrackBar => trackBar;
        public Label    Label    => label;

        public LabelTrackBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            trackBar = new TrackBar();
            label = new Label();

            trackBar.Dock = DockStyle.Fill;
            trackBar.Location = new Point(0, 0);
            trackBar.Margin = new Padding(0);
            trackBar.Size = new Size(446, 43);
            trackBar.TabIndex = 0;
            trackBar.TickStyle = TickStyle.Both;

            label.Dock = DockStyle.Right;
            label.Location = new Point(446, 0);
            label.Size = new Size(50, 43);
            label.TabIndex = 1;
            label.Text = "label1";
            label.TextAlign = ContentAlignment.MiddleCenter;

            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Margin = new Padding(0);
            Size = new Size(496, 43);

            SuspendLayout();
            Controls.Add(trackBar);
            Controls.Add(label);
            ResumeLayout(true);
        }
    }
}
