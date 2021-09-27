using Android.App;
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

        private string[] demoProjects;
        private string[] userProjects;
        private string storageFilename;

        public OpenProjectDialog(FamiStudio fami)
        {
            famistudio = fami;

            dialog = new PropertyDialog("Open FamiStudio Project", 100);
            dialog.SetVerb("Open");

            // Demo songs.
            var assembly = Assembly.GetExecutingAssembly();
            var files = assembly.GetManifestResourceNames();
            var demoProjectsList = new List<string>();

            foreach (var file in files)
            {
                if (file.ToLowerInvariant().EndsWith(".fms"))
                {
                    // Filename will be in the for 'FamiStudio.Ducktales.fms'.
                    var trimmedFilename = Path.GetFileNameWithoutExtension(file.Substring(file.IndexOf('.') + 1));
                    demoProjectsList.Add(trimmedFilename);
                }
            }

            demoProjects = demoProjectsList.ToArray();

            // User files.
            var userProjectsDir = Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects");
            if (Directory.Exists(userProjectsDir))
            {
                userProjects = Directory.GetFiles(userProjectsDir, "*.fms");

                for (int i = 0; i < userProjects.Length; i++)
                    userProjects[i] = Path.GetFileNameWithoutExtension(userProjects[i]);
            }

            if (userProjects == null || userProjects.Length == 0)
                dialog.Properties.AddLabel("User Projects", "No user projects found!", false, "These are your projects.");
            else
                dialog.Properties.AddRadioButtonList("User Projects", userProjects, 0, "These are your projects.");
            dialog.Properties.AddRadioButtonList("Demo Projects", demoProjects, 0, "These are demo projects provided with FamiStudio. They are great resource for learning.");
            dialog.Properties.AddButton("Open project from storage", "Open From Storage", "This will prompt you to open a file from your device's storage. You can open FamiStudio .FMS files, as well as other file formats such as FamiTracker FTM or TXT files.");
            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
            dialog.Properties.PropertyClicked += Properties_PropertyClicked;

            dialog.Properties.Build();
        }

        private void Properties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
                props.ClearRadioList(1);
            else if (propIdx == 1 && userProjects != null && userProjects.Length > 0)
                props.ClearRadioList(0);
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
                        return;
                    }

                    var userProjectIdx = dialog.Properties.GetSelectedIndex(0);
                    if (userProjectIdx >= 0)
                    {
                        var userProjectsDir = Path.Combine(Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects"), $"{userProjects[userProjectIdx]}.fms");
                        famistudio.OpenProject(userProjectsDir);
                        return;
                    }

                    var demoProjectIdx = dialog.Properties.GetSelectedIndex(1);
                    if (demoProjectIdx >= 0)
                    {
                        // Save to temporary file.
                        var filename = Path.Combine(Path.GetTempPath(), "Temp.fms");

                        using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.{demoProjects[demoProjectIdx]}.fms"))
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
            });
        }
    }
}

