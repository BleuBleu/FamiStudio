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
        private static readonly bool isDisplayAvailable;
        private static readonly bool isRunningInFlatpak;
        private static readonly bool isKdialogAvailable;
        private static readonly bool isZenityAvailable;

        private const string FlatpakInfoPath = "/.flatpak-info";
        private const string FlatpakPrefix = "/app";
        private const string FlatpakIdEnvVar = "FLATPAK_ID";
        private const string DisplayEnvVar = "DISPLAY";
        
        public static bool IsDialogOpen => dlgInstance != null;

        static LinuxDialog()
        {
            isDisplayAvailable = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DisplayEnvVar));
            isRunningInFlatpak = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(FlatpakIdEnvVar));
            isKdialogAvailable = IsCommandAvailable("kdialog");
            isZenityAvailable = IsCommandAvailable("zenity");
        }

        private DialogMode dialogMode;
        private string dialogTitle;
        private string dialogText;
        private string dialogExts;
        private string dialogPath;
        private string flatpakPath;
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

        public LinuxDialog(DialogMode mode, string title, ref string defaultPath, string extensions = "", bool multiselect = false)
        {
            dlgInstance = this;
            Localization.Localize(this);

            dialogMode  = mode;
            dialogTitle = title;
            dialogExts  = extensions;
            dialogMulti = multiselect;
            dialogPath  = defaultPath;

            SelectedPaths = ShowFileDialog();
            defaultPath   = dialogPath;

            // If we're using flatpak, we may have converted the sandbox "/app" path
            // to a system one. If so, convert the results so the sandbox can find them.
            ConditionalRestoreFlatpakPaths(SelectedPaths);
            dlgInstance = null;
        }

        public LinuxDialog(string text, string title, MessageBoxButtons buttons)
        {
            dlgInstance = this;
            Localization.Localize(this);

            dialogTitle   = title;
            dialogText    = text;
            dialogButtons = buttons;

            MessageBoxSelection = ShowMessageBoxDialog();
            dlgInstance = null;
        }

        private static bool IsCommandAvailable(string program)
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

        private void ConditionalSetFlatpakPaths()
        {
            if (isRunningInFlatpak && dialogPath.StartsWith(FlatpakPrefix))
            {
                try
                {
                    if (File.Exists(FlatpakInfoPath))
                    {
                        var lines = File.ReadAllLines(FlatpakInfoPath);

                        foreach (var line in lines)
                        {
                            if (line.StartsWith("app-path="))
                            {
                                flatpakPath = line.Split('=', 2)[1].Trim(); // System flatpak path.
                                dialogPath  = string.Concat(flatpakPath, dialogPath.AsSpan(FlatpakPrefix.Length));
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void ConditionalRestoreFlatpakPaths(string[] paths)
        {
            if (isRunningInFlatpak && !string.IsNullOrEmpty(flatpakPath))
            {
                for (var i = 0; i < paths.Length; i++)
                    if (paths[i].StartsWith(flatpakPath))
                        paths[i] = string.Concat(FlatpakPrefix, paths[i].AsSpan(flatpakPath.Length));
            }
        }

        private string[] ShowFileDialog()
        {
            if (!isDisplayAvailable)
                return null;

            // If we're using flatpak and are within the sandboxed "/app" path, the 
            // external process can't see it. We use the real system path to it instead.
            ConditionalSetFlatpakPaths();

            if (isKdialogAvailable)
            {
                return ShowKDialogFileDialog();
            }
            else if (isZenityAvailable)
            {
                return ShowZenityFileDialog();
            }

            var dlg = new MessageDialog(FamiStudioWindow.Instance, DialogErrorMessage, DialogErrorTitle, MessageBoxButtons.OK);
            dlg.ShowDialog();

            return null;
        }

        private string[] ShowKDialogFileDialog()
        {
            var args = $"--title \"{dialogTitle}\" ";
            var extPairs = dialogExts.Split("|");

            Debug.Assert(extPairs.Length % 2 == 0);

            var filters = "";
            for (int i = 0; i < extPairs.Length - 1; i += 2)
            {
                var name    = extPairs[i].Split(" (")[0];;
                var pattern = extPairs[i + 1].Replace(";", " ");
                filters += $"{name} {pattern}|";
            }

            filters = filters.TrimEnd('|');

            switch (dialogMode)
            {
                case DialogMode.Open:
                    args += dialogMulti 
                        ? $"--getopenfilename \"{dialogPath}\" \"{filters}\" --multiple --separate-output"
                        : $"--getopenfilename \"{dialogPath}\" \"{filters}\"";
                    break;

                case DialogMode.Save:
                    args += $"--getsavefilename \"{dialogPath}\" \"{filters}\"";
                    break;

                case DialogMode.Folder:
                    args += $"--getexistingdirectory \"{dialogPath}\"";
                    break;
            }

            var result = ShowDialog("kdialog", args);

            if (result.value.Length == 0)
                return null;

            dialogPath = dialogMode == DialogMode.Folder ? result.value[0] : Path.GetDirectoryName(result.value[0]);
            return result.value;
        }

        private string[] ShowZenityFileDialog()
        {
            var filters  = "";
            var extPairs = dialogExts.Split("|");

            if (extPairs.Length > 0)
            {
                Debug.Assert(extPairs.Length % 2 == 0);

                for (int i = 0; i < extPairs.Length - 1; i += 2)
                    filters += "--file-filter=\"" + (extPairs[i] + "|" + extPairs[i + 1]).Replace(";", " ").Trim() + "\" ";
            }

            var args = $"--file-selection --title=\"{dialogTitle}\" --filename=\"{dialogPath.TrimEnd('/')}/\" {filters} ";

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
            
            dialogPath = dialogMode == DialogMode.Folder ? result.value[0] : Path.GetDirectoryName(result.value[0]);
            return result.value;
        }

        private DialogResult ShowMessageBoxDialog()
        {
            if (!isDisplayAvailable)
                return DialogResult.None;
                
            if (isKdialogAvailable)
            {
                return ShowKDialogMessageBoxDialog();
            }
            else if (isZenityAvailable)
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

            process.Start();

            while (!process.HasExited)
            {
                FamiStudioWindow.Instance.RunEventLoop(true);
            }

            var results = process.StandardOutput.ReadToEnd().Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return (results, process.ExitCode);
        }
    }
}