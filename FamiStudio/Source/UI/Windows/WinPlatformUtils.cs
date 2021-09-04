using System;
using System.Reflection;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public static class PlatformUtils
    {
        public static PrivateFontCollection PrivateFontCollection;
        public static string ApplicationVersion => Application.ProductVersion;

        public static void Initialize()
        {
            PrivateFontCollection = new PrivateFontCollection();
            AddFontFromMemory(PrivateFontCollection, "FamiStudio.Resources.Quicksand-Regular.ttf");
            AddFontFromMemory(PrivateFontCollection, "FamiStudio.Resources.Quicksand-Bold.ttf");
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

        private static void AddFontFromMemory(PrivateFontCollection pfc, string name)
        {
            var fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

            byte[] fontdata = new byte[fontStream.Length];
            fontStream.Read(fontdata, 0, (int)fontStream.Length);
            fontStream.Close();

            uint c = 0;
            var p = Marshal.AllocCoTaskMem(fontdata.Length);
            Marshal.Copy(fontdata, 0, p, fontdata.Length);
            AddFontMemResourceEx(p, (uint)fontdata.Length, IntPtr.Zero, ref c);
            pfc.AddMemoryFont(p, fontdata.Length);
            Marshal.FreeCoTaskMem(p);
        }

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect, object parentWindowUnused = null)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = extensions,
                Title = title,
                InitialDirectory = defaultPath,
                Multiselect = multiselect
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                defaultPath = System.IO.Path.GetDirectoryName(ofd.FileName);

                if (multiselect)
                    return ofd.FileNames;
                else
                    return new[] { ofd.FileName };
            }

            return null;
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, object parentWindowUnused = null)
        {
            var filenames = ShowOpenFileDialog(title, extensions, ref defaultPath, false, parentWindowUnused);

            if (filenames == null || filenames.Length == 0)
                return null;

            return filenames[0];
        }

        public static string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = extensions,
                Title = title,
                InitialDirectory = defaultPath
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                defaultPath = System.IO.Path.GetDirectoryName(sfd.FileName);
                return sfd.FileName;
            }

            return null;
        }

        public static string ShowSaveFileDialog(string title, string extensions)
        {
            string dummy = "";
            return ShowSaveFileDialog(title, extensions, ref dummy);
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = title;

            if (!string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath))
                folderBrowserDialog.SelectedPath = Settings.LastExportFolder;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                defaultPath = folderBrowserDialog.SelectedPath;
                return folderBrowserDialog.SelectedPath;
            }

            return null;
        }

        public static DialogResult MessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icons = MessageBoxIcon.None)
        {
            return System.Windows.Forms.MessageBox.Show(text, title, buttons, icons);
        }

        public static MouseEventArgs ConvertHorizontalMouseWheelMessage(Control ctrl, System.Windows.Forms.Message m)
        {
            // TODO: Test hi-dpi and things like this.
            short x = (short)((m.LParam.ToInt32() >> 0) & 0xffff);
            short y = (short)((m.LParam.ToInt32() >> 16) & 0xffff);
            short delta = (short)((m.WParam.ToInt32() >> 16) & 0xffff);
            var clientPos = ctrl.PointToClient(new Point(x, y));

            return new MouseEventArgs(MouseButtons.None, 1, clientPos.X, clientPos.Y, delta);
        }

        public static bool IsVS2015RuntimeInstalled()
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

        // From : https://stackoverflow.com/questions/318777/c-sharp-how-to-translate-virtual-keycode-to-char
        [DllImport("user32.dll")]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

        static Dictionary<Tuple<IntPtr, uint>, string> KeyCodeStringCache = new Dictionary<Tuple<IntPtr, uint>, string>();

        static readonly byte[] keyStateNull = new byte[256];

        // Needed to get rid of dead keys.
        // https://web.archive.org/web/20101004154432/http://blogs.msdn.com/b/michkap/archive/2006/04/06/569632.aspx
        // https://web.archive.org/web/20100820152419/http://blogs.msdn.com/b/michkap/archive/2007/10/27/5717859.aspx
        private static void ClearKeyboardBuffer(uint vk, uint sc, IntPtr hkl)
        {
            var rc = 0;
            var sb = new StringBuilder(10);
            do
            {
                rc = ToUnicodeEx(vk, sc, keyStateNull, sb, sb.Capacity, 0, hkl);
            }
            while (rc < 0);
        }

        public static string KeyCodeToString(int key)
        {
            var virtualKeyCode = (uint)key;
            var scanCode = MapVirtualKey(virtualKeyCode, 0);
            var inputLocaleIdentifier = GetKeyboardLayout(0);

            var mapKey = new Tuple<IntPtr, uint>(inputLocaleIdentifier, virtualKeyCode);

            if (!KeyCodeStringCache.TryGetValue(mapKey, out var str))
            {
                StringBuilder result = new StringBuilder();
                ToUnicodeEx(virtualKeyCode, scanCode, keyStateNull, result, 5, 0, inputLocaleIdentifier);
                ClearKeyboardBuffer(virtualKeyCode, scanCode, inputLocaleIdentifier);

                // Fall back to Key enum for special keys.
                if (result.Length == 0)
                {
                    str = ((System.Windows.Forms.Keys)key).ToString();
                }
                else
                {
                    str = result.ToString().ToUpper();

                    // Ignore invisible characters.
                    if (str.Length == 1 && str[0] <= 32)
                        str = null;
                }

                KeyCodeStringCache.Add(mapKey, str);
            }

            return str;
        }

        public static Bitmap LoadBitmapFromResource(string name)
        {
            return System.Drawing.Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) as System.Drawing.Bitmap;
        }

        public static float GetDesktopScaling()
        {
            var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            return graphics.DpiX / 96.0f;
        }

        public const bool IsMobile  = false;
        public const bool IsAndroid = false;
        public const bool IsDesktop = true;
        public const bool IsWindows = true;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = false;
    }
}
