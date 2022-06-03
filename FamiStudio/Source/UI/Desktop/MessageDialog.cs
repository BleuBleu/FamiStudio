using System;
using System.IO;
using System.Collections.Generic;

namespace FamiStudio
{
    public class MessageDialog : Dialog
    {
        private int margin = DpiScaling.ScaleForWindow(16);

        private ImageBox image;
        private Label label;
        private Button[] buttons;
        private DialogResult[] results;

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
            "",       // None
            "Yes",    // OK
            "No",     // Cancel
            "",       // (unused)
            "",       // (unused)
            "",       // (unused)
            "Yes",    // Yes
            "No",     // No
        };

        public MessageDialog(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            Resize(DpiScaling.ScaleForWindow(700), DpiScaling.ScaleForWindow(500));
            CreateControls(text, buttons, icon);
        }

        private void CreateControls(string text, MessageBoxButtons btns, MessageBoxIcon icon)
        {
            image = new ImageBox("Yes");
            image.Move(margin, margin);

            label = new Label(text, true);
            label.Move(margin * 2 + image.Width, margin, width - margin * 3 - image.Width, label.Height);

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

            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var button = new Button("", ButtonIcons[(int)result]);
                button.Click += Button_Click;
                //button.Resize(buttonSize, buttonSize);
                //button.Move(Width - buttonSize * 2 - margin * 2, y);
                button.ToolTip = ButtonLabels[i];
                buttons[i] = button;
                AddControl(button);
            }

            AddControl(image);
            AddControl(label);
        }

        private void Button_Click(Control sender)
        {
            var result = results[Array.IndexOf(buttons, sender as Button)];
            Close(result);
        }

        protected override void OnShowDialog()
        {
            // MATTT : Do we need to wait here to do this?
            CenterToWindow();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled && e.Key == Keys.Escape)
            {
                Close(results[results.Length - 1]);
            }
        }
    }
}
