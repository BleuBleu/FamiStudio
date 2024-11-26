using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FamiStudio
{
    public class TutorialDialog : TopBarDialog
    {
        private int margin = DpiScaling.ScaleForWindow(4);

        private LocalizedString[] TutorialMessages = new LocalizedString[12];
        private LocalizedString WelcomeTitle;

        private int pageIndex = -1;
        private Label label;
        private GifImageBox image;

        public TutorialDialog(FamiStudioWindow win) : base(win)
        {
            Localization.Localize(this);

            CancelButtonImage = "ArrowLeft";
            AcceptButtonImage = "ArrowRight";
            CancelButtonVisible = false;

            label = new Label("");
            label.Multiline = true;
            image = new GifImageBox();
            image.StretchImageToFill = DpiScaling.Window > 1.0f;
            titleLabel.Text = WelcomeTitle;

            AddControl(label);
            AddControl(image);

            SetPage(0); 
        }

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            label.Text = TutorialMessages[pageIndex];
            CancelButtonVisible = pageIndex != 0;
            AcceptButtonImage = pageIndex == TutorialMessages.Length - 1 ? "Yes" : "ArrowRight";
            image.LoadGifFromResource($"FamiStudio.Resources.Tutorials.MobileTutorial{pageIndex + 1}.gif");
            RepositionControls();
        }

        protected override void ButtonAccept_Click(Control sender)
        {
            if (pageIndex == TutorialMessages.Length - 1)
            {
                Close(DialogResult.OK);
            }
            else
            {
                SetPage(pageIndex + 1);
            }
        }

        protected override void ButtonCancel_Click(Control sender)
        {
            SetPage(pageIndex - 1);
        }

        protected override void OnCloseDialog(DialogResult res)
        {
            Utils.DisposeAndNullify(ref image);
        }

        private void RepositionControls()
        {
            // Size of our tutorial gifs
            var ratio = 1100.0f / 540.0f;
            var actualWidth = clientRect.Width - margin * 2;

            var imgSizeX = (int)(actualWidth);
            var imgSizeY = (int)(actualWidth / ratio);

            var res = Platform.GetScreenResolution();
            var maxHeight = Math.Min(res.Width, res.Height) - topBarHeight * 5 / 2;

            if (imgSizeY > maxHeight)
            {
                ratio = maxHeight / (float)imgSizeY;
                imgSizeX = (int)Math.Round(imgSizeX * ratio);
                imgSizeY = (int)Math.Round(imgSizeY * ratio);
            }

            label.Move(clientRect.Left + margin, clientRect.Top + margin, actualWidth, 1);
            image.Move(clientRect.Left + margin + (actualWidth - imgSizeX) / 2, label.Bottom + margin, imgSizeX, imgSizeY);
        }

        public override void OnWindowResize(EventArgs e)
        {
            base.OnWindowResize(e);
            RepositionControls();
        }
    }
}
