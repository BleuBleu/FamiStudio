using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class LogDialog : Dialog, ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private List<string>   messages = new List<string>();

        public LogDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog("Log", 800, false);
            dialog.Properties.AddLogTextBox(null); // 0
            dialog.Properties.Build();
        }

        public void ShowDialogIfMessages()
        {
            if (HasMessages)
            {
                foreach (var msg in messages)
                    dialog.Properties.AppendText(0, msg);

                ShowDialogAsync(null, (r) => { });
            }
        }

        public void LogMessage(string msg)
        {
            messages.Add(msg);
        }

        public bool HasMessages => messages.Count > 0;
        public bool AbortOperation =>  dialog.DialogResult != DialogResult.None;
        public void ReportProgress(float progress) { }
    }

    class LogProgressDialog : Dialog, ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private bool hasMessages = false;

        public unsafe LogProgressDialog(FamiStudioForm parentForm, string title = null, string text = null)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog("Log", 800, false);
            dialog.Properties.AddLogTextBox(null); // 0
            dialog.Properties.AddProgressBar(null); // 1
            dialog.Properties.Build();
        }

        public void LogMessage(string msg)
        {
            if (AbortOperation)
                return;

            hasMessages = true;
            if (!dialog.Visible)
                dialog.ShowDialogAsync(null, (r) => { });
            dialog.Properties.AppendText(0, msg);

            // MATTT : Total HACK!
            ParentForm.RunEventLoop();
        }

        public void ReportProgress(float progress)
        {
            if (AbortOperation)
                return;

            dialog.Properties.SetPropertyValue(1, progress);

            // MATTT : Total HACK!
            ParentForm.RunEventLoop();
        }

        // MATTT : Total HACK!
        public void StayModalUntilClosed()
        {
            while (dialog.Visible)
            {
                ParentForm.RunEventLoop();
            }
        }

        public void Close()
        {
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
    }
}
