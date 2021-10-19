using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Gtk;
using OpenTK;

using Action = System.Action;

namespace FamiStudio
{
    public static class PlatformUtils
    {
        public static string ApplicationVersion => System.Windows.Forms.Application.ProductVersion;
        public static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/FamiStudio");
        public static string UserProjectsDirectory => null;

        private static Thread mainThread;

        public static void Initialize()
        {
            mainThread = Thread.CurrentThread;

            // When debugging or when in a app package, our paths are a bit different.
            string[] pathsToSearch =
            {
                "./Resources/",
                "../../Resources/",
                "../Resources/Fonts/",
                "."
            };

            string[] fontsToLoad =
            {
                "Quicksand-Regular.ttf",
                "Quicksand-Bold.ttf"
            };

            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var path in pathsToSearch)
            {
                var absPath = Path.Combine(appPath, path);

                if (File.Exists(Path.Combine(absPath, fontsToLoad[0])))
                {
                    foreach (var font in fontsToLoad)
                        MacUtils.CoreTextRegisterFont(Path.Combine(absPath, font));
                    break;
                }
            }

            InitializeGtk();
        }

        public static bool IsInMainThread()
        {
            return mainThread == Thread.CurrentThread;
        }

        public static Size GetScreenResolution()
        {
            Debug.Assert(false);
            return Size.Empty;
        }

        public static int GetOutputAudioSampleSampleRate()
        {
            return 44100;
        }

        private static void ParseRcFileFromResource(string rcFile)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(rcFile))
            using (var reader = new StreamReader(stream))
            {
                string gtkrc = reader.ReadToEnd();
                Gtk.Rc.ParseString(gtkrc);
            }
        }

        public static void InitializeGtk()
        {
            Gtk.Application.Init();

            var rcFile = "FamiStudio.Resources.gtk_mac.rc";
            ParseRcFileFromResource(rcFile);
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

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect, Window parentWindow = null)
        {
            var extensionList = GetExtensionList(extensions);

            var filenames = MacUtils.ShowOpenDialog(title, extensionList, multiselect, defaultPath);
            if (filenames != null && !string.IsNullOrEmpty(filenames[0]))
                defaultPath = Path.GetDirectoryName(filenames[0]);
            return filenames;
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, Window parentWindow = null)
        {
            var filenames = ShowOpenFileDialog(title, extensions, ref defaultPath, false, parentWindow);

            if (filenames == null || filenames.Length == 0)
                return null;

            return filenames[0];
        }

        public static string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            var extensionList = GetExtensionList(extensions);

            var filename = MacUtils.ShowSaveDialog(title, extensionList, defaultPath);
            if (!string.IsNullOrEmpty(filename))
                defaultPath = Path.GetDirectoryName(filename);
            return filename;
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            var filename = MacUtils.ShowBrowseFolderDialog(title, defaultPath);
            if (!string.IsNullOrEmpty(filename))
            {
                if (Directory.Exists(filename))
                    defaultPath = filename;
                else
                    defaultPath = Path.GetDirectoryName(filename);
                return defaultPath;
            }
            return null;
        }

        public static string ShowSaveFileDialog(string title, string extensions)
        {
            string dummy = "";
            return ShowSaveFileDialog(title, extensions, ref dummy);
        }

        public static DialogResult MessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            return MacUtils.ShowAlert(text, title, buttons);
        }

        public static void MessageBoxAsync(string text, string title, MessageBoxButtons buttons, Action<DialogResult> callback = null)
        {
            var res = MessageBox(text, title, buttons);
            callback?.Invoke(res);
        }

        public static string KeyCodeToString(int keyval)
        {
            var str = char.ConvertFromUtf32((int)Gdk.Keyval.ToUnicode((uint)keyval));

            // Fallback to key enum for special keys.
            if (str.Length == 0 || (str.Length == 1 && str[0] <= 32))
            {
                return ((Gdk.Key)keyval).ToString();
            }

            return str.ToUpper();
        }

        public static Gdk.Pixbuf LoadBitmapFromResource(string name)
        {
            return Gdk.Pixbuf.LoadFromResource(name);
        }

        public static float GetDesktopScaling()
        {
            return MacUtils.MainWindowScaling;
        }

        public static void StartMobileLoadFileOperationAsync(string mimeType, Action<string> callback)
        {
        }

        public static void StartMobileSaveFileOperationAsync(string mimeType, string filename, Action<string> callback)
        {
        }

        public static void FinishMobileSaveFileOperationAsync(bool commit, Action callback)
        {
        }

        public static void StartShareFileAsync(string filename, Action callback)
        {
        }

        public static string GetShareFilename(string filename)
        {
            return null;
        }

        public static void VibrateTick()
        {
        }

        public static void VibrateClick()
        {
        }

        public static void ShowToast(string text)
        {
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start("open", url);
            }
            catch { }
        }

        public static void Beep()
        {
            SystemSounds.Beep.Play();
        }

        public static void SetClipboardData(byte[] data)
        {
            MacUtils.SetPasteboardData(data);
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            return MacUtils.GetPasteboardData();
        }

        public static string GetClipboardString()
        {
            return MacUtils.GetPasteboardString();
        }

        public static void ClearClipboardString()
        {
            MacUtils.ClearPasteboardString();
        }

        public const bool IsMobile  = false;
        public const bool IsAndroid = false;
        public const bool IsDesktop = true;
        public const bool IsWindows = false;
        public const bool IsGTK     = true;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = true;
    }
}

