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

namespace FamiStudio
{
    public static class PlatformUtils
    {
        public static PrivateFontCollection PrivateFontCollection;

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

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, object parentWindowUnused = null)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = extensions,
                Title = title,
                InitialDirectory = defaultPath
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                defaultPath = System.IO.Path.GetDirectoryName(ofd.FileName);
                return ofd.FileName;
            }

            return null;
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
                NesApu.GetAudioExpansion(0);
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

        public static string KeyCodeToString(Keys key)
        {
            byte[] keyboardState = new byte[255];
            bool keyboardStateStatus = GetKeyboardState(keyboardState);

            if (!keyboardStateStatus)
            {
                return "";
            }

            uint virtualKeyCode = (uint)key;
            uint scanCode = MapVirtualKey(virtualKeyCode, 0);
            IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);

            StringBuilder result = new StringBuilder();
            ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);

            if (result.Length == 0)
            {
                return key.ToString();
            }

            var str = result.ToString().ToUpper();

            // Ignore invisible characters.
            if (str.Length == 0 || (str.Length == 1 && str[0] <= 32))
            {
                return null;
            }

            return str;
        }
    }
}
