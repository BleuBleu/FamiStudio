using System;
using System.IO;
using System.Collections.Generic;

namespace FamiStudio
{
    public class MessageDialog : Dialog
    {
        private int margin         = DpiScaling.ScaleForWindow(16);
        private int imageSize      = DpiScaling.ScaleForWindow(32);
        private int buttonSize     = DpiScaling.ScaleForWindow(24);
        private int minDialogWidth = DpiScaling.ScaleForWindow(250);
        private int maxTextSize    = DpiScaling.ScaleForWindow(500);

        private ImageBox image;
        private Label label;
        private Button[] buttons;
        private DialogResult[] results;

        public override bool Modal => true;

        private static readonly string[] ButtonLabels = new []
        {
            "",       // None
            "Ok",     // OK
            "Cancel", // Cancel
            "",       // (unused)
            "",       // (unused)
            "",       // (unused)
            "Yes",    // Yes
            "No",     // No
        };

        private static readonly string[] ButtonIcons = new []
        {
            "",              // None
            "MessageYes",    // OK
            "MessageCancel", // Cancel
            "",              // (unused)
            "",              // (unused)
            "",              // (unused)
            "MessageYes",    // Yes
            "MessageNo",     // No
        };

        public MessageDialog(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons) : base(win, title)
        {
            CreateControls(text, buttons);
        }

        protected override void OnShowDialog()
        {
            Platform.Beep();
        }

        private void CreateControls(string text, MessageBoxButtons btns)
        {
            var imageName = "MessageInfo";
            var tint = Theme.LightGreyColor1;

            if (btns == MessageBoxButtons.YesNo ||
                btns == MessageBoxButtons.YesNoCancel)
            {
                imageName = "MessageQuestion";
                tint = Theme.YellowColor;
            }
            else if (text.ToLower().Contains("error"))
            {
                imageName = "MessageError";
                tint = Theme.DarkRedColor;
            }

            image = new ImageBox(imageName);
            image.Tint = tint;
            image.Move(margin, margin + titleBarSizeY, imageSize, imageSize);
            AddControl(image);

            label = new Label(text);
            label.Move(margin * 2 + image.Width, margin + titleBarSizeY, width - margin * 3 - image.Width, label.Height);
            AddControl(label);
            label.AutosizeWidth();

            if (label.Width > maxTextSize)
            {
                label.Multiline = true;
                label.Resize(maxTextSize, 1);
            }

            Resize(Math.Max(minDialogWidth, margin * 3 + image.Width  + label.Width), margin * 3 + label.Height + buttonSize + titleBarSizeY);

            switch (btns)
            {
                case MessageBoxButtons.OK:
                    results = new[] { DialogResult.OK };
                    break;
                case MessageBoxButtons.YesNo:
                    results = new[] { DialogResult.Yes, DialogResult.No };
                    break;
                case MessageBoxButtons.YesNoCancel:
                    results = new[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel };
                    break;
            }

            buttons = new Button[results.Length];

            var x = Width - margin;

            for (int i = results.Length - 1; i >= 0; i--)
            {
                var result = results[i];
                var button = new Button(ButtonIcons[(int)result], ButtonLabels[(int)result]);
                buttons[i] = button;
                AddControl(button);

                button.Click += Button_Click;
                button.Resize(buttonSize, buttonSize);
                button.AutosizeWidth();
                x -= button.Width;
                button.Move(x, height - margin - buttonSize);
            }

            CenterToWindow();
        }

        private void Button_Click(Control sender)
        {
            var result = results[Array.IndexOf(buttons, sender as Button)];
            Close(result);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Keys.Escape)
                {
                    Close(results[results.Length - 1]);
                }
                else if ((e.Key == Keys.Enter || e.Key == Keys.Y || e.Key == Keys.KeypadEnter) && results[0] == DialogResult.Yes)
                {
                    Close(results[0]);
                }
                else if (e.Key == Keys.N && results.Length >= 1 && results[1] == DialogResult.No)
                {
                    Close(results[1]);
                }
            }
        }
    }
}
