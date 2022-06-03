using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using static GLFWDotNet.GLFW;

using Action = System.Action;

namespace FamiStudio
{
    public static class Platform
    {
        public static string ApplicationVersion => version;
        public static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/FamiStudio");
        public static string UserProjectsDirectory => null;
        public static float DoubleClickTime => 0.25f; // MATTT

        public const string DllPrefix = "";
        public const string DllExtension = ".dylib";

        private static Thread mainThread;
        private static string version;

        public static bool Initialize()
        {
            mainThread = Thread.CurrentThread;
            version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            return true;
        }

        public static bool IsInMainThread()
        {
            return mainThread == Thread.CurrentThread;
        }

        public static int GetPixelDensity()
        {
            return 96; // Unused.
        }

        public static System.Drawing.Size GetScreenResolution()
        {
            Debug.Assert(false);
            return System.Drawing.Size.Empty;
        }

        public static int GetOutputAudioSampleSampleRate()
        {
            return 44100;
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
            var extensionList = GetExtensionList(extensions);

            var filenames = MacUtils.ShowOpenDialog(title, extensionList, multiselect, defaultPath);
            if (filenames != null && !string.IsNullOrEmpty(filenames[0]))
                defaultPath = Path.GetDirectoryName(filenames[0]);
            return filenames;
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
            var extensionList = GetExtensionList(extensions);

            var filename = MacUtils.ShowSaveDialog(title, extensionList, defaultPath);
            if (!string.IsNullOrEmpty(filename))
                defaultPath = Path.GetDirectoryName(filename);
            return filename;
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            var filename = MacUtils.ShowBrowseFolderDialog(title, ref defaultPath);
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

        public static void DelayedMessageBoxAsync(string text, string title)
        {
        }

        // MATTT : Do we want to move the GLFW common code else where?
        public static int GetKeyScancode(Keys key)
        {
            return glfwGetKeyScancode((int)key);
        }

        public static string KeyToString(Keys key)
        {
            return glfwGetKeyName((int)key, 0);
        }

        public static string ScancodeToString(int scancode)
        {
            return glfwGetKeyName((int)Keys.Unknown, scancode);
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
                Process.Start("open", $"\"{url}\"");
            }
            catch { }
        }

        public static void Beep()
        {
            SystemSounds.Beep.Play();
        }

        public static double TimeSeconds()
        {
            return glfwGetTime();
        }

        public static int GetCursorSize()
        {
            return 32; // MATTT
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

        public static void SetClipboardString(string str)
        {
            // MATTT
            Debug.Assert(false);
        }

        public static void ClearClipboardString()
        {
            MacUtils.ClearPasteboardString();
        }

        public const bool IsMobile  = false;
        public const bool IsAndroid = false;
        public const bool IsDesktop = true;
        public const bool IsWindows = false;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = true;
    }
}

