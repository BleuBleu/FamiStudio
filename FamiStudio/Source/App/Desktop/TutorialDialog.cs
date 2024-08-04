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
        private GifImageBox imageBox;
        private Label label;
        private CheckBox checkBoxDontShow;

        private int margin = DpiScaling.ScaleForWindow(8);
        private int imageSizeX = DpiScaling.ScaleForWindow(960);
        private int imageSizeY = DpiScaling.ScaleForWindow(540);
        private int labelSizeY = DpiScaling.ScaleForWindow(36);
        private int buttonSize = DpiScaling.ScaleForWindow(36);
        private int checkSizeY = DpiScaling.ScaleForWindow(16);

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

            imageBox = new GifImageBox();
            imageBox.Move(margin, margin * 2 + labelSizeY + titleBarSizeY, imageSizeX, imageSizeY);

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

        private void SetPage(int idx)
        {
            pageIndex = Utils.Clamp(idx, 0, TutorialMessages.Length - 1);
            label.Text = TutorialMessages[pageIndex];
            buttonLeft.Visible = pageIndex != 0;
            imageBox.LoadGifFromResource($"FamiStudio.Resources.Tutorials.Tutorial{pageIndex}.gif");
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
