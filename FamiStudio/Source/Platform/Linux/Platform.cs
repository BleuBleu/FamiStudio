using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public static partial class Platform
    {
        private static byte[] internalClipboardData;

        private static short[] beep;

        public static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FamiStudio");
        public static string UserProjectsDirectory => null;
        public static float DoubleClickTime => 0.5f; // 0.5 sec is the default on both Windows and Mac. So let's use that.

        public const bool   DllStaticLib = false;
        public const string DllPrefix = "lib";
        public const string DllExtension = ".so";

        public static int RtMidiVersionHint { get; private set; } = 6; // We whip with version 5.0, which is named 6.0.0. Go figure.

        public static bool Initialize(bool commandLine)
        {
            // Must be set before GLFW dll tries to load.
            NativeLibrary.SetDllImportResolver(typeof(Platform).Assembly, DllImportResolver);

            if (!InitializeDesktop(commandLine))
                return false;

            SetProcessName("FamiStudio");

            beep = WaveFile.LoadFromResource("FamiStudio.Resources.Sounds.LinuxBeep.wav", out _);

            return true;
        }

        public static void Shutdown()
        {
            ShutdownDesktop();
        }
        
        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            var handle = IntPtr.Zero;

            if (libraryName.Contains("libopenal"))
            {
                // Try to use the OpenAL that is on the system first.
                if (NativeLibrary.TryLoad("libopenal.so.1", assembly, DllImportSearchPath.System32, out handle))
                {
                    return handle;
                }
            }
            else if (libraryName.Contains("libdl"))
            {
                // I've seen some distros with various names here. 
                if (NativeLibrary.TryLoad("libdl.so.2", assembly, DllImportSearchPath.System32, out handle))
                {
                    return handle;
                }
            }
            else if (libraryName.Contains("libX11"))
            {
                // Ubuntu 22.04 (on my laptop) seem to want the full name.
                if (NativeLibrary.TryLoad("libX11.so.6", assembly, DllImportSearchPath.System32, out handle))
                {
                    return handle;
                }
            }
            else if (libraryName.Contains("rtmidi"))
            {
                // Let's try to load version 4.0, 5.0, or 6.0 (which are strangely named 5.0.0, 6.0.0, and 7.0.0 respectively...)
                for (var i = 7; i >= 5; i--)
                {
                    if (NativeLibrary.TryLoad($"librtmidi.so.{i}", assembly, DllImportSearchPath.System32, out handle))
                    {
                        // See comment in RtMidi.GetDeviceName() to see why we do this.
                        RtMidiVersionHint = i;
                        return handle;
                    }
                }
            }

            // Fallback to our own binary as a last resort.
            NativeLibrary.TryLoad(libraryName, assembly, DllImportSearchPath.ApplicationDirectory, out handle);
            
            return handle;
        }

        public static string[] GetAvailableAudioAPIs()
        {
            return new[] { "OpenAL Soft" };
        }

        public static IAudioStream CreateAudioStream(string api, int rate, bool stereo, int bufferSizeMs)
        {
            return OpenALStream.Create(rate, stereo, bufferSizeMs);
        }

        public static unsafe string[] ShowPlatformOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect)
        {
            var dlg = new LinuxDialog(LinuxDialog.DialogMode.Open, title, ref defaultPath, extensions, multiselect);
            return dlg.SelectedPaths;
        }

        public static unsafe string ShowPlatformSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            var dlg = new LinuxDialog(LinuxDialog.DialogMode.Save, title, ref defaultPath, extensions);
            return dlg.SelectedPaths?[0];
        }

        public static string ShowPlatformBrowseFolderDialog(string title, ref string defaultPath)
        {
            var dlg = new LinuxDialog(LinuxDialog.DialogMode.Folder, title, ref defaultPath);
            return dlg.SelectedPaths?[0];
        }

        public static DialogResult PlatformMessageBox(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons)
        {
            var dlg = new LinuxDialog(text, title, buttons);
            return dlg.MessageBoxSelection;
        }

        public static bool PlatformIsOutOfProcessDialogInProgress()
        {
            return LinuxDialog.IsDialogOpen;
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
            FamiStudio.StaticInstance.PlayRawPcmSample(beep, 44100, 1.0f, 1);
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
        
        public static int GetCursorSize(float scaling)
        {
            return (int)(32 * scaling); // No way to detect this on Linux.
        }

        public static void SetClipboardData(byte[] data)
        {
            internalClipboardData = data;
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            return internalClipboardData;
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

