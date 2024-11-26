using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class VideoPreviewDialog : Dialog, IVideoEncoder, ILogOutput
    {
        private int margin = DpiScaling.ScaleForWindow(8);
        private int buttonSize = DpiScaling.ScaleForWindow(36);
        private int labelSizeY = DpiScaling.ScaleForWindow(18);
        private bool firstFrameAdded;
        private float targetFrameTime;
        private double lastFrameTime = -1;
        private Label label;
        private ImageBox imageBox;
        private Button buttonYes;
        private LocalizedString VideoPreviewTitle;
        private LocalizedString VideoPreviewLabel;
        private LocalizedString AcceptTooltip;

        public VideoPreviewDialog(FamiStudioWindow win, int resX, int resY, float fps) : base(win, "A")
        {
            Localization.Localize(this);

            var winSizeX = 0;
            var winSizeY = 0;
            var previewSizeX = 0;
            var previewSizeY = 0;
            var fraction = 4;

            for (; fraction >= 1; fraction--)
            {
                previewSizeX = resX * fraction / 4;
                previewSizeY = resY * fraction / 4;

                winSizeX = previewSizeX + margin * 2;
                winSizeY = margin * 4 + labelSizeY + previewSizeY + buttonSize + titleBarSizeY;
                
                if (winSizeX < win.Size.Width && 
                    winSizeY < win.Size.Height)
                {
                    break;
                }
            }

            Title = VideoPreviewTitle.Format(fraction * 100 / 4);

            Resize(winSizeX, winSizeY);

            label = new Label(VideoPreviewLabel, true);
            label.Move(margin, margin + titleBarSizeY, width - margin, labelSizeY);

            imageBox = new ImageBox((Texture)null);
            imageBox.Move(margin, margin * 2 + labelSizeY + titleBarSizeY, previewSizeX, previewSizeY);
            imageBox.StretchImageToFill = true;
            imageBox.FlipImage = true;

            buttonYes = new Button("Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Resize(buttonSize, buttonSize);
            buttonYes.ToolTip = AcceptTooltip;
            buttonYes.Move(width - buttonSize - margin, height - buttonSize - margin);

            AddControl(label);
            AddControl(imageBox);
            AddControl(buttonYes);

            targetFrameTime = 1.0f / fps;
            lastFrameTime = Platform.TimeSeconds();
        }

        public bool AbortOperation => ParentWindow == null;

        private void ButtonYes_Click(Control sender)
        {
            Close(DialogResult.OK);
        }

        protected override void OnShowDialog()
        {
            CenterToWindow();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled && e.Key == Keys.Escape)
            {
                Close(DialogResult.Cancel);
            }
        }

        public bool AddFrame(OffscreenGraphics graphics)
        {
            firstFrameAdded = true;
            label.Text = VideoPreviewLabel;
            imageBox.Image = graphics.GetTexture();
            if (ParentWindow == null)
            {
                imageBox.Image = null;
                return false;
            }
            ParentWindow.RunEventLoop();

            // Throttle to mimic target FPS.
            var currentFrameTime = Platform.TimeSeconds();
            var delta = currentFrameTime - lastFrameTime;
            if (delta < targetFrameTime)
                System.Threading.Thread.Sleep((int)((targetFrameTime - delta) * 1000));

            lastFrameTime = currentFrameTime;

            return true;
        }

        public bool BeginEncoding(int resX, int resY, int rateNumer, int rateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            return true;
        }

        public void EndEncoding(bool abort)
        {
            // Texture will be disposed, cant display anymore.
            imageBox.Image = null;
        }

        public void LogMessage(string msg)
        {
            if (!firstFrameAdded)
                label.Text = msg;
        }

        public void ReportProgress(float progress)
        {
            ParentWindow?.RunEventLoop();
        }
    }
}
