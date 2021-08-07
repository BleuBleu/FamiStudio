using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class LogDialog : ILogOutput
    {
        public bool AbortOperation => false;
        public void LogMessage(string msg)
        {
        }
        public void ReportProgress(float progress)
        {
        }

        // DROIDTODO
        /*
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private List<string>   messages = new List<string>();

        public LogDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog(800, false);
            dialog.Properties.AddMultilineString(null, ""); // 0
            dialog.Properties.Build();
        }

        public DialogResult ShowDialog()
        {
            return dialog.ShowDialog(parentForm);
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
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
        public void ReportProgress(float progress) { }
        */
    }
}
