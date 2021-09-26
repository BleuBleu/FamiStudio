using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public class OpenProjectDialog
    {
        private FamiStudio famistudio;
        private PropertyDialog dialog;

        private Dictionary<int, string> demoProjects = new Dictionary<int, string>();
        private Dictionary<int, string> userProjects = new Dictionary<int, string>();
        private string storageFilename;

        public OpenProjectDialog(FamiStudio fami)
        {
            famistudio = fami;

            dialog = new PropertyDialog("Open FamiStudio Project", 100);
            dialog.SetVerb("Open");

            // Demo songs.
            var assembly = Assembly.GetExecutingAssembly();
            var files = assembly.GetManifestResourceNames();
            var first = true;

            foreach (var file in files)
            {
                if (file.ToLowerInvariant().EndsWith(".fms"))
                {
                    // Filename will be in the for 'FamiStudio.Ducktales.fms'.
                    var trimmedFilename = Path.GetFileNameWithoutExtension(file.Substring(file.IndexOf('.') + 1));
                    var idx = dialog.Properties.AddRadioButton("Demo Projects", trimmedFilename, first, first);
                    demoProjects.Add(idx, file);
                    first = false;
                }
            }

            // User files.
            // TODO
            for (int i = 0; i < 3; i++)
            {
                var idx = dialog.Properties.AddRadioButton("Your Projects", $"Allo{i}", false, i == 0);
                userProjects.Add(idx, $"Allo{i}");
            }

            // Import
            dialog.Properties.AddButton("Open project from storage", "Open");

            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
            dialog.Properties.PropertyClicked += Properties_PropertyClicked;

            dialog.Properties.Build();
        }

        private void Properties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (demoProjects.ContainsKey(propIdx))
            {
                foreach (var kv in userProjects)
                {
                    props.ClearRadioGroup(kv.Key);
                    break;
                }
            }
            else if (userProjects.ContainsKey(propIdx))
            {
                foreach (var kv in demoProjects)
                {
                    props.ClearRadioGroup(kv.Key);
                    break;
                }
            }
        }

        private async void Properties_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            var result = await Xamarin.Essentials.FilePicker.PickAsync();

            if (result != null)
            {
                storageFilename = result.FullPath;
                dialog.CloseWithResult(DialogResult.OK);
            }
        }

        public void ShowDialog(FamiStudioForm parent)
        {
            dialog.ShowDialog(parent, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    if (storageFilename != null)
                    {
                        famistudio.OpenProject(storageFilename);
                        famistudio.Project.Filename = null; // Wipe filename so it asks when saving.
                    }

                    foreach (var kv in demoProjects)
                    {
                        if (dialog.Properties.GetPropertyValue<bool>(kv.Key))
                        {
                            var filename = Path.Combine(Path.GetTempPath(), "Temp.fms");

                            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(kv.Value))
                            {
                                var buffer = new byte[(int)s.Length];
                                s.Read(buffer, 0, (int)s.Length);
                                File.WriteAllBytes(filename, buffer);
                            }

                            famistudio.OpenProject(filename);
                            famistudio.Project.Filename = null; // Wipe filename for demo songs.

                            return;
                        }
                    }

                    foreach (var kv in userProjects)
                    {
                        if (dialog.Properties.GetPropertyValue<bool>(kv.Key))
                        {
                            return;
                        }
                    }
                }
            });
        }
    }
}

