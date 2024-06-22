using System.Collections.Generic;

namespace FamiStudio
{
    class LogDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private List<string>   messages = new List<string>();

        public LogDialog(FamiStudioWindow win)
        {
            dialog = new PropertyDialog(win, "Log", 800, false);
            dialog.Properties.AddLogTextBox(null); // 0
            dialog.Properties.Build();
        }

        public void ShowDialogIfMessages()
        {
            if (HasMessages)
            {
                foreach (var msg in messages)
                    dialog.Properties.AppendText(0, msg);

                dialog.ShowDialog();
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

    class LogProgressDialog : ILogOutput
    {
        private const double ProcessEventDelay = 1.0 / 30.0;

        private PropertyDialog dialog;
        private bool hasMessages = false;
        private double lastEventLoop;

        public unsafe LogProgressDialog(FamiStudioWindow win, string title = null, string text = null)
        {
            dialog = new PropertyDialog(win, "Log", 820, false);
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
                dialog.ShowDialogNonModal();
            dialog.Properties.AppendText(0, msg);
            ConditionalRunEventLoop();
        }

        public void ReportProgress(float progress)
        {
            if (AbortOperation)
                return;

            dialog.Properties.SetPropertyValue(1, progress);
            ConditionalRunEventLoop();
        }

        public void StayModalUntilClosed()
        {
            while (dialog.Visible)
                ConditionalRunEventLoop();
        }

        public void Close()
        {
        }

        private void ConditionalRunEventLoop()
        {
            var now = Platform.TimeSeconds();

            if (now - lastEventLoop > ProcessEventDelay)
            {
                dialog.ParentWindow.RunEventLoop();
                lastEventLoop = now;
            }
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
    }
}
