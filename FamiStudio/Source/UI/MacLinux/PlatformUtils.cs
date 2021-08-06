using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Gtk;
using OpenTK;

namespace FamiStudio
{
    public static class PlatformUtils
    {
#if FAMISTUDIO_LINUX
        [System.Runtime.InteropServices.DllImport("fontconfig.so")]
        static extern bool FcConfigAppFontAddFile(System.IntPtr config, string fontPath);
        [System.Runtime.InteropServices.DllImport("fontconfig.so.1", EntryPoint = "FcConfigAppFontAddFile")]
        static extern bool FcConfigAppFontAddFile1(System.IntPtr config, string fontPath);
#endif

        public static void Initialize()
        {
            // When debugging or when in a app package, our paths are a bit different.
            string[] pathsToSearch =
            {
                "./Resources/",
                "../../Resources/",
                "../Resources/Fonts/",
                "."
            };

            string[] fontsToLoad =
            {
                "Quicksand-Regular.ttf",
                "Quicksand-Bold.ttf"
            };

            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var path in pathsToSearch)
            {
                var absPath = Path.Combine(appPath, path);

                if (File.Exists(Path.Combine(absPath, fontsToLoad[0])))
                {
                    foreach (var font in fontsToLoad)
                    {
                        var fullpath = Path.Combine(absPath, font);
#if FAMISTUDIO_MACOS
                        MacUtils.CoreTextRegisterFont(fullpath);
#else
                        try
                        {
                            FcConfigAppFontAddFile(IntPtr.Zero, fullpath);
                        }
                        catch
                        {
                            try { FcConfigAppFontAddFile1(IntPtr.Zero, fullpath); } catch { }
                        }
#endif
                    }
                    break;
                }
            }

#if FAMISTUDIO_LINUX
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferX11,
                EnableHighResolution = false
            });
#endif

            InitializeGtk();
        }

        private static void ParseRcFileFromResource(string rcFile)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(rcFile))
            using (var reader = new StreamReader(stream))
            {
                string gtkrc = reader.ReadToEnd();
                Gtk.Rc.ParseString(gtkrc);
            }
        }

        public static void InitializeGtk()
        {
            Gtk.Application.Init();

#if FAMISTUDIO_MACOS
            var rcFile = "FamiStudio.Resources.gtk_mac.rc";
#else
            var rcFile = "FamiStudio.Resources.gtk_linux.rc";
#endif

            ParseRcFileFromResource(rcFile);

#if FAMISTUDIO_LINUX
            var dpi = Gdk.Display.Default.DefaultScreen.Resolution;

            if (dpi < 0)
            {
                dpi = Utils.Clamp(Gdk.Display.Default.DefaultScreen.Width / (Gdk.Display.Default.DefaultScreen.WidthMm / 25.4f), 96, 384);
                Gdk.Display.Default.DefaultScreen.Resolution = dpi;
            }

            if (dpi >= 192.0)
                ParseRcFileFromResource("FamiStudio.Resources.gtk_linux_hidpi.rc");
#endif
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

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect, Window parentWindow = null)
        {
            var extensionList = GetExtensionList(extensions);

#if FAMISTUDIO_MACOS
            var filenames = MacUtils.ShowOpenDialog(title, extensionList, multiselect, defaultPath);
            if (filenames != null && !string.IsNullOrEmpty(filenames[0]))
                defaultPath = Path.GetDirectoryName(filenames[0]);
            return filenames;
#else
            Gtk.Rc.ResetStyles(Gtk.Settings.GetForScreen(Gdk.Screen.Default));
            Gtk.Rc.ReparseAll();

            Gtk.FileChooserDialog filechooser =
                new Gtk.FileChooserDialog(title,
                    null,
                    FileChooserAction.Open,
                    "Cancel", ResponseType.Cancel,
                    "Open", ResponseType.Accept);

            filechooser.Modal = true;
            filechooser.SkipTaskbarHint = true;
            filechooser.TransientFor = parentWindow != null ? parentWindow : FamiStudioForm.Instance;
            filechooser.SetCurrentFolder(defaultPath);
            filechooser.SelectMultiple = multiselect;

            if (extensionList.Length > 0)
            {
                filechooser.Filter = new FileFilter();
                foreach (var ext in extensionList)
                    filechooser.Filter.AddPattern($"*.{ext}");
            }

            string[] filenames = null;
            if (filechooser.Run() == (int)ResponseType.Accept)
            {
                if (multiselect)
                {
                    filenames = filechooser.Filenames;
                    if (filenames.Length > 0)
                        defaultPath = Path.GetDirectoryName(filenames[0]);
                }
                else
                {
                    var filename = filechooser.Filename;
                    if (!string.IsNullOrEmpty(filename))
                    {
                        defaultPath = Path.GetDirectoryName(filename);
                        filenames = new[] { filename };
                    }
                }
            }

            filechooser.Destroy();

            return filenames;
#endif
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, Window parentWindow = null)
        {
            var filenames = ShowOpenFileDialog(title, extensions, ref defaultPath, false, parentWindow);

            if (filenames == null || filenames.Length == 0)
                return null;

            return filenames[0];
        }

        public static string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            var extensionList = GetExtensionList(extensions);
#if FAMISTUDIO_MACOS
            var filename = MacUtils.ShowSaveDialog(title, extensionList, defaultPath);
            if (!string.IsNullOrEmpty(filename))
                defaultPath = Path.GetDirectoryName(filename);
            return filename;
#else
            Gtk.FileChooserDialog filechooser =
                new Gtk.FileChooserDialog(title,
                    null,
                    FileChooserAction.Save,
                    "Cancel", ResponseType.Cancel,
                    "Save", ResponseType.Accept);

            filechooser.Modal = true;
            filechooser.SkipTaskbarHint = true;
            filechooser.TransientFor = FamiStudioForm.Instance;
            filechooser.SetCurrentFolder(defaultPath);

            filechooser.Filter = new FileFilter();
            foreach (var ext in extensionList)
                filechooser.Filter.AddPattern($"*.{ext}");

            string filename = null;
            if (filechooser.Run() == (int)ResponseType.Accept)
            {
                filename = filechooser.Filename;

                // GTK file chooser does not add the extension automatically.
                var extension = Path.GetExtension(filename).ToLower();
                var desiredExtension = $".{extensionList[0]}";

                if (extension != desiredExtension)
                    filename = Path.ChangeExtension(filename, desiredExtension);

                defaultPath = Path.GetDirectoryName(filename);
            }

            filechooser.Destroy();

            return filename;
#endif
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
#if FAMISTUDIO_MACOS
            var filename = MacUtils.ShowBrowseFolderDialog(title, defaultPath);
            if (!string.IsNullOrEmpty(filename))
            {
                if (Directory.Exists(filename))
                    defaultPath = filename;
                else
                    defaultPath = Path.GetDirectoryName(filename);
                return defaultPath;
            }
            return null;
#else
            Gtk.FileChooserDialog filechooser =
                new Gtk.FileChooserDialog("Choose the file to save",
                    null,
                    FileChooserAction.Save,
                    "Cancel", ResponseType.Cancel,
                    "Save", ResponseType.Accept);

            filechooser.Modal = true;
            filechooser.Action = FileChooserAction.SelectFolder;
            filechooser.SkipTaskbarHint = true;
            filechooser.TransientFor = FamiStudioForm.Instance;
            filechooser.SetCurrentFolder(defaultPath);

            string filename = null;
            if (filechooser.Run() == (int)ResponseType.Accept)
            {
                filename = filechooser.Filename;
                defaultPath = Path.GetDirectoryName(filename);
            }

            filechooser.Destroy();

            return filename;
#endif
        }

        public static string ShowSaveFileDialog(string title, string extensions)
        {
            string dummy = "";
            return ShowSaveFileDialog(title, extensions, ref dummy);
        }

        public static DialogResult MessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
#if FAMISTUDIO_MACOS
            return MacUtils.ShowAlert(text, title, buttons);
#else
            if (buttons == MessageBoxButtons.YesNoCancel)
            {
                buttons = MessageBoxButtons.YesNo;
                text += " (Close or ESC to cancel)";
            }

            MessageDialog md = new MessageDialog(null, 
                DialogFlags.Modal | DialogFlags.DestroyWithParent, 
                icon == MessageBoxIcon.Error ? MessageType.Error : MessageType.Info,
                buttons == MessageBoxButtons.YesNo ? ButtonsType.YesNo : ButtonsType.Ok, text);

            md.Modal = true;
            md.SkipTaskbarHint = true;
            md.TypeHint = Gdk.WindowTypeHint.Dialog;
            md.Title = title;
            md.TransientFor = FamiStudioForm.Instance;

            int ret = md.Run();

            md.Destroy();

            if (buttons == MessageBoxButtons.YesNo)
                return ret == -8 ? DialogResult.Yes : ret == -9 ? DialogResult.No : DialogResult.Cancel;
            else
                return DialogResult.OK;
#endif
        }

        public static string KeyCodeToString(int keyval)
        {
            var str = char.ConvertFromUtf32((int)Gdk.Keyval.ToUnicode((uint)keyval));

            // Fallback to key enum for special keys.
            if (str.Length == 0 || (str.Length == 1 && str[0] <= 32))
            {
                return ((Gdk.Key)keyval).ToString();
            }

            return str.ToUpper();
        }
    }
}

