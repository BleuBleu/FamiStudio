using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class TutorialDialog : Form
    {
        readonly string[] messages = new[]
        {
            @"Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest.",
            @"To PAN around the piano roll or the sequencer, simply PRESS and HOLD the MIDDLE MOUSE BUTTON and DRAG around to smoothly move the viewport. Yes, that wheel on your mouse is also a button!",
            @"To ZOOM in and out in the piano roll or the sequencer, simply rotate the mouse wheel.",
            @"If you are on a TRACKPAD or a LAPTOP, simply enable TRACKPAD CONTROLS in the settings.",
            @"To ADD things like patterns and notes, simply CLICK with the LEFT MOUSE BUTTON.",
            @"To DELETE things like patterns, notes, instruments and songs, simply CLICK on them with the RIGHT MOUSE BUTTON.",
            @"Always keep and eye on the TOOLTIPS! They change constantly as you move the mouse and they will teach you how to use the app! For the complete DOCUMENTATION and over 1 hour of VIDEO TUTORIAL, please click on the big QUESTION MARK!"
        };

        readonly string[] images = new[]
        {
            "Tutorial0.jpg",
            "Tutorial1.jpg",
            "Tutorial2.jpg",
            "Tutorial3.jpg",
            "Tutorial4.jpg",
            "Tutorial5.jpg",
            "Tutorial6.jpg"
        };

        int pageIndex = 0;

        public TutorialDialog()
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

            buttonRight.Left  = Width  - buttonRight.Width  - 16;
            buttonRight.Top   = Height - buttonRight.Height - 16;
            buttonLeft.Left   = buttonRight.Left - buttonLeft.Width - 16;
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
            pageIndex = Utils.Clamp(idx, 0, messages.Length - 1);
            pictureBox1.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{images[pageIndex]}"));
            label1.Text = messages[pageIndex];
            buttonLeft.Visible = pageIndex != 0;

            string suffix = Direct2DTheme.DialogScaling >= 2.0f ? "@2x" : "";
            buttonRight.Image = pageIndex == messages.Length - 1 ?
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
            if (pageIndex == messages.Length - 1)
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
