using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class TutorialDialog : Form
    {
#if FAMISTUDIO_WINDOWS
        private const string GifDecDll = "GifDec.dll";
#elif FAMISTUDIO_MACOS
        private const string GifDecDll = "GifDec.dylib";
#else
        private const string GifDecDll = "GifDec.so";
#endif

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GifOpen(IntPtr data, int swap);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetWidth(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetHeight(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifAdvanceFrame(IntPtr gif, IntPtr buffer, int stride);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetFrameDelay(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern void GifClose(IntPtr gif);

        public static readonly string[] TutorialMessages = new[]
        {
            @"(1/9) Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest.",
            @"(2/9) To PAN around the piano roll or the sequencer, simply PRESS and HOLD the MIDDLE MOUSE BUTTON and DRAG around to smoothly move the viewport. Yes, that wheel on your mouse is also a button!",
            @"(3/9) To ZOOM in and out in the piano roll or the sequencer, simply rotate the mouse wheel.",
            @"(4/9) If you are on a TRACKPAD or a LAPTOP, simply enable TRACKPAD CONTROLS in the settings.",
            @"(5/9) To ADD things like patterns and notes, simply CLICK with the LEFT MOUSE BUTTON.",
            @"(6/9) To DELETE things like patterns, notes, instruments and songs, simply CLICK on them with the RIGHT MOUSE BUTTON.",
            @"(7/9) SNAPPING is ON by default and is expressed in BEATS. With the default settings, it will snap to 1/4 notes. You can change the snapping precision or disable it completely to create notes of different lengths!",
            @"(8/9) Always keep an eye on the TOOLTIPS! They change constantly as you move the mouse and they will teach you how to use the app! For the complete DOCUMENTATION and over 1 hour of VIDEO TUTORIAL, please click on the big QUESTION MARK!",
            @"(9/9) Join us on DISCORD to meet other FamiStudio users and share your songs with them! Link in the documentation (question mark icon in the toolbar)."
        };

        public static readonly string[] TutorialImages = new[]
        {
            "Tutorial0.gif",
            "Tutorial1.gif",
            "Tutorial2.gif",
            "Tutorial3.gif",
            "Tutorial4.gif",
            "Tutorial5.gif",
            "Tutorial6.gif",
            "Tutorial7.gif",
            "Tutorial8.gif",
        };

        int pageIndex = 0;

        //private NoFocusButton buttonRight;
        //private NoFocusButton buttonLeft;
        private Button buttonRight;
        private Button buttonLeft;
        private PictureBox pictureBox1;
        private Label label1;
        private CheckBox checkBoxDontShow;
        private ToolTip toolTip;

        private Timer gifTimer = new Timer();
        private IntPtr gif;
        private Bitmap gifBmp;
        private byte[] gifData;
        private GCHandle gifHandle;

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

            buttonRight.Left  = ClientSize.Width  - buttonRight.Width  - 10;
            buttonRight.Top   = ClientSize.Height - buttonRight.Height - 10;
            buttonLeft.Left   = buttonRight.Left - buttonLeft.Width - 10;
            buttonLeft.Top    = buttonRight.Top;

            toolTip.SetToolTip(buttonRight, "Next");
            toolTip.SetToolTip(buttonLeft, "Previous");

            gifTimer.Interval = 10;
            gifTimer.Tick += GifTimer_Tick;

            try
            {
                label1.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
                checkBoxDontShow.Font = label1.Font;
            }
            catch {}

            SetPage(0);
        }

        private void GifTimer_Tick(object sender, EventArgs e)
        {
            UpdateGif();
        }

        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            label1 = new Label();
            checkBoxDontShow = new CheckBox();
            buttonLeft = new Button();
            buttonRight = new Button();
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

            buttonLeft.FlatAppearance.BorderSize = 0;
            buttonLeft.FlatStyle = FlatStyle.Flat;
            buttonLeft.Location = new Point(676, 504);
            buttonLeft.Size = new Size(32, 32);
            buttonLeft.UseVisualStyleBackColor = true;
            buttonLeft.Click += new EventHandler(buttonLeft_Click);

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

        private void OpenGif(string filename)
        {
            if (gif != IntPtr.Zero)
            {
                CloseGif();
            }

            // GifDec works only with files, create a copy.
            // TODO : Change it to take a buffer as input.
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{filename}"))
            {
                gifData = new byte[stream.Length];
                stream.Read(gifData, 0, (int)stream.Length);
                stream.Close();
            }

            gifHandle = GCHandle.Alloc(gifData, GCHandleType.Pinned);
            
            gif = GifOpen(gifHandle.AddrOfPinnedObject(), 1);

            var width  = GifGetWidth(gif);
            var height = GifGetHeight(gif);

            gifBmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            pictureBox1.Image = gifBmp;
        }

        private void CloseGif()
        {
            if (gif != IntPtr.Zero)
            {
                gifData = null;
                gifHandle.Free();
                gifTimer.Stop();
                GifClose(gif);
                gif = IntPtr.Zero;
                gifBmp = null;
            }
        }

        private void UpdateGif()
        {
            var data = gifBmp.LockBits(new Rectangle(0, 0, gifBmp.Width, gifBmp.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GifAdvanceFrame(gif, data.Scan0, data.Stride);
            gifTimer.Interval = GifGetFrameDelay(gif);
            gifTimer.Start();
            gifBmp.UnlockBits(data);
            pictureBox1.Invalidate();
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            label1.Text = TutorialMessages[pageIndex];
            buttonLeft.Visible = pageIndex != 0;

            OpenGif(TutorialImages[pageIndex]);
            UpdateGif();

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

        public void ShowDialogAsync(IWin32Window parent, Action<DialogResult> callback)
        {
            callback(ShowDialog(parent));
        }
    }
}
