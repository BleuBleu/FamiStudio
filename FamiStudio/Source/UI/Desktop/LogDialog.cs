using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class LogDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private List<string>   messages = new List<string>();

        public LogDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            // MATTT
            //dialog = new PropertyDialog("Log", 800, false);
            //dialog.Properties.AddMultilineTextBox(null, ""); // 0
            //dialog.Properties.Build();
        }

        public DialogResult ShowDialog()
        {
            return DialogResult.OK; // MATTT dialog.ShowDialog(parentForm);
        }

        public DialogResult ShowDialogIfMessages()
        {
            if (HasMessages)
            {
                dialog.Properties.AppendText(0, string.Join("\r\n", messages));
                return ShowDialog();
            }

            return DialogResult.Cancel;
        }

        public void LogMessage(string msg)
        {
            messages.Add(msg);
        }

        public bool HasMessages => messages.Count > 0;
        public bool AbortOperation => false; // MATTT dialog.DialogResult != DialogResult.None;
        public void ReportProgress(float progress) { }
    }

    class LogProgressDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private bool hasMessages = false;

        public unsafe LogProgressDialog(FamiStudioForm parentForm, string title = null, string text = null)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog("Log", 800, false);
            dialog.Properties.AddMultilineTextBox(null, ""); // 0
            dialog.Properties.AddProgressBar(null); // 1
            dialog.Properties.Build();
        }

        public void LogMessage(string msg)
        {
            // MATTT
            //dialog.UpdateModalEvents();

            //if (AbortOperation)
            //    return;

            //hasMessages = true;
            //if (!dialog.Visible)
            //    dialog.ShowModal(parentForm);
            //dialog.Properties.AppendText(0, msg);
            //dialog.UpdateModalEvents();
        }

        public void ReportProgress(float progress)
        {
            // MATTT
            //dialog.UpdateModalEvents();

            //if (AbortOperation)
            //    return;

            //dialog.Properties.SetPropertyValue(1, progress);
            //dialog.UpdateModalEvents();
        }

        public void StayModalUntilClosed()
        {
            // MATTT dialog.StayModalUntilClosed();
        }

        public void Close()
        {
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => false; // MATTT dialog.DialogResult != DialogResult.None;
    }
}
