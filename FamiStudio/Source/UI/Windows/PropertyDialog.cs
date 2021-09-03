using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class PropertyDialog : Form
    {
        public delegate bool ValidateDelegate(PropertyPage props);
        public event ValidateDelegate ValidateProperties;

        public PropertyPage Properties => propertyPage;
        private bool top = false;
        private bool advancedPropertiesVisible = false;

        public PropertyDialog(int width, bool canAccept = true, bool canCancel = true, Form parent = null)
        {
            StartPosition = FormStartPosition.CenterParent;
            Init();
            Width = DpiScaling.ScaleForDialog(width);
            buttonYes.Visible = canAccept;
            buttonNo.Visible = canCancel;
            FormClosed += PropertyDialog_FormClosed;
        }

        public PropertyDialog(Point pt, int width, bool leftAlign = false, bool topAlign = false)
        {
            top   = topAlign;
            width = DpiScaling.ScaleForDialog(width);

            if (leftAlign)
                pt.X -= width;

            StartPosition = FormStartPosition.Manual;
            Location = pt;
            FormClosed += PropertyDialog_FormClosed;

            Init();

            Width = width;
        }

        private void PropertyDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Owner != null)
            {
                Owner.Enabled = true;
            }
        }

        private void Init()
        {
            InitializeComponent();

            string suffix = DpiScaling.Dialog >= 2.0f ? "@2x" : "";

            buttonYes.Image      = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonNo.Image       = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.No{suffix}.png"));
            buttonAdvanced.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.PlusSmall{suffix}.png"));

            toolTip.SetToolTip(buttonYes, "Accept");
            toolTip.SetToolTip(buttonNo, "Cancel");
            toolTip.SetToolTip(buttonAdvanced, "Toggle Advanced Options");
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.Style &= (~0x02000000); // WS_CLIPCHILDREN
                return p;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            UpdateLayout();

            if (top)
                Location = new Point(Location.X, Location.Y - Height);

            if (StartPosition == FormStartPosition.CenterParent)
                CenterToParent();
        }

        private void UpdateLayout()
        {
            buttonYes.Width  = DpiScaling.ScaleForDialog(buttonYes.Width);
            buttonYes.Height = DpiScaling.ScaleForDialog(buttonYes.Height);
            buttonNo.Width   = DpiScaling.ScaleForDialog(buttonNo.Width);
            buttonNo.Height  = DpiScaling.ScaleForDialog(buttonNo.Height);

            Height = propertyPage.Height + buttonNo.Height + 7;

            buttonYes.Top = propertyPage.Bottom;
            buttonNo.Top  = propertyPage.Bottom;

            if (buttonNo.Visible)
            {
                buttonYes.Left = propertyPage.Right - buttonYes.Width * 2 - 10;
                buttonNo.Left  = propertyPage.Right - buttonNo.Width - 5;
            }
            else
            {
                buttonYes.Left = propertyPage.Right - buttonNo.Width - 5;
            }

            if (propertyPage.HasAdvancedProperties)
            {
                buttonAdvanced.Visible = true;
                buttonAdvanced.Width   = DpiScaling.ScaleForMainWindow(buttonAdvanced.Width);
                buttonAdvanced.Height  = DpiScaling.ScaleForMainWindow(buttonAdvanced.Height);
                buttonAdvanced.Left    = 5;
                buttonAdvanced.Top     = propertyPage.Bottom + 0;
            }
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
            if (ValidateProperties == null || ValidateProperties.Invoke(propertyPage))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonAdvanced_Click(object sender, EventArgs e)
        {
            Debug.Assert(propertyPage.HasAdvancedProperties);

            advancedPropertiesVisible = !advancedPropertiesVisible;
            propertyPage.Build(advancedPropertiesVisible);
            UpdateLayout();

            var iconName = advancedPropertiesVisible ? "Minus" : "Plus";
            var suffix = DpiScaling.Dialog >= 2.0f ? "@2x" : "";
            buttonAdvanced.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{iconName}Small{suffix}.png"));
        }

        public void UpdateModalEvents()
        {
            if (Visible)
                Application.DoEvents();
        }

        public void ShowDialog(IWin32Window parent, Action<DialogResult> callback)
        {
            callback(ShowDialog());
        }

        public void ShowModal(FamiStudioForm form)
        {
            form.Enabled = false;
            Show(form);
        }

        public void StayModalUntilClosed()
        {
        }
    }
}
