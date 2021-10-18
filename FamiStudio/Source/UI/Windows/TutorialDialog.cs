using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class TutorialDialog : Form
    {
        int pageIndex = 0;

        private NoFocusButton buttonRight;
        private NoFocusButton buttonLeft;
        private PictureBox pictureBox1;
        private Label label1;
        private CheckBox checkBoxDontShow;
        private ToolTip toolTip;

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

        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            label1 = new Label();
            checkBoxDontShow = new CheckBox();
            buttonLeft = new NoFocusButton();
            buttonRight = new NoFocusButton();
            toolTip = new ToolTip();

            pictureBox1.Location = new Point(10, 88);
            pictureBox1.Size = new Size(736, 414);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabStop = false;

            label1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label1.Location = new Point(10, 10);
            label1.Size = new Size(736, 64);
            label1.Text = "Welcome To FamiStudio!";

            checkBoxDontShow.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            checkBoxDontShow.AutoSize = true;
            checkBoxDontShow.Location = new Point(10, 513);
            checkBoxDontShow.Size = new Size(115, 17);
            checkBoxDontShow.Text = "Do not show again";
            checkBoxDontShow.UseVisualStyleBackColor = true;

            buttonLeft.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonLeft.FlatAppearance.BorderSize = 0;
            buttonLeft.FlatStyle = FlatStyle.Flat;
            buttonLeft.Location = new Point(676, 504);
            buttonLeft.Size = new Size(32, 32);
            buttonLeft.UseVisualStyleBackColor = true;
            buttonLeft.Click += new EventHandler(buttonLeft_Click);

            buttonRight.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonRight.FlatAppearance.BorderSize = 0;
            buttonRight.FlatStyle = FlatStyle.Flat;
            buttonRight.Location = new Point(714, 504);
            buttonRight.Size = new Size(32, 32);
            buttonRight.UseVisualStyleBackColor = true;
            buttonRight.Click += new EventHandler(buttonRight_Click);

            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.DarkGreyFillColor1;
            ClientSize = new Size(754, 548);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ControlBox = false;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            KeyDown += new KeyEventHandler(TutorialDialog_KeyDown);

            SuspendLayout();
            Controls.Add(checkBoxDontShow);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Controls.Add(buttonLeft);
            Controls.Add(buttonRight);
            ResumeLayout(true);
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            pictureBox1.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{TutorialImages[pageIndex]}"));
            label1.Text = TutorialMessages[pageIndex];
            buttonLeft.Visible = pageIndex != 0;

            string suffix = DpiScaling.Dialog >= 2.0f ? "@2x" : "";
            buttonRight.Image = pageIndex == TutorialMessages.Length - 1 ?
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
            if (pageIndex == TutorialMessages.Length - 1)
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
