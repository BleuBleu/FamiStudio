using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class TutorialDialog : Form
    {
        int pageIndex = 0;

        public TutorialDialog()
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();

            string suffix = DpiScaling.Dialog >= 2.0f ? "@2x" : "";
            buttonLeft.Image   = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.ArrowLeft{suffix}.png"));

            Width         = DpiScaling.ScaleForDialog(Width);
            Height        = DpiScaling.ScaleForDialog(Height);
            label1.Height = DpiScaling.ScaleForDialog(label1.Height);

            pictureBox1.Top    = (label1.Top + label1.Height) + 8;
            pictureBox1.Height = (int)(pictureBox1.Width / 1.7777777f); // 16:9
            pictureBox1.Width  = DpiScaling.ScaleForDialog(pictureBox1.Width);
            pictureBox1.Height = DpiScaling.ScaleForDialog(pictureBox1.Height);

            label1.ForeColor           = Theme.LightGreyFillColor2;
            checkBoxDontShow.ForeColor = Theme.LightGreyFillColor2;

            buttonLeft.Width   = DpiScaling.ScaleForDialog(buttonLeft.Width);
            buttonLeft.Height  = DpiScaling.ScaleForDialog(buttonLeft.Height);
            buttonRight.Width  = DpiScaling.ScaleForDialog(buttonRight.Width);
            buttonRight.Height = DpiScaling.ScaleForDialog(buttonRight.Height);

            buttonRight.Left  = Width  - buttonRight.Width  - 10;
            buttonRight.Top   = Height - buttonRight.Height - 10;
            buttonLeft.Left   = buttonRight.Left - buttonLeft.Width - 10;
            buttonLeft.Top    = buttonRight.Top;

            toolTip.SetToolTip(buttonRight, "Next");
            toolTip.SetToolTip(buttonLeft, "Previous");

            try
            {
                label1.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
                checkBoxDontShow.Font = label1.Font;
            }
            catch {}

            SetPage(0);
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Messages.Length - 1);
            pictureBox1.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{TutorialMessages.Images[pageIndex]}"));
            label1.Text = TutorialMessages.Messages[pageIndex];
            buttonLeft.Visible = pageIndex != 0;

            string suffix = DpiScaling.Dialog >= 2.0f ? "@2x" : "";
            buttonRight.Image = pageIndex == TutorialMessages.Messages.Length - 1 ?
                Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png")) :
                Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.ArrowRight{suffix}.png"));
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

        private void buttonLeft_Click(object sender, EventArgs e)
        {
            SetPage(pageIndex - 1);
        }

        private void buttonRight_Click(object sender, EventArgs e)
        {
            if (pageIndex == TutorialMessages.Messages.Length - 1)
            {
                DialogResult = checkBoxDontShow.Checked ? DialogResult.OK : DialogResult.Cancel;
                Close();
            }
            else
            {
                SetPage(pageIndex + 1);
            }
        }

        private void TutorialDialog_KeyDown(object sender, KeyEventArgs e)
        {
            buttonRight_Click(null, EventArgs.Empty);
        }
    }
}
