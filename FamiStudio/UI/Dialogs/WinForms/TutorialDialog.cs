using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class TutorialDialog : Form
    {
        int pageIndex = 0;

        public TutorialDialog(System.Drawing.Rectangle rect)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();

            string suffix = Direct2DTheme.DialogScaling >= 2.0f ? "@2x" : "";
            buttonLeft.Image   = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.ArrowLeft{suffix}.png"));

            Width  = (int)(Direct2DTheme.DialogScaling * Width);
            Height = (int)(Direct2DTheme.DialogScaling * Height);
            label1.Height = (int)(Direct2DTheme.DialogScaling * label1.Height);
            pictureBox1.Top = (label1.Top + label1.Height) + 8;
            pictureBox1.Height = (int)(pictureBox1.Width / 1.7777777f); // 16:9

            label1.ForeColor = ThemeBase.LightGreyFillColor2;
            checkBoxDontShow.ForeColor = ThemeBase.LightGreyFillColor2;

            buttonLeft.Width   = (int)(buttonLeft.Width   * Direct2DTheme.DialogScaling);
            buttonLeft.Height  = (int)(buttonLeft.Height  * Direct2DTheme.DialogScaling);
            buttonRight.Width  = (int)(buttonRight.Width  * Direct2DTheme.DialogScaling);
            buttonRight.Height = (int)(buttonRight.Height * Direct2DTheme.DialogScaling);

            buttonRight.Left  = Width  - buttonRight.Width  - 10;
            buttonRight.Top   = Height - buttonRight.Height - 10;
            buttonLeft.Left   = buttonRight.Left - buttonLeft.Width - 10;
            buttonLeft.Top    = buttonRight.Top;

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

            string suffix = Direct2DTheme.DialogScaling >= 2.0f ? "@2x" : "";
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
    }
}
