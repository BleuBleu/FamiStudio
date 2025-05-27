using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class LinuxDialog
    {
        public enum DialogMode
        {
            Open = 0,
            Save = 1,
            Folder = 2,
            Message = 3
        }

        private const string FlatpakInfoPath = "/.flatpak-info";
        private const string FlatpakPrefix = "/app";
        private const string FlatpakIdEnvVar = "FLATPAK_ID";
        private const string DisplayEnvVar = "DISPLAY";
        private const string XdgCurrentDesktopEnvVar = "XDG_CURRENT_DESKTOP";
        private const string XdgSessionTypeEnvVar = "XDG_SESSION_TYPE";
        private const string GobjectDllName = "libgobject-2.0.so.0";
        private const string GtkDllName = "libgtk-3.so.0";

        private static LinuxDialog dlgInstance;
        private static string desktopEnvironment;
        private static bool isDisplayAvailable;
        private static bool isGtkInitialized;
        private static bool isX11;
        private static bool isWayland;
        private static bool isFlatpak;

        public static bool IsDialogOpen => dlgInstance != null;

        static LinuxDialog()
        {
            desktopEnvironment = Environment.GetEnvironmentVariable(XdgCurrentDesktopEnvVar);
            isDisplayAvailable = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DisplayEnvVar));
            isFlatpak = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(FlatpakIdEnvVar));
            isX11 = Environment.GetEnvironmentVariable(XdgSessionTypeEnvVar)?.ToLowerInvariant() == "x11";
            isWayland = !isX11;

            try
            {
                // TODO: Properly debug intermittent crashing in Wayland. Debian 12 seems to work, even with
                // GLFW 3.4 compiled. Arch Linux seems to have issues on KDE and GNOME with Wayland. 
                //
                // NOTE: When it crashes, it will be on the first dialog, as if GTK never initialised properly 
                // despite this try block passing.
                GtkInit(0, IntPtr.Zero);
                isGtkInitialized = true;
            }
            catch (Exception ex)
            {
                isGtkInitialized = false;
                Console.Error.WriteLine($"[LinuxDialog] Failed to initialize GTK: {ex}");
            }
        }

        // GTK Message Types
        private const int GTK_MESSAGE_INFO = 0;
        private const int GTK_MESSAGE_WARNING = 1;
        private const int GTK_MESSAGE_QUESTION = 2;
        private const int GTK_MESSAGE_ERROR = 3;
        private const int GTK_MESSAGE_OTHER = 4;

        // GTK Buttons
        private const int GTK_BUTTONS_OK = 0;
        private const int GTK_BUTTONS_CLOSE = 1;
        private const int GTK_BUTTONS_CANCEL = 2;
        private const int GTK_BUTTONS_YES_NO = 3;
        private const int GTK_BUTTONS_OK_CANCEL = 4;

        // GTK Responses
        private const int GTK_RESPONSE_ACCEPT = -3;
        private const int GTK_RESPONSE_DELETE_EVENT = -4;
        private const int GTK_RESPONSE_OK = -5;
        private const int GTK_RESPONSE_CANCEL = -6;
        private const int GTK_RESPONSE_CLOSE = -7;
        private const int GTK_RESPONSE_YES = -8;
        private const int GTK_RESPONSE_NO = -9;

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

        // Used for zenity and GTK, which don't have default 3 button dialogs.
        LocalizedString CancelLabel;

        // Used if GTK, kdialog, and zenity are not found.
        LocalizedString DialogErrorTitle;
        LocalizedString DialogErrorMessage;

        #endregion

        public LinuxDialog(DialogMode mode, string title, ref string defaultPath, string extensions = "", bool multiselect = false)
        {
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
        }

        public LinuxDialog(string text, string title, MessageBoxButtons buttons)
        {
            Localization.Localize(this);
            dialogMode    = DialogMode.Message;
            dialogTitle   = title;
            dialogText    = text;
            dialogButtons = buttons;
            MessageBoxSelection = ShowMessageBoxDialog();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GtkResponseCallback(IntPtr dialog, int response_id, IntPtr user_data);

        [DllImport(GobjectDllName, EntryPoint = "g_signal_connect_data", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong GSignalConnectData(IntPtr instance, string detailed_signal, GtkResponseCallback handler, IntPtr data, IntPtr destroy_data, int connect_flags);

        [DllImport(GobjectDllName, EntryPoint = "g_type_check_instance_is_a", CallingConvention = CallingConvention.Cdecl)]
        static extern bool GTypeCheckInstanceIsA(IntPtr obj, IntPtr gType);

        [DllImport(GtkDllName, EntryPoint = "gtk_init", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkInit(int argc, IntPtr argv);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_set_title", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWindowSetTitle(IntPtr dialog, string title);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_chooser_dialog_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GtkFileChooserDialogNew(
            string title,
            IntPtr parent,
            int action,
            string firstButtonText, int firstResponse,
            string secondButtonText, int secondResponse,
            IntPtr nullTerminator);

        [DllImport(GtkDllName, EntryPoint = "gtk_message_dialog_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GtkMessageDialogNew(
            IntPtr parent,
            int flags,
            int type,
            int buttons,
            string message_format);

        [DllImport(GtkDllName, EntryPoint = "gtk_dialog_add_button", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GtkDialogAddButton(IntPtr dialog, string button_text, int response_id);

        [DllImport(GtkDllName, EntryPoint = "gtk_dialog_run", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GtkDialogRun(IntPtr dialog);

        [DllImport(GtkDllName, EntryPoint = "gtk_widget_show", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWidgetShow(IntPtr widget);

        [DllImport(GtkDllName, EntryPoint = "gtk_widget_get_visible", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GtkGetWidgetVisible(IntPtr widget);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_get_type", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr GtkWindowGetType();

        [DllImport(GtkDllName, EntryPoint = "gtk_widget_get_type", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr GtkWidgetGetType();

        [DllImport(GtkDllName, EntryPoint = "gtk_widget_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWidgetDestroy(IntPtr widget);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_is_active", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GtkWindowIsActive(IntPtr window);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_set_keep_above", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWindowSetKeepAbove(IntPtr window, bool setting);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_set_transient_for", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWindowSetTransientFor(IntPtr window, IntPtr parent);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_present", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWindowPresent(IntPtr window);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_set_modal", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWindowSetModal(IntPtr window, bool modal);

        [DllImport(GtkDllName, EntryPoint = "gtk_window_set_default_icon_from_file", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkWindowSetDefaultIconFromFile(string filename, out IntPtr error);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_chooser_set_current_folder", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GtkFileChooserSetCurrentFolder(IntPtr chooser, string folder);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_chooser_get_filename", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GtkFileChooserGetFilename(IntPtr dialog);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_chooser_set_select_multiple", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkFileChooserSetSelectMultiple(IntPtr dialog, bool select_multiple);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_filter_new", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GtkFileFilterNew();

        [DllImport(GtkDllName, EntryPoint = "gtk_file_filter_set_name", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkFileFilterSetName(IntPtr filter, string name);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_filter_add_pattern", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkFileFilterAddPattern(IntPtr filter, string pattern);

        [DllImport(GtkDllName, EntryPoint = "gtk_file_chooser_add_filter", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkFileChooserAddFilter(IntPtr chooser, IntPtr filter);

        [DllImport(GtkDllName, EntryPoint = "g_filename_to_utf8", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GFilenameToUtf8(IntPtr filename, IntPtr len, IntPtr bytesRead, IntPtr bytesWritten, IntPtr error);

        [DllImport(GtkDllName, EntryPoint = "g_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GFree(IntPtr ptr);

        [DllImport(GtkDllName, EntryPoint = "gtk_events_pending", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GtkEventsPending();

        [DllImport(GtkDllName, EntryPoint = "gtk_main_iteration", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GtkMainIteration();
        
        private static bool IsGtkWindow(IntPtr ptr)
        {
            return GTypeCheckInstanceIsA(ptr, GtkWindowGetType());
        }

        private static bool IsGtkWidget(IntPtr ptr)
        {
            return GTypeCheckInstanceIsA(ptr, GtkWidgetGetType());
        }

        private static int GtkGetDialogResponse(IntPtr dialog)
        {
            var response = -1;
            var done = false;

            // Use a callback and custom loop to keep the window responsive and simulate a "modal" effect.
            // GNOME is a little inconsistent here and will have to be tested properly.
            GtkResponseCallback callback = (dlg, responseId, userData) =>
            {
                response = responseId;
                done = true;
            };

            var isGnome = desktopEnvironment.Contains("gnome", StringComparison.InvariantCultureIgnoreCase);
            var callbackHandle = GCHandle.Alloc(callback);
            var hasPresented = true;
            var isFirstRun   = true; // Workaround for GNOME, otherwise dialogs may open in the background.
            var canSetBehind = false;
            GSignalConnectData(dialog, "response", callback, IntPtr.Zero, IntPtr.Zero, 0);

            while (!done)
            {
                while (GtkEventsPending())
                    GtkMainIteration();

                // In case a user exits via the window manager or X button.
                if (!IsGtkWindow(dialog))
                {
                    done = true;
                    break;
                }

                FamiStudioWindow.Instance.RunEventLoop(true);

                var fsActive  = FamiStudioWindow.Instance.IsWindowInFocus;
                var gtkActive = GtkWindowIsActive(dialog);

                var shouldPresent = (fsActive && !hasPresented) || isFirstRun;
                var keepAbove     = (fsActive || gtkActive) && !isGnome;

                // Behave like a modal dialog.
                if (shouldPresent)
                {
                    // Present the window if the FS window is clicked. Environments behave differently here.
                    // Some will bring the window back in front, others will highlight the icon on the taskbar.
                    Console.WriteLine("WINDOW PRESENT!");
                    GtkWindowPresent(dialog);
                    GtkWindowSetKeepAbove(dialog, keepAbove);
                    canSetBehind = true;
                    hasPresented = true;

                    if (!isFirstRun)
                        Platform.Beep();
                }
                else if (!keepAbove && canSetBehind) 
                {
                    Console.WriteLine("KEEP ABOVE UNSET!");
                    GtkWindowSetKeepAbove(dialog, false);
                    canSetBehind = false;
                }

                hasPresented = fsActive;
                isFirstRun   = false;
            }

            callbackHandle.Free();
            return response;
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

        private void SetFlatpakPaths()
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
                            dialogPath = dialogPath.Replace(FlatpakPrefix, flatpakPath);
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        private void ConditionalRestoreFlatpakPaths(string[] paths)
        {
            if (isFlatpak && !string.IsNullOrEmpty(flatpakPath))
            {
                for (var i = 0; i < paths.Length; i++)
                    paths[i] = paths[i].Replace(flatpakPath, FlatpakPrefix);
            }
        }

        private string[] ShowFileDialog()
        {
            if (!isDisplayAvailable)
                return null;

            // If we're using flatpak and are within the sandboxed "/app" path, the
            // external process can't see it. We use the real system path to it instead.
            if (isFlatpak && dialogPath.StartsWith(FlatpakPrefix))
                SetFlatpakPaths();

            // GTK seems the most flexible overall, but the others still work.
            if (isGtkInitialized)
            {
                return ShowGtkFileDialog();
            }
            else if (IsCommandAvailable("kdialog"))
            {
                return ShowKDialogFileDialog();
            }
            else if (IsCommandAvailable("zenity"))
            {
                return ShowZenityFileDialog();
            }

            var dlg = new MessageDialog(FamiStudioWindow.Instance, DialogErrorMessage, DialogErrorTitle, MessageBoxButtons.OK);
            dlg.ShowDialog();

            return null;
        }

        private string[] ShowGtkFileDialog()
        {
            IntPtr dialog = GtkFileChooserDialogNew(
                dialogTitle,
                IntPtr.Zero,
                (int)dialogMode,
                "_Cancel", GTK_RESPONSE_CANCEL,
                dialogMode == DialogMode.Save ? "_Save" : "_Open", GTK_RESPONSE_ACCEPT,
                IntPtr.Zero);

            if (dialogMulti)
                GtkFileChooserSetSelectMultiple(dialog, true);

            GtkFileChooserSetCurrentFolder(dialog, dialogPath);

            // Extension filters
            var extPairs = dialogExts.Split("|");
            for (var i = 0; i < extPairs.Length; i += 2)
            {
                IntPtr filter = GtkFileFilterNew();

                var pair = extPairs[i].Split('(');
                GtkFileFilterSetName(filter, pair[0].Trim());

                foreach (var ext in pair[1].Split(';'))
                {
                    GtkFileFilterAddPattern(filter, ext.TrimEnd(')'));
                }

                GtkFileChooserAddFilter(dialog, filter);
            }

            Debug.Assert(dlgInstance == null);
            dlgInstance = this;
            
            GtkWidgetShow(dialog);
            var response = GtkGetDialogResponse(dialog);

            string[] result = null;
            if (response == GTK_RESPONSE_ACCEPT)
            {
                if (GtkGetWidgetVisible(dialog))
                {
                    IntPtr filenamePtr = GtkFileChooserGetFilename(dialog);

                    if (filenamePtr != IntPtr.Zero)
                    {
                        var file = Marshal.PtrToStringUTF8(filenamePtr)!;
                        result = new[] { file };

                        if (result.Length > 0)
                            dialogPath = Path.GetDirectoryName(result[0]);

                        GFree(filenamePtr);
                    }
                }
            }

            if (IsGtkWidget(dialog))
                GtkWidgetDestroy(dialog);

            while (GtkEventsPending())
                GtkMainIteration();

            dlgInstance = null;
            return result;
        }

        private string[] ShowKDialogFileDialog()
        {
            var args = $"--title \"{dialogTitle}\" ";
            var extPairs = dialogExts.Split("|");

            Debug.Assert(extPairs.Length % 2 == 0);

            var filters = "";
            for (int i = 0; i < extPairs.Length - 1; i += 2)
            {
                var name = extPairs[i].Split(" (")[0]; ;
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

            // GTK seems the most flexible overall, but the others still work.
            if (isGtkInitialized)
            {
                return ShowGtkMessageBoxDialog();
            }
            else if (IsCommandAvailable("kdialog"))
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

        private DialogResult ShowGtkMessageBoxDialog()
        {
            var messageType = dialogButtons == MessageBoxButtons.OK ? GTK_MESSAGE_INFO : GTK_MESSAGE_QUESTION;
            var buttonsType = dialogButtons switch
            {
                MessageBoxButtons.OK          => GTK_BUTTONS_OK,
                MessageBoxButtons.YesNo       => GTK_BUTTONS_YES_NO,
                MessageBoxButtons.YesNoCancel => GTK_BUTTONS_OK_CANCEL,
                _                             => GTK_BUTTONS_OK,
            };
            IntPtr dialog = GtkMessageDialogNew(
                IntPtr.Zero,
                0,
                messageType,
                buttonsType,
                dialogText);

            GtkWindowSetTitle(dialog, dialogTitle);

            if (dialogButtons == MessageBoxButtons.YesNoCancel)
            {
                GtkDialogAddButton(dialog, CancelLabel, GTK_RESPONSE_CANCEL);
            }

            Debug.Assert(dlgInstance == null);
            dlgInstance = this;

            GtkWidgetShow(dialog);
            var response = GtkGetDialogResponse(dialog);

            DialogResult result = response switch
            {
                GTK_RESPONSE_DELETE_EVENT => DialogResult.None,
                GTK_RESPONSE_OK => DialogResult.OK,
                GTK_RESPONSE_CANCEL => DialogResult.Cancel,
                GTK_RESPONSE_YES => DialogResult.Yes,
                GTK_RESPONSE_NO => DialogResult.No,
                _ => DialogResult.None
            };

            if (IsGtkWidget(dialog))
                GtkWidgetDestroy(dialog);
            
            while (GtkEventsPending())
                GtkMainIteration();

            dlgInstance = null;
            return result;
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

            var result = ShowDialog("kdialog", args);
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

            Debug.Assert(dlgInstance == null);
            dlgInstance = this;

            process.Start();

            while (!process.HasExited)
            {
                FamiStudioWindow.Instance.RunEventLoop(true);
            }

            dlgInstance = null;

            var results = process.StandardOutput.ReadToEnd().Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return (results, process.ExitCode);
        }
    }
}
