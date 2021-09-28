using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Xamarin.Essentials;

using Debug = System.Diagnostics.Debug;

namespace FamiStudio
{
#if FAMISTUDIO_ANDROID
    // DROIDTODO : Move to another file.
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
                dialog = new PropertyDialog("Exporting Video", 100, false); // DROIDTODO : CanAccept is false here. Take into account.
                dialog.Properties.AddProgressBar("Export progress", 0.0f, "Exporting videos may take a very long time, especially at high resolutions. Make sure FamiStudio remains open, clicking BACK or closing this window will abort the operation."); // 0
                dialog.Properties.AddLabel("Current Step", ""); // 1
                dialog.Properties.Build();

                dialog.Properties.SetPropertyVisible(1, false);
            });
        }

        public void LogMessage(string msg)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!hasMessages)
                {
                    hasMessages = true;
                    dialog.Properties.SetPropertyVisible(1, true);
                }

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
                    dialog.ShowDialog(parentForm, (r) => { abort = r != DialogResult.None; });
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

        public bool HasMessages    => false;
        public bool AbortOperation => abort;
    }
#else
    class LogProgressDialog : ILogOutput
    {
        private PropertyDialog dialog;
        private FamiStudioForm parentForm;
        private bool hasMessages = false;

        public unsafe LogProgressDialog(FamiStudioForm parentForm)
        {
            this.parentForm = parentForm;

            dialog = new PropertyDialog("Log", 800, false);
            dialog.Properties.AddMultilineTextBox(null, ""); // 0
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

        public void StayModalUntilClosed()
        {
            dialog.StayModalUntilClosed();
        }

        public bool HasMessages => hasMessages;
        public bool AbortOperation => dialog.DialogResult != DialogResult.None;
    }
#endif
}
