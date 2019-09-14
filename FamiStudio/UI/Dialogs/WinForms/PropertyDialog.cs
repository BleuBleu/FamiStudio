using System;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class PropertyDialog : Form
    {
        public PropertyPage Properties => propertyPage;

        public PropertyDialog(int width, Point pt)
        {
            if (pt.X >= 0 && pt.Y >= 0)
            {
                StartPosition = FormStartPosition.Manual;
                Location = PointToScreen(new System.Drawing.Point(pt.X, pt.Y));
            }

            InitializeComponent();
            Width = width;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ExStyle |= 0x2000000; // WS_EX_COMPOSITED
                return p;
            }
        }

        private void PropertyDialog_Shown(object sender, EventArgs e)
        {
            Height = propertyPage.Height + buttonNo.Height + 5;
        }
        
        private void propertyPage_PropertyWantsClose(int idx)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        
        private void PropertyDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void buttonYes_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
