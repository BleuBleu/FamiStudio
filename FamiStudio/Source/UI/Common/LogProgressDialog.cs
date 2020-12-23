using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class LogProgressDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private bool hasMessages = false;

        public unsafe LogProgressDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog(800, parentForm.Bounds, false);
            dialog.Properties.AddMultilineString(null, ""); // 0
            dialog.Properties.AddProgressBar(null, 0.0f); // 1
            dialog.Properties.Build();
        }

        public void LogMessage(string msg)
        {
            dialog.UpdateModalEvents();

            if (AbortOperation)
                return;

            hasMessages = true;
            if (!dialog.Visible)
                dialog.ShowModal(parentForm);
            dialog.Properties.AppendText(0, msg);
            dialog.UpdateModalEvents();
        }

        public void ReportProgress(float progress)
        {
            dialog.UpdateModalEvents();

            if (AbortOperation)
                return;

            dialog.Properties.SetPropertyValue(1, progress);
            dialog.UpdateModalEvents();
        }

        public void StayModelUntilClosed()
        {
            dialog.StayModelUntilClosed();
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
    }
}
