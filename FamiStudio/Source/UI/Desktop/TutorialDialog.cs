using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class TutorialDialog : Dialog
    {
        private LocalizedString[] TutorialMessages = new LocalizedString[10];
        private LocalizedString DoNotShowAgainLabel;
        private LocalizedString WelcomeTitle;

        private int pageIndex = 0;
        private Button buttonRight;
        private Button buttonLeft;
        private ImageBox imageBox;
        private Label label;
        private CheckBox checkBoxDontShow;

        private int margin = DpiScaling.ScaleForWindow(8);
        private int imageSizeX = DpiScaling.ScaleForWindow(960);
        private int imageSizeY = DpiScaling.ScaleForWindow(540);
        private int labelSizeY = DpiScaling.ScaleForWindow(36);
        private int buttonSize = DpiScaling.ScaleForWindow(36);
        private int checkSizeY = DpiScaling.ScaleForWindow(16);

        private float gifTimer = 0.0f;
        private IntPtr gif;
        private Texture gifBmp;
        private int gifSizeX;
        private int gifSizeY;
        private byte[] gifData;
        private byte[] gifBuffer;
        private GCHandle gifHandle;

        public TutorialDialog(FamiStudioWindow win) : base(win, "")
        {
            Localization.Localize(this);

            Title = WelcomeTitle;

            Move(0, 0,
                imageSizeX + margin * 2,
                imageSizeY + margin * 4 + buttonSize + labelSizeY + titleBarSizeY);

            Init();
            SetTickEnabled(true);
        }

        private void Init()
        {
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
            label.Move(margin, margin + titleBarSizeY, width - margin, labelSizeY);

            imageBox = new ImageBox((Texture)null);
            imageBox.Move(margin, margin * 2 + labelSizeY + titleBarSizeY, imageSizeX, imageSizeY);
            imageBox.ScaleImage = DpiScaling.Window > 1;

            checkBoxDontShow = new CheckBox(false, DoNotShowAgainLabel);
            checkBoxDontShow.Move(margin, margin * 3 + labelSizeY + imageSizeY + titleBarSizeY, width - buttonSize * 3, checkSizeY);

            AddControl(buttonLeft);
            AddControl(buttonRight);
            AddControl(label);
            AddControl(imageBox);
            AddControl(checkBoxDontShow);

            SetPage(0);
            CenterToWindow();
        }

        private void OpenGif(string filename)
        {
            if (gif != IntPtr.Zero)
            {
                CloseGif();
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Tutorials.{filename}"))
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
            gifBmp = ParentWindow.Graphics.CreateEmptyTexture(gifSizeX, gifSizeY, TextureFormat.Rgb, DpiScaling.Window > 1.0f);
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
            ParentWindow.Graphics.UpdateTexture(gifBmp, 0, 0, gifSizeX, gifSizeY, gifBuffer);

            var decodeTime = (int)((Platform.TimeSeconds() - decodeStart) * 1000);

            gifTimer = (Gif.GetFrameDelay(gif) - decodeTime) / 1000.0f;
            MarkDirty();
        }

        public override void Tick(float delta)
        {
            gifTimer -= delta;
            if (gifTimer <= 0)
                UpdateGif();
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            label.Text = TutorialMessages[pageIndex];
            label.AdjustHeightForMultiline();
            buttonLeft.Visible = pageIndex != 0;

            OpenGif($"Tutorial{pageIndex}.gif");
            UpdateGif();

            buttonRight.ImageName = pageIndex == TutorialMessages.Length - 1 ? "Yes" : "ArrowRight";
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled && e.Key == Keys.Escape)
            {
                ButtonRight_Click(this);
            }
        }

        private void TutorialDialog_KeyDown(object sender, KeyEventArgs e)
        {
            ButtonRight_Click(null);
        }
    }
}
