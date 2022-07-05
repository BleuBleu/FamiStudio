using System;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static partial class Platform
    {
        public delegate void AudioDeviceChangedDelegate();
        public static event AudioDeviceChangedDelegate AudioDeviceChanged;

        public static string ApplicationVersion => version;
        public static bool IsCommandLine => !initializedGlfw;

        private static bool initializedGlfw;
        private static string version;
        private static Thread mainThread;

        private static bool InitializeDesktop(bool commandLine)
        {
            if (!commandLine)
            {
                if (!InitializeGLFW())
                    return false;
                initializedGlfw = true;
            }

            version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            mainThread = Thread.CurrentThread;
            return true;
        }

        private static void ShutdownDesktop()
        {
            if (initializedGlfw)
                glfwTerminate();
        }

        private static bool InitializeGLFW()
        {
            if (glfwInit() == 0)
            {
                MessageBox(null, "Error initializing GLFW.", "Error", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        public static bool IsInMainThread()
        {
            return mainThread == Thread.CurrentThread;
        }

        public static bool IsPortableMode
        {
            get
            {
                var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var portableFile = Path.Combine(appPath, "portable.txt");

                return File.Exists(portableFile);
            }
        }

        public static int GetPixelDensity()
        {
            return 96; // Unused.
        }

        public static Size GetScreenResolution()
        {
            Debug.Assert(false); // Unused.
            return Size.Empty;
        }

        public static int GetOutputAudioSampleSampleRate()
        {
            return 44100;
        }

        public static unsafe string[] ShowOpenFileDialog(FamiStudioWindow win, string title, string extensions, ref string defaultPath, bool multiselect)
        {
            if (Settings.UseOSDialogs)
            {
                return ShowPlatformOpenFileDialog(win, title, extensions, ref defaultPath, multiselect);
            }
            else
            {
                var dlg = new FileDialog(win, FileDialog.Mode.Open, title, defaultPath, extensions);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    defaultPath = Path.GetDirectoryName(dlg.SelectedPath);
                    return new[] { dlg.SelectedPath };
                }
                return null;
            }
        }

        public static unsafe string ShowSaveFileDialog(FamiStudioWindow win, string title, string extensions, ref string defaultPath)
        {
            if (Settings.UseOSDialogs)
            {
                return ShowPlatformSaveFileDialog(win, title, extensions, ref defaultPath);
            }
            else
            {
                var dlg = new FileDialog(win, FileDialog.Mode.Save, title, defaultPath, extensions);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    defaultPath = Path.GetDirectoryName(dlg.SelectedPath);
                    return dlg.SelectedPath;
                }
                return null;
            }
        }

        public static string ShowBrowseFolderDialog(FamiStudioWindow win, string title, ref string defaultPath)
        {
            if (Settings.UseOSDialogs)
            {
                return ShowPlatformBrowseFolderDialog(win, title, ref defaultPath);
            }
            else
            {
                var dlg = new FileDialog(win, FileDialog.Mode.Folder, title, defaultPath);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    defaultPath = dlg.SelectedPath;
                    return defaultPath;
                }
                return null;
            }
        }

        public static DialogResult MessageBox(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons)
        {
            if (Settings.UseOSDialogs || win == null)
            {
                return PlatformMessageBox(win, text, title, buttons);
            }
            else
            {
                var dlg = new MessageDialog(win, text, title, buttons);
                return dlg.ShowDialog();
            }
        }

        public static string ShowOpenFileDialog(FamiStudioWindow win, string title, string extensions, ref string defaultPath)
        {
            var filenames = ShowOpenFileDialog(win, title, extensions, ref defaultPath, false);

            if (filenames == null || filenames.Length == 0)
                return null;

            return filenames[0];
        }

        public static string ShowSaveFileDialog(FamiStudioWindow win, string title, string extensions)
        {
            string dummy = "";
            return ShowSaveFileDialog(win, title, extensions, ref dummy);
        }

        public static void MessageBoxAsync(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons, Action<DialogResult> callback = null)
        {
            var res = MessageBox(win, text, title, buttons);
            callback?.Invoke(res);
        }

        public static void DelayedMessageBoxAsync(string text, string title)
        {
        }

        public static int GetKeyScancode(Keys key)
        {
            return initializedGlfw ? glfwGetKeyScancode((int)key) : 0;
        }

        public static string KeyToString(Keys key)
        {
            return glfwGetKeyName((int)key, 0);
        }

        public static string ScancodeToString(int scancode)
        {
            return glfwGetKeyName((int)Keys.Unknown, scancode);
        }

        public static string GetClipboardString()
        {
            return glfwGetClipboardString(IntPtr.Zero);
        }

        public static void SetClipboardString(string str)
        {
            glfwSetClipboardString(IntPtr.Zero, str);
        }

        public static void ClearClipboardString()
        {
            glfwSetClipboardString(IntPtr.Zero, "");
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

        public static double TimeSeconds()
        {
            return glfwGetTime();
        }

        public const bool IsDesktop = true;
    }
}
