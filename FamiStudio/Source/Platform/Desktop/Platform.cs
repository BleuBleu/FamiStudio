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

        private static string version;
        private static Thread mainThread;

        private static bool InitializeDesktop()
        {
            if (!InitializeGLFW())
                return false;

            version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            mainThread = Thread.CurrentThread;
            return true;
        }

        private static bool InitializeGLFW()
        {
            if (glfwInit() == 0)
            {
                // MATTT : We will need a "low level" message box if we ever roll out our own.
                Platform.MessageBox("Error initializing GLFW.", "Error", MessageBoxButtons.OK);
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

        public static void DelayedMessageBoxAsync(string text, string title)
        {
        }

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

        public static double TimeSeconds()
        {
            return glfwGetTime();
        }

        public const bool IsDesktop = true;
    }
}
