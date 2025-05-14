using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class LinuxDialog
    {
        public enum DialogMode
        {
            Open = 0,
            Save = 1,
            Folder = 2,
        }

        private static LinuxDialog dlgInstance;

        private FamiStudioWindow window = FamiStudioWindow.Instance;
        private DialogMode dialogMode;
        private string dialogTitle;
        private string dialogText;
        private string dialogExts;
        private bool dialogMulti;
        private MessageBoxButtons dialogButtons;

        public string[] SelectedPaths { get; private set; }
        public DialogResult MessageBoxSelection { get; private set; }

        #region Localization

        // Only needed for zenity. It doesn't have native 3 button question dialogs, so we manually create cancel.
        LocalizedString CancelLabel; 

        // Used if kdialog and zenity are not found.
        LocalizedString DialogErrorTitle;
        LocalizedString DialogErrorMessage;

        #endregion

        public static bool IsDialogOpen => dlgInstance != null;

        public LinuxDialog(DialogMode mode, string title, ref string defaultPath, string extensions = "", bool multiselect = false)
        {
            Localization.Localize(this);
            dialogMode  = mode;
            dialogTitle = title;
            dialogExts  = extensions;
            dialogMulti = multiselect;
            dlgInstance = this;

            SelectedPaths = ShowFileDialog(ref defaultPath);
        }

        public LinuxDialog(string text, string title, MessageBoxButtons buttons)
        {
            Localization.Localize(this);
            dialogTitle   = title;
            dialogText    = text;
            dialogButtons = buttons;
            dlgInstance   = this;

            MessageBoxSelection = ShowMessageBoxDialog();
        }

        private bool IsCommandAvailable(string program)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"command -v {program}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string[] ShowFileDialog(ref string defaultPath)
        {
            if (IsCommandAvailable("kdialog"))
            {
                return ShowKDialogFileDialog(ref defaultPath);
            }
            else if (IsCommandAvailable("zenity"))
            {
                return ShowZenityFileDialog(ref defaultPath);
            }

            var dlg = new MessageDialog(window, DialogErrorMessage, DialogErrorTitle, MessageBoxButtons.OK);
            dlg.ShowDialog();

            return null;
        }

        private string[] ShowKDialogFileDialog(ref string defaultPath)
        {
            var args = $"--title \"{dialogTitle}\" ";
            var extPairs = dialogExts.Split("|");

            Debug.Assert(extPairs.Length % 2 == 0);

            var filters = "";
            for (int i = 0; i < extPairs.Length - 1; i += 2)
            {
                string name    = extPairs[i].Split(" (")[0];;
                string pattern = extPairs[i + 1].Replace(";", " ");
                filters += $"{name} {pattern}|";
            }

            filters = filters.TrimEnd('|');

            switch (dialogMode)
            {
                case DialogMode.Open:
                    args += dialogMulti 
                        ? $"--getopenfilename \"{defaultPath}\" \"{filters}\" --multiple --separate-output"
                        : $"--getopenfilename \"{defaultPath}\" \"{filters}\"";
                    break;

                case DialogMode.Save:
                    args += $"--getsavefilename \"{defaultPath}\" \"{filters}\"";
                    break;

                case DialogMode.Folder:
                    args += $"--getexistingdirectory \"{defaultPath}\"";
                    break;
            }

            var result = ShowDialog("kdialog", args);

            if (result.Item1.Length == 0)
                return null;

            defaultPath = result.Item1[0];
            return result.Item1;
        }

        private string[] ShowZenityFileDialog(ref string defaultPath)
        {
            var filters  = "";
            var extPairs = dialogExts.Split("|");

            if (extPairs.Length > 0)
            {
                Debug.Assert(extPairs.Length % 2 == 0);

                for (int i = 0; i < extPairs.Length - 1; i += 2)
                    filters += "--file-filter=\"" + (extPairs[i] + "|" + extPairs[i + 1]).Replace(";", " ").Trim() + "\" ";
            }

            var args = $"--file-selection --title=\"{dialogTitle}\" --filename=\"{defaultPath.TrimEnd('/')}/\" {filters} ";

            switch (dialogMode)
            {
                case DialogMode.Open:
                    if (dialogMulti) args += "--multiple --separator=\"\n\"";
                    break;

                case DialogMode.Save:
                    args += "--save";
                    break;

                case DialogMode.Folder:
                    args += "--directory";
                    break;
            }

            var result = ShowDialog("zenity", args);

            if (result.Item1.Length == 0)
                return null;
            
            defaultPath = result.Item1[0];
            return result.Item1;
        }

        private DialogResult ShowMessageBoxDialog()
        {
            if (IsCommandAvailable("kdialog"))
            {
                return ShowKDialogMessageBoxDialog();
            }
            else if (IsCommandAvailable("zenity"))
            {
                return ShowZenityMessageBoxDialog();
            }

            var dlg = new MessageDialog(window, DialogErrorMessage, DialogErrorTitle, MessageBoxButtons.OK);
            dlg.ShowDialog();

            return DialogResult.None;
        }

        private DialogResult ShowKDialogMessageBoxDialog()
        {
            string args = string.Empty;

            switch (dialogButtons)
            {
                case MessageBoxButtons.OK:
                    args = $"--msgbox \"{dialogText}\" --title \"{dialogTitle}\"";
                    break;

                case MessageBoxButtons.YesNo:
                    args = $"--yesno \"{dialogText}\" --title \"{dialogTitle}\"";
                    break;

                case MessageBoxButtons.YesNoCancel:
                    args = $"--yesnocancel \"{dialogText}\" --title \"{dialogTitle}\"";
                    break;
            }

            var result   = ShowDialog("kdialog", args);
            var exitCode = result.Item2;

            return exitCode switch
            {
                0 => DialogResult.Yes,
                1 => DialogResult.No,
                2 => DialogResult.Cancel,
                _ => DialogResult.None,
            };
        }

        private DialogResult ShowZenityMessageBoxDialog()
        {
            var args = string.Empty;
            switch (dialogButtons)
            {
                case MessageBoxButtons.OK:
                    args = $"--info --title=\"{dialogTitle}\" --text=\"{dialogText}\"";
                    break;

                case MessageBoxButtons.YesNo:
                    args = $"--question --title=\"{dialogTitle}\" --text=\"{dialogText}\"";
                    break;

                case MessageBoxButtons.YesNoCancel:
                    args = $"--question --title=\"{dialogTitle}\" --text=\"{dialogText}\" --extra-button=\"{CancelLabel}\"";
                    break;
            }

            var result   = ShowDialog("zenity", args);
            var exitCode = result.Item2;

            if (exitCode == 0)
            {
                return DialogResult.Yes;
            }
            else if (exitCode == 1)
            {
                // No and Cancel are both exit code 1 on zenity, since it doesn't natively support 3 buttons. "No" returns an empty array.
                if (result.Item1.Length == 0)
                {
                    return DialogResult.No;
                }
                else if (result.Item1[0] == CancelLabel)
                {
                    return DialogResult.Cancel;
                }
            }

            return DialogResult.None;
        }

        private (string[], int) ShowDialog(string command, string arguments)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();

            while (!process.HasExited)
            {
                window.RunEventLoop(true);
            }

            dlgInstance = null;

            return (process.StandardOutput.ReadToEnd().Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries), process.ExitCode);
        }
    }
}