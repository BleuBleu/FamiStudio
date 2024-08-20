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
        public static bool CanExportToVideo => !string.IsNullOrEmpty(Settings.FFmpegExecutablePath);

        public const bool ThreadOwnsGLContext = true;

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

        public static IVideoEncoder CreateVideoEncoder()
        {
            if (VideoEncoderFFmpeg.DetectFFmpeg())
            {
                return new VideoEncoderFFmpeg();
            }
            else
            {
                return null;
            }
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

        public static unsafe string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect)
        {
            if (Settings.UseOSDialogs)
            {
                return ShowPlatformOpenFileDialog(title, extensions, ref defaultPath, multiselect);
            }
            else
            {
                var dlg = new FileDialog(FamiStudioWindow.Instance, FileDialog.Mode.Open, title, defaultPath, extensions);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    defaultPath = Path.GetDirectoryName(dlg.SelectedPath);
                    return new[] { dlg.SelectedPath };
                }
                return null;
            }
        }

        public static unsafe string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            if (Settings.UseOSDialogs)
            {
                return ShowPlatformSaveFileDialog(title, extensions, ref defaultPath);
            }
            else
            {
                var dlg = new FileDialog(FamiStudioWindow.Instance, FileDialog.Mode.Save, title, defaultPath, extensions);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    defaultPath = Path.GetDirectoryName(dlg.SelectedPath);
                    return dlg.SelectedPath;
                }
                return null;
            }
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            if (Settings.UseOSDialogs)
            {
                return ShowPlatformBrowseFolderDialog(title, ref defaultPath);
            }
            else
            {
                var dlg = new FileDialog(FamiStudioWindow.Instance, FileDialog.Mode.Folder, title, defaultPath);
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

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath)
        {
            var filenames = ShowOpenFileDialog(title, extensions, ref defaultPath, false);

            if (filenames == null || filenames.Length == 0)
                return null;

            return filenames[0];
        }

        public static string ShowSaveFileDialog(string title, string extensions)
        {
            string dummy = "";
            return ShowSaveFileDialog(title, extensions, ref dummy);
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
            Debug.Assert(initializedGlfw);
            return initializedGlfw && key != Keys.Unknown ? glfwGetKeyScancode((int)key) : -1;
        }

        public static string KeyToString(Keys key)
        {
            Debug.Assert(initializedGlfw);
            return glfwGetKeyName((int)key, 0);
        }

        public static string ScancodeToString(int scancode)
        {
            Debug.Assert(initializedGlfw);
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

        public static void StartMobileLoadFileOperationAsync(string[] extensions, Action<string> callback)
        {
        }

        public static void StartMobileSaveFileOperationAsync(string[] extensions, string filename, Action<string> callback)
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

        public static void ForceScreenOn(bool on)
        {
        }
        
        public static void AcquireGLContext()
        {
        }

        public static void EditTextAsync(string prompt, string text, Action<string> callback)
        {
        }

        public static void ShowToast(FamiStudioWindow win, string text, bool longDuration = false, Action click = null)
        {
            win.ShowToast(text, longDuration, click);
        }

        public static double TimeSeconds()
        {
            return glfwGetTime();
        }

        public const bool IsDesktop = true;
    }
}
