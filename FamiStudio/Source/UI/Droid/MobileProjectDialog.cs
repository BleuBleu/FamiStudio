using Android.App;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace FamiStudio
{
    public class MobileProjectDialog
    {
        private FamiStudio famistudio;
        private PropertyDialog dialog;
        private bool saveMode;
        private bool inPropertyChange = false;

        private List<string> demoProjects = new List<string>();
        private List<string> userProjects = new List<string>();
        private string storageFilename;

        private readonly string UserProjectSaveTooltip = "These are your projects. Select one to overwrite it. The save to a new project file, select the last option and enter a name.";
        private readonly string UserProjectLoadTooltip = "These are your projects. Select one to load it.";
        private readonly string DemoProjectLoadTooltip = "These are demo projects provided with FamiStudio. They are great resources for learning.";
        private readonly string StorageTooltip         = "This will prompt you to open a file from your device's storage. You can open FamiStudio .FMS files, as well as other file formats such as FamiTracker FTM or TXT files.";

        public MobileProjectDialog(FamiStudio fami, string title, bool save, bool allowStorage = true)
        {
            famistudio = fami;
            saveMode = save;

            dialog = new PropertyDialog(title, 100);
            dialog.SetVerb(save ? "Save" : "Open");

            // User files.
            var userProjectsDir = Platform.UserProjectsDirectory;
            Directory.CreateDirectory(userProjectsDir);

            userProjects.AddRange(Directory.GetFiles(userProjectsDir, "*.fms"));
            for (int i = 0; i < userProjects.Count; i++)
                userProjects[i] = Path.GetFileNameWithoutExtension(userProjects[i]);

            var hasUserProjects = userProjects != null && userProjects.Count > 0;
            var newProjectName = (string)null;

            if (save)
            {
                userProjects.Add("Save to a New Project.");

                // Generate unique name
                for (int i = 1; ; i++)
                {
                    newProjectName = $"NewProject{i}";
                    if (userProjects.Find(p => p.ToLower() == newProjectName.ToLower()) == null)
                        break;
                }
            }
            else
            {
                // Demo songs.
                var assembly = Assembly.GetExecutingAssembly();
                var files = assembly.GetManifestResourceNames();

                foreach (var file in files)
                {
                    if (file.ToLowerInvariant().EndsWith(".fms"))
                    {
                        // Filename will be in the for 'FamiStudio.Ducktales.fms'.
                        var trimmedFilename = Path.GetFileNameWithoutExtension(file.Substring(file.IndexOf('.') + 1));
                        demoProjects.Add(trimmedFilename);
                    }
                }
            }

            dialog.Properties.AddRadioButtonList("User Projects", userProjects.ToArray(), userProjects.Count - 1, save ? UserProjectSaveTooltip : UserProjectLoadTooltip); // 0

            if (save)
            {
                dialog.Properties.AddTextBox("New Project Name", newProjectName, 0, "Enter the name of the new project."); // 1
                dialog.Properties.AddButton("Delete Selected Project", "Delete"); // 2
                dialog.Properties.SetPropertyEnabled(1, true);
                dialog.Properties.SetPropertyEnabled(2, false);
                dialog.Properties.AddLabel("Note", "To export your project to your device's storage or to share them (email, messaging, etc.), please use the 'Share' option from the Export dialog."); // 3
            }
            else
            {
                dialog.Properties.AddRadioButtonList("Demo Projects", demoProjects.ToArray(), hasUserProjects ? -1 : 0, DemoProjectLoadTooltip); // 1
                if (allowStorage)
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
                    var newFile = (int)value == userProjects.Count - 1;
                    dialog.Properties.SetPropertyEnabled(1,  newFile);
                    dialog.Properties.SetPropertyEnabled(2, !newFile);
                }
            }
            else
            {
                if (inPropertyChange)
                    return;

                inPropertyChange = true;

                if (propIdx == 0)
                    props.ClearRadioList(1);
                else if (propIdx == 1 && userProjects != null && userProjects.Count > 0)
                    props.ClearRadioList(0);

                inPropertyChange = false;
            }
        }

        private void Properties_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (saveMode)
            {
                var idx = dialog.Properties.GetSelectedIndex(0);
                if (idx >= 0 && idx < userProjects.Count - 1)
                {
                    Platform.MessageBoxAsync("Delete project?", "Delete", MessageBoxButtons.YesNo, (r) =>
                    {
                        if (r == DialogResult.Yes)
                        {
                            File.Delete(GetUserProjectFilename(userProjects[idx]));
                            userProjects.RemoveAt(idx);
                            props.UpdateRadioButtonList(0, userProjects.ToArray(), userProjects.Count - 1);
                            props.SetPropertyEnabled(1, true);
                            props.SetPropertyEnabled(2, false);
                            Platform.ShowToast("Project Deleted!");
                        }
                    });
                }
            }
            else
            {
                // HACK : We dont support nested activities right now, so return
                // this special code to signal that we should open from storage.
                storageFilename = "///STORAGE///";
                dialog.CloseWithResult(DialogResult.OK);
            }
        }

        private string GetUserProjectFilename(string name)
        {
            return Path.Combine(Platform.UserProjectsDirectory, $"{name}.fms");
        }

        public void ShowDialogAsync(Action<string> callback)
        {
            dialog.ShowDialogAsync(famistudio.MainWindow, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    if (saveMode)
                    {
                        var userProjectIdx = dialog.Properties.GetSelectedIndex(0);
                        var filename = "";

                        // New file requested.
                        if (userProjectIdx == userProjects.Count - 1)
                            filename = dialog.Properties.GetPropertyValue<string>(1);
                        else
                            filename = userProjects[userProjectIdx];

                        if (!string.IsNullOrEmpty(filename))
                        {
                            filename = Path.Combine(Platform.UserProjectsDirectory, $"{filename}.fms");
                            callback(filename);
                        }
                    }
                    else
                    {
                        if (storageFilename != null)
                        {
                            callback(storageFilename);
                            return;
                        }

                        var userProjectIdx = dialog.Properties.GetSelectedIndex(0);
                        if (userProjectIdx >= 0)
                        {
                            var filename = GetUserProjectFilename(userProjects[userProjectIdx]);
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
                            File.Delete(tempFilename);
                            return;
                        }
                    }
                }
            });
        }
    }
}

