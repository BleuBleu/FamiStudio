using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class PropertyDialog : Form
    {
        public PropertyPage Properties => propertyPage;
        private bool top = false;

        public PropertyDialog(Point pt, int width, bool leftAlign = false, bool topAlign = false)
        {
            top = topAlign;
            width = (int)(width * Direct2DTheme.DialogScaling);

            if (leftAlign)
                pt.X -= width;

            StartPosition = FormStartPosition.Manual;
            Location = pt;

            Init();

            Width = width;
        }

        public PropertyDialog(int x, int y, int width, int height)
        {
            width = (int)(width * Direct2DTheme.DialogScaling);
            Init();
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Width = width;
        }

        private void Init()
        {
            InitializeComponent();

            string suffix = Direct2DTheme.DialogScaling >= 2.0f ? "@2x" : "";
            buttonYes.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonNo.Image  = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.No{suffix}.png"));
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
            buttonYes.Width  = (int)(buttonYes.Width  * Direct2DTheme.DialogScaling);
            buttonYes.Height = (int)(buttonYes.Height * Direct2DTheme.DialogScaling);
            buttonNo.Width   = (int)(buttonNo.Width   * Direct2DTheme.DialogScaling);
            buttonNo.Height  = (int)(buttonNo.Height  * Direct2DTheme.DialogScaling);

            Height = propertyPage.Height + buttonNo.Height + 7;

            buttonYes.Left = propertyPage.Right - buttonYes.Width * 2 - 10;
            buttonYes.Top  = propertyPage.Bottom + 0;
            buttonNo.Left  = propertyPage.Right - buttonNo.Width - 5;
            buttonNo.Top   = propertyPage.Bottom + 0;

            if (top)
                Location = new Point(Location.X, Location.Y - Height);
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
