using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class TutorialDialog : Dialog
    {
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

        private int pageIndex = 0;
        private Button buttonRight;
        private Button buttonLeft;
        private ImageBox imageBox;
        private Label label;
        private CheckBox checkBoxDontShow;

        private int margin     = DpiScaling.ScaleForWindow(8);
        private int imageSizeX = DpiScaling.ScaleForWindow(960);
        private int imageSizeY = DpiScaling.ScaleForWindow(540);
        private int labelSizeY = DpiScaling.ScaleForWindow(36);
        private int buttonSize = DpiScaling.ScaleForWindow(36);
        private int checkSizeY = DpiScaling.ScaleForWindow(16);

        private float gifTimer = 0.0f;
        private IntPtr gif;
        private Bitmap gifBmp;
        private int gifSizeX;
        private int gifSizeY;
        private byte[] gifData;
        private byte[] gifBuffer;
        private GCHandle gifHandle;

        public TutorialDialog()
        {
            Move(0, 0, 
                imageSizeX + margin * 2, 
                imageSizeY + margin * 4 + buttonSize + labelSizeY);

            Init();
        }

        private void Init()
        {
            // MATTT : This doesnt work with scaling i think.
            buttonLeft = new Button("ArrowLeft", null);
            buttonLeft.Click += ButtonLeft_Click;
            buttonLeft.Resize(buttonSize, buttonSize);
            buttonLeft.ToolTip = "Previous";
            buttonLeft.Move(width - buttonSize * 2 - margin * 2, height - buttonSize - margin);

            buttonRight = new Button("ArrowRight", null);
            buttonRight.Click += ButtonRight_Click;
            buttonRight.Resize(buttonSize, buttonSize);
            buttonRight.ToolTip = "Next";
            buttonRight.Move(width - buttonSize - margin, height - buttonSize - margin);

            label = new Label("This is a nice label", true);
            label.Move(margin, margin, width - margin, labelSizeY);

            imageBox = new ImageBox("VideoWatermark");
            imageBox.Move(margin, margin * 2 + labelSizeY, imageSizeX, imageSizeY);

            checkBoxDontShow = new CheckBox(false, "Do not show again");
            checkBoxDontShow.Move(margin, margin * 3 + labelSizeY + imageSizeY, width - buttonSize * 3, checkSizeY);

            AddControl(buttonLeft);
            AddControl(buttonRight);
            AddControl(label);
            AddControl(imageBox);
            AddControl(checkBoxDontShow);

            SetPage(0);
        }

        private void OpenGif(string filename)
        {
            if (gif != IntPtr.Zero)
            {
                CloseGif();
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{filename}"))
            {
                gifData = new byte[stream.Length];
                stream.Read(gifData, 0, (int)stream.Length);
                stream.Close();
            }

            gifHandle = GCHandle.Alloc(gifData, GCHandleType.Pinned);
            gif = Gif.Open(gifHandle.AddrOfPinnedObject(), 1);
            gifSizeX = Gif.GetWidth(gif);
            gifSizeY = Gif.GetHeight(gif);
            gifBuffer = new byte[gifSizeX * gifSizeY * 3];
            gifBmp = ParentWindow.Graphics.CreateEmptyBitmap(gifSizeX, gifSizeY, false, windowScaling > 1.0f);
            imageBox.Image = gifBmp;
        }

        private void CloseGif()
        {
            if (gif != IntPtr.Zero)
            {
                gifData = null;
                gifBuffer = null;
                Gif.Close(gif);
                gif = IntPtr.Zero;
                gifHandle.Free();
                gifBmp.Dispose();
                gifBmp = null;
            }
        }

        private unsafe void UpdateGif()
        {
            var decodeStart = Platform.TimeSeconds();

            Gif.AdvanceFrame(gif);
            fixed (byte* p = &gifBuffer[0])
                Gif.RenderFrame(gif, new IntPtr(p), gifSizeX * 3, 3);
            ParentWindow.Graphics.UpdateBitmap(gifBmp, 0, 0, gifSizeX, gifSizeY, gifBuffer);

            var decodeTime = (int)((Platform.TimeSeconds() - decodeStart) * 1000);

            gifTimer = (Gif.GetFrameDelay(gif) - decodeTime) / 1000.0f;
            MarkDirty();
        }

        public override void Tick(float delta)
        {
            gifTimer -= delta;

            // MATTT : Pass remainder to next frame!
            if (gifTimer <= 0)
                UpdateGif();
        }

        protected override void OnShowDialog()
        {
            CenterToWindow();
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            label.Text = TutorialMessages[pageIndex];
            label.ResizeForMultiline();
            buttonLeft.Visible = pageIndex != 0;

            OpenGif(TutorialImages[pageIndex]);
            UpdateGif();

            buttonRight.Image = pageIndex == TutorialMessages.Length - 1 ? "Yes" : "ArrowRight";
        }

        private void ButtonLeft_Click(Control sender)
        {
            SetPage(pageIndex - 1);
        }

        private void ButtonRight_Click(Control sender)
        {
            if (pageIndex == TutorialMessages.Length - 1)
            {
                Close(checkBoxDontShow.Checked ? DialogResult.OK : DialogResult.Cancel);
            }
            else
            {
                SetPage(pageIndex + 1);
            }
        }

        private void TutorialDialog_KeyDown(object sender, KeyEventArgs e)
        {
            ButtonRight_Click(null);
        }
    }
}
