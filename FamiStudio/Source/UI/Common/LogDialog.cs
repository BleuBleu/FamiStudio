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
            dialog = new PropertyDialog(800, parentForm.Bounds, false);
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
            dialog.UpdateModalEvents();

            if (AbortOperation)
                return;

            hasMessages = true;
            dialog.Properties.AppendText(0, msg);

            if (showImmediately && !dialog.Visible)
                dialog.ShowModal(parentForm);

            dialog.UpdateModalEvents();
        }

        public void ReportProgress(float progress)
        {
            dialog.UpdateModalEvents();

            if (AbortOperation)
                return;

            hasMessages = true;
            dialog.Properties.SetPropertyValue(1, progress);

            if (showImmediately && !dialog.Visible)
                dialog.ShowModal(parentForm);

            dialog.UpdateModalEvents();
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
    }
}
