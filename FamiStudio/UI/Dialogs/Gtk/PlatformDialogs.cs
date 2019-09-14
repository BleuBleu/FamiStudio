using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public static class PlatformDialogs
    {
        public static void Initialize()
        {
            // When debugging or when in a app package, our paths are a bit different.
            string[] pathsToSearch =
            {
                "../../Resources/",
                "../Resources/Fonts/"
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
                        MacUtils.CoreTextRegisterFont(fullpath);
                    }
                    break;
                }
            }

            Gtk.Application.Init();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FamiStudio.Resources.gtk.rc"))
            using (var reader = new StreamReader(stream))
            {
                string gtkrc = reader.ReadToEnd();
                Gtk.Rc.ParseString(gtkrc);
            }
        }

        private static string[] GetExtensionList(string str)
        {
            var splits = str.Split('|');
            var extensions = new List<string>();

            for (int i = 1; i < splits.Length; i += 2)
            {
                extensions.AddRange(splits[i].Split(new[] { ';', '*', '.' }, StringSplitOptions.RemoveEmptyEntries));
            }

            return extensions.ToArray();
        }

        public static string ShowOpenFileDialog(string title, string extensions)
        {
            return MacUtils.ShowOpenDialog(title, GetExtensionList(extensions));
        }

        public static string ShowSaveFileDialog(string title, string extensions)
        {
            return MacUtils.ShowSaveDialog(title, GetExtensionList(extensions));
        }

        public static DialogResult MessageBox(string text, string title, MessageBoxButtons buttons, MessageBoxIcon icons = MessageBoxIcon.None)
        {
            return MacUtils.ShowAlert(text, title, buttons);
        }
    }
}

