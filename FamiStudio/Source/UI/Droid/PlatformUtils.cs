using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FamiStudio
{
    public static class PlatformUtils
    {
        public static void Initialize()
        {
        }

        public static System.Windows.Forms.DialogResult MessageBox(string text, string title, System.Windows.Forms.MessageBoxButtons buttons, System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.None)
        {
            return System.Windows.Forms.DialogResult.None;
        }

        public static string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            return null;
        }

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect, object parentWindow = null)
        {
            return null;
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, object parentWindow = null)
        {
            return null;
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            return null;
        }

        public static string ApplicationVersion => "9.9.9"; // DROIDTODO

        public static string KeyCodeToString(int keyval)
        {
            return string.Empty;
        }

        public static Android.Graphics.Bitmap LoadBitmapFromResource(string name, bool premultiplied = false)
        {
            return Android.Graphics.BitmapFactory.DecodeStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(name), null,
                new Android.Graphics.BitmapFactory.Options() { InPremultiplied = premultiplied });
        }

        public const bool IsMobile  = true;
        public const bool IsAndroid = true;
        public const bool IsDesktop = false;
        public const bool IsWindows = false;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = false;
    }
}