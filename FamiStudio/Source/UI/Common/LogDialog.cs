using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
#if !FAMISTUDIO_ANDROID // DROIDTODO!
    class LogDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private List<string>   messages = new List<string>();

        public LogDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog("Log", 800, false);
            dialog.Properties.AddMultilineTextBox(null, ""); // 0
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
    }
#else
    class LogDialog : ILogOutput
    {
        public LogDialog(FamiStudioForm parentForm)
        {
        }

        public DialogResult ShowDialog()
        {
            return DialogResult.OK;
        }

        public DialogResult ShowDialogIfMessages()
        {
            return DialogResult.OK;
        }

        public void LogMessage(string msg)
        {
        }

        public bool HasMessages => false;
        public bool AbortOperation => false;
        public void ReportProgress(float progress) { }
    }
#endif
}
