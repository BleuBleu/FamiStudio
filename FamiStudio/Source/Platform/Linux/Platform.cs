using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public static partial class Platform
    {
        private static byte[] internalClipboardData;

        public static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/FamiStudio");
        public static string UserProjectsDirectory => null;
        public static float DoubleClickTime => 0.25f; // MATTT

        public const string DllPrefix = "";
        public const string DllExtension = ".so";

        public static bool Initialize()
        {
            if (!InitializeDesktop())
                return false;

            SetProcessName("FamiStudio");

            return true;
        }

        public static IAudioStream CreateAudioStream(int rate, bool stereo, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            return new OpenALStream(rate, stereo, bufferSize, numBuffers, bufferFillCallback);
        }

        private static string[] GetExtensionList(string str)
        {
            var splits = str.Split('|');
            var extensions = new List<string>();

            for (int i = 1; i < splits.Length; i += 2)
            {
                extensions.AddRange(splits[i].Split(new[] { ';', '*', '.' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return extensions.Distinct().ToArray();
        }

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect, IntPtr parentWindow = default(IntPtr))
        {
            /*
            var extensionList = GetExtensionList(extensions);

            Gtk.Rc.ResetStyles(Gtk.Settings.GetForScreen(Gdk.Screen.Default));
            Gtk.Rc.ReparseAll();

            Gtk.FileChooserDialog filechooser =
                new Gtk.FileChooserDialog(title,
                    null,
                    FileChooserAction.Open,
                    "Cancel", ResponseType.Cancel,
                    "Open", ResponseType.Accept);

            filechooser.Modal = true;
            filechooser.SkipTaskbarHint = true;
            filechooser.TransientFor = parentWindow != null ? parentWindow : FamiStudioForm.Instance;
            filechooser.SetCurrentFolder(defaultPath);
            filechooser.SelectMultiple = multiselect;

            if (extensionList.Length > 0)
            {
                filechooser.Filter = new FileFilter();
                foreach (var ext in extensionList)
                    filechooser.Filter.AddPattern($"*.{ext}");
            }

            string[] filenames = null;
            if (filechooser.Run() == (int)ResponseType.Accept)
            {
                if (multiselect)
                {
                    filenames = filechooser.Filenames;
                    if (filenames.Length > 0)
                        defaultPath = Path.GetDirectoryName(filenames[0]);
                }
                else
                {
                    var filename = filechooser.Filename;
                    if (!string.IsNullOrEmpty(filename))
                    {
                        defaultPath = Path.GetDirectoryName(filename);
                        filenames = new[] { filename };
                    }
                }
            }

            filechooser.Destroy();

            return filenames;
            */

            return null;
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, IntPtr parentWindow = default(IntPtr))
        {
            var filenames = ShowOpenFileDialog(title, extensions, ref defaultPath, false, parentWindow);

            if (filenames == null || filenames.Length == 0)
                return null;

            return filenames[0];
        }

        public static string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            /*
            var extensionList = GetExtensionList(extensions);

            Gtk.FileChooserDialog filechooser =
                new Gtk.FileChooserDialog(title,
                    null,
                    FileChooserAction.Save,
                    "Cancel", ResponseType.Cancel,
                    "Save", ResponseType.Accept);

            filechooser.Modal = true;
            filechooser.SkipTaskbarHint = true;
            filechooser.TransientFor = FamiStudioForm.Instance;
            filechooser.SetCurrentFolder(defaultPath);

            filechooser.Filter = new FileFilter();
            foreach (var ext in extensionList)
                filechooser.Filter.AddPattern($"*.{ext}");

            string filename = null;
            if (filechooser.Run() == (int)ResponseType.Accept)
            {
                filename = filechooser.Filename;

                // GTK file chooser does not add the extension automatically.
                var extension = Path.GetExtension(filename).ToLower();
                var desiredExtension = $".{extensionList[0]}";

                if (extension != desiredExtension)
                    filename = Path.ChangeExtension(filename, desiredExtension);

                defaultPath = Path.GetDirectoryName(filename);
            }

            filechooser.Destroy();

            return filename;
            */

            return null;
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            /*
            Gtk.FileChooserDialog filechooser =
                new Gtk.FileChooserDialog("Choose the file to save",
                    null,
                    FileChooserAction.Save,
                    "Cancel", ResponseType.Cancel,
                    "Save", ResponseType.Accept);

            filechooser.Modal = true;
            filechooser.Action = FileChooserAction.SelectFolder;
            filechooser.SkipTaskbarHint = true;
            filechooser.TransientFor = FamiStudioForm.Instance;
            filechooser.SetCurrentFolder(defaultPath);

            string filename = null;
            if (filechooser.Run() == (int)ResponseType.Accept)
            {
                filename = filechooser.Filename;
                defaultPath = Path.GetDirectoryName(filename);
            }

            filechooser.Destroy();

            return filename;
            */

            return null;
        }

        public static string ShowSaveFileDialog(string title, string extensions)
        {
            string dummy = "";
            return ShowSaveFileDialog(title, extensions, ref dummy);
        }

        public static DialogResult MessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            /*
            if (buttons == MessageBoxButtons.YesNoCancel)
            {
                buttons = MessageBoxButtons.YesNo;
                text += " (Close or ESC to cancel)";
            }

            MessageDialog md = new MessageDialog(null, 
                DialogFlags.Modal | DialogFlags.DestroyWithParent, 
                icon == MessageBoxIcon.Error ? MessageType.Error : MessageType.Info,
                buttons == MessageBoxButtons.YesNo ? ButtonsType.YesNo : ButtonsType.Ok, text);

            md.Modal = true;
            md.SkipTaskbarHint = true;
            md.TypeHint = Gdk.WindowTypeHint.Dialog;
            md.Title = title;
            md.TransientFor = FamiStudioForm.Instance;

            int ret = md.Run();

            md.Destroy();

            if (buttons == MessageBoxButtons.YesNo)
                return ret == -8 ? DialogResult.Yes : ret == -9 ? DialogResult.No : DialogResult.Cancel;
            else
                return DialogResult.OK;
            */

            return DialogResult.OK;
        }

        public static void MessageBoxAsync(string text, string title, MessageBoxButtons buttons, Action<DialogResult> callback = null)
        {
            var res = MessageBox(text, title, buttons);
            callback?.Invoke(res);
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start("xdg-open", url);
            }
            catch { }
        }

        public static void Beep()
        {
            SystemSounds.Beep.Play();
        }

        [DllImport("libc")]
        private static extern int prctl(int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

        public static void SetProcessName(string name)
        {
            try
            {
                var ret = prctl(15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes(name + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (ret == 0)
                    return;
            }
            catch
            {
            }

            Debug.WriteLine("Error setting process name.");
        }
        
        public static int GetCursorSize()
        {
            return 32; // MATTT
        }

        public static void SetClipboardData(byte[] data)
        {
            internalClipboardData = data;
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            return internalClipboardData;
        }

        public static string GetClipboardString()
        {
            return null;
        }

        public static void SetClipboardString(string str)
        {
            Debug.Assert(false);
        }

        public static void ClearClipboardString()
        {
        }

        public static void InitializeConsole()
        {
        }

        public static void ShutdownConsole()
        {
        }

        public const bool IsMobile  = false;
        public const bool IsAndroid = false;
        public const bool IsWindows = false;
        public const bool IsLinux   = true;
        public const bool IsMacOS   = false;
    }
}

