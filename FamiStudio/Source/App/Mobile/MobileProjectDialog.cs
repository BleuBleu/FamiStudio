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

        #region Localization

        private LocalizedString UserProjectsSaveTooltip;
        private LocalizedString UserProjectsLoadTooltip;
        private LocalizedString DemoProjectsLoadTooltip;
        private LocalizedString OpenFromStorageTooltip;

        private LocalizedString UserProjectsLabel;
        private LocalizedString NewProjectNameLabel;
        private LocalizedString NewProjectNameTooltip;
        private LocalizedString DeleteSelectedProjectLabel;
        private LocalizedString DeleteButton;
        private LocalizedString StorageNoteLabel;
        private LocalizedString StorageNoteText;
        private LocalizedString DemoProjectLoadLabel;
        private LocalizedString OpenFromStorageLabel;
        private LocalizedString OpenFromStorageButton;
        private LocalizedString EnterValidFilenameToast;
        private LocalizedString OverwriteProjectText;
        private LocalizedString OverwriteProjectTitle;
        private LocalizedString DeleteProjectText;
        private LocalizedString DeleteProjectTitle;
        private LocalizedString ProjectDeletedToast;

        private LocalizedString SaveToNewProjectRadio;
        private LocalizedString NewProjectPrefix;

        #endregion

        public MobileProjectDialog(FamiStudio fami, string title, bool save, bool allowStorage = true)
        {
            Localization.Localize(this);

            famistudio = fami;
            saveMode = save;

            dialog = new PropertyDialog(famistudio.Window, title, 100);

            if (save)
                dialog.ValidateProperties += Dialog_ValidateProperties;

            // User files.
            var userProjectsDir = Platform.UserProjectsDirectory;
            
            if (Platform.IsAndroid)
            {
                Directory.CreateDirectory(userProjectsDir);
            }

            userProjects.AddRange(Directory.GetFiles(userProjectsDir, "*.fms"));
            for (int i = 0; i < userProjects.Count; i++)
                userProjects[i] = Path.GetFileNameWithoutExtension(userProjects[i]);

            var hasUserProjects = userProjects != null && userProjects.Count > 0;
            var newProjectName = (string)null;

            if (save)
            {
                userProjects.Add(SaveToNewProjectRadio);

                // Generate unique name
                for (int i = 1; ; i++)
                {
                    newProjectName = $"{NewProjectPrefix}{i}";
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
                        // Filename will be in the for 'FamiStudio.DemoSongs.Ducktales.fms'.
                        demoProjects.Add(file.Split('.')[2]);
                    }
                }
            }

            dialog.Properties.AddRadioButtonList(UserProjectsLabel, userProjects.ToArray(), userProjects.Count - 1, save ? UserProjectsSaveTooltip : UserProjectsLoadTooltip); // 0

            if (save)
            {
                dialog.Properties.AddTextBox(NewProjectNameLabel, newProjectName, 0, false, NewProjectNameTooltip); // 1
                dialog.Properties.AddButton(DeleteSelectedProjectLabel, DeleteButton); // 2
                dialog.Properties.SetPropertyEnabled(1, true);
                dialog.Properties.SetPropertyEnabled(2, false);
                dialog.Properties.AddLabel(StorageNoteLabel, StorageNoteText); // 3
            }
            else
            {
                dialog.Properties.AddRadioButtonList(DemoProjectLoadLabel, demoProjects.ToArray(), hasUserProjects ? -1 : 0, DemoProjectsLoadTooltip); // 1
                if (allowStorage)
                    dialog.Properties.AddButton(OpenFromStorageLabel, OpenFromStorageButton, OpenFromStorageTooltip); // 2
            }

            dialog.Properties.PropertyClicked += Properties_PropertyClicked;
            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
            dialog.Properties.Build();
        }

        private bool Dialog_ValidateProperties(PropertyPage props)
        {
            var idx = dialog.Properties.GetSelectedIndex(0);
            var newFile = idx == userProjects.Count - 1;
            var filename = dialog.Properties.GetPropertyValue<string>(1);

            // In case user types the exact same name as exsiting project.
            if (newFile && File.Exists(GetUserProjectFilename(filename)))
            {
                newFile = false;
            }

            if (newFile && string.IsNullOrEmpty(filename))
            {
                Platform.ShowToast(famistudio.Window, EnterValidFilenameToast);
                return false;
            }
            else if (!newFile)
            {
                Platform.MessageBoxAsync(famistudio.Window, OverwriteProjectText, OverwriteProjectTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                        dialog.Close(DialogResult.OK);
                });
                return false;
            }

            return true;
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
                    Platform.MessageBoxAsync(famistudio.Window, DeleteProjectText, DeleteProjectTitle, MessageBoxButtons.YesNo, (r) =>
                    {
                        if (r == DialogResult.Yes)
                        {
                            File.Delete(GetUserProjectFilename(userProjects[idx]));
                            userProjects.RemoveAt(idx);
                            props.UpdateRadioButtonList(0, userProjects.ToArray(), userProjects.Count - 1);
                            props.SetPropertyEnabled(1, true);
                            props.SetPropertyEnabled(2, false);
                            Platform.ShowToast(famistudio.Window, ProjectDeletedToast);
                        }
                    });
                }
            }
            else
            {
                // HACK : We dont support nested activities right now, so return
                // this special code to signal that we should open from storage.
                storageFilename = "///STORAGE///";
                dialog.Close(DialogResult.OK); 
            }
        }

        private string GetUserProjectFilename(string name)
        {
            return Path.Combine(Platform.UserProjectsDirectory, $"{name}.fms");
        }

        public void ShowDialogAsync(Action<string> callback)
        {
            dialog.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    if (saveMode)
                    {
                        var userProjectIdx = dialog.Properties.GetSelectedIndex(0);
                        var newFile = userProjectIdx == userProjects.Count - 1;
                        var filename = newFile ? dialog.Properties.GetPropertyValue<string>(1) : userProjects[userProjectIdx];

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
                            
                            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.DemoSongs.{demoProjects[demoProjectIdx]}.fms"))
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

