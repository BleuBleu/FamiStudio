using Android.App;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace FamiStudio
{
    public class MobileProjectDialog
    {
        private FamiStudio famistudio;
        private PropertyDialog dialog;
        private bool saveMode;
        private bool inPropertyChange = false;

        private string[] demoProjects;
        private string[] userProjects;
        private string storageFilename;

        private readonly string UserProjectSaveTooltip = "These are your projects. Select one to overwrite it. The save to a new project file, select the last option and enter a name.";
        private readonly string UserProjectLoadTooltip = "These are your projects. Select one to load it.";
        private readonly string DemoProjectLoadTooltip = "These are demo projects provided with FamiStudio. They are great resource for learning.";
        private readonly string StorageTooltip         = "This will prompt you to open a file from your device's storage. You can open FamiStudio .FMS files, as well as other file formats such as FamiTracker FTM or TXT files.";

        public MobileProjectDialog(FamiStudio fami, bool save)
        {
            famistudio = fami;
            saveMode = save;

            var verb = save ? "Save" : "Open";

            dialog = new PropertyDialog($"{verb} FamiStudio Project", 100);
            dialog.SetVerb(verb);

            // User files.
            var userProjectsDir = Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects");
            if (Directory.Exists(userProjectsDir))
            {
                userProjects = Directory.GetFiles(userProjectsDir, "*.fms");

                for (int i = 0; i < userProjects.Length; i++)
                    userProjects[i] = Path.GetFileNameWithoutExtension(userProjects[i]);
            }

            var hasUserProjects = userProjects != null && userProjects.Length > 0;

            if (save)
            {
                if (userProjects == null)
                    userProjects = new string[1];
                else
                    Array.Resize(ref userProjects, userProjects.Length + 1);

                userProjects[userProjects.Length - 1] = "Save to a New Project.";
            }
            else
            {
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
            }

            dialog.Properties.AddRadioButtonList("User Projects", userProjects, 0, save ? UserProjectSaveTooltip : UserProjectLoadTooltip); // 0

            if (save)
            {
                dialog.Properties.AddTextBox("New Project Name", "NewProject", 0, "Enter the name of the new project."); // 1
                dialog.Properties.SetPropertyEnabled(1, userProjects.Length == 1);
            }
            else
            {
                dialog.Properties.AddRadioButtonList("Demo Projects", demoProjects, hasUserProjects ? -1 : 0, DemoProjectLoadTooltip); // 1
                dialog.Properties.AddButton("Open project from storage", "Open From Storage", StorageTooltip); // 2
            }

            dialog.Properties.PropertyClicked += Properties_PropertyClicked;
            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
            dialog.Properties.Build();
        }

        private void Properties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (saveMode)
            {
                if (propIdx == 0)
                {
                    var newFile = (int)value == userProjects.Length - 1;
                    dialog.Properties.SetPropertyEnabled(1, newFile);
                }
            }
            else
            {
                if (inPropertyChange)
                    return;

                inPropertyChange = true;

                if (propIdx == 0)
                    props.ClearRadioList(1);
                else if (propIdx == 1 && userProjects != null && userProjects.Length > 0)
                    props.ClearRadioList(0);

                inPropertyChange = false;
            }
        }

        private async void Properties_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (saveMode)
            {
            }
            else
            {
                var result = await Xamarin.Essentials.FilePicker.PickAsync();

                if (result != null)
                {
                    storageFilename = result.FullPath;
                    dialog.CloseWithResult(DialogResult.OK);
                }
            }
        }

        public void ShowDialogAsync(FamiStudioForm parent, Action<string> callback)
        {
            dialog.ShowDialog(parent, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    if (saveMode)
                    {
                        var userProjectIdx = dialog.Properties.GetSelectedIndex(0);
                        var filename = "";

                        // New file requested.
                        if (userProjectIdx == userProjects.Length - 1)
                            filename = dialog.Properties.GetPropertyValue<string>(1);
                        else
                            filename = userProjects[userProjectIdx];

                        if (!string.IsNullOrEmpty(filename))
                        {
                            filename = Path.Combine(Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects"), $"{filename}.fms");
                            callback(filename);
                        }
                    }
                    else
                    {
                        if (storageFilename != null)
                        {
                            callback(storageFilename);
                            famistudio.Project.Filename = null; // Wipe filename so it asks when saving.
                            return;
                        }

                        var userProjectIdx = dialog.Properties.GetSelectedIndex(0);
                        if (userProjectIdx >= 0)
                        {
                            var filename = Path.Combine(Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects"), $"{userProjects[userProjectIdx]}.fms");
                            callback(filename);
                            return;
                        }

                        var demoProjectIdx = dialog.Properties.GetSelectedIndex(1);
                        if (demoProjectIdx >= 0)
                        {
                            // Save to temporary file.
                            var tempFilename = Path.Combine(Path.GetTempPath(), "Temp.fms");

                            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.{demoProjects[demoProjectIdx]}.fms"))
                            {
                                var buffer = new byte[(int)s.Length];
                                s.Read(buffer, 0, (int)s.Length);
                                File.WriteAllBytes(tempFilename, buffer);
                            }

                            callback(tempFilename);
                            famistudio.Project.Filename = null; // Wipe filename for demo songs.
                            File.Delete(tempFilename);
                            return;
                        }
                    }
                }
            });
        }
    }
}

