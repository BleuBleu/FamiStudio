using System;
using System.Diagnostics;
using System.IO;

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
        
        private const string flatpakInfo = "/.flatpak-info";
        private const string displayInfo = "DISPLAY";
        private const string flatpakId   = "FLATPAK_ID";

        private DialogMode dialogMode;
        private string dialogTitle;
        private string dialogText;
        private string dialogExts;
        private bool dialogMulti;
        private MessageBoxButtons dialogButtons;
        private string flatpakPath;
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

            SelectedPaths = ShowFileDialog(ref defaultPath);
        }

        public LinuxDialog(string text, string title, MessageBoxButtons buttons)
        {
            Localization.Localize(this);
            dialogTitle   = title;
            dialogText    = text;
            dialogButtons = buttons;
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

        private static bool IsDisplayAvailable()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(displayInfo));
        }

        private static bool IsFlatpak()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(flatpakId));
        }

        private void UpdateDefaultPathForFlatpak(ref string defaultPath)
        {
            try
            {
                if (!File.Exists(flatpakInfo))
                    return;

                var lines = File.ReadAllLines(flatpakInfo);

                foreach (var line in lines)
                {
                    if (line.StartsWith("app-path="))
                    {
                        var appPath = line.Split('=', 2)[1].Trim();
                        defaultPath = defaultPath.Replace("/app/bin", $"{appPath}/bin");
                        flatpakPath = defaultPath[..(defaultPath.IndexOf("/bin") + 4)];
                        break; 
                    }
                }
            }
            catch { }
        }

        private string[] ShowFileDialog(ref string defaultPath)
        {
            if (!IsDisplayAvailable())
                return null;

            // The external process can't see the flatpak path, so we temporarily use the real system path.
            if (IsFlatpak() && defaultPath.StartsWith("/app/bin"))
                UpdateDefaultPathForFlatpak(ref defaultPath);

            if (IsCommandAvailable("kdialog"))
            {
                return ShowKDialogFileDialog(ref defaultPath);
            }
            else if (IsCommandAvailable("zenity"))
            {
                return ShowZenityFileDialog(ref defaultPath);
            }

            var dlg = new MessageDialog(FamiStudioWindow.Instance, DialogErrorMessage, DialogErrorTitle, MessageBoxButtons.OK);
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

            if (result.value.Length == 0)
                return null;

            defaultPath = result.value[0];
            return result.value;
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

            if (result.value.Length == 0)
                return null;
            
            defaultPath = result.value[0];
            return result.value;
        }

        private DialogResult ShowMessageBoxDialog()
        {
            if (!IsDisplayAvailable())
                return DialogResult.None;
                
            if (IsCommandAvailable("kdialog"))
            {
                return ShowKDialogMessageBoxDialog();
            }
            else if (IsCommandAvailable("zenity"))
            {
                return ShowZenityMessageBoxDialog();
            }

            var dlg = new MessageDialog(FamiStudioWindow.Instance, DialogErrorMessage, DialogErrorTitle, MessageBoxButtons.OK);
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
            var exitCode = result.exitCode;

            return exitCode switch
            {
                0 => dialogButtons == MessageBoxButtons.OK ? DialogResult.OK : DialogResult.Yes,
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
            var exitCode = result.exitCode;

            if (exitCode == 0)
            {
                return dialogButtons == MessageBoxButtons.OK ? DialogResult.OK : DialogResult.Yes;
            }
            else if (exitCode == 1)
            {
                // No and Cancel are both exit code 1 on zenity, since it doesn't natively support 3 buttons. "No" returns an empty array.
                if (result.value.Length == 0)
                {
                    return DialogResult.No;
                }
                else if (result.value[0] == CancelLabel)
                {
                    return DialogResult.Cancel;
                }
            }

            return DialogResult.None;
        }

        private (string[] value, int exitCode) ShowDialog(string command, string arguments)
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

            dlgInstance = this;
            process.Start();

            while (!process.HasExited)
            {
                FamiStudioWindow.Instance.RunEventLoop(true);
            }

            dlgInstance = null;
            var results = process.StandardOutput.ReadToEnd().Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // If we're using flatpak and changed the path, revert any paths that use it so the sandbox can find them.
            if (!string.IsNullOrEmpty(flatpakPath))
            {
                for (var i = 0; i < results.Length; i++)
                {
                    if (results[i].StartsWith(flatpakPath))
                        results[i] = results[i].Replace(flatpakPath, "/app/bin");
                }
            }

            return (results, process.ExitCode);
        }
    }
}