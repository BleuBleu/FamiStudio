using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class FileDialog : Dialog
    {
        public enum Mode
        {
            Open,
            Save,
            Folder
        }

        private enum EntryType
        {
            Drive,
            Directory,
            File
        }

        private class FileEntry
        {
            public FileEntry(string img, string name, string path, EntryType t, DateTime date = default(DateTime))
            {
                Type = t;
                Image = img;
                Name = name;
                Path = path;
                Date = date;
            }

            public EntryType Type;
            public string Image;
            public string Name;
            public string Path;
            public DateTime Date;
        };

        private int margin          = DpiScaling.ScaleForWindow(8);
        private int pathButtonSizeY = DpiScaling.ScaleForWindow(24);
        private int buttonSize      = DpiScaling.ScaleForWindow(36);

        private Button       buttonComputer;
        private Grid         gridFiles;
        private TextBox      textFile;
        private DropDown     dropDownType;
        private Button       buttonYes;
        private Button       buttonNo;

        private List<string> buttonPathPaths = new List<string>();
        private List<Button> buttonsPath     = new List<Button>(); 

        private bool sortByDate;
        private bool sortDesc;

        private Mode mode;
        private string filename;
        private string path;
        private string[] extensions;
        private List<FileEntry> files = new List<FileEntry>();

        public string SelectedPath => filename;

        #region Localization

        // Dialogs
        LocalizedString OverwriteFileTitle;
        LocalizedString OverwriteFileMessage;
        LocalizedString FileNoAccessTitle;
        LocalizedString FileNoAccessMessage;
        LocalizedString FolderNoAccessTitle;
        LocalizedString FolderNoAccessMessage;

        // Directories
        LocalizedString ComputerLabel;
        LocalizedString HomeFolderLabel;
        LocalizedString DesktopFolderLabel;
        LocalizedString DocumentsFolderLabel;
        LocalizedString DownloadsFolderLabel;

        // Buttons
        LocalizedString NameLabel;
        LocalizedString DateLabel;
        LocalizedString AcceptLabel;
        LocalizedString CancelLabel;
        LocalizedString AscendLabel;
        LocalizedString DescendLabel;

        #endregion

        public FileDialog(FamiStudioWindow win, Mode m, string title, string defaultPath, string extensionList = "") : base(win, title)
        {
            Localization.Localize(this);

            mode = m;
            SplitExtensionList(extensionList, out extensions, out var descriptions);
            Resize(DpiScaling.ScaleForWindow(800), DpiScaling.ScaleForWindow(500));
            CreateControls(descriptions);

            if (Directory.Exists(defaultPath))
            {
                GoToPath(defaultPath);

                if (mode == Mode.Save)
                    textFile.GrabDialogFocus();
            }
            else
            {
                GoToComputer();
            }
        }

        private void SplitExtensionList(string list, out string[] extensions, out string[] descriptions)
        {
            if (!string.IsNullOrEmpty(list))
            {
                var splits = list.Split('|');

                extensions   = new string[splits.Length / 2];
                descriptions = new string[splits.Length / 2];

                for (int i = 0; i < splits.Length; i += 2)
                {
                    extensions[i / 2] = splits[i + 1];
                    descriptions[i / 2] = splits[i + 0];
                }
            }
            else
            {
                extensions   = new[] { "" };
                descriptions = new[] { "" };
            }
        }

        private void CreateControls(string[] descriptions)
        {
            var widthNoMargin = width - margin * 2;
            var y = titleBarSizeY + margin;

            buttonComputer = new Button("FileComputer", ComputerLabel);
            buttonComputer.Move(margin, y, 100, pathButtonSizeY); 
            buttonComputer.Click += ButtonComputer_Click;
            y += buttonComputer.Height + margin;

            gridFiles = new Grid(new[] {
                new ColumnDesc("",     0.0f, ColumnType.Image),
                new ColumnDesc(NameLabel, 0.7f, ColumnType.Label) { Ellipsis = true },
                new ColumnDesc(DateLabel, 0.3f, ColumnType.Label) }, 16, true);
            gridFiles.Move(margin, y, widthNoMargin, gridFiles.Height);
            gridFiles.FullRowSelect = true;
            gridFiles.IsFileDialog  = true;
            gridFiles.CellClicked += GridFiles_CellClicked;
            gridFiles.EmptyCellClicked += GridFiles_EmptyCellClicked;
            gridFiles.CellDoubleClicked += GridFiles_CellDoubleClicked;
            gridFiles.HeaderCellClicked += GridFiles_HeaderCellClicked;
            gridFiles.SelectedRowUpdated += GridFiles_SelectedRowUpdated;
            y += gridFiles.Height + margin;

            textFile = new TextBox("");
            textFile.Move(margin, y, widthNoMargin / 3, textFile.Height);

            dropDownType = new DropDown( descriptions, 0);
            dropDownType.Move(margin * 2 + textFile.Width, y, widthNoMargin - margin - textFile.Width, textFile.Height);
            dropDownType.SelectedIndexChanged += DropDownType_SelectedIndexChanged;
            dropDownType.Enabled = mode != Mode.Folder;
            y += textFile.Height + margin;

            buttonYes = new Button("Yes", null);
            buttonYes.Click += ButtonYes_Click;
            buttonYes.Resize(buttonSize, buttonSize);
            buttonYes.Move(Width - buttonSize * 2 - margin * 2, y);
            buttonYes.ToolTip = AcceptLabel;

            buttonNo = new Button("No", null); 
            buttonNo.Click += ButtonNo_Click;
            buttonNo.Resize(buttonSize, buttonSize);
            buttonNo.Move(Width - buttonSize - margin, y);
            buttonNo.ToolTip = CancelLabel;
            y += buttonNo.Height + margin;

            Resize(Width, y);
            UpdateColumnNames();

            AddControl(buttonYes);
            AddControl(buttonNo);
            AddControl(buttonComputer);
            AddControl(gridFiles);
            AddControl(textFile);
            AddControl(dropDownType);

            UpdatePathBar();
            CenterToWindow();

            gridFiles.GrabDialogFocus();
        }

        private void ButtonComputer_Click(Control sender)
        {
            GoToComputer();
        }

        private void UpdatePathBar()
        {
            buttonComputer.AutosizeWidth();

            foreach (var btn in buttonsPath)
                RemoveControl(btn);

            buttonsPath.Clear();
            buttonPathPaths.Clear();

            if (path != null)
            {
                var splits = path.Split(new[] {
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries);

                var tempButton = new Button("FileFolder", "");
                var buttonSizes = new int[splits.Length];
                var totalSize = 0;

                // HACK : Use a temp button to measure the length.
                for (int i = 0; i < splits.Length; i++)
                {
                    tempButton.Text = TrimLongFilename(splits[i]);
                    AddControl(tempButton);
                    tempButton.AutosizeWidth();
                    buttonSizes[i] = tempButton.Width;
                    totalSize += tempButton.Width + (i > 0 ? margin : 0);
                    RemoveControl(tempButton);
                }

                var maxWidth = width - margin * 2 - buttonComputer.Width;
                var startButtonIndex = 0;

                // If there is not enough space, only add the end folders.
                if (totalSize > maxWidth)
                {
                    startButtonIndex = splits.Length - 1;
                    totalSize = 0;

                    for (int i = splits.Length - 1; i >= 0; i--)
                    {
                        var newTotalSize = totalSize + buttonSizes[i];

                        if (i != splits.Length - 1)
                            newTotalSize += margin;

                        if (newTotalSize > maxWidth)
                            break;

                        totalSize = newTotalSize;
                        startButtonIndex = i;
                    }
                }

                var x = margin * 2 + buttonComputer.Width;

                for (int i = startButtonIndex; i < splits.Length; i++)
                {
                    var button = new Button("FileFolder", TrimLongFilename(splits[i]));
                    AddControl(button);
                    button.Move(x, margin + titleBarSizeY, 100, pathButtonSizeY);
                    button.AutosizeWidth();
                    button.Click += Button_Click;
                    buttonsPath.Add(button);
                    buttonPathPaths.Add(BuildPath(splits, i));
                    x += button.Width + margin;
                }
            }
        }

        private void Button_Click(Control sender)
        {
            var idx = buttonsPath.IndexOf(sender as Button);
            GoToPath(buttonPathPaths[idx]);
        }

        private string BuildPath(string[] elements, int maxIndex)
        {
            string p = Path.VolumeSeparatorChar == Path.DirectorySeparatorChar ? "/" : ""; 

            p += elements[0];

            if (p[p.Length - 1] != Path.DirectorySeparatorChar ||
                p[p.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                p += Path.DirectorySeparatorChar;
            }

            for (int i = 1; i <= maxIndex; i++)
                p = Path.Combine(p, elements[i]);

            return p;
        }

        private string TrimLongFilename(string f)
        {
            if (f.Length > 24)
            {
                return f.Substring(0, 24) + "...";
            }
            else
            {
                return f;
            }
        }

        private void DropDownType_SelectedIndexChanged(Control sender, int index)
        {
            if (path != null)
                GoToPath(path);
        }

        private void GridFiles_HeaderCellClicked(Control sender, int colIndex)
        {
            if (path != null)
            {
                if (colIndex == 1) // Name
                {
                    if (sortByDate)
                        sortByDate = false;
                    else
                        sortDesc = !sortDesc;
                }
                else if (colIndex == 2) // Date
                {
                    if (sortByDate)
                        sortDesc = !sortDesc;
                    else
                        sortByDate = true;
                }

                GoToPath(path, false);
            }
        }

        private void GridFiles_CellClicked(Control sender, bool left, int rowIndex, int colIndex)
        {
            if (left)
            {
                UpdateTextByIndex(rowIndex);
            }
        }

        private void GridFiles_EmptyCellClicked(Control sender, bool left)
        {
            textFile.Text = "";
            gridFiles.ResetSelectedRow();
        }

        private void GridFiles_CellDoubleClicked(Control sender, int rowIndex, int colIndex)
        {
            var f = files[rowIndex];

            if (f.Type == EntryType.Drive)
            {
                var driveInfo = new DriveInfo(f.Name);
                GoToPath(driveInfo.RootDirectory.FullName);
            }
            else if (f.Type == EntryType.Directory)
            {
                GoToPath(f.Path);
            }
            else
            {
                textFile.Text = f.Name;
                ValidateAndClose();
            }
        }

        private void GridFiles_SelectedRowUpdated(Control sender, int index)
        {
            Debug.Assert(index < files.Count);
            UpdateTextByIndex(index);
        }

        private void TryOpenOrValidate()
        {
            if (!OpenFolderOrDrive())
                ValidateAndClose();
        }

        private bool OpenFolderOrDrive()
        {
            if (string.IsNullOrWhiteSpace(textFile.Text))
                return false;

            var f = files.FirstOrDefault(file => file.Name.Contains(textFile.Text, StringComparison.OrdinalIgnoreCase));

            return f?.Type switch
            {
                EntryType.Drive     => GoToPath(new DriveInfo(f.Name).RootDirectory.FullName),
                EntryType.Directory => GoToPath(f.Path),
                null                => false,
                _                   => false
            };
        }

        private bool ValidateAndClose()
        {
            if (path != null)
            {
                var f = Path.Combine(path, textFile.Text);

                if (mode == Mode.Open)
                {
                    if (File.Exists(f))
                    {
                        filename = f;
                        Close(DialogResult.OK);
                        return true;
                    }

                    Platform.Beep();
                }
                else if (mode == Mode.Save)
                {
                    if (string.IsNullOrEmpty(textFile.Text))
                        return false;

                    // Force extension.
                    if (!MatchesExtensionList(f, extensions))
                        f += extensions[0].Trim('*');

                    if (!File.Exists(f) || Platform.MessageBox(ParentWindow, OverwriteFileMessage.Format(Path.GetFileName(f)), OverwriteFileTitle, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        filename = f;
                        Close(DialogResult.OK);
                        return true;
                    }
                }
                else
                {
                    if (Directory.Exists(path))
                    {
                        filename = path;
                        Close(DialogResult.OK);
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdateTextByIndex(int index)
        {
            textFile.Text = index >= 0 ? files[index].Name : string.Empty;
        }

        private void UpdateColumnNames()
        {
            var colNames = new[]
            {
                "",
                 sortByDate || path == null ? NameLabel : sortDesc ? $"{NameLabel} ({DescendLabel})" : $"{NameLabel} ({AscendLabel})",
                !sortByDate || path == null ? DateLabel : sortDesc ? $"{DateLabel} ({DescendLabel})" : $"{DateLabel} ({AscendLabel})"
            };

            gridFiles.RenameColumns(colNames);
        }

        private object[,] GetGridData(List<FileEntry> files)
        {
            var data = new object[files.Count, 3];
            var zeroDate = default(DateTime);

            for (int i = 0; i < files.Count; i++)
            {
                var f = files[i];

                data[i, 0] = f.Image;
                data[i, 1] = f.Name;
                data[i, 2] = f.Date == zeroDate ? "" : f.Date.ToString();
            }

            return data;
        }

        private void GoToComputer()
        {
            var userDir      = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var desktopDir   = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var downloadDir  = Path.Combine(userDir, "Downloads");

            files.Clear();

            if (Directory.Exists(userDir))
                files.Add(new FileEntry("FileHome", HomeFolderLabel, userDir, EntryType.Directory));
            if (Directory.Exists(desktopDir))
                files.Add(new FileEntry("FileDesktop", DesktopFolderLabel, desktopDir, EntryType.Directory));
            if (Directory.Exists(downloadDir))
                files.Add(new FileEntry("FileDownload", DownloadsFolderLabel, downloadDir, EntryType.Directory));
            if (Directory.Exists(documentsDir))
                files.Add(new FileEntry("FileDocuments", DocumentsFolderLabel, documentsDir, EntryType.Directory));

            if (Platform.IsLinux)
            {
                // On Linux, this seem to work better.
                // Environment.GetLogicalDrives() returns things that are not drives.
                var drives = DriveInfo.GetDrives();

                foreach (var d in drives)
                {
                    if (d.IsReady)
                        files.Add(new FileEntry("FileDisk", d.Name, null, EntryType.Drive));
                }
            }
            else
            {
                // On Windows, this is way faster, networked drives not take 10 secs to load.
                var drives = Environment.GetLogicalDrives();
                foreach (var d in drives)
                { 
                    files.Add(new FileEntry("FileDisk", d, null, EntryType.Drive));
                }
            }

            path = null;
            gridFiles.UpdateData(GetGridData(files));
            UpdateColumnNames();
            UpdatePathBar();
        }

        private string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        private bool MatchesExtensionList(string s, string[] extensions)
        {
            foreach (var ext in extensions)
            {
                // HACK : Files with no extensions fails the regex check below and i cant
                // be bothered to debug it.
                if (ext == "*.*")
                    return true;
                if (Regex.IsMatch(s, WildCardToRegular(ext)))
                    return true;
            }

            return false;
        }
        
        private bool GoToPath(string p, bool stack = true)
        {
            if (string.IsNullOrEmpty(p))
            {
                GoToComputer();
                return false;
            }

            var dirInfo = new DirectoryInfo(p);

            DirectoryInfo[] dirInfos;
            FileInfo[] fileInfos;

            // Prevent a crash if a file or directory doesn't have access permissions.
            try
            {
                dirInfos = dirInfo.GetDirectories();
            }
            catch
            {
                Platform.MessageBoxAsync(ParentWindow, FolderNoAccessMessage.Format(Path.GetFileName(p)), FolderNoAccessTitle, MessageBoxButtons.OK);
                return false;
            }

            try
            {
                fileInfos = dirInfo.GetFiles();
            }
            catch
            {
                Platform.MessageBoxAsync(ParentWindow, FileNoAccessMessage.Format(Path.GetFileName(p)), FileNoAccessTitle, MessageBoxButtons.OK);
                return false;
            }

            files.Clear();

            var sortSign = sortDesc ? -1 : 1;
            var comp = sortByDate ?
                new Comparison<FileSystemInfo>((a, b) => a.LastWriteTime.CompareTo(b.LastWriteTime) * sortSign) :
                new Comparison<FileSystemInfo>((a, b) => a.Name.CompareTo(b.Name) * sortSign);

            Array.Sort(dirInfos, comp);
            Array.Sort(fileInfos, comp);

            foreach (var di in dirInfos)
            {
                if (!di.Attributes.HasFlag(FileAttributes.Hidden))
                    files.Add(new FileEntry("FileFolder", di.Name, di.FullName, EntryType.Directory, di.LastWriteTime));
            }

            if (mode != Mode.Folder)
            {
                var validExtentions = extensions[dropDownType.SelectedIndex].ToLower().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var fi in fileInfos)
                {
                    if (!fi.Attributes.HasFlag(FileAttributes.Hidden) && MatchesExtensionList(fi.Name.ToLower(), validExtentions))
                        files.Add(new FileEntry("FileFile", fi.Name, fi.FullName, EntryType.File, fi.LastWriteTime));
                }
            }

            filename = null;
            textFile.Text = "";
            
            path = p;

            gridFiles.UpdateData(GetGridData(files));
            gridFiles.ResetScroll();
            gridFiles.ResetSelectedRow();
            UpdateColumnNames();
            UpdatePathBar();

            return true;
        }

        private void ButtonYes_Click(Control sender)
        {
            TryOpenOrValidate();
        }

        private void ButtonNo_Click(Control sender)
        {
            Close(DialogResult.Cancel);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!e.Handled)
            {
                if (e.Key == Keys.Enter || e.Key == Keys.KeypadEnter)
                {
                    TryOpenOrValidate();
                }
                else if (e.Key == Keys.Escape)
                {
                    Close(DialogResult.Cancel);
                }
            }
        }
    }
}
