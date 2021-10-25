using System.Windows.Forms;
using Xamarin.Essentials;

namespace FamiStudio
{
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

    class LogProgressDialog : ILogOutput
    {
        bool shown = false;
        bool abort = false;
        bool hasMessages = false;
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;

        public unsafe LogProgressDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            MainThread.InvokeOnMainThreadAsync(() =>
            {
                // HACK : We only use this for video export on mobile.
                dialog = new PropertyDialog("Exporting Video", 100, false);
                dialog.Properties.AddProgressBar("Export progress", 0.0f, "Exporting videos may take a very long time, especially at high resolutions. Make sure FamiStudio remains open, clicking BACK or closing this window will abort the operation. FamiStudio is currently preventing the screen from going to sleep."); // 0
                dialog.Properties.AddLabel("Current Step", ""); // 1
                dialog.Properties.Build();
            });
        }

        public void LogMessage(string msg)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                dialog.Properties.SetPropertyValue(1, msg);
            });
        }

        public void ReportProgress(float progress)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!shown)
                {
                    shown = true;
                    dialog.ShowDialogAsync(parentForm, (r) => { abort = r != DialogResult.None; });
                }

                dialog.Properties.SetPropertyValue(0, progress);
            });
        }

        public void StayModalUntilClosed()
        {
        }

        public void Close()
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                dialog.CloseWithResult(DialogResult.OK);
            });
        }

        public bool HasMessages => false;
        public bool AbortOperation => abort;
    }
}
