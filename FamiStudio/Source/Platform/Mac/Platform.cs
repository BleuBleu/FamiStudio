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
    public static partial class Platform
    {
        public static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/FamiStudio");
        public static string UserProjectsDirectory => null;
        public static float DoubleClickTime => MacUtils.DoubleClickInterval;

        public const string DllPrefix = "";
        public const string DllExtension = ".dylib";

        public const int RtMidiVersionHint = 6;

        public static bool Initialize(bool commandLine)
        {
            MacUtils.Initialize();
            MacUtils.AudioDeviceChanged += Platform_AudioDeviceChanged;

            if (!InitializeDesktop(commandLine))
                return false;

            return true;
        }

        public static void Shutdown()
        {
            ShutdownDesktop();
        }

        private void Platform_AudioDeviceChanged()
        {
            AudioDeviceChanged?.Invoke();
        }

        public static IAudioStream CreateAudioStream(int rate, bool stereo, int bufferSizeMs)
        {
            return PortAudioStream.Create(rate, stereo, bufferSizeMs);
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

        public static unsafe string[] ShowPlatformOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect)
        {
            var extensionList = GetExtensionList(extensions);

            var filenames = MacUtils.ShowOpenDialog(title, extensionList, multiselect, defaultPath);
            if (filenames != null && !string.IsNullOrEmpty(filenames[0]))
                defaultPath = Path.GetDirectoryName(filenames[0]);
            return filenames;
        }

        public static unsafe string ShowPlatformSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            var extensionList = GetExtensionList(extensions);

            var filename = MacUtils.ShowSaveDialog(title, extensionList, defaultPath);
            if (!string.IsNullOrEmpty(filename))
                defaultPath = Path.GetDirectoryName(filename);
            return filename;
        }

        public static string ShowPlatformBrowseFolderDialog(string title, ref string defaultPath)
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

        public static DialogResult PlatformMessageBox(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons)
        {
            return MacUtils.ShowAlert(text, title, buttons);
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
            MacUtils.Beep();
        }

        public static unsafe void ZeroMemory(IntPtr p, int len)
        {
            byte* pp = (byte*)p.ToPointer();
            for (var i = 0; i < len; i++)
                pp[i] = 0;
        }

        public static int GetCursorSize(float scaling)
        {
            // TODO : There is a way to get the cursor size on MacOS from the accessibility
            // settings. Will eventually do that.
            return (int)(32 * scaling); 
        }

        public static void SetClipboardData(byte[] data)
        {
            MacUtils.SetPasteboardData(data);
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            return MacUtils.GetPasteboardData();
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
        public const bool IsLinux   = false;
        public const bool IsMacOS   = true;
    }
}

