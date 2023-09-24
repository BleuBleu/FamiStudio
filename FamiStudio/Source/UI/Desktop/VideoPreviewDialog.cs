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
        private float targetFrameTime;
        private double lastFrameTime = -1;
        private Label label;
        private ImageBox imageBox;
        private Button buttonYes;
        private LocalizedString VideoPreviewLabel;
        private LocalizedString AcceptTooltip;

        public VideoPreviewDialog(FamiStudioWindow win, int resX, int resY, float fps) : base(win, "Video Preview")
        {
            Localization.Localize(this);

            Resize(resX + margin * 2, margin * 4 + labelSizeY + resY + buttonSize + titleBarSizeY);

            label = new Label(VideoPreviewLabel, true);
            label.Move(margin, margin + titleBarSizeY, width - margin, labelSizeY);

            imageBox = new ImageBox((Bitmap)null);
            imageBox.Move(margin, margin * 2 + labelSizeY + titleBarSizeY, resX, resY);
            imageBox.ScaleImage = true;
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

        public bool AddFrame(OffscreenGraphics graphics)
        {
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
        }

        public void LogMessage(string msg)
        {
            label.Text = msg;
        }

        public void ReportProgress(float progress)
        {
            ParentWindow?.RunEventLoop();
        }
    }
}
