using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class LogDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private bool hasMessages = false;
        private bool showImmediately;

        public unsafe LogDialog(FamiStudioForm parentForm, bool showImmediately = false, bool progressBar = false)
        {
            int width  = 800;
            int height = 400;
            int x = parentForm.Bounds.Left + (parentForm.Bounds.Width  - width)  / 2;
            int y = parentForm.Bounds.Top  + (parentForm.Bounds.Height - height) / 2;

            dialog = new PropertyDialog(x, y, width, height, false);
            dialog.Properties.AddMultilineString(null, ""); // 0
            if (progressBar)
                dialog.Properties.AddProgressBar(null, 0.0f); // 1
            dialog.Properties.Build();

            this.parentForm = parentForm;
            this.showImmediately = showImmediately;
        }

        public DialogResult ShowDialog()
        {
            return dialog.ShowDialog(parentForm);
        }

        public DialogResult ShowDialogIfMessages()
        {
            if (hasMessages)
            {
                return ShowDialog();
            }

            return DialogResult.Cancel;
        }

        public void LogMessage(string msg)
        {
            if (AbortOperation)
                return;

            hasMessages = true;
            dialog.Properties.AppendText(0, msg);
            Application.DoEvents();

            if (showImmediately && !dialog.Visible)
            {
                dialog.Show(parentForm);
                Application.DoEvents();
            }
        }

        public void ReportProgress(float progress)
        {
            if (AbortOperation)
                return;

            hasMessages = true;
            dialog.Properties.SetPropertyValue(1, progress);
            Application.DoEvents();

            if (showImmediately && !dialog.Visible)
            {
                dialog.Show(parentForm);
                Application.DoEvents();
            }
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
    }
}
