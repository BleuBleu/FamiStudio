using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Media;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Reflection;

namespace FamiStudio
{
    public static partial class Platform
    {
        public static string UserProjectsDirectory => null;
        public static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FamiStudio");
        public static float DoubleClickTime => doubleClickTime;

        public const string DllPrefix = "";
        public const string DllExtension = ".dll";

        private static bool xaudio2Available;
        private static float doubleClickTime;

        private static MultiMediaNotificationListener mediaNotificationListener;

        public static bool Initialize(bool commandLine)
        {
            if (!DetectRequiredDependencies())
                return false;

            if (!InitializeDesktop(commandLine))
                return false;

            if (!commandLine)
            {
                mediaNotificationListener = new MultiMediaNotificationListener();
                mediaNotificationListener.DefaultDeviceChanged += MmNoticiations_DefaultDeviceChanged;

                xaudio2Available = XAudio2Stream.TryDetectXAudio2();
            }

            clipboardFormat = RegisterClipboardFormat("FamiStudio");
            doubleClickTime = GetDoubleClickTime() / 1000.0f;

#if !DEBUG
            if (IsPortableMode)
                AssociateExtension(".fms", Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe"), "FamiStudio Project", "FamiStudio Project");
#endif

            return true;
        }

        public static void Shutdown()
        {
            ShutdownDesktop();
        }

        public static string[] GetAvailableAudioAPIs()
        {
            var apis = new string[xaudio2Available ? 2 : 1];
            apis[0] = "WASAPI";
            if (xaudio2Available) 
                apis[1] = "XAudio2";
            return apis;
        }

        public static IAudioStream CreateAudioStream(string api, int rate, bool stereo, int bufferSizeMs)
        {
            if (api == "XAudio2" && xaudio2Available)
            {
                return XAudio2Stream.Create(rate, stereo, bufferSizeMs);
            }
            else
            {
                return PortAudioStream.Create(rate, stereo, bufferSizeMs);
            }
        }

        public static int AudioDeviceSampleRate => PortAudioStream.DeviceSampleRate;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public IntPtr file = IntPtr.Zero;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        private const int OFN_ALLOWMULTISELECT = 0x00000200;
        private const int OFN_EXPLORER = 0x00080000;
        private const int OFN_OVERWRITEPROMPT = 0x00000002;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_FILEMUSTEXIST = 0x00001000;

        public static unsafe string[] ShowPlatformOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect)
        {
            OpenFileName ofn = new OpenFileName();

            var str = new char[4096];

            fixed (char* p = &str[0])
            {
                ofn.structSize = Marshal.SizeOf(ofn);
                ofn.dlgOwner = FamiStudioWindow.Instance.Handle;
                ofn.filter = extensions.Replace('|', '\0') + "\0";
                ofn.file = new IntPtr(p);
                ofn.maxFile = str.Length;
                ofn.flags = (multiselect ? OFN_ALLOWMULTISELECT | OFN_EXPLORER : 0) | OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
                ofn.initialDir = defaultPath;
                ofn.title = title;

                if (GetOpenFileName(ofn))
                {
                    var strings = new List<string>();

                    var idx0 = -1;
                    while (true)
                    {
                        var idx1 = Array.IndexOf(str, '\0', idx0 + 1);
                        if (idx1 == idx0 + 1)
                            break;
                        strings.Add(new string(str, idx0 + 1, idx1 - idx0 - 1));
                        idx0 = idx1;
                    }

                    // When multiselect is allowed and the user selects multiple
                    // first, index 0 is the path, followed by the filenames.
                    if (strings.Count > 1)
                    {
                        Debug.Assert(multiselect);

                        for (int i = 1; i < strings.Count; i++)
                            strings[i] = Path.Combine(strings[0], strings[i]);

                        strings.RemoveAt(0);
                    }

                    defaultPath = Path.GetDirectoryName(strings[0]);
                    return strings.ToArray();
                }
            }

            return null;
        }

        public static unsafe string ShowPlatformSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            OpenFileName ofn = new OpenFileName();

            var str = new char[4096];

            fixed (char* p = &str[0])
            {
                ofn.structSize = Marshal.SizeOf(ofn);
                ofn.dlgOwner = FamiStudioWindow.Instance.Handle;
                ofn.filter = extensions.Replace('|', '\0') + "\0";
                ofn.file = new IntPtr(p);
                ofn.maxFile = str.Length;
                ofn.flags = OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST;
                ofn.initialDir = defaultPath;
                ofn.title = title;
                ofn.defExt = extensions.Substring(extensions.Length - 3);

                if (GetSaveFileName(ofn))
                {
                    var len = Array.IndexOf(str, '\0');
                    var filename = new string(str, 0, len);
                    defaultPath = Path.GetDirectoryName(filename);
                    return filename;
                }
            }

            return null;
        }

        private struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string pszDisplayName;
            public string lpszTitle;
            public uint ulFlags;
            public BrowseCallBackProc lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        private const int BIF_SHAREABLE = 0x00008000;
        private const int BIF_NEWDIALOGSTYLE = 0x00000040;

        private const int WM_USER = 0x400;
        private const int BFFM_INITIALIZED = 1;
        private const int BFFM_SETSELECTIONA = WM_USER + 102;
        private const int BFFM_SETSELECTIONW = WM_USER + 103;

        private delegate int BrowseCallBackProc(IntPtr hwnd, int msg, IntPtr lp, IntPtr wp);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        [DllImport("shell32.dll")]
        private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        [DllImport("shell32.dll")]
        private static extern Int32 SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        private static string browseInitialPath;

        private static int OnBrowseEvent(IntPtr hwnd, int msg, IntPtr lp, IntPtr wp)
        {
            if (msg == BFFM_INITIALIZED)
                SendMessage(hwnd, BFFM_SETSELECTIONW, 1, browseInitialPath);
            return 0;
        }

        public static string ShowPlatformBrowseFolderDialog(string title, ref string defaultPath)
        {
            var sb = new StringBuilder(4096);
            var pidl = IntPtr.Zero;

            BROWSEINFO bi;
            bi.hwndOwner = FamiStudioWindow.Instance.Handle;
            bi.pidlRoot = IntPtr.Zero;
            bi.pszDisplayName = null;
            bi.lpszTitle = title;
            bi.ulFlags = BIF_NEWDIALOGSTYLE | BIF_SHAREABLE;
            bi.lpfn = Directory.Exists(defaultPath) ? new BrowseCallBackProc(OnBrowseEvent) : null;
            bi.lParam = IntPtr.Zero;
            bi.iImage = 0;

            try
            {
                browseInitialPath = defaultPath;
                pidl = SHBrowseForFolder(ref bi);
                if (SHGetPathFromIDList(pidl, sb) == 0)
                {
                    return null;
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pidl);
            }

            defaultPath = sb.ToString();
            return defaultPath;
        }

        // Declares managed prototypes for unmanaged functions.
        [DllImport("User32.dll", EntryPoint = "MessageBox", CharSet = CharSet.Auto)]
        internal static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        const uint MB_TASKMODAL = 0x00002000;
        const uint MB_ICONERROR = 0x00000010;

        public static DialogResult PlatformMessageBox(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons)
        {
            var icons = title.ToLowerInvariant().Contains("error") ? MB_ICONERROR : 0;
            return (DialogResult)MessageBox(IntPtr.Zero, text, title, (uint)buttons | (uint)icons | MB_TASKMODAL);
        }

        public static bool IsVS2019RuntimeInstalled()
        {
            try
            {
                // Super ghetto way of detecting if the runtime is installed is simply by calling
                // any function that will cause a C++ DLL to be loaded.
                NesApu.GetAudioExpansions(0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DetectRequiredDependencies()
        {
            if (!IsVS2019RuntimeInstalled())
            {
                if (MessageBox(null, "You seem to be missing the VS 2019 C++ Runtime which is required to run FamiStudio, would you like to visit the FamiStudio website for instruction on how to install it?", "Missing Component", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    OpenUrl("https://famistudio.org/doc/install/#windows");
                }

                return false;
            }

            return true;
        }

        [DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        [DllImport("user32.dll")]
        static extern int MessageBeep(int type);

        public static void Beep()
        {
            MessageBeep(0);
        }

        [DllImport("kernel32.dll")]
        public unsafe static extern bool RtlZeroMemory(void* destination, int length);

        public static unsafe void ZeroMemory(IntPtr p, int len)
        {
            RtlZeroMemory(p.ToPointer(), len);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr name);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hgdiobj, int cbBuffer, out BITMAP bmp);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public Int32 xHotspot;
            public Int32 yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public int bmPlanes;
            public int bmBitsPixel;
            public IntPtr bmBits;
        };

        private const int IDC_ARROW = 32512;

        public static int GetCursorSize(float scaling)
        {
            var size = 32;
            var arrow = LoadCursor(IntPtr.Zero, (IntPtr)IDC_ARROW);

            if (GetIconInfo(arrow, out var info))
            {
                var bBWCursor = (info.hbmColor == IntPtr.Zero);

                if (GetObject(info.hbmMask, Marshal.SizeOf(typeof(BITMAP)), out var bmpInfo) != 0)
                {
                    size = bmpInfo.bmWidth;
                    size = Math.Abs(bmpInfo.bmHeight) / (bBWCursor ? 2 : 1);
                }

                DeleteObject(info.hbmColor);
                DeleteObject(info.hbmMask);
            }

            return size;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegisterClipboardFormat(string format);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int IsClipboardFormatAvailable(int format);
        [DllImport("user32.dll")]
        private static extern int OpenClipboard(IntPtr hwnd);
        [DllImport("user32.dll", EntryPoint = "GetClipboardData")]
        private static extern IntPtr GetClipboardDataWin32(int wFormat);
        [DllImport("user32.dll", EntryPoint = "GetClipboardFormatNameA")]
        private static extern int GetClipboardFormatName(int wFormat, string lpString, int nMaxCount);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(int wFlags, int dwBytes);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern int GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr mem);
        [DllImport("user32.dll")]
        private static extern int CloseClipboard();
        [DllImport("user32.dll")]
        private static extern int SetClipboardData(int wFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        private static extern int EmptyClipboard();

        const int CF_TEXT = 1;
        const int CF_UNICODETEXT = 13;
        const int GMEM_MOVEABLE = 2;

        private static int clipboardFormat = -1;

        public static void SetClipboardData(byte[] data)
        {
            IntPtr mem = IntPtr.Zero;

            if (data == null)
            {
                mem = GlobalAlloc(GMEM_MOVEABLE, 0);
            }
            else
            {
                mem = GlobalAlloc(GMEM_MOVEABLE, data == null ? 0 : data.Length);
                var ptr = GlobalLock(mem);
                Marshal.Copy(data, 0, ptr, data.Length);
                GlobalUnlock(mem);
            }

            if (OpenClipboard(IntPtr.Zero) != 0)
            {
                SetClipboardData(clipboardFormat, mem);
                CloseClipboard();
            }
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            byte[] buffer = null;

            if (IsClipboardFormatAvailable(clipboardFormat) != 0)
            {
                if (OpenClipboard(IntPtr.Zero) != 0)
                {
                    var mem = GetClipboardDataWin32(clipboardFormat);
                    if (mem != IntPtr.Zero)
                    {
                        var size = Math.Min(maxSize, GlobalSize(mem));
                        var ptr = GlobalLock(mem);
                        buffer = new byte[size];
                        Marshal.Copy(ptr, buffer, 0, size);
                        GlobalUnlock(mem);
                    }
                    CloseClipboard();
                }
            }

            return buffer;
        }

        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        private const uint HKEY_CURRENT_USER = 0x80000001;

        private const int KEY_CREATE_SUB_KEY = 0x0004;
        private const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
        private const int KEY_QUERY_VALUE = 0x0001;
        private const int KEY_READ = 0x20019;
        private const int KEY_SET_VALUE = 0x0002;
        private const int KEY_WRITE = 0x20006;

        private const int RRF_RT_REG_SZ = 0x00000002;

        [DllImport("advapi32.dll", SetLastError = false)]
        static extern int RegCreateKeyEx(
                    IntPtr hKey,
                    string lpSubKey,
                    IntPtr Reserved,
                    string lpClass,
                    int dwOptions,
                    int samDesired,
                    IntPtr lpSecurityAttributes,
                    out IntPtr phkResult,
                    out int lpdwDisposition);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern uint RegQueryValueEx(
            IntPtr hKey,
            string lpValueName,
            int lpReserved,
            ref int lpType,
            IntPtr lpData,
            ref int lpcbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegSetValueEx(
            IntPtr hKey,
            [MarshalAs(UnmanagedType.LPStr)] string lpValueName,
            int Reserved,
            int dwType,
            IntPtr lpData,
            int cbData);

        private static string GetKeyDefaultValueString(IntPtr hkey)
        {
            var size = 0;
            var type = RRF_RT_REG_SZ;
            RegQueryValueEx(hkey, null, 0, ref type, IntPtr.Zero, ref size);
            var result = Marshal.AllocHGlobal(size);
            RegQueryValueEx(hkey, null, 0, ref type, result, ref size);
            var str = Marshal.PtrToStringAnsi(result);
            Marshal.FreeHGlobal(result);
            return str;
        }

        private static void SetKeyDefaultValueString(IntPtr hkey, string value)
        {
            var size = value.Length + 1;
            var data = Marshal.StringToHGlobalAnsi(value);
            RegSetValueEx(hkey, null, 0, RRF_RT_REG_SZ, data, size);
        }

        private static bool SetKeyDefaultValue(string keyPath, string value)
        {
            if (RegCreateKeyEx(new IntPtr(HKEY_CURRENT_USER), keyPath, IntPtr.Zero, null, 0, KEY_QUERY_VALUE | KEY_SET_VALUE, IntPtr.Zero, out var hkey, out _) == 0)
            {
                if (GetKeyDefaultValueString(hkey) != value)
                {
                    SetKeyDefaultValueString(hkey, value);
                }

                RegCloseKey(hkey);
            }

            return false;
        }

        public static void AssociateExtension(string extension, string executable, string description, string progId)
        {
            try
            {
                var madeChanges = false;
                madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + extension, progId);
                madeChanges |= SetKeyDefaultValue(@"Software\Classes\" + progId, description);
                madeChanges |= SetKeyDefaultValue($@"Software\Classes\{progId}\shell\open\command", "\"" + executable + "\" \"%1\"");
                if (madeChanges)
                {
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch
            {
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        const uint WM_CHAR = 0x0102;
        const int VK_ENTER = 0x0D;

        public static void InitializeConsole()
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
        }

        public static void ShutdownConsole()
        {
            SendMessage(GetConsoleWindow(), WM_CHAR, (IntPtr)VK_ENTER, IntPtr.Zero);
        }

        private static void MmNoticiations_DefaultDeviceChanged()
        {
            AudioDeviceChanged?.Invoke();
        }

        public const bool IsMobile  = false;
        public const bool IsAndroid = false;
        public const bool IsWindows = true;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = false;
    }
}
